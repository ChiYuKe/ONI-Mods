using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StorageNetwork.UI;

namespace StorageNetwork.ModConfig
{
    public static partial class ModConfigDialog
    {
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
    }
}
