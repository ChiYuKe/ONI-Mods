param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed'
)
$ErrorActionPreference = 'Stop'
foreach ($file in Get-ChildItem -LiteralPath $GameManagedDir -Filter 'UnityEngine.*.dll') {
    try { [void][Reflection.Assembly]::LoadFrom($file.FullName) } catch { }
}
foreach ($file in @('Unity.TextMeshPro.dll','Assembly-CSharp-firstpass.dll','Assembly-CSharp.dll','0Harmony.dll','Newtonsoft.Json.dll')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $GameManagedDir $file))
}
$basePath = Join-Path $PSScriptRoot '..\StorageNetwork\bin\Release\StorageNetwork.dll'
$addonPath = Join-Path $PSScriptRoot '..\StorageNetworkTrading\bin\Release\StorageNetworkTrading.dll'
$base = [Reflection.Assembly]::LoadFrom((Resolve-Path $basePath).Path)
$addon = [Reflection.Assembly]::LoadFrom((Resolve-Path $addonPath).Path)
if (@($base.GetTypes() | Where-Object { $_.Namespace -eq 'StorageNetwork.Trading' }).Count) { throw 'Trading types remain in prerequisite.' }
if (@($base.GetReferencedAssemblies() | Where-Object Name -eq 'StorageNetworkTrading').Count) { throw 'Prerequisite references add-on.' }
if (-not @($addon.GetReferencedAssemblies() | Where-Object Name -eq 'StorageNetwork').Count) { throw 'Add-on prerequisite reference missing.' }
if (Test-Path (Join-Path (Split-Path $addonPath) 'StorageNetwork.dll')) { throw 'Add-on bundles duplicate prerequisite.' }
$state = $addon.GetType('StorageNetwork.Trading.StorageNetworkTradingState', $true)
$panel = $addon.GetType('StorageNetworkTrading.UI.TradingPanel', $true)
if ($panel.BaseType.Name -ne 'KScreen') { throw 'Trading window is not independent.' }
if (-not $base.GetType('StorageNetwork.API.StorageNetworkInventoryAccess', $true).IsPublic) { throw 'Inventory bridge is not public.' }
if ($base.GetType('StorageNetwork.API.StorageNetworkApi', $true).GetProperty('ApiVersion').GetValue($null) -lt 1) {
    throw 'Versioned prerequisite API is missing.'
}

# Check actual compiled type dependencies, including types that appear only in
# method signatures (such as the old internal scene-snapshot parameter).
$stream = [IO.File]::OpenRead((Resolve-Path $addonPath).Path)
$pe = [Reflection.PortableExecutable.PEReader]::new($stream)
try {
    $reader = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    foreach ($handle in $reader.TypeReferences) {
        $typeRef = $reader.GetTypeReference($handle)
        $scope = $typeRef.ResolutionScope
        while ($scope.Kind -eq [Reflection.Metadata.HandleKind]::TypeReference) {
            $typeHandle = [Reflection.Metadata.TypeReferenceHandle]$scope
            $scope = $reader.GetTypeReference($typeHandle).ResolutionScope
        }
        if ($scope.Kind -ne [Reflection.Metadata.HandleKind]::AssemblyReference) { continue }
        $assemblyHandle = [Reflection.Metadata.AssemblyReferenceHandle]$scope
        if ($reader.GetString($reader.GetAssemblyReference($assemblyHandle).Name) -ne 'StorageNetwork') { continue }
        $namespace = $reader.GetString($typeRef.Namespace)
        if ($namespace -ne 'StorageNetwork.API') {
            throw ('Add-on bypasses public API: ' + $namespace + '.' + $reader.GetString($typeRef.Name))
        }
    }
}
finally { $pe.Dispose(); $stream.Dispose() }

$uiApi = $base.GetType('StorageNetwork.API.StorageNetworkUiAccess', $true)
$registerFocus = $uiApi.GetMethod('RegisterTextInputFocus')
$unregisterFocus = $uiApi.GetMethod('UnregisterTextInputFocus')
$queryFocus = $uiApi.GetMethod('IsExternalTextInputFocused', [Reflection.BindingFlags]'NonPublic,Static')
try {
    $registerFocus.Invoke($null, @('test-a', [Func[bool]]{ return $false })) | Out-Null
    $registerFocus.Invoke($null, @('test-b', [Func[bool]]{ return $true })) | Out-Null
    if (-not $queryFocus.Invoke($null, @())) { throw 'Multiple add-on focus providers did not combine.' }
    $unregisterFocus.Invoke($null, @('test-b')) | Out-Null
    if ($queryFocus.Invoke($null, @())) { throw 'Unregistering focused add-on did not restore input.' }
    $registerFocus.Invoke($null, @('test-a', [Func[bool]]{ return $true })) | Out-Null
    if (-not $queryFocus.Invoke($null, @())) { throw 'Replacing same add-on focus provider failed.' }
}
finally {
    $unregisterFocus.Invoke($null, @('test-a')) | Out-Null
    $unregisterFocus.Invoke($null, @('test-b')) | Out-Null
}
$entry = $addon.GetType('StorageNetworkTrading.ModEntry', $true)
$registerClose = $uiApi.GetMethod('RegisterRightClickClose')
$unregisterClose = $uiApi.GetMethod('UnregisterRightClickClose')
$processClose = $uiApi.GetMethod('ProcessRightClickClose', [Reflection.BindingFlags]'NonPublic,Static')
$resetClose = $uiApi.GetMethod('ResetRightClickGesture', [Reflection.BindingFlags]'NonPublic,Static')
$closeCounts = [Collections.Generic.Dictionary[string,int]]::new()
$closeCounts['low'] = $closeCounts['high'] = 0
function Invoke-RightClickFrame([int]$frame, [bool]$down, [bool]$up, [bool]$held, [float]$x) {
    $frameArguments = [object[]]@($frame, $down, $up, $held, [UnityEngine.Vector3]::new($x, 0, 0), $false)
    $handled = $processClose.Invoke($null, $frameArguments)
    return [pscustomobject]@{Handled=$handled; Closed=$frameArguments[5]}
}
try {
    $lowClose = [System.Action]({ $closeCounts['low']++ }.GetNewClosure())
    $highClose = [System.Action]({ $closeCounts['high']++ }.GetNewClosure())
    $registerClose.Invoke($null, @('test-low', [Func[bool]]{return $true}, $lowClose, 50)) | Out-Null
    $registerClose.Invoke($null, @('test-high', [Func[bool]]{return $true}, $highClose, 200)) | Out-Null
    $press = Invoke-RightClickFrame 100 $true $false $true 0
    if (-not $press.Handled -or $press.Closed) { throw 'Right click closed before release.' }
    $release = Invoke-RightClickFrame 101 $false $true $false 0
    $duplicate = Invoke-RightClickFrame 101 $false $true $false 0
    if (-not $release.Closed -or -not $duplicate.Handled -or $closeCounts['high'] -ne 1 -or $closeCounts['low'] -ne 0) {
        throw 'Right click did not close exactly one highest-priority window across repeated dispatch.'
    }
    $null = Invoke-RightClickFrame 102 $true $false $true 0
    $null = Invoke-RightClickFrame 103 $false $false $true 20
    $dragRelease = Invoke-RightClickFrame 104 $false $true $false 0
    if ($dragRelease.Closed -or $closeCounts['high'] -ne 1) { throw 'Right dragging and returning to origin closed the window.' }
    $unregisterClose.Invoke($null, @('test-high')) | Out-Null
    $null = Invoke-RightClickFrame 105 $true $false $true 0
    $release = Invoke-RightClickFrame 106 $false $true $false 0
    if (-not $release.Closed -or $closeCounts['low'] -ne 1) { throw 'Unregistering top window did not restore lower close handler.' }
}
finally {
    $unregisterClose.Invoke($null, @('test-low')) | Out-Null
    $unregisterClose.Invoke($null, @('test-high')) | Out-Null
    $resetClose.Invoke($null, @()) | Out-Null
}
$factory = $entry.GetMethod('GetHeaderButtons', [Reflection.BindingFlags]'NonPublic,Static')
$buttons = @($factory.Invoke($null, @()))
if ($buttons.Count -ne 1 -or $buttons[0].Id -ne 'StorageNetworkTrading') { throw 'Trading entry registration mismatch.' }
$sourcePo = Get-FileHash (Join-Path $PSScriptRoot '..\StorageNetworkTrading\translations\en.po')
$outputPo = Get-FileHash (Join-Path (Split-Path $addonPath) 'translations\en.po')
if ($sourcePo.Hash -ne $outputPo.Hash) { throw 'Prerequisite translation overwrote add-on translation.' }
Write-Output 'Trading mod split verified: API-only dependencies, API version, focus providers, right-click priority/drag/single-close behavior, independent UI and isolated output.'
