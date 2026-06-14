using System.Globalization;
using StorageNetwork.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private void ShowAmountDialog(
            string title,
            string itemName,
            string details,
            float maxAmount,
            System.Action<float> onConfirm,
            string targetButtonText = null,
            System.Action targetButtonAction = null)
        {
            CloseModal();
            maxAmount = Mathf.Max(0f, maxAmount);
            modalRoot = CreateAmountModalFrame(title, 520f, string.IsNullOrEmpty(targetButtonText) ? 318f : 348f, out GameObject body);

            GameObject infoCard = CreatePlainImage("AmountInfoCard", body.transform, new Color(0.76f, 0.76f, 0.70f, 1f));
            LayoutElement infoLayout = infoCard.AddComponent<LayoutElement>();
            infoLayout.minHeight = string.IsNullOrEmpty(targetButtonText) ? 66f : 94f;
            infoLayout.preferredHeight = -1f;
            VerticalLayoutGroup infoGroup = infoCard.AddComponent<VerticalLayoutGroup>();
            infoGroup.padding = new RectOffset(10, 10, 8, 8);
            infoGroup.spacing = 3f;
            infoGroup.childControlWidth = true;
            infoGroup.childControlHeight = true;
            infoGroup.childForceExpandWidth = true;
            infoGroup.childForceExpandHeight = false;

            TextMeshProUGUI nameText = CreateAmountModalText(infoCard.transform, itemName, 15, FontStyles.Bold);
            nameText.color = new Color(0.18f, 0.19f, 0.18f, 1f);
            TextMeshProUGUI detailsText = CreateAmountModalText(infoCard.transform, details, 11, FontStyles.Normal);
            detailsText.color = new Color(0.36f, 0.39f, 0.40f, 1f);

            if (!string.IsNullOrEmpty(targetButtonText) && targetButtonAction != null)
            {
                GameObject targetRow = AddAmountModalRow(infoCard.transform, 6f, 28f);
                TextMeshProUGUI targetLabel = CreateAmountModalText(targetRow.transform, targetButtonText, 11, FontStyles.Bold);
                targetLabel.color = new Color(0.20f, 0.21f, 0.20f, 1f);
                targetLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                GameObject targetButton = AddAmountModalButton(targetRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CHANGE_TARGET), 126f, targetButtonAction);
                targetButton.GetComponent<KImage>().colorStyleSetting = KleiPinkStyle();
                AddFooterSpacer(targetRow.transform);
            }

            float currentAmount = maxAmount;
            bool updating = false;
            TextMeshProUGUI valueLabel = CreateAmountModalText(body.transform, string.Empty, 13, FontStyles.Bold);
            valueLabel.color = new Color(0.18f, 0.19f, 0.18f, 1f);
            KSlider slider = CreateAmountSlider(body.transform, maxAmount);
            KInputTextField input = CreateAmountInputRow(body.transform);
            StorageNetworkNumberInputField numberInput = input.GetComponent<StorageNetworkNumberInputField>();
            numberInput?.Configure(input, 0f, maxAmount, false);

            System.Action<float> setAmount = value =>
            {
                currentAmount = Mathf.Clamp(value, 0f, maxAmount);
                valueLabel.text = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.AMOUNT_LABEL), GameUtil.GetFormattedMass(currentAmount));
                if (!updating)
                {
                    updating = true;
                    slider.value = currentAmount;
                    if (numberInput != null)
                    {
                        numberInput.SetAmount(currentAmount);
                    }
                    else
                    {
                        input.text = FormatAmount(currentAmount);
                    }

                    updating = false;
                }
            };

            slider.onValueChanged.AddListener(value =>
            {
                if (!updating)
                {
                    setAmount(value);
                }
            });

            if (numberInput != null)
            {
                numberInput.onEndEdit += () =>
                {
                    setAmount(numberInput.currentValue);
                };
            }

            setAmount(currentAmount);

            GameObject shortcutRow = AddAmountModalRow(body.transform, 6f, 28f);
            AddFooterSpacer(shortcutRow.transform);
            AddAmountModalButton(shortcutRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL), 86f, () => setAmount(maxAmount));

            GameObject footer = AddAmountModalRow(body.transform, 8f, 30f);
            AddFooterSpacer(footer.transform);
            AddAmountModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), 92f, CloseModal);
            GameObject confirmButton = AddAmountModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), 100f, () =>
            {
                float finalAmount = Mathf.Clamp(currentAmount, 0f, maxAmount);
                CloseModal();
                if (finalAmount > 0f)
                {
                    onConfirm?.Invoke(finalAmount);
                }
            });
            confirmButton.GetComponent<KImage>().colorStyleSetting = KleiPinkStyle();
        }

        private GameObject CreateAmountModalFrame(string title, float width, float height, out GameObject body)
        {
            GameObject overlay = new GameObject("ModalOverlay");
            overlay.transform.SetParent(transform, false);
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            Stretch(overlayRect, 0f, 0f);
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.34f);

            GameObject dialog = CreateBox("AmountDialog", overlay.transform, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(dialog.GetComponent<Image>());
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(width, height);

            GameObject header = CreateBox("Header", dialog.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 40f);
            TextMeshProUGUI titleText = CreateText("Title", header.transform, title, 14, TextAlignmentOptions.MidlineLeft);
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            Stretch(titleText.rectTransform(), 12f, 0f);

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, CloseModal);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-5f, 0f);
            closeRect.sizeDelta = new Vector2(24f, 22f);

            body = CreateBox("AmountBody", dialog.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 10f, 10f, 10f, 48f);
            VerticalLayoutGroup layout = body.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return overlay;
        }

        private static TextMeshProUGUI CreateAmountModalText(Transform parent, string text, int size, FontStyles style)
        {
            TextMeshProUGUI label = CreateText("AmountModalText", parent, text, size, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = text.Contains("\n") ? 40f : 22f;
            return label;
        }

        private static GameObject AddAmountModalRow(Transform parent, float spacing, float height)
        {
            GameObject row = new GameObject("AmountModalRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = height;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static GameObject AddAmountModalButton(Transform parent, string text, float width, System.Action onClick)
        {
            GameObject button = CreateGameButton("AmountModalButton", parent, text, onClick);
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.preferredHeight = 24f;
            layout.minHeight = 24f;
            return button;
        }

        private static KSlider CreateAmountSlider(Transform parent, float maxAmount)
        {
            GameObject sliderObject = new GameObject("AmountSlider");
            sliderObject.SetActive(false);
            sliderObject.transform.SetParent(parent, false);
            sliderObject.AddComponent<RectTransform>();
            sliderObject.AddComponent<LayoutElement>().preferredHeight = 32f;

            GameObject background = CreatePlainImage("Background", sliderObject.transform, Color.white);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            ApplyOniSliderFrame(background.GetComponent<Image>());

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = Vector2.zero;

            GameObject fillStart = CreatePlainImage("Fill Start", fillArea.transform, Color.white);
            RectTransform fillStartRect = fillStart.GetComponent<RectTransform>();
            fillStartRect.anchorMin = Vector2.zero;
            fillStartRect.anchorMax = new Vector2(0f, 1f);
            fillStartRect.anchoredPosition = Vector2.zero;
            fillStartRect.sizeDelta = new Vector2(12f, 0f);
            ApplyOniSliderFillCap(fillStart.GetComponent<Image>());

            GameObject fill = CreatePlainImage("Fill", fillArea.transform, Color.white);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            ApplyOniSliderFill(fill.GetComponent<Image>());

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.anchoredPosition = Vector2.zero;
            handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject handle = CreatePlainImage("Handle", handleArea.transform, Color.white);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.anchoredPosition = new Vector2(0.9f, 0f);
            handleRect.sizeDelta = new Vector2(22.7f, -5.8f);
            ApplyOniSliderHandle(handle.GetComponent<Image>());

            KSlider slider = sliderObject.AddComponent<KSlider>();
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(0.001f, maxAmount);
            slider.value = maxAmount;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = null;
            slider.direction = Slider.Direction.LeftToRight;
            sliderObject.SetActive(true);
            return slider;
        }

        private static KInputTextField CreateAmountInputRow(Transform parent)
        {
            GameObject row = AddAmountModalRow(parent, 8f, 30f);
            TextMeshProUGUI label = CreateText("AmountInputLabel", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.AMOUNT_INPUT), 12, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return CreateAmountInput(row.transform);
        }

        private static KInputTextField CreateAmountInput(Transform parent)
        {
            KInputTextField input = StorageNetworkInputBuilder.CreateKNumberInput(
                parent,
                "AmountInput",
                string.Empty,
                150f,
                24f,
                13,
                TextAlignmentOptions.MidlineLeft,
                Color.white,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                new Vector2(8f, 2f),
                true);
            input.gameObject.AddComponent<StorageNetworkNumberInputField>().Configure(input, 0f, float.MaxValue, false);
            return input;
        }

        private static string FormatAmount(float amount)
        {
            return amount.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryParseAmount(string value, out float amount)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out amount) ||
                   float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
        }
    }
}
