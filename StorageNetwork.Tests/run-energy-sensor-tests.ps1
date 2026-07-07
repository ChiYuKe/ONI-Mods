$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $repositoryRoot '.tools\tests'
$outputFile = Join-Path $outputDirectory 'StorageNetworkEnergySensorLogicTests.exe'
$logicFile = Join-Path $repositoryRoot 'StorageNetwork\Buildings\EnergySensor\Components\StorageNetworkEnergySensorLogic.cs'
$testFile = Join-Path $PSScriptRoot 'StorageNetworkEnergySensorLogicTests.cs'

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
& $compiler /nologo /target:exe "/out:$outputFile" $logicFile $testFile
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $outputFile
exit $LASTEXITCODE
