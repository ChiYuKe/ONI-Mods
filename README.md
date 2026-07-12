# Oxygen Not Included Mods Collection (缺氧 Mod 合集)

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Game Version](https://img.shields.io/badge/Game-Oxygen%20Not%20Included-orange)](https://store.steampowered.com/app/457140/_/)


欢迎来到我的《缺氧》(ONI) Mod 仓库！这里包含了我个人制作的缺氧模组代码。

---

## 🛠️ Mod 列表

本仓库采用多项目管理，每个目录对应一个 Mod 或开发工具。

### 功能型 Mod

| 源码目录 | Mod 名称 | 功能描述 | 获取 / 下载 |
| :--- | :--- | :--- | :--- |
| [AutomaticHarvest](./AutomaticHarvest) | 自动收获 | 优化植物收获流程，自动处理成熟作物。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3623778703) |
| [DarkMoonGalaxyTree](./DarkMoonGalaxyTree) | 食碳草桩 | 新增以二氧化碳为资源的特殊植物及产物。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=2919913185) |
| [DeepSeekDanmaku](./DeepSeekDanmaku) | DeepSeek 殖民地弹幕 | 定期汇总殖民地状态并请求 AI 点评，以滚动弹幕显示。 | 源码 |
| [ElementExpansion](./ElementExpansion) | 元素扩展 | 添加新元素或调整现有元素属性。 | 源码 |
| [EternalDecay](./EternalDecay) | 永恒衰老 | 为复制人增加随时间变化的衰老机制。 | 源码 |
| [MiniBox](./MiniBox) | 游手的小工具箱 | 提供一组便捷的游戏内小工具和可配置功能。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=2890083659) |
| [MinionAge](./MinionAge) | 复制人年龄 | 记录并显示复制人年龄，并提供寿命相关机制。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3323127058) |
| [MinionAge_DLC](./MinionAge_DLC) | 复制人年龄扩展 | MinionAge 的扩展内容和交互功能。 | 源码 |
| [MoreFood](./MoreFood) | 更多食物 | 丰富游戏菜谱，提供更多食物和饮食选择。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=2957924585) |
| [MusicBox](./MusicBox) | 音乐盒 | 新增 1×1 音乐盒建筑，接收 4-bit 信号并播放对应钢琴音符。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3762583359) |
| [NewMap](./NewMap) | 葱翠裂谷 | 新增资源较宽裕、保持原版风格的自定义地图。 | 源码 |
| [ONIVisualEnhancer](./ONIVisualEnhancer) | ONI Visual Enhancer | 提供色调、暗角、颗粒和扫描线等轻量画面预设。 | 源码 |
| [OxygenConsumingPlant](./OxygenConsumingPlant) | 耗氧植物 | 引入消耗氧气的特殊植物及其产物。 | 源码 |
| [RunningOutOfTime](./RunningOutOfTime) | Running Out of Time | 为复制人增加时间与寿命相关机制。 | 源码 |
| [StorageNetwork](./StorageNetwork) | 储存网络 | 将储存、生产建筑、喷泉和订单调度整合到可视化管理窗口。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3732422991) |
| [TAccessories](./TAccessories) | TAccessories | 扩展复制人饰品、效果及相关游戏内容。 | 源码 |
| [VignetteBegone](./VignetteBegone) | 移除晕影 | 移除或切换游戏边缘暗角与警报晕影。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3460554894) |
| [WASDMinionControl](./WASDMinionControl) | WASD 复制人控制 | 使用 WASD 直接控制当前跟随镜头中的复制人。 | 源码 |
| [WireAnywhere](./WireAnywhere) | 任意布线 | 放宽电线与高压电线的建造限制。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=2923332049) |

### 开发工具与示例

| 源码目录 | 项目 | 用途 |
| :--- | :--- | :--- |
| [CykModUtils](./CykModUtils) | CykModUtils | 仓库内 Mod 共用的工具库。 |
| [DebugUI](./DebugUI) | Debug UI | 用于查看实体、组件、日志和纹理的游戏内调试界面。 |
| [FunnyComponents](./FunnyComponents) | Funny Components | 组件和 Mod 机制实验项目。 |
| [NewElementRegistration](./NewElementRegistration) | 新元素注册示例 | 演示通过 elements YAML 和 Substance 补丁注册自定义元素。 |
| [ONIResourceBridge](./ONIResourceBridge) | ONI Resource Bridge | 将游戏已加载的 KAnim 资源通过本机接口提供给 KAnimGUI。 |
| [SN_DuplicantGenetics](./SN_DuplicantGenetics) | StorageNetwork 扩展示例 | 演示储存网络组件接口和主面板扩展。 |
| [TestMod](./TestMod) | AB UI Toolkit | 用于测试 AssetBundle UI 预制体和运行时辅助功能。 |
| [TestPlanter](./TestPlanter) | 植物测试 | 自定义植物、果实和作物机制的测试项目。 |
| [FoodandFoodBuffTutorialCase](./TutorialCase/FoodandFoodBuffTutorialCase) | 食物与 Buff 教程 | 演示自定义食物和 Buff 的教程工程。 |

> **提示**：点击“源码目录”可直接查看代码，点击“获取 / 下载”跳转至 Steam 创意工坊或发布页。

---

## 📦 如何安装

### 1. 编译安装 (开发者)
如果您想从源码进行编译：
1. 克隆本仓库到本地。
2. 使用 Visual Studio 打开 `NOIMods.sln` 解决方案。
3. 检查项目引用，确保 `Assembly-CSharp.dll` 等游戏库文件路径正确。
4. 编译项目并将生成的 `.dll` 文件放入游戏的 `mods/Dev` 文件夹中。

## 💻 技术细节

- **开发语言:** C#
- **项目格式:** SDK-style `.csproj`，主要目标框架为 `netstandard2.1`。
- **补丁框架:** Harmony (`0Harmony`)。
- **游戏依赖:** 引用《缺氧》`OxygenNotIncluded_Data/Managed` 下的 `Assembly-CSharp.dll`、`Assembly-CSharp-firstpass.dll`、Unity 模块和 TextMeshPro 等运行库。
- **常用依赖:** 部分项目会使用 `Newtonsoft.Json`、PLib、FMODUnity、Unity UI / AssetBundle 等。
- **构建方式:** 使用 Visual Studio 打开 `NOIMods.sln`，或对单个项目执行 `dotnet build <项目>.csproj`。
- **输出位置:** 部分项目 Debug 配置会直接输出到 `Documents\Klei\OxygenNotIncluded\mods\Dev\<ModName>`。
- **适用版本:** 以当前 Steam 版 ONI/U58+ 为主，实际兼容性取决于项目引用的游戏 Managed 程序集版本。

---

## 📜 开源协议

本项目基于 **GPL-3.0 License** 开源。
这意味着你可以自由地学习、修改和分享这些代码，但如果你基于此发布了新作品，请务必保持开源并注明原作者。

---

## 🤝 贡献与反馈

如果你在游戏中遇到了 Bug，或者有更好的创意：
- 欢迎提交 [Issue](../../issues) 说明问题。
- 如果你已经修复了问题，欢迎提交 [Pull Request](../../pulls)。
