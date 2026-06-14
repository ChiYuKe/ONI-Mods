using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StorageNetwork.UI;

namespace StorageNetwork.ModConfig
{
    public static partial class ModConfigDialog
    {
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
    }
}
