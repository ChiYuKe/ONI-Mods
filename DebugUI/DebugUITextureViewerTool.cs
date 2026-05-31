using System;
using ImGuiNET;
using UnityEngine;

namespace DebugUI
{
    public sealed class DebugUITextureViewerTool : DevTool
    {
        private readonly Texture2D texture;
        private readonly Rect sourceRect;
        private readonly string sourceKind;
        private readonly string exportPrefix;
        private readonly Sprite sprite;
        private float zoom = 1f;

        private DebugUITextureViewerTool(Texture2D texture, Rect sourceRect, string sourceKind, string exportPrefix, Sprite sprite)
        {
            this.texture = texture;
            this.sourceRect = sourceRect;
            this.sourceKind = sourceKind;
            this.exportPrefix = exportPrefix;
            this.sprite = sprite;
            Name = "Texture Viewer / 纹理查看";
            RequiresGameRunning = false;
        }

        public static DebugUITextureViewerTool ForTexture(Texture2D texture)
        {
            Rect rect = texture == null ? Rect.zero : new Rect(0f, 0f, texture.width, texture.height);
            string name = texture == null ? "texture" : texture.name;
            return new DebugUITextureViewerTool(texture, rect, "Texture / 纹理", "texture_" + name, null);
        }

        public static DebugUITextureViewerTool ForSprite(Sprite sprite)
        {
            Texture2D spriteTexture = sprite == null ? null : sprite.texture;
            Rect rect = spriteTexture == null
                ? Rect.zero
                : sprite.textureRect;
            string name = sprite == null ? "sprite" : sprite.name;
            return new DebugUITextureViewerTool(spriteTexture, rect, "Sprite / 精灵", "sprite_" + name, sprite);
        }

        protected override void RenderTo(DevPanel panel)
        {
            if (texture == null)
            {
                ImGui.Text("<Texture missing / 纹理不存在>");
                return;
            }

            Name = "Texture Viewer: " + texture.name;
            ImGui.Text(sourceKind + ": " + texture.name);
            ImGui.Text("Size / 尺寸: " + Mathf.RoundToInt(sourceRect.width) + " x " + Mathf.RoundToInt(sourceRect.height));
            ImGui.SliderFloat("Zoom / 缩放", ref zoom, 0.05f, 16f, "%.2fx");
            if (ImGui.Button("Fit / 适配"))
            {
                zoom = CalculateFitZoom();
            }
            ImGui.SameLine();
            if (ImGui.Button("1:1 / 原始"))
            {
                zoom = 1f;
            }
            if (sprite != null)
            {
                ImGui.SameLine();
                int previewMode = DebugUIComponentEditorTool.GetSpritePreviewMode(sprite);
                if (ImGui.Button(DebugUIComponentEditorTool.GetSpritePreviewModeLabel(previewMode)))
                {
                    DebugUIComponentEditorTool.SetSpritePreviewMode(sprite, (previewMode + 1) % 3);
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Export PNG / 导出 PNG"))
            {
                string path = sprite == null
                    ? DebugUIComponentEditorTool.ExportTextureRegion(texture, sourceRect, exportPrefix)
                    : DebugUIComponentEditorTool.ExportSpriteTexture(sprite);
                Debug.Log("[DebugUI] Exported viewer texture: " + path);
            }
            ImGui.Separator();

            IntPtr textureId = DebugUIComponentEditorTool.GetTextureId(texture);
            if (textureId == IntPtr.Zero)
            {
                ImGui.Text("<bind failed / 绑定失败>");
                return;
            }

            Vector2 imageSize = new Vector2(
                Mathf.Max(1f, sourceRect.width * zoom),
                Mathf.Max(1f, sourceRect.height * zoom));
            Vector2 childSize = ImGui.GetContentRegionAvail();
            if (childSize.y < 64f)
            {
                childSize.y = 64f;
            }

            ImGui.BeginChild("Texture Scroll View / 纹理滚动视图", childSize, true, ImGuiWindowFlags.HorizontalScrollbar);
            Vector2 uv0;
            Vector2 uv1;
            if (sprite == null)
            {
                uv0 = Vector2.zero;
                uv1 = Vector2.one;
            }
            else
            {
                DebugUIComponentEditorTool.GetSpriteUv(texture, sourceRect, DebugUIComponentEditorTool.GetSpritePreviewMode(sprite), out uv0, out uv1);
            }
            ImGui.Image(textureId, imageSize, uv0, uv1);
            ImGui.EndChild();
        }

        private float CalculateFitZoom()
        {
            Vector2 available = ImGui.GetContentRegionAvail();
            float width = Mathf.Max(1f, sourceRect.width);
            float height = Mathf.Max(1f, sourceRect.height);
            float scale = Mathf.Min(available.x / width, Mathf.Max(64f, available.y - 96f) / height);
            return Mathf.Clamp(scale, 0.05f, 16f);
        }
    }
}
