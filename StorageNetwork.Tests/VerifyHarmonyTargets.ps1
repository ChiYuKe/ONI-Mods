param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = (Join-Path $PSScriptRoot '..\StorageNetwork\bin\Release\StorageNetwork.dll')
)

$ErrorActionPreference = 'Stop'

# Windows PowerShell runs on .NET Framework and cannot inspect this netstandard2.1
# assembly reliably. Relaunch under the installed .NET-based PowerShell host.
if ($PSVersionTable.PSEdition -ne 'Core') {
    $pwsh = Get-Command pwsh -ErrorAction Stop
    & $pwsh.Source -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
        -GameManagedDir $GameManagedDir `
        -ModAssembly $ModAssembly
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $GameManagedDir -PathType Container)) {
    throw "Game managed directory does not exist: $GameManagedDir"
}
if (-not (Test-Path -LiteralPath $ModAssembly -PathType Leaf)) {
    throw "StorageNetwork assembly does not exist: $ModAssembly"
}

foreach ($unityDependency in Get-ChildItem -LiteralPath $GameManagedDir -Filter 'UnityEngine.*.dll' -File) {
    try {
        [void][Reflection.Assembly]::LoadFrom($unityDependency.FullName)
    }
    catch {
        # Only the Unity modules referenced by the mod must load; optional modules
        # may have native/runtime-only dependencies outside this verification host.
    }
}

foreach ($dependency in @(
    'Unity.TextMeshPro.dll',
    'Assembly-CSharp-firstpass.dll',
    'Assembly-CSharp.dll',
    '0Harmony.dll',
    'Newtonsoft.Json.dll'
)) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $GameManagedDir $dependency))
}

$harmonyAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $GameManagedDir '0Harmony.dll'))
$mod = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ModAssembly))
$harmonyPatch = $harmonyAssembly.GetType('HarmonyLib.HarmonyPatch', $true)
$harmonyMethodExtensions = $harmonyAssembly.GetType('HarmonyLib.HarmonyMethodExtensions', $true)
$patchTools = $harmonyAssembly.GetType('HarmonyLib.PatchTools', $true)
$getMergedFromType = $harmonyMethodExtensions.GetMethod(
    'GetMergedFromType',
    [Reflection.BindingFlags]'Static,Public,NonPublic')
$getOriginalMethod = $patchTools.GetMethod(
    'GetOriginalMethod',
    [Reflection.BindingFlags]'Static,Public,NonPublic')
$harmonyMethodType = $harmonyAssembly.GetType('HarmonyLib.MethodType', $true)
$normalMethodType = [Enum]::Parse($harmonyMethodType, 'Normal')
$methodTypeField = $harmonyAssembly.GetType('HarmonyLib.HarmonyMethod', $true).GetField('methodType')
$bindingFlags = [Reflection.BindingFlags]'Static,Public,NonPublic'
$checkedTypes = 0
$resolvedTargets = 0
$validatedSignatures = 0
$failed = 0

try {
    $modTypes = @($mod.GetTypes())
}
catch [Reflection.ReflectionTypeLoadException] {
    foreach ($loaderException in $_.Exception.LoaderExceptions) {
        Write-Error $loaderException.Message -ErrorAction Continue
    }
    throw 'StorageNetwork types could not be loaded for Harmony verification.'
}

foreach ($typeWrapper in $modTypes) {
    $type = $typeWrapper.psobject.BaseObject
    if (-not $type.IsDefined($harmonyPatch, $true)) {
        continue
    }

    $checkedTypes++
    $targets = [Collections.Generic.List[Reflection.MethodBase]]::new()
    try {
        $targetMethodsMethod = $type.GetMethod('TargetMethods', $bindingFlags)
        $targetMethodMethod = $type.GetMethod('TargetMethod', $bindingFlags)
        if ($null -ne $targetMethodsMethod) {
            foreach ($targetWrapper in @($targetMethodsMethod.Invoke($null, $null))) {
                if ($null -ne $targetWrapper) {
                    $targets.Add($targetWrapper.psobject.BaseObject)
                }
            }
        }
        elseif ($null -ne $targetMethodMethod) {
            $target = $targetMethodMethod.Invoke($null, $null)
            if ($null -ne $target) {
                $targets.Add($target.psobject.BaseObject)
            }
        }
        else {
            $typeArguments = [object[]]::new(1)
            $typeArguments[0] = $type
            $harmonyMethod = $getMergedFromType.Invoke($null, $typeArguments)
            if ($null -eq $methodTypeField.GetValue($harmonyMethod)) {
                $methodTypeField.SetValue($harmonyMethod, $normalMethodType)
            }

            $methodArguments = [object[]]::new(1)
            $methodArguments[0] = $harmonyMethod.psobject.BaseObject
            $target = $getOriginalMethod.Invoke($null, $methodArguments)
            if ($null -ne $target) {
                $targets.Add($target.psobject.BaseObject)
            }
        }
    }
    catch {
        $failed++
        Write-Error "Harmony target resolution threw for $($type.FullName): $($_.Exception.Message)" -ErrorAction Continue
        continue
    }

    if ($targets.Count -eq 0) {
        $failed++
        Write-Error "Harmony target did not resolve: $($type.FullName)" -ErrorAction Continue
        continue
    }

    $resolvedTargets += $targets.Count
    $patchMethods = @($type.GetMethods($bindingFlags) | Where-Object {
        $_.Name -eq 'Prefix' -or $_.Name -eq 'Postfix'
    })
    foreach ($target in $targets) {
        $targetParameters = @{}
        foreach ($parameter in $target.GetParameters()) {
            $targetParameters[$parameter.Name] = $parameter
        }

        foreach ($patchMethodWrapper in $patchMethods) {
            $patchMethod = $patchMethodWrapper.psobject.BaseObject
            $signatureValid = $true
            foreach ($parameter in $patchMethod.GetParameters()) {
                if ($parameter.Name -eq '__instance') {
                    $instanceType = if ($parameter.ParameterType.IsByRef) {
                        $parameter.ParameterType.GetElementType()
                    }
                    else {
                        $parameter.ParameterType
                    }
                    $signatureValid = $signatureValid -and
                        -not $target.IsStatic -and
                        $instanceType.IsAssignableFrom($target.DeclaringType)
                    continue
                }
                if ($parameter.Name -eq '__result') {
                    $resultType = if ($parameter.ParameterType.IsByRef) {
                        $parameter.ParameterType.GetElementType()
                    }
                    else {
                        $parameter.ParameterType
                    }
                    $signatureValid = $signatureValid -and $resultType -eq $target.ReturnType
                    continue
                }
                if ($parameter.Name.StartsWith('__')) {
                    continue
                }
                if (-not $targetParameters.ContainsKey($parameter.Name)) {
                    $signatureValid = $false
                    continue
                }

                $patchParameterType = if ($parameter.ParameterType.IsByRef) {
                    $parameter.ParameterType.GetElementType()
                }
                else {
                    $parameter.ParameterType
                }
                $targetParameterType = $targetParameters[$parameter.Name].ParameterType
                if ($targetParameterType.IsByRef) {
                    $targetParameterType = $targetParameterType.GetElementType()
                }
                $signatureValid = $signatureValid -and $patchParameterType -eq $targetParameterType
            }

            if (-not $signatureValid) {
                $failed++
                Write-Error "Harmony patch parameter mismatch: $($type.FullName).$($patchMethod.Name) -> $target" -ErrorAction Continue
            }
            else {
                $validatedSignatures++
            }
        }
    }
}

if ($checkedTypes -eq 0) {
    throw 'No Harmony patch classes were discovered.'
}
if ($failed -ne 0) {
    throw "$failed Harmony verification failures across $checkedTypes patch classes."
}

Write-Output "Resolved $resolvedTargets targets across $checkedTypes Harmony patch classes and validated $validatedSignatures patch signatures."

$profilerType = $mod.GetType('StorageNetwork.Core.StorageNetworkHarmonyProfileTool', $true)
$shouldProfileMethod = $profilerType.GetMethod(
    'ShouldProfileMethod',
    [Reflection.BindingFlags]'Static,NonPublic')
$explicitProfileTypesField = $profilerType.GetField(
    'ExplicitProfileTypes',
    [Reflection.BindingFlags]'Static,NonPublic')
$explicitProfileTypes = @($explicitProfileTypesField.GetValue($null))
$declaredFlags = [Reflection.BindingFlags]'Instance,Static,Public,NonPublic,DeclaredOnly'
$aggregateProfileTargetCount = 0
$declaredMethodCount = 0
foreach ($typeWrapper in $explicitProfileTypes) {
    $type = $typeWrapper.psobject.BaseObject
    foreach ($methodWrapper in @($type.GetMethods($declaredFlags))) {
        $method = $methodWrapper.psobject.BaseObject
        $declaredMethodCount++
        $methodArguments = [object[]]::new(1)
        $methodArguments[0] = $method
        if ($shouldProfileMethod.Invoke($null, $methodArguments)) {
            $aggregateProfileTargetCount++
        }
    }
}

if ($explicitProfileTypes.Count -le 0 -or
    $aggregateProfileTargetCount -le 0 -or
    $aggregateProfileTargetCount -gt $declaredMethodCount) {
    throw "Aggregate profiler selected an invalid number of roots: $aggregateProfileTargetCount of $declaredMethodCount declared methods."
}

Write-Output "Aggregate profiler explicit types=$($explicitProfileTypes.Count), selected roots=$aggregateProfileTargetCount of $declaredMethodCount declared methods."
