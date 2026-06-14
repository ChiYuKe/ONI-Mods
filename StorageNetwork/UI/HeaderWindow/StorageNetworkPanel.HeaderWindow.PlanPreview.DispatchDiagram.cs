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
        private void AddMaterialDispatchDiagram(Transform parent, ProductionOrderDraft draft)
        {
            if (draft.Plan == null || draft.Plan.Requirements.Count == 0)
            {
                AddInfoText(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_MATERIALS), 36f);
                return;
            }

            GameObject root = new GameObject("MaterialDispatchDiagram");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Stretch(rootRect, 10f, 10f);

            bool wide = false;
            AddDiagramRootNode(root.transform, draft.Plan, wide);
            AddDiagramConnector(root.transform, 228f, 58f, Mathf.Min(3, draft.Plan.Requirements.Count));
            AddDiagramMaterialStack(root.transform, draft.Plan.Requirements.Take(4).ToList(), wide);
        }

        private void AddDiagramRootNode(Transform parent, ProductionPlanNode node, bool wide)
        {
            GameObject card = CreatePlainImage("DiagramRootNode", parent, new Color(0.82f, 0.80f, 0.73f, 1f));
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(wide ? 28f : 18f, 0f);
            rect.sizeDelta = new Vector2(wide ? 244f : 202f, wide ? 122f : 96f);

            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 10, 10, 10);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddIcon(card.transform, node.Recipe?.GetUIIcon(), wide ? 52f : 40f);

            GameObject text = new GameObject("RootText");
            text.transform.SetParent(card.transform, false);
            text.AddComponent<RectTransform>();
            text.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(text, 1f, 0, 0, 0, 0);

            AddPlanLine(text.transform, node.Recipe != null ? node.Recipe.GetUIName(false) : "?", 10, FontStyles.Bold, NeutralTextColor(), 19f);
            AddPlanLine(text.transform, string.Format("x{0}  {1}", node.OrderCount, StorageNetworkPlanPreviewText.BuildAssignmentSummary(node, 2)), 8, FontStyles.Bold, NeutralBlue(), 17f);
            AddPlanLine(text.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_OUTPUT_AMOUNT), GameUtil.GetFormattedMass(node.OutputAmount * node.OrderCount)), 8, FontStyles.Bold, PositiveColor(), 17f);
        }

        private static void AddDiagramConnector(Transform parent, float x, float stepY, int count)
        {
            if (count <= 0)
            {
                return;
            }

            float top = (count - 1) * stepY * 0.5f;
            AddAbsoluteLine(parent, new Vector2(x - 36f, 0f), new Vector2(x, 0f), new Color(0.36f, 0.37f, 0.33f, 1f));
            AddAbsoluteLine(parent, new Vector2(x, -top), new Vector2(x, top), new Color(0.36f, 0.37f, 0.33f, 1f));
            for (int i = 0; i < count; i++)
            {
                float y = top - i * stepY;
                AddAbsoluteLine(parent, new Vector2(x, y), new Vector2(x + 34f, y), new Color(0.36f, 0.37f, 0.33f, 1f));
            }
        }

        private void AddDiagramMaterialStack(Transform parent, List<ProductionPlanRequirement> requirements, bool wide)
        {
            GameObject stack = new GameObject("DiagramMaterialStack");
            stack.transform.SetParent(parent, false);
            RectTransform rect = stack.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(wide ? 390f : 264f, 0f);
            rect.sizeDelta = new Vector2(wide ? 340f : 184f, wide ? 278f : 236f);

            VerticalLayoutGroup layout = stack.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            foreach (ProductionPlanRequirement requirement in requirements)
            {
                AddDiagramMaterialNode(stack.transform, requirement, wide);
            }
        }

        private void AddDiagramMaterialNode(Transform parent, ProductionPlanRequirement requirement, bool wide)
        {
            Color color = GetRequirementColor(requirement);
            GameObject card = CreatePlainImage("DiagramMaterialNode", parent, new Color(0.86f, 0.85f, 0.79f, 1f));
            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = wide ? 58f : 46f;
            cardLayout.minHeight = cardLayout.preferredHeight;
            if (!wide)
            {
                cardLayout.preferredWidth = 184f;
                cardLayout.minWidth = 184f;
                cardLayout.flexibleWidth = 0f;
            }
            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddMaterialIcon(card.transform, requirement.Material, wide ? 34f : 28f);
            GameObject text = new GameObject("MaterialText");
            text.transform.SetParent(card.transform, false);
            text.AddComponent<RectTransform>();
            text.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(text, 0f, 0, 0, 0, 0);

            TextMeshProUGUI name = CreateOrderText("MaterialName", text.transform, ProductionOrderFormatting.GetTagDisplayName(requirement.Material), 9, TextAlignmentOptions.MidlineLeft);
            name.color = color;
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            string detail = StorageNetworkPlanPreviewText.BuildResearchAmountText(requirement, true);
            TextMeshProUGUI amount = CreateOrderText("MaterialAmount", text.transform, detail, 8, TextAlignmentOptions.MidlineLeft);
            amount.color = NeutralTextColor();
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        private static void AddAbsoluteLine(Transform parent, Vector2 start, Vector2 end, Color color)
        {
            GameObject line = CreatePlainImage("DiagramConnector", parent, color);
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(Mathf.Max(2f, Mathf.Abs(end.x - start.x)), Mathf.Max(2f, Mathf.Abs(end.y - start.y)));
        }
    }
}
