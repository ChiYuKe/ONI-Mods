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
        private void AddPlanTreeNode(Transform parent, ProductionPlanNode node, int depth)
        {
            if (node == null || depth > 3)
            {
                return;
            }

            AddRecipeNode(parent, node, depth);
            List<ProductionPlanRequirement> requirements = node.Requirements
                .Where(requirement => requirement != null && requirement.Material != Tag.Invalid)
                .Take(depth == 0 ? 5 : 3)
                .ToList();
            if (requirements.Count == 0)
            {
                return;
            }

            AddConnector(parent, 34f, 2f, new Color(0.52f, 0.54f, 0.50f, 1f));
            GameObject branchColumn = new GameObject("RequirementBranches");
            branchColumn.transform.SetParent(parent, false);
            branchColumn.AddComponent<RectTransform>();
            LayoutElement branchLayout = branchColumn.AddComponent<LayoutElement>();
            branchLayout.preferredWidth = depth == 0 ? 520f : 360f;
            branchLayout.preferredHeight = 156f;
            VerticalLayoutGroup layout = branchColumn.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (ProductionPlanRequirement requirement in requirements)
            {
                AddRequirementBranch(branchColumn.transform, requirement, depth);
            }
        }

        private void AddRequirementBranch(Transform parent, ProductionPlanRequirement requirement, int depth)
        {
            GameObject row = new GameObject("RequirementBranch");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = requirement.Child != null && depth < 2 ? 76f : 36f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddConnector(row.transform, 18f, 2f, GetRequirementColor(requirement));
            AddMaterialNode(row.transform, requirement);
            if (requirement.Child != null && depth < 2)
            {
                AddConnector(row.transform, 24f, 2f, WarningColor());
                AddPlanTreeNode(row.transform, requirement.Child, depth + 1);
            }
        }

        private void AddRecipeNode(Transform parent, ProductionPlanNode node, int depth)
        {
            GameObject card = CreatePlainImage("RecipeNode", parent, new Color(0.76f, 0.77f, 0.70f, 1f));
            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = depth == 0 ? 190f : 164f;
            cardLayout.preferredHeight = depth == 0 ? 84f : 70f;
            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 6, 6);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddIcon(card.transform, node.Recipe?.GetUIIcon(), depth == 0 ? 38f : 30f);
            GameObject textColumn = new GameObject("RecipeText");
            textColumn.transform.SetParent(card.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(textColumn, 1f, 0, 0, 0, 0);

            TextMeshProUGUI title = CreateText("RecipeName", textColumn.transform, node.Recipe != null ? node.Recipe.GetUIName(false) : "?", depth == 0 ? 10 : 9, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.18f, 0.20f, 0.19f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = depth == 0 ? 20f : 17f;

            TextMeshProUGUI meta = CreateText("RecipeMeta", textColumn.transform, string.Format("x{0}  {1}", node.OrderCount, node.FabricatorName), 8, TextAlignmentOptions.MidlineLeft);
            meta.color = NeutralTextColor();
            meta.textWrappingMode = TextWrappingModes.NoWrap;
            meta.overflowMode = TextOverflowModes.Ellipsis;
            meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            TextMeshProUGUI output = CreateText("RecipeOutput", textColumn.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_OUTPUT_AMOUNT), GameUtil.GetFormattedMass(node.OutputAmount * node.OrderCount)), 8, TextAlignmentOptions.MidlineLeft);
            output.color = PositiveColor();
            output.textWrappingMode = TextWrappingModes.NoWrap;
            output.overflowMode = TextOverflowModes.Ellipsis;
            output.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        private void AddMaterialNode(Transform parent, ProductionPlanRequirement requirement)
        {
            Color color = GetRequirementColor(requirement);
            GameObject card = CreatePlainImage("MaterialNode", parent, new Color(0.80f, 0.80f, 0.74f, 1f));
            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = 178f;
            cardLayout.preferredHeight = 36f;
            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(5, 6, 4, 4);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddMaterialIcon(card.transform, requirement.Material, 18f);
            GameObject textColumn = new GameObject("MaterialText");
            textColumn.transform.SetParent(card.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(textColumn, 0f, 0, 0, 0, 0);

            TextMeshProUGUI name = CreateText("MaterialName", textColumn.transform, ProductionOrderFormatting.GetTagDisplayName(requirement.Material), 8, TextAlignmentOptions.MidlineLeft);
            name.color = color;
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

            string status = StorageNetworkPlanPreviewText.BuildResearchAmountText(requirement, true);
            TextMeshProUGUI detail = CreateText("MaterialDetail", textColumn.transform, status, 7, TextAlignmentOptions.MidlineLeft);
            detail.color = NeutralTextColor();
            detail.textWrappingMode = TextWrappingModes.NoWrap;
            detail.overflowMode = TextOverflowModes.Ellipsis;
            detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 13f;
        }

        private static void AddConnector(Transform parent, float width, float height, Color color)
        {
            GameObject slot = new GameObject("ConnectorSlot");
            slot.transform.SetParent(parent, false);
            slot.AddComponent<RectTransform>();
            LayoutElement slotLayout = slot.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = width;
            slotLayout.preferredHeight = 24f;
            slotLayout.flexibleHeight = 0f;

            GameObject line = CreatePlainImage("Connector", slot.transform, color);
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void AddMaterialIcon(Transform parent, Tag tag, float size)
        {
            AddIcon(parent, GetTagIcon(tag), size);
        }

        private Color GetRequirementColor(ProductionPlanRequirement requirement)
        {
            if (StorageNetworkPlanPreviewText.IsCoveredByNetwork(requirement))
            {
                return PositiveColor();
            }

            return requirement.Child != null ? WarningColor() : DangerColor();
        }
    }
}
