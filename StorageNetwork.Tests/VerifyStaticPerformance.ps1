param(
    [string] $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Core') {
    $pwsh = Get-Command pwsh -ErrorAction Stop
    & $pwsh.Source -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
        -RepositoryRoot $RepositoryRoot
    exit $LASTEXITCODE
}

$sourceRoot = Join-Path $RepositoryRoot 'StorageNetwork'
$failures = [Collections.Generic.List[string]]::new()

function Add-Failure([string] $message) {
    $script:failures.Add($message)
}

function Get-Source([string] $relativePath) {
    $path = Join-Path $RepositoryRoot ($relativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required verification source is missing: $relativePath"
    }

    return [IO.File]::ReadAllText($path)
}

function Get-MethodRange([string] $source, [string] $marker) {
    $start = $source.IndexOf($marker, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        throw "Required method marker was not found: $marker"
    }

    $brace = $source.IndexOf('{', $start)
    if ($brace -lt 0) {
        throw "Required method body was not found: $marker"
    }

    $depth = 0
    for ($index = $brace; $index -lt $source.Length; $index++) {
        if ($source[$index] -eq '{') {
            $depth++
        }
        elseif ($source[$index] -eq '}') {
            $depth--
            if ($depth -eq 0) {
                return [pscustomobject]@{
                    Start = $start
                    End = $index
                    Body = $source.Substring($start, $index - $start + 1)
                }
            }
        }
    }

    throw "Required method body was unterminated: $marker"
}

function Test-IndexInRanges([int] $index, [object[]] $ranges) {
    foreach ($range in $ranges) {
        if ($index -ge $range.Start -and $index -le $range.End) {
            return $true
        }
    }

    return $false
}

# The deterministic harness is a first-class solution project; a previous local
# ignore rule silently removed it from review/CI coverage.
$solution = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'NOIMods.sln'))
$gitIgnore = [IO.File]::ReadAllText((Join-Path $RepositoryRoot '.gitignore'))
if (-not $solution.Contains('"StorageNetwork.Tests", "StorageNetwork.Tests\StorageNetwork.Tests.csproj"', [StringComparison]::Ordinal)) {
    Add-Failure 'StorageNetwork.Tests is no longer included in NOIMods.sln.'
}
if ($gitIgnore -match '(?m)^\s*StorageNetwork\.Tests[/\\]\s*$') {
    Add-Failure 'StorageNetwork.Tests is ignored and would silently disappear from CI.'
}

# Sprite lookup must remain asset-bundle/direct-resource based. A global Resources
# scan allocates and scales with every loaded asset, even when the panel is hidden.
foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -File -Recurse) {
    $source = [IO.File]::ReadAllText($file.FullName)
    if ($source -match 'Resources\s*\.\s*FindObjectsOfTypeAll\s*<\s*Sprite\s*>' -or
        $source -match 'Resources\s*\.\s*FindObjectsOfTypeAll\s*\(\s*typeof\s*\(\s*Sprite\s*\)') {
        Add-Failure "Global Sprite resource scan is forbidden: $($file.FullName)"
    }
}

# Scene-wide component scans are permitted only for the one-time bootstrap and
# explicitly named compatibility migrations below. New runtime fallbacks must use
# lifecycle registries instead of expanding this list.
$allowedSceneScans = @{
    'StorageNetwork/Core/StorageSceneRegistry.cs' = @(
        'private static IEnumerable<int> EnumerateSceneSeed()'
    )
    'StorageNetwork/Services/Supply/StorageNetworkConstructionSupplyService.cs' = @(
        'public static void CleanupLegacyTagsFromNativeStorages()'
    )
    'StorageNetwork/Services/StorageNetworkBuildingRegistry.cs' = @(
        'public static void RebuildFromScene()'
    )
    'StorageNetwork/Game/StorageNetworkEnrollmentInstaller.cs' = @(
        'public static void RefreshPlainStorageInstances()',
        'public static void RefreshGeyserInstances()'
    )
}

foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -File -Recurse) {
    $relativePath = [IO.Path]::GetRelativePath($RepositoryRoot, $file.FullName).Replace('\', '/')
    $source = [IO.File]::ReadAllText($file.FullName)
    $ranges = @()
    if ($allowedSceneScans.ContainsKey($relativePath)) {
        foreach ($marker in $allowedSceneScans[$relativePath]) {
            $ranges += Get-MethodRange $source $marker
        }
    }

    foreach ($pattern in @(
        'FindObjectsByType<',
        'FindObjectsByType(',
        'FindObjectsOfType<',
        'FindObjectsOfType(',
        'FindObjectsOfTypeAll<',
        'FindObjectsOfTypeAll('
    )) {
        $searchFrom = 0
        while ($searchFrom -lt $source.Length) {
            $index = $source.IndexOf($pattern, $searchFrom, [StringComparison]::Ordinal)
            if ($index -lt 0) {
                break
            }

            if (-not (Test-IndexInRanges $index $ranges)) {
                $line = 1 + ($source.Substring(0, $index).Split("`n").Length - 1)
                Add-Failure "Scene-wide scan outside bootstrap/migration whitelist: ${relativePath}:$line ($pattern)"
            }
            $searchFrom = $index + $pattern.Length
        }
    }
}

# Storage.MassStored is a game-wide patch. Its negative path is intentionally only
# the overflow-capacity guard; resolving flags/components here taxes every vanilla
# Storage in the colony.
$massPatchPath = 'StorageNetwork/Patches/Buildings/StorageNetworkLargeStorageMassPatch.cs'
$massPatch = Get-Source $massPatchPath
$massPrefix = (Get-MethodRange $massPatch 'public static bool Prefix(Storage __instance, ref float __result)').Body
$massGuard = (Get-MethodRange $massPatch 'private static bool ShouldUseSafeMassStored(').Body
foreach ($forbidden in @(
    'GetComponent',
    'TryGetComponent',
    'StorageNetworkStorageRules',
    'StorageNetworkInterfaceResolver',
    'StorageSceneRegistry',
    'Flags',
    'Resolve'
)) {
    if ($massGuard.Contains($forbidden, [StringComparison]::Ordinal)) {
        Add-Failure "MassStored miss guard contains forbidden '$forbidden'."
    }
}
if (-not $massGuard.Contains('storage.capacityKg >= UnsafeVanillaRoundedMassKg', [StringComparison]::Ordinal)) {
    Add-Failure 'MassStored miss guard no longer uses the O(1) overflow-capacity threshold.'
}
if ($massPrefix.IndexOf('if (!ShouldUseSafeMassStored(__instance))', [StringComparison]::Ordinal) -lt 0 -or
    $massPrefix.IndexOf('return true;', [StringComparison]::Ordinal) -lt 0 -or
    $massPrefix.IndexOf('__instance.ExactMassStored()', [StringComparison]::Ordinal) -lt
        $massPrefix.IndexOf('return true;', [StringComparison]::Ordinal)) {
    Add-Failure 'MassStored Prefix no longer exits through the capacity-only miss guard before native work.'
}

# Other game-wide patches must reject vanilla objects through lifecycle-owned O(1)
# sets before any component resolution, reflection, or network rule evaluation.
$fetchPatch = Get-Source 'StorageNetwork/Patches/Buildings/StorageNetworkInputPortFetchLoopPatch.cs'
$fetchPostfix = (Get-MethodRange $fetchPatch 'public static void Postfix(Pickupable pickup, FetchChore chore, Storage destination, ref bool __result)').Body
$fetchRegistration = $fetchPostfix.IndexOf(
    'StorageSceneRegistry.IsExplicitlyRegisteredStorage(destination)',
    [StringComparison]::Ordinal)
$fetchComponent = $fetchPostfix.IndexOf(
    'destination.GetComponent<StorageNetworkSolidInputPortIngress>()',
    [StringComparison]::Ordinal)
$fetchRules = $fetchPostfix.IndexOf(
    'StorageNetworkStorageRules.IsSolidInputPort(destination)',
    [StringComparison]::Ordinal)
if ($fetchRegistration -lt 0 -or
    $fetchComponent -lt $fetchRegistration -or
    $fetchRules -lt $fetchRegistration) {
    Add-Failure 'FetchManager miss path no longer checks the O(1) registry before components/rules.'
}

$constructionPatch = Get-Source 'StorageNetwork/Patches/Production/SolidOutputConstructionReservePatch.cs'
$storageTransferPrefix = (Get-MethodRange $constructionPatch 'public static void Prefix(Storage __instance, UnityEngine.GameObject go)').Body
if (-not $storageTransferPrefix.Contains(
        'StorageNetworkConstructionSupplyService.IsRegisteredSolidOutputPort(__instance)',
        [StringComparison]::Ordinal) -or
    $storageTransferPrefix.Contains('GetComponent', [StringComparison]::Ordinal) -or
    $storageTransferPrefix.Contains('StorageNetworkStorageRules', [StringComparison]::Ordinal)) {
    Add-Failure 'Storage.Transfer construction miss path is not an O(1) registered-port guard.'
}
$constructionService = Get-Source 'StorageNetwork/Services/Supply/StorageNetworkConstructionSupplyService.cs'
$registeredPortGuard = (Get-MethodRange $constructionService 'public static bool IsRegisteredSolidOutputPort(').Body
if ($registeredPortGuard -notmatch '\b[A-Za-z]\w*Storages\.Contains\(storage\)' -or
    $registeredPortGuard.Contains('GetComponent', [StringComparison]::Ordinal) -or
    $registeredPortGuard -match 'FindObjects') {
    Add-Failure 'Construction registered-port guard no longer uses only its lifecycle set.'
}

$batteryPatch = Get-Source 'StorageNetwork/Patches/Buildings/StorageNetworkPowerOverlayBatterySyncPatch.cs'
$batteryPostfix = (Get-MethodRange $batteryPatch 'public static void Postfix(BatteryUI __instance, Battery bat)').Body
$batteryGuard = $batteryPostfix.IndexOf('!ShouldForceWhite(bat)', [StringComparison]::Ordinal)
$batteryFieldRead = $batteryPostfix.IndexOf('BatteryBGRef(__instance)', [StringComparison]::Ordinal)
if ($batteryGuard -lt 0 -or $batteryFieldRead -lt $batteryGuard) {
    Add-Failure 'BatteryUI miss path no longer exits through the registered-battery guard before field reads.'
}
$batteryComponent = Get-Source 'StorageNetwork/Buildings/Infrastructure/Components/StorageNetworkPowerOverlayBattery.cs'
$whiteUiGuard = (Get-MethodRange $batteryComponent 'internal static bool ShouldForceWhiteUi(Battery battery)').Body
if (-not $whiteUiGuard.Contains('WhiteUiBatteries.Contains(battery)', [StringComparison]::Ordinal) -or
    $whiteUiGuard.Contains('GetComponent', [StringComparison]::Ordinal) -or
    $whiteUiGuard.Contains('AccessTools', [StringComparison]::Ordinal)) {
    Add-Failure 'BatteryUI registered-battery guard no longer uses only its lifecycle set.'
}

# The optional Harmony profiler must select explicit StorageNetwork hot roots. It
# must never enumerate all loaded assemblies/types or emit per-call log messages.
$profilerPath = 'StorageNetwork/Core/StorageNetworkHarmonyProfileTool.cs'
$profiler = Get-Source $profilerPath
if (-not $profiler.Contains('private static readonly Type[] ExplicitProfileTypes', [StringComparison]::Ordinal) -or
    -not $profiler.Contains('foreach (Type type in ExplicitProfileTypes)', [StringComparison]::Ordinal)) {
    Add-Failure 'Harmony profiler is not rooted in an explicit type whitelist.'
}
foreach ($forbidden in @('AppDomain.CurrentDomain', 'GetAssemblies()', 'assembly.GetTypes()', 'Assembly.GetTypes()')) {
    if ($profiler.Contains($forbidden, [StringComparison]::Ordinal)) {
        Add-Failure "Harmony profiler contains broad assembly/type enumeration: $forbidden"
    }
}
foreach ($marker in @(
    'public static void Prefix(',
    'public static void Postfix(',
    'public static Exception Finalizer('
)) {
    $body = (Get-MethodRange $profiler $marker).Body
    if ($body -match 'Debug\s*\.\s*Log') {
        Add-Failure "Harmony profiler hot callback emits per-call logging: $marker"
    }
}

# Keep the aggregate profiler schema stable so benchmark windows and log parsers
# can compare the same subsystem names across releases.
$frameProfiler = Get-Source 'StorageNetwork/Core/StorageNetworkFrameProfileTool.cs'
$performanceAreas = (Get-MethodRange $frameProfiler 'internal enum StorageNetworkPerformanceArea').Body
foreach ($area in @(
    'Registry',
    'ContentIndex',
    'SourceSelection',
    'TargetSelection',
    'Transfer',
    'Reservation',
    'Power',
    'ProductionMaintenance',
    'ProductionPlanning',
    'LogicDiy',
    'MainPanel',
    'CategorySummary',
    'OrderEditor',
    'Tracking',
    'LiquidSideScreen',
    'WorldPanel',
    'Layout',
    'GlobalHarmony'
)) {
    if ($performanceAreas -notmatch "(?m)^\s*$([regex]::Escape($area))\s*,") {
        Add-Failure "PerformanceArea schema is missing '$area'."
    }
}
if (-not $frameProfiler.Contains('modAlloc=unsupported', [StringComparison]::Ordinal)) {
    Add-Failure 'Frame profiler no longer reports unsupported allocation tracking explicitly.'
}
$beginWork = (Get-MethodRange $frameProfiler 'public static WorkScope BeginWork(StorageNetworkPerformanceArea area)').Body
$disabledGuard = $beginWork.IndexOf(
    'Volatile.Read(ref frameProfilerEnabled) == 0',
    [StringComparison]::Ordinal)
$disabledReturn = $beginWork.IndexOf('return default;', [StringComparison]::Ordinal)
$timestampRead = $beginWork.IndexOf('Stopwatch.GetTimestamp()', [StringComparison]::Ordinal)
if ($disabledGuard -lt 0 -or
    $disabledReturn -lt $disabledGuard -or
    $timestampRead -lt $disabledReturn) {
    Add-Failure 'Disabled frame profiling no longer exits before timers/allocation tracking.'
}

# Transfer callers must enter one ambient content-index transaction at each public
# service boundary. This catches accidental removal even though runtime state uses
# private ONI types that cannot be instantiated in the net8 deterministic harness.
$transferService = Get-Source 'StorageNetwork/Services/Transfer/NetworkStorageTransferService.cs'
if (-not $transferService.Contains('StorageNetworkContentIndexService.BeginTransferTransaction(worldId)', [StringComparison]::Ordinal) -or
    -not $transferService.Contains('StorageNetworkContentIndexService.BeginTransferTransaction(source, target)', [StringComparison]::Ordinal) -or
    -not $transferService.Contains('StorageNetworkContentIndexService.EndTransferTransaction()', [StringComparison]::Ordinal)) {
    Add-Failure 'Public transfer service no longer brackets writes in one content-index transaction.'
}
$transferScopes = @{
    'public static StorageTransferResult TransferStoredItemsToNetwork(' = 'sourceWorldId'
    'public static StorageTransferResult TransferStoredFluidsToNetwork(' = 'sourceWorldId'
    'public static StorageTransferResult TransferLooseItemToNetwork(' = 'sourceWorldId'
    'public static StorageTransferResult TransferStoredItemToNetwork(' = 'sourceWorldId'
    'public static float TransferFromNetworkToStorage(' = 'destinationWorldId'
    'public static float TransferMatchingItemsFromStorage(' = 'source, destination'
    'public static float TransferMatchingItemUnitsFromStorage(' = 'source, destination'
    'public static StorageTransferResult TransferAnySolidFromNetworkToStorage(' = 'destinationWorldId'
    'private static StorageTransferResult TransferAnyElementStateFromNetworkToStorage(' = 'destinationWorldId'
}
foreach ($entry in $transferScopes.GetEnumerator()) {
    $body = (Get-MethodRange $transferService $entry.Key).Body
    if (-not $body.Contains("using (workspace.BeginContentIndexTransaction($($entry.Value)))", [StringComparison]::Ordinal)) {
        Add-Failure "Transfer boundary must bracket writes in its explicit world/endpoint scope: $($entry.Key)"
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure -ErrorAction Continue
    }
    throw "$($failures.Count) static performance contract failures."
}

Write-Output 'Static performance contracts passed: profiler schema, Sprite/scene scans, global patch miss paths, Harmony scope, transfer transaction.'
