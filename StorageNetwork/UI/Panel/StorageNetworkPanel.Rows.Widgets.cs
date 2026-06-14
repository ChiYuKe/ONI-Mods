using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private static void CreateFoldoutIcon(Transform parent, bool expanded)
        {
            GameObject iconObject = new GameObject("FoldoutIcon");
            iconObject.transform.SetParent(parent, false);
            iconObject.AddComponent<RectTransform>();
            LayoutElement layout = iconObject.AddComponent<LayoutElement>();
            layout.minWidth = 18f;
            layout.preferredWidth = 18f;
            layout.minHeight = 18f;
            layout.preferredHeight = 18f;

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            Sprite sprite = GetSpriteByName(expanded ? "iconDown" : "iconRight");
            if (sprite == null)
            {
                sprite = GetSpriteByName(expanded ? "dash_arrow_down" : "dash_arrow");
            }

            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.type = Image.Type.Simple;
                icon.color = new Color(0.28f, 0.30f, 0.30f, 0.72f);
                return;
            }

            UnityEngine.Object.DestroyImmediate(icon);
            TextMeshProUGUI arrow = CreateText("Arrow", iconObject.transform, expanded ? "▼" : "▶", 12, TextAlignmentOptions.Center);
            arrow.color = new Color(0.28f, 0.30f, 0.30f, 0.72f);
            Stretch(arrow.rectTransform(), 0f, 0f);
        }

        private static void CreateFoldoutTitleIcon(Transform parent, Sprite sprite, Color color)
        {
            GameObject iconObject = new GameObject("TitleIcon");
            iconObject.transform.SetParent(parent, false);
            iconObject.AddComponent<RectTransform>();
            LayoutElement layout = iconObject.AddComponent<LayoutElement>();
            layout.minWidth = 22f;
            layout.preferredWidth = 22f;
            layout.minHeight = 22f;
            layout.preferredHeight = 22f;

            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = color;
        }

        private static void AddButtonIcon(Transform parent, string spriteName, string fallbackText)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            Stretch(iconRect, 5f, 3f);

            Sprite sprite = GetSpriteByName(spriteName);
            if (sprite != null)
            {
                Image icon = iconObject.AddComponent<Image>();
                icon.sprite = sprite;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = new Color(0.92f, 0.94f, 0.96f, 1f);
                return;
            }

            TextMeshProUGUI text = CreateText("FallbackText", iconObject.transform, fallbackText, 10, TextAlignmentOptions.Center);
            text.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            Stretch(text.rectTransform(), 0f, 0f);
        }

        private static void AddButtonIconLabel(Transform parent, string spriteName, string fallbackText, string labelText)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(5f, 0f);
            iconRect.sizeDelta = new Vector2(16f, 16f);

            Sprite sprite = GetSpriteByName(spriteName);
            if (sprite != null)
            {
                Image icon = iconObject.AddComponent<Image>();
                icon.sprite = sprite;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            }
            else
            {
                TextMeshProUGUI fallback = CreateText("FallbackText", iconObject.transform, fallbackText, 10, TextAlignmentOptions.Center);
                fallback.color = new Color(0.92f, 0.94f, 0.96f, 1f);
                Stretch(fallback.rectTransform(), 0f, 0f);
            }

            TextMeshProUGUI label = CreateText("Label", parent, labelText, 11, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform labelRect = label.rectTransform();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 0f);
            labelRect.offsetMax = new Vector2(-5f, 0f);
        }
    }
}
