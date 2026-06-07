using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StorageNetwork.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.ModConfig
{
    public sealed class ModConfigField
    {
        public string Label { get; set; }
        public string Description { get; set; }
        public float Value { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public bool Integer { get; set; }
        public bool IsBoolean { get; set; }
        public bool BoolValue { get; set; }
        public System.Action<float> Apply { get; set; }
        public System.Action<bool> ApplyBool { get; set; }
    }

    public sealed class ModConfigDialogDefinition
    {
        public string OverlayName { get; set; } = "ModConfigOverlay";
        public string Title { get; set; }
        public string Hint { get; set; }
        public List<ModConfigField> Fields { get; } = new List<ModConfigField>();
        public System.Action<List<ModConfigFieldControl>> Reset { get; set; }
        public System.Action Save { get; set; }
        public bool RestartRequired { get; set; } = true;
    }

    public sealed class ModConfigFieldControl
    {
        private readonly TMP_InputField input;
        private readonly Toggle toggle;
        private readonly TextMeshProUGUI toggleText;

        public ModConfigFieldControl(TMP_InputField input)
        {
            this.input = input;
        }

        public ModConfigFieldControl(Toggle toggle, TextMeshProUGUI toggleText)
        {
            this.toggle = toggle;
            this.toggleText = toggleText;
            RefreshToggleText();
        }

        public string NumberText
        {
            get { return input != null ? input.text : string.Empty; }
        }

        public bool BoolValue
        {
            get { return toggle != null && toggle.isOn; }
        }

        public void SetNumber(float value)
        {
            if (input == null)
            {
                return;
            }

            ModConfigDialog.ModConfigInputBinding binding = input.GetComponent<ModConfigDialog.ModConfigInputBinding>();
            if (binding != null)
            {
                binding.SetValue(value);
                return;
            }

            input.text = value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public void SetBool(bool value)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.isOn = value;
            RefreshToggleText();
        }

        private void RefreshToggleText()
        {
            if (toggleText != null)
            {
                toggleText.text = BoolValue
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIG_TOGGLE_ON)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIG_TOGGLE_OFF);
            }
        }
    }

    public static class ModConfigDialog
    {
        private static readonly Color DialogBackgroundColor = new Color(0.18f, 0.20f, 0.25f, 0.98f);
        private static readonly Color HeaderColor = new Color(0.5294118f, 0.2724914f, 0.4009516f, 1f);
        private static readonly Color ContentColor = new Color(0.24f, 0.27f, 0.33f, 1f);
        private static readonly Color RowColor = new Color(0.30f, 0.33f, 0.41f, 1f);
        private static readonly Color InputColor = Color.white;
        private static readonly Color InputTextColor = new Color(0.08f, 0.09f, 0.10f, 1f);
        private static readonly Color SliderFillColor = HeaderColor;
        private static readonly Vector2 BaseDialogSize = new Vector2(760f, 600f);
        private static readonly Vector2 MinDialogSize = new Vector2(620f, 440f);
        private const float ScreenMargin = 80f;
        private static GameObject currentDialog;

        public static void Show(ModConfigDialogDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            Close();
            List<ModConfigFieldControl> inputs = new List<ModConfigFieldControl>();
            Transform canvas = Global.Instance.globalCanvas.transform;

            GameObject overlay = new GameObject(definition.OverlayName);
            currentDialog = overlay;
            overlay.transform.SetParent(canvas, false);
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
            dialogRect.sizeDelta = GetDialogSize(canvas);

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
            CreateButton("ResetButton", footer.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.RESET_DEFAULTS), () => definition.Reset?.Invoke(inputs), false);
            CreateButton("CancelButton", footer.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.CANCEL), Close, false);
            CreateButton("SaveButton", footer.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                for (int i = 0; i < definition.Fields.Count && i < inputs.Count; i++)
                {
                    ApplyInput(definition.Fields[i], inputs[i]);
                }

                definition.Save?.Invoke();
                Close();
                if (definition.RestartRequired)
                {
                    ShowRestartRequiredDialog();
                }
            }, true);
        }

        public static void ResetRuntimeState()
        {
            Close();
        }

        private static void Close()
        {
            if (currentDialog != null)
            {
                Object.Destroy(currentDialog);
                currentDialog = null;
            }
        }

        private static ModConfigFieldControl AddField(Transform parent, ModConfigField field)
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

            return field.IsBoolean
                ? CreateBoolToggle(row.transform, field)
                : CreateInputWithSlider(row.transform, field);
        }

        private static void ApplyInput(ModConfigField field, ModConfigFieldControl input)
        {
            if (field == null || input == null)
            {
                return;
            }

            if (field.IsBoolean)
            {
                field.ApplyBool?.Invoke(input.BoolValue);
                return;
            }

            if (!float.TryParse(input.NumberText, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) &&
                !float.TryParse(input.NumberText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return;
            }

            field.Apply?.Invoke(ClampFieldValue(field, value));
        }

        private static ModConfigFieldControl CreateInputWithSlider(Transform parent, ModConfigField field)
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

            GameObject inputRow = new GameObject("InputRow");
            inputRow.transform.SetParent(controlColumn.transform, false);
            inputRow.AddComponent<RectTransform>();
            inputRow.AddComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup inputRowLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
            inputRowLayout.spacing = 6f;
            inputRowLayout.childAlignment = TextAnchor.MiddleRight;
            inputRowLayout.childControlWidth = true;
            inputRowLayout.childControlHeight = true;
            inputRowLayout.childForceExpandWidth = false;
            inputRowLayout.childForceExpandHeight = false;

            AddSpacer(inputRow.transform);
            TMP_InputField input = CreateInput(inputRow.transform, Format(field.Value));
            KSlider slider = CreateSlider(controlColumn.transform, field);
            ModConfigInputBinding binding = input.gameObject.AddComponent<ModConfigInputBinding>();
            binding.Configure(input, slider, field.Min, field.Max, field.Integer);
            binding.SetValue(field.Value);
            return new ModConfigFieldControl(input);
        }

        private static ModConfigFieldControl CreateBoolToggle(Transform parent, ModConfigField field)
        {
            GameObject controlColumn = new GameObject("ControlColumn");
            controlColumn.transform.SetParent(parent, false);
            controlColumn.AddComponent<RectTransform>();
            controlColumn.AddComponent<LayoutElement>().preferredWidth = 250f;

            HorizontalLayoutGroup layout = controlColumn.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddSpacer(controlColumn.transform);

            GameObject toggleObject = CreatePanel("Toggle", controlColumn.transform, field.BoolValue ? HeaderColor : new Color(0.18f, 0.21f, 0.28f, 1f));
            LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 92f;
            toggleLayout.preferredHeight = 28f;
            toggleLayout.minWidth = 92f;
            toggleLayout.minHeight = 28f;

            Toggle toggle = toggleObject.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = toggleObject.GetComponent<KImage>();
            toggle.isOn = field.BoolValue;

            GameObject check = CreatePanel("Checkmark", toggleObject.transform, new Color(0.88f, 0.96f, 1f, 1f));
            RectTransform checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.anchoredPosition = new Vector2(16f, 0f);
            checkRect.sizeDelta = new Vector2(14f, 14f);
            toggle.graphic = check.GetComponent<KImage>();

            TextMeshProUGUI text = CreateText("Label", toggleObject.transform, string.Empty, 12, TextAlignmentOptions.Center);
            Stretch(text.rectTransform(), 18f, 0f);

            ModConfigFieldControl control = new ModConfigFieldControl(toggle, text);
            toggle.onValueChanged.AddListener(_ =>
            {
                toggle.targetGraphic.color = toggle.isOn ? HeaderColor : new Color(0.18f, 0.21f, 0.28f, 1f);
                control.SetBool(toggle.isOn);
            });
            control.SetBool(field.BoolValue);
            return control;
        }

        private static TMP_InputField CreateInput(Transform parent, string value)
        {
            TMP_InputField input = ModConfigInputBuilder.CreateTmpNumberInput(
                parent,
                "Input",
                value,
                78f,
                24f,
                14,
                TextAlignmentOptions.Center,
                InputTextColor,
                InputColor,
                InputTextColor,
                Vector2.one);
            input.gameObject.AddComponent<StorageNetworkInputFieldEvents>().Configure(input);
            return input;
        }

        private static KSlider CreateSlider(Transform parent, ModConfigField field)
        {
            GameObject sliderObject = new GameObject("Slider");
            sliderObject.SetActive(false);
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
            fillAreaRect.sizeDelta = Vector2.zero;

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

            KSlider slider = sliderObject.AddComponent<KSlider>();
            slider.minValue = field.Min;
            slider.maxValue = field.Max;
            slider.wholeNumbers = field.Integer;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = null;
            sliderObject.SetActive(true);
            return slider;
        }

        private static void ShowRestartRequiredDialog()
        {
            GameObject parent = Global.Instance != null && Global.Instance.globalCanvas != null
                ? Global.Instance.globalCanvas.gameObject
                : null;
            GameObject dialogObject = Util.KInstantiateUI(ScreenPrefabs.Instance.ConfirmDialogScreen.gameObject, parent, false);
            ConfirmDialogScreen dialog = dialogObject != null ? dialogObject.GetComponent<ConfirmDialogScreen>() : null;
            if (dialog == null)
            {
                return;
            }

            dialog.PopupConfirmDialog(
                "要使这些选项完全生效，可能需要重新启动游戏。",
                () => App.instance.Restart(),
                () => { },
                null,
                null,
                "信息",
                "重启",
                "继续");
            dialogObject.SetActive(true);
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
                primary ? new Color(0.6176471f, 0.3315311f, 0.4745891f, 1f) : new Color(0.31f, 0.35f, 0.45f, 1f),
                primary ? new Color(0.7941176f, 0.4496107f, 0.6242238f, 1f) : new Color(0.18f, 0.21f, 0.28f, 1f));

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

            SmoothScrollEdgeBounce edgeBounce = scrollRect.gameObject.GetComponent<SmoothScrollEdgeBounce>();
            if (edgeBounce == null)
            {
                edgeBounce = scrollRect.gameObject.AddComponent<SmoothScrollEdgeBounce>();
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

        private static Vector2 GetDialogSize(Transform canvas)
        {
            float userScale = GetUserInterfaceScale(canvas);
            float canvasScale = Mathf.Max(0.01f, GetCanvasScale(canvas));
            Vector2 desired = BaseDialogSize * Mathf.Clamp(userScale, 0.85f, 1.25f);
            Vector2 max = new Vector2(
                Mathf.Max(MinDialogSize.x, (Screen.width - ScreenMargin) / canvasScale),
                Mathf.Max(MinDialogSize.y, (Screen.height - ScreenMargin) / canvasScale));
            return new Vector2(
                Mathf.Clamp(desired.x, MinDialogSize.x, max.x),
                Mathf.Clamp(desired.y, MinDialogSize.y, max.y));
        }

        private static float GetUserInterfaceScale(Transform canvas)
        {
            KCanvasScaler scaler = canvas != null ? canvas.GetComponentInParent<KCanvasScaler>() : null;
            if (scaler != null)
            {
                return scaler.GetUserScale();
            }

            return KPlayerPrefs.HasKey(KCanvasScaler.UIScalePrefKey)
                ? Mathf.Clamp(KPlayerPrefs.GetFloat(KCanvasScaler.UIScalePrefKey) / 100f, 0.75f, 2f)
                : 1f;
        }

        private static float GetCanvasScale(Transform canvas)
        {
            Canvas rootCanvas = canvas != null ? canvas.GetComponentInParent<Canvas>() : null;
            if (rootCanvas != null && rootCanvas.scaleFactor > 0f)
            {
                return rootCanvas.scaleFactor;
            }

            CanvasScaler scaler = canvas != null ? canvas.GetComponentInParent<CanvasScaler>() : null;
            return scaler != null && scaler.scaleFactor > 0f ? scaler.scaleFactor : 1f;
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

        internal sealed class ModConfigInputBinding : MonoBehaviour
        {
            private TMP_InputField input;
            private Slider slider;
            private float min;
            private float max;
            private bool integer;
            private bool updating;

            public void Configure(TMP_InputField inputField, Slider valueSlider, float minValue, float maxValue, bool integerValue)
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
                    input.onEndEdit.AddListener(OnInputEndEdit);
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

            private void OnInputEndEdit(string text)
            {
                if (updating || input == null)
                {
                    return;
                }

                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) &&
                    !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    value = min;
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
