[CmdletBinding()]
param(
    [string]$GameDir,
    [string]$DevModDir
)

$ErrorActionPreference = 'Stop'

function Get-OniManagedDirectory {
    param([string]$Root)

    if ([string]::IsNullOrWhiteSpace($Root)) {
        return $null
    }

    $normalizedRoot = $Root.Trim().TrimEnd('\', '/')
    $managed = Join-Path $normalizedRoot 'OxygenNotIncluded_Data\Managed'
    if (Test-Path -LiteralPath (Join-Path $managed 'Assembly-CSharp.dll')) {
        return [PSCustomObject]@{
            GameDir = $normalizedRoot
            ManagedDir = $managed
            AssemblyTimestamp = (Get-Item -LiteralPath (Join-Path $managed 'Assembly-CSharp.dll')).LastWriteTimeUtc
        }
    }

    return $null
}

function Add-Candidate {
    param(
        [System.Collections.Generic.List[string]]$Candidates,
        [string]$Path
    )

    if (-not [string]::IsNullOrWhiteSpace($Path) -and -not $Candidates.Contains($Path)) {
        $Candidates.Add($Path)
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet não foi encontrado no PATH.'
}

$explicitGameDir = if (-not [string]::IsNullOrWhiteSpace($GameDir)) { $GameDir } else { $env:ONI_GAME_DIR }
$selectedGame = Get-OniManagedDirectory $explicitGameDir

if ($null -eq $selectedGame) {
    $candidates = [System.Collections.Generic.List[string]]::new()

    foreach ($registryPath in @(
        'HKCU:\Software\Valve\Steam',
        'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
        'HKLM:\SOFTWARE\Valve\Steam'
    )) {
        if (Test-Path $registryPath) {
            $steam = Get-ItemProperty -Path $registryPath -ErrorAction SilentlyContinue
            if (-not [string]::IsNullOrWhiteSpace($steam.SteamPath)) {
                Add-Candidate $candidates (Join-Path $steam.SteamPath 'steamapps\common\OxygenNotIncluded')
            }
            if (-not [string]::IsNullOrWhiteSpace($steam.InstallPath)) {
                Add-Candidate $candidates (Join-Path $steam.InstallPath 'steamapps\common\OxygenNotIncluded')
            }
        }
    }

    foreach ($drive in Get-PSDrive -PSProvider FileSystem) {
        Add-Candidate $candidates (Join-Path $drive.Root 'Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded')
        Add-Candidate $candidates (Join-Path $drive.Root 'SteamLibrary\steamapps\common\OxygenNotIncluded')
    }

    $selectedGame = $candidates |
        ForEach-Object { Get-OniManagedDirectory $_ } |
        Where-Object { $null -ne $_ } |
        Sort-Object AssemblyTimestamp -Descending |
        Select-Object -First 1
}

if ($null -eq $selectedGame) {
    throw 'Oxygen Not Included não foi encontrado. Informe -GameDir ou defina ONI_GAME_DIR.'
}

if ([string]::IsNullOrWhiteSpace($DevModDir)) {
    $documents = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
    if ([string]::IsNullOrWhiteSpace($documents)) {
        $documents = Join-Path $env:USERPROFILE 'Documents'
    }

    $DevModDir = Join-Path $documents 'Klei\OxygenNotIncluded\mods\Dev\StorageNetwork'
}

$projectDir = $PSScriptRoot
$projectFile = Join-Path $projectDir 'StorageNetwork.csproj'
$outputDir = [System.IO.Path]::GetFullPath($DevModDir)

Write-Host "Jogo: $($selectedGame.GameDir)"
Write-Host "DLLs: $($selectedGame.ManagedDir)"
Write-Host "Mod Dev: $outputDir"

$buildArguments = @(
    'build',
    $projectFile,
    '-c',
    'Debug',
    "-p:GameManagedDir=$($selectedGame.ManagedDir)",
    "-p:OutputPath=$outputDir\"
)

& dotnet @buildArguments
if ($LASTEXITCODE -ne 0) {
    throw "Build do StorageNetwork falhou com código $LASTEXITCODE."
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

$animSource = Join-Path $projectDir 'anim'
$animDestination = Join-Path $outputDir 'anim'
if (-not (Test-Path -LiteralPath $animSource)) {
    throw "Assets de animação não encontrados em: $animSource"
}

New-Item -ItemType Directory -Path $animDestination -Force | Out-Null
Copy-Item -Path (Join-Path $animSource '*') -Destination $animDestination -Recurse -Force

$logicDiyAnim = Join-Path $animDestination 'assets\storagenetwork_logic_diy\storagenetwork_logic_diy_build.bytes'
if (-not (Test-Path -LiteralPath $logicDiyAnim)) {
    throw "A animação do StorageNetworkLogicDiy não foi instalada em: $logicDiyAnim"
}

$modYaml = Join-Path $outputDir 'mod.yaml'
if (-not (Test-Path -LiteralPath $modYaml)) {
    Set-Content -LiteralPath $modYaml -Encoding UTF8 -Value 'title: "StorageNetwork (Dev)"'
}

$modInfoYaml = Join-Path $outputDir 'mod_info.yaml'
if (-not (Test-Path -LiteralPath $modInfoYaml)) {
    Set-Content -LiteralPath $modInfoYaml -Encoding UTF8 -Value @(
        'supportedContent: ALL',
        'minimumSupportedBuild: 525812',
        'version: 1.2.0',
        'APIVersion: 2'
    )
}

Write-Host "StorageNetwork (Dev) instalado com sucesso em: $outputDir"
