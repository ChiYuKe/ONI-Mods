using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private static Sprite GetTagIcon(Tag tag)
        {
            if (tag == Tag.Invalid)
            {
                return Assets.GetSprite("unknown");
            }

            var sprite = Def.GetUISprite(tag, "ui", false);
            return sprite.first != null ? sprite.first : Assets.GetSprite("unknown");
        }

        private GameObject CreateSection(Transform parent, string name, float height, Color color)
        {
            GameObject section = CreatePlainImage(name, parent, color);
            LayoutElement layout = section.AddComponent<LayoutElement>();
            if (height > 0f)
            {
                layout.preferredHeight = height;
                layout.minHeight = height;
            }
            else
            {
                layout.flexibleHeight = 1f;
            }

            return section;
        }

        private GameObject CreateSubPanel(Transform parent, string name, string title, float preferredWidth, float minWidth, float flexibleWidth)
        {
            GameObject panel = CreatePlainImage(name, parent, new Color(0.84f, 0.84f, 0.78f, 1f));
            LayoutElement layout = panel.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.minWidth = minWidth;
            layout.flexibleWidth = flexibleWidth;
            AddVerticalContainer(panel, 6f, 8, 8, 8, 8);
            AddSmallTitle(panel.transform, title);
            return panel;
        }

        private static void AddVerticalLayout(GameObject gameObject, float spacing, int left, int right, int top, int bottom)
        {
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(left, right, top, bottom);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void AddMetricTile(Transform parent, string label, string value, Color valueColor, float width)
        {
            GameObject tile = CreatePlainImage("MetricTile", parent, new Color(0.78f, 0.78f, 0.72f, 1f));
            LayoutElement layout = tile.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = Mathf.Min(width, 72f);
            layout.preferredHeight = 42f;
            layout.minHeight = 42f;
            layout.flexibleHeight = 0f;
            AddVerticalContainer(tile, 0f, 6, 6, 3, 3);

            TextMeshProUGUI name = CreateOrderText("Label", tile.transform, label, 9, TextAlignmentOptions.MidlineLeft);
            name.color = MutedTextColor();
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            TextMeshProUGUI amount = CreateOrderText("Value", tile.transform, value, 11, TextAlignmentOptions.MidlineLeft);
            amount.color = valueColor;
            amount.fontStyle = FontStyles.Bold;
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().preferredHeight = 21f;
        }

        private void AddSmallTitle(Transform parent, string text)
        {
            TextMeshProUGUI title = CreateOrderText("SectionTitle", parent, text, 11, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.28f, 0.30f, 0.29f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 19f;
        }

        private void AddPlanLine(Transform parent, string text, int size, FontStyles style, Color color, float height)
        {
            TextMeshProUGUI line = CreateOrderText("PlanLine", parent, text, size, TextAlignmentOptions.MidlineLeft);
            line.color = color;
            line.fontStyle = style;
            line.richText = true;
            line.textWrappingMode = TextWrappingModes.Normal;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = Mathf.Max(height, size + 7f);
        }

        private void AddWrappedPlanLine(Transform parent, string text, int size, FontStyles style, Color color, float lineHeight, int maxLines, int charsPerLine)
        {
            TextMeshProUGUI line = CreateOrderText("PlanLine", parent, text, size, TextAlignmentOptions.TopLeft);
            line.color = color;
            line.fontStyle = style;
            line.richText = true;
            line.textWrappingMode = TextWrappingModes.Normal;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.maxVisibleLines = maxLines;
            float height = Mathf.Max(lineHeight, size + 7f) * EstimateTextLineCount(text, maxLines, charsPerLine);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private void AddInfoText(Transform parent, string text, float height)
        {
            AddPlanLine(parent, text, 10, FontStyles.Italic, MutedTextColor(), height);
        }

        private static TextMeshProUGUI CreateOrderText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            return CreateText(name, parent, text, size, alignment);
        }

        private static int EstimateTextLineCount(string text, int maxLines, int charsPerLine)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 1;
            }

            int weightedLength = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                weightedLength += c <= 0x7f ? 1 : 2;
            }

            return Mathf.Clamp(Mathf.CeilToInt(weightedLength / (float)charsPerLine), 1, Mathf.Max(1, maxLines));
        }

        private static Color PositiveColor()
        {
            return new Color(0.24f, 0.40f, 0.26f, 1f);
        }

        private static Color WarningColor()
        {
            return new Color(0.58f, 0.43f, 0.20f, 1f);
        }

        private static Color DangerColor()
        {
            return new Color(0.68f, 0.18f, 0.14f, 1f);
        }

        private static Color NeutralBlue()
        {
            return new Color(0.20f, 0.34f, 0.46f, 1f);
        }

        private static Color NeutralTextColor()
        {
            return new Color(0.23f, 0.24f, 0.23f, 1f);
        }

        private static Color MutedTextColor()
        {
            return new Color(0.34f, 0.35f, 0.33f, 1f);
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                foreach (TMP_InputField input in child.GetComponentsInChildren<TMP_InputField>(true))
                {
                    SafeDeactivateInput(input);
                }

                Destroy(child);
            }
        }

        private static void ForceOrderLayout(RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            RectTransform parent = content.parent as RectTransform;
            if (parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void AddIcon(Transform parent, Sprite sprite, float size)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            LayoutElement layout = iconObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            layout.minWidth = size;
            layout.minHeight = size;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
            Image image = iconObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

    }
}
