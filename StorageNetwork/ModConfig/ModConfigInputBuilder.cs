using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModConfig
{
    internal sealed class ModConfigInputFieldMarker : MonoBehaviour
    {
    }

    internal static class ModConfigInputBuilder
    {
        public static TMP_InputField CreateTmpNumberInput(
            Transform parent,
            string name,
            string value,
            float width,
            float height,
            int fontSize,
            TextAlignmentOptions alignment,
            Color borderColor,
            Color backgroundColor,
            Color textColor,
            Vector2 textAreaPadding)
        {
            GameObject root = CreateRoot(name, parent, borderColor, "web_border", width, height);
            root.SetActive(false);
            TMP_InputField input = CreateInputCore(root, value, fontSize, alignment, backgroundColor, textColor, textAreaPadding);
            root.SetActive(true);
            return input;
        }

        private static TMP_InputField CreateInputCore(
            GameObject root,
            string value,
            int fontSize,
            TextAlignmentOptions alignment,
            Color backgroundColor,
            Color textColor,
            Vector2 textAreaPadding)
        {
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(root.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.pivot = new Vector2(0.5f, 0.5f);
            textAreaRect.offsetMin = textAreaPadding;
            textAreaRect.offsetMax = -textAreaPadding;
            Image textAreaImage = textArea.AddComponent<Image>();
            textAreaImage.color = backgroundColor;
            textArea.AddComponent<RectMask2D>();

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? string.Empty;
            text.fontSize = fontSize;
            text.alignment = NormalizeSingleLineAlignment(alignment);
            text.color = textColor;
            text.enabled = true;
            text.raycastTarget = true;
            text.autoSizeTextContainer = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.maxVisibleLines = 1;
            text.margin = Vector4.zero;
            Stretch(text.rectTransform(), 0f, 0f);

            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.textComponent = text;
            input.textViewport = textAreaRect;
            input.characterLimit = 16;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.inputType = TMP_InputField.InputType.Standard;
            input.interactable = true;
            input.enabled = true;
            input.isRichTextEditingAllowed = false;
            input.keyboardType = TouchScreenKeyboardType.Default;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.navigation = Navigation.defaultNavigation;
            input.richText = false;
            input.selectionColor = new Color(0.7411765f, 0.854902f, 1f, 1f);
            input.transition = Selectable.Transition.None;
            input.restoreOriginalTextOnEscape = true;
            input.text = value ?? string.Empty;
            root.AddComponent<ModConfigInputFieldMarker>();
            return input;
        }

        private static TextAlignmentOptions NormalizeSingleLineAlignment(TextAlignmentOptions alignment)
        {
            switch (alignment)
            {
                case TextAlignmentOptions.TopLeft:
                case TextAlignmentOptions.Left:
                case TextAlignmentOptions.BottomLeft:
                case TextAlignmentOptions.BaselineLeft:
                case TextAlignmentOptions.CaplineLeft:
                    return TextAlignmentOptions.MidlineLeft;
                case TextAlignmentOptions.TopRight:
                case TextAlignmentOptions.Right:
                case TextAlignmentOptions.BottomRight:
                case TextAlignmentOptions.BaselineRight:
                case TextAlignmentOptions.CaplineRight:
                    return TextAlignmentOptions.MidlineRight;
                case TextAlignmentOptions.Top:
                case TextAlignmentOptions.Bottom:
                case TextAlignmentOptions.Baseline:
                case TextAlignmentOptions.Capline:
                    return TextAlignmentOptions.Midline;
                default:
                    return alignment;
            }
        }

        private static GameObject CreateRoot(string name, Transform parent, Color color, string spriteName, float width, float height)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.AddComponent<RectTransform>();
            Image image = root.AddComponent<Image>();
            ApplyOniSprite(image, spriteName, color, Image.Type.Sliced);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.minWidth = width;
            layout.minHeight = height;
            return root;
        }

        private static void ApplyOniSprite(Image image, string spriteName, Color color, Image.Type type)
        {
            Sprite sprite = Assets.GetSprite(spriteName);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = type;
                image.fillCenter = true;
                image.pixelsPerUnitMultiplier = 2f;
            }

            image.color = color;
        }

        private static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}
