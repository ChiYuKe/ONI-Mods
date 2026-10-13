param(
    [string] $LogPath = (Join-Path $env:USERPROFILE 'AppData\LocalLow\Klei\Oxygen Not Included\player.log'),
    [int] $SkipWindows = 2,
    [int] $MinimumWindows = 5,
    [int] $ExpectedActivePorts = -1,
    [double] $MaxModP95Ms = 2.0,
    [double] $MaxModP99Ms = 3.0,
    [double] $MaxModSpikeMs = 20.0,
    [switch] $RequireMainPanel,
    [switch] $RequireCategorySummary,
    [double] $MaxUiP95Ms = 0.5,
    [double] $MaxUiP99Ms = 1.0,
    [int] $MaxLayoutCalls = -1,
    [int] $MaxWorldSnapshotRebuilds = -1,
    [string] $OutputJson
)

$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Core') {
    $pwsh = Get-Command pwsh -ErrorAction Stop
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $PSCommandPath,
        '-LogPath', $LogPath,
        '-SkipWindows', $SkipWindows,
        '-MinimumWindows', $MinimumWindows,
        '-ExpectedActivePorts', $ExpectedActivePorts,
        '-MaxModP95Ms', $MaxModP95Ms,
        '-MaxModP99Ms', $MaxModP99Ms,
        '-MaxModSpikeMs', $MaxModSpikeMs,
        '-MaxUiP95Ms', $MaxUiP95Ms,
        '-MaxUiP99Ms', $MaxUiP99Ms,
        '-MaxLayoutCalls', $MaxLayoutCalls,
        '-MaxWorldSnapshotRebuilds', $MaxWorldSnapshotRebuilds
        '-OutputJson', $OutputJson
    )
    if ($RequireMainPanel) {
        $arguments += '-RequireMainPanel'
    }
    if ($RequireCategorySummary) {
        $arguments += '-RequireCategorySummary'
    }
    & $pwsh.Source @arguments
    exit $LASTEXITCODE
}

function Get-Number([string] $text, [string] $pattern, [double] $default = [double]::NaN) {
    $match = [regex]::Match($text, $pattern)
    if (-not $match.Success) {
        return $default
    }
    return [double]::Parse($match.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture)
}

function Complete-Window([Collections.Generic.List[object]] $target, [object] $window) {
    if ($null -ne $window -and $null -ne $window.ModP95Ms -and $null -ne $window.ActivePorts) {
        $target.Add($window)
    }
}

$resolvedLog = (Resolve-Path -LiteralPath $LogPath).Path
$stream = [IO.File]::Open(
    $resolvedLog,
    [IO.FileMode]::Open,
    [IO.FileAccess]::Read,
    [IO.FileShare]::ReadWrite)
try {
    $reader = [IO.StreamReader]::new($stream)
    try {
        $logText = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}
finally {
    $stream.Dispose()
}
$lines = $logText -split "`r?`n"
$windows = [Collections.Generic.List[object]]::new()
$current = $null
$latestSessionLine = 0
for ($index = 0; $index -lt $lines.Length; $index++) {
    $line = $lines[$index]
    if ($line.Contains('[StorageNetwork][FrameProfile] enabled.', [StringComparison]::Ordinal)) {
        Complete-Window $windows $current
        $windows.Clear()
        $current = $null
        $latestSessionLine = $index
        continue
    }

    $marker = '[StorageNetwork][FrameProfile] '
    $markerIndex = $line.IndexOf($marker, [StringComparison]::Ordinal)
    if ($markerIndex -lt 0) {
        continue
    }
    $payload = $line.Substring($markerIndex + $marker.Length)
    if ($payload -match '^60(?:\.\d+)?s frames=') {
        Complete-Window $windows $current
        $current = [pscustomobject]@{
            Line = $index + 1
            Timestamp = if ($line -match '^\[([^]]+)\]') { $Matches[1] } else { '' }
            ModP95Ms = $null
            ModP99Ms = $null
            ModMaxMs = $null
            ActivePorts = $null
            WorldSnapshotRebuilds = $null
            LayoutCalls = 0
            Areas = @{}
            ModAllocBytes = $null
            Gen0 = $null
            Algorithms = @{}
        }
        continue
    }

    if ($null -eq $current) {
        continue
    }
    if ($payload.StartsWith('modCpu ', [StringComparison]::Ordinal)) {
        $current.ModP95Ms = Get-Number $payload 'p95=([0-9.]+)ms'
        $current.ModP99Ms = Get-Number $payload 'p99=([0-9.]+)ms'
        $current.ModMaxMs = Get-Number $payload 'max=([0-9.]+)ms'
        if ($payload -match 'modAlloc total=([0-9]+)B') { $current.ModAllocBytes = [long]$Matches[1] }
        if ($payload -match 'gen0=([0-9]+)') { $current.Gen0 = [int]$Matches[1] }
    }
    elseif ($payload.StartsWith('area=', [StringComparison]::Ordinal)) {
        $areaMatch = [regex]::Match($payload, '^area=([^ ]+) calls=([0-9]+).+p95<=([0-9.]+)ms p99<=([0-9.]+)ms max=([0-9.]+)ms')
        if ($areaMatch.Success) {
            $area = [pscustomobject]@{
                Calls = [int]$areaMatch.Groups[2].Value
                P95UpperMs = [double]::Parse($areaMatch.Groups[3].Value, [Globalization.CultureInfo]::InvariantCulture)
                P99UpperMs = [double]::Parse($areaMatch.Groups[4].Value, [Globalization.CultureInfo]::InvariantCulture)
                MaxMs = [double]::Parse($areaMatch.Groups[5].Value, [Globalization.CultureInfo]::InvariantCulture)
            }
            $current.Areas[$areaMatch.Groups[1].Value] = $area
            if ($areaMatch.Groups[1].Value -eq 'Layout') {
                $current.LayoutCalls = $area.Calls
            }
        }
    }
    elseif ($payload.StartsWith('algorithms ', [StringComparison]::Ordinal)) {
        foreach ($metric in [regex]::Matches($payload, '(\w+)=([0-9]+)')) {
            $current.Algorithms[$metric.Groups[1].Value] = [long]$metric.Groups[2].Value
        }
    }
    elseif ($payload.StartsWith('counters ', [StringComparison]::Ordinal)) {
        $current.ActivePorts = [int](Get-Number $payload 'activePorts=([0-9]+)' -default -1)
        $current.WorldSnapshotRebuilds = [int](Get-Number $payload 'worldSnapshotRebuilds=([0-9]+)' -default -1)
    }
}
Complete-Window $windows $current

$eligible = @($windows | Select-Object -Skip $SkipWindows)
$selected = @($eligible | Select-Object -Last $MinimumWindows)
$failures = [Collections.Generic.List[string]]::new()
if ($selected.Count -lt $MinimumWindows) {
    $failures.Add("Expected $MinimumWindows complete windows after skipping $SkipWindows; found $($selected.Count).")
}

foreach ($window in $selected) {
    $label = "line $($window.Line) [$($window.Timestamp)]"
    if ($window.ModP95Ms -ge $MaxModP95Ms) {
        $failures.Add("$label mod p95 $($window.ModP95Ms)ms is not below $MaxModP95Ms ms.")
    }
    if ($window.ModP99Ms -ge $MaxModP99Ms) {
        $failures.Add("$label mod p99 $($window.ModP99Ms)ms is not below $MaxModP99Ms ms.")
    }
    if ($window.ModMaxMs -gt $MaxModSpikeMs) {
        $failures.Add("$label mod max $($window.ModMaxMs)ms exceeds $MaxModSpikeMs ms.")
    }
    if ($ExpectedActivePorts -ge 0 -and $window.ActivePorts -ne $ExpectedActivePorts) {
        $failures.Add("$label activePorts=$($window.ActivePorts), expected $ExpectedActivePorts.")
    }
    if ($MaxLayoutCalls -ge 0 -and $window.LayoutCalls -gt $MaxLayoutCalls) {
        $failures.Add("$label layout calls=$($window.LayoutCalls), maximum $MaxLayoutCalls.")
    }
    if ($MaxWorldSnapshotRebuilds -ge 0 -and
        $window.WorldSnapshotRebuilds -gt $MaxWorldSnapshotRebuilds) {
        $failures.Add("$label world snapshot rebuilds=$($window.WorldSnapshotRebuilds), maximum $MaxWorldSnapshotRebuilds.")
    }

    foreach ($requiredArea in @(
            if ($RequireMainPanel) { 'MainPanel' }
            if ($RequireCategorySummary) { 'CategorySummary' }
        )) {
        if (-not $window.Areas.ContainsKey($requiredArea)) {
            $failures.Add("$label is missing required UI area $requiredArea.")
            continue
        }
        $area = $window.Areas[$requiredArea]
        if ($area.P95UpperMs -gt $MaxUiP95Ms) {
            $failures.Add("$label $requiredArea p95 bucket <=$($area.P95UpperMs)ms exceeds $MaxUiP95Ms ms.")
        }
        if ($area.P99UpperMs -gt $MaxUiP99Ms) {
            $failures.Add("$label $requiredArea p99 bucket <=$($area.P99UpperMs)ms exceeds $MaxUiP99Ms ms.")
        }
    }
}

$sessionErrors = @($lines[$latestSessionLine..($lines.Length - 1)] | Where-Object {
        $_ -match '\[StorageNetwork\].*(Exception|\[ERROR\]|ShadowMismatch)'
    })
if ($sessionErrors.Count -gt 0) {
    $failures.Add("Latest profiler session contains $($sessionErrors.Count) StorageNetwork error/mismatch lines.")
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    [pscustomobject]@{
        SchemaVersion = 1
        AcceptancePassed = $failures.Count -eq 0
        AcceptanceFailures = @($failures)
        SessionErrorCount = $sessionErrors.Count
        LogPath = $resolvedLog
        SkipWindows = $SkipWindows
        MinimumWindows = $MinimumWindows
        RequireMainPanel = [bool]$RequireMainPanel
        RequireCategorySummary = [bool]$RequireCategorySummary
        Windows = $selected
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputJson -Encoding utf8
}

$selected | Select-Object Timestamp,ModP95Ms,ModP99Ms,ModMaxMs,ActivePorts,WorldSnapshotRebuilds,LayoutCalls | Format-Table -AutoSize
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure -ErrorAction Continue
    }
    throw "$($failures.Count) frame-profile acceptance failures."
}

Write-Output "Frame-profile acceptance passed for $($selected.Count) windows."
