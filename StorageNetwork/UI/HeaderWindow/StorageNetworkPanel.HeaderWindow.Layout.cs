using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private static bool IsCompactOrderWindow()
        {
            float canvasWidth = GetCanvasWidth();
            return canvasWidth < 1320f;
        }

        private static bool ShouldInlineOrderTracking()
        {
            return GetCanvasWidth() < 1960f;
        }

        private static float GetCanvasWidth()
        {
            Canvas canvas = Global.Instance != null && Global.Instance.globalCanvas != null
                ? Global.Instance.globalCanvas.GetComponentInParent<Canvas>()
                : null;
            float scaleFactor = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            return Screen.width / Mathf.Max(0.01f, scaleFactor);
        }

        private void ApplyOrderWindowRootLayout(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(1760f, 980f);
            rectTransform.anchoredPosition = Vector2.zero;
            StorageNetworkWindowDrag.TryApplyLayout("orderCenter", rectTransform, new Vector2(1760f, 980f), new Vector2(1760f, 980f));
            StorageNetworkWindowDrag.ClampToScreen(rectTransform);
        }

        private GameObject CreatePane(Transform parent, string name, string title, float preferredWidth, float minWidth, float flexibleWidth)
        {
            GameObject pane = CreateBox(name, parent, new Color(0.50f, 0.52f, 0.48f, 1f));
            LayoutElement paneLayout = pane.AddComponent<LayoutElement>();
            paneLayout.preferredWidth = preferredWidth;
            paneLayout.minWidth = minWidth;
            paneLayout.flexibleWidth = flexibleWidth;

            GameObject titleBar = CreateBox(name + "Title", pane.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(titleBar.GetComponent<RectTransform>(), 6f, 6f, 6f, 30f);
            TextMeshProUGUI titleText = CreateText("Title", titleBar.transform, title, 13, TextAlignmentOptions.MidlineLeft);
            titleText.fontStyle = FontStyles.Bold;
            Stretch(titleText.rectTransform(), 10f, 0f);
            return pane;
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

        private static void ConfigureTrackingContentForHorizontalScroll(RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = new Vector2(TrackingContentWidth, 0f);
            LayoutElement layout = content.gameObject.GetComponent<LayoutElement>() ?? content.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = TrackingContentWidth;

            VerticalLayoutGroup vertical = content.gameObject.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                vertical.childForceExpandWidth = false;
            }

            ContentSizeFitter fitter = content.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
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
    }
}
