using System.Linq;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void AddMaterialLedgerHeader(Transform parent)
        {
            GameObject header = CreatePlainImage("MaterialLedgerHeader", parent, new Color(0.70f, 0.71f, 0.65f, 1f));
            header.AddComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 3, 3);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddFixedSpacer(header.transform, 18f);
            AddTableCell(header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TABLE_MATERIAL), 8, FontStyles.Bold, NeutralTextColor(), 0f, true);
            AddTableCell(header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TABLE_REQUIRED), 8, FontStyles.Bold, NeutralTextColor(), 82f, false);
            AddTableCell(header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TABLE_STOCK_MISSING), 8, FontStyles.Bold, NeutralTextColor(), 104f, false);
            AddTableCell(header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TABLE_ACTION), 8, FontStyles.Bold, NeutralTextColor(), 128f, false);
        }

        private void AddMaterialCard(Transform parent, ProductionPlanRequirement requirement, int depth)
        {
            bool covered = StorageNetworkPlanPreviewText.IsCoveredByNetwork(requirement);
            bool produced = StorageNetworkPlanPreviewText.CanProduceRequirement(requirement);
            Color color = covered ? PositiveColor() : produced ? WarningColor() : DangerColor();
            string name = ProductionOrderFormatting.GetTagDisplayName(requirement.Material);
            string stockLine = StorageNetworkPlanPreviewText.BuildRequirementStockLine(requirement);
            string actionLine = StorageNetworkPlanPreviewText.BuildRequirementActionLine(requirement, 3);

            int detailLines = EstimateTextLineCount(stockLine, 2, compactOrderWindow ? 20 : 28) +
                              EstimateTextLineCount(actionLine, 3, compactOrderWindow ? 20 : 28);
            float cardHeight = Mathf.Max(depth == 0 ? 62f : 56f, 34f + detailLines * 15f);

            GameObject card = CreatePlainImage("MaterialCard", parent, depth == 0 ? new Color(0.78f, 0.79f, 0.73f, 1f) : new Color(0.71f, 0.73f, 0.68f, 1f));
            card.AddComponent<LayoutElement>().preferredHeight = cardHeight;
            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8 + depth * 18, 8, 6, 6);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (depth > 0)
            {
                AddIndentGuide(card.transform, GetRequirementColor(requirement));
            }

            AddMaterialIcon(card.transform, requirement.Material, depth == 0 ? 18f : 16f);

            GameObject textColumn = new GameObject("MaterialCardText");
            textColumn.transform.SetParent(card.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(textColumn, 2f, 0, 0, 0, 0);

            TextMeshProUGUI title = CreateOrderText("MaterialCardTitle", textColumn.transform, name, depth == 0 ? 9 : 8, TextAlignmentOptions.TopLeft);
            title.color = color;
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.maxVisibleLines = 2;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f * EstimateTextLineCount(name, 2, compactOrderWindow ? 12 : 18);

            AddWrappedPlanLine(textColumn.transform, stockLine, 8, FontStyles.Normal, NeutralTextColor(), 15f, 2, compactOrderWindow ? 20 : 28);
            AddWrappedPlanLine(textColumn.transform, actionLine, 8, FontStyles.Bold, color, 15f, 3, compactOrderWindow ? 20 : 28);

            if (requirement.Child != null && depth < 2)
            {
                AddChildRouteCard(parent, requirement.Child, depth + 1);
                foreach (ProductionPlanRequirement childRequirement in requirement.Child.Requirements.Take(3))
                {
                    AddMaterialCard(parent, childRequirement, depth + 1);
                }
            }
        }

        private void AddChildRouteCard(Transform parent, ProductionPlanNode child, int depth)
        {
            string routeText = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CHILD_ROUTE), child.Recipe != null ? child.Recipe.GetUIName(false) : "?", child.OrderCount);
            string assignmentText = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DEVICE_LINE), StorageNetworkPlanPreviewText.BuildAssignmentSummary(child, 3));
            GameObject card = CreatePlainImage("MaterialRouteCard", parent, new Color(0.69f, 0.69f, 0.62f, 1f));
            card.AddComponent<LayoutElement>().preferredHeight = 58f;
            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(26 + depth * 18, 8, 6, 6);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddIndentGuide(card.transform, WarningColor());
            AddIcon(card.transform, child.Recipe?.GetUIIcon(), 20f);

            GameObject textColumn = new GameObject("MaterialRouteText");
            textColumn.transform.SetParent(card.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(textColumn, 2f, 0, 0, 0, 0);

            AddWrappedPlanLine(textColumn.transform, routeText, 8, FontStyles.Bold, WarningColor(), 16f, 2, compactOrderWindow ? 18 : 28);
            AddWrappedPlanLine(textColumn.transform, assignmentText, 8, FontStyles.Normal, NeutralBlue(), 15f, 2, compactOrderWindow ? 18 : 28);
        }

        private void AddMaterialRow(Transform parent, ProductionPlanRequirement requirement, int depth)
        {
            bool covered = StorageNetworkPlanPreviewText.IsCoveredByNetwork(requirement);
            bool produced = StorageNetworkPlanPreviewText.CanProduceRequirement(requirement);
            Color color = covered ? PositiveColor() : produced ? WarningColor() : DangerColor();
            GameObject row = CreatePlainImage("MaterialLedgerRow", parent, depth == 0 ? new Color(0.78f, 0.79f, 0.73f, 1f) : new Color(0.71f, 0.73f, 0.68f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = depth == 0 ? 44f : 38f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8 + depth * 24, 8, 4, 4);
            layout.spacing = depth == 0 ? 8f : 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            if (depth > 0)
            {
                AddIndentGuide(row.transform, GetRequirementColor(requirement));
                AddFlowArrow(row.transform, GetRequirementColor(requirement));
            }

            AddMaterialIcon(row.transform, requirement.Material, depth == 0 ? 18f : 16f);
            TextMeshProUGUI name = CreateOrderText("MaterialName", row.transform, ProductionOrderFormatting.GetTagDisplayName(requirement.Material), depth == 0 ? 9 : 8, TextAlignmentOptions.MidlineLeft);
            name.color = color;
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            AddLedgerValue(row.transform, GameUtil.GetFormattedMass(requirement.RequiredAmount), NeutralTextColor(), 82f);
            AddLedgerValue(row.transform, StorageNetworkPlanPreviewText.BuildLedgerStockText(requirement), color, 104f);
            AddStatusBadge(row.transform, StorageNetworkPlanPreviewText.GetDispatchStatusLabel(requirement), color, 128f);

            if (requirement.Child != null && depth < 2)
            {
                AddChildRouteRow(parent, requirement.Child, depth + 1);
                foreach (ProductionPlanRequirement childRequirement in requirement.Child.Requirements.Take(4))
                {
                    AddMaterialRow(parent, childRequirement, depth + 1);
                }
            }
        }

        private void AddChildRouteRow(Transform parent, ProductionPlanNode child, int depth)
        {
            GameObject row = CreatePlainImage("MaterialRouteRow", parent, new Color(0.69f, 0.69f, 0.62f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 36f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(28 + depth * 24, 8, 4, 4);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIndentGuide(row.transform, WarningColor());
            AddFlowArrow(row.transform, WarningColor());
            AddStatusBadge(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_AUTO), WarningColor(), 68f);
            TextMeshProUGUI route = CreateOrderText("RouteText", row.transform, string.Format("{0}  x{1}", child.Recipe != null ? child.Recipe.GetUIName(false) : "?", child.OrderCount), 8, TextAlignmentOptions.MidlineLeft);
            route.color = WarningColor();
            route.fontStyle = FontStyles.Bold;
            route.textWrappingMode = TextWrappingModes.NoWrap;
            route.overflowMode = TextOverflowModes.Ellipsis;
            route.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            AddStatusBadge(row.transform, StorageNetworkPlanPreviewText.BuildAssignmentSummary(child, 2), NeutralBlue(), 178f);
        }
    }
}
