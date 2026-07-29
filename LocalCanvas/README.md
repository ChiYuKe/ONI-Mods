# 本地画布

## 使用方式

第一次启动游戏后，Mod 文件夹中会自动生成 `config.json` 和 `images` 文件夹。将图片分别放入对应的子文件夹：

```json
{
  "CanvasFolder": "images/Canvas",
  "CanvasTallFolder": "images/CanvasTall",
  "CanvasWideFolder": "images/CanvasWide"
}
```

图片会出现在原版画布的“重新装饰”选择界面中。点击缩略图并完成绘画后，图片会保存到该画布。每张图片会按普通、不错、精美三种艺术品质注册，以适配原版的艺术品质筛选。
