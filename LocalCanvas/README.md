# 本地画布

## 使用方式

第一次启动游戏后，Mod 文件夹中会自动生成 `config.json` 和 `images` 文件夹。将图片分别放入对应的子文件夹：

```json
{
  "CanvasFolder": "images/Canvas",
  "CanvasTallFolder": "images/CanvasTall",
  "CanvasWideFolder": "images/CanvasWide",
  "ImageBrightness": 0.8,
  "VerticalOffset": 0.0,
  "DepthOffset": -0.05
}
```

图片会出现在原版画布的“重新装饰”选择界面中。点击缩略图并完成绘画后，图片会保存到该画布。每张图片会按普通、不错、精美三种艺术品质注册，以适配原版的艺术品质筛选。

支持 PNG、JPG 和 JPEG 图片。`ImageBrightness` 控制图片亮度，`VerticalOffset` 与 `DepthOffset` 调整显示位置。

## 游戏内截图

选中普通、竖版或横版画布后，点击“截图”进入取景模式，再点击“保存”。截图以 `Capture_时间戳.png` 保存到该画布类型配置的图片目录；右键保留摄像机拖动功能，点击“取消”或按 Escape 退出。
