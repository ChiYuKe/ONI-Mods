param(
    [Parameter(Mandatory = $true)]
    [string] $SourceSave,
    [string] $DestinationDirectory,
    [string] $DestinationName = 'StorageNetwork_Benchmark.sav',
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Core') {
    $pwsh = Get-Command pwsh -ErrorAction Stop
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $PSCommandPath,
        '-SourceSave', $SourceSave,
        '-DestinationName', $DestinationName
    )
    if (-not [string]::IsNullOrWhiteSpace($DestinationDirectory)) {
        $arguments += @('-DestinationDirectory', $DestinationDirectory)
    }
    if ($Force) {
        $arguments += '-Force'
    }
    & $pwsh.Source @arguments
    exit $LASTEXITCODE
}

if (Get-Process OxygenNotIncluded -ErrorAction SilentlyContinue) {
    throw 'Close Oxygen Not Included before preparing a benchmark fixture.'
}

$source = Get-Item -LiteralPath $SourceSave -ErrorAction Stop
if ($source.Extension -ne '.sav') {
    throw "Benchmark source must be an ONI .sav file: $($source.FullName)"
}

if ([string]::IsNullOrWhiteSpace($DestinationDirectory)) {
    $saveFilesRoot = Split-Path -Parent $source.DirectoryName
    $DestinationDirectory = Join-Path $saveFilesRoot 'StorageNetwork_Benchmark'
}

$destinationRoot = [IO.Path]::GetFullPath($DestinationDirectory)
$destinationSave = Join-Path $destinationRoot $DestinationName
if ([string]::Equals($source.FullName, $destinationSave, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Benchmark destination must differ from the source save.'
}

if (-not (Test-Path -LiteralPath $destinationRoot -PathType Container)) {
    [void](New-Item -ItemType Directory -Path $destinationRoot)
}

$copies = [Collections.Generic.List[object]]::new()
$sourceBaseName = [IO.Path]::GetFileNameWithoutExtension($source.Name)
$destinationBaseName = [IO.Path]::GetFileNameWithoutExtension($DestinationName)
foreach ($candidate in Get-ChildItem -LiteralPath $source.DirectoryName -File) {
    $destinationFileName = $null
    if ([string]::Equals($candidate.Name, $source.Name, [StringComparison]::OrdinalIgnoreCase)) {
        $destinationFileName = $DestinationName
    }
    elseif ($candidate.Name.StartsWith($source.Name + '.', [StringComparison]::OrdinalIgnoreCase)) {
        $destinationFileName = $DestinationName + $candidate.Name.Substring($source.Name.Length)
    }
    elseif ([string]::Equals(
            $candidate.Name,
            $sourceBaseName + '.png',
            [StringComparison]::OrdinalIgnoreCase)) {
        $destinationFileName = $destinationBaseName + '.png'
    }

    if ([string]::IsNullOrEmpty($destinationFileName)) {
        continue
    }

    $destination = Join-Path $destinationRoot $destinationFileName
    if ((Test-Path -LiteralPath $destination) -and -not $Force) {
        throw "Benchmark fixture already exists; pass -Force to replace it: $destination"
    }

    Copy-Item -LiteralPath $candidate.FullName -Destination $destination -Force:$Force
    $copied = Get-Item -LiteralPath $destination
    $copies.Add([pscustomobject]@{
        Source = $candidate.FullName
        Destination = $copied.FullName
        Length = $copied.Length
        Sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $copied.FullName).Hash
    })
}

$sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $source.FullName).Hash
$manifest = [pscustomobject]@{
    PreparedAtUtc = [DateTime]::UtcNow.ToString('O')
    SourceSave = $source.FullName
    SourceLength = $source.Length
    SourceLastWriteTimeUtc = $source.LastWriteTimeUtc.ToString('O')
    SourceSha256 = $sourceHash
    DestinationSave = $destinationSave
    Files = $copies
}
$manifestPath = Join-Path $destinationRoot ($destinationBaseName + '.benchmark.json')
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8

$manifest
