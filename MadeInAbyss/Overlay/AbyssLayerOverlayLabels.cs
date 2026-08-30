using TMPro;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 「深渊层阶」概览层的文字标注：概览激活时在屏幕左侧绘制每层的名称标签，
    /// 标签随镜头移动对齐各层带的垂直位置，切走概览或离开画面时隐藏。
    /// 显隐与刷新由 AbyssLayerOverlay.ModeInstance 的生命周期驱动。
    /// </summary>
    public static class AbyssLayerOverlayLabels
    {
        private static Canvas canvas;
        private static readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[AbyssStatics.Layers.Length];
        private static bool active;

        /// <summary>随概览菜单初始化。由 OverlayMenu 补丁调用。</summary>
        public static void Initialize()
        {
            // 画布对象随场景销毁，引用失效时置空以便重建。
            if (canvas != null && canvas.gameObject == null)
                canvas = null;
            SetActive(false);
        }

        public static void SetActive(bool value)
        {
            active = value;
            if (active)
                EnsureBuilt();
            if (canvas != null)
                canvas.gameObject.SetActive(value);
        }

        private static void EnsureBuilt()
        {
            if (canvas != null)
                return;

            GameObject canvasGo = new GameObject("AbyssLayerOverlayCanvas", typeof(Canvas));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            TMP_FontAsset font = FindCjkFont();

            for (int i = 0; i < labels.Length; i++)
            {
                GameObject labelGo = new GameObject($"layerLabel{i}", typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(canvas.transform, false);
                TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
                if (font != null)
                    label.font = font;
                label.fontSize = 40;
                label.fontStyle = FontStyles.Bold;
                label.enableWordWrapping = false;
                label.raycastTarget = false;
                label.color = Color.Lerp(AbyssLayerOverlayColors(i), Color.white, 0.35f);
                label.text = Strings.Get($"STRINGS.OVERLAYS.ABYSS_LAYERS.LAYER{i + 1}.NAME");

                RectTransform rt = label.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(12f, 0f);
                rt.sizeDelta = new Vector2(520f, 34f);
                labels[i] = label;
            }

            canvasGo.AddComponent<LabelUpdater>();
        }

        private static TMP_FontAsset FindCjkFont()
        {
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (TMP_FontAsset font in fonts)
            {
                if (font != null && font.name != null && font.name.Contains("Noto"))
                    return font;
            }
            return TMP_Settings.defaultFontAsset;
        }

        private static Color AbyssLayerOverlayColors(int layer)
        {
            // 与概览层染色同源：通过反射外的公共入口不可用，这里按层返回概览的层色。
            switch (layer)
            {
                case 0: return new Color32(0xC7, 0xC7, 0x4A, 0xFF);
                case 1: return new Color32(0xF2, 0x91, 0x3D, 0xFF);
                case 2: return new Color32(0xB0, 0x4A, 0xF2, 0xFF);
                case 3: return new Color32(0xF4, 0x4A, 0x4A, 0xFF);
                case 4: return new Color32(0x4A, 0x9B, 0xF2, 0xFF);
                default: return new Color32(0xF2, 0x41, 0x8A, 0xFF);
            }
        }

        /// <summary>每帧把标签对齐到各自层带中心的屏幕位置。</summary>
        private class LabelUpdater : MonoBehaviour
        {
            private void Update()
            {
                if (!active || canvas == null)
                    return;

                Camera cam = Camera.main;
                if (cam == null)
                    return;

                int worldId = ClusterManager.Instance.activeWorldId;
                float surfaceY = AbyssAnchors.GetSurfaceY(worldId);
                float[] thresholds = AbyssStatics.GetLayerThresholds(worldId);
                if (float.IsNaN(surfaceY) || thresholds == null || thresholds.Length < labels.Length)
                    return;

                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i] == null)
                        continue;

                    // 每帧刷新文本以跟随游戏语言。
                    labels[i].text = Strings.Get($"STRINGS.OVERLAYS.ABYSS_LAYERS.LAYER{i + 1}.NAME");

                    // 层带 i 的染色范围为 [阈值i, 阈值i+1)，标签取染色带的中点；
                    // 最后一层向下无限延伸，取其下方半个层高处。
                    float bandTopDepth = thresholds[i];
                    float bandBottomDepth = (i + 1 < thresholds.Length)
                        ? thresholds[i + 1]
                        : thresholds[i] + (thresholds[i] - (i > 0 ? thresholds[i - 1] : 0f)) * 0.5f;
                    float bandCenterDepth = (bandTopDepth + bandBottomDepth) * 0.5f;

                    Vector3 screen = cam.WorldToScreenPoint(new Vector3(cam.transform.position.x, surfaceY - bandCenterDepth, 0f));
                    bool visible = screen.z > 0f && screen.y > 50f && screen.y < Screen.height - 30f;
                    labels[i].gameObject.SetActive(visible);
                    if (visible)
                        labels[i].rectTransform.anchoredPosition = new Vector2(12f, screen.y);
                }
            }
        }
    }
}
