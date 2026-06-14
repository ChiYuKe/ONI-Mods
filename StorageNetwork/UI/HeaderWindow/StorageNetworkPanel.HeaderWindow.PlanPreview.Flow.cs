using System.Collections.Generic;
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
        private void AddChainSummary(Transform parent, ProductionPlanNode node)
        {
            GameObject summary = CreatePlainImage("ChainSummary", parent, new Color(0.76f, 0.77f, 0.70f, 1f));
            summary.AddComponent<LayoutElement>().preferredHeight = 44f;
            HorizontalLayoutGroup layout = summary.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIcon(summary.transform, node.Recipe?.GetUIIcon(), 30f);

            GameObject textColumn = new GameObject("ChainSummaryText");
            textColumn.transform.SetParent(summary.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(textColumn, 0f, 0, 0, 0, 0);

            string titleText = node.Recipe != null ? node.Recipe.GetUIName(false) : "?";
            TextMeshProUGUI title = CreateText("ChainTitle", textColumn.transform, titleText, 10, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.18f, 0.20f, 0.19f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            AddPlanLine(textColumn.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_FLOW_DESC), 8, FontStyles.Italic, MutedTextColor(), 16f);
            AddFlowStatusPill(summary.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_BATCH_COUNT), node.OrderCount), NeutralBlue(), 58f);
            AddFlowStatusPill(summary.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MACHINE_COUNT), node.Assignments.Count), NeutralBlue(), 58f);
            AddFlowStatusPill(summary.transform, GameUtil.GetFormattedMass(node.OutputAmount * node.OrderCount), PositiveColor(), 92f);
        }

        private GameObject CreateLedgerPanel(Transform parent, string title, float preferredWidth, float minWidth, float flexibleWidth)
        {
            GameObject panel = CreatePlainImage("LedgerPanel", parent, new Color(0.80f, 0.80f, 0.74f, 1f));
            LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
            panelLayout.preferredWidth = preferredWidth;
            panelLayout.minWidth = minWidth;
            panelLayout.flexibleWidth = flexibleWidth;
            AddVerticalContainer(panel, 4f, 6, 6, 6, 6);
            AddSmallTitle(panel.transform, title);
            return panel;
        }

        private void AddPlanFlowColumnHeader(Transform parent)
        {
            GameObject header = CreatePlainImage("FlowColumnHeader", parent, new Color(0.70f, 0.71f, 0.65f, 1f));
            header.AddComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 3, 3);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddFlowColumnLabel(header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_FLOW_RECIPE), 150f);
            AddFlowColumnSpacer(header.transform, 16f);
            AddFlowColumnLabel(header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_FLOW_MATERIAL), 184f);
            AddFlowColumnSpacer(header.transform, 16f);
            AddFlowColumnLabel(header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_FLOW_STATUS), 236f);
        }

        private void AddPlanFlowHeader(Transform parent, ProductionPlanNode node)
        {
            GameObject header = CreatePlainImage("FlowHeader", parent, new Color(0.76f, 0.77f, 0.70f, 1f));
            header.AddComponent<LayoutElement>().preferredHeight = 36f;
            HorizontalLayoutGroup layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIcon(header.transform, node.Recipe?.GetUIIcon(), 26f);
            TextMeshProUGUI title = CreateText("FlowTitle", header.transform, node.Recipe != null ? node.Recipe.GetUIName(false) : "?", 10, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.18f, 0.20f, 0.19f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            AddFlowStatusPill(header.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_BATCH_LABEL), node.OrderCount), NeutralBlue(), 74f);
            AddFlowStatusPill(header.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_EQUIPMENT_LABEL), node.Assignments.Count), NeutralBlue(), 74f);
            AddFlowStatusPill(header.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_OUTPUT_AMOUNT), GameUtil.GetFormattedMass(node.OutputAmount * node.OrderCount)), PositiveColor(), 116f);
        }

        private void AddPlanFlowRows(Transform parent, ProductionPlanNode node, int depth, int maxRows)
        {
            GameObject rows = new GameObject("FlowRows");
            rows.transform.SetParent(parent, false);
            rows.AddComponent<RectTransform>();
            rows.AddComponent<LayoutElement>().preferredHeight = 148f;
            VerticalLayoutGroup layout = rows.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            List<ProductionPlanRequirement> requirements = node.Requirements
                .Where(requirement => requirement != null && requirement.Material != Tag.Invalid)
                .Take(maxRows)
                .ToList();
            if (requirements.Count == 0)
            {
                AddInfoText(rows.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_MATERIALS), 36f);
                return;
            }

            foreach (ProductionPlanRequirement requirement in requirements)
            {
                AddPlanFlowRow(rows.transform, node, requirement, depth);
            }
        }

        private void AddPlanFlowRow(Transform parent, ProductionPlanNode sourceNode, ProductionPlanRequirement requirement, int depth)
        {
            bool covered = StorageNetworkPlanPreviewText.IsCoveredByNetwork(requirement);
            bool produced = StorageNetworkPlanPreviewText.CanProduceRequirement(requirement);
            Color statusColor = GetRequirementColor(requirement);
            GameObject row = CreatePlainImage("FlowRow", parent, covered ? new Color(0.78f, 0.80f, 0.73f, 1f) : produced ? new Color(0.80f, 0.78f, 0.70f, 1f) : new Color(0.80f, 0.73f, 0.70f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 34f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddFlowRecipePill(row.transform, sourceNode, depth == 0 ? 142f : 122f);
            AddFlowArrow(row.transform, statusColor);
            AddFlowMaterialPill(row.transform, requirement, 184f);
            if (produced && depth < 2)
            {
                AddFlowArrow(row.transform, WarningColor());
                AddFlowRecipePill(row.transform, requirement.Child, 164f);
                AddFlowArrow(row.transform, WarningColor());
                AddFlowStatusPill(row.transform, StorageNetworkPlanPreviewText.BuildAssignmentSummary(requirement.Child, 2), WarningColor(), 132f);
            }
            else
            {
                AddFlowStatusPill(
                    row.transform,
                    StorageNetworkPlanPreviewText.GetDispatchFlowStatusLabel(requirement),
                    statusColor,
                    82f);
            }
        }

        private void AddFlowColumnLabel(Transform parent, string text, float width)
        {
            TextMeshProUGUI label = CreateOrderText("FlowColumnLabel", parent, text, 8, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.26f, 0.29f, 0.27f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
        }

        private static void AddFlowColumnSpacer(Transform parent, float width)
        {
            GameObject spacer = new GameObject("FlowColumnSpacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().preferredWidth = width;
        }

        private void AddFlowRecipePill(Transform parent, ProductionPlanNode node, float width)
        {
            GameObject pill = CreatePlainImage("RecipePill", parent, new Color(0.69f, 0.70f, 0.64f, 1f));
            LayoutElement pillLayout = pill.AddComponent<LayoutElement>();
            pillLayout.preferredWidth = width;
            pillLayout.preferredHeight = 26f;
            HorizontalLayoutGroup layout = pill.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 5, 3, 3);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIcon(pill.transform, node.Recipe?.GetUIIcon(), 20f);
            TextMeshProUGUI text = CreateOrderText("RecipePillText", pill.transform, string.Format("{0} x{1}", node.Recipe != null ? node.Recipe.GetUIName(false) : "?", node.OrderCount), 8, TextAlignmentOptions.MidlineLeft);
            text.color = new Color(0.17f, 0.19f, 0.18f, 1f);
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void AddFlowMaterialPill(Transform parent, ProductionPlanRequirement requirement, float width)
        {
            Color color = GetRequirementColor(requirement);
            GameObject pill = CreatePlainImage("MaterialPill", parent, new Color(0.84f, 0.84f, 0.78f, 1f));
            LayoutElement pillLayout = pill.AddComponent<LayoutElement>();
            pillLayout.preferredWidth = width;
            pillLayout.preferredHeight = 26f;
            HorizontalLayoutGroup layout = pill.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 5, 3, 3);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddMaterialIcon(pill.transform, requirement.Material, 16f);
            string textValue = StorageNetworkPlanPreviewText.BuildMaterialPillText(requirement);
            TextMeshProUGUI text = CreateOrderText("MaterialPillText", pill.transform, textValue, 8, TextAlignmentOptions.MidlineLeft);
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static void AddFlowArrow(Transform parent, Color color)
        {
            TextMeshProUGUI arrow = CreateText("FlowArrow", parent, ">", 11, TextAlignmentOptions.Center);
            arrow.color = color;
            arrow.fontStyle = FontStyles.Bold;
            arrow.gameObject.AddComponent<LayoutElement>().preferredWidth = 16f;
        }

        private void AddFlowStatusPill(Transform parent, string text, Color color, float width)
        {
            GameObject pill = CreatePlainImage("StatusPill", parent, new Color(0.72f, 0.73f, 0.67f, 1f));
            LayoutElement layout = pill.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 24f;
            TextMeshProUGUI label = CreateOrderText("StatusText", pill.transform, text, 8, TextAlignmentOptions.Center);
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform(), 4f, 0f);
        }

    }
}
