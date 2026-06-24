using TMPro;
using StorageNetwork.API;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private readonly struct SettingsWindowParts
        {
            public SettingsWindowParts(GameObject root, TextMeshProUGUI title, RectTransform content)
            {
                Root = root;
                Title = title;
                Content = content;
            }

            public GameObject Root { get; }
            public TextMeshProUGUI Title { get; }
            public RectTransform Content { get; }
        }

        // 创建带标题栏、关闭按钮、滚动内容区的设置窗口；标题栏自动支持拖动。
        private SettingsWindowParts CreateDraggableSettingsWindow(
            string rootName,
            string titleName,
            System.Action closeAction,
            bool withScrollbar = false,
            string layoutKey = null)
        {
            GameObject root = CreateBox(rootName, transform, StorageNetworkPanelPalette.WindowBackground);
            root.AddComponent<ScrollWheelBlocker>();
            ApplyThinBoxSprite(root.GetComponent<Image>());
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 24f);
            rootRect.sizeDelta = new Vector2(760f, 560f);

            TextMeshProUGUI title = CreateDraggableSettingsHeader(root.transform, rootRect, titleName, closeAction, layoutKey);
            RectTransform content = CreateSettingsWindowViewport(root.transform, out ScrollRect scrollRect);

            if (withScrollbar)
            {
                Scrollbar scrollbar = CreateScrollbar(root.transform, 70f, 10f);
                scrollRect.verticalScrollbar = scrollbar;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                scrollRect.verticalScrollbarSpacing = 2f;
            }

            root.SetActive(false);
            return new SettingsWindowParts(root, title, content);
        }

        // 创建设置窗口的顶部栏；标题文字不接收射线，避免挡住拖动事件。
        private TextMeshProUGUI CreateDraggableSettingsHeader(Transform parent, RectTransform targetRect, string titleName, System.Action closeAction, string layoutKey)
        {
            GameObject header = CreateBox("Header", parent, StorageNetworkPanelPalette.SettingsHeaderBackground);
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);
            header.AddComponent<StorageNetworkWindowDrag>().Configure(targetRect, layoutKey);

            TextMeshProUGUI title = CreateText("Title", header.transform, string.Empty, 13, TextAlignmentOptions.TopLeft);
            title.name = titleName;
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = 2f;
            title.raycastTarget = false;
            Stretch(title.rectTransform(), 10f, 7f);

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, closeAction);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-4f, -4f);
            closeRect.sizeDelta = new Vector2(22f, 20f);

            return title;
        }

        // 创建设置窗口内部滚动区域，并返回可反复填充的内容容器。
        private RectTransform CreateSettingsWindowViewport(Transform parent, out ScrollRect scrollRect)
        {
            GameObject viewport = CreateBox("Viewport", parent, new Color(0.72f, 0.72f, 0.66f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 10f, 10f, 10f, 70f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            ConfigureSmoothVerticalScroll(scrollRect, 26f);
            viewport.AddComponent<ScrollWheelBlocker>();

            return contentRect;
        }

        // 保持设置窗口在屏幕内：首次打开使用默认位置，之后刷新只夹取位置，不覆盖用户拖动结果。
        private static bool KeepDraggableSettingsWindowOnScreen(GameObject root, bool positionInitialized, string layoutKey = null)
        {
            if (root == null)
            {
                return positionInitialized;
            }

            RectTransform panelRect = root.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return positionInitialized;
            }

            const float sideSafeArea = 18f;
            const float topSafeArea = 86f;
            const float bottomMargin = 24f;
            const float leftSafeArea = 18f;

            float width = Mathf.Clamp(760f, 620f, Mathf.Max(620f, Screen.width - sideSafeArea - leftSafeArea));
            float height = Mathf.Clamp(560f, 360f, Mathf.Max(360f, Screen.height - topSafeArea - bottomMargin));
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(width, height);
            if (!positionInitialized)
            {
                if (!StorageNetworkWindowDrag.TryApplyLayout(layoutKey, panelRect, new Vector2(620f, 360f), new Vector2(width, height)))
                {
                    panelRect.anchoredPosition = new Vector2(0f, bottomMargin);
                }

                StorageNetworkWindowDrag.ClampToScreen(panelRect);
                return true;
            }

            float minX = -Screen.width * 0.5f + leftSafeArea + width * 0.5f;
            float maxX = Screen.width * 0.5f - sideSafeArea - width * 0.5f;
            float minY = bottomMargin;
            float maxY = Mathf.Max(minY, Screen.height - topSafeArea - height);
            panelRect.anchoredPosition = new Vector2(
                Mathf.Clamp(panelRect.anchoredPosition.x, minX, maxX),
                Mathf.Clamp(panelRect.anchoredPosition.y, minY, maxY));
            StorageNetworkWindowDrag.ClampToScreen(panelRect);
            return true;
        }
    }
}
