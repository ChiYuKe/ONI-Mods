using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.API;
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
            Color? infoColor = null,
            Sprite titleIcon = null,
            Color? titleIconColor = null,
            IEnumerable<StorageNetworkStorageRowButton> extraButtons = null,
            Storage storageButtonContext = null)
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

            if (titleIcon != null)
            {
                CreateFoldoutTitleIcon(header.transform, titleIcon, titleIconColor ?? Color.white);
            }

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

            if (extraButtons != null)
            {
                foreach (StorageNetworkStorageRowButton descriptor in extraButtons.OrderBy(button => button.Order).ThenBy(button => button.Id))
                {
                    CreateStorageRowExtraButton(header, descriptor, storageButtonContext);
                }
            }

            TextMeshProUGUI amount = CreateText("Amount", header.transform, amountText, fontSize, TextAlignmentOptions.MidlineRight);
            amount.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = amountWidth;

            return header;
        }

        private void CreateStorageRowExtraButton(GameObject rowHeader, StorageNetworkStorageRowButton descriptor, Storage storage)
        {
            GameObject button = null;
            button = CreateGameButton("StorageRowAddonButton_" + descriptor.Id, rowHeader.transform, string.Empty, () =>
            {
                descriptor.OnClick?.Invoke(new StorageNetworkStorageRowButtonContext(storage, windowRect != null ? windowRect.gameObject : gameObject, rowHeader, button));
            });
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = descriptor.Width;
            layout.preferredHeight = 20f;

            if (!string.IsNullOrEmpty(descriptor.IconName) || !string.IsNullOrEmpty(descriptor.FallbackIconText))
            {
                AddButtonIconLabel(button.transform, descriptor.IconName, descriptor.FallbackIconText, descriptor.Label);
            }
            else
            {
                TextMeshProUGUI label = CreateText("Label", button.transform, descriptor.Label, 10, TextAlignmentOptions.Center);
                label.color = new Color(0.94f, 0.96f, 0.98f, 1f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                Stretch(label.rectTransform(), 4f, 0f);
            }

            KButton kButton = button.GetComponent<KButton>();
            if (kButton != null)
            {
                kButton.isInteractable = descriptor.IsEnabled;
            }

            KImage image = button.GetComponent<KImage>();
            if (image != null && !descriptor.IsEnabled)
            {
                image.ColorState = KImage.ColorSelector.Disabled;
            }

            if (!string.IsNullOrEmpty(descriptor.Tooltip))
            {
                ToolTip tooltip = button.AddComponent<ToolTip>();
                tooltip.toolTip = descriptor.Tooltip;
            }
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

    }
}
