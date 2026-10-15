param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = (Join-Path $PSScriptRoot '..\StorageNetworkTrading\bin\Release\StorageNetworkTrading.dll')
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
$market.LastAmounts.Add('Iron:buy', 17.125)
$market.Pressure.Add('Iron', [decimal]0.025)
$market.LastPressureCycle = 21
$market.LastHistoryCycle = 21
$supplier = [Activator]::CreateInstance($supplierType)
$supplier.Id = 8
$supplier.Name = 'Merchant Ada'
$personaType = $assembly.GetType('StorageNetwork.Trading.TradingPersona', $true)
$publicPersonaType = $assembly.GetType('StorageNetwork.Trading.TradingPublicPersona', $true)
$privatePersonaType = $assembly.GetType('StorageNetwork.Trading.TradingPrivatePersona', $true)
$persona = [Activator]::CreateInstance($personaType)
$persona.IdentityId = 'greyfinch'
$persona.Public = [Activator]::CreateInstance($publicPersonaType)
$persona.Public.Temperament = 'Calm and watchful'
$persona.Public.SpeechStyle = 'Brief sentences'
$persona.Public.Likes = @('Honest quantities', 'Privacy')
$persona.Private = [Activator]::CreateInstance($privatePersonaType)
$persona.Private.Background = 'AUTHOR_KNOWLEDGE'
$persona.Private.DisclosureRule = 'Hints only'
$supplier.Persona = $persona
$market.CharacterPersonas.Add('greyfinch', $persona)
$supplier.CharacterId = 'greyfinch'
$supplier.DialogueAssigned = $true
$supplier.DialogueEventId = 'greyfinch'
$supplier.DialogueStage = 1
$supplier.DialogueChoice = 0
$supplier.DialogueVersion = 2
$supplier.DialogueNode = 'deal'
$supplier.DialogueNegotiated = $true
$supplier.DialoguePriceFactor = [decimal]0.95
$supplier.DialogueTopics.Add('respect')
$lineType = $assembly.GetType('StorageNetwork.Trading.TradingDialogueLine', $true)
$line = [Activator]::CreateInstance($lineType)
$line.Player = $true
$line.Text = 'Accept cooperation offer'
$supplier.DialogueLines.Add($line)
$memoryType = $assembly.GetType('StorageNetwork.Trading.TradingDialogueMemory', $true)
$memory = [Activator]::CreateInstance($memoryType)
$memory.Trust = 1
$memory.Visits = 2
$memory.LastVisitId = 8
$memory.Affection = [decimal]62.125
$memory.SuccessfulTrades = 12
$memory.TotalTradeValue = [decimal]100000.75
$supplier.Relationship = $memory
$supplier.RelationshipBaselineSet = $true
$supplier.RelationshipQuotaTier = 3
$supplier.BaseDeparture = 33.5
$supplier.BaseBuyLimit = [decimal]70000
$supplier.BaseSellLimit = [decimal]18000
$market.DialogueMemories.Add('greyfinch', $memory)
$appearanceType = $assembly.GetType('StorageNetwork.Trading.TradingAppearance', $true)
$appearance = [Activator]::CreateInstance($appearanceType)
$appearance.BasePersonalityId = 'ADA'
$appearance.HeadShape = 'headshape_001'
$appearance.Hair = 'hair_002'
$appearance.Eyes = 'eyes_003'
$appearance.Mouth = 'mouth_004'
$supplier.Appearance = $appearance
$market.CharacterAppearances.Add('greyfinch', $appearance)
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
$receipt.SupplierName = $supplier.Name
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
$receipt.Delivery = 'Solid server · 17.125 kg'
$receipt.StorageIds.Add(42)
$market.Receipts.Add($receipt)
$market.NextTradeId = 8
$pointType = $assembly.GetType('StorageNetwork.Trading.TradingPricePoint', $true)
$point = [Activator]::CreateInstance($pointType)
$point.Cycle = 21
$point.Buy = [decimal]8.75
$point.Sell = [decimal]8.75
$pointListType = [Collections.Generic.List``1].MakeGenericType($pointType)
$pointList = [Activator]::CreateInstance($pointListType)
$pointList.Add($point)
$market.History.Add('Iron', $pointList)
$inventoryType = $assembly.GetType('StorageNetwork.Trading.TradingWorldInventorySignal', $true)
$inventory = [Activator]::CreateInstance($inventoryType)
$inventory.Cycle = 21
$inventory.Snapshot.Add('Iron', 10017.125)
$inventory.NeutralStock.Add('Iron', 10000)
$inventory.TradeDelta.Add('Iron', 17.125)
$inventory.Factors.Add('Iron', [decimal]0.975)
$inventory.History.Add('Iron', $pointList)
$market.WorldInventorySignals.Add(3, $inventory)
$weeklyType = $assembly.GetType('StorageNetwork.Trading.TradingWeeklyReport', $true)
$articleType = $assembly.GetType('StorageNetwork.Trading.TradingWeeklyArticle', $true)
$arrivalType = $assembly.GetType('StorageNetwork.Trading.TradingArrivalPlan', $true)
$weekly = [Activator]::CreateInstance($weeklyType)
$weekly.Number = 4
$weekly.Cycle = 21.25
$weekly.Read = $true
$article = [Activator]::CreateInstance($articleType)
$article.Title = 'Dockside report'
$article.Body = 'Remember this visitor and their habit.'
$weekly.Articles.Add($article)
$market.WeeklyReports.Add($weekly)
$market.LastBulletinWeek = 3
$arrival = [Activator]::CreateInstance($arrivalType)
$arrival.ExpectedCycle = 35.5
$arrival.Visitor = [Activator]::CreateInstance($supplierType)
$arrival.Visitor.Id = 9
$arrival.Visitor.Name = 'Expected visitor'
$market.UpcomingVisit = $arrival
$tipType = $assembly.GetType('StorageNetwork.Trading.TradingMerchantTip', $true)
$tip = [Activator]::CreateInstance($tipType)
$tip.SupplierId = 8
$tip.Window = 5
$tip.Cycle = 21.25
$tip.Expires = 24
$tip.Read = $true
$tip.Articles.Add($article)
$market.MerchantTips.Add(8, $tip)
$market.LastTipDay = 21
$json = [Newtonsoft.Json.JsonConvert]::SerializeObject($market)
if ($json.Contains('"Text":') -or -not $json.Contains('"Content":')) {
    throw 'Chat saves must store structured content parts instead of the runtime Text property.'
}
$restored = [Newtonsoft.Json.JsonConvert]::DeserializeObject($json, $marketType)
if ($restored.LastTipDay -ne 21 -or -not $restored.MerchantTips[8].Read -or
    $restored.MerchantTips[8].Expires -ne 24 -or $restored.MerchantTips[8].Articles[0].Body -ne $article.Body) {
    throw 'Merchant intelligence content, expiration or read state changed during native save round trip.'
}
if ($restored.LastBulletinWeek -ne 3 -or $restored.WeeklyReports.Count -ne 1 -or
    -not $restored.WeeklyReports[0].Read -or $restored.WeeklyReports[0].Cycle -ne 21.25 -or
    $restored.WeeklyReports[0].Articles[0].Body -ne $article.Body -or
    $restored.UpcomingVisit.ExpectedCycle -ne 35.5 -or $restored.UpcomingVisit.Visitor.Name -ne 'Expected visitor') {
    throw 'Weekly intelligence, archive read state or promised arrivals changed during native save round trip.'
}
if ($restored.WorldInventorySignals[3].TradeDelta['Iron'] -ne 17.125 -or
    $restored.WorldInventorySignals[3].NeutralStock['Iron'] -ne 10000 -or
    $restored.WorldInventorySignals[3].Factors['Iron'] -ne [decimal]0.975 -or
    $restored.WorldInventorySignals[3].History['Iron'].Count -ne 1 -or $json.Contains('"InventoryFactors":')) {
    throw 'World resource snapshots, neutral stock, pending trades or history did not survive native JSON serialization.'
}
$legacy = [Newtonsoft.Json.JsonConvert]::DeserializeObject('{"Balance":1234.567,"NextId":9,"Suppliers":[],"Favorites":["Iron"]}', $marketType)
if ($legacy.Balance -ne [decimal]1234.567 -or $null -eq $legacy.Receipts -or $legacy.Receipts.Count -ne 0 -or $legacy.NextTradeId -ne 1) {
    throw 'Older trading saves did not retain their balance or initialize an empty receipt history.'
}
if ($restored.Balance -ne $market.Balance -or $restored.NextId -ne 9 -or $restored.Suppliers.Count -ne 1) {
    throw 'Trading wallet or supplier sequence changed during JSON round trip.'
}
$visitor = $restored.Suppliers[0]
if ($visitor.Relationship.Affection -ne [decimal]62.125 -or $restored.DialogueMemories['greyfinch'].Affection -ne [decimal]62.125 -or
    $visitor.Relationship.SuccessfulTrades -ne 12 -or $visitor.Relationship.TotalTradeValue -ne [decimal]100000.75 -or
    -not $visitor.RelationshipBaselineSet -or $visitor.RelationshipQuotaTier -ne 3 -or $visitor.BaseDeparture -ne 33.5 -or
    $visitor.BaseBuyLimit -ne 70000 -or $visitor.BaseSellLimit -ne 18000) {
    throw 'Affection, trade progress or visit benefit baselines changed during game save round trip.'
}
if ($visitor.Persona.IdentityId -ne 'greyfinch' -or $visitor.Persona.Public.Temperament -ne 'Calm and watchful' -or
    $visitor.Persona.Public.Likes[1] -ne 'Privacy' -or $visitor.Persona.Private.Background -ne 'AUTHOR_KNOWLEDGE' -or
    $restored.CharacterPersonas['greyfinch'].Private.DisclosureRule -ne 'Hints only' -or $null -eq $legacy.CharacterPersonas) {
    throw 'Merchant identity cards or private author knowledge changed during save round trip.'
}
if ($visitor.DialogueNode -ne 'deal' -or $visitor.DialoguePriceFactor -ne [decimal]0.95 -or
    -not $visitor.DialogueNegotiated -or $visitor.DialogueVersion -ne 2 -or
    $visitor.DialogueLines[0].Text -ne $line.Text -or -not $visitor.DialogueLines[0].Player -or
    $visitor.DialogueTopics[0] -ne 'respect' -or $restored.DialogueMemories['greyfinch'].Trust -ne 1 -or
    $restored.DialogueMemories['greyfinch'].Visits -ne 2 -or $null -eq $legacy.DialogueMemories) {
    throw 'Conversation history, character memory or negotiated prices changed during save round trip.'
}
if (-not $visitor.DialogueAssigned -or $visitor.DialogueEventId -ne 'greyfinch' -or
    $visitor.DialogueStage -ne 1 -or $visitor.DialogueChoice -ne 0) {
    throw 'Supplier conversation event or selected branch changed during JSON round trip.'
}
if ($visitor.CharacterId -ne 'greyfinch' -or $restored.CharacterAppearances['greyfinch'].Hair -ne 'hair_002' -or
    $restored.CharacterAppearances['greyfinch'].BasePersonalityId -ne 'ADA' -or $null -eq $legacy.CharacterAppearances) {
    throw 'Recurring character IDs or portrait cache changed during JSON round trip or legacy migration.'
}
if ($visitor.Name -ne $supplier.Name -or $visitor.Appearance.BasePersonalityId -ne 'ADA' -or
    $visitor.Appearance.HeadShape -ne 'headshape_001' -or $visitor.Appearance.Hair -ne 'hair_002' -or
    $visitor.Appearance.Eyes -ne 'eyes_003' -or $visitor.Appearance.Mouth -ne 'mouth_004' -or
    $restored.Receipts[0].SupplierName -ne $supplier.Name) {
    throw 'Saved supplier identity, modular portrait parts or receipt names changed during JSON round trip.'
}
if ($restored.LastAmounts['Iron:buy'] -ne 17.125 -or
    $restored.Pressure['Iron'] -ne [decimal]0.025 -or $restored.LastPressureCycle -ne 21 -or $restored.LastHistoryCycle -ne 21 -or
    $restored.History['Iron'].Count -ne 1 -or $restored.History['Iron'][0].Buy -ne [decimal]8.75 -or
    $restored.Receipts[0].Delivery -ne $receipt.Delivery -or $restored.Receipts[0].StorageIds[0] -ne 42) {
    throw 'Saved quantities, market history, demand or delivery provenance changed during JSON round trip.'
}
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
$selectWorld = $marketType.GetMethod('SelectInventoryWorld', [Reflection.BindingFlags]'Instance,NonPublic')
$inventory.SupplierHistory.Add(8, $supplier.History)
$selectWorld.Invoke($market, @([int]3)) | Out-Null
$compactJson = [Newtonsoft.Json.JsonConvert]::SerializeObject($market)
$compactTree = [Newtonsoft.Json.Linq.JObject]::Parse($compactJson)
if ($null -ne $compactTree.Property('History') -or $null -ne $compactTree['Suppliers'][0].Property('History')) {
    throw 'Active market and supplier histories must not duplicate canonical world histories in the save.'
}
$compactRestored = [Newtonsoft.Json.JsonConvert]::DeserializeObject($compactJson, $marketType)
$selectWorld.Invoke($compactRestored, @([int]3)) | Out-Null
if ($compactRestored.History['Iron'][0].Buy -ne [decimal]8.75 -or
    -not [Object]::ReferenceEquals($compactRestored.History, $compactRestored.WorldInventorySignals[3].History) -or
    -not [Object]::ReferenceEquals($compactRestored.Suppliers[0].History, $compactRestored.WorldInventorySignals[3].SupplierHistory[8])) {
    throw 'Canonical price history aliases were not restored after compact serialization.'
}
$stateType = $assembly.GetType('StorageNetwork.Trading.StorageNetworkTradingState', $true)
$field = $stateType.GetField('marketJson', [Reflection.BindingFlags]'Instance,NonPublic')
if (-not $field -or -not $field.IsDefined([KSerialization.Serialize], $true)) {
    throw 'Trading state is not marked for game save serialization.'
}
Write-Output 'Trading JSON round trip and game save field checks passed against the actual mod assembly.'
