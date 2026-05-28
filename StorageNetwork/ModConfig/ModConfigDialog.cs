using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModConfig
{
    public sealed class ModConfigField
    {
        public string Label { get; set; }
        public string Description { get; set; }
        public float Value { get; set; }
        public System.Action<float> Apply { get; set; }
    }

    public sealed class ModConfigDialogDefinition
    {
        public string OverlayName { get; set; } = "ModConfigOverlay";
        public string Title { get; set; }
        public string Hint { get; set; }
        public List<ModConfigField> Fields { get; } = new List<ModConfigField>();
        public System.Action<List<KInputTextField>> Reset { get; set; }
        public System.Action Save { get; set; }
    }

    public static class ModConfigDialog
    {
        private static GameObject currentDialog;

        public static void Show(ModConfigDialogDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            Close();
            List<KInputTextField> inputs = new List<KInputTextField>();

            GameObject overlay = new GameObject(definition.OverlayName);
            currentDialog = overlay;
            overlay.transform.SetParent(Global.Instance.globalCanvas.transform, false);
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            Stretch(overlayRect, 0f, 0f);
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.45f);

            GameObject dialog = CreatePanel("Dialog", overlay.transform, new Color(0.20f, 0.22f, 0.27f, 0.98f));
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(620f, Mathf.Clamp(170f + definition.Fields.Count * 56f, 300f, 720f));

            VerticalLayoutGroup layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText("Title", dialog.transform, definition.Title, 18, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            foreach (ModConfigField field in definition.Fields)
            {
                inputs.Add(AddField(dialog.transform, field));
            }

            if (!string.IsNullOrEmpty(definition.Hint))
            {
                TextMeshProUGUI hint = CreateText("Hint", dialog.transform, definition.Hint, 11, TextAlignmentOptions.MidlineLeft);
                hint.color = new Color(0.78f, 0.82f, 0.86f, 1f);
                hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            }

            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(dialog.transform, false);
            footer.AddComponent<RectTransform>();
            footer.AddComponent<LayoutElement>().preferredHeight = 34f;
            HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 8f;
            footerLayout.childAlignment = TextAnchor.MiddleRight;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;

            AddSpacer(footer.transform);
            CreateButton("ResetButton", footer.transform, "默认值", () => definition.Reset?.Invoke(inputs));
            CreateButton("CancelButton", footer.transform, "取消", Close);
            CreateButton("SaveButton", footer.transform, "保存", () =>
            {
                for (int i = 0; i < definition.Fields.Count && i < inputs.Count; i++)
                {
                    ApplyInput(definition.Fields[i], inputs[i]);
                }

                definition.Save?.Invoke();
                Close();
            });
        }

        public static void SetInput(KInputTextField input, float value)
        {
            if (input != null)
            {
                input.text = Format(value);
            }
        }

        private static void Close()
        {
            if (currentDialog != null)
            {
                Object.Destroy(currentDialog);
                currentDialog = null;
            }
        }

        private static KInputTextField AddField(Transform parent, ModConfigField field)
        {
            GameObject row = CreatePanel("FieldRow", parent, new Color(0.28f, 0.31f, 0.36f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 48f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            GameObject textColumn = new GameObject("TextColumn");
            textColumn.transform.SetParent(row.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 1f;
            textLayout.childControlHeight = true;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandHeight = false;

            TextMeshProUGUI name = CreateText("Label", textColumn.transform, field.Label, 12, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.96f, 0.97f, 0.98f, 1f);
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            TextMeshProUGUI desc = CreateText("Description", textColumn.transform, field.Description, 10, TextAlignmentOptions.MidlineLeft);
            desc.color = new Color(0.72f, 0.76f, 0.80f, 1f);
            desc.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            return CreateInput(row.transform, Format(field.Value));
        }

        private static void ApplyInput(ModConfigField field, KInputTextField input)
        {
            if (field == null || input == null)
            {
                return;
            }

            if (!float.TryParse(input.text, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) &&
                !float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return;
            }

            field.Apply?.Invoke(value);
        }

        private static KInputTextField CreateInput(Transform parent, string value)
        {
            GameObject inputObject = CreatePanel("Input", parent, new Color(0.12f, 0.14f, 0.18f, 1f));
            LayoutElement layout = inputObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 130f;
            layout.preferredHeight = 28f;

            TextMeshProUGUI text = CreateText("Text", inputObject.transform, value, 13, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform(), 8f, 3f);

            KInputTextField input = inputObject.AddComponent<KInputTextField>();
            input.textComponent = text;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.55f, 0.67f, 0.76f, 0.55f);
            input.text = value;
            return input;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, System.Action onClick)
        {
            GameObject buttonObject = CreatePanel(name, parent, new Color(0.50f, 0.25f, 0.39f, 1f));
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 92f;
            layout.preferredHeight = 30f;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = buttonObject.GetComponent<KImage>();
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 12, TextAlignmentOptions.Center);
            Stretch(text.rectTransform(), 4f, 0f);
            return buttonObject;
        }

        private static void AddSpacer(Transform parent)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            KImage image = panel.AddComponent<KImage>();
            image.type = Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = size;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
