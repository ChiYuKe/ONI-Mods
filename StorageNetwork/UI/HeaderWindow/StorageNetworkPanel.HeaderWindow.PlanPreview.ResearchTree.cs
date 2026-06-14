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
        private void AddMaterialResearchTreeViewport(Transform parent, ProductionOrderDraft draft)
        {
            if (draft.Plan == null || draft.Plan.Requirements.Count == 0)
            {
                AddInfoText(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_MATERIALS), 36f);
                return;
            }

            GameObject viewport = CreatePlainImage("MaterialResearchTreeViewport", parent, new Color(0.88f, 0.86f, 0.79f, 1f));
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect, 10f, 10f);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("MaterialResearchTreeContent");
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = new Vector2(0f, 0f);

            float contentHeight = StorageNetworkPlanPreviewMetrics.EstimateResearchTreeHeight(draft.Plan, 0) + 32f;
            float contentWidth = Mathf.Max(680f, StorageNetworkPlanPreviewMetrics.EstimateResearchTreeDepth(draft.Plan, 0) * 500f + 470f);
            content.sizeDelta = new Vector2(contentWidth, Mathf.Max(compactOrderWindow ? 250f : 310f, contentHeight));

            float cursorY = 16f;
            AddResearchRecipeBranch(content.transform, draft.Plan, 0, ref cursorY);

            viewport.AddComponent<StorageNetworkPanZoom>().Configure(viewportRect, content);
        }

        private float AddResearchRecipeBranch(Transform parent, ProductionPlanNode node, int depth, ref float cursorY)
        {
            const float columnStep = 570f;
            const float recipeWidth = 226f;
            const float recipeHeight = 98f;
            const float materialWidth = 206f;
            const float materialHeight = 58f;
            const float rowGap = 18f;

            float recipeX = 18f + depth * columnStep;
            float materialX = recipeX + recipeWidth + 82f;
            float startY = cursorY;
            List<ProductionPlanRequirement> requirements = node.Requirements
                .Where(requirement => requirement != null && requirement.Material != Tag.Invalid)
                .Take(depth == 0 ? 5 : 4)
                .ToList();

            if (requirements.Count == 0)
            {
                cursorY += recipeHeight + rowGap;
                AddResearchRecipeNode(parent, node, depth, new Vector2(recipeX, startY), new Vector2(recipeWidth, recipeHeight));
                return startY + recipeHeight * 0.5f;
            }

            List<float> materialCenters = new List<float>();
            foreach (ProductionPlanRequirement requirement in requirements)
            {
                float materialY = cursorY;
                float materialCenter = materialY + materialHeight * 0.5f;

                if (requirement.Child != null && depth < 2)
                {
                    float childCursor = cursorY;
                    float childCenter = AddResearchRecipeBranch(parent, requirement.Child, depth + 1, ref childCursor);
                    childCursor = Mathf.Max(childCursor, cursorY + recipeHeight + rowGap);
                    materialCenter = childCenter;
                    materialY = materialCenter - materialHeight * 0.5f;
                    cursorY = Mathf.Max(childCursor, materialY + materialHeight + rowGap);
                    AddResearchConnector(parent, materialX + materialWidth + 8f, materialCenter, 18f + (depth + 1) * columnStep - 8f, childCenter, WarningColor());
                }
                else
                {
                    cursorY += materialHeight + rowGap;
                }

                AddResearchMaterialNode(parent, requirement, new Vector2(materialX, materialY), new Vector2(materialWidth, materialHeight));
                materialCenters.Add(materialCenter);
            }

            float recipeCenter = (materialCenters.First() + materialCenters.Last()) * 0.5f;
            float recipeY = Mathf.Max(startY, recipeCenter - recipeHeight * 0.5f);
            AddResearchRecipeNode(parent, node, depth, new Vector2(recipeX, recipeY), new Vector2(recipeWidth, recipeHeight));
            AddResearchConnector(parent, recipeX + recipeWidth + 8f, recipeCenter, materialX - 8f, recipeCenter, new Color(0.52f, 0.54f, 0.50f, 1f));
            AddResearchVerticalBus(parent, materialX - 22f, materialCenters, new Color(0.52f, 0.54f, 0.50f, 1f));
            foreach (float materialCenter in materialCenters)
            {
                AddResearchConnector(parent, materialX - 22f, materialCenter, materialX - 8f, materialCenter, new Color(0.52f, 0.54f, 0.50f, 1f));
            }

            return recipeCenter;
        }

        private void AddResearchRecipeNode(Transform parent, ProductionPlanNode node, int depth, Vector2 position, Vector2 size)
        {
            // 生产节点卡片采用固定三段布局：顶部建筑栏、中部配方信息、底部产出条。
            Vector2 headerIconPosition = new Vector2(8f, 4f);
            Vector2 fabricatorIconPosition = new Vector2(25f, 42f);
            Vector2 textColumnLeft = new Vector2(78f, 0f);
            Vector2 textColumnRight = new Vector2(-12f, 0f);
            const float progressY = 79f;

            GameObject card = CreatePlainImage("ResearchRecipeNode", parent, depth == 0 ? new Color(0.78f, 0.78f, 0.72f, 1f) : new Color(0.74f, 0.74f, 0.68f, 1f));
            ApplyOniInputSlotStyle(card.GetComponent<Image>());
            ApplyResearchNodeRect(card, position, size);

            GameObject accent = CreatePlainImage("ResearchRecipeAccent", card.transform, NeutralBlue());
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.offsetMin = new Vector2(7f, 24f);
            accentRect.offsetMax = new Vector2(12f, -8f);

            GameObject header = CreatePlainImage("ResearchRecipeHeader", card.transform, new Color(0.90f, 0.88f, 0.80f, 1f));
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(18f, -34f);
            headerRect.offsetMax = new Vector2(-10f, -8f);

            Sprite fabricatorIcon = node.Assignments
                .Select(assignment => assignment.Fabricator != null ? GetFabricatorSprite(assignment.Fabricator) : null)
                .FirstOrDefault(sprite => sprite != null);
            AddResearchIconSlot(header.transform, node.Recipe?.GetUIIcon(), headerIconPosition, 20f);

            TextMeshProUGUI title = CreateOrderText("RecipeTitle", header.transform, node.FabricatorName, depth == 0 ? 9 : 8, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.25f, 0.29f, 0.29f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(title.rectTransform(), 7f, 0f);
            title.rectTransform().offsetMin = new Vector2(34f, 0f);
            title.rectTransform().offsetMax = new Vector2(-66f, 0f);

            GameObject countBadge = CreatePlainImage("MachineCountBadge", header.transform, new Color(0.38f, 0.46f, 0.50f, 1f));
            RectTransform badgeRect = countBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 0.5f);
            badgeRect.anchorMax = new Vector2(1f, 0.5f);
            badgeRect.pivot = new Vector2(1f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-6f, 0f);
            badgeRect.sizeDelta = new Vector2(52f, 20f);
            TextMeshProUGUI badge = CreateOrderText("MachineCountText", countBadge.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MACHINE_COUNT), node.Assignments.Count), 7, TextAlignmentOptions.Center);
            badge.color = new Color(0.95f, 0.94f, 0.88f, 1f);
            badge.fontStyle = FontStyles.Bold;
            badge.textWrappingMode = TextWrappingModes.NoWrap;
            badge.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(badge.rectTransform(), 2f, 0f);

            AddResearchIconSlot(card.transform, fabricatorIcon, fabricatorIconPosition, 40f);

            TextMeshProUGUI recipe = CreateOrderText("RecipeName", card.transform, node.Recipe != null ? node.Recipe.GetUIName(false) : "?", depth == 0 ? 9 : 8, TextAlignmentOptions.MidlineLeft);
            recipe.color = new Color(0.18f, 0.20f, 0.19f, 1f);
            recipe.fontStyle = FontStyles.Bold;
            recipe.textWrappingMode = TextWrappingModes.NoWrap;
            recipe.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform recipeRect = recipe.rectTransform();
            recipeRect.anchorMin = new Vector2(0f, 1f);
            recipeRect.anchorMax = new Vector2(1f, 1f);
            recipeRect.pivot = new Vector2(0.5f, 1f);
            recipeRect.offsetMin = new Vector2(textColumnLeft.x, -51f);
            recipeRect.offsetMax = new Vector2(textColumnRight.x, -34f);

            TextMeshProUGUI assignment = CreateOrderText("RecipeAssignment", card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RESEARCH_BATCH_SUMMARY), node.OrderCount, StorageNetworkPlanPreviewText.BuildAssignmentSummary(node, 1)), 7, TextAlignmentOptions.MidlineLeft);
            assignment.color = NeutralTextColor();
            assignment.fontStyle = FontStyles.Bold;
            assignment.textWrappingMode = TextWrappingModes.NoWrap;
            assignment.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform assignmentRect = assignment.rectTransform();
            assignmentRect.anchorMin = new Vector2(0f, 1f);
            assignmentRect.anchorMax = new Vector2(1f, 1f);
            assignmentRect.pivot = new Vector2(0.5f, 1f);
            assignmentRect.offsetMin = new Vector2(textColumnLeft.x, -69f);
            assignmentRect.offsetMax = new Vector2(textColumnRight.x, -52f);

            AddResearchProgressLine(card.transform, progressY, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_OUTPUT_AMOUNT), GameUtil.GetFormattedMass(node.OutputAmount * node.OrderCount)), PositiveColor());
        }

        private void AddResearchMaterialNode(Transform parent, ProductionPlanRequirement requirement, Vector2 position, Vector2 size)
        {
            bool covered = StorageNetworkPlanPreviewText.IsCoveredByNetwork(requirement);
            bool produced = StorageNetworkPlanPreviewText.CanProduceRequirement(requirement);
            Color statusColor = covered ? PositiveColor() : produced ? WarningColor() : DangerColor();

            GameObject card = CreatePlainImage("ResearchMaterialNode", parent, new Color(0.74f, 0.74f, 0.68f, 1f));
            ApplyResearchNodeRect(card, position, size);

            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 6, 5);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddResearchIconSlot(card.transform, GetTagIcon(requirement.Material), 34f);

            GameObject textColumn = new GameObject("ResearchMaterialText");
            textColumn.transform.SetParent(card.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(textColumn, 1f, 0, 0, 0, 0);

            TextMeshProUGUI name = CreateOrderText("MaterialName", textColumn.transform, ProductionOrderFormatting.GetTagDisplayName(requirement.Material), 9, TextAlignmentOptions.MidlineLeft);
            name.color = statusColor;
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            string amount = StorageNetworkPlanPreviewText.BuildResearchAmountText(requirement, false);
            TextMeshProUGUI detail = CreateOrderText("MaterialAmount", textColumn.transform, amount, 8, TextAlignmentOptions.MidlineLeft);
            detail.color = NeutralTextColor();
            detail.textWrappingMode = TextWrappingModes.NoWrap;
            detail.overflowMode = TextOverflowModes.Ellipsis;
            detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            TextMeshProUGUI status = CreateOrderText("MaterialStatus", textColumn.transform, covered ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DIRECT) : produced ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_AUTO) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_NO_ROUTE), 8, TextAlignmentOptions.MidlineLeft);
            status.color = statusColor;
            status.fontStyle = FontStyles.Bold;
            status.textWrappingMode = TextWrappingModes.NoWrap;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }
    }
}
