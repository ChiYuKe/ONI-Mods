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
        private void ShowOrderTrackingDetail(ProductionOrderRecord record)
        {
            if (record == null)
            {
                return;
            }

            CloseOrderTrackingDetail();
            Transform parent = headerWindowRoot != null && headerWindowRoot.transform.parent != null
                ? headerWindowRoot.transform.parent
                : transform;
            orderTrackingDetailRoot = CreateBox("OrderTrackingDetailWindow", parent, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(orderTrackingDetailRoot.GetComponent<Image>());
            RectTransform window = orderTrackingDetailRoot.GetComponent<RectTransform>();
            window.anchorMin = new Vector2(0.5f, 0.5f);
            window.anchorMax = new Vector2(0.5f, 0.5f);
            window.pivot = new Vector2(0.5f, 0.5f);
            window.anchoredPosition = Vector2.zero;
            window.sizeDelta = new Vector2(820f, 560f);
            StorageNetworkWindowDrag.TryApplyLayout("orderTrackingDetail", window, new Vector2(680f, 420f), new Vector2(1200f, 860f));

            GameObject header = CreateBox("Header", orderTrackingDetailRoot.transform, OniPinkInactive());
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 40f);
            header.AddComponent<StorageNetworkWindowDrag>().Configure(window, "orderTrackingDetail");

            TextMeshProUGUI title = CreateText(
                "Title",
                header.transform,
                string.Format("#{0} {1}", record.DisplayId, record.ProductName),
                15,
                TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.raycastTarget = false;
            Stretch(title.rectTransform(), 12f, 0f);
            title.rectTransform().offsetMax = new Vector2(-42f, 0f);

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, CloseOrderTrackingDetail);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-8f, 0f);
            closeRect.sizeDelta = new Vector2(26f, 24f);

            GameObject viewport = CreateBox("TrackingDetailViewport", orderTrackingDetailRoot.transform, new Color(0.88f, 0.86f, 0.79f, 1f));
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            SetStretch(viewportRect, 12f, 12f, 12f, 58f);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("TrackingDetailContent");
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;

            List<ProductionOrderQueueAssignment> assignments = (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment != null && ProductionOrderService.IsOrderProductionFabricator(assignment.Fabricator) && assignment.Recipe != null)
                .OrderByDescending(assignment => assignment.Primary)
                .ThenBy(assignment => assignment.Fabricator.GetProperName())
                .ToList();
            float contentHeight = Mathf.Max(420f, 80f + assignments.Count * 118f);
            content.sizeDelta = new Vector2(1100f, contentHeight);
            AddTrackingDetailTree(content.transform, record, assignments);
            viewport.AddComponent<StorageNetworkPanZoom>().Configure(viewportRect, content);
            StorageNetworkWindowDrag.ClampToScreen(window);
            orderTrackingDetailRoot.transform.SetAsLastSibling();
        }

        private void CloseOrderTrackingDetail()
        {
            if (orderTrackingDetailRoot != null)
            {
                Destroy(orderTrackingDetailRoot);
                orderTrackingDetailRoot = null;
            }
        }

        private void AddTrackingDetailTree(Transform parent, ProductionOrderRecord record, List<ProductionOrderQueueAssignment> assignments)
        {
            Vector2 rootPosition = new Vector2(28f, 36f);
            Vector2 fabricatorStart = new Vector2(330f, 24f);
            AddTrackingDetailOrderNode(parent, record, rootPosition);

            if (assignments.Count == 0)
            {
                AddTrackingDetailInfoNode(parent, new Vector2(330f, 62f), Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_EMPTY));
                AddResearchConnector(parent, 248f, 102f, 330f, 102f, MutedTextColor());
                return;
            }

            for (int i = 0; i < assignments.Count; i++)
            {
                ProductionOrderQueueAssignment assignment = assignments[i];
                float y = fabricatorStart.y + i * 118f;
                AddTrackingDetailFabricatorNode(parent, record, assignment, new Vector2(fabricatorStart.x, y));
                AddResearchConnector(parent, 248f, 102f, fabricatorStart.x - 14f, y + 52f, assignment.Primary ? NeutralBlue() : WarningColor());
                AddTrackingDetailMaterialNodes(parent, record, assignment, new Vector2(730f, y + 8f));
                AddResearchConnector(parent, fabricatorStart.x + 360f, y + 52f, 730f, y + 52f, assignment.Primary ? PositiveColor() : WarningColor());
            }
        }

        private void AddTrackingDetailOrderNode(Transform parent, ProductionOrderRecord record, Vector2 position)
        {
            GameObject card = CreatePlainImage("TrackingDetailOrderNode", parent, new Color(0.76f, 0.77f, 0.70f, 1f));
            ApplyOniInputSlotStyle(card.GetComponent<Image>());
            ApplyAbsoluteRect(card, position, new Vector2(220f, 150f));
            AddVerticalContainer(card, 5f, 10, 10, 10, 10);

            GameObject top = AddHorizontalRow(card.transform, 8f);
            top.GetComponent<LayoutElement>().preferredHeight = 42f;
            AddIcon(top.transform, GetTagIcon(record.ProductTag), 38f);
            TextMeshProUGUI title = CreateOrderText("OrderTitle", top.transform, string.Format("#{0} {1}", record.DisplayId, record.ProductName), 11, TextAlignmentOptions.MidlineLeft);
            title.color = NeutralTextColor();
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            AddPlanLine(card.transform, StorageNetworkOrderTrackingRules.GetOrderStateLabel(record.State), 10, FontStyles.Bold, GetOrderStateColor(record.State), 20f);
            AddPlanLine(card.transform, string.Format("{0} / {1}", GameUtil.GetFormattedMass(record.ProducedAtSubmit), GameUtil.GetFormattedMass(record.RequestedAmount)), 9, FontStyles.Bold, NeutralTextColor(), 18f);
            AddPlanLine(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_VALUE), ProductionOrderFormatting.FormatCycleStamp(record.LastActivityCycle)), 8, FontStyles.Normal, MutedTextColor(), 16f);
        }

        private void AddTrackingDetailFabricatorNode(Transform parent, ProductionOrderRecord record, ProductionOrderQueueAssignment assignment, Vector2 position)
        {
            ComplexFabricator fabricator = assignment.Fabricator;
            int queued = StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(fabricator, assignment.Recipe);
            bool working = ProductionOrderRuntimeAllocation.GetRunningCountForAssignment(record, assignment) > 0;
            float progress = working ? ProductionOrderRuntimeAllocation.GetProgressForAssignment(record, assignment) : 0f;
            string queuedText = queued == ComplexFabricator.QUEUE_INFINITE ? "∞" : Mathf.Max(0, queued).ToString();
            Color stateColor = GetBuildingStateColor(record, assignment);

            GameObject card = CreatePlainImage("TrackingDetailFabricatorNode", parent, new Color(0.78f, 0.79f, 0.73f, 1f));
            ApplyOniInputSlotStyle(card.GetComponent<Image>());
            ApplyAbsoluteRect(card, position, new Vector2(360f, 96f));

            GameObject accent = CreatePlainImage("Accent", card.transform, stateColor);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = new Vector2(5f, 0f);

            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 8, 8, 8);
            layout.spacing = 9f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddIcon(card.transform, GetFabricatorSprite(fabricator), 48f);
            GameObject text = new GameObject("Text");
            text.transform.SetParent(card.transform, false);
            text.AddComponent<RectTransform>();
            text.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(text, 2f, 0, 0, 0, 0);

            AddPlanLine(text.transform, fabricator.GetProperName(), 10, FontStyles.Bold, NeutralTextColor(), 17f);
            AddPlanLine(text.transform, string.Format("{0}  {1}", GetBuildingStateLabel(record, assignment), assignment.Recipe != null ? assignment.Recipe.GetUIName(false) : "?"), 8, FontStyles.Bold, stateColor, 15f);
            AddPlanLine(text.transform, StorageNetworkOrderTrackingRules.BuildBuildingQueueLine(assignment, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_DISPATCH), Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_AUTO), queuedText), 8, FontStyles.Normal, MutedTextColor(), 15f);
            AddPlanLine(text.transform, BuildBuildingDetailLine(record, assignment, progress), 8, FontStyles.Bold, stateColor, 15f);
        }

        private void AddTrackingDetailMaterialNodes(Transform parent, ProductionOrderRecord record, ProductionOrderQueueAssignment assignment, Vector2 position)
        {
            GameObject card = CreatePlainImage("TrackingDetailMaterialNode", parent, new Color(0.74f, 0.74f, 0.68f, 1f));
            ApplyResearchNodeRect(card, position, new Vector2(260f, 80f));
            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 7, 7);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddMaterialIcon(card.transform, assignment.OutputTag.IsValid ? assignment.OutputTag : record.ProductTag, 38f);
            GameObject text = new GameObject("Text");
            text.transform.SetParent(card.transform, false);
            text.AddComponent<RectTransform>();
            text.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(text, 1f, 0, 0, 0, 0);

            string outputName = !string.IsNullOrEmpty(assignment.OutputName) ? assignment.OutputName : ProductionOrderFormatting.GetTagDisplayName(assignment.OutputTag);
            AddPlanLine(text.transform, outputName, 9, FontStyles.Bold, assignment.Primary ? PositiveColor() : WarningColor(), 16f);
            AddPlanLine(text.transform, StorageNetworkOrderTrackingRules.BuildMaterialDetailLine(record, assignment), 8, FontStyles.Normal, NeutralTextColor(), 15f);
            AddPlanLine(text.transform, StorageNetworkOrderTrackingRules.BuildLeaseSummary(record, assignment), 8, FontStyles.Bold, MutedTextColor(), 28f);
        }

        private void AddTrackingDetailInfoNode(Transform parent, Vector2 position, string text)
        {
            GameObject card = CreatePlainImage("TrackingDetailInfoNode", parent, new Color(0.74f, 0.74f, 0.68f, 1f));
            ApplyAbsoluteRect(card, position, new Vector2(260f, 52f));
            TextMeshProUGUI label = CreateOrderText("Info", card.transform, text, 10, TextAlignmentOptions.MidlineLeft);
            label.color = MutedTextColor();
            label.fontStyle = FontStyles.Italic;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform(), 10f, 0f);
        }

        private static void ApplyAbsoluteRect(GameObject node, Vector2 position, Vector2 size)
        {
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }
    }
}
