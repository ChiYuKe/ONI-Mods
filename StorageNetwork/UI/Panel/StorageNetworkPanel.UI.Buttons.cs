using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
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
    }
}
