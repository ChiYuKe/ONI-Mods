param(
    [string] $GameManagedDir = 'D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed',
    [string] $ModAssembly = 'C:\Users\Administrator\Documents\Klei\OxygenNotIncluded\mods\Dev\StorageNetwork\StorageNetwork.dll'
)
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Join-Path $GameManagedDir 'Newtonsoft.Json.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ModAssembly).Path)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$protocolType = $assembly.GetType('StorageNetwork.Trading.TradingAiChatProtocol', $true)
$url = $protocolType.GetMethod('CompletionUrl', $flags)
if ($url.Invoke($null, @('https://example.invalid/v1/')) -ne 'https://example.invalid/v1/chat/completions' -or
    $url.Invoke($null, @('http://localhost:1234/v1/chat/completions')) -ne 'http://localhost:1234/v1/chat/completions') { throw 'Chat completion URL normalization failed.' }
$chatType = $assembly.GetType('StorageNetwork.Trading.TradingGroupChat', $true)
$lineType = $assembly.GetType('StorageNetwork.Trading.TradingGroupMessage', $true)
$contextType = $assembly.GetType('StorageNetwork.Trading.TradingDialogueContext', $true)
$chat = [Activator]::CreateInstance($chatType)
foreach ($id in @(-1, 7, 8, -1)) {
    $line = [Activator]::CreateInstance($lineType)
    $line.Player = $id -eq -1
    $line.SupplierId = $id
    $line.Name = 'Merchant ' + $id
    $line.Text = 'Test message ' + $id
    $chat.Messages.Add($line)
}
$chat.EarlierContext = 'Earlier conversation: discussed iron prices.'
$chat.FocusSupplierId = 7
$chat.TopicAnchor = 'Oil pricing with merchant 7'
$chat.LatestQuestion = 'Can you make it cheaper?'
$chat.RoutingMode = 'follow_up'
$chat.AddressedSuppliers.Add(7)
$context = [Activator]::CreateInstance($contextType)
$context.VisitId = 7
$context.Name = 'Merchant 7'
$context.Language = 'en'
$context.Specialty = 'Metal'
$contextTypeRelationship = $assembly.GetType('StorageNetwork.Trading.TradingDialogueMemory', $true)
$context.Relationship = [Activator]::CreateInstance($contextTypeRelationship)
$context.Relationship.Affection = [decimal]62.5
$goodType = $assembly.GetType('StorageNetwork.Trading.TradingContextGood', $true)
$good = [Activator]::CreateInstance($goodType)
$good.Id = 'Iron'
$good.Name = 'Iron'
$good.Stock = 3000
$good.PlayerBuyPrice = [decimal]1.25
$good.PlayerSellPrice = [decimal]1.10
$good.BuyChangePercent = [decimal]-3.5
$good.SellChangePercent = [decimal]2.2
$listType = [Collections.Generic.List``1].MakeGenericType($goodType)
$context.Goods = [Activator]::CreateInstance($listType)
$context.Goods.Add($good)
$request = $protocolType.GetMethod('Request', $flags).Invoke($null, @('test-model', $context, @{ RemainingCycles = 4 }, @(@{ Id = 7 }, @{ Id = 8 }), $chat))
$tree = [Newtonsoft.Json.Linq.JObject]::Parse($request)
if ($tree['model'].ToString() -ne 'test-model' -or $tree['stream'].ToString() -ne 'False' -or $tree['messages'].Count -ne 5) { throw 'Chat request shape is incorrect.' }
if ($tree['messages'][2]['role'].ToString() -ne 'assistant' -or $tree['messages'][3]['role'].ToString() -ne 'user') { throw 'Independent agent speaker roles were lost.' }
$system = $tree['messages'][0]['content'].ToString()
foreach ($required in @('Earlier conversation', 'Affection', '62.5', 'Iron', 'BuyChangePercent', '-3.5', 'SellChangePercent', '2.2', 'RemainingCycles', 'CURRENT_AGENT', 'CURRENT_TURN', 'follow_up', 'Oil pricing', '"IsAddressed":true', '[NO_REPLY]')) {
    if (-not $system.Contains($required)) { throw ('Missing prompt data: ' + $required) }
}
if ($request.Contains('ApiKey') -or $request.Contains('ProtectedApiKey')) { throw 'Credentials leaked into the AI prompt.' }
$reply = $protocolType.GetMethod('Reply', $flags)
$failure = $protocolType.GetMethod('Failure', $flags)
$textType = $assembly.GetType('StorageNetwork.Trading.TradingText', $true)
$source = $textType.GetMethod('Source', $flags)
$problem = $failure.Invoke($null, @([long]400, 'Bad Request', '{"error":{"message":"model_not_found synthetic-secret"}}', 'synthetic-secret'))
$visibleProblem = $source.Invoke($null, @($problem))
if (-not $visibleProblem.Contains('400') -or -not $visibleProblem.Contains('model_not_found') -or $visibleProblem.Contains('synthetic-secret')) { throw 'Service error diagnostic did not retain the cause or redact credentials.' }
$problem = $failure.Invoke($null, @([long]200, '', '{"choices":[{"message":{"content":"","reasoning_content":"reasoning"},"finish_reason":"length"}]}', ''))
if (-not $source.Invoke($null, @($problem)).Contains('推理')) { throw 'Reasoning-only responses were not diagnosed separately.' }
if ($reply.Invoke($null, @('{"choices":[{"message":{"content":"  Hello trader!  "}}]}')) -ne 'Hello trader!') { throw 'Compatible completion response could not be read.' }
if ($null -ne $reply.Invoke($null, @('{"error":{"message":"unauthorized"}}')) -or $null -ne $reply.Invoke($null, @('invalid JSON'))) { throw 'Invalid completion response was treated as a message.' }
$marketType = $assembly.GetType('StorageNetwork.Trading.TradingMarket', $true)
$market = [Activator]::CreateInstance($marketType)
$market.GroupChat = $chat
$saved = [Newtonsoft.Json.JsonConvert]::SerializeObject($market)
$restored = [Newtonsoft.Json.JsonConvert]::DeserializeObject($saved, $marketType)
if ($restored.GroupChat.Messages.Count -ne 4 -or $restored.GroupChat.EarlierContext -ne $chat.EarlierContext -or $restored.GroupChat.FocusSupplierId -ne 7 -or
    $restored.GroupChat.TopicAnchor -ne $chat.TopicAnchor) { throw 'Group chat save or conversation focus continuity failed.' }
Write-Output 'Group chat prompt contents, independent speaker roles, compatible API response parsing and native save continuity checks passed.'
