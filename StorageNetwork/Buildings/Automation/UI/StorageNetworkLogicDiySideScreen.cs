using StorageNetwork.Components;
using StorageNetwork.API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkLogicDiySideScreen : SideScreenContent
    {
        private StorageNetworkLogicDiy targetLogic;
        private GameObject contentRoot;
        private TextMeshProUGUI openButtonText;

        public StorageNetworkLogicDiySideScreen()
        {
            titleKey = "STRINGS.UI.STORAGE_NETWORK.LOGIC_DIY_SIDE_SCREEN_TITLE";
        }

        public override string GetTitle()
        {
            return Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SIDE_SCREEN_TITLE);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            BuildContent();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<StorageNetworkLogicDiy>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            base.SetTarget(target);
            targetLogic = target != null ? target.GetComponent<StorageNetworkLogicDiy>() : null;
            BuildContent();
            Refresh();
        }

        public override void ClearTarget()
        {
            targetLogic = null;
            base.ClearTarget();
        }

        public override int GetSideScreenSortOrder()
        {
            return 24;
        }

        private void BuildContent()
        {
            if (contentRoot != null)
            {
                return;
            }

            EnsureRootLayout();
            Transform parent = ContentContainer != null ? ContentContainer.transform : transform;
            contentRoot = new GameObject("LogicDiyOutputMode");
            contentRoot.transform.SetParent(parent, false);
            contentRoot.AddComponent<RectTransform>();

            LayoutElement rootLayout = contentRoot.AddComponent<LayoutElement>();
            rootLayout.minHeight = 32f;
            rootLayout.preferredHeight = 36f;
            rootLayout.flexibleWidth = 1f;

            VerticalLayoutGroup layout = contentRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.spacing = 0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            KButton openButton = CreateSmallButton(contentRoot.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OPEN_SETTINGS), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OPEN_SETTINGS_TOOLTIP), () =>
            {
                if (targetLogic != null)
                {
                    StorageNetworkPanel.ShowLogicDiyOutputModePicker(targetLogic);
                }
            }, out openButtonText);
            LayoutElement openButtonLayout = openButton.gameObject.AddComponent<LayoutElement>();
            openButtonLayout.minHeight = 32f;
            openButtonLayout.preferredHeight = 32f;
        }

        private void EnsureRootLayout()
        {
            if (GetComponent<RectTransform>() == null)
            {
                gameObject.AddComponent<RectTransform>();
            }

            LayoutElement layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 36f;
            layoutElement.preferredHeight = 42f;
            layoutElement.flexibleWidth = 1f;

            if (GetComponent<VerticalLayoutGroup>() == null)
            {
                VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
        }

        private KButton CreateSmallButton(Transform parent, string label, string tooltipText, System.Action onClick, out TextMeshProUGUI labelText)
        {
            GameObject buttonObject = new GameObject("Action");
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage image = buttonObject.AddComponent<KImage>();
            image.type = Image.Type.Sliced;
            ApplyButtonSprite(image);
            image.colorStyleSetting = CreateButtonStyle();
            image.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = image;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            labelText = CreateText(buttonObject.transform, label, 10f, FontStyles.Normal, Color.white);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(labelText.rectTransform(), 4f, 0f);

            ToolTip tooltip = buttonObject.AddComponent<ToolTip>();
            tooltip.SetSimpleTooltip(tooltipText ?? string.Empty);
            return button;
        }

        private void Refresh()
        {
            if (targetLogic == null)
            {
                return;
            }

            if (openButtonText != null)
            {
                openButtonText.text = Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OPEN_SETTINGS);
            }

        }

        private static TextMeshProUGUI CreateText(Transform parent, string textValue, float fontSize, FontStyles style, Color color)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = textValue ?? string.Empty;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        internal static ColorStyleSetting CreateButtonStyle()
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = StorageNetworkPanelPalette.BlueButtonNormal;
            style.hoverColor = StorageNetworkPanelPalette.BlueButtonHover;
            style.activeColor = StorageNetworkPanelPalette.BlueButtonPressed;
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
        }

        internal static ColorStyleSetting CreateSelectedStyle()
        {
            ColorStyleSetting style = CreateButtonStyle();
            style.inactiveColor = StorageNetworkPanelPalette.PinkButtonNormal;
            style.hoverColor = StorageNetworkPanelPalette.PinkButtonHover;
            style.activeColor = StorageNetworkPanelPalette.PinkButtonPressed;
            return style;
        }

        internal static void ApplyButtonSprite(KImage image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSprite("web_button");
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.fillCenter = true;
        }

        internal static void ApplyBoxSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSprite("web_box");
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.fillCenter = true;
        }

        private static Sprite GetSprite(string spriteName)
        {
            Sprite sprite = Assets.GetSprite(spriteName);
            return sprite != null ? sprite : StorageNetwork.Core.StorageNetworkSpriteLoader.GetSprite(spriteName);
        }

        internal static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}
