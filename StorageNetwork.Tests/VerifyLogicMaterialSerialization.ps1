param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = (Join-Path $PSScriptRoot '..\StorageNetwork\bin\Release\StorageNetwork.dll'),
    [string] $ExpectedJson = (Join-Path $PSScriptRoot 'LogicMaterialSerializationBaseline.json'),
    [switch] $ExportBaseline,
    [string] $OutputJson
)
$ErrorActionPreference = 'Stop'
foreach ($dependency in @('UnityEngine.CoreModule.dll', 'Assembly-CSharp-firstpass.dll', 'Assembly-CSharp.dll', 'Newtonsoft.Json.dll')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $GameManagedDir $dependency))
}
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ModAssembly).Path)
$nodeType = $assembly.GetType('StorageNetwork.Components.StorageNetworkLogicDiy+RuntimeBlueprintNode', $true)
$node = [Activator]::CreateInstance($nodeType)
$node.Id = 'material-test'
$node.Module = 'material_condition'
$node.SelectedMaterialKey = 'Water'
$node.Value = 50.0
$serialized = [Newtonsoft.Json.JsonConvert]::SerializeObject($node)
if ($ExportBaseline) {
    if (-not $OutputJson) { throw 'Baseline export requires OutputJson.' }
    $serialized | Set-Content -LiteralPath $OutputJson -Encoding utf8
    exit 0
}
$expected = (Get-Content -Raw -LiteralPath $ExpectedJson).Trim()
if ($serialized -ne $expected) { throw 'Runtime blueprint node serialization changed from the pre-optimization assembly.' }
$cache = $nodeType.GetProperty('SelectedMaterialTag', [Reflection.BindingFlags]'Instance,NonPublic')
if ($null -eq $cache) { throw 'Compiled material cache is absent.' }
$tag = $cache.GetValue($node)
if (-not $tag.IsValid) { throw 'Selected material failed to compile.' }
$node.SelectedMaterialKey = 'Water'
if ($cache.GetValue($node).GetHashCode() -ne $tag.GetHashCode()) { throw 'Unchanged selection changed its Tag.' }
$node.SelectedMaterialKey = ''
if ($cache.GetValue($node).IsValid) { throw 'Cleared selection did not invalidate the cache.' }
$node.SelectedMaterialKey = 'Dirt'
if (-not $cache.GetValue($node).IsValid) { throw 'Changed selection failed to recompile.' }
Write-Output 'Logic material cache and pre-optimization JSON serialization checks passed against the actual mod assembly.'
