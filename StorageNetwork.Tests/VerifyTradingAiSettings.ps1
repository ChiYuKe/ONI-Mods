param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = 'C:\Users\Administrator\Documents\Klei\OxygenNotIncluded\mods\Dev\StorageNetwork\StorageNetwork.dll'
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
$store = $storeType.GetConstructors($flags)[0].Invoke([object[]]@([string]$testPath))
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
    [IO.File]::WriteAllText($testPath, 'invalid JSON')
    if ($load.Invoke($store, $loadedArgs)) { throw 'Corrupted configuration was silently accepted.' }
    Write-Output 'AI settings validation, atomic replacement and Windows credential encryption checks passed.'
}
finally { if ([IO.File]::Exists($testPath)) { [IO.File]::Delete($testPath) } }
