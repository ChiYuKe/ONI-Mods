param(
    [Parameter(Mandatory)][string[]] $BaselineReports,
    [Parameter(Mandatory)][string[]] $CandidateReports,
    [switch] $PrimaryScenario,
    [string] $OutputJson
)
$ErrorActionPreference = 'Stop'

function Get-Median([double[]] $values) {
    if ($values.Count -eq 0) { throw 'No samples.' }
    foreach ($value in $values) {
        if ([double]::IsNaN($value) -or [double]::IsInfinity($value) -or $value -lt 0) { throw 'Invalid sample.' }
    }
    $sorted = @($values | Sort-Object)
    $middle = [int][Math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2) { return $sorted[$middle] }
    return ($sorted[$middle - 1] + $sorted[$middle]) / 2
}

function Read-Reports([string[]] $paths) {
    if ($paths.Count -lt 3) { throw 'At least three independent runs are required per version.' }
    if (@($paths | ForEach-Object { (Resolve-Path -LiteralPath $_).Path } | Sort-Object -Unique).Count -ne $paths.Count) {
        throw 'A report cannot be reused as another independent run.'
    }
    foreach ($path in $paths) {
        $report = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
        if ($report.SchemaVersion -ne 1 -or $report.SkipWindows -lt 2 -or @($report.Windows).Count -lt 5) {
            throw "Insufficient warmup/windows or incompatible report: $path"
        }
        if ($null -ne $report.SessionErrorCount -and $report.SessionErrorCount -gt 0) {
            throw "Profiler session contains mod errors/shadow mismatches: $path"
        }
        foreach ($window in $report.Windows) {
            if ($null -eq $window.ModP95Ms -or $null -eq $window.ModP99Ms -or $null -eq $window.ActivePorts -or $window.ActivePorts -lt 0) {
                throw "Missing CPU/workload metrics: $path"
            }
        }
        $report
    }
}

$baseline = @(Read-Reports $BaselineReports)
$candidate = @(Read-Reports $CandidateReports)
if ($baseline.Count -ne $candidate.Count) { throw 'Use equal numbers of before/after runs.' }
$reference = $baseline[0]
$ports = $reference.Windows[0].ActivePorts
foreach ($run in @($baseline) + @($candidate)) {
    if ($run.RequireMainPanel -ne $reference.RequireMainPanel -or $run.RequireCategorySummary -ne $reference.RequireCategorySummary) {
        throw 'Panel scenarios differ between reports.'
    }
    foreach ($window in $run.Windows) {
        if ($window.ActivePorts -ne $ports) { throw 'Active port counts differ; repeat with a stable workload.' }
    }
}

$rows = foreach ($metric in @('ModP95Ms', 'ModP99Ms', 'ModAllocBytes', 'Gen0')) {
    $missing = @(@($baseline) + @($candidate) | ForEach-Object { $_.Windows } | Where-Object { $null -eq $_.$metric }).Count -gt 0
    if ($missing) {
        [pscustomobject]@{ Metric = $metric; Baseline = $null; Candidate = $null; ChangePercent = $null; Accepted = $null }
        continue
    }
    $before = Get-Median @($baseline | ForEach-Object { Get-Median @($_.Windows | ForEach-Object { [double]$_.$metric }) })
    $after = Get-Median @($candidate | ForEach-Object { Get-Median @($_.Windows | ForEach-Object { [double]$_.$metric }) })
    $change = if ($before -gt 0) { ($after - $before) / $before * 100 } else { $null }
    $limit = if ($metric -eq 'ModP95Ms' -and $PrimaryScenario) { 0.85 } else { 1.05 }
    $accepted = if ($metric -in @('ModP95Ms', 'ModP99Ms')) {
        # Rounded-to-zero timings cannot establish the primary improvement goal.
        if ($PrimaryScenario -and $metric -eq 'ModP95Ms' -and $before -eq 0) { $false }
        else { $after -le $before * $limit }
    } else { $null }
    [pscustomobject]@{ Metric = $metric; Baseline = $before; Candidate = $after; ChangePercent = $change; Accepted = $accepted }
}
$rows | Format-Table -AutoSize
if ($OutputJson) {
    [pscustomobject]@{ PrimaryScenario = [bool]$PrimaryScenario; Runs = $baseline.Count; ActivePorts = $ports; Metrics = @($rows) } |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputJson -Encoding utf8
}
if (@($rows | Where-Object { $null -ne $_.Accepted -and -not $_.Accepted }).Count -gt 0) {
    throw 'Relative CPU performance acceptance failed. Do not claim the target was achieved.'
}
Write-Output 'Relative CPU acceptance passed; verify fixture, speed, camera, mods and shadow mode against the run manifest.'
