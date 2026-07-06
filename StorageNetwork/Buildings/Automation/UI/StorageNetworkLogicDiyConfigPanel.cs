using StorageNetwork.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkLogicDiyConfigPanel : KScreen, IInputHandler
    {
        private static StorageNetworkLogicDiyConfigPanel instance;

        private StorageNetworkLogicDiy targetLogic;
        private TextMeshProUGUI modeText;
        private TextMeshProUGUI descText;
        private KButton singleButton;
        private KButton fourButton;

        public static void Show(StorageNetworkLogicDiy target)
        {
            if (target == null)
            {
                return;
            }

            if (instance == null || instance.gameObject == null)
            {
                instance = Create();
            }

            instance.SetTarget(target);
            instance.gameObject.SetActive(true);
            if (!instance.IsActive())
            {
                instance.Activate();
            }
        }

        private static StorageNetworkLogicDiyConfigPanel Create()
        {
            Transform parent = GameScreenManager.Instance?.ssOverlayCanvas?.transform;
            GameObject root = new GameObject("StorageNetworkLogicDiyConfigPanel");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.10f);
            blocker.raycastTarget = true;

            StorageNetworkLogicDiyConfigPanel panel = root.AddComponent<StorageNetworkLogicDiyConfigPanel>();
            panel.activateOnSpawn = false;
            panel.ConsumeMouseScroll = true;
            panel.Build();
            root.SetActive(false);
            return panel;
        }

        private void SetTarget(StorageNetworkLogicDiy target)
        {
            targetLogic = target;
            Refresh();
        }

        private void Build()
        {
            GameObject window = CreateBox("Window", transform, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(360f, 260f);

            VerticalLayoutGroup windowLayout = window.AddComponent<VerticalLayoutGroup>();
            windowLayout.spacing = 0f;
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            GameObject header = CreateBox("Header", window.transform, new Color(0.50f, 0.24f, 0.38f, 1f));
            LayoutElement headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.minHeight = 36f;
            headerLayout.preferredHeight = 36f;

            HorizontalLayoutGroup headerGroup = header.AddComponent<HorizontalLayoutGroup>();
            headerGroup.padding = new RectOffset(10, 6, 4, 4);
            headerGroup.spacing = 6f;
            headerGroup.childAlignment = TextAnchor.MiddleCenter;
            headerGroup.childControlWidth = true;
            headerGroup.childControlHeight = true;
            headerGroup.childForceExpandWidth = false;
            headerGroup.childForceExpandHeight = true;

            TextMeshProUGUI title = CreateText(header.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CONFIG_PANEL_TITLE), 13f, FontStyles.Bold, Color.white);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            KButton closeButton = CreateButton(header.transform, "X", Loc.Get(Loc.UI.STORAGE_NETWORK.CANCEL), Close);
            LayoutElement closeLayout = closeButton.gameObject.GetComponent<LayoutElement>() ?? closeButton.gameObject.AddComponent<LayoutElement>();
            closeLayout.minWidth = 28f;
            closeLayout.preferredWidth = 28f;

            GameObject body = new GameObject("Body");
            body.transform.SetParent(window.transform, false);
            body.AddComponent<RectTransform>();
            body.AddComponent<LayoutElement>().flexibleHeight = 1f;

            VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(12, 12, 12, 12);
            bodyLayout.spacing = 8f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            modeText = CreateText(body.transform, string.Empty, 12f, FontStyles.Bold, new Color(0.20f, 0.22f, 0.24f, 1f));
            modeText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            singleButton = CreateModeButton(
                body.transform,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL_TOOLTIP),
                () => SetMode(StorageNetworkLogicDiy.ChannelMode.SingleChannel));

            fourButton = CreateModeButton(
                body.transform,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL_TOOLTIP),
                () => SetMode(StorageNetworkLogicDiy.ChannelMode.FourChannel));

            descText = CreateText(body.transform, string.Empty, 10f, FontStyles.Normal, new Color(0.27f, 0.29f, 0.30f, 1f));
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        }

        private KButton CreateModeButton(Transform parent, string label, string tooltipText, System.Action onClick)
        {
            KButton button = CreateButton(parent, label, tooltipText, onClick);
            LayoutElement layout = button.gameObject.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 40f;
            layout.preferredHeight = 40f;
            return button;
        }

        private void SetMode(StorageNetworkLogicDiy.ChannelMode mode)
        {
            targetLogic?.SetOutputMode(mode);
            Refresh();
        }

        private void Refresh()
        {
            if (targetLogic == null)
            {
                return;
            }

            bool fourChannel = targetLogic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel;
            string modeName = fourChannel
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL);

            if (modeText != null)
            {
                modeText.text = string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CURRENT_MODE), modeName);
            }

            if (descText != null)
            {
                descText.text = fourChannel
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL_DESC)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL_DESC);
            }

            SetSelected(singleButton, !fourChannel);
            SetSelected(fourButton, fourChannel);
        }

        private static void SetSelected(KButton button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            KImage image = button.bgImage ?? button.GetComponent<KImage>();
            if (image != null)
            {
                image.colorStyleSetting = selected
                    ? StorageNetworkLogicDiySideScreen.CreateSelectedStyle()
                    : StorageNetworkLogicDiySideScreen.CreateButtonStyle();
                image.ColorState = KImage.ColorSelector.Inactive;
            }
        }

        private void Close()
        {
            if (IsActive())
            {
                Deactivate();
            }

            gameObject.SetActive(false);
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(global::Action.Escape))
            {
                Close();
                return;
            }

            e.Consumed = true;
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

        private static KButton CreateButton(Transform parent, string label, string tooltipText, System.Action onClick)
        {
            GameObject buttonObject = new GameObject("Button");
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage background = buttonObject.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            background.colorStyleSetting = StorageNetworkLogicDiySideScreen.CreateButtonStyle();
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            TextMeshProUGUI text = CreateText(buttonObject.transform, label, 11f, FontStyles.Bold, Color.white);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(text.rectTransform(), 4f, 4f);

            ToolTip tooltip = buttonObject.AddComponent<ToolTip>();
            tooltip.SetSimpleTooltip(tooltipText ?? string.Empty);
            return button;
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

        private static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }
    }
}
