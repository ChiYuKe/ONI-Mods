param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = (Join-Path $PSScriptRoot '..\StorageNetwork\bin\Release\StorageNetwork.dll')
)
$ErrorActionPreference = 'Stop'
foreach ($dependency in @('UnityEngine.CoreModule.dll', 'Assembly-CSharp-firstpass.dll', 'Assembly-CSharp.dll', 'Newtonsoft.Json.dll')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $GameManagedDir $dependency))
}
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ModAssembly).Path)
$marketType = $assembly.GetType('StorageNetwork.Trading.TradingMarket', $true)
$supplierType = $assembly.GetType('StorageNetwork.Trading.TradingSupplier', $true)
$market = [Activator]::CreateInstance($marketType)
$market.Balance = [decimal]1234.567
$market.NextId = 9
$supplier = [Activator]::CreateInstance($supplierType)
$supplier.Id = 8
$supplier.Specialty = 'Metal'
$supplier.Arrival = 20.5
$supplier.Departure = 35.5
$supplier.BuyLimit = [decimal]80000
$supplier.SellLimit = [decimal]20000
$supplier.Bought = [decimal]1000.75
$supplier.Sold = [decimal]50.25
$supplier.Stock.Add('Iron', 17.125)
$market.Suppliers.Add($supplier)
$receiptType = $assembly.GetType('StorageNetwork.Trading.TradingReceipt', $true)
$receipt = [Activator]::CreateInstance($receiptType)
$receipt.Id = 7
$receipt.SupplierId = 8
$receipt.Specialty = 'Metal'
$receipt.GoodId = 'Iron'
$receipt.GoodName = 'Iron'
$receipt.Amount = 17.125
$receipt.UnitPrice = [decimal]8.25
$receipt.Total = [decimal]141.28125
$receipt.BalanceAfter = [decimal]1234.567
$receipt.WorldId = 3
$receipt.Cycle = 21.5
$receipt.Buy = $true
$market.Receipts.Add($receipt)
$market.NextTradeId = 8
$json = [Newtonsoft.Json.JsonConvert]::SerializeObject($market)
$restored = [Newtonsoft.Json.JsonConvert]::DeserializeObject($json, $marketType)
$legacy = [Newtonsoft.Json.JsonConvert]::DeserializeObject('{"Balance":1234.567,"NextId":9,"Suppliers":[]}', $marketType)
if ($legacy.Balance -ne [decimal]1234.567 -or $null -eq $legacy.Receipts -or $legacy.Receipts.Count -ne 0 -or $legacy.NextTradeId -ne 1) {
    throw 'Older trading saves did not retain their balance or initialize an empty receipt history.'
}
if ($restored.Balance -ne $market.Balance -or $restored.NextId -ne 9 -or $restored.Suppliers.Count -ne 1) {
    throw 'Trading wallet or supplier sequence changed during JSON round trip.'
}
$visitor = $restored.Suppliers[0]
if ($restored.NextTradeId -ne 8 -or $restored.Receipts.Count -ne 1 -or $restored.Receipts[0].Id -ne 7 -or
    $restored.Receipts[0].Total -ne [decimal]141.28125 -or $restored.Receipts[0].BalanceAfter -ne [decimal]1234.567 -or
    $restored.Receipts[0].WorldId -ne 3 -or $restored.Receipts[0].UnitPrice -ne [decimal]8.25) {
    throw 'Trading receipt amounts, provenance or sequence changed during JSON round trip.'
}
if ($visitor.Id -ne 8 -or $visitor.Specialty -ne 'Metal' -or $visitor.Arrival -ne 20.5 -or $visitor.Departure -ne 35.5 -or
    $visitor.BuyLimit -ne 80000 -or $visitor.SellLimit -ne 20000 -or
    $visitor.Bought -ne 1000.75 -or $visitor.Sold -ne 50.25 -or $visitor.Stock['Iron'] -ne 17.125) {
    throw 'Trading visitor lifetime, turnover, or inventory changed during JSON round trip.'
}
$stateType = $assembly.GetType('StorageNetwork.Trading.StorageNetworkTradingState', $true)
$field = $stateType.GetField('marketJson', [Reflection.BindingFlags]'Instance,NonPublic')
if (-not $field -or -not $field.IsDefined([KSerialization.Serialize], $true)) {
    throw 'Trading state is not marked for game save serialization.'
}
Write-Output 'Trading JSON round trip and game save field checks passed against the actual mod assembly.'
