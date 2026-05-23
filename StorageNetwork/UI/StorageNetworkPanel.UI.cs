using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
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
            string actionText = null,
            System.Action actionClick = null)
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

        private static GameObject CreateGameButton(string name, Transform parent, string text, System.Action onClick)
        {
            return CreateStyledButton(name, parent, text, onClick, KleiBlueStyle());
        }

        private static GameObject CreateStyledButton(string name, Transform parent, string text, System.Action onClick, ColorStyleSetting style)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage image = buttonObject.AddComponent<KImage>();
            image.type = Image.Type.Sliced;
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

        private static Scrollbar CreateScrollbar(Transform parent)
        {
            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-18f, 8f);
            scrollbarRect.offsetMax = new Vector2(-8f, -8f);

            Image background = scrollbarObject.AddComponent<Image>();
            background.color = new Color(0.48f, 0.49f, 0.50f, 1f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(0.22f, 0.25f, 0.34f, 1f);

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }
    }
}
