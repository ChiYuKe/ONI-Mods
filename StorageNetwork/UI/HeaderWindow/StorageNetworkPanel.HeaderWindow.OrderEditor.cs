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
            if (IsOrderWorldFilterBlockedByRelay())
            {
                string relayOfflineSignature = "relay_offline";
                if (orderDetailsSignature == relayOfflineSignature)
                {
                    RebuildOrderTracking(null);
                    return;
                }

                orderDetailsSignature = relayOfflineSignature;
                DeactivateOrderInputs();
                ClearChildren(orderDetailsContent);
                AddInfoText(orderDetailsContent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CROSS_WORLD_RELAY_OFFLINE), 96f);
                RebuildOrderTracking(null);
                ForceOrderLayout(orderDetailsContent);
                return;
            }

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

    }
}
