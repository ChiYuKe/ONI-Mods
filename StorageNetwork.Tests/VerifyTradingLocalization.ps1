param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = "$env:USERPROFILE\Documents\Klei\OxygenNotIncluded\mods\Dev\StorageNetworkTrading\StorageNetworkTrading.dll"
)
$ErrorActionPreference = 'Stop'
foreach ($dependency in @('UnityEngine.CoreModule.dll', 'Assembly-CSharp-firstpass.dll', 'Assembly-CSharp.dll', 'Newtonsoft.Json.dll')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $GameManagedDir $dependency))
}
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ModAssembly).Path)
$catalog = $assembly.GetType('StorageNetwork.Trading.TradingTextCatalog', $true)
$entries = $catalog.GetField('Entries', [Reflection.BindingFlags]'Static,NonPublic').GetValue($null)
$poPath = Join-Path (Split-Path -Parent $ModAssembly) 'translations\en.po'
$translations = [Localization]::LoadStringsFile($poPath, $false)
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$textType = $assembly.GetType('StorageNetwork.Trading.TradingText', $true)
$staticFlags = [Reflection.BindingFlags]'Static,NonPublic'
$configure = $textType.GetMethod('Configure', $staticFlags)
$ref = $textType.GetMethod('Ref', $staticFlags)
$render = $textType.GetMethod('Render', $staticFlags)
$lookup = [Func[string,string]] ({ param($key) return $translations[$key] }.GetNewClosure())
$configure.Invoke($null, @('en', $lookup)) | Out-Null
foreach ($entry in $entries) {
    $key = $entry.GetType().GetField('Key', $flags).GetValue($entry)
    $source = $entry.GetType().GetField('Source', $flags).GetValue($entry)
    if ($entry.GetType().GetField('English', $flags)) { throw 'English text is still duplicated in the compiled catalog.' }
    if (-not $translations.ContainsKey($key) -or [string]::IsNullOrEmpty($translations[$key])) {
        throw "Native game PO loader did not return the expected English translation for $key."
    }
    $count = [regex]::Matches($source, '\{\d+\}').Count
    $arguments = [string[]]@(for ($index = 0; $index -lt $count; $index++) { "VALUE$index" })
    $invokeArguments = [object[]]::new(2)
    $invokeArguments[0] = $key
    $invokeArguments[1] = $arguments
    $reference = $ref.Invoke($null, $invokeArguments)
    $actual = $render.Invoke($null, @($reference))
    $expected = if ($count -eq 0) { $translations[$key] } else { [string]::Format($translations[$key], [object[]]$arguments) }
    if ($actual -ne $expected) { throw "Compiled key-based rendering did not use the deployed PO entry: $key" }
}
Write-Output "Native game PO loader verified $($entries.Count) deployed trading and delivery-name translations."
