using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private const int MaxDisplayedProducts = 96;
        private const int MaxDisplayedTrackingRecords = 48;
        private const float OrderWindowMaxWidth = 1760f;
        private const float TrackingContentWidth = 460f;

        private RectTransform productListContent;
        private StorageNetworkKeyedRowCache productRows;
        private RectTransform orderDetailsContent;
        private RectTransform orderDetailsViewport;
        private RectTransform orderTrackingContent;
        private StorageNetworkKeyedRowCache orderTrackingRows;
        private RectTransform orderTrackingRowsContent;
        private KInputTextField orderTrackingSearchInput;
        private GameObject orderTrackingSearchRow;
        private GameObject orderTrackingDetailRoot;
        private bool compactOrderWindow;
        private bool inlineOrderTracking;
        private readonly ProductionOrderService productionOrderService = new ProductionOrderService();
        private List<ProductDisplayGroup> orderProducts = new List<ProductDisplayGroup>();
        private List<RecipeDisplayInfo> craftableRecipes = new List<RecipeDisplayInfo>();
        private string lastOrderStatus;
        private string selectedProductKey;
        private int selectedRouteIndex;
        private float requestedProductAmount;
        private float orderAmountStep = 100f;
        private float orderPanelRefreshElapsed;
        private string orderDetailsSignature;
        private string orderTrackingSignature;
        private string orderTrackingSearchText;
        private TrackingFilterMode orderTrackingFilterMode = TrackingFilterMode.Current;
        private KInputTextField orderAmountInput;
        private KInputTextField keepRuleAmountInput;
        private string keepRuleDraftProductKey;
        private float keepRuleDraftAmount;

        private enum TrackingFilterMode
        {
            Current,
            All,
            Running,
            Completed,
            Abnormal
        }
        private void ToggleHeaderWindow()
        {
            EnsureHeaderWindow();
            bool shouldOpen = !headerWindowRoot.activeSelf;
            if (!shouldOpen)
            {
                DeactivateOrderInputs();
                orderDetailsSignature = null;
                orderTrackingSignature = null;
            }

            headerWindowRoot.SetActive(shouldOpen);
            if (headerWindowRoot.activeSelf)
            {
                orderDetailsSignature = null;
                orderTrackingSignature = null;
                orderPanelRefreshElapsed = 0f;
                RefreshOrderPanel(true);
            }
        }

        private void CloseHeaderWindow()
        {
            if (headerWindowRoot != null)
            {
                DeactivateOrderInputs();
                orderDetailsSignature = null;
                orderTrackingSignature = null;
                CloseOrderTrackingDetail();
                headerWindowRoot.SetActive(false);
            }
        }

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

        private void EnsureHeaderWindow()
        {
            if (headerWindowRoot != null)
            {
                return;
            }

            Transform rootParent = transform.parent != null ? transform.parent : windowRect.parent;
            compactOrderWindow = IsCompactOrderWindow();
            inlineOrderTracking = true;
            headerWindowRoot = CreateBox("ProductionOrderCenter", rootParent, new Color(0.18f, 0.20f, 0.21f, 0.98f));
            ApplyThinBoxSprite(headerWindowRoot.GetComponent<Image>());
            RectTransform rootRect = headerWindowRoot.GetComponent<RectTransform>();
            ApplyOrderWindowRootLayout(rootRect);

            GameObject header = CreateBox("Header", headerWindowRoot.transform, OniPinkInactive());
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 50f);
            header.AddComponent<StorageNetworkWindowDrag>().Configure(rootRect, "orderCenter");

            TextMeshProUGUI title = CreateText("Title", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CENTER_TITLE), 18, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.raycastTarget = false;
            Stretch(title.rectTransform(), 12f, 0f);
            title.rectTransform().offsetMax = new Vector2(-270f, 0f);

            TextMeshProUGUI subtitle = CreateText("Subtitle", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CENTER_SUBTITLE), 11, TextAlignmentOptions.MidlineRight);
            subtitle.color = new Color(0.76f, 0.82f, 0.84f, 1f);
            subtitle.raycastTarget = false;
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform subtitleRect = subtitle.rectTransform();
            subtitleRect.anchorMin = new Vector2(0.44f, 0f);
            subtitleRect.anchorMax = Vector2.one;
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = new Vector2(-48f, 0f);

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, CloseHeaderWindow);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-10f, 0f);
            closeRect.sizeDelta = new Vector2(28f, 26f);

            GameObject body = CreateBox("Body", headerWindowRoot.transform, new Color(0.68f, 0.68f, 0.61f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 66f);
            HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.padding = new RectOffset(8, 8, 8, 8);
            bodyLayout.spacing = 8f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = false;
            bodyLayout.childForceExpandHeight = true;

            CreateProductListPane(body.transform);
            CreateOrderWorkspacePane(body.transform);
            CreateOrderTrackingPane(body.transform);
            headerWindowRoot.SetActive(false);
        }

        private void ApplyOrderWindowRootLayout(RectTransform rectTransform)
        {
            float canvasWidth = GetCanvasWidth();
            float canvasHeight = Screen.height;
            float width = Mathf.Clamp(canvasWidth - (compactOrderWindow ? 48f : 144f), compactOrderWindow ? 980f : 1180f, OrderWindowMaxWidth);
            float height = Mathf.Clamp(canvasHeight - (compactOrderWindow ? 104f : 136f), 620f, 980f);

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchoredPosition = Vector2.zero;
            StorageNetworkWindowDrag.TryApplyLayout("orderCenter", rectTransform, new Vector2(940f, 560f), new Vector2(width, height));
            StorageNetworkWindowDrag.ClampToScreen(rectTransform);
        }

        private void CreateProductListPane(Transform parent)
        {
            GameObject pane = CreatePane(parent, "ProductPane", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_LIST_TITLE), compactOrderWindow ? 220f : 260f, compactOrderWindow ? 200f : 240f, 0f);
            RectTransform viewport = CreateScrollViewport(pane.transform, "ProductViewport", out productListContent, 42f, 8f, 8f, 8f, 8f);
            productRows = new StorageNetworkKeyedRowCache(productListContent);
            Scrollbar scrollbar = CreateScrollbar(pane.transform, 96f, 8f);
            WireScrollRect(viewport.gameObject, productListContent, scrollbar, 24f);
        }

        private void CreateOrderWorkspacePane(Transform parent)
        {
            GameObject pane = CreatePane(parent, "OrderWorkspacePane", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_WORKSPACE_TITLE), 0f, compactOrderWindow ? 500f : 700f, 1f);
            RectTransform viewport = CreateScrollViewport(pane.transform, "OrderWorkspaceViewport", out orderDetailsContent, 42f, 8f, 8f, 8f, 8f);
            orderDetailsViewport = viewport;
            VerticalLayoutGroup contentLayout = orderDetailsContent.GetComponent<VerticalLayoutGroup>();
            if (contentLayout != null)
            {
                contentLayout.childControlWidth = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = true;
            }

            Scrollbar scrollbar = CreateScrollbar(pane.transform, 42f, 8f);
            WireScrollRect(viewport.gameObject, orderDetailsContent, scrollbar, 26f);
        }

        private void CreateOrderTrackingPane(Transform parent)
        {
            GameObject pane = CreatePane(parent, "OrderTrackingPane", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TRACKING_TITLE), compactOrderWindow ? 360f : 460f, compactOrderWindow ? 330f : 430f, 0f);
            CreateOrderTrackingSearchBar(pane.transform);
            RectTransform viewport = CreateScrollViewport(pane.transform, "OrderTrackingViewport", out orderTrackingContent, 96f, 8f, 8f, 8f, 8f);
            Scrollbar scrollbar = CreateScrollbar(pane.transform, 42f, 8f);
            ConfigureTrackingContentForHorizontalScroll(orderTrackingContent);
            WireScrollRect(viewport.gameObject, orderTrackingContent, scrollbar, 22f, allowHorizontal: true);
        }

        private void CreateOrderTrackingSearchBar(Transform parent)
        {
            if (orderTrackingSearchRow != null)
            {
                Destroy(orderTrackingSearchRow);
                orderTrackingSearchRow = null;
            }

            orderTrackingSearchRow = CreatePlainImage("TrackingSearchRow", parent, new Color(0.60f, 0.61f, 0.55f, 1f));
            RectTransform rowRect = orderTrackingSearchRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.offsetMin = new Vector2(8f, -92f);
            rowRect.offsetMax = new Vector2(-22f, -36f);

            VerticalLayoutGroup layout = orderTrackingSearchRow.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 5, 5);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            orderTrackingSearchInput = CreateFixedTextInput(
                orderTrackingSearchRow.transform,
                "TrackingSearchInput",
                orderTrackingSearchText ?? string.Empty,
                320f,
                22f,
                11);
            orderTrackingSearchInput.onValueChanged.AddListener(value =>
            {
                orderTrackingSearchText = value;
                orderTrackingSignature = null;
                RebuildOrderDetails();
            });

            GameObject filterRow = new GameObject("TrackingFilterRow");
            filterRow.transform.SetParent(orderTrackingSearchRow.transform, false);
            filterRow.AddComponent<RectTransform>();
            filterRow.AddComponent<LayoutElement>().preferredHeight = 22f;
            HorizontalLayoutGroup filterLayout = filterRow.AddComponent<HorizontalLayoutGroup>();
            filterLayout.spacing = 4f;
            filterLayout.childAlignment = TextAnchor.MiddleLeft;
            filterLayout.childControlWidth = true;
            filterLayout.childControlHeight = true;
            filterLayout.childForceExpandWidth = true;
            filterLayout.childForceExpandHeight = false;

            AddTrackingFilterButton(filterRow.transform, "当前", TrackingFilterMode.Current);
            AddTrackingFilterButton(filterRow.transform, "全部", TrackingFilterMode.All);
            AddTrackingFilterButton(filterRow.transform, "运行中", TrackingFilterMode.Running);
            AddTrackingFilterButton(filterRow.transform, "已完成", TrackingFilterMode.Completed);
            AddTrackingFilterButton(filterRow.transform, "异常", TrackingFilterMode.Abnormal);
        }

        private void AddTrackingFilterButton(Transform parent, string text, TrackingFilterMode mode)
        {
            bool selected = orderTrackingFilterMode == mode;
            GameObject button = CreateStyledButton("TrackingFilterButton", parent, text, () =>
            {
                orderTrackingFilterMode = mode;
                orderTrackingSignature = null;
                if (orderTrackingSearchRow != null && orderTrackingSearchRow.transform.parent != null)
                {
                    Transform searchParent = orderTrackingSearchRow.transform.parent;
                    CreateOrderTrackingSearchBar(searchParent);
                }

                RebuildOrderDetails();
            }, selected ? KleiPinkStyle() : KleiBlueStyle());
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = 58f;
            layout.preferredHeight = 20f;
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

        private void RefreshOrderPanel(bool rebuildProducts)
        {
            if (rebuildProducts)
            {
                productionOrderService.Refresh();
                craftableRecipes = productionOrderService.GetCraftableRecipes();
                orderProducts = productionOrderService.GetProductGroups();
                if (orderProducts.Count == 0)
                {
                    selectedProductKey = null;
                    selectedRouteIndex = 0;
                }
                else if (string.IsNullOrEmpty(selectedProductKey) || orderProducts.All(product => product.ProductKey != selectedProductKey))
                {
                    SelectProduct(orderProducts[0].ProductKey, false);
                }

                if (productListContent != null)
                {
                    RebuildProductList();
                }
            }

            RebuildOrderDetails();
        }

        private void UpdateOrderPanelAutoRefresh(float dt)
        {
            if (headerWindowRoot == null || !headerWindowRoot.activeSelf)
            {
                return;
            }

            orderPanelRefreshElapsed += dt;
            if (orderPanelRefreshElapsed < 1f)
            {
                return;
            }

            orderPanelRefreshElapsed = 0f;
            productionOrderService.Refresh();
            if (IsOrderInputFocused())
            {
                RebuildOrderTracking(GetSelectedProduct());
                return;
            }

            RebuildOrderTracking(GetSelectedProduct());
        }

    }
}
