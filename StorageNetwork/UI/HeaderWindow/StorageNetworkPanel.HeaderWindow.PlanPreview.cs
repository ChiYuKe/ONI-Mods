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
        private void AddMaterialSchedulePanel(Transform parent, ProductionOrderDraft draft)
        {
            AddSmallTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MATERIAL_SCHEDULE_TITLE));
            GameObject panel = CreatePlainImage("MaterialSchedule", parent, new Color(0.91f, 0.89f, 0.82f, 1f));
            LayoutElement layout = panel.AddComponent<LayoutElement>();
            layout.preferredHeight = compactOrderWindow ? 300f : 360f;
            layout.minHeight = layout.preferredHeight;
            AddMaterialResearchTreeViewport(panel.transform, draft);
        }

        private void AddPlanMetrics(Transform parent, ProductionOrderDraft draft)
        {
            GameObject row = new GameObject("PlanMetrics");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = compactOrderWindow ? 112f : 62f;
            rowLayout.minHeight = rowLayout.preferredHeight;
            rowLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            float currentCycle = StorageNetworkCycleTime.GetCurrent();
            float estimateSeconds = productionOrderService.EstimatePlanSeconds(draft.Plan, out bool infinite);
            string finish = infinite ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_UNKNOWN) : ProductionOrderFormatting.FormatCycleStamp(currentCycle + estimateSeconds / 600f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_CURRENT_CYCLE), ProductionOrderFormatting.FormatCycleStamp(currentCycle), NeutralBlue(), 86f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_FINISH), finish, infinite ? WarningColor() : PositiveColor(), 92f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_EQUIPMENT), draft.Plan?.Assignments.Count.ToString() ?? "0", NeutralBlue(), 72f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_AUTO_PRODUCE), draft.ProducedRequirementCount.ToString(), draft.ProducedRequirementCount > 0 ? WarningColor() : PositiveColor(), 76f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_BLOCKED), draft.BlockedRequirementCount.ToString(), draft.BlockedRequirementCount > 0 ? DangerColor() : PositiveColor(), 70f);
        }

        private void AddMaterialTreeViewport(Transform parent, ProductionOrderDraft draft)
        {
            if (draft.Plan == null || draft.Plan.Requirements.Count == 0)
            {
                AddInfoText(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_MATERIALS), 36f);
                return;
            }

            GameObject viewport = CreatePlainImage("MaterialTreeViewport", parent, new Color(0.74f, 0.75f, 0.69f, 1f));
            LayoutElement viewportLayout = viewport.AddComponent<LayoutElement>();
            viewportLayout.preferredHeight = compactOrderWindow ? 262f : 292f;
            viewportLayout.flexibleWidth = 1f;
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<ScrollWheelBlocker>();

            RectTransform content = CreateMaterialTreeContent(viewport.transform, draft.Plan);
            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.08f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.08f;
            scrollRect.scrollSensitivity = 24f;
        }

        private RectTransform CreateMaterialTreeContent(Transform viewport, ProductionPlanNode plan)
        {
            GameObject contentObject = new GameObject("MaterialTreeContent");
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 0.5f);
            content.anchorMax = new Vector2(0f, 0.5f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = new Vector2(12f, 0f);
            content.sizeDelta = new Vector2(StorageNetworkPlanPreviewMetrics.EstimateMaterialTreeWidth(plan), compactOrderWindow ? 238f : 268f);

            HorizontalLayoutGroup layout = contentObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 28, 10, 10);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            AddPlanTreeNode(contentObject.transform, plan, 0);
            return content;
        }

        private void AddProductionChain(Transform parent, ProductionOrderDraft draft)
        {
            GameObject panel = CreateSubPanel(parent, "ProductionChain", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PREVIEW_PANEL), 0f, 0f, 0f);
            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            panelLayout.preferredHeight = 206f;
            panelLayout.minHeight = 206f;
            panelLayout.flexibleHeight = 0f;
            if (draft.Plan == null)
            {
                AddInfoText(panel.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_CHAIN), 36f);
                return;
            }

            AddDispatchOverview(panel.transform, draft.Plan);
            AddDispatchRows(panel.transform, draft.Plan);
        }

        private void AddDispatchOverview(Transform parent, ProductionPlanNode node)
        {
            GameObject summary = CreatePlainImage("DispatchOverview", parent, new Color(0.76f, 0.77f, 0.70f, 1f));
            LayoutElement summaryLayout = summary.AddComponent<LayoutElement>();
            summaryLayout.preferredHeight = 48f;
            summaryLayout.minHeight = 48f;
            summaryLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup layout = summary.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIcon(summary.transform, node.Recipe?.GetUIIcon(), 30f);

            GameObject textColumn = new GameObject("DispatchText");
            textColumn.transform.SetParent(summary.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(textColumn, 0f, 0, 0, 0, 0);

            TextMeshProUGUI title = CreateOrderText("DispatchTitle", textColumn.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_TITLE_LINE), node.Recipe != null ? node.Recipe.GetUIName(false) : "?"), 10, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.18f, 0.20f, 0.19f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            AddPlanLine(textColumn.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DESC), 8, FontStyles.Italic, MutedTextColor(), 16f);
            AddFlowStatusPill(summary.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_BATCH_COUNT), node.OrderCount), NeutralBlue(), 58f);
            AddFlowStatusPill(summary.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MACHINE_COUNT), node.Assignments.Count), NeutralBlue(), 58f);
            AddFlowStatusPill(summary.transform, GameUtil.GetFormattedMass(node.OutputAmount * node.OrderCount), PositiveColor(), 92f);
        }

        private void AddDispatchRows(Transform parent, ProductionPlanNode node)
        {
            GameObject rows = new GameObject("DispatchRows");
            rows.transform.SetParent(parent, false);
            rows.AddComponent<RectTransform>();
            LayoutElement rowsLayout = rows.AddComponent<LayoutElement>();
            rowsLayout.preferredHeight = 128f;
            rowsLayout.minHeight = 128f;
            rowsLayout.flexibleHeight = 0f;
            VerticalLayoutGroup layout = rows.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            AddDispatchMachineRow(rows.transform, node);
            List<ProductionPlanRequirement> requirements = node.Requirements
                .Where(requirement => requirement != null && requirement.Material != Tag.Invalid)
                .Take(3)
                .ToList();
            if (requirements.Count == 0)
            {
                AddInfoText(rows.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_MATERIALS), 30f);
                return;
            }

            foreach (ProductionPlanRequirement requirement in requirements)
            {
                AddDispatchRequirementRow(rows.transform, requirement);
            }
        }

        private void AddDispatchMachineRow(Transform parent, ProductionPlanNode node)
        {
            GameObject row = CreatePlainImage("DispatchMachineRow", parent, new Color(0.78f, 0.79f, 0.73f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 36f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddStatusBadge(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_DISPATCH), NeutralBlue(), 72f);
            AddFlowRecipePill(row.transform, node, 180f);
            AddFlowArrow(row.transform, NeutralBlue());
            AddStatusBadge(row.transform, StorageNetworkPlanPreviewText.BuildAssignmentSummary(node, 3), NeutralBlue(), 260f);
        }

        private void AddDispatchRequirementRow(Transform parent, ProductionPlanRequirement requirement)
        {
            bool covered = StorageNetworkPlanPreviewText.IsCoveredByNetwork(requirement);
            bool produced = StorageNetworkPlanPreviewText.CanProduceRequirement(requirement);
            Color color = covered ? PositiveColor() : produced ? WarningColor() : DangerColor();
            GameObject row = CreatePlainImage("DispatchRequirementRow", parent, covered ? new Color(0.78f, 0.80f, 0.73f, 1f) : produced ? new Color(0.80f, 0.78f, 0.70f, 1f) : new Color(0.80f, 0.73f, 0.70f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 32f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 3, 3);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddStatusBadge(row.transform, covered ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DIRECT) : produced ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NEEDS_PRODUCTION) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_BLOCKED), color, 72f);
            AddFlowMaterialPill(row.transform, requirement, 220f);
            AddFlowArrow(row.transform, color);
            AddStatusBadge(row.transform, covered ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_SEND_FROM_NETWORK) : produced ? StorageNetworkPlanPreviewText.BuildAssignmentSummary(requirement.Child, 3) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_NO_ROUTE), color, 260f);
        }

    }
}

