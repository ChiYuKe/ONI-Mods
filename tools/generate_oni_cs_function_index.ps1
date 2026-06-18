param(
    [string]$SourceDir = "ONI源码\Assembly-CSharp",
    [string]$OutputPath = "ONI源码\Assembly-CSharp\CS_FILE_FUNCTION_INDEX.md",
    [string]$StringsPath = "D:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\StreamingAssets\strings\strings_preinstalled_zh_klei.po"
)

$ErrorActionPreference = "Stop"

function Get-RelativePath {
    param([string]$BasePath, [string]$FullPath)
    $base = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $full = [System.IO.Path]::GetFullPath($FullPath)
    return $full.Substring($base.Length).Replace('\', '/')
}

function Escape-MarkdownCell {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return "" }
    return ($Text -replace '\|', '\|' -replace "`r?`n", " ").Trim()
}

function ConvertFrom-PoQuotedString {
    param([string]$Text)
    if ($null -eq $Text) { return "" }
    $value = $Text
    if ($value.StartsWith('"') -and $value.EndsWith('"')) {
        $value = $value.Substring(1, $value.Length - 2)
    }
    $value = $value -replace '\\"', '"' -replace '\\n', "`n" -replace '\\t', "`t" -replace '\\\\', '\'
    return $value
}

function Get-DisplayNameFromPoValue {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return "" }
    $value = $Text.Trim()
    $linkMatch = [regex]::Match($value, '<link="[^"]+">(.+?)</link>')
    if ($linkMatch.Success) {
        return $linkMatch.Groups[1].Value.Trim()
    }
    return (($value -replace '<[^>]+>', '')).Trim()
}

function Import-BuildingChineseNames {
    param([string]$PoPath)
    $names = @{}
    if ([string]::IsNullOrWhiteSpace($PoPath) -or -not (Test-Path -LiteralPath $PoPath)) {
        return $names
    }

    $currentId = $null
    foreach ($line in [System.IO.File]::ReadLines($PoPath)) {
        $contextMatch = [regex]::Match($line, '^msgctxt\s+"STRINGS\.BUILDINGS\.PREFABS\.([^."]+)\.NAME"$')
        if ($contextMatch.Success) {
            $currentId = $contextMatch.Groups[1].Value.ToUpperInvariant()
            continue
        }

        if ($null -ne $currentId) {
            $msgStrMatch = [regex]::Match($line, '^msgstr\s+(".*")$')
            if ($msgStrMatch.Success) {
                $raw = ConvertFrom-PoQuotedString $msgStrMatch.Groups[1].Value
                $displayName = Get-DisplayNameFromPoValue $raw
                if (-not [string]::IsNullOrWhiteSpace($displayName)) {
                    $names[$currentId] = $displayName
                }
                $currentId = $null
            }
        }
    }
    return $names
}

function Get-PrimaryTypes {
    param([string]$Text)
    $matches = [regex]::Matches(
        $Text,
        '(?m)^\s*(?:\[.*?\]\s*)*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|new)\s+)*(class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)'
    )
    $types = New-Object System.Collections.Generic.List[string]
    foreach ($match in $matches) {
        $name = $match.Groups[2].Value
        if (-not $types.Contains($name)) {
            $types.Add($name)
        }
        if ($types.Count -ge 5) { break }
    }
    if ($types.Count -eq 0) { return "" }
    return ($types -join ", ")
}

function Get-BuildingId {
    param([string]$Text)

    $constMatch = [regex]::Match($Text, 'public\s+const\s+string\s+ID\s*=\s*"([^"]+)"')
    if ($constMatch.Success) {
        return $constMatch.Groups[1].Value
    }

    $staticMatch = [regex]::Match($Text, 'public\s+static\s+(?:readonly\s+)?string\s+ID\s*=\s*"([^"]+)"')
    if ($staticMatch.Success) {
        return $staticMatch.Groups[1].Value
    }

    $createDefLiteral = [regex]::Match($Text, '(?:BuildingTemplates\.)?CreateBuildingDef\s*\(\s*"([^"]+)"')
    if ($createDefLiteral.Success) {
        return $createDefLiteral.Groups[1].Value
    }

    $baseCreateDefLiteral = [regex]::Match($Text, 'base\.CreateBuildingDef\s*\(\s*"([^"]+)"')
    if ($baseCreateDefLiteral.Success) {
        return $baseCreateDefLiteral.Groups[1].Value
    }

    $localIdMatch = [regex]::Match($Text, '(?s)CreateBuildingDef\s*\(\s*\)\s*\{.{0,1200?}string\s+\w+\s*=\s*"([^"]+)".{0,1200?}(?:CreateBuildingDef|CreateBuildingDef\()')
    if ($localIdMatch.Success) {
        return $localIdMatch.Groups[1].Value
    }

    $fileClassMatch = [regex]::Match($Text, '(?m)^\s*public\s+class\s+([A-Za-z_][A-Za-z0-9_]*)Config\s*:')
    if ($fileClassMatch.Success) {
        return $fileClassMatch.Groups[1].Value
    }

    return ""
}

function Get-CategoryAndDescription {
    param(
        [string]$RelativePath,
        [string]$FileName,
        [string]$Text
    )

    $nameNoExt = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
    $pathLower = $RelativePath.ToLowerInvariant()
    $nameLower = $nameNoExt.ToLowerInvariant()
    $sample = if ($Text.Length -gt 16000) { $Text.Substring(0, 16000) } else { $Text }

    $category = "其他/系统基础"
    $description = "游戏系统、工具类或底层逻辑；需结合类名和引用位置继续确认。"

    if ($pathLower.StartsWith("strings/")) {
        return @("文本/本地化", "STRINGS 文本定义：界面、建筑、物品、状态项等显示文字。")
    }
    if ($pathLower.StartsWith("tuning/")) {
        return @("调参/TUNING", "TUNING 常量：建筑成本、温度、食物、复制人、科技等平衡参数。")
    }
    if ($pathLower.StartsWith("database/")) {
        return @("数据库/Db", "数据库条目：注册技能、科技、状态、动画、房间、食谱、世界规则等游戏数据。")
    }
    if ($pathLower.StartsWith("procgen/") -or $pathLower.StartsWith("procgengame/")) {
        return @("世界生成/ProcGen", "世界生成相关：星球、地形、地点、遗迹、模板或生成规则。")
    }
    if ($pathLower.StartsWith("klei/ai/")) {
        return @("AI/属性状态", "Klei.AI 系统：属性、效果、状态、修饰器、疾病或行为数据。")
    }
    if ($pathLower.StartsWith("unityengine/") -or $pathLower.StartsWith("system/") -or $pathLower.StartsWith("microsoft/") -or $pathLower.StartsWith("tmpro/")) {
        return @("外部/引擎库", "Unity/.NET/TextMeshPro 等反编译库或兼容代码，通常不是缺氧玩法主体。")
    }

    if ($sample -match 'IBuildingConfig|CreateBuildingDef|BuildingDef') {
        return @("建筑配置", "建筑配置：定义建筑 ID、尺寸、动画、材料、建造菜单、科技、工作温度和完成后组件。")
    }
    if ($sample -match 'IEntityConfig|CreatePrefab|EntityTemplates\.ExtendEntityTo') {
        if ($sample -match 'ExtendEntityToFood|FoodInfo|EdiblesManager|Rottable|Calories') {
            return @("食物/可食用实体", "食物实体配置：定义食物 prefab、热量、品质、腐败、可食用组件和掉落物。")
        }
        if ($sample -match 'BaseMinionConfig|DUPLICANTSTATS|MinionIdentity') {
            return @("复制人/小人", "复制人实体配置：创建小人 prefab、身份、属性、装备槽或相关组件。")
        }
        if ($sample -match 'BaseCritterConfig|BaseBabyConfig|CreatureBrain|Butcherable|Traits\.CREATURE') {
            return @("生物/小动物", "生物实体配置：创建小动物、蛋、幼体、行为组件、掉落和驯养相关数据。")
        }
        return @("实体配置", "实体配置：创建物品、生物、掉落物、装饰物或其他 prefab，并挂载组件。")
    }

    if ($sample -match 'GameStateMachine|StateMachine|StateMachineComponent') {
        return @("状态机", "状态机：控制建筑、生物、任务或交互对象的运行状态和状态切换。")
    }
    if ($sample -match 'SideScreen|KScreen|FScreen|Screen|Panel|Widget|UserMenu|ToolMenu|DetailsPanel|HoverText|Tooltip') {
        return @("UI/界面", "界面逻辑：侧边栏、详情面板、按钮、弹窗、工具菜单、提示文本或 HUD 显示。")
    }
    if ($sample -match 'Chore|Workable|ComplexFabricator|FetchChore|StorageChore|RanchChore|DoctorChore') {
        return @("任务/工作 Chore", "复制人任务逻辑：工作台、搬运、操作、治疗、牧场、清扫或可工作组件。")
    }
    if ($sample -match 'Conduit|UtilityNetwork|FlowUtility|Pipe|Valve|Bridge|PortDisplay|SolidConduit|LiquidConduit|GasConduit') {
        return @("管道/运输网络", "管道与运输网络：气体、液体、固体运输、桥、阀门、端口和网络连接逻辑。")
    }
    if ($sample -match 'Battery|Generator|EnergyConsumer|EnergyGenerator|Power|Wire|Circuit|Joule|Watt') {
        return @("电力/电池", "电力系统：发电、耗电、电池储能、电路、导线、过载或电力 UI。")
    }
    if ($sample -match 'Storage|UserNameable|TreeFilterable|FilteredStorage|Refrigerator|Rottable') {
        return @("储存/库存", "储存与库存：容器、过滤器、冰箱、物品容量、取放逻辑和保存数据。")
    }
    if ($sample -match 'EdiblesManager|FoodInfo|FoodQuality|Calories|Rottable|Gourmet|Crop|CookingStation') {
        return @("食物/农业/烹饪", "食物、作物或烹饪逻辑：热量、品质、腐败、收获、配方和食材处理。")
    }
    if ($sample -match 'Plant|Crop|Wilt|Growing|Harvest|Fertilizer|Irrigation|Seed') {
        return @("植物/农业", "植物农业：种子、作物、生长、收获、灌溉、施肥、枯萎或环境需求。")
    }
    if ($sample -match 'Creature|Critter|Egg|Ranch|Tame|Wildness|Butcher|Navigator') {
        return @("生物/小动物", "生物与小动物：行为、导航、繁殖、蛋、驯养、屠宰或牧场交互。")
    }
    if ($sample -match 'Minion|Duplicant|Dupe|Skill|Trait|Identity|Assignable|RoleStation') {
        return @("复制人/小人", "复制人系统：身份、技能、特质、装备、分配、需求或小人状态。")
    }
    if ($sample -match 'Disease|Germ|Sickness|Medical|Clinic|Doctor|Immune') {
        return @("疾病/医疗", "疾病和医疗：病菌、免疫、治疗站、医生工作或状态效果。")
    }
    if ($sample -match 'Research|Tech|TechTree|Knowledge') {
        return @("科技/研究", "科技研究：科技树、研究点、研究站、解锁和知识数据库。")
    }
    if ($sample -match 'Room|RoomType|RoomTracker|Cavity|BuildingComplete') {
        return @("房间/基地规则", "房间和基地规则：房间类型、检测、加成、建筑完成体或空间条件。")
    }
    if ($sample -match 'Element|SimHashes|Cell|Grid|Temperature|Mass|PrimaryElement|BuildingHP|Overheat') {
        return @("元素/网格/模拟", "元素与模拟：格子、质量、温度、物质状态、过热、传热或世界格点数据。")
    }
    if ($sample -match 'SaveLoad|ISaveLoadable|KSerialization|Serialize|Deserialize|Game.Instance') {
        return @("存档/序列化", "存档序列化：保存加载、数据迁移、组件状态持久化或游戏实例数据。")
    }
    if ($sample -match 'Achievement|ColonyAchievement|ReportManager') {
        return @("成就/报告", "成就和报告：殖民地成就、日报、统计、消息或进度追踪。")
    }
    if ($nameLower -match 'config$') {
        return @("配置/Prefab", "配置类：通常负责创建 prefab、注册资源或给对象挂载组件。")
    }
    if ($sample -match 'KMonoBehaviour|MonoBehaviour') {
        return @("组件/行为脚本", "组件脚本：挂在建筑、实体或 UI 对象上，处理生命周期、交互和运行逻辑。")
    }

    return @($category, $description)
}

$sourceFull = [System.IO.Path]::GetFullPath($SourceDir)
if (-not (Test-Path -LiteralPath $sourceFull)) {
    throw "SourceDir not found: $sourceFull"
}

$files = Get-ChildItem -LiteralPath $sourceFull -Filter "*.cs" -File -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Sort-Object FullName

$buildingChineseNames = Import-BuildingChineseNames -PoPath $StringsPath

$rows = foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction Stop
    $relative = Get-RelativePath -BasePath $sourceFull -FullPath $file.FullName
    $types = Get-PrimaryTypes -Text $text
    $info = Get-CategoryAndDescription -RelativePath $relative -FileName $file.Name -Text $text
    $buildingId = ""
    $buildingName = ""
    if ($info[0] -eq "建筑配置") {
        $buildingId = Get-BuildingId -Text $text
        if (-not [string]::IsNullOrWhiteSpace($buildingId)) {
            $nameKey = $buildingId.ToUpperInvariant()
            if ($buildingChineseNames.ContainsKey($nameKey)) {
                $buildingName = $buildingChineseNames[$nameKey]
            }
        }
    }
    [pscustomobject]@{
        Category = $info[0]
        Path = $relative
        Id = $buildingId
        ChineseName = $buildingName
        Types = $types
        Description = $info[1]
    }
}

$categoryOrder = @(
    "建筑配置",
    "实体配置",
    "食物/可食用实体",
    "食物/农业/烹饪",
    "储存/库存",
    "电力/电池",
    "管道/运输网络",
    "UI/界面",
    "任务/工作 Chore",
    "状态机",
    "组件/行为脚本",
    "复制人/小人",
    "生物/小动物",
    "植物/农业",
    "疾病/医疗",
    "科技/研究",
    "房间/基地规则",
    "元素/网格/模拟",
    "存档/序列化",
    "成就/报告",
    "数据库/Db",
    "调参/TUNING",
    "文本/本地化",
    "世界生成/ProcGen",
    "AI/属性状态",
    "配置/Prefab",
    "外部/引擎库",
    "其他/系统基础"
)

$grouped = $rows | Group-Object Category
$categoryMap = @{}
foreach ($group in $grouped) { $categoryMap[$group.Name] = $group.Group }

$now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$builder = New-Object System.Text.StringBuilder
[void]$builder.AppendLine("# ONI Assembly-CSharp C# 文件功能索引")
[void]$builder.AppendLine()
[void]$builder.AppendLine('- 源码目录：`' + $SourceDir + '`')
[void]$builder.AppendLine("- 生成时间：$now")
[void]$builder.AppendLine("- 文件数量：$($rows.Count)")
[void]$builder.AppendLine('- 中文文本：`' + $StringsPath + '`')
[void]$builder.AppendLine()
[void]$builder.AppendLine("> 说明：这是按文件名、路径、类声明和常见缺氧代码特征自动推断的索引，用来快速定位源码；个别文件的实际职责仍以源码内容和调用链为准。")
[void]$builder.AppendLine()
[void]$builder.AppendLine("## 按功能分类快速入口")
[void]$builder.AppendLine()
[void]$builder.AppendLine("| 功能分类 | 文件数 |")
[void]$builder.AppendLine("|---|---:|")

foreach ($category in $categoryOrder) {
    if ($categoryMap.ContainsKey($category)) {
        $anchor = ($category.ToLowerInvariant() -replace '[ /]', '-' -replace '[^\p{L}\p{Nd}\-]', '')
        [void]$builder.AppendLine("| [$category](#$anchor) | $($categoryMap[$category].Count) |")
    }
}

foreach ($category in $categoryOrder) {
    if (-not $categoryMap.ContainsKey($category)) { continue }
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("## $category")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("| 文件 | 对象ID | 中文名 | 主要类型 | 功能说明 |")
    [void]$builder.AppendLine("|---|---|---|---|---|")
    foreach ($row in ($categoryMap[$category] | Sort-Object Path)) {
        $path = Escape-MarkdownCell $row.Path
        $id = Escape-MarkdownCell $row.Id
        $chineseName = Escape-MarkdownCell $row.ChineseName
        $types = Escape-MarkdownCell $row.Types
        $description = Escape-MarkdownCell $row.Description
        [void]$builder.AppendLine('| `' + $path + '` | ' + $id + ' | ' + $chineseName + ' | ' + $types + ' | ' + $description + ' |')
    }
}

$outputFull = [System.IO.Path]::GetFullPath($OutputPath)
$outputDir = [System.IO.Path]::GetDirectoryName($outputFull)
if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}
[System.IO.File]::WriteAllText($outputFull, $builder.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Generated $outputFull"
Write-Host "Indexed $($rows.Count) C# files"
