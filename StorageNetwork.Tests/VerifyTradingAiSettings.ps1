param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = 'C:\Users\Administrator\Documents\Klei\OxygenNotIncluded\mods\Dev\StorageNetworkTrading\StorageNetworkTrading.dll'
)
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Join-Path $GameManagedDir 'Newtonsoft.Json.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ModAssembly).Path)
$settingsType = $assembly.GetType('StorageNetwork.Trading.TradingAiSettings', $true)
$storeType = $assembly.GetType('StorageNetwork.Trading.TradingAiSettingsStore', $true)
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$directory = Join-Path $PSScriptRoot 'bin'
[void][IO.Directory]::CreateDirectory($directory)
$testPath = Join-Path $directory ('ai-config-check-' + [Guid]::NewGuid().ToString('N') + '.json')
$constructor = $storeType.GetConstructors($flags) | Where-Object { $_.GetParameters().Count -eq 1 }
$store = $constructor.Invoke([object[]]@([string]$testPath))
$validate = $settingsType.GetMethod('ValidationKey', $flags)
$canChat = $settingsType.GetMethod('CanChat', $flags)
$save = $storeType.GetMethod('Save', $flags)
$load = $storeType.GetMethod('Load', $flags)
$settings = [Activator]::CreateInstance($settingsType)
$settings.Provider = 'Local test provider'
$settings.Model = 'test-model'
$settings.Endpoint = 'http://localhost:1234/v1'
# Synthetic test credential, not a real API key.
$settings.ApiKey = 'synthetic-key-only-for-tests-1234567890'
try {
    if ($null -ne $validate.Invoke($settings, @())) { throw 'Local HTTP endpoint was rejected.' }
    $settings.ApiKey = ''
    if (-not $canChat.Invoke($settings, @())) { throw 'Local AI chat must permit an empty API Key.' }
    $settings.Endpoint = 'https://example.invalid/v1'
    if ($canChat.Invoke($settings, @())) { throw 'Remote AI chat must require a configured API Key.' }
    $settings.ApiKey = 'synthetic-key-only-for-tests-1234567890'
    $settings.ReplyProbability = 30
    $settings.Endpoint = 'file:///tmp/api'
    if ($validate.Invoke($settings, @()) -ne 'AI_INVALID_URL') { throw 'Non-HTTP endpoint was accepted.' }
    $settings.Endpoint = 'https://example.invalid/v1?key=secret'
    if ($validate.Invoke($settings, @()) -ne 'AI_INVALID_URL') { throw 'Credentials in query parameters were accepted.' }
    $settings.Endpoint = 'https://example.invalid/v1'
    $settings.Model = ''
    if ($validate.Invoke($settings, @()) -ne 'AI_REQUIRED') { throw 'Empty model was accepted.' }
    $settings.Model = 'test-model'
    $save.Invoke($store, @($settings)) | Out-Null
    $json = [IO.File]::ReadAllText($testPath)
    if ($json.Contains($settings.ApiKey) -or $json.Contains('"ApiKey"')) { throw 'API key leaked into plaintext configuration.' }
    $loadedArgs = @($null)
    if (-not $load.Invoke($store, $loadedArgs)) { throw 'Saved AI configuration failed to load.' }
    if ($loadedArgs[0].ApiKey -ne $settings.ApiKey -or $loadedArgs[0].Model -ne 'test-model' -or $loadedArgs[0].ReplyProbability -ne 30) { throw 'Encrypted credentials, model or reply probability failed round trip.' }
    $settings.ApiKey = ''
    $settings.Model = 'second-model'
    $settings.ReplyProbability = 75
    $save.Invoke($store, @($settings)) | Out-Null
    if (-not $load.Invoke($store, $loadedArgs) -or $loadedArgs[0].ApiKey -ne '' -or $loadedArgs[0].Model -ne 'second-model' -or $loadedArgs[0].ReplyProbability -ne 75) {
        throw 'Replacing settings or clearing credentials failed.'
    }
    $migrationPath = $testPath + '.migrated'
    try {
        $settings.ApiKey = 'synthetic-key-only-for-tests-1234567890'
        $save.Invoke($store, @($settings)) | Out-Null
        $migrationConstructor = $storeType.GetConstructors($flags) | Where-Object { $_.GetParameters().Count -eq 2 }
        $migrationStore = $migrationConstructor.Invoke([object[]]@([string]$migrationPath, [string]$testPath))
        if (-not $load.Invoke($migrationStore, $loadedArgs) -or $loadedArgs[0].ApiKey -ne $settings.ApiKey -or -not [IO.File]::Exists($migrationPath)) {
            throw 'Legacy configuration migration failed to preserve settings and credential.'
        }
        if ([IO.File]::ReadAllText($migrationPath).Contains($settings.ApiKey)) { throw 'Migration wrote a plaintext credential.' }
        $settings.Model = 'new-location-wins'
        $save.Invoke($migrationStore, @($settings)) | Out-Null
        if (-not $load.Invoke($migrationStore, $loadedArgs) -or $loadedArgs[0].Model -ne 'new-location-wins') {
            throw 'Existing new configuration was overwritten by legacy data.'
        }
        [IO.File]::WriteAllText($migrationPath, 'invalid JSON')
        if ($load.Invoke($migrationStore, $loadedArgs)) { throw 'Corrupted new configuration was silently replaced with legacy data.' }
    }
    finally { if ([IO.File]::Exists($migrationPath)) { [IO.File]::Delete($migrationPath) } }
    $savePath = $testPath + '.sav'
    $otherSave = $testPath + '.other.sav'
    $sidecar = $savePath + '.StorageNetworkTradingAi.json'
    try {
        [IO.File]::WriteAllText($savePath, 'synthetic save placeholder')
        [IO.File]::WriteAllText($otherSave, 'synthetic save placeholder')
        $savePatch = $assembly.GetType('StorageNetwork.Trading.TradingAiSettingsSavePatch', $true)
        $postfix = $savePatch.GetMethod('Postfix', [Reflection.BindingFlags]'Static,NonPublic')
        $postfix.Invoke($null, [object[]]@([string]$savePath, [string]$savePath, $settings)) | Out-Null
        if (-not [IO.File]::Exists($sidecar)) { throw 'Save completion did not create an adjacent AI configuration.' }
        $sidecarStore = $constructor.Invoke([object[]]@([string]$sidecar))
        if (-not $load.Invoke($sidecarStore, $loadedArgs) -or $loadedArgs[0].ApiKey -ne $settings.ApiKey) {
            throw 'Adjacent save configuration lost its credential.'
        }
        $postfix.Invoke($null, [object[]]@([string]$otherSave, [string]$savePath, $settings)) | Out-Null
        if ([IO.File]::Exists($otherSave + '.StorageNetworkTradingAi.json')) {
            throw 'Unsuccessful save created an AI configuration at the requested path.'
        }
    }
    finally {
        foreach ($file in @($savePath, $otherSave, $sidecar)) { if ([IO.File]::Exists($file)) { [IO.File]::Delete($file) } }
    }
    [IO.File]::WriteAllText($testPath, 'invalid JSON')
    if ($load.Invoke($store, $loadedArgs)) { throw 'Corrupted configuration was silently accepted.' }
    Write-Output 'AI settings validation, atomic replacement, migration, save sidecars and Windows credential encryption checks passed.'
}
finally { if ([IO.File]::Exists($testPath)) { [IO.File]::Delete($testPath) } }
