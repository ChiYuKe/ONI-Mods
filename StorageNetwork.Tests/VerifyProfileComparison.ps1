$ErrorActionPreference = 'Stop'
$testDirectory = Join-Path $PSScriptRoot 'bin/profile-parser-tests'
[void](New-Item -ItemType Directory -Force -Path $testDirectory)
$baseline = @()
$candidate = @()
for ($run = 0; $run -lt 3; $run++) {
    foreach ($variant in @('baseline', 'candidate')) {
        $cpu = if ($variant -eq 'baseline') { '1.000' } else { '0.800' }
        $lines = [Collections.Generic.List[string]]::new()
        $lines.Add('[StorageNetwork][FrameProfile] enabled.')
        for ($window = 0; $window -lt 7; $window++) {
            $lines.Add('[StorageNetwork][FrameProfile] 60.0s frames=3600 fps=60.0')
            $lines.Add("[StorageNetwork][FrameProfile] modCpu p95=${cpu}ms p99=1.200ms max=2.000ms modAlloc total=1000B p95=10B p99=20B max=50B gen0=1 managedDelta=0B")
            $lines.Add('[StorageNetwork][FrameProfile] counters activePorts=28 worldSnapshotRebuilds=0')
            $lines.Add('[StorageNetwork][FrameProfile] algorithms scannedItems=20 updatedTags=8 sourceBucketSorts=0 farmingSnapshotEntries=1000 textMeshRebuilds=0')
        }
        $log = Join-Path $testDirectory "$variant-$run.synthetic.log"
        $json = Join-Path $testDirectory "$variant-$run.synthetic.json"
        $lines | Set-Content -LiteralPath $log -Encoding utf8
        & (Join-Path $PSScriptRoot 'AnalyzeFrameProfile.ps1') -LogPath $log -ExpectedActivePorts 28 -OutputJson $json | Out-Null
        $report = Get-Content -Raw -LiteralPath $json | ConvertFrom-Json
        if ($report.Windows.Count -ne 5 -or $report.Windows[0].ModAllocBytes -ne 1000 -or
            $report.Windows[0].Gen0 -ne 1 -or $report.Windows[0].Algorithms.updatedTags -ne 8) {
            throw 'Profile export lost required metrics.'
        }
        if ($variant -eq 'baseline') { $baseline += $json } else { $candidate += $json }
    }
}
$compare = Join-Path $PSScriptRoot 'CompareFrameProfiles.ps1'
& $compare -BaselineReports $baseline -CandidateReports $candidate -PrimaryScenario | Out-Null
$rejected = $false
try { & $compare -BaselineReports $candidate -CandidateReports $baseline -PrimaryScenario | Out-Null }
catch { $rejected = $_.Exception.Message -match 'Relative CPU performance acceptance failed' }
if (-not $rejected) { throw 'Performance regression was not rejected.' }
$rejected = $false
try { & $compare -BaselineReports @($baseline[0], $baseline[0], $baseline[0]) -CandidateReports $candidate | Out-Null }
catch { $rejected = $_.Exception.Message -match 'independent run' }
if (-not $rejected) { throw 'Duplicate reports were not rejected.' }
$highLog = Join-Path $testDirectory 'over-limit.synthetic.log'
$highJson = Join-Path $testDirectory 'over-limit.synthetic.json'
(Get-Content -Raw -LiteralPath (Join-Path $testDirectory 'baseline-0.synthetic.log')).Replace('p95=1.000ms', 'p95=5.000ms') |
    Set-Content -LiteralPath $highLog -Encoding utf8
$rejected = $false
try { & (Join-Path $PSScriptRoot 'AnalyzeFrameProfile.ps1') -LogPath $highLog -OutputJson $highJson 2>$null | Out-Null }
catch { $rejected = $_.Exception.Message -match 'frame-profile acceptance failures' }
$highReport = Get-Content -Raw -LiteralPath $highJson | ConvertFrom-Json
if (-not $rejected -or $highReport.AcceptancePassed -or $highReport.Windows.Count -ne 5) {
    throw 'Over-limit measurements must be exported while retaining a failed acceptance result.'
}
Write-Output 'Profile parser/comparison tests passed using synthetic data; this is not a game benchmark.'
