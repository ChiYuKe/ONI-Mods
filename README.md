# Oxygen Not Included Mods Collection (缺氧 Mod 合集)

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Game Version](https://img.shields.io/badge/Game-Oxygen%20Not%20Included-orange)](https://store.steampowered.com/app/457140/_/)


欢迎来到我的《缺氧》(ONI) Mod 仓库！这里包含了我个人制作的缺氧模组代码。

---

## 🛠️ Mod 列表说明

本仓库采用多项目管理，以下是各个 Mod 的功能简述：
| 源码目录 | Mod 名称 | 功能描述 | 获取 / 下载 |
| :--- | :--- | :--- | :--- |
| [**AutomaticHarvest**](./AutomaticHarvest) | 自动收获 | 优化植物收获逻辑，智能处理成熟作物。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3623778703) |
| [**MinionAge**](./MinionAge)| 复制人年龄年龄 | 记录并显示复制人的生存天数，且达到目标天数死亡 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3323127058) |
| [**MinionAge_Dlc**](./MinionAge_DLC) | 复制人年龄扩展 | 对MinionAge的扩展 |  |
| [**ElementExpansion**](./ElementExpansion) | 元素扩展 | 增加新元素或调整现有元素属性。 |  |
| [**MoreFood**](./MoreFood) | 更多食物 | 丰富游戏内菜谱，提供更多饮食选择。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=2957924585) |
| [**WireAnywhere**](./WireAnywhere) | 任意布线 | 优化电线建造规则，减少地形限制。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=2923332049) |
| [**VignetteBegone**](./VignetteBegone) | 移除晕影 | 去除游戏 界面 边缘暗影，画面更通透。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3460554894) |
| [**DarkMoonGalaxyTree**](./DarkMoonGalaxyTree) | 食碳草桩 | 增加了一种吃二氧化碳的植物 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=2919913185) |
| [**EternalDecay**](./EternalDecay) | 复制人年龄 | 给复制添加年龄系统 | |
| [**OxygenConsumingPlant**](./OxygenConsumingPlant) | 耗氧植物 | 引入消耗氧气的特殊植物增加挑战性。 |  |
| [**MiniBox**](./MiniBox) | 游手的小工具箱 |  | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=2890083659) |
| [**StorageNetwork**](./StorageNetwork) | 储存网络 | 将场景储存、生产建筑、喷泉和订单调度整合到一个可视化管理窗口。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3732422991) |
| [**MusicBox**](./MusicBox) | 音乐盒 | 新增一个 1x1 音乐盒建筑，接受 4-bit 信号播放对应钢琴音符。 | [Steam 订阅](https://steamcommunity.com/sharedfiles/filedetails/?id=3762583359) |

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
