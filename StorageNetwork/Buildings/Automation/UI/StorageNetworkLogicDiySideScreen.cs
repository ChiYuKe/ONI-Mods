using StorageNetwork.Components;
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
        private TextMeshProUGUI statusText;
        private float refreshTimer;

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
            rootLayout.minHeight = 82f;
            rootLayout.preferredHeight = 96f;
            rootLayout.flexibleWidth = 1f;

            VerticalLayoutGroup layout = contentRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            statusText = CreateText(
                contentRoot.transform,
                string.Empty,
                10f,
                FontStyles.Bold,
                new Color(0.30f, 0.31f, 0.30f, 1f));
            statusText.textWrappingMode = TextWrappingModes.Normal;
            statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            KButton configureButton = CreateButton(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OPEN_SETTINGS),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OPEN_SETTINGS_TOOLTIP),
                () =>
                {
                    if (targetLogic != null)
                    {
                        StorageNetworkLogicDiyConfigPanel.Show(targetLogic);
                    }
                });
            LayoutElement buttonLayout = configureButton.gameObject.GetComponent<LayoutElement>() ?? configureButton.gameObject.AddComponent<LayoutElement>();
            buttonLayout.minHeight = 32f;
            buttonLayout.preferredHeight = 32f;
        }

        private void EnsureRootLayout()
        {
            if (GetComponent<RectTransform>() == null)
            {
                gameObject.AddComponent<RectTransform>();
            }

            LayoutElement layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 90f;
            layoutElement.preferredHeight = 104f;
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

        private KButton CreateButton(string label, string tooltipText, System.Action onClick)
        {
            GameObject row = new GameObject("ConfigureButton");
            row.transform.SetParent(contentRoot.transform, false);
            row.AddComponent<RectTransform>();

            KImage background = row.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            background.colorStyleSetting = CreateButtonStyle();
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton button = row.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += onClick;

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 32f;
            rowLayout.preferredHeight = 32f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 3, 3);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI text = CreateText(row.transform, label, 10f, FontStyles.Bold, new Color(0.94f, 0.96f, 0.98f, 1f));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            ToolTip tooltip = row.AddComponent<ToolTip>();
            tooltip.SetSimpleTooltip(tooltipText ?? string.Empty);
            return button;
        }

        private void Update()
        {
            if (targetLogic == null)
            {
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = 0.25f;
            Refresh();
        }

        private void Refresh()
        {
            if (targetLogic == null)
            {
                return;
            }

            bool fourChannel = targetLogic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel;
            if (statusText != null)
            {
                statusText.text = string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CURRENT_MODE),
                    fourChannel
                        ? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL)
                        : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL));
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
            style.inactiveColor = new Color(0.17f, 0.19f, 0.25f, 1f);
            style.hoverColor = new Color(0.25f, 0.28f, 0.35f, 1f);
            style.activeColor = new Color(0.11f, 0.12f, 0.16f, 1f);
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
        }

        internal static ColorStyleSetting CreateSelectedStyle()
        {
            ColorStyleSetting style = CreateButtonStyle();
            style.inactiveColor = new Color(0.12f, 0.42f, 0.30f, 1f);
            style.hoverColor = new Color(0.17f, 0.53f, 0.38f, 1f);
            style.activeColor = new Color(0.09f, 0.31f, 0.23f, 1f);
            return style;
        }
    }
}
