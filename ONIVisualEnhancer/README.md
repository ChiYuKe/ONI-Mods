# ONI Visual Enhancer

《缺氧》画面调整模组。点击顶部状态栏新增的按钮打开设置窗口，修改会立即应用，并通过 `PlayerPrefs` 保存，下次进入游戏恢复。

## 当前功能

- 隐藏原版屏幕暗角与警报暗角。
- 基础调整：亮度、暗部压低。
- 相机后处理开关：曝光、对比度、饱和度、色温、色相偏移、色散、镜头畸变和泛光。
- 材质调整开关：液体色彩、反光与流动，固体色彩与反光，以及纹理缩放。
- 滚动设置窗口、参数滑条和重置按钮。

当前界面直接调整参数；旧说明中的 Soft、Cinematic 等预设，以及颗粒、扫描线选项已不适用于当前代码。

## 构建与安装

在仓库根目录执行：

```powershell
dotnet build ONIVisualEnhancer/ONIVisualEnhancer.csproj -c Release
```

Release 产物位于 `ONIVisualEnhancer/bin/Release`。安装时需保留模组元数据和 `assets/visual_enhancer_postprocess` 资源包，不能只复制 DLL；相机效果需要可加载的 `Hidden/ONIVisualEnhancer/PostProcess` Shader，加载失败时画面直接透传。

## 实现

通过 `Game.OnSpawn` 初始化设置窗口、相机后处理与材质调整，通过 `MeterScreen.OnSpawn` 添加状态栏按钮。相机效果使用 `OnRenderImage` 与 Shader，材质调整由 `MaterialVisualController` 执行。模组不依赖 StorageNetwork，设置不写入殖民地存档。
