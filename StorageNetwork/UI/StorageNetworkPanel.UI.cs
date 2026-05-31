using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {


        private GameObject CreateFoldoutHeader(
            Transform parent,
            bool expanded,
            string title,
            string amountText,
            Color backgroundColor,
            int fontSize,
            float amountWidth,
            System.Action onClick,
            string infoText = null,
            string actionText = null,
            System.Action actionClick = null,
            Color? infoColor = null)
        {
            GameObject header = CreateBox("Header", parent, backgroundColor);
            header.AddComponent<LayoutElement>().preferredHeight = 34f;

            UnityEngine.Object.DestroyImmediate(header.GetComponent<Image>());
            KImage headerImage = header.AddComponent<KImage>();
            headerImage.type = Image.Type.Sliced;
            headerImage.colorStyleSetting = CreateColorStyle(backgroundColor, Lighten(backgroundColor, 0.08f), Darken(backgroundColor, 0.08f));
            headerImage.ColorState = KImage.ColorSelector.Inactive;

            KButton button = header.AddComponent<KButton>();
            button.bgImage = headerImage;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 28, 0, 0);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            CreateFoldoutIcon(header.transform, expanded);

            TextMeshProUGUI name = CreateText("Name", header.transform, title, fontSize, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            name.fontStyle = FontStyles.Bold;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            if (!string.IsNullOrEmpty(infoText))
            {
                TextMeshProUGUI info = CreateText("Info", header.transform, infoText, Mathf.Max(9, fontSize - 2), TextAlignmentOptions.MidlineLeft);
                info.color = infoColor ?? new Color(0.34f, 0.36f, 0.34f, 1f);
                info.textWrappingMode = TextWrappingModes.NoWrap;
                info.overflowMode = TextOverflowModes.Ellipsis;
                info.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;
            }

            if (!string.IsNullOrEmpty(actionText) && actionClick != null)
            {
                GameObject actionButton = CreateGameButton("HeaderActionButton", header.transform, actionText, actionClick);
                LayoutElement actionLayout = actionButton.AddComponent<LayoutElement>();
                actionLayout.preferredWidth = 50f;
                actionLayout.preferredHeight = 20f;
            }

            TextMeshProUGUI amount = CreateText("Amount", header.transform, amountText, fontSize, TextAlignmentOptions.MidlineRight);
            amount.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = amountWidth;

            return header;
        }

        private static void AddVerticalContainer(GameObject gameObject, float spacing, int left, int right, int top, int bottom)
        {
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(left, right, top, bottom);
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
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

        private static void ApplyOniInputSlotStyle(Image image)
        {
            ApplyOniSprite(image, "web_box", Color.white, Image.Type.Sliced, preserveAspect: false);
        }

        private static void ApplyOniSliderFrame(Image image)
        {
            ApplyOniSprite(image, "build_menu_scrollbar_frame_horizontal", Color.white, Image.Type.Sliced, preserveAspect: false);
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

        private static void ApplyOniSliderFillCap(Image image)
        {
            ApplyOniSprite(image, "build_menu_scrollbar_inner_horizontal", OniPinkInactive(), Image.Type.Simple, preserveAspect: false);
        }

        private static void ApplyOniSliderHandle(Image image)
        {
            ApplyOniSprite(image, "game_speed_selected_med", Color.white, Image.Type.Simple, preserveAspect: true);
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

        private static GameObject CreateGameButton(string name, Transform parent, string text, System.Action onClick, Image.Type type = Image.Type.Sliced)
        {
            return CreateStyledButton(name, parent, text, onClick, KleiBlueStyle(), type);
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

        private static GameObject CreateIconOnlyButton(string name, Transform parent, Sprite icon, System.Action onClick)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage background = buttonObject.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = CreateColorStyle(
                new Color(0.17f, 0.19f, 0.25f, 1f),
                new Color(0.25f, 0.28f, 0.35f, 1f),
                new Color(0.11f, 0.12f, 0.16f, 1f));
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

        private static GameObject CreateTransparentIconButton(string name, Transform parent, Sprite icon, System.Action onClick)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage background = buttonObject.AddComponent<KImage>();
            background.type = Image.Type.Simple;
            background.colorStyleSetting = CreateColorStyle(
                new Color(0f, 0f, 0f, 0f),
                new Color(0.48f, 0.08f, 0.08f, 0.12f),
                new Color(0.48f, 0.08f, 0.08f, 0.22f));
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            Image iconImage = new GameObject("Icon").AddComponent<Image>();
            iconImage.transform.SetParent(buttonObject.transform, false);
            iconImage.sprite = icon;
            iconImage.color = new Color(0.55f, 0.04f, 0.04f, 1f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            Stretch(iconImage.rectTransform(), 3f, 3f);
            return buttonObject;
        }

        private static GameObject CreateCloseIconButton(string name, Transform parent, System.Action onClick)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage background = buttonObject.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = CreateColorStyle(
                new Color(0.17f, 0.19f, 0.25f, 1f),
                new Color(0.25f, 0.28f, 0.35f, 1f),
                new Color(0.11f, 0.12f, 0.16f, 1f));
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

        private static Scrollbar CreateScrollbar(Transform parent)
        {
            return CreateScrollbar(parent, 4f, 4f);
        }

        private static Scrollbar CreateScrollbar(Transform parent, float topInset, float bottomInset)
        {
            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-13f, bottomInset);
            scrollbarRect.offsetMax = new Vector2(-4f, -topInset);

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

        /// <summary>
        /// 使用原版建造菜单竖滚动条轨道纹理。
        /// </summary>
        private static void ApplyVerticalScrollbarFrame(Image image)
        {
            ApplyScrollbarSprite(image, "build_menu_scrollbar_frame", Color.white, new Color(0.09f, 0.1f, 0.12f, 1f));
        }

        /// <summary>
        /// 使用原版建造菜单竖滚动条滑块纹理。
        /// </summary>
        private static void ApplyVerticalScrollbarHandle(Image image)
        {
            ApplyScrollbarSprite(image, "build_menu_scrollbar_inner", new Color(0.6313726f, 0.6392157f, 0.682353f, 1f), new Color(0.6313726f, 0.6392157f, 0.682353f, 1f));
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

    }
}
