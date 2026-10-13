param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = (Join-Path $PSScriptRoot '..\StorageNetwork\bin\Release\StorageNetwork.dll'),
    [string] $Baseline = (Join-Path $PSScriptRoot 'PublicApiBaseline.txt'),
    [switch] $PrintCurrent
)

$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Core') {
    $pwsh = Get-Command pwsh -ErrorAction Stop
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $PSCommandPath,
        '-GameManagedDir', $GameManagedDir,
        '-ModAssembly', $ModAssembly,
        '-Baseline', $Baseline
    )
    if ($PrintCurrent) {
        $arguments += '-PrintCurrent'
    }
    & $pwsh.Source @arguments
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
        # Optional Unity modules can have runtime/native-only dependencies.
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

function Format-TypeName([Type] $type) {
    if ($type.IsByRef) {
        return (Format-TypeName $type.GetElementType()) + '&'
    }
    if ($type.IsPointer) {
        return (Format-TypeName $type.GetElementType()) + '*'
    }
    if ($type.IsArray) {
        $commas = if ($type.GetArrayRank() -gt 1) { ',' * ($type.GetArrayRank() - 1) } else { '' }
        return (Format-TypeName $type.GetElementType()) + '[' + $commas + ']'
    }
    if ($type.IsGenericParameter) {
        return $type.Name
    }
    if ($type.IsGenericType) {
        $definition = $type.GetGenericTypeDefinition()
        $name = $definition.FullName
        $tick = $name.IndexOf('`')
        if ($tick -ge 0) {
            $name = $name.Substring(0, $tick)
        }
        $arguments = @($type.GetGenericArguments() | ForEach-Object { Format-TypeName $_ })
        return $name.Replace('+', '.') + '<' + ($arguments -join ',') + '>'
    }

    return ($type.FullName ?? $type.Name).Replace('+', '.')
}

function Format-DefaultValue([object] $value) {
    if ($null -eq $value) {
        return 'null'
    }
    if ($value -is [string]) {
        return '"' + $value.Replace('\', '\\').Replace('"', '\"') + '"'
    }
    if ($value -is [char]) {
        return "'$value'"
    }
    if ($value -is [bool]) {
        return $value.ToString().ToLowerInvariant()
    }
    if ($value.GetType().IsEnum) {
        return (Format-TypeName $value.GetType()) + '.' + $value.ToString()
    }

    return [Convert]::ToString($value, [Globalization.CultureInfo]::InvariantCulture)
}

function Format-Parameter([Reflection.ParameterInfo] $parameter) {
    $type = $parameter.ParameterType
    $modifier = ''
    if ($type.IsByRef) {
        $modifier = if ($parameter.IsOut) { 'out ' } else { 'ref ' }
        $type = $type.GetElementType()
    }

    $formatted = $modifier + (Format-TypeName $type) + ' ' + $parameter.Name
    if ($parameter.IsOptional) {
        $default = $parameter.DefaultValue
        if ([object]::ReferenceEquals($default, [DBNull]::Value) -or
            [object]::ReferenceEquals($default, [Reflection.Missing]::Value)) {
            $formatted += ' [optional]'
        }
        else {
            $formatted += '=' + (Format-DefaultValue $default)
        }
    }
    return $formatted
}

function Format-Parameters([Reflection.ParameterInfo[]] $parameters) {
    return (@($parameters | ForEach-Object { Format-Parameter $_ }) -join ',')
}

function Get-TypeKind([Type] $type) {
    if (($type.Attributes -band [Reflection.TypeAttributes]::Interface) -ne 0) { return 'interface' }
    if ($type.IsEnum) { return 'enum' }
    if ($type.IsValueType) { return 'struct' }
    if ($type.BaseType -eq [MulticastDelegate]) { return 'delegate' }
    if ($type.IsAbstract -and $type.IsSealed) { return 'static class' }
    if ($type.IsAbstract) { return 'abstract class' }
    if ($type.IsSealed) { return 'sealed class' }
    return 'class'
}

function Get-PublicSurface([Reflection.Assembly] $assembly) {
    $facadeNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($name in @(
        'StorageNetwork.Core.StorageSceneRegistry',
        'StorageNetwork.Core.StorageSceneCollector',
        'StorageNetwork.Core.StorageSceneSnapshot',
        'StorageNetwork.Core.StorageSceneLightweightSnapshot',
        'StorageNetwork.Core.StorageInfo'
    )) {
        [void]$facadeNames.Add($name)
    }

    try {
        $types = @($assembly.GetTypes())
    }
    catch [Reflection.ReflectionTypeLoadException] {
        foreach ($loaderException in $_.Exception.LoaderExceptions) {
            Write-Error $loaderException.Message -ErrorAction Continue
        }
        throw 'StorageNetwork types could not be loaded for public API verification.'
    }

    $lines = [Collections.Generic.List[string]]::new()
    $flags = [Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly'
    foreach ($typeWrapper in $types) {
        $type = $typeWrapper.psobject.BaseObject
        if (-not ($type.IsPublic -or $type.IsNestedPublic) -or
            ($type.Namespace -ne 'StorageNetwork.API' -and -not $facadeNames.Contains($type.FullName))) {
            continue
        }

        $typeName = Format-TypeName $type
        $lines.Add('T ' + (Get-TypeKind $type) + ' ' + $typeName)

        foreach ($fieldWrapper in @($type.GetFields($flags))) {
            $field = $fieldWrapper.psobject.BaseObject
            $modifiers = if ($field.IsLiteral) { 'const ' } elseif ($field.IsStatic -and $field.IsInitOnly) { 'static readonly ' } elseif ($field.IsStatic) { 'static ' } elseif ($field.IsInitOnly) { 'readonly ' } else { '' }
            $line = 'F ' + $modifiers + (Format-TypeName $field.FieldType) + ' ' + $typeName + '.' + $field.Name
            if ($field.IsLiteral) {
                $line += '=' + (Format-DefaultValue $field.GetRawConstantValue())
            }
            $lines.Add($line)
        }

        foreach ($constructorWrapper in @($type.GetConstructors($flags))) {
            $constructor = $constructorWrapper.psobject.BaseObject
            $lines.Add('C ' + $typeName + '(' + (Format-Parameters $constructor.GetParameters()) + ')')
        }

        foreach ($propertyWrapper in @($type.GetProperties($flags))) {
            $property = $propertyWrapper.psobject.BaseObject
            $accessor = $property.GetMethod ?? $property.SetMethod
            $static = if ($null -ne $accessor -and $accessor.IsStatic) { 'static ' } else { '' }
            $access = ''
            if ($null -ne $property.GetMethod -and $property.GetMethod.IsPublic) { $access += 'get;' }
            if ($null -ne $property.SetMethod -and $property.SetMethod.IsPublic) { $access += 'set;' }
            $indexParameters = @($property.GetIndexParameters())
            $suffix = if ($indexParameters.Count -gt 0) { '[' + (Format-Parameters $indexParameters) + ']' } else { '' }
            $lines.Add('P ' + $static + (Format-TypeName $property.PropertyType) + ' ' + $typeName + '.' + $property.Name + $suffix + '{' + $access + '}')
        }

        foreach ($eventWrapper in @($type.GetEvents($flags))) {
            $eventInfo = $eventWrapper.psobject.BaseObject
            $addMethod = $eventInfo.AddMethod
            $static = if ($null -ne $addMethod -and $addMethod.IsStatic) { 'static ' } else { '' }
            $lines.Add('E ' + $static + (Format-TypeName $eventInfo.EventHandlerType) + ' ' + $typeName + '.' + $eventInfo.Name)
        }

        foreach ($methodWrapper in @($type.GetMethods($flags))) {
            $method = $methodWrapper.psobject.BaseObject
            if ($method.IsSpecialName) {
                continue
            }
            $static = if ($method.IsStatic) { 'static ' } else { '' }
            $generic = if ($method.IsGenericMethodDefinition) {
                '<' + (@($method.GetGenericArguments() | ForEach-Object { $_.Name }) -join ',') + '>'
            }
            else { '' }
            $lines.Add('M ' + $static + (Format-TypeName $method.ReturnType) + ' ' + $typeName + '.' + $method.Name + $generic + '(' + (Format-Parameters $method.GetParameters()) + ')')
        }
    }

    return @($lines | Sort-Object -CaseSensitive)
}

$mod = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ModAssembly))
$actual = @(Get-PublicSurface $mod)
if ($PrintCurrent) {
    $actual
    exit 0
}

if (-not (Test-Path -LiteralPath $Baseline -PathType Leaf)) {
    throw "Public API baseline does not exist: $Baseline"
}
$expected = @([IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Baseline)) |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith('#') } |
    Sort-Object -CaseSensitive)
$difference = @(Compare-Object -ReferenceObject $expected -DifferenceObject $actual -CaseSensitive)
if ($difference.Count -gt 0) {
    foreach ($entry in $difference) {
        $meaning = if ($entry.SideIndicator -eq '<=') { 'missing/changed' } else { 'unexpected public addition' }
        Write-Error "Public API ${meaning}: $($entry.InputObject)" -ErrorAction Continue
    }
    throw "$($difference.Count) public API signature differences from the locked baseline."
}

Write-Output "Public API surface matched $($expected.Count) locked signatures."
