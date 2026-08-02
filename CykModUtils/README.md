# CykModUtils

`CykModUtils` 是给自己的《缺氧》Mod 共用的小工具库。它只收录仓库里确实反复使用的代码，不负责 Mod 生命周期、补丁自动发现或跨版本组件转发，也不打算替代 PLib。

## 当前常用能力

- `Core`：独立前缀日志、一次性日志、Mod 路径与启用状态查询。
- `Configuration` / `IO`：JSON 配置、UTF-8 文本和原子写入、安全的根目录路径解析。
- `Game`：资源查询、建筑占格、建造菜单与科技注册、复杂配方、复制人、网格、KAnim 单实例符号替换。
- `Localization`：STRINGS 类型注册、`.po` 加载，以及建筑、植物、种子、食物、效果和死亡原因字符串注册。
- `Unity` / `UI`：组件查询、本地图片转纹理或 Sprite、Transform 与常用 UI 引用。
- `Diagnostics`：按启用文件打开的帧耗时和 Harmony 补丁诊断。

## 引用

在 Mod 项目中添加项目引用：

```xml
<ItemGroup>
  <ProjectReference Include="..\CykModUtils\CykModUtils.csproj" />
</ItemGroup>
```

构建和发布 Mod 时，需要让 `CykModUtils.dll` 与 Mod DLL 一起出现在该 Mod 的目录中。

## 常见用法

```csharp
using CykModUtils.Configuration;
using CykModUtils.Core;
using CykModUtils.Game;
using CykModUtils.Localization;

private static readonly ModLogger Logger = Log.Create("MyMod");

public static void RegisterBuilding()
{
    const string id = "MyBuilding";

    GameStringRegistrationUtility.RegisterBuilding(
        id,
        "我的建筑",
        "建筑描述",
        "建筑效果");

    BuildingRegistrationUtility.AddToPlanAndTech(
        "Base",
        "FineArt",
        id,
        logger: Logger);
}
```

```csharp
ComplexRecipe recipe = RecipeUtility.Create(
    new[] { RecipeUtility.Element(SimHashes.Water.CreateTag(), 10f) },
    new[] { RecipeUtility.Element(SimHashes.Ice.CreateTag(), 10f) },
    IceCooledFanConfig.ID,
    40f,
    "把水加工成冰。");
```

```csharp
MyConfig config = JsonConfigStore.LoadOrCreate(
    configPath,
    normalize: value => value.Brightness =
        UnityEngine.Mathf.Clamp01(value.Brightness),
    logger: Logger);
```

新增工具前，优先确认至少有两个 Mod 在重复实现同一件事；只被单个 Mod 使用且业务性很强的代码留在原项目中。
