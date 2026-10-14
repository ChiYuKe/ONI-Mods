# 项目索引

本仓库共 32 个 C# 项目，全部已加入 `NOIMods.sln`。保留现有源码目录和项目引用，按用途在此索引中分类。`StorageNetwork` 是储存网络的维护目录。

## 项目与构建状态

2026-10-15 使用本机游戏 Managed 程序集、.NET SDK 10.0.101 执行 Release 构建：25 个项目生成产物，7 个项目存在编译错误。编译成功不代表游戏内行为已验证。以下状态是本次检查快照。

| 项目入口 | 类型 | 目标框架 | Release 构建 |
|---|---|---|---|
| [AutomaticHarvest](./AutomaticHarvest/AutomaticHarvest.csproj) | 模组 | netstandard2.1 | 通过 |
| [CykModUtils](./CykModUtils/CykModUtils.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [DarkMoonGalaxy](./DarkMoonGalaxyTree/DarkMoonGalaxy.csproj) | 模组 | netstandard2.1 | 通过 |
| [DebugUI](./DebugUI/DebugUI.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [DeepSeekDanmaku](./DeepSeekDanmaku/DeepSeekDanmaku.csproj) | 模组 | netstandard2.1 | 通过 |
| [ElementExpansion](./ElementExpansion/ElementExpansion.csproj) | 模组 | netstandard2.1 | 通过 |
| [EternalDecay](./EternalDecay/EternalDecay.csproj) | 模组 | netstandard2.1 | 失败，见下表 |
| [FunnyComponents](./FunnyComponents/FunnyComponents.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [LocalCanvas](./LocalCanvas/LocalCanvas.csproj) | 模组 | netstandard2.1 | 通过 |
| [LogicNetwork](./LogicNetwork/LogicNetwork.csproj) | 模组 | netstandard2.1 | 通过 |
| [MadeInAbyss](./MadeInAbyss/MadeInAbyss.csproj) | 模组 | netstandard2.1 | 通过 |
| [MiniBox](./MiniBox/MiniBox.csproj) | 模组 | netstandard2.1 | 通过 |
| [MinionAge](./MinionAge/MinionAge.csproj) | 模组 | netstandard2.1 | 失败，见下表 |
| [MinionAge_DLC](./MinionAge_DLC/MinionAge_DLC.csproj) | 模组 | netstandard2.1 | 失败，见下表 |
| [MinionAge_MoreFood](./MoreFood/MinionAge_MoreFood.csproj) | 模组 | netstandard2.1 | 通过 |
| [MusicBox](./MusicBox/MusicBox.csproj) | 模组 | netstandard2.1 | 通过 |
| [NewElementRegistration](./NewElementRegistration/NewElementRegistration.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [NewMap](./NewMap/NewMap.csproj) | 模组 | netstandard2.1 | 失败，见下表 |
| [ONIResourceBridge](./ONIResourceBridge/ONIResourceBridge.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [ONIVisualEnhancer](./ONIVisualEnhancer/ONIVisualEnhancer.csproj) | 模组 | netstandard2.1 | 通过 |
| [OxygenConsumingPlant](./OxygenConsumingPlant/OxygenConsumingPlant.csproj) | 模组 | netstandard2.1 | 失败，见下表 |
| [RunningOutOfTime](./RunningOutOfTime/RunningOutOfTime.csproj) | 模组 | netstandard2.1 | 失败，见下表 |
| [SN_DuplicantGenetics](./SN_DuplicantGenetics/SN_DuplicantGenetics.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [StorageNetwork.Tests](./StorageNetwork.Tests/StorageNetwork.Tests.csproj) | 验证 | net8.0 | 通过 |
| [StorageNetwork](./StorageNetwork/StorageNetwork.csproj) | 模组 | netstandard2.1 | 通过 |
| [TAccessories](./TAccessories/TAccessories.csproj) | 模组 | netstandard2.1 | 失败，见下表 |
| [TestMod](./TestMod/TestMod.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [TestPlanter](./TestPlanter/TestPlanter.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [FoodandFoodBuffTutorialCase](./TutorialCase/FoodandFoodBuffTutorialCase/FoodandFoodBuffTutorialCase.csproj) | 工具 / 示例 | netstandard2.1 | 通过 |
| [VignetteBegone](./VignetteBegone/VignetteBegone.csproj) | 模组 | netstandard2.1 | 通过 |
| [WASDMinionControl](./WASDMinionControl/WASDMinionControl.csproj) | 模组 | netstandard2.1 | 通过 |
| [WireAnywhere](./WireAnywhere/WireAnywhere.csproj) | 模组 | netstandard2.1 | 通过 |

## 旧项目编译问题

以下为代表性错误，修复时仍需检查对应项目的完整编译输出。

| 项目 | 当前阻碍 |
|---|---|
| MinionAge | 缺少 `MinionBrainCounter`、`TUNINGS` 等类型或定义，并使用旧游戏接口。 |
| MinionAge_DLC | 缺少 `DebuffRoulette.ModPatch`，`PathProber` 等接口与当前游戏不兼容。 |
| OxygenConsumingPlant | 使用已变化的 `Health.CanBeIncapacitated`、`Navigator.maxProbingRadius` 等接口。 |
| EternalDecay | 使用当前游戏未提供的 `GameUtil.FloodFillFind`、`CollectCellsBreadthFirst` 等接口。 |
| RunningOutOfTime | `Content.UI.DETAILTABS` 等引用无法解析。 |
| TAccessories | 使用旧 `Navigator.maxProbingRadius` 接口，缺少 `AETE_CursorDurationMarker` 等类型。 |
| NewMap | `ModConfig` 引用以及链接的 StorageNetwork 配置源码缺少依赖。 |

## 构建与验证

```powershell
dotnet build NOIMods.sln -c Release --nologo -m:1
dotnet build StorageNetwork/StorageNetwork.csproj -c Release --nologo
pwsh -NoProfile -File StorageNetwork.Tests/Verify.ps1
```

Release 产物统一留在各项目 `bin/Release`；`StorageNetwork.Tests` 为 `bin/Release/net8.0`。本轮统一了 MadeInAbyss、MiniBox、MinionAge、MinionAge_DLC、MoreFood、MusicBox、ONIResourceBridge 的 Release 输出。Debug 部署配置保持原有设置。游戏路径可通过项目支持的 `GameManagedDir` 或 `ONIManagedPath` 属性覆盖；旧项目仍有硬编码引用，迁移机器时需逐项确认。

储存网络本次验证：4,637 项断言通过，性能日志解析、逻辑材料与交易序列化通过，279 条公共接口签名匹配，76 个 Harmony 目标及 81 个补丁签名通过。完整门禁仍被 `StorageNetwork/UI/Panel/StorageNetworkPanel.Style.cs` 的全局 Sprite 扫描拦截，共两条静态规则失败；游戏内性能基准尚未验收。详见 [验证说明](./StorageNetwork.Tests/README.md) 与 [优化记录](./StorageNetwork.Tests/ALGORITHM_OPTIMIZATION.md)。

## 源码、资源与本地文件

- 模组源码、翻译、项目文件及工具脚本按现有目录维护。
- `ONI源码`、`ONIUnity项目`、`packages`、`.tools` 和 `.buildtest` 为本地参考、资源工程、依赖或验证产物，受忽略规则管理。
- `bin`、`obj`、`.vs` 为生成内容；提交源码时不包含这些目录。
- 多数 `anim` 资源目录被忽略，发布模组时应检查游戏资源是否齐全。

## 本轮提交阶段

1. 储存网络搬运范围、事务内来源查询与性能计数。
2. 对应运行时验证、日志检查、静态检查及优化说明。
3. 全仓 Release 输出整理、完整项目索引与 README 导航。
