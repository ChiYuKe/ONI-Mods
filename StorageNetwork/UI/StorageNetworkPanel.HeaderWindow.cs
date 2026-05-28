using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {
        private const int MaxDisplayedProducts = 96;
        private const int MaxDisplayedTrackingRecords = 12;

        private RectTransform productListContent;
        private RectTransform orderDetailsContent;
        private RectTransform orderTrackingContent;
        private readonly ProductionOrderService productionOrderService = new ProductionOrderService();
        private List<ProductDisplayGroup> orderProducts = new List<ProductDisplayGroup>();
        private List<RecipeDisplayInfo> craftableRecipes = new List<RecipeDisplayInfo>();
        private string lastOrderStatus;
        private string selectedProductKey;
        private int selectedRouteIndex;
        private float requestedProductAmount;
        private float orderPanelRefreshElapsed;
        private KInputTextField orderAmountInput;

        private void ToggleHeaderWindow()
        {
            EnsureHeaderWindow();
            headerWindowRoot.SetActive(!headerWindowRoot.activeSelf);
            if (headerWindowRoot.activeSelf)
            {
                orderPanelRefreshElapsed = 0f;
                RefreshOrderPanel(true);
            }
        }

        private void CloseHeaderWindow()
        {
            if (headerWindowRoot != null)
            {
                headerWindowRoot.SetActive(false);
            }
        }

        private void EnsureHeaderWindow()
        {
            if (headerWindowRoot != null)
            {
                return;
            }

            Transform rootParent = transform.parent != null ? transform.parent : windowRect.parent;
            headerWindowRoot = CreateBox("ProductionOrderCenter", rootParent, new Color(0.18f, 0.20f, 0.21f, 0.98f));
            ApplyThinBoxSprite(headerWindowRoot.GetComponent<Image>());
            SetStretch(headerWindowRoot.GetComponent<RectTransform>(), 72f, 96f, 44f, 72f);

            GameObject header = CreateBox("Header", headerWindowRoot.transform, new Color(0.23f, 0.27f, 0.29f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 50f);

            TextMeshProUGUI title = CreateText("Title", header.transform, "生产订单中心", 18, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);
            title.rectTransform().offsetMax = new Vector2(-270f, 0f);

            TextMeshProUGUI subtitle = CreateText("Subtitle", header.transform, "库存承诺 / 补产链路 / 多设备调度 / 异常追踪", 11, TextAlignmentOptions.MidlineRight);
            subtitle.color = new Color(0.76f, 0.82f, 0.84f, 1f);
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform subtitleRect = subtitle.rectTransform();
            subtitleRect.anchorMin = new Vector2(0.44f, 0f);
            subtitleRect.anchorMax = Vector2.one;
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = new Vector2(-48f, 0f);

            GameObject closeButton = CreateGameButton("CloseButton", header.transform, "X", CloseHeaderWindow);
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

        private void CreateProductListPane(Transform parent)
        {
            GameObject pane = CreatePane(parent, "ProductPane", "成品目录", 330f, 310f, 0f);
            RectTransform viewport = CreateScrollViewport(pane.transform, "ProductViewport", out productListContent, 42f, 8f, 8f, 8f, 8f);
            Scrollbar scrollbar = CreateScrollbar(pane.transform);
            WireScrollRect(viewport.gameObject, productListContent, scrollbar, 24f);
        }

        private void CreateOrderWorkspacePane(Transform parent)
        {
            GameObject pane = CreatePane(parent, "OrderWorkspacePane", "订单工作台", 0f, 640f, 1f);
            RectTransform viewport = CreateScrollViewport(pane.transform, "OrderWorkspaceViewport", out orderDetailsContent, 42f, 8f, 8f, 8f, 8f);
            Scrollbar scrollbar = CreateScrollbar(pane.transform);
            WireScrollRect(viewport.gameObject, orderDetailsContent, scrollbar, 26f);
        }

        private void CreateOrderTrackingPane(Transform parent)
        {
            GameObject pane = CreatePane(parent, "OrderTrackingPane", "活动订单追踪", 310f, 290f, 0f);
            RectTransform viewport = CreateScrollViewport(pane.transform, "OrderTrackingViewport", out orderTrackingContent, 42f, 8f, 8f, 8f, 8f);
            Scrollbar scrollbar = CreateScrollbar(pane.transform);
            WireScrollRect(viewport.gameObject, orderTrackingContent, scrollbar, 22f);
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

        private static void WireScrollRect(GameObject viewport, RectTransform content, Scrollbar scrollbar, float sensitivity)
        {
            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = sensitivity;
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

                RebuildProductList();
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
            if (orderAmountInput != null && orderAmountInput.isFocused)
            {
                RebuildOrderTracking(GetSelectedProduct());
                return;
            }

            craftableRecipes = productionOrderService.GetCraftableRecipes();
            orderProducts = productionOrderService.GetProductGroups();
            RebuildProductList();
            RebuildOrderDetails();
        }

        private void RebuildProductList()
        {
            ClearChildren(productListContent);
            if (orderProducts.Count == 0)
            {
                AddProductListText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_WINDOW_EMPTY));
                return;
            }

            foreach (ProductDisplayGroup product in orderProducts.Take(MaxDisplayedProducts))
            {
                CreateProductButton(product);
            }
        }

        private void CreateProductButton(ProductDisplayGroup product)
        {
            bool selected = product.ProductKey == selectedProductKey;
            GameObject button = CreateStyledButton("ProductButton", productListContent, string.Empty, () => SelectProduct(product.ProductKey), selected ? KleiPinkStyle() : KleiBlueStyle());
            button.AddComponent<LayoutElement>().preferredHeight = 56f;
            HorizontalLayoutGroup layout = button.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIcon(button.transform, product.Icon, 36f);

            GameObject textColumn = new GameObject("TextColumn");
            textColumn.transform.SetParent(button.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(textColumn, 1f, 0, 0, 0, 0);

            TextMeshProUGUI name = CreateText("Name", textColumn.transform, product.ProductName, 12, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            TextMeshProUGUI meta = CreateText(
                "Meta",
                textColumn.transform,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_META), GameUtil.GetFormattedMass(productionOrderService.GetNetworkAvailableAmount(product.ProductTag)), product.Routes.Count),
                10,
                TextAlignmentOptions.MidlineLeft);
            meta.color = new Color(0.78f, 0.82f, 0.84f, 1f);
            meta.textWrappingMode = TextWrappingModes.NoWrap;
            meta.overflowMode = TextOverflowModes.Ellipsis;
            meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        }

        private void RebuildOrderDetails()
        {
            ClearChildren(orderDetailsContent);
            ProductDisplayGroup product = GetSelectedProduct();
            if (product == null || product.Routes.Count == 0)
            {
                AddInfoText(orderDetailsContent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_WINDOW_EMPTY), 96f);
                RebuildOrderTracking(null);
                return;
            }

            selectedRouteIndex = Mathf.Clamp(selectedRouteIndex, 0, product.Routes.Count - 1);
            RecipeDisplayInfo route = product.Routes[selectedRouteIndex];
            if (requestedProductAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                requestedProductAmount = ProductionRecipeCatalog.GetRecipeResultForProduct(route.Recipe, product.ProductTag)?.amount ?? 1f;
            }

            ProductionOrderDraft draft = productionOrderService.BuildDraft(product, route, requestedProductAmount);
            AddOrderSummaryBand(orderDetailsContent, product, route, draft);
            AddOrderEditorAndPlan(orderDetailsContent, product, route, draft);
            AddProductionChain(orderDetailsContent, draft);
            AddOrderFooter(orderDetailsContent, product, route, draft);
            RebuildOrderTracking(product);
        }

        private void AddOrderSummaryBand(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            GameObject band = CreateSection(parent, "OrderSummaryBand", 88f, new Color(0.74f, 0.74f, 0.68f, 1f));
            HorizontalLayoutGroup layout = band.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIcon(band.transform, product.Icon, 52f);

            GameObject titleColumn = new GameObject("TitleColumn");
            titleColumn.transform.SetParent(band.transform, false);
            titleColumn.AddComponent<RectTransform>();
            titleColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(titleColumn, 2f, 0, 0, 0, 0);

            TextMeshProUGUI title = CreateText("ProductName", titleColumn.transform, product.ProductName, 16, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.13f, 0.15f, 0.15f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            AddPlanLine(titleColumn.transform, string.Format("路线 {0}    配方 {1}", route.FabricatorName, ProductionOrderFormatting.FormatRecipeElements(route.Recipe.ingredients)), 9, FontStyles.Normal, new Color(0.30f, 0.32f, 0.31f, 1f), 22f);
            AddPlanLine(titleColumn.transform, BuildOrderTrackingStatus(product, draft), 9, draft.DuplicateOrder != null ? FontStyles.Bold : FontStyles.Italic, draft.DuplicateOrder != null ? WarningColor() : MutedTextColor(), 22f);

            AddMetricTile(band.transform, "目标", GameUtil.GetFormattedMass(draft.RequestedAmount), GetRiskColor(draft.RiskLevel), 108f);
            AddMetricTile(band.transform, "可用", GameUtil.GetFormattedMass(draft.NetworkAvailableAmount), PositiveColor(), 108f);
            AddMetricTile(band.transform, "批次", draft.Plan != null ? draft.Plan.OrderCount.ToString() : "0", NeutralBlue(), 86f);
            AddMetricTile(band.transform, "状态", GetRiskLabel(draft.RiskLevel), GetRiskColor(draft.RiskLevel), 94f);
        }

        private void AddOrderEditorAndPlan(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            GameObject row = CreateSection(parent, "OrderEditorAndPlan", 450f, new Color(0.72f, 0.73f, 0.67f, 1f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject editor = CreateSubPanel(row.transform, "DraftEditor", "下单参数", 278f, 260f, 0f);
            AddAmountEditor(editor.transform, product);
            AddKeepRuleEditor(editor.transform, product, route);
            AddRouteEditor(editor.transform, product);
            AddRecipeEditor(editor.transform, product, route);
            AddValidationPanel(editor.transform, product, draft);

            GameObject plan = CreateSubPanel(row.transform, "PlanPanel", "调度方案", 0f, 420f, 1f);
            AddPlanMetrics(plan.transform, draft);
            AddAssignmentTable(plan.transform, draft);
            AddMaterialTable(plan.transform, draft);
        }

        private void AddAmountEditor(Transform parent, ProductDisplayGroup product)
        {
            AddSmallTitle(parent, "订单数量");
            GameObject row = CreatePlainImage("AmountRow", parent, new Color(0.78f, 0.78f, 0.72f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 34f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 5, 5);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            orderAmountInput = CreateAmountInput(row.transform);
            orderAmountInput.gameObject.GetComponent<LayoutElement>().preferredWidth = 88f;
            orderAmountInput.text = FormatAmount(requestedProductAmount);
            orderAmountInput.onEndEdit.AddListener(value =>
            {
                if (TryParseAmount(value, out float parsed))
                {
                    requestedProductAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, parsed);
                }

                RebuildOrderDetails();
            });

            AddQuickAmountButton(row.transform, 100f, "100kg");
            AddQuickAmountButton(row.transform, 500f, "500kg");
            AddQuickAmountButton(row.transform, 1000f, "1t");
        }

        private void AddKeepRuleEditor(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route)
        {
            ProductionKeepRule rule = productionOrderService.GetKeepRule(product.ProductTag);
            AddSmallTitle(parent, "货物保持");
            GameObject row = CreatePlainImage("KeepRuleRow", parent, new Color(0.78f, 0.78f, 0.72f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 34f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 5, 5);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            string label = rule != null
                ? string.Format("保持 {0}", GameUtil.GetFormattedMass(rule.TargetAmount))
                : "未开启";
            TextMeshProUGUI status = CreateText("KeepRuleStatus", row.transform, label, 9, TextAlignmentOptions.MidlineLeft);
            status.color = rule != null ? PositiveColor() : MutedTextColor();
            status.fontStyle = FontStyles.Bold;
            status.textWrappingMode = TextWrappingModes.NoWrap;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject enableButton = CreateStyledButton("EnableKeepRule", row.transform, "保持", () =>
            {
                productionOrderService.SetKeepRule(product, route, requestedProductAmount);
                lastOrderStatus = string.Format("货物保持：{0} 低于 {1} 时自动下单。", product.ProductName, GameUtil.GetFormattedMass(requestedProductAmount));
                productionOrderService.Refresh();
                RebuildOrderDetails();
            }, KleiBlueStyle());
            LayoutElement enableLayout = enableButton.AddComponent<LayoutElement>();
            enableLayout.preferredWidth = 50f;
            enableLayout.preferredHeight = 24f;

            GameObject clearButton = CreateStyledButton("ClearKeepRule", row.transform, "关闭", () =>
            {
                productionOrderService.ClearKeepRule(product.ProductTag);
                lastOrderStatus = string.Format("货物保持：已关闭 {0}。", product.ProductName);
                RebuildOrderDetails();
            }, rule != null ? KleiPinkStyle() : KleiBlueStyle());
            LayoutElement clearLayout = clearButton.AddComponent<LayoutElement>();
            clearLayout.preferredWidth = 50f;
            clearLayout.preferredHeight = 24f;
            clearButton.GetComponent<KButton>().isInteractable = rule != null;
        }

        private void AddRouteEditor(Transform parent, ProductDisplayGroup product)
        {
            AddSmallTitle(parent, string.Format("生产设备 ({0})", product.Routes.Select(route => route.FabricatorName).Distinct().Count()));
            foreach (IGrouping<string, RecipeDisplayInfo> group in product.Routes.GroupBy(route => route.FabricatorName).Take(5))
            {
                int routeIndex = product.Routes.FindIndex(route => route.FabricatorName == group.Key);
                bool selected = product.Routes[selectedRouteIndex].FabricatorName == group.Key;
                AddChoiceButton(parent, selected ? "> " + group.Key : group.Key, routeIndex, selected, 24f);
            }
        }

        private void AddRecipeEditor(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo selectedRoute)
        {
            List<RecipeDisplayInfo> alternatives = product.Routes
                .Where(route => route.FabricatorName == selectedRoute.FabricatorName)
                .ToList();
            AddSmallTitle(parent, string.Format("配方方案 ({0})", alternatives.Count));
            if (alternatives.Count <= 1)
            {
                AddPlanLine(parent, ProductionOrderFormatting.FormatRecipeElements(selectedRoute.Recipe.ingredients), 9, FontStyles.Bold, new Color(0.18f, 0.21f, 0.21f, 1f), 24f);
                return;
            }

            foreach (RecipeDisplayInfo route in alternatives.Take(4))
            {
                int routeIndex = product.Routes.IndexOf(route);
                string label = ProductionOrderFormatting.FormatRecipeElements(route.Recipe.ingredients);
                AddChoiceButton(parent, routeIndex == selectedRouteIndex ? "> " + label : label, routeIndex, routeIndex == selectedRouteIndex, 24f);
            }
        }

        private void AddValidationPanel(Transform parent, ProductDisplayGroup product, ProductionOrderDraft draft)
        {
            AddSmallTitle(parent, "草案校验");
            GameObject banner = CreatePlainImage("ValidationBanner", parent, new Color(GetRiskColor(draft.RiskLevel).r, GetRiskColor(draft.RiskLevel).g, GetRiskColor(draft.RiskLevel).b, 0.55f));
            banner.AddComponent<LayoutElement>().preferredHeight = 28f;
            TextMeshProUGUI bannerText = CreateText("Text", banner.transform, GetRiskLabel(draft.RiskLevel), 11, TextAlignmentOptions.Center);
            bannerText.color = new Color(0.12f, 0.13f, 0.12f, 1f);
            bannerText.fontStyle = FontStyles.Bold;
            Stretch(bannerText.rectTransform(), 6f, 0f);

            foreach (string message in draft.ValidationMessages.Take(3))
            {
                AddPlanLine(parent, message, 9, FontStyles.Normal, GetRiskColor(draft.RiskLevel), 22f);
            }

            AddPlanLine(parent, string.Format("{0} 活动订单：{1}", product.ProductName, productionOrderService.GetActiveOrdersForProduct(product.ProductTag, 99).Count), 9, FontStyles.Italic, MutedTextColor(), 22f);
        }

        private void AddPlanMetrics(Transform parent, ProductionOrderDraft draft)
        {
            GameObject row = new GameObject("PlanMetrics");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 48f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            float currentCycle = GetCurrentCycleTime();
            float estimateSeconds = productionOrderService.EstimatePlanSeconds(draft.Plan, out bool infinite);
            string finish = infinite ? "未知" : ProductionOrderFormatting.FormatCycle(currentCycle + estimateSeconds / 600f);
            AddMetricTile(row.transform, "当前周期", ProductionOrderFormatting.FormatCycle(currentCycle), NeutralBlue(), 104f);
            AddMetricTile(row.transform, "预计完成", finish, infinite ? WarningColor() : PositiveColor(), 104f);
            AddMetricTile(row.transform, "设备", draft.Plan?.Assignments.Count.ToString() ?? "0", NeutralBlue(), 84f);
            AddMetricTile(row.transform, "补产项", draft.ProducedRequirementCount.ToString(), draft.ProducedRequirementCount > 0 ? WarningColor() : PositiveColor(), 84f);
            AddMetricTile(row.transform, "缺料项", draft.BlockedRequirementCount.ToString(), draft.BlockedRequirementCount > 0 ? DangerColor() : PositiveColor(), 84f);
        }

        private void AddAssignmentTable(Transform parent, ProductionOrderDraft draft)
        {
            AddSmallTitle(parent, "任务分配");
            AddTableHeader(parent, "设备", "批次", "产量");
            if (draft.Plan == null || draft.Plan.Assignments.Count == 0)
            {
                AddInfoText(parent, "没有可用生产建筑。", 28f);
                return;
            }

            foreach (ProductionPlanAssignment assignment in draft.Plan.Assignments.Take(6))
            {
                AddTableRow(
                    parent,
                    assignment.Fabricator != null ? assignment.Fabricator.GetProperName() : "?",
                    assignment.OrderCount.ToString(),
                    GameUtil.GetFormattedMass(assignment.OutputAmount),
                    NeutralTextColor());
            }
        }

        private void AddMaterialTable(Transform parent, ProductionOrderDraft draft)
        {
            AddSmallTitle(parent, "材料账本");
            AddTableHeader(parent, "材料", "需求", "库存 / 缺口");
            if (draft.Plan == null || draft.Plan.Requirements.Count == 0)
            {
                AddInfoText(parent, "该配方没有材料输入，提交后直接排产。", 28f);
                return;
            }

            foreach (ProductionPlanRequirement requirement in draft.Plan.Requirements.Take(8))
            {
                AddMaterialRow(parent, requirement, 0);
            }
        }

        private void AddMaterialRow(Transform parent, ProductionPlanRequirement requirement, int depth)
        {
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
            bool covered = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool produced = !covered && requirement.Child != null;
            Color color = covered ? PositiveColor() : produced ? WarningColor() : DangerColor();
            string name = new string(' ', depth * 2) + ProductionOrderFormatting.GetTagDisplayName(requirement.Material);
            string available = GameUtil.GetFormattedMass(requirement.AvailableAmount);
            string status = covered
                ? string.Format("{0} / {1}", available, GameUtil.GetFormattedMass(0f))
                : string.Format("{0} / 缺 {1}", available, GameUtil.GetFormattedMass(missing));
            AddTableRow(parent, name, GameUtil.GetFormattedMass(requirement.RequiredAmount), status, color);
            if (requirement.Child != null && depth < 2)
            {
                AddTableRow(parent, "  补产路线", requirement.Child.Recipe.GetUIName(false), requirement.Child.FabricatorName, WarningColor());
            }
        }

        private void AddProductionChain(Transform parent, ProductionOrderDraft draft)
        {
            GameObject panel = CreateSubPanel(parent, "ProductionChain", "产线链路", 0f, 0f, 0f);
            panel.GetComponent<LayoutElement>().preferredHeight = 150f;
            if (draft.Plan == null)
            {
                AddInfoText(panel.transform, "没有可显示的产线。", 36f);
                return;
            }

            foreach (string line in productionOrderService.FormatPlanLines(draft.Plan, 0).Take(6))
            {
                AddPlanLine(panel.transform, line, 9, FontStyles.Normal, NeutralTextColor(), 21f);
            }
        }

        private void AddOrderFooter(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            GameObject footer = CreateSection(parent, "OrderFooter", 48f, new Color(0.64f, 0.65f, 0.59f, 1f));
            HorizontalLayoutGroup layout = footer.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 7, 7);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            string statusText = !string.IsNullOrEmpty(lastOrderStatus)
                ? lastOrderStatus
                : draft.CanSubmit ? "草案已通过，可提交调度。" : "草案存在阻塞，请检查材料或设备。";
            TextMeshProUGUI status = CreateText("FooterStatus", footer.transform, statusText, 10, TextAlignmentOptions.MidlineLeft);
            status.color = draft.CanSubmit ? PositiveColor() : GetRiskColor(draft.RiskLevel);
            status.fontStyle = FontStyles.Bold;
            status.textWrappingMode = TextWrappingModes.NoWrap;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject button = CreateGameButton("ConfirmOrder", footer.transform, draft.DuplicatePolicy == ProductionOrderDuplicatePolicy.MergeIntoExisting ? "合并订单" : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CONFIRM), () =>
            {
                ProductionOrderSubmitResult result = productionOrderService.SubmitOrder(product, route, requestedProductAmount, GetCurrentCycleTime());
                lastOrderStatus = result.Message;
                productionOrderService.Refresh();
                RebuildOrderDetails();
            });
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.preferredHeight = 30f;
            button.GetComponent<KButton>().isInteractable = draft.CanSubmit;
        }

        private void RebuildOrderTracking(ProductDisplayGroup product)
        {
            ClearChildren(orderTrackingContent);
            if (product == null)
            {
                AddInfoText(orderTrackingContent, "选择成品后显示活动订单。", 48f);
                return;
            }

            List<ProductionOrderRecord> records = productionOrderService.GetRecentOrdersForProduct(product.ProductTag, MaxDisplayedTrackingRecords).ToList();
            int activeCount = records.Count(IsTrackingActive);
            AddPlanLine(orderTrackingContent, string.Format("{0}：{1} 个活动订单 / {2} 条最近记录", product.ProductName, activeCount, records.Count), 11, FontStyles.Bold, new Color(0.14f, 0.16f, 0.15f, 1f), 30f);
            if (records.Count == 0)
            {
                AddInfoText(orderTrackingContent, "暂无活动订单。提交后会显示状态、数量、批次和合并记录。", 58f);
                return;
            }

            foreach (ProductionOrderRecord record in records)
            {
                AddTrackingCard(orderTrackingContent, record);
            }
        }

        private void AddTrackingCard(Transform parent, ProductionOrderRecord record)
        {
            bool abnormal = record.State == ProductionOrderState.Abnormal;
            bool active = IsTrackingActive(record);
            Color cardColor = GetTrackingCardColor(record);
            GameObject card = CreatePlainImage("TrackingCard", parent, cardColor);
            card.AddComponent<LayoutElement>().preferredHeight = active || abnormal || record.MergeCount > 0 ? 78f : 58f;

            HorizontalLayoutGroup cardLayout = card.AddComponent<HorizontalLayoutGroup>();
            cardLayout.padding = new RectOffset(8, 8, 5, 5);
            cardLayout.spacing = 8f;
            cardLayout.childAlignment = TextAnchor.MiddleLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;

            GameObject textColumn = new GameObject("TrackingText");
            textColumn.transform.SetParent(card.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(textColumn, 2f, 0, 0, 0, 0);

            AddPlanLine(textColumn.transform, string.Format("#{0} {1}    {2}    {3}", record.DisplayId, record.ProductName, GetOrderSourceLabel(record), GetOrderStateLabel(record.State)), 10, FontStyles.Bold, GetOrderStateColor(record.State), 22f);
            AddPlanLine(textColumn.transform, string.Format("进度 {0}/{1}    批次 {2}    创建周期 {3}", GameUtil.GetFormattedMass(record.ProducedAtSubmit), GameUtil.GetFormattedMass(record.RequestedAmount), record.OrderCount, ProductionOrderFormatting.FormatCycle(record.CreatedCycle)), 9, FontStyles.Normal, NeutralTextColor(), 20f);
            if (record.MergeCount > 0)
            {
                AddPlanLine(textColumn.transform, string.Format("已合并 {0} 次    最后活动周期 {1}", record.MergeCount, ProductionOrderFormatting.FormatCycle(record.LastActivityCycle)), 8, FontStyles.Italic, MutedTextColor(), 18f);
            }

            if (abnormal && !string.IsNullOrEmpty(record.AbnormalReason))
            {
                AddPlanLine(textColumn.transform, record.AbnormalReason, 8, FontStyles.Bold, DangerColor(), 18f);
            }

            if (active)
            {
                GameObject cancelButton = CreateIconOnlyButton("CancelOrderButton", card.transform, GetCancelActionSprite(), () => CancelTrackedOrder(record.Key));
                LayoutElement cancelLayout = cancelButton.AddComponent<LayoutElement>();
                cancelLayout.preferredWidth = 28f;
                cancelLayout.preferredHeight = 28f;
            }
        }

        private static Color GetTrackingCardColor(ProductionOrderRecord record)
        {
            if (record.State == ProductionOrderState.Completed)
            {
                return new Color(0.64f, 0.74f, 0.61f, 1f);
            }

            if (record.State == ProductionOrderState.Abnormal)
            {
                return new Color(0.79f, 0.66f, 0.63f, 1f);
            }

            return new Color(0.76f, 0.76f, 0.70f, 1f);
        }

        private static Sprite GetCancelActionSprite()
        {
            return GetSpriteByName("action_cancel") ??
                   GetSpriteByName("icon_action_cancel") ??
                   GetSpriteByName("action_cancel.png");
        }

        private static string GetOrderSourceLabel(ProductionOrderRecord record)
        {
            return record.IsAutomatic ? "货物保持" : "手动";
        }

        private void CancelTrackedOrder(string orderKey)
        {
            lastOrderStatus = productionOrderService.CancelOrder(orderKey, GetCurrentCycleTime());
            productionOrderService.Refresh();
            RebuildOrderDetails();
        }

        private GameObject CreateSection(Transform parent, string name, float height, Color color)
        {
            GameObject section = CreatePlainImage(name, parent, color);
            LayoutElement layout = section.AddComponent<LayoutElement>();
            if (height > 0f)
            {
                layout.preferredHeight = height;
                layout.minHeight = height;
            }
            else
            {
                layout.flexibleHeight = 1f;
            }

            return section;
        }

        private GameObject CreateSubPanel(Transform parent, string name, string title, float preferredWidth, float minWidth, float flexibleWidth)
        {
            GameObject panel = CreatePlainImage(name, parent, new Color(0.84f, 0.84f, 0.78f, 1f));
            LayoutElement layout = panel.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.minWidth = minWidth;
            layout.flexibleWidth = flexibleWidth;
            AddVerticalContainer(panel, 6f, 8, 8, 8, 8);
            AddSmallTitle(panel.transform, title);
            return panel;
        }

        private static void AddVerticalLayout(GameObject gameObject, float spacing, int left, int right, int top, int bottom)
        {
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(left, right, top, bottom);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void AddMetricTile(Transform parent, string label, string value, Color valueColor, float width)
        {
            GameObject tile = CreatePlainImage("MetricTile", parent, new Color(0.78f, 0.78f, 0.72f, 1f));
            LayoutElement layout = tile.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 42f;
            AddVerticalContainer(tile, 0f, 6, 6, 3, 3);

            TextMeshProUGUI name = CreateText("Label", tile.transform, label, 9, TextAlignmentOptions.MidlineLeft);
            name.color = MutedTextColor();
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

            TextMeshProUGUI amount = CreateText("Value", tile.transform, value, 11, TextAlignmentOptions.MidlineLeft);
            amount.color = valueColor;
            amount.fontStyle = FontStyles.Bold;
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        }

        private void AddSmallTitle(Transform parent, string text)
        {
            TextMeshProUGUI title = CreateText("SectionTitle", parent, text, 11, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.28f, 0.30f, 0.29f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        private void AddPlanLine(Transform parent, string text, int size, FontStyles style, Color color, float height)
        {
            TextMeshProUGUI line = CreateText("PlanLine", parent, text, size, TextAlignmentOptions.MidlineLeft);
            line.color = color;
            line.fontStyle = style;
            line.richText = true;
            line.textWrappingMode = TextWrappingModes.Normal;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private void AddInfoText(Transform parent, string text, float height)
        {
            AddPlanLine(parent, text, 10, FontStyles.Italic, MutedTextColor(), height);
        }

        private void AddChoiceButton(Transform parent, string label, int routeIndex, bool selected, float height)
        {
            GameObject button = CreateStyledButton("ChoiceButton", parent, label, () =>
            {
                selectedRouteIndex = routeIndex;
                lastOrderStatus = null;
                RebuildOrderDetails();
            }, selected ? KleiPinkStyle() : KleiBlueStyle());
            button.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private void AddQuickAmountButton(Transform parent, float amount, string label)
        {
            GameObject button = CreateGameButton("QuickAmount", parent, label, () =>
            {
                requestedProductAmount = amount;
                lastOrderStatus = null;
                RebuildOrderDetails();
            });
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = 44f;
            layout.preferredHeight = 24f;
        }

        private void AddTableHeader(Transform parent, string left, string middle, string right)
        {
            AddTableRow(parent, left, middle, right, new Color(0.18f, 0.19f, 0.18f, 1f), true);
        }

        private void AddTableRow(Transform parent, string left, string middle, string right, Color color, bool header = false)
        {
            GameObject row = CreatePlainImage(header ? "TableHeader" : "TableRow", parent, header ? new Color(0.68f, 0.69f, 0.64f, 1f) : new Color(0.78f, 0.78f, 0.72f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = header ? 24f : 26f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 3, 3);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddTableCell(row.transform, left, 9, header ? FontStyles.Bold : FontStyles.Normal, color, 0f, true);
            AddTableCell(row.transform, middle, 9, header ? FontStyles.Bold : FontStyles.Normal, color, 82f, false);
            AddTableCell(row.transform, right, 9, header ? FontStyles.Bold : FontStyles.Normal, color, 128f, false);
        }

        private void AddTableCell(Transform parent, string text, int size, FontStyles style, Color color, float width, bool flexible)
        {
            TextMeshProUGUI cell = CreateText("Cell", parent, text, size, TextAlignmentOptions.MidlineLeft);
            cell.color = color;
            cell.fontStyle = style;
            cell.textWrappingMode = TextWrappingModes.NoWrap;
            cell.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement layout = cell.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                layout.preferredWidth = width;
            }

            if (flexible)
            {
                layout.flexibleWidth = 1f;
            }
        }

        private void AddProductListText(string text)
        {
            AddInfoText(productListContent, text, 72f);
        }

        private void SelectProduct(string productKey, bool rebuild = true)
        {
            selectedProductKey = productKey;
            selectedRouteIndex = 0;
            lastOrderStatus = null;
            ProductDisplayGroup product = orderProducts.FirstOrDefault(item => item.ProductKey == productKey);
            requestedProductAmount = product?.Routes.Count > 0 ? ProductionRecipeCatalog.GetRecipeResultForProduct(product.Routes[0].Recipe, product.ProductTag)?.amount ?? 1f : 1f;
            if (rebuild)
            {
                RebuildProductList();
                RebuildOrderDetails();
            }
        }

        private ProductDisplayGroup GetSelectedProduct()
        {
            return orderProducts.FirstOrDefault(product => product.ProductKey == selectedProductKey);
        }

        private string BuildOrderTrackingStatus(ProductDisplayGroup product, ProductionOrderDraft draft)
        {
            if (!string.IsNullOrEmpty(lastOrderStatus))
            {
                return lastOrderStatus;
            }

            if (draft.DuplicateOrder != null)
            {
                return string.Format("检测到活动订单 #{0}，提交将合并数量并追加调度。", draft.DuplicateOrder.DisplayId);
            }

            int activeCount = productionOrderService.GetActiveOrdersForProduct(product.ProductTag, 99).Count;
            return activeCount > 0
                ? string.Format("当前成品还有 {0} 个活动订单；本次会创建新的追踪项。", activeCount)
                : "当前没有活动订单；提交后创建新的追踪项。";
        }

        private static bool IsTrackingActive(ProductionOrderRecord order)
        {
            return order.State != ProductionOrderState.Completed &&
                   order.State != ProductionOrderState.Abnormal &&
                   order.State != ProductionOrderState.Cancelled;
        }

        private static string GetRiskLabel(ProductionOrderRiskLevel risk)
        {
            switch (risk)
            {
                case ProductionOrderRiskLevel.Blocked:
                    return "阻塞";
                case ProductionOrderRiskLevel.Warning:
                    return "需调度";
                default:
                    return "可提交";
            }
        }

        private static Color GetRiskColor(ProductionOrderRiskLevel risk)
        {
            switch (risk)
            {
                case ProductionOrderRiskLevel.Blocked:
                    return DangerColor();
                case ProductionOrderRiskLevel.Warning:
                    return WarningColor();
                default:
                    return PositiveColor();
            }
        }

        private static string GetOrderStateLabel(ProductionOrderState state)
        {
            switch (state)
            {
                case ProductionOrderState.Submitted:
                    return "已提交";
                case ProductionOrderState.WaitingMaterials:
                    return "待材料";
                case ProductionOrderState.Producing:
                    return "生产中";
                case ProductionOrderState.Completed:
                    return "完成";
                case ProductionOrderState.Abnormal:
                    return "异常取消";
                case ProductionOrderState.Cancelled:
                    return "取消";
                default:
                    return "追踪中";
            }
        }

        private static Color GetOrderStateColor(ProductionOrderState state)
        {
            switch (state)
            {
                case ProductionOrderState.WaitingMaterials:
                    return WarningColor();
                case ProductionOrderState.Producing:
                    return NeutralBlue();
                case ProductionOrderState.Completed:
                    return PositiveColor();
                case ProductionOrderState.Abnormal:
                    return DangerColor();
                case ProductionOrderState.Cancelled:
                    return new Color(0.42f, 0.42f, 0.42f, 1f);
                default:
                    return NeutralTextColor();
            }
        }

        private static Color PositiveColor()
        {
            return new Color(0.24f, 0.40f, 0.26f, 1f);
        }

        private static Color WarningColor()
        {
            return new Color(0.58f, 0.43f, 0.20f, 1f);
        }

        private static Color DangerColor()
        {
            return new Color(0.68f, 0.18f, 0.14f, 1f);
        }

        private static Color NeutralBlue()
        {
            return new Color(0.20f, 0.34f, 0.46f, 1f);
        }

        private static Color NeutralTextColor()
        {
            return new Color(0.23f, 0.24f, 0.23f, 1f);
        }

        private static Color MutedTextColor()
        {
            return new Color(0.34f, 0.35f, 0.33f, 1f);
        }

        private static string BuildAutomationSummary(ProductionOrderDraft draft)
        {
            if (draft.RiskLevel == ProductionOrderRiskLevel.Blocked)
            {
                return "存在不可满足材料或设备阻塞，提交前需要处理。";
            }

            if (draft.ProducedRequirementCount > 0)
            {
                return string.Format("{0} 项材料会自动补产；提交后接管材料请求和成品回存。", draft.ProducedRequirementCount);
            }

            return "库存可覆盖材料需求；提交后会按设备负载分配批次。";
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        private static void AddIcon(Transform parent, Sprite sprite, float size)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            iconObject.AddComponent<RectTransform>();
            LayoutElement layout = iconObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            Image image = iconObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }
    }
}
