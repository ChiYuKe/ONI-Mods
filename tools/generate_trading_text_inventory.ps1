# Generates the StorageNetworkTrading message-text inventory from the compiled catalog
# (Trading/TradingTextCatalog.cs), the English PO file and the C# call sites.
# Output: StorageNetworkTrading/Trading/TEXT_INVENTORY.md and .csv
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$modDir = Join-Path $root 'StorageNetworkTrading'
$catalogPath = Join-Path $modDir 'Trading\TradingTextCatalog.cs'
$poPath = Join-Path $modDir 'translations\en.po'
$mdPath = Join-Path $modDir 'Trading\TEXT_INVENTORY.md'
$csvPath = Join-Path $modDir 'Trading\TEXT_INVENTORY.csv'
$prefix = 'STRINGS.UI.STORAGENETWORK_TRADING.'

function ConvertFrom-CSharpLiteral([string] $s) {
    $sb = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $s.Length; $i++) {
        $c = $s[$i]
        if ($c -ne '\') { [void]$sb.Append($c); continue }
        $i++
        if ($i -ge $s.Length) { break }
        switch ($s[$i]) {
            'n' { [void]$sb.Append("`n") }
            'r' { [void]$sb.Append("`r") }
            't' { [void]$sb.Append("`t") }
            '0' { [void]$sb.Append([char]0) }
            '"' { [void]$sb.Append('"') }
            '\' { [void]$sb.Append('\') }
            'u' { [void]$sb.Append([char][Convert]::ToInt32($s.Substring($i + 1, 4), 16)); $i += 4 }
            default { [void]$sb.Append($s[$i]) }
        }
    }
    $sb.ToString()
}

$entries = @()
$raw = Get-Content -LiteralPath $catalogPath -Raw
foreach ($m in [regex]::Matches($raw, 'new TradingTextEntry\("([^"]+)",\s*"((?:[^"\\]|\\.)*)"\)')) {
    $key = $m.Groups[1].Value
    $entries += [pscustomobject]@{
        Key = $key; Short = $key.Substring($prefix.Length)
        Chinese = (ConvertFrom-CSharpLiteral $m.Groups[2].Value); English = ''
        Used = (New-Object System.Collections.Generic.HashSet[string])
        Direct = (New-Object System.Collections.Generic.HashSet[string])
        DynamicBy = ''
    }
}
$byShort = @{}
foreach ($e in $entries) { $byShort[$e.Short] = $e }

$map = @{}; $curCtx = $null; $curStr = ''; $mode = ''
$flush = { if ($curCtx) { $map[$curCtx] = $curStr } }
foreach ($line in (Get-Content -LiteralPath $poPath)) {
    if ($line -match '^msgctxt\s+"(.*)"\s*$') { & $flush; $curCtx = $Matches[1]; $curStr = ''; $mode = 'ctx'; continue }
    if ($line -match '^msgid\s+"(.*)"\s*$') { $mode = 'id'; continue }
    if ($line -match '^msgstr\s+"(.*)"\s*$') { $curStr = $Matches[1]; $mode = 'str'; continue }
    if ($line -match '^"(.*)"\s*$') {
        if ($mode -eq 'str') { $curStr += $Matches[1] } elseif ($mode -eq 'ctx') { $curCtx += $Matches[1] }
    }
}
& $flush
foreach ($e in $entries) { if ($map.ContainsKey($e.Key)) { $e.English = (ConvertFrom-CSharpLiteral $map[$e.Key]) } }

$scanFiles = Get-ChildItem -Path $modDir, (Join-Path $root 'StorageNetwork'), (Join-Path $root 'StorageNetwork.Tests') -Recurse -File -Include *.cs -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' -and $_.Name -ne 'TradingTextCatalog.cs' }
$scripts = @{}
foreach ($f in $scanFiles) {
    $rel = $f.FullName.Substring($root.Length + 1)
    $text = Get-Content -LiteralPath $f.FullName -Raw
    $scripts[$rel] = $text
    foreach ($m in [regex]::Matches($text, 'STRINGS\.UI\.STORAGENETWORK_TRADING\.([A-Za-z0-9_]+)')) {
        if ($byShort.ContainsKey($m.Groups[1].Value)) { [void]$byShort[$m.Groups[1].Value].Used.Add($rel); [void]$byShort[$m.Groups[1].Value].Direct.Add($rel) }
    }
    foreach ($m in [regex]::Matches($text, '(?:WeeklyText|WeeklyLabel)\("([A-Za-z0-9_]+)"')) {
        $k = 'WEEKLY_' + $m.Groups[1].Value
        if ($byShort.ContainsKey($k)) { [void]$byShort[$k].Used.Add($rel); [void]$byShort[$k].Direct.Add($rel) }
    }
    foreach ($m in [regex]::Matches($text, '(?:AiText|ChatText|GroupText)\("([A-Za-z0-9_]+)"')) {
        $k = $m.Groups[1].Value
        if ($byShort.ContainsKey($k)) { [void]$byShort[$k].Used.Add($rel); [void]$byShort[$k].Direct.Add($rel) }
    }
    if ($text.Contains('REL_STAGE_"')) { 0..4 | ForEach-Object { $k = "REL_STAGE_$_"; if ($byShort.ContainsKey($k)) { [void]$byShort[$k].Used.Add($rel); [void]$byShort[$k].Direct.Add($rel) } } }
    if ($text.Contains('REL_TONE_"')) {
        foreach ($k in @('REL_TONE_0_CLOSE','REL_TONE_0_KNOWN','REL_TONE_1_CLOSE','REL_TONE_1_KNOWN','REL_TONE_2_CLOSE','REL_TONE_2_KNOWN',
                         'REL_TONE_GREYFINCH_CLOSE','REL_TONE_GREYFINCH_KNOWN','REL_TONE_LAMPLOW_CLOSE','REL_TONE_LAMPLOW_KNOWN',
                         'REL_TONE_OLDMI_CLOSE','REL_TONE_OLDMI_KNOWN')) {
            if ($byShort.ContainsKey($k)) { [void]$byShort[$k].Used.Add($rel); [void]$byShort[$k].Direct.Add($rel) }
        }
    }
}

# Keys that are assembled at run time (variant suffixes, temperament indexes, topic tables)
# are matched by the literal fragment the code concatenates or passes around.
foreach ($e in $entries) {
    if ($e.Used.Count -gt 0) { continue }
    $frags = New-Object System.Collections.Generic.List[string]
    $frags.Add($e.Short)
    if ($e.Short -match '^WEEKLY_') {
        $rest = $e.Short.Substring('WEEKLY_'.Length)
        $frags.Add($rest)
        $frags.Add(($rest -replace '_ALT\d+$', ''))
        $frags.Add(($rest -replace '_\d+$', '_'))
        $frags.Add(($rest -replace '_(FAST|SLOW)$', ''))
        $frags.Add(($rest -replace '_(UP|DOWN)$', ''))
        $frags.Add((($rest -split '_')[0]) + '_')
    }
    if ($e.Short -match '^REL_TONE_') { $frags.Add('REL_TONE_') }
    foreach ($frag in ($frags | Where-Object { $_ } | Sort-Object Length -Descending)) {
        foreach ($rel in $scripts.Keys) {
            if ($scripts[$rel].Contains($frag)) { [void]$e.Used.Add($rel) }
        }
        if ($e.Used.Count -gt 0) { $e.DynamicBy = $frag; break }
    }
}

# ---------------- grouping ----------------
$weeklyGroups = [ordered]@{
    'ARRIVAL'     = '到访预告（航运电台 · 下一班来客）'
    'PORTRAIT'    = '柜台侧写（柜台背后 · 熟客笔记）'
    'CARGO'       = '货单线索（货单边角 · 谁的舱门开着）'
    'COMPARE'     = '同货比价（两张价签 · 同货不同价）'
    'MOVEMENT'    = '价签追踪（价签追踪 · 笔迹未干）'
    'ROUTE'       = '柜台索引（舱单索引 · 别走错柜台）'
    'CONTACT'     = '往来消息（往来账 · 交情有轻重）'
    'DEPARTURE'   = '离港消息（泊位簿 · 船不等人）'
    'PROCUREMENT' = '收购余款（收货簿 · 谁还愿意收）'
    'NEWCOMER'    = '新来商人（柜台换人 · 新面孔）'
    'SKETCH'      = '柜台轶事（8 类独立叙事）'
    'RUMOR'       = '码头茶话（码头茶话 · 言外之意）'
    'TIP'         = '供应商口信（捎个口信，最多两位商人）'
    'FRAME'       = '周报界面框架与单位'
}
function Get-WeeklyGroup([string] $short) {
    $rest = $short.Substring('WEEKLY_'.Length)
    foreach ($k in $weeklyGroups.Keys) { if ($k -ne 'FRAME' -and $rest.StartsWith($k, [StringComparison]::Ordinal)) { return $k } }
    return 'FRAME'
}

$textAreas = [ordered]@{
    'StorageNetworkTrading\Trading\TradingPersona.cs'                    = '供应商人格台词（闲聊、话题与话术）'
    'StorageNetworkTrading\Trading\TradingConversations.cs'              = '情境对话台词（按行情、库存、关系生成）'
    'StorageNetworkTrading\Trading\TradingDialogue.cs'                   = '本地对话事件台词（规则讲解、商人自述）'
    'StorageNetworkTrading\Trading\TradingDialogueContext.cs'            = 'AI 系统提示约束（随请求发给模型）'
    'StorageNetworkTrading\Trading\TradingCharacters.cs'                 = '固定角色姓名与外观描述'
    'StorageNetworkTrading\Trading\StorageNetworkTradingState.cs'        = '交易结算与运行期提示（通知、失败原因）'
    'StorageNetworkTrading\Trading\TradingMarket.Market.cs'              = '市场事件与行情提示'
    'StorageNetworkTrading\Trading\TradingMarket.cs'                     = '交易限制与校验提示'
    'StorageNetworkTrading\Trading\TradingPrecision.cs'                  = '数量精度与最小转移量提示'
    'StorageNetworkTrading\UI\TradingPanel.Trading.cs'                   = '交易主界面标签与提示'
    'StorageNetworkTrading\UI\TradingPanel.Trading.Chart.cs'             = '价格走势图标签与提示'
    'StorageNetworkTrading\UI\TradingPanel.Trading.Delivery.cs'          = '交付与仓库选择'
    'StorageNetworkTrading\UI\TradingPanel.Trading.Dialogue.cs'          = '通讯窗口'
    'StorageNetworkTrading\UI\TradingPanel.Trading.Identity.cs'          = '商人身份卡'
    'StorageNetworkTrading\ModEntry.cs'                                  = '模组入口按钮'
}
function Get-TextArea([string] $usedIn) {
    if ($usedIn -match ';') { return '跨模块共用标签' }
    if ($usedIn -and $textAreas.Contains($usedIn)) { return $textAreas[$usedIn] }
    if (-not $usedIn) { return '未在代码中引用（遗留文本）' }
    return $usedIn
}

$rows = foreach ($e in $entries) {
    $short = $e.Short
    $category = if ($short -match '^(WEEKLY|GROUP|AI|REL)_') { $Matches[1] }
        elseif ($short -like 'TEXT_*') { 'TEXT' }
        else { 'PUNCT' }
    $group = switch ($category) {
        'WEEKLY' { $weeklyGroups[(Get-WeeklyGroup $short)] }
        'TEXT'   { Get-TextArea (($e.Used | Sort-Object) -join '; ') }
        default  { $category }
    }
    $reference = if ($e.Direct.Count -gt 0) { '直接引用' } elseif ($e.Used.Count -gt 0) { '动态拼接' } else { '未引用' }
    [pscustomobject]@{
        Category  = $category
        Group     = $group
        Key       = $e.Key
        Short     = $e.Short
        Reference = $reference
        UsedIn    = (($e.Used | Sort-Object) -join '; ')
        Chinese   = $e.Chinese
        English   = $e.English
    }
}

$rows | Select-Object @{n='键';e={$_.Key}}, @{n='短键';e={$_.Short}}, @{n='分类';e={$_.Category}}, @{n='分组';e={$_.Group}},
    @{n='引用方式';e={$_.Reference}}, @{n='引用位置';e={$_.UsedIn}}, @{n='中文源文本';e={$_.Chinese}}, @{n='英文';e={$_.English}} |
    Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding utf8BOM

# ---------------- markdown ----------------
function Format-Cell([string] $text) {
    if ($null -eq $text) { return '' }
    $t = $text -replace "`r`n", '\n' -replace "`n", '\n' -replace "`r", '\n' -replace '\|', '\|' -replace '<', '\<'
    $t.Trim()
}
$md = New-Object System.Collections.Generic.List[string]
$md.Add('# StorageNetworkTrading 讯息文本清单')
$md.Add('')
$md.Add('> 本文件由 `tools/generate_trading_text_inventory.ps1` 自动生成，请勿手工编辑。')
$md.Add('> 源数据：`StorageNetworkTrading/Trading/TradingTextCatalog.cs`（中文源文本，编译进程序集）与 `StorageNetworkTrading/translations/en.po`（英文，随模组加载）。')
$md.Add('> 表格中的 `\n` 表示源文本里的换行符；`{0}`、`{1}` 等是格式化占位符。')
$md.Add('')

$counts = $rows | Group-Object Category | Sort-Object Count -Descending
$md.Add('## 一、总览')
$md.Add('')
$md.Add("共 **$($rows.Count)** 条文本，键前缀统一为 ``STRINGS.UI.STORAGENETWORK_TRADING.``。")
$md.Add('')
$md.Add('“引用方式”一栏区分三种情况：**直接引用**（代码里写着完整键）、**动态拼接**（代码按栏目、写法编号或性格索引拼出键）、**未引用**（已编译、已翻译，但没有任何调用点）。')
$md.Add('')
$md.Add('| 分类 | 前缀 | 条数 | 引用方式（直接/动态） | 用途 |')
$md.Add('| --- | --- | --- | --- | --- |')
$usage = @{
    WEEKLY = '星际商讯周报的栏目正文、栏目名与界面框架（每 7 周期一期，保留 16 期）'
    GROUP  = 'AI 群聊窗口的按钮、状态、校验与错误提示'
    AI     = 'AI 配置窗口的字段、按钮与保存/校验提示'
    REL    = '供应商关系阶段名、关系口吻台词与关系卡正文'
    TEXT   = '交易界面、对话、结算与市场提示（含发送给 AI 的系统提示约束）'
    PUNCT  = '标点与分隔符片段，用于拼接句子'
}
$prefixes = @{ WEEKLY = '`WEEKLY_`'; GROUP = '`GROUP_`'; AI = '`AI_`'; REL = '`REL_`'; TEXT = '`TEXT_`（键名为源文本哈希）'; PUNCT = '`SENTENCE_` / `LIST_` / `BOUNDARY_` / `OPEN_` / `QUOTE_`' }
foreach ($c in $counts) {
    $direct = @($rows | Where-Object { $_.Category -eq $c.Name -and $_.Reference -eq '直接引用' }).Count
    $dynamic = @($rows | Where-Object { $_.Category -eq $c.Name -and $_.Reference -eq '动态拼接' }).Count
    $md.Add("| $($c.Name) | $($prefixes[$c.Name]) | $($c.Count) | $direct / $dynamic | $($usage[$c.Name]) |")
}
$md.Add('')

function Add-Table([System.Collections.Generic.List[string]] $md, [object[]] $items) {
    $md.Add('| 键 | 中文源文本 | English |')
    $md.Add('| --- | --- | --- |')
    foreach ($i in $items) {
        $md.Add("| ``$($i.Short)`` | $(Format-Cell $i.Chinese) | $(Format-Cell $i.English) |")
    }
    $md.Add('')
}

$md.Add('## 二、星际商讯周报 `WEEKLY_*`')
$md.Add('')
$md.Add('每期目标 8 则，同一栏目每期最多一则；正文多为三种写法（原稿 + `_ALT1` + `_ALT2`），代码按主题哈希与期号轮换，`_ALT1/_ALT2` 因此在代码里是动态拼接的键。')
$md.Add('')
foreach ($g in $weeklyGroups.Keys) {
    $items = @($rows | Where-Object { $_.Category -eq 'WEEKLY' -and $_.Group -eq $weeklyGroups[$g] })
    if ($items.Count -eq 0) { continue }
    $md.Add("### $($weeklyGroups[$g])（$($items.Count) 条）")
    $md.Add('')
    Add-Table $md $items
}

$names = @{ GROUP = '三、AI 群聊窗口 `GROUP_*`'; AI = '四、AI 配置窗口 `AI_*`'; REL = '五、供应商关系 `REL_*`' }
$intros = @{
    GROUP = '群聊窗口的按钮、成员提示、运行状态与错误提示，另含 3 条随请求发给模型的提示模板（`GROUP_PROMPT`、`GROUP_COHERENCE_PROMPT`、`GROUP_LIVELY_PROMPT`）。'
    AI    = 'AI 配置窗口的字段名、按钮与保存/校验提示；这些文本只出现在本地界面，不会发给模型。'
    REL   = '好感阶段名（初识 → 信赖，每 20 点一级）、关系口吻台词（3 种普通性格与 3 位固定角色，各分“熟悉/亲近”两种）以及关系卡正文 `REL_CARD`。'
}
foreach ($cat in @('GROUP', 'AI', 'REL')) {
    $items = @($rows | Where-Object { $_.Category -eq $cat })
    $md.Add("## $($names[$cat])（$($items.Count) 条）")
    $md.Add('')
    $md.Add($intros[$cat])
    $md.Add('')
    Add-Table $md $items
}

$md.Add('## 六、界面、对话与结算文本 `TEXT_*`')
$md.Add('')
$md.Add('键名是源文本的哈希，代码按常量键引用；下表按引用位置归组。')
$md.Add('')
$textGroupOrder = @($textAreas.Values) + @('跨模块共用标签')
foreach ($g in $textGroupOrder) {
    $items = @($rows | Where-Object { $_.Category -eq 'TEXT' -and $_.Group -eq $g })
    if ($items.Count -eq 0) { continue }
    $md.Add("### $g（$($items.Count) 条）")
    $md.Add('')
    Add-Table $md $items
}

$punct = @($rows | Where-Object { $_.Category -eq 'PUNCT' })
$md.Add("## 七、标点与分隔符（$($punct.Count) 条）")
$md.Add('')
Add-Table $md $punct

$unused = @($rows | Where-Object { $_.Reference -eq '未引用' })
if ($unused.Count -gt 0) {
    $md.Add("## 八、未被引用的文本（$($unused.Count) 条）")
    $md.Add('')
    $md.Add('下列键已编译、已翻译，但在本模组、前置 StorageNetwork 与测试代码中都既没有完整键引用，也找不到用于拼接的字面片段，属于遗留文本。')
    $md.Add('')
    Add-Table $md $unused
}

Set-Content -LiteralPath $mdPath -Value ($md -join "`n") -Encoding utf8NoBOM

"wrote $mdPath"
"wrote $csvPath"
$rows | Group-Object Category | Sort-Object Count -Descending | Select-Object Count, Name | Format-Table -AutoSize
"unused: $($unused.Count) -> $(($unused | ForEach-Object { $_.Short }) -join ', ')"
