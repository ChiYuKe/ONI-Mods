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
        private void AddPlanLedger(Transform parent, ProductionOrderDraft draft)
        {
            bool stackedLedger = compactOrderWindow || inlineOrderTracking;
            GameObject row = new GameObject("PlanLedger");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = stackedLedger ? 520f : 312f;
            HorizontalOrVerticalLayoutGroup layout = stackedLedger
                ? (HorizontalOrVerticalLayoutGroup)row.AddComponent<VerticalLayoutGroup>()
                : row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject assignment = CreateLedgerPanel(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ASSIGNMENT_PANEL), 0f, stackedLedger ? 0f : 272f, stackedLedger ? 1f : 0.40f);
            if (stackedLedger)
            {
                assignment.GetComponent<LayoutElement>().preferredHeight = 160f;
            }

            AddAssignmentTable(assignment.transform, draft);

            GameObject materials = CreateLedgerPanel(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MATERIAL_STOCK_PANEL), 0f, stackedLedger ? 0f : 388f, stackedLedger ? 1f : 0.60f);
            if (stackedLedger)
            {
                materials.GetComponent<LayoutElement>().preferredHeight = 352f;
            }

            AddMaterialTable(materials.transform, draft);
        }

        private void AddAssignmentTable(Transform parent, ProductionOrderDraft draft)
        {
            if (draft.Plan == null || draft.Plan.Assignments.Count == 0)
            {
                AddInfoText(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_EQUIPMENT), 36f);
                return;
            }

            foreach (ProductionPlanAssignment assignment in draft.Plan.Assignments.Take(6))
            {
                AddAssignmentCard(
                    parent,
                    assignment.Fabricator != null ? assignment.Fabricator.GetProperName() : "?",
                    assignment.OrderCount.ToString(),
                    GameUtil.GetFormattedMass(assignment.OutputAmount));
            }
        }

        private void AddMaterialTable(Transform parent, ProductionOrderDraft draft)
        {
            if (draft.Plan == null || draft.Plan.Requirements.Count == 0)
            {
                AddInfoText(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_MATERIALS), 36f);
                return;
            }

            if (inlineOrderTracking || compactOrderWindow)
            {
                foreach (ProductionPlanRequirement requirement in draft.Plan.Requirements.Take(6))
                {
                    AddMaterialCard(parent, requirement, 0);
                }

                return;
            }

            AddMaterialLedgerHeader(parent);
            foreach (ProductionPlanRequirement requirement in draft.Plan.Requirements.Take(6))
            {
                AddMaterialRow(parent, requirement, 0);
            }
        }

        private void AddAssignmentCard(Transform parent, string fabricatorName, string orderCount, string outputAmount)
        {
            GameObject card = CreatePlainImage("AssignmentCard", parent, new Color(0.77f, 0.78f, 0.72f, 1f));
            card.AddComponent<LayoutElement>().preferredHeight = 42f;
            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 5, 5);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject textColumn = new GameObject("AssignmentText");
            textColumn.transform.SetParent(card.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(textColumn, 0f, 0, 0, 0, 0);

            TextMeshProUGUI name = CreateText("FabricatorName", textColumn.transform, fabricatorName, 9, TextAlignmentOptions.MidlineLeft);
            name.color = NeutralTextColor();
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;

            TextMeshProUGUI meta = CreateText("AssignmentMeta", textColumn.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ASSIGNMENT_META), 8, TextAlignmentOptions.MidlineLeft);
            meta.color = MutedTextColor();
            meta.textWrappingMode = TextWrappingModes.NoWrap;
            meta.overflowMode = TextOverflowModes.Ellipsis;
            meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 15f;

                AddCompactStat(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_BATCHES), orderCount, NeutralBlue(), 50f);
            AddCompactStat(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_OUTPUT_LABEL), outputAmount, PositiveColor(), 78f);
        }
    }
}
