using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using StorageNetwork.API;
using StorageNetwork.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetworkTrading.UI
{
    public sealed partial class TradingPanel
    {
        private static GameObject CreateIconOnlyButton(string name, Transform parent, Sprite icon, System.Action onClick)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();
            KImage background = buttonObject.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = CreateColorStyle(new Color(0.17f, 0.19f, 0.25f, 1f), new Color(0.25f, 0.28f, 0.35f, 1f), new Color(0.11f, 0.12f, 0.16f, 1f));
            background.ColorState = KImage.ColorSelector.Inactive;
            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();
            Image iconImage = new GameObject("Icon").AddComponent<Image>();
            iconImage.transform.SetParent(buttonObject.transform, false);
            iconImage.sprite = icon;
            iconImage.color = new Color(0.48f, 0.08f, 0.08f, 1f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            Stretch(iconImage.rectTransform(), 4f, 4f);
            return buttonObject;
        }

        private static GameObject CreateGameButton(string name, Transform parent, string text, System.Action onClick, Image.Type type = Image.Type.Sliced)
        {
            return CreateStyledButton(name, parent, text, onClick, KleiBlueStyle(), type);
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

        private KInputTextField CreateFixedTextInput(Transform parent, string name, string value, float width, float height, int fontSize)
        {
            GameObject slot = new GameObject(name + "Slot");
            slot.transform.SetParent(parent, false);
            RectTransform slotRect = slot.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(width, height);
            LayoutElement slotLayout = slot.AddComponent<LayoutElement>();
            slotLayout.minWidth = width;
            slotLayout.preferredWidth = width;
            slotLayout.minHeight = height;
            slotLayout.preferredHeight = height;
            slotLayout.flexibleWidth = 1f;
            slotLayout.flexibleHeight = 0f;
            KInputTextField input = StorageNetworkInputBuilder.CreateKNumberInput(slot.transform, name, value ?? string.Empty, width, height, fontSize, TextAlignmentOptions.MidlineLeft, new Color(0.08f, 0.09f, 0.10f, 1f), "web_box", Color.white, new Color(0.08f, 0.09f, 0.10f, 1f), Vector2.one, false);
            input.characterLimit = 64;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.inputType = TMP_InputField.InputType.Standard;
            input.keyboardType = TouchScreenKeyboardType.Default;
            input.lineType = TMP_InputField.LineType.SingleLine;
            if (input.textComponent != null)
            {
                input.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
                input.textComponent.overflowMode = TextOverflowModes.Ellipsis;
            }

            RectTransform inputRect = input.gameObject.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = Vector2.zero;
            inputRect.sizeDelta = new Vector2(width, height);
            EnsureInputFieldDragReferences(input, width, height);
            input.gameObject.AddComponent<StorageNetworkTextInputGuard>().Configure(input, input.gameObject.GetComponent<Image>());
            return input;
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

        private static void SetTextIfChanged(TextMeshProUGUI text, string value)
        {
            value = StorageNetwork.Trading.TradingText.Render(value);
            if (text != null && text.text != value)
            {
                text.text = value;
            }
        }

        private static TextMeshProUGUI CreateAmountModalText(Transform parent, string text, int size, FontStyles style)
        {
            TextMeshProUGUI label = CreateText("AmountModalText", parent, text, size, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = label.text.Contains("\n") ? 40f : 22f;
            return label;
        }

        private static void AddFooterSpacer(Transform parent)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static GameObject CreatePlainImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<RectTransform>();
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Sliced;
            return gameObject;
        }

        private static void SetStretch(RectTransform rectTransform, float left, float right, float bottom, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = StorageNetwork.Trading.TradingText.Render(text);
            textComponent.fontSize = size;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static ColorStyleSetting KleiPinkStyle()
        {
            if (kleiPinkStyle == null)
            {
                kleiPinkStyle = ScriptableObject.CreateInstance<ColorStyleSetting>();
                kleiPinkStyle.activeColor = OniPinkActive();
                kleiPinkStyle.inactiveColor = OniPinkInactive();
                kleiPinkStyle.hoverColor = OniPinkHover();
                kleiPinkStyle.disabledColor = new Color(0.4156863f, 0.4117647f, 0.4f);
                kleiPinkStyle.disabledActiveColor = Color.clear;
                kleiPinkStyle.disabledhoverColor = new Color(0.5f, 0.5f, 0.5f);
            }

            return kleiPinkStyle;
        }

        private static ColorStyleSetting KleiBlueStyle()
        {
            if (kleiBlueStyle == null)
            {
                kleiBlueStyle = ScriptableObject.CreateInstance<ColorStyleSetting>();
                kleiBlueStyle.activeColor = StorageNetworkPanelPalette.BlueButtonPressed;
                kleiBlueStyle.inactiveColor = StorageNetworkPanelPalette.BlueButtonNormal;
                kleiBlueStyle.hoverColor = StorageNetworkPanelPalette.BlueButtonHover;
                kleiBlueStyle.disabledColor = new Color(0.4156863f, 0.4117647f, 0.4f);
                kleiBlueStyle.disabledActiveColor = new Color(0.625f, 0.6158088f, 0.5882353f);
                kleiBlueStyle.disabledhoverColor = new Color(0.5f, 0.4898898f, 0.4595588f);
            }

            return kleiBlueStyle;
        }

        private static GameObject CreateBox(string name, Transform parent, Color color)
        {
            GameObject box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.AddComponent<RectTransform>();
            Image image = box.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Sliced;
            return box;
        }

        private static void ApplyThinBoxSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSpriteByName("web_box");
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.fillCenter = true;
        }

        private static void Stretch(RectTransform rectTransform, float horizontal, float vertical)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(horizontal, vertical);
            rectTransform.offsetMax = new Vector2(-horizontal, -vertical);
        }

        private static void SetTopStretch(RectTransform rectTransform, float left, float right, float top, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -top - height);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static GameObject CreateCloseIconButton(string name, Transform parent, System.Action onClick)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();
            KImage background = buttonObject.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = CreateColorStyle(new Color(0.17f, 0.19f, 0.25f, 1f), new Color(0.25f, 0.28f, 0.35f, 1f), new Color(0.11f, 0.12f, 0.16f, 1f));
            background.ColorState = KImage.ColorSelector.Inactive;
            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();
            Image iconImage = new GameObject("Icon").AddComponent<Image>();
            iconImage.transform.SetParent(buttonObject.transform, false);
            iconImage.sprite = GetSpriteByName("cancel");
            iconImage.color = Color.white;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            Stretch(iconImage.rectTransform(), 5f, 4f);
            return buttonObject;
        }

        private RectTransform CreateScrollViewport(Transform parent, string name, out RectTransform content, float top, float left, float right, float bottom, float scrollbarInset)
        {
            GameObject viewport = CreateBox(name, parent, new Color(0.82f, 0.81f, 0.76f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), left, right + 14f, bottom, top);
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<ScrollWheelBlocker>();
            GameObject contentObject = new GameObject(name + "Content");
            contentObject.transform.SetParent(viewport.transform, false);
            content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return viewport.GetComponent<RectTransform>();
        }

        private static Scrollbar CreateScrollbar(Transform parent)
        {
            return CreateScrollbar(parent, 4f, 4f);
        }

        private static Scrollbar CreateScrollbar(Transform parent, float topInset, float bottomInset)
        {
            return CreateScrollbar(parent, topInset, bottomInset, 0f);
        }

        private static Scrollbar CreateScrollbar(Transform parent, float topInset, float bottomInset, float horizontalOffset)
        {
            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-13f + horizontalOffset, bottomInset);
            scrollbarRect.offsetMax = new Vector2(-4f + horizontalOffset, -topInset);
            Image background = scrollbarObject.AddComponent<Image>();
            ApplyVerticalScrollbarFrame(background);
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
            ApplyVerticalScrollbarHandle(handleImage);
            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.interactable = true;
            scrollbar.transition = Selectable.Transition.None;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }

        private static void WireScrollRect(GameObject viewport, RectTransform content, Scrollbar scrollbar, float sensitivity, bool allowHorizontal = false)
        {
            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content;
            ConfigureSmoothVerticalScroll(scrollRect, sensitivity);
            scrollRect.horizontal = allowHorizontal;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
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
            TextMeshProUGUI label = CreateText("AmountInputLabel", row.transform, Strings.Get("STRINGS.UI.STORAGE_NETWORK.AMOUNT_INPUT").ToString(), 12, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return CreateAmountInput(row.transform);
        }

        private static void FocusStorage(Storage storage)
        {
            FocusStorage(storage, 0f);
        }

        private static void FocusStorage(Storage storage, float screenOffsetRightPixels)
        {
            FocusObject(storage != null ? storage.gameObject : null, screenOffsetRightPixels);
        }

        private static void ApplyThinButtonSprite(KImage image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSpriteByName("web_button");
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.fillCenter = true;
        }

        private static ColorStyleSetting CreateColorStyle(Color normal, Color hover, Color pressed)
        {
            ColorStyleKey key = new ColorStyleKey(normal, hover, pressed);
            if (colorStyleCache.TryGetValue(key, out ColorStyleSetting cached) && cached != null)
            {
                return cached;
            }

            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = normal;
            style.hoverColor = hover;
            style.activeColor = pressed;
            style.disabledColor = Darken(normal, 0.08f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            colorStyleCache[key] = style;
            return style;
        }

        private static GameObject CreateStyledButton(string name, Transform parent, string text, System.Action onClick, ColorStyleSetting style, Image.Type type = Image.Type.Sliced)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();
            KImage image = buttonObject.AddComponent<KImage>();
            image.type = type;
            ApplyThinButtonSprite(image);
            image.colorStyleSetting = style;
            image.ColorState = KImage.ColorSelector.Inactive;
            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = image;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();
            if (!string.IsNullOrEmpty(text))
            {
                TextMeshProUGUI label = CreateText("Label", buttonObject.transform, text, 11, TextAlignmentOptions.Center);
                label.fontStyle = FontStyles.Normal;
                label.color = new Color(0.94f, 0.96f, 0.98f, 1f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                Stretch(label.rectTransform(), 4f, 0f);
            }

            return buttonObject;
        }

        /// <summary>
        /// TMP_InputField 在拖拽选择文本时会调用 MouseDragOutsideRect，内部强依赖 textViewport。
        /// 有些运行时动态创建的 KInputTextField 没有把 textViewport 正确挂上，拖拽时会在
        /// RectTransformUtility.ScreenPointToLocalPointInRectangle 里空引用。这里强制补齐引用。
        /// </summary>
        private static void EnsureInputFieldDragReferences(TMP_InputField input, float width, float height)
        {
            if (input == null)
            {
                return;
            }

            RectTransform inputRect = input.GetComponent<RectTransform>();
            if (inputRect == null)
            {
                inputRect = input.gameObject.AddComponent<RectTransform>();
            }

            if (input.textComponent == null)
            {
                input.textComponent = input.GetComponentInChildren<TMP_Text>(true);
            }

            if (input.textViewport == null)
            {
                RectTransform viewport = null;
                if (input.textComponent != null)
                {
                    viewport = input.textComponent.rectTransform.parent as RectTransform;
                }

                input.textViewport = viewport != null ? viewport : inputRect;
            }

            input.textViewport.anchorMin = Vector2.zero;
            input.textViewport.anchorMax = Vector2.one;
            input.textViewport.pivot = new Vector2(0.5f, 0.5f);
            input.textViewport.offsetMin = Vector2.one;
            input.textViewport.offsetMax = -Vector2.one;
            if (input.textComponent != null)
            {
                RectTransform textRect = input.textComponent.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.offsetMin = new Vector2(4f, 0f);
                textRect.offsetMax = new Vector2(-4f, 0f);
                input.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
                input.textComponent.overflowMode = TextOverflowModes.Overflow;
                input.textComponent.maxVisibleLines = 1;
                input.textComponent.raycastTarget = true;
            }
        }

        private static ColorStyleSetting kleiPinkStyle;
        private static Color OniPinkActive()
        {
            return StorageNetworkPanelPalette.PinkButtonPressed;
        }

        private static Color OniPinkInactive()
        {
            return StorageNetworkPanelPalette.PinkButtonNormal;
        }

        private static Color OniPinkHover()
        {
            return StorageNetworkPanelPalette.PinkButtonHover;
        }

        private static ColorStyleSetting kleiBlueStyle;
        private static Sprite GetSpriteByName(string spriteName)
        {
            Sprite sprite = StorageNetworkUiAccess.GetSprite(spriteName);
            if (sprite != null) return sprite;

            if (spriteCache == null)
            {
                spriteCache = new Dictionary<string, Sprite>();
                foreach (Sprite resourceSprite in Assets.instance?.SpriteAssets ?? new List<Sprite>())
                {
                    string name = resourceSprite != null ? resourceSprite.name : null;
                    if (!string.IsNullOrEmpty(name) && !spriteCache.ContainsKey(name))
                    {
                        spriteCache.Add(name, resourceSprite);
                    }
                }
            }

            return spriteCache.TryGetValue(spriteName, out sprite) ? sprite : null;
        }

        private static void ApplyVerticalScrollbarFrame(Image image)
        {
            ApplyScrollbarSprite(image, "build_menu_scrollbar_frame", Color.white, new Color(0.09f, 0.1f, 0.12f, 1f));
        }

        private static void ApplyVerticalScrollbarHandle(Image image)
        {
            ApplyScrollbarSprite(image, "build_menu_scrollbar_inner", new Color(0.6313726f, 0.6392157f, 0.682353f, 1f), new Color(0.6313726f, 0.6392157f, 0.682353f, 1f));
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

        private static void ApplyOniSliderFrame(Image image)
        {
            ApplyOniSprite(image, "build_menu_scrollbar_frame_horizontal", Color.white, Image.Type.Sliced, preserveAspect: false);
        }

        private static void ApplyOniSliderFillCap(Image image)
        {
            ApplyOniSprite(image, "build_menu_scrollbar_inner_horizontal", OniPinkInactive(), Image.Type.Simple, preserveAspect: false);
        }

        private static void ApplyOniSliderFill(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = OniPinkInactive();
        }

        private static void ApplyOniSliderHandle(Image image)
        {
            ApplyOniSprite(image, "game_speed_selected_med", Color.white, Image.Type.Simple, preserveAspect: true);
        }

        private static KInputTextField CreateAmountInput(Transform parent)
        {
            KInputTextField input = StorageNetworkInputBuilder.CreateKNumberInput(parent, "AmountInput", string.Empty, 150f, 24f, 13, TextAlignmentOptions.MidlineLeft, Color.white, new Color(0.08f, 0.09f, 0.10f, 1f), new Vector2(8f, 2f), true);
            input.gameObject.AddComponent<StorageNetworkNumberInputField>().Configure(input, 0f, float.MaxValue, false);
            return input;
        }

        private static void FocusObject(GameObject gameObject, float screenOffsetRightPixels)
        {
            if (gameObject == null || SelectTool.Instance == null)
            {
                return;
            }

            KSelectable selectable = gameObject.GetComponent<KSelectable>();
            if (selectable != null)
            {
                SelectTool.Instance.SelectAndFocus(GetOffsetFocusPosition(gameObject.transform.position, screenOffsetRightPixels), selectable, Vector3.zero);
            }
        }

        private readonly struct ColorStyleKey : System.IEquatable<ColorStyleKey>
        {
            private readonly Color32 normal;
            private readonly Color32 hover;
            private readonly Color32 pressed;
            public ColorStyleKey(Color normal, Color hover, Color pressed)
            {
                this.normal = normal;
                this.hover = hover;
                this.pressed = pressed;
            }

            public bool Equals(ColorStyleKey other)
            {
                return normal.Equals(other.normal) && hover.Equals(other.hover) && pressed.Equals(other.pressed);
            }

            public override bool Equals(object obj)
            {
                return obj is ColorStyleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = normal.GetHashCode();
                    hashCode = (hashCode * 397) ^ hover.GetHashCode();
                    hashCode = (hashCode * 397) ^ pressed.GetHashCode();
                    return hashCode;
                }
            }
        }

        private static readonly Dictionary<ColorStyleKey, ColorStyleSetting> colorStyleCache = new Dictionary<ColorStyleKey, ColorStyleSetting>();
        private static Color Darken(Color color, float amount)
        {
            return new Color(Mathf.Clamp01(color.r - amount), Mathf.Clamp01(color.g - amount), Mathf.Clamp01(color.b - amount), color.a);
        }

        private static void ApplyScrollbarSprite(Image image, string spriteName, Color spriteColor, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSpriteByName(spriteName);
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

        private static void ApplyOniSprite(Image image, string spriteName, Color color, Image.Type type, bool preserveAspect)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSpriteByName(spriteName);
            if (sprite == null)
            {
                image.color = color;
                return;
            }

            image.sprite = sprite;
            image.type = type;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
        }

        private static Vector3 GetOffsetFocusPosition(Vector3 targetPosition, float screenOffsetRightPixels)
        {
            if (screenOffsetRightPixels <= 0f || Camera.main == null || Screen.width <= 0)
            {
                return targetPosition;
            }

            float worldUnitsPerPixel = Camera.main.orthographic ? Camera.main.orthographicSize * 2f / Mathf.Max(1f, Screen.height) : 1f / Mathf.Max(1f, Screen.height);
            targetPosition.x -= screenOffsetRightPixels * worldUnitsPerPixel;
            return targetPosition;
        }
    }
}
