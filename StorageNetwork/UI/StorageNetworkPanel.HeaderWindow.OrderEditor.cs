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
        private void RebuildOrderDetails()
        {
            ProductDisplayGroup product = GetSelectedProduct();
            if (product == null || product.Routes.Count == 0)
            {
                string emptySignature = "empty";
                if (orderDetailsSignature == emptySignature)
                {
                    RebuildOrderTracking(null);
                    return;
                }

                orderDetailsSignature = emptySignature;
                DeactivateOrderInputs();
                ClearChildren(orderDetailsContent);
                AddInfoText(orderDetailsContent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_WINDOW_EMPTY), 96f);
                RebuildOrderTracking(null);
                ForceOrderLayout(orderDetailsContent);
                return;
            }

            selectedRouteIndex = Mathf.Clamp(selectedRouteIndex, 0, product.Routes.Count - 1);
            RecipeDisplayInfo route = product.Routes[selectedRouteIndex];
            if (requestedProductAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                requestedProductAmount = ProductionRecipeCatalog.GetRecipeResultForProduct(route.Recipe, product.ProductTag)?.amount ?? 1f;
            }

            ProductionOrderDraft draft = productionOrderService.BuildDraft(product, route, requestedProductAmount);
            string signature = StorageNetworkOrderEditorSignatureBuilder.Build(
                product,
                route,
                draft,
                productionOrderService.GetKeepRule(product.ProductTag),
                selectedRouteIndex,
                requestedProductAmount,
                lastOrderStatus);
            if (signature == orderDetailsSignature)
            {
                RebuildOrderTracking(product);
                return;
            }

            orderDetailsSignature = signature;
            DeactivateOrderInputs();
            ClearChildren(orderDetailsContent);
            AddOrderWorkspace(orderDetailsContent, product, route, draft);
            RebuildOrderTracking(product);

            ForceOrderLayout(orderDetailsContent);
            ApplyOrderWorkspaceViewportFill();
            ForceOrderLayout(orderDetailsContent);
        }

        private void AddOrderWorkspace(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            float workspaceHeight = Mathf.Max(compactOrderWindow ? 1040f : 760f, GetOrderWorkspaceViewportHeight());
            GameObject workspace = CreatePlainImage("OrderWorkspaceCanvas", parent, new Color(0.89f, 0.88f, 0.81f, 1f));
            LayoutElement workspaceLayout = workspace.AddComponent<LayoutElement>();
            workspaceLayout.flexibleWidth = 1f;
            workspaceLayout.flexibleHeight = 1f;
            workspaceLayout.preferredHeight = workspaceHeight;
            workspaceLayout.minHeight = workspaceHeight;

            HorizontalLayoutGroup layout = workspace.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            float editorWidth = compactOrderWindow ? 574f : 287f;
            GameObject editor = CreateOrderWorkspaceColumn(workspace.transform, "OrderEditorColumn", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_EDITOR_TITLE), editorWidth, workspaceHeight - 16f, 0f);
            AddProductBrief(editor.transform, product, route, draft);
            AddAmountEditor(editor.transform, product);
            AddKeepRuleEditor(editor.transform, product, route);
            AddRouteEditor(editor.transform, product);
            AddRecipeEditor(editor.transform, product, route);
            AddValidationPanel(editor.transform, product, draft);
            AddOrderFooter(editor.transform, product, route, draft);

            GameObject preview = CreateOrderWorkspaceColumn(workspace.transform, "OrderPreviewColumn", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PREVIEW_TITLE), 0f, workspaceHeight - 16f, 1f);
            AddPlanMetrics(preview.transform, draft);
            AddProductionChain(preview.transform, draft);
            AddMaterialSchedulePanel(preview.transform, draft);
        }

        private float GetOrderWorkspaceViewportHeight()
        {
            if (orderDetailsViewport == null)
            {
                return 0f;
            }

            Canvas.ForceUpdateCanvases();
            float height = orderDetailsViewport.rect.height;
            VerticalLayoutGroup layout = orderDetailsContent != null ? orderDetailsContent.GetComponent<VerticalLayoutGroup>() : null;
            if (layout != null)
            {
                height -= layout.padding.top + layout.padding.bottom;
            }

            return Mathf.Max(0f, height);
        }

        private void ApplyOrderWorkspaceViewportFill()
        {
            if (orderDetailsContent == null)
            {
                return;
            }

            RectTransform workspace = orderDetailsContent.Find("OrderWorkspaceCanvas") as RectTransform;
            if (workspace == null)
            {
                return;
            }

            float workspaceHeight = Mathf.Max(compactOrderWindow ? 1040f : 760f, GetOrderWorkspaceViewportHeight());
            LayoutElement workspaceLayout = workspace.GetComponent<LayoutElement>();
            if (workspaceLayout != null)
            {
                workspaceLayout.preferredHeight = workspaceHeight;
                workspaceLayout.minHeight = workspaceHeight;
            }

            float columnHeight = Mathf.Max(0f, workspaceHeight - 16f);
            ApplyOrderWorkspaceColumnHeight(workspace.Find("OrderEditorColumn") as RectTransform, columnHeight);
            ApplyOrderWorkspaceColumnHeight(workspace.Find("OrderPreviewColumn") as RectTransform, columnHeight);
        }

        private static void ApplyOrderWorkspaceColumnHeight(RectTransform column, float height)
        {
            if (column == null)
            {
                return;
            }

            LayoutElement layout = column.GetComponent<LayoutElement>();
            if (layout == null)
            {
                return;
            }

            layout.preferredHeight = height;
            layout.minHeight = height;
        }

        private GameObject CreateOrderWorkspaceColumn(Transform parent, string name, string title, float width, float height, float flexibleWidth)
        {
            GameObject panel = CreatePlainImage(name, parent, new Color(0.92f, 0.90f, 0.83f, 1f));
            LayoutElement layout = panel.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                layout.preferredWidth = width;
                layout.minWidth = width;
            }

            if (height > 0f)
            {
                layout.preferredHeight = height;
                layout.minHeight = height;
            }

            layout.flexibleWidth = flexibleWidth;
            layout.flexibleHeight = 0f;
            AddVerticalContainer(panel, 6f, 8, 8, 8, 8);
            AddSmallTitle(panel.transform, title);
            return panel;
        }

        private static float GetOrderWorkspaceCanvasWidth()
        {
            return Mathf.Clamp(GetCanvasWidth() - 64f, 980f, OrderWindowMaxWidth - 34f);
        }

        private void AddOrderControlsRow(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            AddProductBrief(parent, product, route, draft);
            GameObject row = CreateSection(parent, "OrderControlsRow", compactOrderWindow ? 520f : 330f, new Color(0.72f, 0.73f, 0.67f, 1f));
            HorizontalOrVerticalLayoutGroup layout = compactOrderWindow
                ? (HorizontalOrVerticalLayoutGroup)row.AddComponent<VerticalLayoutGroup>()
                : row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject amount = CreateSubPanel(row.transform, "AmountAndKeep", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AMOUNT_KEEP_TITLE), compactOrderWindow ? 0f : 270f, compactOrderWindow ? 0f : 250f, compactOrderWindow ? 1f : 0f);
            amount.GetComponent<LayoutElement>().preferredHeight = compactOrderWindow ? 170f : 0f;
            AddAmountEditor(amount.transform, product);
            AddKeepRuleEditor(amount.transform, product, route);

            GameObject routePanel = CreateSubPanel(row.transform, "RouteAndRecipe", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_RECIPE_TITLE), 0f, compactOrderWindow ? 0f : 320f, 1f);
            routePanel.GetComponent<LayoutElement>().preferredHeight = compactOrderWindow ? 220f : 0f;
            AddRouteEditor(routePanel.transform, product);
            AddRecipeEditor(routePanel.transform, product, route);

            GameObject validation = CreateSubPanel(row.transform, "DraftValidation", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_VALIDATION_TITLE), compactOrderWindow ? 0f : 260f, compactOrderWindow ? 0f : 240f, compactOrderWindow ? 1f : 0f);
            validation.GetComponent<LayoutElement>().preferredHeight = compactOrderWindow ? 120f : 0f;
            AddValidationPanel(validation.transform, product, draft);
        }

        private void AddOrderExecutionRow(Transform parent, ProductionOrderDraft draft)
        {
            GameObject row = CreateSection(parent, "OrderExecutionRow", compactOrderWindow ? 620f : 430f, new Color(0.72f, 0.73f, 0.67f, 1f));
            HorizontalOrVerticalLayoutGroup layout = compactOrderWindow
                ? (HorizontalOrVerticalLayoutGroup)row.AddComponent<VerticalLayoutGroup>()
                : row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject preview = CreateSubPanel(row.transform, "ExecutionPreview", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PREVIEW_TITLE), 0f, compactOrderWindow ? 0f : 430f, 1f);
            preview.GetComponent<LayoutElement>().preferredHeight = compactOrderWindow ? 300f : 0f;
            AddPlanMetrics(preview.transform, draft);
            AddProductionChain(preview.transform, draft);

            GameObject materials = CreateSubPanel(row.transform, "MaterialSchedule", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MATERIAL_SCHEDULE_TITLE), 0f, compactOrderWindow ? 0f : 430f, 1f);
            materials.GetComponent<LayoutElement>().preferredHeight = compactOrderWindow ? 300f : 0f;
            AddMaterialTable(materials.transform, draft);
        }

        private void AddProductBrief(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            GameObject brief = CreatePlainImage("ProductBrief", parent, new Color(0.78f, 0.78f, 0.72f, 1f));

            LayoutElement briefLayout = brief.AddComponent<LayoutElement>();
            briefLayout.preferredHeight = 72f;
            briefLayout.minHeight = 72f;
            briefLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup layout = brief.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddIcon(brief.transform, product.Icon, 44f);

            GameObject textColumn = new GameObject("ProductBriefText");
            textColumn.transform.SetParent(brief.transform, false);
            textColumn.AddComponent<RectTransform>();

            LayoutElement textLayout = textColumn.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.preferredHeight = 44f;
            textLayout.minHeight = 44f;
            textLayout.flexibleHeight = 0f;

            AddVerticalLayout(textColumn, 1f, 0, 0, 0, 0);

            TextMeshProUGUI title = CreateText("ProductName", textColumn.transform, product.ProductName, 15, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.13f, 0.15f, 0.15f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 23f;

            string subtitleText = string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_BRIEF),
                route.FabricatorName,
                draft.Plan != null ? draft.Plan.OrderCount : 0,
                ProductionOrderFormatting.FormatCycle(productionOrderService.EstimatePlanSeconds(draft.Plan, out bool infinite) / 600f)
            );

            if (infinite)
            {
                subtitleText = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_BRIEF_UNKNOWN), route.FabricatorName, draft.Plan != null ? draft.Plan.OrderCount : 0);
            }

            TextMeshProUGUI subtitle = CreateText("ProductMeta", textColumn.transform, subtitleText, 10, TextAlignmentOptions.MidlineLeft);
            subtitle.color = new Color(0.28f, 0.30f, 0.29f, 1f);
            subtitle.fontStyle = FontStyles.Bold;
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        }

        private void AddOrderSummaryBand(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            GameObject band = CreateSection(parent, "OrderSummaryBand", 104f, new Color(0.74f, 0.74f, 0.68f, 1f));
            HorizontalLayoutGroup layout = band.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddIcon(band.transform, product.Icon, 52f);

            GameObject titleColumn = new GameObject("TitleColumn");
            titleColumn.transform.SetParent(band.transform, false);
            titleColumn.AddComponent<RectTransform>();
            titleColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(titleColumn, 1f, 0, 0, 0, 0);

            TextMeshProUGUI title = CreateText("ProductName", titleColumn.transform, product.ProductName, 16, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.13f, 0.15f, 0.15f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            AddWrappedPlanLine(titleColumn.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_RECIPE_LINE), route.FabricatorName, ProductionOrderFormatting.FormatRecipeElements(route.Recipe.ingredients)), 9, FontStyles.Normal, new Color(0.30f, 0.32f, 0.31f, 1f), 20f, 2, compactOrderWindow ? 34 : 48);
            AddPlanLine(titleColumn.transform, BuildOrderTrackingStatus(product, draft), 9, draft.DuplicateOrder != null ? FontStyles.Bold : FontStyles.Italic, draft.DuplicateOrder != null ? WarningColor() : MutedTextColor(), 22f);

            AddMetricTile(band.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_TARGET), GameUtil.GetFormattedMass(draft.RequestedAmount), GetRiskColor(draft.RiskLevel), 108f);
            AddMetricTile(band.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_AVAILABLE), GameUtil.GetFormattedMass(draft.NetworkAvailableAmount), PositiveColor(), 108f);
            AddMetricTile(band.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_BATCHES), draft.Plan != null ? draft.Plan.OrderCount.ToString() : "0", NeutralBlue(), 86f);
            AddMetricTile(band.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_STATUS), GetRiskLabel(draft.RiskLevel), GetRiskColor(draft.RiskLevel), 94f);
        }

        private void AddOrderEditorAndPlan(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            GameObject row = CreateSection(parent, "OrderEditorAndPlan", compactOrderWindow ? 800f : 500f, new Color(0.72f, 0.73f, 0.67f, 1f));
            HorizontalOrVerticalLayoutGroup layout = compactOrderWindow
                ? (HorizontalOrVerticalLayoutGroup)row.AddComponent<VerticalLayoutGroup>()
                : row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject editor = CreateSubPanel(row.transform, "DraftEditor", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_EDITOR_TITLE), compactOrderWindow ? 0f : 278f, compactOrderWindow ? 0f : 260f, compactOrderWindow ? 1f : 0f);
            AddAmountEditor(editor.transform, product);
            AddKeepRuleEditor(editor.transform, product, route);
            AddRouteEditor(editor.transform, product);
            AddRecipeEditor(editor.transform, product, route);
            AddValidationPanel(editor.transform, product, draft);

            GameObject plan = CreateSubPanel(row.transform, "PlanPanel", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PLAN_PANEL_TITLE), 0f, compactOrderWindow ? 0f : 420f, 1f);
            AddPlanMetrics(plan.transform, draft);
            AddPlanLedger(plan.transform, draft);
        }

        private void AddAmountEditor(Transform parent, ProductDisplayGroup product)
        {
            AddSmallTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AMOUNT_LABEL));
            GameObject panel = CreatePlainImage("AmountEditor", parent, new Color(0.78f, 0.78f, 0.72f, 1f));
            LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
            panelLayout.minHeight = 66f;
            panelLayout.preferredHeight = 66f;
            panelLayout.flexibleHeight = 0f;
            VerticalLayoutGroup panelGroup = panel.AddComponent<VerticalLayoutGroup>();
            panelGroup.padding = new RectOffset(6, 6, 5, 5);
            panelGroup.spacing = 4f;
            panelGroup.childAlignment = TextAnchor.MiddleLeft;
            panelGroup.childControlWidth = true;
            panelGroup.childControlHeight = true;
            panelGroup.childForceExpandWidth = true;
            panelGroup.childForceExpandHeight = false;

            GameObject row = new GameObject("AmountValueRow");
            row.transform.SetParent(panel.transform, false);
            row.AddComponent<RectTransform>();
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 26f;
            rowLayout.minHeight = 26f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            orderAmountInput = CreateFixedAmountInput(row.transform, 92f, 24f);
            StorageNetworkNumberInputField orderAmountNumberInput = orderAmountInput.GetComponent<StorageNetworkNumberInputField>();
            orderAmountNumberInput?.Configure(orderAmountInput, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, float.MaxValue, false);
            orderAmountNumberInput?.SetAmount(requestedProductAmount);
            if (orderAmountNumberInput != null)
            {
                orderAmountNumberInput.onEndEdit += () =>
                {
                    requestedProductAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, orderAmountNumberInput.currentValue);
                    orderDetailsSignature = null;
                    RebuildOrderDetails();
                };
            }
            else
            {
                orderAmountInput.text = FormatAmount(requestedProductAmount);
                orderAmountInput.onEndEdit.AddListener(value =>
                {
                    if (TryParseAmount(value, out float parsed))
                    {
                        requestedProductAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, parsed);
                        orderDetailsSignature = null;
                    }

                    RebuildOrderDetails();
                });
            }

            TextMeshProUGUI unit = CreateText("Unit", row.transform, "kg", 15, TextAlignmentOptions.MidlineLeft);
            unit.color = MutedTextColor();
            unit.gameObject.AddComponent<LayoutElement>().preferredWidth = 20f;

            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            AddAmountAdjustButton(row.transform, "icon_TrendArrows_Down_1", () => AdjustRequestedAmount(-orderAmountStep));
            AddAmountAdjustButton(row.transform, "icon_TrendArrows_Up_1", () => AdjustRequestedAmount(orderAmountStep));
            AddAmountStepSelector(panel.transform);
        }

        private void AddKeepRuleEditor(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route)
        {
            ProductionKeepRule rule = productionOrderService.GetKeepRule(product.ProductTag);
            AddSmallTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_TITLE));
            GameObject row = CreatePlainImage("KeepRuleRow", parent, new Color(0.78f, 0.78f, 0.72f, 1f));
            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 34f;
            rowElement.minHeight = 34f;
            rowElement.flexibleHeight = 0f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 5, 5);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            string label = rule != null
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_STATUS), GameUtil.GetFormattedMass(rule.TargetAmount))
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_DISABLED);
            TextMeshProUGUI status = CreateText("KeepRuleStatus", row.transform, label, 11, TextAlignmentOptions.MidlineLeft);
            status.color = rule != null ? PositiveColor() : MutedTextColor();
            status.fontStyle = FontStyles.Bold;
            status.textWrappingMode = TextWrappingModes.NoWrap;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            EnsureKeepRuleDraft(product, rule);
            keepRuleAmountInput = CreateFixedAmountInput(row.transform, 74f, 24f);
            StorageNetworkNumberInputField keepRuleNumberInput = keepRuleAmountInput.GetComponent<StorageNetworkNumberInputField>();
            keepRuleNumberInput?.Configure(keepRuleAmountInput, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, float.MaxValue, false);
            keepRuleNumberInput?.SetAmount(keepRuleDraftAmount);
            if (keepRuleNumberInput != null)
            {
                keepRuleNumberInput.onEndEdit += () =>
                {
                    keepRuleDraftAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, keepRuleNumberInput.currentValue);
                    orderDetailsSignature = null;
                };
            }
            else
            {
                keepRuleAmountInput.text = FormatAmount(keepRuleDraftAmount);
                keepRuleAmountInput.onEndEdit.AddListener(value =>
                {
                    if (TryParseAmount(value, out float parsed))
                    {
                        keepRuleDraftAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, parsed);
                        orderDetailsSignature = null;
                    }
                });
            }

            GameObject enableButton = CreateStyledButton("EnableKeepRule", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_BUTTON), () =>
            {
                float keepAmount = GetKeepRuleAmountFromInput(rule);
                productionOrderService.SetKeepRule(product, route, keepAmount);
                lastOrderStatus = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_ENABLED_STATUS), product.ProductName, GameUtil.GetFormattedMass(keepAmount));
                productionOrderService.Refresh();
                orderDetailsSignature = null;
                RebuildOrderDetails();
            }, KleiBlueStyle());
            LayoutElement enableLayout = enableButton.AddComponent<LayoutElement>();
            enableLayout.preferredWidth = 44f;
            enableLayout.preferredHeight = 24f;

            GameObject clearButton = CreateStyledButton("ClearKeepRule", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE), () =>
            {
                productionOrderService.ClearKeepRule(product.ProductTag);
                lastOrderStatus = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_CLEARED_STATUS), product.ProductName);
                orderDetailsSignature = null;
                RebuildOrderDetails();
            }, rule != null ? KleiPinkStyle() : KleiBlueStyle());
            LayoutElement clearLayout = clearButton.AddComponent<LayoutElement>();
            clearLayout.preferredWidth = 44f;
            clearLayout.preferredHeight = 24f;
            clearButton.GetComponent<KButton>().isInteractable = rule != null;
        }

        private void AddRouteEditor(Transform parent, ProductDisplayGroup product)
        {
            AddSmallTitle(parent, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_SECTION), product.Routes.Select(route => route.FabricatorName).Distinct().Count()));

            foreach (IGrouping<string, RecipeDisplayInfo> group in product.Routes.GroupBy(route => route.FabricatorName).Take(5))
            {
                int routeIndex = product.Routes.FindIndex(route => route.FabricatorName == group.Key);
                bool selected = selectedRouteIndex >= 0 &&
                                selectedRouteIndex < product.Routes.Count &&
                                product.Routes[selectedRouteIndex].FabricatorName == group.Key;

                GameObject button = CreateStyledButton(
                    "RouteDeviceButton",
                    parent,
                    string.Empty,
                    () =>
                    {
                        selectedRouteIndex = routeIndex;
                        lastOrderStatus = null;
                        orderDetailsSignature = null;
                        RebuildOrderDetails();
                    },
                    selected ? KleiPinkStyle() : KleiBlueStyle());

                LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
                buttonLayout.preferredHeight = 58f;
                buttonLayout.minHeight = 58f;
                buttonLayout.flexibleHeight = 0f;

                HorizontalLayoutGroup layout = button.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(10, 12, 6, 6);
                layout.spacing = 10f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                RecipeDisplayInfo firstRoute = group.FirstOrDefault();
                AddIcon(button.transform, GetFabricatorIcon(firstRoute), 38f);

                int fabricatorCount = group.SelectMany(route => route.Fabricators ?? new List<ComplexFabricator>()).Distinct().Count();
                TextMeshProUGUI label = CreateText("DeviceName", button.transform, BuildRouteDeviceLabel(group.Key, fabricatorCount, group.Count()), 12, TextAlignmentOptions.MidlineLeft);
                label.color = new Color(0.94f, 0.96f, 0.98f, 1f);
                label.fontStyle = FontStyles.Bold;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                TextMeshProUGUI arrow = CreateText("Arrow", button.transform, ">", 18, TextAlignmentOptions.Center);
                arrow.color = new Color(0.94f, 0.96f, 0.98f, 1f);
                arrow.fontStyle = FontStyles.Bold;
                arrow.gameObject.AddComponent<LayoutElement>().preferredWidth = 22f;
            }
        }

        private static Sprite GetFabricatorIcon(RecipeDisplayInfo route)
        {
            ComplexFabricator fabricator = route.Fabricators?.FirstOrDefault(item => item != null);
            KPrefabID prefabId = fabricator != null ? fabricator.GetComponent<KPrefabID>() : null;
            if (prefabId != null)
            {
                var uiSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                if (uiSprite.first != null)
                {
                    return uiSprite.first;
                }
            }

            return route.Icon;
        }

        private static string BuildRouteDeviceLabel(string fabricatorName, int fabricatorCount, int recipeCount)
        {
            return recipeCount > 1
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_DEVICE_MULTI), fabricatorCount, recipeCount)
                : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_DEVICE_SINGLE), fabricatorCount);
        }

        private void AddRecipeEditor(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo selectedRoute)
        {
            List<RecipeDisplayInfo> alternatives = product.Routes
                .Where(route => route.FabricatorName == selectedRoute.FabricatorName)
                .ToList();
            AddSmallTitle(parent, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RECIPE_SECTION), alternatives.Count));
            if (alternatives.Count <= 1)
            {
                AddPlanLine(parent, ProductionOrderFormatting.FormatRecipeElements(selectedRoute.Recipe.ingredients), 12, FontStyles.Bold, new Color(0.18f, 0.21f, 0.21f, 1f), 24f);
                return;
            }

            foreach (RecipeDisplayInfo route in alternatives.Take(4))
            {
                int routeIndex = product.Routes.IndexOf(route);
                string label = ProductionOrderFormatting.FormatRecipeElements(route.Recipe.ingredients);
                AddChoiceButton(parent, routeIndex == selectedRouteIndex ? "> " + label : label, routeIndex, routeIndex == selectedRouteIndex,15f);
            }
        }

        private void AddValidationPanel(Transform parent, ProductDisplayGroup product, ProductionOrderDraft draft)
        {
            AddSmallTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_VALIDATION_TITLE));
            GameObject card = CreatePlainImage("ValidationCard", parent, new Color(0.94f, 0.92f, 0.84f, 1f));
            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 118f;
            cardLayout.minHeight = 118f;
            cardLayout.flexibleHeight = 0f;
            AddVerticalLayout(card, 0f, 0, 0, 0, 0);

            Color statusColor = draft.CanSubmit
                ? new Color(0.39f, 0.53f, 0.37f, 1f)
                : new Color(GetRiskColor(draft.RiskLevel).r, GetRiskColor(draft.RiskLevel).g, GetRiskColor(draft.RiskLevel).b, 0.88f);
            GameObject header = CreatePlainImage("ValidationHeader", card.transform, statusColor);
            header.AddComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(8, 8, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            AddStatusIcon(header.transform, draft.CanSubmit);

            TextMeshProUGUI title = CreateText("ValidationTitle", header.transform, draft.CanSubmit ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RISK_READY) : GetRiskLabel(draft.RiskLevel), 12, TextAlignmentOptions.MidlineLeft);
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject body = new GameObject("ValidationBody");
            body.transform.SetParent(card.transform, false);
            body.AddComponent<RectTransform>();
            body.AddComponent<LayoutElement>().preferredHeight = 88f;
            AddVerticalLayout(body, 4f, 10, 10, 8, 8);

            string message = draft.ValidationMessages.Count > 0
                ? string.Join("\n", draft.ValidationMessages.Take(2))
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_VALIDATION_READY_BODY);
            AddWrappedPlanLine(body.transform, message, 10, FontStyles.Bold, draft.CanSubmit ? new Color(0.25f, 0.42f, 0.27f, 1f) : GetRiskColor(draft.RiskLevel), 34f, 2, 40);
            AddPlanLine(body.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_VALIDATION_OUTPUT), GameUtil.GetFormattedMass(draft.RequestedAmount)), 10, FontStyles.Bold, new Color(0.18f, 0.20f, 0.18f, 1f), 20f);
            AddPlanLine(body.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_VALIDATION_ACTIVE_ORDERS), product.ProductName, productionOrderService.GetActiveOrdersForProduct(product.ProductTag, 99).Count), 8, FontStyles.Italic, MutedTextColor(), 18f);
        }

        private static void AddStatusIcon(Transform parent, bool positive)
        {
            GameObject iconObject = new GameObject("StatusIcon");
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(20f, 20f);
            LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 20f;
            iconLayout.preferredHeight = 20f;

            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = GetSpriteByName(positive ? "crew_state_encourage" : "crew_state_unhappy");
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private void AddOrderFooter(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            GameObject footer = CreateSection(parent, "OrderFooter", 72f, new Color(0.64f, 0.65f, 0.59f, 1f));
            HorizontalLayoutGroup layout = footer.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 9, 9);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            string statusText = !string.IsNullOrEmpty(lastOrderStatus)
                ? lastOrderStatus
                : draft.CanSubmit ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_FOOTER_READY) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_FOOTER_BLOCKED);
            TextMeshProUGUI status = CreateText("FooterStatus", footer.transform, statusText, 10, TextAlignmentOptions.MidlineLeft);
            status.color = draft.CanSubmit ? PositiveColor() : GetRiskColor(draft.RiskLevel);
            status.fontStyle = FontStyles.Bold;
            status.textWrappingMode = TextWrappingModes.Normal;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.maxVisibleLines = 3;
            LayoutElement statusLayout = status.gameObject.AddComponent<LayoutElement>();
            statusLayout.flexibleWidth = 1f;
            statusLayout.preferredHeight = 54f;

            GameObject button = CreateGameButton("ConfirmOrder", footer.transform, draft.DuplicatePolicy == ProductionOrderDuplicatePolicy.MergeIntoExisting ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CONFIRM_MERGE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CONFIRM), () =>
            {
                ProductionOrderSubmitResult result = productionOrderService.SubmitOrder(product, route, requestedProductAmount, GetCurrentCycleTime());
                lastOrderStatus = result.Message;
                productionOrderService.Refresh();
                orderDetailsSignature = null;
                orderTrackingSignature = null;
                RebuildOrderDetails();
            });
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.minWidth = 150f;
            buttonLayout.preferredHeight = 42f;
            buttonLayout.minHeight = 42f;
            buttonLayout.flexibleWidth = 0f;
            buttonLayout.flexibleHeight = 0f;
            button.GetComponent<KButton>().isInteractable = draft.CanSubmit;
        }

        private string BuildOrderTrackingStatus(ProductDisplayGroup product, ProductionOrderDraft draft)
        {
            if (!string.IsNullOrEmpty(lastOrderStatus))
            {
                return lastOrderStatus;
            }

            if (draft.DuplicateOrder != null)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DUPLICATE_FOUND), draft.DuplicateOrder.DisplayId);
            }

            int activeCount = productionOrderService.GetActiveOrdersForProduct(product.ProductTag, 99).Count;
            return activeCount > 0
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ACTIVE_ORDERS_FOUND), activeCount)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_ACTIVE_ORDERS);
        }

        private static string GetRiskLabel(ProductionOrderRiskLevel risk)
        {
            switch (risk)
            {
                case ProductionOrderRiskLevel.Blocked:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RISK_BLOCKED);
                case ProductionOrderRiskLevel.Warning:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RISK_WARNING);
                default:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RISK_READY);
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

        private static string BuildAutomationSummary(ProductionOrderDraft draft)
        {
            if (draft.RiskLevel == ProductionOrderRiskLevel.Blocked)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AUTOMATION_BLOCKED);
            }

            if (draft.ProducedRequirementCount > 0)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AUTOMATION_PRODUCE), draft.ProducedRequirementCount);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AUTOMATION_READY);
        }

    }
}
