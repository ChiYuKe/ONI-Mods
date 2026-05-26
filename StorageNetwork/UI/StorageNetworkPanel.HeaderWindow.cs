using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {
        private RectTransform productListContent;
        private RectTransform orderDetailsContent;
        private List<ProductDisplayGroup> orderProducts = new List<ProductDisplayGroup>();
        private List<RecipeDisplayInfo> craftableRecipes = new List<RecipeDisplayInfo>();
        private Dictionary<Tag, float> networkAmountCache = new Dictionary<Tag, float>();
        private List<Storage> networkSourceStorageCache = new List<Storage>();
        private string selectedProductKey;
        private int selectedRouteIndex;
        private float requestedProductAmount;
        private KInputTextField orderAmountInput;

        private void ToggleHeaderWindow()
        {
            EnsureHeaderWindow();
            headerWindowRoot.SetActive(!headerWindowRoot.activeSelf);
            if (headerWindowRoot.activeSelf)
            {
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

            headerWindowRoot = CreateBox("ProductionOrderPanel", windowRect, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(headerWindowRoot.GetComponent<Image>());
            RectTransform panelRect = headerWindowRoot.GetComponent<RectTransform>();
            SetStretch(panelRect, 8f, 8f, 8f, 42f);

            GameObject header = CreateBox("Header", headerWindowRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 42f);

            TextMeshProUGUI title = CreateText("Title", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEADER_WINDOW_TITLE), 14, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);
            title.rectTransform().offsetMax = new Vector2(-42f, 0f);

            GameObject closeButton = CreateGameButton("CloseButton", header.transform, "X", CloseHeaderWindow);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-10f, 0f);
            closeRect.sizeDelta = new Vector2(24f, 22f);

            GameObject body = CreateBox("Body", headerWindowRoot.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 10f, 10f, 10f, 58f);

            HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.padding = new RectOffset(10, 10, 10, 10);
            bodyLayout.spacing = 10f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = false;
            bodyLayout.childForceExpandHeight = true;

            CreateProductListPane(body.transform);
            CreateOrderDetailsPane(body.transform);

            headerWindowRoot.SetActive(false);
        }

        private void CreateProductListPane(Transform parent)
        {
            GameObject pane = CreateBox("ProductPane", parent, new Color(0.72f, 0.72f, 0.66f, 1f));
            LayoutElement paneLayout = pane.AddComponent<LayoutElement>();
            paneLayout.preferredWidth = 260f;
            paneLayout.minWidth = 240f;
            paneLayout.flexibleWidth = 0f;

            GameObject title = CreateBox("ProductPaneTitle", pane.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(title.GetComponent<RectTransform>(), 6f, 6f, 6f, 30f);
            TextMeshProUGUI titleText = CreateText("Title", title.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_LIST_TITLE), 13, TextAlignmentOptions.MidlineLeft);
            titleText.fontStyle = FontStyles.Bold;
            Stretch(titleText.rectTransform(), 10f, 0f);

            GameObject viewport = CreateBox("ProductViewport", pane.transform, new Color(0.82f, 0.81f, 0.76f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 6f, 6f, 6f, 42f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("ProductContent");
            content.transform.SetParent(viewport.transform, false);
            productListContent = content.AddComponent<RectTransform>();
            productListContent.anchorMin = new Vector2(0f, 1f);
            productListContent.anchorMax = new Vector2(1f, 1f);
            productListContent.pivot = new Vector2(0.5f, 1f);
            productListContent.offsetMin = Vector2.zero;
            productListContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(pane.transform);
            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = productListContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
            viewport.AddComponent<ScrollWheelBlocker>();
        }

        private void CreateOrderDetailsPane(Transform parent)
        {
            GameObject pane = CreateBox("OrderDetailsPane", parent, new Color(0.84f, 0.83f, 0.78f, 1f));
            pane.AddComponent<LayoutElement>().flexibleWidth = 1f;

            orderDetailsContent = pane.GetComponent<RectTransform>();
        }

        private void RefreshOrderPanel(bool rebuildProducts)
        {
            if (rebuildProducts)
            {
                RefreshNetworkStorageCache();
                craftableRecipes = GetCraftableRecipeDisplayInfos();
                orderProducts = BuildProductGroups(craftableRecipes);
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

        private void RebuildProductList()
        {
            ClearChildren(productListContent);
            if (orderProducts.Count == 0)
            {
                AddProductListText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_WINDOW_EMPTY));
                return;
            }

            foreach (ProductDisplayGroup product in orderProducts.Take(80))
            {
                CreateProductButton(product);
            }
        }

        private void CreateProductButton(ProductDisplayGroup product)
        {
            bool selected = product.ProductKey == selectedProductKey;
            GameObject button = CreateStyledButton("ProductButton", productListContent, string.Empty, () => SelectProduct(product.ProductKey), selected ? KleiPinkStyle() : KleiBlueStyle());
            button.AddComponent<LayoutElement>().preferredHeight = 54f;

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
            VerticalLayoutGroup textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 1f;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            TextMeshProUGUI name = CreateText("Name", textColumn.transform, product.ProductName, 12, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            TextMeshProUGUI meta = CreateText(
                "Meta",
                textColumn.transform,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_META), GameUtil.GetFormattedMass(GetNetworkAvailableAmount(product.ProductTag)), product.Routes.Count),
                10,
                TextAlignmentOptions.MidlineLeft);
            meta.color = new Color(0.78f, 0.82f, 0.84f, 1f);
            meta.textWrappingMode = TextWrappingModes.NoWrap;
            meta.overflowMode = TextOverflowModes.Ellipsis;
            meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        }

        private void AddProductListText(string text)
        {
            TextMeshProUGUI label = CreateText("Info", productListContent, text, 12, TextAlignmentOptions.TopLeft);
            label.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        }

        private void RefreshNetworkStorageCache()
        {
            networkSourceStorageCache = StorageSceneCollector.Collect().Storages
                .SelectMany(info => info.ContentStorages)
                .Where(storage => storage != null)
                .Distinct()
                .ToList();
            networkAmountCache = new Dictionary<Tag, float>();
            foreach (GameObject item in networkSourceStorageCache
                .Where(storage => storage.GetComponent<ComplexFabricator>() == null)
                .SelectMany(storage => storage.items.Where(item => item != null)))
            {
                PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                if (primaryElement == null)
                {
                    continue;
                }

                AddNetworkAmount(primaryElement.ElementID.CreateTag(), primaryElement.Mass);

                KPrefabID prefabID = item.GetComponent<KPrefabID>();
                if (prefabID != null)
                {
                    AddNetworkAmount(prefabID.PrefabTag, primaryElement.Mass);
                }
            }
        }

        private void AddNetworkAmount(Tag tag, float amount)
        {
            if (tag == Tag.Invalid || amount <= 0f)
            {
                return;
            }

            networkAmountCache[tag] = networkAmountCache.TryGetValue(tag, out float existing) ? existing + amount : amount;
        }

        private void RebuildOrderDetails()
        {
            ClearChildren(orderDetailsContent);
            ProductDisplayGroup product = GetSelectedProduct();
            if (product == null)
            {
                AddDetailsInfo(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_WINDOW_EMPTY));
                return;
            }

            selectedRouteIndex = Mathf.Clamp(selectedRouteIndex, 0, product.Routes.Count - 1);
            RecipeDisplayInfo route = product.Routes[selectedRouteIndex];
            if (requestedProductAmount <= 0f)
            {
                requestedProductAmount = GetRecipeResultForProduct(route.Recipe, product.ProductTag)?.amount ?? 1f;
            }

            AddDetailsHeader(product);
            AddAmountControls(product);
            AddExecutionWorkspace(product, route);
            AddConfirmButton(route);
        }

        private void AddDetailsHeader(ProductDisplayGroup product)
        {
            GameObject header = CreatePlainImage("DetailsHeader", orderDetailsContent, new Color(0.74f, 0.74f, 0.68f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 10f, 10f, 10f, 82f);

            HorizontalLayoutGroup layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 9, 9);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIcon(header.transform, product.Icon, 54f);

            GameObject textColumn = new GameObject("TextColumn");
            textColumn.transform.SetParent(header.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            TextMeshProUGUI name = CreateText("ProductName", textColumn.transform, product.ProductName, 18, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.12f, 0.13f, 0.12f, 1f);
            name.fontStyle = FontStyles.Bold;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            TextMeshProUGUI meta = CreateText(
                "ProductMeta",
                textColumn.transform,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_PRODUCT_STOCK), GameUtil.GetFormattedMass(GetNetworkAvailableAmount(product.ProductTag)), FormatRecipeElement(GetRecipeResultForProduct(product.Routes[0].Recipe, product.ProductTag))),
                12,
                TextAlignmentOptions.MidlineLeft);
            meta.color = new Color(0.28f, 0.30f, 0.30f, 1f);
            meta.textWrappingMode = TextWrappingModes.NoWrap;
            meta.overflowMode = TextOverflowModes.Ellipsis;
            meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        }

        private void AddAmountControls(ProductDisplayGroup product)
        {
            GameObject amountPanel = CreatePlainImage("AmountPanel", orderDetailsContent, new Color(0.84f, 0.83f, 0.78f, 1f));
            SetTopStretch(amountPanel.GetComponent<RectTransform>(), 10f, 10f, 96f, 50f);

            HorizontalLayoutGroup layout = amountPanel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI label = CreateText("AmountLabel", amountPanel.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AMOUNT_LABEL), 12, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 76f;

            orderAmountInput = CreateAmountInput(amountPanel.transform);
            orderAmountInput.text = FormatAmount(requestedProductAmount);
            orderAmountInput.onEndEdit.AddListener(value =>
            {
                if (TryParseAmount(value, out float parsed) && parsed > 0f)
                {
                    requestedProductAmount = parsed;
                    RebuildOrderDetails();
                }
            });
            LayoutElement inputLayout = orderAmountInput.GetComponent<LayoutElement>();
            inputLayout.preferredWidth = 130f;

            AddQuickAmountButton(amountPanel.transform, 100f, "100kg");
            AddQuickAmountButton(amountPanel.transform, 500f, "500kg");
            AddQuickAmountButton(amountPanel.transform, 1000f, "1t");

            AddFooterSpacer(amountPanel.transform);
        }

        private void AddQuickAmountButton(Transform parent, float amount, string label)
        {
            GameObject button = CreateGameButton("QuickAmount", parent, label, () =>
            {
                requestedProductAmount = amount;
                RebuildOrderDetails();
            });
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = 58f;
            layout.preferredHeight = 24f;
        }

        private void AddRouteSelector(ProductDisplayGroup product)
        {
            GameObject routePanel = CreatePlainImage("RoutePanel", orderDetailsContent, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetTopStretch(routePanel.GetComponent<RectTransform>(), 10f, 10f, 150f, 104f);

            VerticalLayoutGroup layout = routePanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            AddCompactSectionTitle(routePanel.transform, string.Format("{0} ({1})", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_TITLE), product.Routes.Select(route => route.FabricatorName).Distinct().Count()));
            foreach (IGrouping<string, RecipeDisplayInfo> group in product.Routes.GroupBy(route => route.FabricatorName))
            {
                int index = product.Routes.FindIndex(route => route.FabricatorName == group.Key);
                bool selected = product.Routes[selectedRouteIndex].FabricatorName == group.Key;
                CreateRouteButton(routePanel.transform, group.Key, index, selected);
            }
        }

        private void CreateRouteButton(Transform parent, string fabricatorName, int index, bool selected)
        {
            GameObject row = CreateStyledButton("RouteButton", parent, selected ? "✓ " + fabricatorName : fabricatorName, () =>
            {
                selectedRouteIndex = index;
                RebuildOrderDetails();
            }, selected ? KleiPinkStyle() : KleiBlueStyle());
            row.AddComponent<LayoutElement>().preferredHeight = 24f;
        }

        private void AddExecutionWorkspace(ProductDisplayGroup product, RecipeDisplayInfo route)
        {
            GameObject workspace = CreatePlainImage("ExecutionWorkspace", orderDetailsContent, new Color(0.80f, 0.80f, 0.74f, 1f));
            SetStretch(workspace.GetComponent<RectTransform>(), 10f, 10f, 64f, 150f);

            HorizontalLayoutGroup layout = workspace.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject configPane = CreatePlainImage("OrderConfigPane", workspace.transform, new Color(0.86f, 0.85f, 0.79f, 1f));
            LayoutElement configLayout = configPane.AddComponent<LayoutElement>();
            configLayout.preferredWidth = 280f;
            configLayout.minWidth = 260f;
            AddVerticalContainer(configPane, 4f, 8, 8, 8, 8);
            AddCompactSectionTitle(configPane.transform, "订单配置");
            AddDeviceSelector(configPane.transform, product);
            AddRecipeSelector(configPane.transform, product, route);

            GameObject resultPane = CreatePlainImage("OrderResultPane", workspace.transform, new Color(0.86f, 0.85f, 0.79f, 1f));
            resultPane.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(resultPane, 3f, 8, 8, 8, 8);
            AddPlanDashboard(resultPane.transform, product, route);
        }

        private void AddCompactSectionTitle(Transform parent, string text)
        {
            TextMeshProUGUI title = CreateText("SectionTitle", parent, text, 11, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.28f, 0.30f, 0.29f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        private void AddDeviceSelector(Transform parent, ProductDisplayGroup product)
        {
            AddCompactSectionTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_TITLE));
            foreach (IGrouping<string, RecipeDisplayInfo> group in product.Routes.GroupBy(route => route.FabricatorName).Take(4))
            {
                int index = product.Routes.FindIndex(route => route.FabricatorName == group.Key);
                bool selected = product.Routes[selectedRouteIndex].FabricatorName == group.Key;
                CreateRouteButton(parent, group.Key, index, selected);
            }
        }

        private void AddPlanDashboard(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route)
        {
            ProductionPlanNode plan = BuildProductionPlan(route.Recipe, route.Fabricators, product.ProductTag, requestedProductAmount, 0);
            AddCompactSectionTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_PLAN_TITLE));
            AddPlanLine(parent, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_PLAN_SUMMARY), GameUtil.GetFormattedMass(requestedProductAmount), plan.OrderCount), 10, FontStyles.Bold, new Color(0.16f, 0.17f, 0.16f, 1f));
            AddInfoChipRow(parent, plan);
            AddPlanTimeSummary(parent, plan);

            AddCompactSectionTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ASSIGNMENT_TITLE));
            foreach (string assignmentLine in FormatAssignmentLines(plan).Take(3))
            {
                AddPlanLine(parent, assignmentLine, 9, FontStyles.Normal, new Color(0.18f, 0.24f, 0.28f, 1f));
            }

            AddCompactSectionTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MATERIAL_TITLE));
            foreach (ProductionPlanRequirement requirement in plan.Requirements.Take(4))
            {
                AddMaterialRequirementLine(parent, requirement);
            }
        }

        private void AddPlanPreview(ProductDisplayGroup product, RecipeDisplayInfo route)
        {
            GameObject planPanel = CreatePlainImage("PlanPanel", orderDetailsContent, new Color(0.72f, 0.72f, 0.66f, 1f));
            SetStretch(planPanel.GetComponent<RectTransform>(), 10f, 10f, 64f, 260f);

            VerticalLayoutGroup layout = planPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText("PlanTitle", planPanel.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_PLAN_TITLE), 13, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            AddRecipeSelector(planPanel.transform, product, route);
            ProductionPlanNode plan = BuildProductionPlan(route.Recipe, route.Fabricators, product.ProductTag, requestedProductAmount, 0);
            AddPlanLine(planPanel.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_PLAN_SUMMARY), GameUtil.GetFormattedMass(requestedProductAmount), plan.OrderCount), 11, FontStyles.Bold, new Color(0.18f, 0.19f, 0.19f, 1f));
            AddInfoChipRow(planPanel.transform, plan);
            AddPlanLine(planPanel.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_TITLE), 10, FontStyles.Bold, new Color(0.18f, 0.19f, 0.19f, 1f));
            AddPlanLine(planPanel.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_SUMMARY), 9, FontStyles.Normal, new Color(0.24f, 0.26f, 0.25f, 1f));
            AddPlanLine(planPanel.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ASSIGNMENT_TITLE), 10, FontStyles.Bold, new Color(0.18f, 0.19f, 0.19f, 1f));
            foreach (string assignmentLine in FormatAssignmentLines(plan).Take(4))
            {
                AddPlanLine(planPanel.transform, assignmentLine, 10, FontStyles.Normal, new Color(0.18f, 0.24f, 0.28f, 1f));
            }

            AddPlanTimeSummary(planPanel.transform, plan);
            AddPlanLine(planPanel.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MATERIAL_TITLE), 11, FontStyles.Bold, new Color(0.18f, 0.19f, 0.19f, 1f));
            foreach (ProductionPlanRequirement requirement in plan.Requirements.Take(6))
            {
                AddMaterialRequirementLine(planPanel.transform, requirement);
            }

            AddPlanLine(planPanel.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CHAIN_TITLE), 10, FontStyles.Bold, new Color(0.18f, 0.19f, 0.19f, 1f));
            foreach (string line in FormatPlanLines(plan, 0).Take(4))
            {
                AddPlanLine(planPanel.transform, line, 10, FontStyles.Normal, new Color(0.24f, 0.25f, 0.25f, 1f));
            }
        }

        private void AddInfoChipRow(Transform parent, ProductionPlanNode plan)
        {
            GameObject row = new GameObject("PlanChipRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 24f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddInfoChip(row.transform, string.Format("设备 {0}", plan.Assignments.Count), new Color(0.48f, 0.54f, 0.56f, 1f));
            AddInfoChip(row.transform, string.Format("批次 {0}", plan.OrderCount), new Color(0.56f, 0.54f, 0.46f, 1f));
            AddInfoChip(row.transform, HasBlockedRequirement(plan) ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_BLOCKED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_READY), HasBlockedRequirement(plan) ? new Color(0.62f, 0.38f, 0.32f, 1f) : new Color(0.40f, 0.58f, 0.44f, 1f));
        }

        private static bool HasBlockedRequirement(ProductionPlanNode plan)
        {
            return plan.Requirements.Any(requirement =>
                requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
                requirement.Child == null);
        }

        private void AddInfoChip(Transform parent, string text, Color color)
        {
            GameObject chip = CreatePlainImage("InfoChip", parent, color);
            LayoutElement layout = chip.AddComponent<LayoutElement>();
            layout.preferredWidth = 72f;
            layout.preferredHeight = 18f;
            TextMeshProUGUI label = CreateText("Label", chip.transform, text, 10, TextAlignmentOptions.Center);
            label.color = new Color(0.12f, 0.13f, 0.12f, 1f);
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform(), 5f, 0f);
        }

        private static IEnumerable<string> FormatAssignmentLines(ProductionPlanNode plan)
        {
            if (plan == null || plan.Assignments.Count == 0)
            {
                yield return "无可用生产建筑";
                yield break;
            }

            foreach (ProductionPlanAssignment assignment in plan.Assignments)
            {
                yield return string.Format("{0}    {1}    {2} 批次", assignment.Fabricator.GetProperName(), GameUtil.GetFormattedMass(assignment.OutputAmount), assignment.OrderCount);
            }
        }

        private void AddRecipeSelector(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo selectedRoute)
        {
            List<RecipeDisplayInfo> alternatives = product.Routes
                .Where(route => route.FabricatorName == selectedRoute.FabricatorName)
                .ToList();
            if (alternatives.Count <= 1)
            {
                return;
            }

            AddCompactSectionTitle(parent, string.Format("{0} ({1})", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RECIPE_TITLE), alternatives.Count));
            foreach (RecipeDisplayInfo route in alternatives.Take(4))
            {
                int index = product.Routes.IndexOf(route);
                string label = FormatRecipeElements(route.Recipe.ingredients);
                GameObject button = CreateStyledButton("RecipeChoice", parent, index == selectedRouteIndex ? "✓ " + label : label, () =>
                {
                    selectedRouteIndex = index;
                    RebuildOrderDetails();
                }, index == selectedRouteIndex ? KleiPinkStyle() : KleiBlueStyle());
                button.AddComponent<LayoutElement>().preferredHeight = 22f;
            }
        }

        private void AddPlanTimeSummary(Transform parent, ProductionPlanNode plan)
        {
            float currentCycle = GetCurrentCycleTime();
            float estimatedSeconds = EstimatePlanSeconds(plan, out bool hasInfiniteQueue);
            string text = hasInfiniteQueue
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TIME_UNKNOWN), FormatCycle(currentCycle))
                : string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TIME_SUMMARY),
                    FormatCycle(currentCycle),
                    FormatCycle(currentCycle + estimatedSeconds / 600f));
            AddPlanLine(parent, text, 10, FontStyles.Normal, new Color(0.22f, 0.24f, 0.24f, 1f));
        }

        private void AddMaterialRequirementLine(Transform parent, ProductionPlanRequirement requirement)
        {
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
            bool covered = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool canProduce = !covered && requirement.Child != null;
            string status = covered
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_READY)
                : canProduce
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_PRODUCE)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_BLOCKED);
            Color color = covered
                ? new Color(0.24f, 0.38f, 0.28f, 1f)
                : canProduce
                    ? new Color(0.44f, 0.36f, 0.22f, 1f)
                    : new Color(0.48f, 0.26f, 0.22f, 1f);

            string text = string.Format(
                "[{0}] {1}    需求 {2}    库存 {3}    缺口 {4}",
                status,
                GetTagDisplayName(requirement.Material),
                GameUtil.GetFormattedMass(requirement.RequiredAmount),
                GameUtil.GetFormattedMass(Mathf.Min(requirement.AvailableAmount, requirement.RequiredAmount)),
                GameUtil.GetFormattedMass(missing));
            AddPlanLine(parent, text, 10, covered ? FontStyles.Normal : FontStyles.Bold, color);

            if (requirement.Child != null)
            {
                AddPlanLine(
                    parent,
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MATERIAL_PRODUCED), requirement.Child.Recipe.GetUIName(false)),
                    9,
                    FontStyles.Italic,
                    new Color(0.25f, 0.28f, 0.30f, 1f));
            }
        }

        private void AddPlanLine(Transform parent, string text, int size, FontStyles style, Color color)
        {
            TextMeshProUGUI line = CreateText("PlanLine", parent, text, size, TextAlignmentOptions.MidlineLeft);
            line.color = color;
            line.fontStyle = style;
            line.richText = true;
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        private void AddConfirmButton(RecipeDisplayInfo route)
        {
            GameObject footer = CreatePlainImage("Footer", orderDetailsContent, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetBottomStretch(footer.GetComponent<RectTransform>(), 10f, 10f, 10f, 44f);

            HorizontalLayoutGroup layout = footer.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 7, 7);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddFooterSpacer(footer.transform);
            GameObject button = CreateGameButton("ConfirmOrder", footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CONFIRM), () =>
            {
                ProductDisplayGroup product = GetSelectedProduct();
                ProductionPlanNode plan = BuildProductionPlan(route.Recipe, route.Fabricators, product?.ProductTag ?? Tag.Invalid, requestedProductAmount, 0);
                if (plan.Assignments.Count == 0)
                {
                    return;
                }

                ApplyProductionPlan(plan);
                RebuildOrderDetails();
            });
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 140f;
            buttonLayout.preferredHeight = 28f;
        }

        private void AddDetailsInfo(string text)
        {
            TextMeshProUGUI label = CreateText("DetailsInfo", orderDetailsContent, text, 12, TextAlignmentOptions.TopLeft);
            label.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            Stretch(label.rectTransform(), 14f, 14f);
        }

        private void SelectProduct(string productKey, bool rebuild = true)
        {
            selectedProductKey = productKey;
            selectedRouteIndex = 0;
            ProductDisplayGroup product = orderProducts.FirstOrDefault(item => item.ProductKey == productKey);
            requestedProductAmount = product?.Routes.Count > 0 ? GetRecipeResultForProduct(product.Routes[0].Recipe, product.ProductTag)?.amount ?? 1f : 1f;
            if (rebuild)
            {
                RebuildOrderDetails();
            }
        }

        private ProductDisplayGroup GetSelectedProduct()
        {
            return orderProducts.FirstOrDefault(product => product.ProductKey == selectedProductKey);
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

        private static List<ProductDisplayGroup> BuildProductGroups(List<RecipeDisplayInfo> recipes)
        {
            return recipes
                .GroupBy(recipe => recipe.ProductKey)
                .Select(group => new ProductDisplayGroup(group.Key, group.OrderBy(recipe => recipe.FabricatorName).ThenBy(recipe => recipe.Name).ToList()))
                .OrderBy(group => group.ProductName)
                .ToList();
        }

        private static List<RecipeDisplayInfo> GetCraftableRecipeDisplayInfos()
        {
            Dictionary<string, List<ComplexFabricator>> recipeFabricators = new Dictionary<string, List<ComplexFabricator>>();
            Dictionary<string, ComplexRecipe> recipeByKey = new Dictionary<string, ComplexRecipe>();
            foreach (ComplexFabricator fabricator in Object.FindObjectsByType<ComplexFabricator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (fabricator == null)
                {
                    continue;
                }

                StorageNetworkEnrollment enrollment = fabricator.GetComponent<StorageNetworkEnrollment>();
                if (enrollment == null || !enrollment.IncludedInSceneNetwork)
                {
                    continue;
                }

                foreach (ComplexRecipe recipe in fabricator.GetRecipes())
                {
                    if (recipe == null || !recipe.IsRequiredTechUnlocked())
                    {
                        continue;
                    }

                    string key = recipe.id ?? recipe.GetUIName(false);
                    if (!recipeFabricators.TryGetValue(key, out List<ComplexFabricator> fabricators))
                    {
                        fabricators = new List<ComplexFabricator>();
                        recipeFabricators.Add(key, fabricators);
                        recipeByKey.Add(key, recipe);
                    }

                    fabricators.Add(fabricator);
                }
            }

            return recipeFabricators
                .Select(pair =>
                {
                    ComplexRecipe recipe = recipeByKey[pair.Key];
                    List<ComplexFabricator> fabricators = pair.Value.OrderBy(fabricator => fabricator.gameObject.GetProperName()).ToList();
                    return new RecipeDisplayInfo(
                        recipe.GetUIName(false),
                        FormatFabricatorGroupName(fabricators),
                        FormatRecipeDetails(recipe),
                        recipe,
                        fabricators,
                        recipe.GetUIIcon(),
                        GetProductKey(recipe),
                        GetProductDisplayName(recipe),
                        GetProductTag(recipe));
                })
                .OrderBy(recipe => recipe.ProductName)
                .ThenBy(recipe => recipe.FabricatorName)
                .ThenBy(recipe => recipe.Name)
                .ToList();
        }

        private static string FormatFabricatorGroupName(List<ComplexFabricator> fabricators)
        {
            if (fabricators == null || fabricators.Count == 0)
            {
                return "?";
            }

            string firstName = fabricators[0].gameObject.GetProperName();
            return fabricators.Count == 1 ? firstName : string.Format("{0} x{1}", firstName, fabricators.Count);
        }

        private static string FormatRecipeDetails(ComplexRecipe recipe)
        {
            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_WINDOW_DETAILS),
                FormatRecipeElements(recipe.ingredients),
                FormatRecipeElements(recipe.results));
        }

        private static string FormatRecipeElements(IEnumerable<ComplexRecipe.RecipeElement> elements)
        {
            return elements == null ? string.Empty : string.Join("  +  ", elements.Select(FormatRecipeElement));
        }

        private static string FormatRecipeElement(ComplexRecipe.RecipeElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            return string.Format("{0} {1}", GameUtil.GetFormattedMass(element.amount), GetRecipeElementName(element));
        }

        private static string GetRecipeElementName(ComplexRecipe.RecipeElement element)
        {
            if (element.material != Tag.Invalid)
            {
                return GetTagDisplayName(element.material);
            }

            return element.possibleMaterials != null && element.possibleMaterials.Length > 0
                ? string.Join("/", element.possibleMaterials.Select(GetTagDisplayName).ToArray())
                : "?";
        }

        private static string GetTagDisplayName(Tag tag)
        {
            Element element = ElementLoader.FindElementByHash((SimHashes)tag.GetHash());
            if (element != null && !string.IsNullOrEmpty(element.name))
            {
                return element.name;
            }

            GameObject prefab = Assets.GetPrefab(tag);
            if (prefab != null)
            {
                return prefab.GetProperName();
            }

            string key = "STRINGS.MISC.TAGS." + tag.Name.ToUpperInvariant();
            if (Strings.TryGet(key, out StringEntry entry) && entry != null && !string.IsNullOrEmpty(entry.String))
            {
                return entry.String;
            }

            return tag.Name;
        }

        private ProductionPlanNode BuildProductionPlan(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount, int depth)
        {
            ComplexRecipe.RecipeElement result = GetRecipeResultForProduct(recipe, productTag) ?? GetPrimaryResult(recipe);
            float outputAmount = result != null ? Mathf.Max(result.amount, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) : 1f;
            int orderCount = Mathf.Max(1, Mathf.CeilToInt(requestedAmount / outputAmount));
            ProductionPlanNode node = new ProductionPlanNode(recipe, fabricators, productTag, outputAmount, orderCount);
            if (recipe.ingredients == null || depth >= 4)
            {
                return node;
            }

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                Tag tag = GetPreferredMaterial(ingredient);
                float required = ingredient.amount * orderCount;
                float available = GetNetworkAvailableAmount(tag);
                RecipeDisplayInfo producer = FindConnectedRecipeProducing(tag);
                ProductionPlanRequirement requirement = new ProductionPlanRequirement(tag, required, available);
                if (available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required && producer.Recipe != null)
                {
                    requirement.Child = BuildProductionPlan(producer.Recipe, producer.Fabricators, tag, required - available, depth + 1);
                }

                node.Requirements.Add(requirement);
            }

            return node;
        }

        private List<string> FormatPlanLines(ProductionPlanNode node, int depth)
        {
            List<string> lines = new List<string>();
            string indent = new string(' ', depth * 4);
            lines.Add(string.Format("{0}<b>{1}</b> x{2} -> {3}", indent, node.Recipe.GetUIName(false), node.OrderCount, node.FabricatorName));
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
                lines.Add(string.Format(
                    "{0}{1}: {2}/{3}{4}",
                    indent,
                    GetTagDisplayName(requirement.Material),
                    GameUtil.GetFormattedMass(Mathf.Min(requirement.AvailableAmount, requirement.RequiredAmount)),
                    GameUtil.GetFormattedMass(requirement.RequiredAmount),
                    missing > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? string.Format("  缺 {0}", GameUtil.GetFormattedMass(missing)) : string.Empty));

                if (requirement.Child != null)
                {
                    lines.AddRange(FormatPlanLines(requirement.Child, depth + 1));
                }
            }

            return lines;
        }

        private static float EstimatePlanSeconds(ProductionPlanNode node, out bool hasInfiniteQueue)
        {
            hasInfiniteQueue = false;
            if (node == null || node.Recipe == null)
            {
                return 0f;
            }

            float seconds = 0f;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child == null)
                {
                    continue;
                }

                seconds += EstimatePlanSeconds(requirement.Child, out bool childHasInfiniteQueue);
                hasInfiniteQueue |= childHasInfiniteQueue;
            }

            seconds += EstimateQueuedSeconds(node, out bool fabricatorHasInfiniteQueue);
            hasInfiniteQueue |= fabricatorHasInfiniteQueue;
            int busiestAssignedCount = node.Assignments.Count == 0 ? node.OrderCount : node.Assignments.Max(assignment => assignment.OrderCount);
            seconds += Mathf.Max(0f, node.Recipe.time) * busiestAssignedCount;
            return seconds;
        }

        private static float EstimateQueuedSeconds(ProductionPlanNode node, out bool hasInfiniteQueue)
        {
            hasInfiniteQueue = false;
            if (node == null || node.Recipe == null || node.Assignments.Count == 0)
            {
                return 0f;
            }

            float maxQueuedSeconds = 0f;
            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                int queued = assignment.Fabricator.GetRecipeQueueCount(node.Recipe);
                if (queued == ComplexFabricator.QUEUE_INFINITE)
                {
                    hasInfiniteQueue = true;
                    continue;
                }

                maxQueuedSeconds = Mathf.Max(maxQueuedSeconds, Mathf.Max(0, queued) * Mathf.Max(0f, node.Recipe.time));
            }

            return maxQueuedSeconds;
        }

        private static string FormatCycle(float cycle)
        {
            return cycle.ToString("0.0");
        }

        private void ApplyProductionPlan(ProductionPlanNode node)
        {
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child != null)
                {
                    ApplyProductionPlan(requirement.Child);
                }
            }

            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                int queued = assignment.Fabricator.GetRecipeQueueCount(node.Recipe);
                assignment.Fabricator.SetRecipeQueueCount(node.Recipe, (queued == ComplexFabricator.QUEUE_INFINITE ? 0 : Mathf.Max(0, queued)) + assignment.OrderCount);
                EnsureMaterialRequestEnabled(assignment.Fabricator);
                DispatchRecipeIngredients(node, assignment);
            }
        }

        private static void EnsureMaterialRequestEnabled(ComplexFabricator fabricator)
        {
            StorageNetworkMaterialRequester requester = fabricator != null ? fabricator.GetComponent<StorageNetworkMaterialRequester>() : null;
            if (requester != null)
            {
                requester.RequestEnabled = true;
            }
        }

        private void DispatchRecipeIngredients(ProductionPlanNode node, ProductionPlanAssignment assignment)
        {
            Storage target = assignment.Fabricator.inStorage;
            if (target == null)
            {
                return;
            }

            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                float required = requirement.RequiredAmount * assignment.OrderCount / Mathf.Max(1, node.OrderCount);
                float needed = Mathf.Max(0f, required - target.GetAmountAvailable(requirement.Material));
                TransferMaterialToStorage(requirement.Material, target, needed);
            }
        }

        private float TransferMaterialToStorage(Tag tag, Storage target, float amount)
        {
            float moved = 0f;
            if (target == null || amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return moved;
            }

            foreach (Storage source in GetNetworkSourceStorages()
                .Where(storage => storage != target && storage.GetComponent<ComplexFabricator>() == null && storage.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .OrderByDescending(storage => storage.GetAmountAvailable(tag)))
            {
                float transferAmount = Mathf.Min(amount - moved, source.GetAmountAvailable(tag), Mathf.Max(0f, target.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                moved += source.Transfer(target, tag, transferAmount, block_events: false, hide_popups: true);
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }
            }

            return moved;
        }

        private float GetNetworkAvailableAmount(Tag tag)
        {
            return networkAmountCache.TryGetValue(tag, out float amount) ? amount : 0f;
        }

        private IEnumerable<Storage> GetNetworkSourceStorages()
        {
            return networkSourceStorageCache;
        }

        private RecipeDisplayInfo FindConnectedRecipeProducing(Tag tag)
        {
            return craftableRecipes
                .FirstOrDefault(info => info.Recipe != null && info.Recipe.results != null && info.Recipe.results.Any(result => result != null && result.material == tag));
        }

        private static ComplexRecipe.RecipeElement GetPrimaryResult(ComplexRecipe recipe)
        {
            return recipe?.results?.FirstOrDefault();
        }

        private static ComplexRecipe.RecipeElement GetRecipeResultForProduct(ComplexRecipe recipe, Tag productTag)
        {
            if (recipe?.results == null || productTag == Tag.Invalid)
            {
                return null;
            }

            return recipe.results.FirstOrDefault(result => result != null && result.material == productTag);
        }

        private static Tag GetRecipeResultTag(ComplexRecipe.RecipeElement result)
        {
            return result != null && result.material != Tag.Invalid ? result.material : Tag.Invalid;
        }

        private static string GetProductKey(ComplexRecipe recipe)
        {
            ComplexRecipe.RecipeElement result = GetPrimaryResult(recipe);
            if (result == null)
            {
                return recipe?.id ?? string.Empty;
            }

            return !string.IsNullOrEmpty(result.facadeID) ? result.facadeID : result.material.Name;
        }

        private static Tag GetProductTag(ComplexRecipe recipe)
        {
            return GetRecipeResultTag(GetPrimaryResult(recipe));
        }

        private static string GetProductDisplayName(ComplexRecipe recipe)
        {
            ComplexRecipe.RecipeElement result = GetPrimaryResult(recipe);
            if (result == null)
            {
                return recipe?.GetUIName(false) ?? string.Empty;
            }

            return !string.IsNullOrEmpty(result.facadeID)
                ? GetTagDisplayName(result.facadeID.ToTag())
                : GetTagDisplayName(result.material);
        }

        private Tag GetPreferredMaterial(ComplexRecipe.RecipeElement element)
        {
            if (element.material != Tag.Invalid)
            {
                return element.material;
            }

            return element.possibleMaterials == null || element.possibleMaterials.Length == 0
                ? Tag.Invalid
                : element.possibleMaterials.OrderByDescending(material => GetNetworkAvailableAmount(material)).FirstOrDefault();
        }

        private static void SetBottomStretch(RectTransform rectTransform, float left, float right, float bottom, float height)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, bottom + height);
        }

        private struct RecipeDisplayInfo
        {
            public RecipeDisplayInfo(string name, string fabricatorName, string details, ComplexRecipe recipe, List<ComplexFabricator> fabricators, Sprite icon, string productKey, string productName, Tag productTag)
            {
                Name = name;
                FabricatorName = fabricatorName;
                Details = details;
                Recipe = recipe;
                Fabricators = fabricators ?? new List<ComplexFabricator>();
                Icon = icon;
                ProductKey = productKey;
                ProductName = productName;
                ProductTag = productTag;
            }

            public string Name { get; }

            public string FabricatorName { get; }

            public string Details { get; }

            public ComplexRecipe Recipe { get; }

            public List<ComplexFabricator> Fabricators { get; }

            public Sprite Icon { get; }

            public string ProductKey { get; }

            public string ProductName { get; }

            public Tag ProductTag { get; }
        }

        private sealed class ProductDisplayGroup
        {
            public ProductDisplayGroup(string productKey, List<RecipeDisplayInfo> routes)
            {
                ProductKey = productKey;
                Routes = routes;
            }

            public string ProductKey { get; }

            public List<RecipeDisplayInfo> Routes { get; }

            public string ProductName => Routes.Count > 0 ? Routes[0].ProductName : ProductKey;

            public Tag ProductTag => Routes.Count > 0 ? Routes[0].ProductTag : Tag.Invalid;

            public Sprite Icon => Routes.Count > 0 ? Routes[0].Icon : null;

        }

        private sealed class ProductionPlanNode
        {
            public ProductionPlanNode(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float outputAmount, int orderCount)
            {
                Recipe = recipe;
                Fabricators = fabricators?.Where(fabricator => fabricator != null).ToList() ?? new List<ComplexFabricator>();
                ProductTag = productTag;
                OutputAmount = outputAmount;
                OrderCount = orderCount;
                Assignments = BuildAssignments(Recipe, Fabricators, outputAmount, orderCount);
            }

            public ComplexRecipe Recipe { get; }

            public List<ComplexFabricator> Fabricators { get; }

            public Tag ProductTag { get; }

            public float OutputAmount { get; }

            public int OrderCount { get; }

            public List<ProductionPlanAssignment> Assignments { get; }

            public string FabricatorName => FormatFabricatorGroupName(Fabricators);

            public List<ProductionPlanRequirement> Requirements { get; } = new List<ProductionPlanRequirement>();

            private static List<ProductionPlanAssignment> BuildAssignments(ComplexRecipe recipe, List<ComplexFabricator> fabricators, float outputAmount, int orderCount)
            {
                List<ProductionPlanAssignment> assignments = new List<ProductionPlanAssignment>();
                if (fabricators == null || fabricators.Count == 0 || orderCount <= 0)
                {
                    return assignments;
                }

                List<ComplexFabricator> orderedFabricators = fabricators
                    .OrderBy(fabricator => GetFiniteQueueCount(fabricator, recipe))
                    .ThenBy(fabricator => fabricator.gameObject.GetProperName())
                    .ToList();
                int baseCount = orderCount / orderedFabricators.Count;
                int remainder = orderCount % orderedFabricators.Count;
                for (int i = 0; i < orderedFabricators.Count; i++)
                {
                    int count = baseCount + (i < remainder ? 1 : 0);
                    if (count > 0)
                    {
                        assignments.Add(new ProductionPlanAssignment(orderedFabricators[i], count, outputAmount * count));
                    }
                }

                return assignments;
            }

            private static int GetFiniteQueueCount(ComplexFabricator fabricator, ComplexRecipe recipe)
            {
                int queued = fabricator != null && recipe != null ? fabricator.GetRecipeQueueCount(recipe) : 0;
                return queued == ComplexFabricator.QUEUE_INFINITE ? int.MaxValue : Mathf.Max(0, queued);
            }
        }

        private sealed class ProductionPlanAssignment
        {
            public ProductionPlanAssignment(ComplexFabricator fabricator, int orderCount, float outputAmount)
            {
                Fabricator = fabricator;
                OrderCount = orderCount;
                OutputAmount = outputAmount;
            }

            public ComplexFabricator Fabricator { get; }

            public int OrderCount { get; }

            public float OutputAmount { get; }
        }

        private sealed class ProductionPlanRequirement
        {
            public ProductionPlanRequirement(Tag material, float requiredAmount, float availableAmount)
            {
                Material = material;
                RequiredAmount = requiredAmount;
                AvailableAmount = availableAmount;
            }

            public Tag Material { get; }

            public float RequiredAmount { get; }

            public float AvailableAmount { get; }

            public ProductionPlanNode Child { get; set; }
        }
    }
}
