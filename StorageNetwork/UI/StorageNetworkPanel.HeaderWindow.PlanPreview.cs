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

            float currentCycle = GetCurrentCycleTime();
            float estimateSeconds = productionOrderService.EstimatePlanSeconds(draft.Plan, out bool infinite);
            string finish = infinite ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_UNKNOWN) : ProductionOrderFormatting.FormatCycle(currentCycle + estimateSeconds / 600f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_CURRENT_CYCLE), ProductionOrderFormatting.FormatCycle(currentCycle), NeutralBlue(), 86f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_FINISH), finish, infinite ? WarningColor() : PositiveColor(), 92f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_EQUIPMENT), draft.Plan?.Assignments.Count.ToString() ?? "0", NeutralBlue(), 72f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_AUTO_PRODUCE), draft.ProducedRequirementCount.ToString(), draft.ProducedRequirementCount > 0 ? WarningColor() : PositiveColor(), 76f);
            AddMetricTile(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_METRIC_BLOCKED), draft.BlockedRequirementCount.ToString(), draft.BlockedRequirementCount > 0 ? DangerColor() : PositiveColor(), 70f);
        }

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
            content.sizeDelta = new Vector2(EstimateMaterialTreeWidth(plan), compactOrderWindow ? 238f : 268f);

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
            viewport.AddComponent<ScrollWheelBlocker>();

            GameObject contentObject = new GameObject("MaterialResearchTreeContent");
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = new Vector2(0f, 0f);

            float contentHeight = EstimateResearchTreeHeight(draft.Plan, 0) + 32f;
            float contentWidth = Mathf.Max(680f, EstimateResearchTreeDepth(draft.Plan, 0) * 500f + 470f);
            content.sizeDelta = new Vector2(contentWidth, Mathf.Max(compactOrderWindow ? 250f : 310f, contentHeight));

            float cursorY = 16f;
            AddResearchRecipeBranch(content.transform, draft.Plan, 0, ref cursorY);

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.08f;
            scrollRect.scrollSensitivity = 24f;
        }

        private float AddResearchRecipeBranch(Transform parent, ProductionPlanNode node, int depth, ref float cursorY)
        {
            const float columnStep = 540f;
            const float recipeWidth = 180f;
            const float recipeHeight = 82f;
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
            GameObject card = CreatePlainImage("ResearchRecipeNode", parent, new Color(0.42f, 0.42f, 0.38f, 1f));
            ApplyResearchNodeRect(card, position, size);

            GameObject header = CreatePlainImage("ResearchRecipeHeader", card.transform, new Color(0.19f, 0.20f, 0.18f, 1f));
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(0f, -22f);
            headerRect.offsetMax = Vector2.zero;

            TextMeshProUGUI title = CreateOrderText("RecipeTitle", header.transform, node.FabricatorName, depth == 0 ? 8 : 7, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.95f, 0.94f, 0.88f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(title.rectTransform(), 6f, 0f);

            AddResearchRecipeSlots(card.transform, node);
            AddResearchProgressLine(card.transform, 56f, string.Format("{0} / {1}", GameUtil.GetFormattedMass(node.OutputAmount * node.OrderCount), GameUtil.GetFormattedMass(node.OutputAmount * node.OrderCount)), PositiveColor());
            AddResearchProgressLine(card.transform, 69f, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RESEARCH_BATCH_SUMMARY), node.OrderCount, BuildAssignmentSummary(node, 1)), NeutralBlue());
        }

        private void AddResearchRecipeSlots(Transform parent, ProductionPlanNode node)
        {
            AddResearchIconSlot(parent, node.Recipe?.GetUIIcon(), new Vector2(7f, 27f), 26f);
            int index = 0;
            foreach (ProductionPlanAssignment assignment in node.Assignments.Take(4))
            {
                Sprite sprite = assignment.Fabricator != null ? GetFabricatorSprite(assignment.Fabricator) : null;
                AddResearchIconSlot(parent, sprite, new Vector2(37f + index * 30f, 27f), 26f);
                index++;
            }
        }

        private void AddResearchMaterialNode(Transform parent, ProductionPlanRequirement requirement, Vector2 position, Vector2 size)
        {
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
            bool covered = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool produced = !covered && requirement.Child != null;
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

            string amount = covered
                ? string.Format("{0}/{1}", GameUtil.GetFormattedMass(requirement.AvailableAmount), GameUtil.GetFormattedMass(requirement.RequiredAmount))
                : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RESEARCH_MISSING_STOCK), GameUtil.GetFormattedMass(missing), GameUtil.GetFormattedMass(requirement.AvailableAmount));
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

        private static void ApplyResearchNodeRect(GameObject node, Vector2 position, Vector2 size)
        {
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }

        private void AddResearchIconSlot(Transform parent, Sprite sprite, float size)
        {
            GameObject slot = CreatePlainImage("ResearchIconSlot", parent, new Color(0.93f, 0.92f, 0.87f, 1f));
            LayoutElement layout = slot.AddComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            layout.minWidth = size;
            layout.minHeight = size;
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slot.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2f, 2f);
            iconRect.offsetMax = new Vector2(-2f, -2f);
            Image image = icon.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void AddResearchIconSlot(Transform parent, Sprite sprite, Vector2 topLeft, float size)
        {
            GameObject slot = CreatePlainImage("ResearchIconSlot", parent, new Color(0.93f, 0.92f, 0.87f, 1f));
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            slotRect.sizeDelta = new Vector2(size, size);

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slot.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2f, 2f);
            iconRect.offsetMax = new Vector2(-2f, -2f);
            Image image = icon.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void AddResearchProgressLine(Transform parent, float y, string text, Color color)
        {
            GameObject bar = CreatePlainImage("ResearchProgress", parent, new Color(0.61f, 0.61f, 0.57f, 1f));
            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(7f, -y - 11f);
            rect.offsetMax = new Vector2(-7f, -y);

            TextMeshProUGUI label = CreateOrderText("ProgressText", bar.transform, text, 7, TextAlignmentOptions.Center);
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform(), 2f, 0f);
        }

        private static void AddResearchConnector(Transform parent, float x1, float y1, float x2, float y2, Color color)
        {
            if (Mathf.Abs(x1 - x2) > 0.5f)
            {
                AddResearchLine(parent, new Vector2(Mathf.Min(x1, x2), y1), new Vector2(Mathf.Abs(x2 - x1), 2f), color);
            }

            if (Mathf.Abs(y1 - y2) > 0.5f)
            {
                AddResearchLine(parent, new Vector2(x2, Mathf.Min(y1, y2)), new Vector2(2f, Mathf.Abs(y2 - y1)), color);
            }
        }

        private static void AddResearchVerticalBus(Transform parent, float x, List<float> yCenters, Color color)
        {
            if (yCenters == null || yCenters.Count <= 1)
            {
                return;
            }

            float minY = yCenters.Min();
            float maxY = yCenters.Max();
            AddResearchLine(parent, new Vector2(x, minY), new Vector2(2f, Mathf.Max(2f, maxY - minY)), color);
        }

        private static void AddResearchLine(Transform parent, Vector2 topLeft, Vector2 size, Color color)
        {
            GameObject line = CreatePlainImage("ResearchConnector", parent, color);
            line.transform.SetAsFirstSibling();
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rect.sizeDelta = size;
        }

        private static float EstimateResearchTreeHeight(ProductionPlanNode node, int depth)
        {
            if (node == null)
            {
                return 0f;
            }

            List<ProductionPlanRequirement> requirements = node.Requirements
                .Where(requirement => requirement != null && requirement.Material != Tag.Invalid)
                .Take(depth == 0 ? 5 : 4)
                .ToList();
            if (requirements.Count == 0)
            {
                return 104f;
            }

            float height = 0f;
            foreach (ProductionPlanRequirement requirement in requirements)
            {
                height += requirement.Child != null && depth < 2
                    ? Mathf.Max(74f, EstimateResearchTreeHeight(requirement.Child, depth + 1))
                    : 74f;
            }

            return Mathf.Max(104f, height);
        }

        private static int EstimateResearchTreeDepth(ProductionPlanNode node, int depth)
        {
            if (node == null || depth >= 3)
            {
                return depth + 1;
            }

            int maxDepth = depth + 1;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement != null && requirement.Child != null)
                {
                    maxDepth = Mathf.Max(maxDepth, EstimateResearchTreeDepth(requirement.Child, depth + 1));
                }
            }

            return maxDepth;
        }

        private static Sprite GetFabricatorSprite(ComplexFabricator fabricator)
        {
            if (fabricator == null || fabricator.gameObject == null)
            {
                return Assets.GetSprite("unknown");
            }

            KPrefabID prefabId = fabricator.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                var uiSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                if (uiSprite.first != null)
                {
                    return uiSprite.first;
                }
            }

            return Assets.GetSprite("unknown");
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
            AddPlanLine(text.transform, string.Format("x{0}  {1}", node.OrderCount, BuildAssignmentSummary(node, 2)), 8, FontStyles.Bold, NeutralBlue(), 17f);
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
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
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

            string detail = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? string.Format("{0} / {1}", GameUtil.GetFormattedMass(requirement.AvailableAmount), GameUtil.GetFormattedMass(requirement.RequiredAmount))
                : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RESEARCH_MISSING_STOCK), GameUtil.GetFormattedMass(missing), GameUtil.GetFormattedMass(requirement.AvailableAmount));
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

        private static float EstimateMaterialTreeWidth(ProductionPlanNode node)
        {
            return Mathf.Max(760f, 240f + EstimateMaterialTreeDepth(node, 0) * 360f);
        }

        private static int EstimateMaterialTreeDepth(ProductionPlanNode node, int depth)
        {
            if (node == null || depth >= 3)
            {
                return depth;
            }

            int maxDepth = depth;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement != null && requirement.Child != null)
                {
                    maxDepth = Mathf.Max(maxDepth, EstimateMaterialTreeDepth(requirement.Child, depth + 1));
                }
            }

            return maxDepth;
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
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
            bool covered = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool produced = !covered && requirement.Child != null;
            Color color = covered ? PositiveColor() : produced ? WarningColor() : DangerColor();
            string name = ProductionOrderFormatting.GetTagDisplayName(requirement.Material);
            string stockLine = covered
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_REQUIREMENT_STOCK), GameUtil.GetFormattedMass(requirement.RequiredAmount), GameUtil.GetFormattedMass(requirement.AvailableAmount))
                : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_REQUIREMENT_MISSING), GameUtil.GetFormattedMass(requirement.RequiredAmount), GameUtil.GetFormattedMass(missing));
            string actionLine = covered
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ACTION_DIRECT)
                : produced
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ACTION_AUTO), BuildAssignmentSummary(requirement.Child, 3))
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ACTION_BLOCKED);

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
            string assignmentText = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DEVICE_LINE), BuildAssignmentSummary(child, 3));
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
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
            bool covered = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool produced = !covered && requirement.Child != null;
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
            AddLedgerValue(row.transform, covered ? GameUtil.GetFormattedMass(requirement.AvailableAmount) : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MISSING_PREFIX), GameUtil.GetFormattedMass(missing)), color, 104f);
            AddStatusBadge(row.transform, covered ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DIRECT) : produced ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_AUTO) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_BLOCKED), color, 128f);

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

            AddStatusBadge(row.transform, BuildAssignmentSummary(child, 2), NeutralBlue(), 178f);
        }

        private static void AddIndentGuide(Transform parent, Color color)
        {
            GameObject guide = CreatePlainImage("IndentGuide", parent, color);
            LayoutElement layout = guide.AddComponent<LayoutElement>();
            layout.preferredWidth = 3f;
            layout.preferredHeight = 22f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private static void AddFixedSpacer(Transform parent, float width)
        {
            GameObject spacer = new GameObject("FixedSpacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().preferredWidth = width;
        }

        private void AddLedgerValue(Transform parent, string value, Color color, float width)
        {
            TextMeshProUGUI text = CreateOrderText("LedgerValue", parent, value, 8, TextAlignmentOptions.MidlineLeft);
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.flexibleWidth = 0f;
        }

        private void AddCompactStat(Transform parent, string label, string value, Color valueColor, float width)
        {
            GameObject stat = new GameObject("CompactStat");
            stat.transform.SetParent(parent, false);
            stat.AddComponent<RectTransform>();
            LayoutElement statLayout = stat.AddComponent<LayoutElement>();
            statLayout.preferredWidth = width;
            statLayout.flexibleWidth = 0f;
            AddVerticalContainer(stat, 0f, 0, 0, 0, 0);

            TextMeshProUGUI labelText = CreateOrderText("StatLabel", stat.transform, label, 7, TextAlignmentOptions.MidlineLeft);
            labelText.color = MutedTextColor();
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            labelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

            TextMeshProUGUI valueText = CreateOrderText("StatValue", stat.transform, value, 8, TextAlignmentOptions.MidlineLeft);
            valueText.color = valueColor;
            valueText.fontStyle = FontStyles.Bold;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;
        }

        private void AddStatusBadge(Transform parent, string text, Color color, float width)
        {
            GameObject badge = CreatePlainImage("StatusBadge", parent, new Color(0.69f, 0.70f, 0.64f, 1f));
            LayoutElement badgeLayout = badge.AddComponent<LayoutElement>();
            badgeLayout.preferredWidth = width;
            badgeLayout.preferredHeight = 24f;
            TextMeshProUGUI label = CreateOrderText("StatusBadgeText", badge.transform, text, 8, TextAlignmentOptions.Center);
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform(), 4f, 0f);
        }

        private static string BuildAssignmentSummary(ProductionPlanNode node, int maxItems)
        {
            if (node == null || node.Assignments == null || node.Assignments.Count == 0)
            {
                return node != null ? node.FabricatorName : "?";
            }

            List<string> names = node.Assignments
                .Take(maxItems)
                .Select(assignment => string.Format("{0} x{1}", assignment.Fabricator != null ? assignment.Fabricator.GetProperName() : "?", assignment.OrderCount))
                .ToList();
            if (node.Assignments.Count > maxItems)
            {
                names.Add("+" + (node.Assignments.Count - maxItems));
            }

            return string.Join(" / ", names.ToArray());
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
            AddStatusBadge(row.transform, BuildAssignmentSummary(node, 3), NeutralBlue(), 260f);
        }

        private void AddDispatchRequirementRow(Transform parent, ProductionPlanRequirement requirement)
        {
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
            bool covered = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool produced = !covered && requirement.Child != null;
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
            AddStatusBadge(row.transform, covered ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_SEND_FROM_NETWORK) : produced ? BuildAssignmentSummary(requirement.Child, 3) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_NO_ROUTE), color, 260f);
        }

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
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
            bool covered = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool produced = !covered && requirement.Child != null;
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
                AddFlowStatusPill(row.transform, BuildAssignmentSummary(requirement.Child, 2), WarningColor(), 132f);
            }
            else
            {
                AddFlowStatusPill(
                    row.transform,
                    covered ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DIRECT) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STILL_MISSING),
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
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
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
            string textValue = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? string.Format("{0}  {1}/{2}", ProductionOrderFormatting.GetTagDisplayName(requirement.Material), GameUtil.GetFormattedMass(requirement.AvailableAmount), GameUtil.GetFormattedMass(requirement.RequiredAmount))
                : string.Format("{0}  {1}", ProductionOrderFormatting.GetTagDisplayName(requirement.Material), string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MISSING_PREFIX), GameUtil.GetFormattedMass(missing)));
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
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
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

            string status = missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? string.Format("{0} / {1}", GameUtil.GetFormattedMass(requirement.AvailableAmount), GameUtil.GetFormattedMass(requirement.RequiredAmount))
                : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RESEARCH_MISSING_STOCK), GameUtil.GetFormattedMass(missing), GameUtil.GetFormattedMass(requirement.AvailableAmount));
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
            float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
            if (missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return PositiveColor();
            }

            return requirement.Child != null ? WarningColor() : DangerColor();
        }

    }
}

