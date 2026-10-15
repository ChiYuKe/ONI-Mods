param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed'
)

$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Core') {
    $pwsh = Get-Command pwsh -ErrorAction Stop
    & $pwsh.Source -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
        -GameManagedDir $GameManagedDir
    exit $LASTEXITCODE
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$env:STORAGE_NETWORK_REPOSITORY_ROOT = $repositoryRoot

Push-Location $repositoryRoot
try {
    & dotnet build 'StorageNetworkTrading\StorageNetworkTrading.csproj' -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "StorageNetwork Release build failed with exit code $LASTEXITCODE."
    }

    & dotnet build 'StorageNetwork.Tests\StorageNetwork.Tests.csproj' -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "StorageNetwork deterministic test build failed with exit code $LASTEXITCODE."
    }

    & dotnet 'StorageNetwork.Tests\bin\Release\net8.0\StorageNetwork.Tests.dll'
    if ($LASTEXITCODE -ne 0) {
        throw "StorageNetwork deterministic tests failed with exit code $LASTEXITCODE."
    }

    & (Join-Path $PSScriptRoot 'VerifyProfileComparison.ps1')
    & (Join-Path $PSScriptRoot 'VerifyLogicMaterialSerialization.ps1') -GameManagedDir $GameManagedDir
    & (Join-Path $PSScriptRoot 'VerifyTradingSerialization.ps1') -GameManagedDir $GameManagedDir
    & (Join-Path $PSScriptRoot 'VerifyTradingModSplit.ps1') -GameManagedDir $GameManagedDir
    $tradingAssembly = Join-Path $repositoryRoot 'StorageNetworkTrading\bin\Release\StorageNetworkTrading.dll'
    & (Join-Path $PSScriptRoot 'VerifyTradingLocalization.ps1') -GameManagedDir $GameManagedDir -ModAssembly $tradingAssembly
    & (Join-Path $PSScriptRoot 'VerifyTradingGroupChat.ps1') -GameManagedDir $GameManagedDir -ModAssembly $tradingAssembly
    & (Join-Path $PSScriptRoot 'VerifyTradingAiSettings.ps1') -GameManagedDir $GameManagedDir -ModAssembly $tradingAssembly
    & (Join-Path $PSScriptRoot 'VerifyHarmonyTargets.ps1') -GameManagedDir $GameManagedDir -ModAssembly $tradingAssembly

    & (Join-Path $PSScriptRoot 'VerifyStaticPerformance.ps1') `
        -RepositoryRoot $repositoryRoot
    if ($LASTEXITCODE -ne 0) {
        throw "StorageNetwork static performance verification failed with exit code $LASTEXITCODE."
    }

    & (Join-Path $PSScriptRoot 'VerifyPublicApi.ps1') `
        -GameManagedDir $GameManagedDir `
        -ModAssembly (Join-Path $repositoryRoot 'StorageNetwork\bin\Release\StorageNetwork.dll') `
        -Baseline (Join-Path $PSScriptRoot 'PublicApiBaseline.txt')
    if ($LASTEXITCODE -ne 0) {
        throw "StorageNetwork public API verification failed with exit code $LASTEXITCODE."
    }

    & (Join-Path $PSScriptRoot 'VerifyHarmonyTargets.ps1') `
        -GameManagedDir $GameManagedDir `
        -ModAssembly (Join-Path $repositoryRoot 'StorageNetwork\bin\Release\StorageNetwork.dll')
    if ($LASTEXITCODE -ne 0) {
        throw "StorageNetwork Harmony verification failed with exit code $LASTEXITCODE."
    }

    & git -c core.safecrlf=false diff --check -- `
        'StorageNetwork' `
        'StorageNetworkTrading' `
        'StorageNetwork.Tests' `
        '.gitignore' `
        'NOIMods.sln'
    if ($LASTEXITCODE -ne 0) {
        throw "StorageNetwork whitespace verification failed with exit code $LASTEXITCODE."
    }

    # `git diff --check` intentionally ignores untracked files. Several core files
    # and this verification project are new in the optimization branch, so inspect
    # every in-scope source artifact directly until they become tracked.
    $textExtensions = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase)
    # Markdown permits two trailing spaces as a hard line break, so it remains
    # covered by git diff for changed files rather than this raw source scanner.
    foreach ($extension in @('.cs', '.ps1', '.csproj', '.txt')) {
        [void]$textExtensions.Add($extension)
    }
    $sourceWhitespaceFailures = [Collections.Generic.List[string]]::new()
    foreach ($sourceFile in Get-ChildItem -LiteralPath @(
            (Join-Path $repositoryRoot 'StorageNetwork'),
            (Join-Path $repositoryRoot 'StorageNetwork.Tests')) -File -Recurse) {
        if (-not $textExtensions.Contains($sourceFile.Extension) -or
            $sourceFile.FullName -match '[\\/](bin|obj)[\\/]') {
            continue
        }

        $lineNumber = 0
        foreach ($line in [IO.File]::ReadLines($sourceFile.FullName)) {
            $lineNumber++
            if ($line -match '[ \t]+$' -or $line -match '^(<<<<<<<|=======|>>>>>>>)') {
                $relative = [IO.Path]::GetRelativePath($repositoryRoot, $sourceFile.FullName)
                $sourceWhitespaceFailures.Add("${relative}:$lineNumber")
            }
        }
    }
    if ($sourceWhitespaceFailures.Count -gt 0) {
        throw "Source whitespace/conflict-marker verification failed: $($sourceWhitespaceFailures -join ', ')"
    }

    Write-Output 'StorageNetwork unified verification passed: Release, deterministic contracts, static performance gates, public API, Harmony targets, whitespace.'
}
finally {
    Pop-Location
}
