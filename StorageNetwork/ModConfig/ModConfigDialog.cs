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
        public float Min { get; set; }
        public float Max { get; set; }
        public bool Integer { get; set; }
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
        private static readonly Color DialogBackgroundColor = new Color(0.18f, 0.20f, 0.25f, 0.98f);
        private static readonly Color HeaderColor = new Color(0.56f, 0.27f, 0.44f, 1f);
        private static readonly Color ContentColor = new Color(0.24f, 0.27f, 0.33f, 1f);
        private static readonly Color RowColor = new Color(0.30f, 0.33f, 0.41f, 1f);
        private static readonly Color InputColor = Color.white;
        private static readonly Color InputTextColor = new Color(0.08f, 0.09f, 0.10f, 1f);
        private static readonly Color SliderFillColor = new Color(0.65882355f, 0.2901961f, 0.47450984f, 1f);
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

            GameObject dialog = CreatePanel("Dialog", overlay.transform, DialogBackgroundColor);
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(760f, 600f);

            VerticalLayoutGroup layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject header = CreatePanel("Header", dialog.transform, HeaderColor);
            header.AddComponent<LayoutElement>().preferredHeight = 36f;
            TextMeshProUGUI title = CreateText("Title", header.transform, definition.Title, 18, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);

            RectTransform bodyContent = CreateScrollBody(dialog.transform);
            foreach (ModConfigField field in definition.Fields)
            {
                inputs.Add(AddField(bodyContent, field));
            }

            if (!string.IsNullOrEmpty(definition.Hint))
            {
                TextMeshProUGUI hint = CreateText("Hint", bodyContent, definition.Hint, 11, TextAlignmentOptions.MidlineLeft);
                hint.color = new Color(0.78f, 0.82f, 0.86f, 1f);
                hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            }

            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(dialog.transform, false);
            footer.AddComponent<RectTransform>();
            footer.AddComponent<LayoutElement>().preferredHeight = 40f;
            HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 10f;
            footerLayout.childAlignment = TextAnchor.MiddleRight;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;

            AddSpacer(footer.transform);
            CreateButton("ResetButton", footer.transform, "默认值", () => definition.Reset?.Invoke(inputs), false);
            CreateButton("CancelButton", footer.transform, "取消", Close, false);
            CreateButton("SaveButton", footer.transform, "确定", () =>
            {
                for (int i = 0; i < definition.Fields.Count && i < inputs.Count; i++)
                {
                    ApplyInput(definition.Fields[i], inputs[i]);
                }

                definition.Save?.Invoke();
                Close();
            }, true);
        }

        public static void SetInput(KInputTextField input, float value)
        {
            if (input != null)
            {
                ModConfigInputBinding binding = input.GetComponent<ModConfigInputBinding>();
                if (binding != null)
                {
                    binding.SetValue(value);
                    return;
                }

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
            GameObject row = CreatePanel("FieldRow", parent, RowColor);
            row.AddComponent<LayoutElement>().preferredHeight = 74f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.spacing = 14f;
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

            return CreateInputWithSlider(row.transform, field);
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

            field.Apply?.Invoke(ClampFieldValue(field, value));
        }

        private static KInputTextField CreateInputWithSlider(Transform parent, ModConfigField field)
        {
            GameObject controlColumn = new GameObject("ControlColumn");
            controlColumn.transform.SetParent(parent, false);
            controlColumn.AddComponent<RectTransform>();
            controlColumn.AddComponent<LayoutElement>().preferredWidth = 250f;

            VerticalLayoutGroup layout = controlColumn.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            KInputTextField input = CreateInput(controlColumn.transform, Format(field.Value));
            Slider slider = CreateSlider(controlColumn.transform, field);
            ModConfigInputBinding binding = input.gameObject.AddComponent<ModConfigInputBinding>();
            binding.Configure(input, slider, field.Min, field.Max, field.Integer);
            binding.SetValue(field.Value);
            return input;
        }

        private static KInputTextField CreateInput(Transform parent, string value)
        {
            GameObject inputObject = CreatePanel("Input", parent, InputColor);
            ApplyKleiSprite(inputObject.GetComponent<KImage>(), "web_box", InputColor, Image.Type.Sliced, false);
            LayoutElement layout = inputObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 130f;
            layout.preferredHeight = 32f;
            layout.flexibleWidth = 0f;

            TextMeshProUGUI text = CreateText("Text", inputObject.transform, value, 13, TextAlignmentOptions.MidlineLeft);
            text.color = InputTextColor;
            Stretch(text.rectTransform(), 8f, 3f);

            KInputTextField input = inputObject.AddComponent<KInputTextField>();
            input.textComponent = text;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
            input.selectionColor = new Color(0.65882355f, 0.80784315f, 1f, 0.7529412f);
            input.text = value;
            return input;
        }

        private static Slider CreateSlider(Transform parent, ModConfigField field)
        {
            GameObject sliderObject = new GameObject("Slider");
            sliderObject.transform.SetParent(parent, false);
            sliderObject.AddComponent<RectTransform>();
            LayoutElement sliderLayout = sliderObject.AddComponent<LayoutElement>();
            sliderLayout.preferredHeight = 32f;
            sliderLayout.minHeight = 32f;

            GameObject background = CreatePanel("Background", sliderObject.transform, Color.white);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            ApplyKleiSprite(background.GetComponent<KImage>(), "build_menu_scrollbar_frame_horizontal", Color.white, Image.Type.Sliced, false);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject fillStart = CreatePanel("Fill Start", fillArea.transform, SliderFillColor);
            RectTransform fillStartRect = fillStart.GetComponent<RectTransform>();
            fillStartRect.anchorMin = Vector2.zero;
            fillStartRect.anchorMax = new Vector2(0f, 1f);
            fillStartRect.anchoredPosition = Vector2.zero;
            fillStartRect.sizeDelta = new Vector2(12f, 0f);
            ApplyKleiSprite(fillStart.GetComponent<KImage>(), "build_menu_scrollbar_inner_horizontal", SliderFillColor, Image.Type.Simple, false);

            GameObject fill = CreatePanel("Fill", fillArea.transform, SliderFillColor);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            KImage fillImage = fill.GetComponent<KImage>();
            fillImage.sprite = null;
            fillImage.type = Image.Type.Simple;
            fillImage.color = SliderFillColor;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.anchoredPosition = Vector2.zero;
            handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject handle = CreatePanel("Handle", handleArea.transform, Color.white);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.anchoredPosition = new Vector2(0.9f, 0f);
            handleRect.sizeDelta = new Vector2(22.7f, -5.8f);
            ApplyKleiSprite(handle.GetComponent<KImage>(), "game_speed_selected_med", Color.white, Image.Type.Simple, true);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = field.Min;
            slider.maxValue = field.Max;
            slider.wholeNumbers = field.Integer;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = null;
            return slider;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, System.Action onClick, bool primary)
        {
            GameObject buttonObject = CreatePanel(name, parent, primary ? HeaderColor : new Color(0.24f, 0.27f, 0.35f, 1f));
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 92f;
            layout.preferredHeight = 34f;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = buttonObject.GetComponent<KImage>();
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();
            button.bgImage.colorStyleSetting = CreateColorStyle(
                primary ? HeaderColor : new Color(0.24f, 0.27f, 0.35f, 1f),
                primary ? new Color(0.66f, 0.36f, 0.54f, 1f) : new Color(0.31f, 0.35f, 0.45f, 1f),
                primary ? new Color(0.48f, 0.20f, 0.37f, 1f) : new Color(0.18f, 0.21f, 0.28f, 1f));

            TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 12, TextAlignmentOptions.Center);
            Stretch(text.rectTransform(), 4f, 0f);
            return buttonObject;
        }

        /// <summary>
        /// 创建类似 PLib OptionsDialog 的滚动内容区，避免配置项过多时撑出屏幕。
        /// </summary>
        private static RectTransform CreateScrollBody(Transform parent)
        {
            GameObject viewport = CreatePanel("ScrollViewport", parent, ContentColor);
            LayoutElement viewportLayout = viewport.AddComponent<LayoutElement>();
            viewportLayout.flexibleHeight = 1f;
            viewportLayout.minHeight = 360f;
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("ScrollContent");
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(8f, 0f);
            content.offsetMax = new Vector2(-8f, -8f);

            VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(viewport.transform);
            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content;
            ConfigureSmoothVerticalScroll(scroll, 26f);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 2f;
            return content;
        }

        private static void ConfigureSmoothVerticalScroll(ScrollRect scrollRect, float sensitivity)
        {
            if (scrollRect == null)
            {
                return;
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.10f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.08f;
            scrollRect.scrollSensitivity = sensitivity;

            StorageNetwork.UI.SmoothScrollEdgeBounce edgeBounce = scrollRect.gameObject.GetComponent<StorageNetwork.UI.SmoothScrollEdgeBounce>();
            if (edgeBounce == null)
            {
                edgeBounce = scrollRect.gameObject.AddComponent<StorageNetwork.UI.SmoothScrollEdgeBounce>();
            }

            edgeBounce.Configure(scrollRect);
        }

        private static Scrollbar CreateScrollbar(Transform parent)
        {
            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-13f, 4f);
            scrollbarRect.offsetMax = new Vector2(-4f, -4f);

            Image background = scrollbarObject.AddComponent<Image>();
            ApplyScrollbarSprite(background, "build_menu_scrollbar_frame", Color.white, new Color(0.09f, 0.1f, 0.12f, 1f));

            GameObject slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingRect = slidingArea.AddComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.anchoredPosition = Vector2.zero;
            slidingRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(16f, -10f);

            Image handleImage = handleObject.AddComponent<Image>();
            ApplyScrollbarSprite(handleImage, "build_menu_scrollbar_inner", new Color(0.6313726f, 0.6392157f, 0.682353f, 1f), new Color(0.6313726f, 0.6392157f, 0.682353f, 1f));

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.interactable = true;
            scrollbar.transition = Selectable.Transition.None;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
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
            ApplyKleiBoxSprite(image);
            return panel;
        }

        private static void ApplyKleiBoxSprite(KImage image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = Assets.GetSprite("web_box");
            if (sprite == null)
            {
                return;
            }

            ApplyKleiSprite(image, "web_box", image.color, Image.Type.Sliced, true, 2f);
        }

        private static void ApplyKleiSprite(Image image, string spriteName, Color color, Image.Type type, bool preserveAspect, float pixelsPerUnitMultiplier = 1f)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = Assets.GetSprite(spriteName);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = type;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        }

        private static void ApplyScrollbarSprite(Image image, string spriteName, Color spriteColor, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = Assets.GetSprite(spriteName);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = spriteColor;
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = fallbackColor;
        }

        private static ColorStyleSetting CreateColorStyle(Color normal, Color hover, Color pressed)
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = normal;
            style.hoverColor = hover;
            style.activeColor = pressed;
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
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

        private static float ClampFieldValue(ModConfigField field, float value)
        {
            if (field == null || float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            value = Mathf.Clamp(value, field.Min, field.Max);
            return field.Integer ? Mathf.Round(value) : value;
        }

        private sealed class ModConfigInputBinding : MonoBehaviour
        {
            private KInputTextField input;
            private Slider slider;
            private float min;
            private float max;
            private bool integer;
            private bool updating;

            public void Configure(KInputTextField inputField, Slider valueSlider, float minValue, float maxValue, bool integerValue)
            {
                input = inputField;
                slider = valueSlider;
                min = minValue;
                max = maxValue;
                integer = integerValue;

                if (slider != null)
                {
                    slider.minValue = min;
                    slider.maxValue = max;
                    slider.wholeNumbers = integer;
                    slider.onValueChanged.AddListener(OnSliderChanged);
                }

                if (input != null)
                {
                    input.OnValueChangesPaused += OnInputChanged;
                }
            }

            public void SetValue(float value)
            {
                value = Normalize(value);
                updating = true;
                if (input != null)
                {
                    input.text = Format(value);
                }

                if (slider != null)
                {
                    slider.value = value;
                }

                updating = false;
            }

            private void OnSliderChanged(float value)
            {
                if (!updating)
                {
                    SetValue(value);
                }
            }

            private void OnInputChanged()
            {
                if (updating || input == null)
                {
                    return;
                }

                if (!float.TryParse(input.text, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) &&
                    !float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    return;
                }

                SetValue(value);
            }

            private float Normalize(float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    value = min;
                }

                value = Mathf.Clamp(value, min, max);
                return integer ? Mathf.Round(value) : value;
            }
        }
    }
}
