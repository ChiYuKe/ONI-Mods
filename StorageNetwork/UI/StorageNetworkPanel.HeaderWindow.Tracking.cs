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
        private void RebuildOrderTracking(ProductDisplayGroup product)
        {
            if (orderTrackingContent == null)
            {
                return;
            }

            IEnumerable<ProductionOrderRecord> sourceRecords = orderTrackingFilterMode == TrackingFilterMode.Current && product != null
                ? productionOrderService.GetRecentOrdersForProduct(product.ProductTag, MaxDisplayedTrackingRecords)
                : productionOrderService.GetRecentOrders(MaxDisplayedTrackingRecords);
            List<ProductionOrderRecord> records = sourceRecords
                .Where(MatchesTrackingFilter)
                .ToList();
            string signature = BuildOrderTrackingSignature(product, records);
            if (signature == orderTrackingSignature)
            {
                return;
            }

            orderTrackingSignature = signature;
            EnsureOrderTrackingRows();
            orderTrackingRows.Begin();
            int activeCount = records.Count(IsTrackingActive);
            UpdateTrackingHeaderRow(GetTrackingScopeTitle(product), activeCount, records.Count);
            if (records.Count == 0)
            {
                UpdateTrackingInfoRow("empty", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_EMPTY), 58f);
                orderTrackingRows.Commit();
                ForceOrderLayout(orderTrackingContent);
                return;
            }

            foreach (ProductionOrderRecord record in records)
            {
                UpdateTrackingCard(orderTrackingContent, record);
            }

            orderTrackingRows.Commit();
            ForceOrderLayout(orderTrackingContent);
        }

        private string BuildOrderTrackingSignature(ProductDisplayGroup product, List<ProductionOrderRecord> records)
        {
            string recordsSignature = string.Join("|", records.Select(record => string.Format(
                "{0}:{1}:{2}:{3:0.###}:{4:0.###}:{5}:{6}:{7:0.###}:{8}",
                record.Key,
                record.DisplayId,
                record.State,
                record.ProducedAtSubmit,
                record.RequestedAmount,
                record.OrderCount,
                record.MergeCount,
                record.LastActivityCycle,
                record.AbnormalReason ?? string.Empty)));

            return string.Format("{0}|{1}|{2}|{3}", product?.ProductKey ?? string.Empty, orderTrackingSearchText ?? string.Empty, orderTrackingFilterMode, recordsSignature);
        }

        private bool MatchesTrackingFilter(ProductionOrderRecord record)
        {
            if (record == null)
            {
                return false;
            }

            switch (orderTrackingFilterMode)
            {
                case TrackingFilterMode.Current:
                case TrackingFilterMode.All:
                    break;
                case TrackingFilterMode.Abnormal:
                    if (record.State != ProductionOrderState.Abnormal)
                    {
                        return false;
                    }
                    break;
                case TrackingFilterMode.Completed:
                    if (record.State != ProductionOrderState.Completed)
                    {
                        return false;
                    }
                    break;
                case TrackingFilterMode.Running:
                    if (!IsTrackingActive(record))
                    {
                        return false;
                    }
                    break;
            }

            if (string.IsNullOrWhiteSpace(orderTrackingSearchText))
            {
                return true;
            }

            string needle = orderTrackingSearchText.Trim();
            return ContainsIgnoreCase(record.DisplayId.ToString(), needle) ||
                   ContainsIgnoreCase(record.ProductName, needle) ||
                   ContainsIgnoreCase(record.AbnormalReason, needle) ||
                   (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>()).Any(assignment =>
                       assignment != null &&
                       (ContainsIgnoreCase(assignment.Fabricator != null ? assignment.Fabricator.GetProperName() : null, needle) ||
                        ContainsIgnoreCase(assignment.OutputName, needle) ||
                        ContainsIgnoreCase(assignment.ConsumerName, needle) ||
                        ContainsIgnoreCase(assignment.Recipe != null ? assignment.Recipe.GetUIName(false) : null, needle)));
        }

        private static bool ContainsIgnoreCase(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack) &&
                   !string.IsNullOrEmpty(needle) &&
                   haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetTrackingScopeTitle(ProductDisplayGroup product)
        {
            if (orderTrackingFilterMode == TrackingFilterMode.Current && product != null)
            {
                return product.ProductName;
            }

            return "全部成品";
        }

        private void EnsureOrderTrackingRows()
        {
            if (orderTrackingRows == null || orderTrackingRowsContent != orderTrackingContent)
            {
                orderTrackingRows = new StorageNetworkKeyedRowCache(orderTrackingContent);
                orderTrackingRowsContent = orderTrackingContent;
            }
        }

        private void AddCompactOrderTrackingSection(ProductDisplayGroup product)
        {
            GameObject section = CreateSubPanel(orderDetailsContent, "CompactOrderTracking", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ACTIVE_TITLE), 0f, 0f, 1f);
            section.GetComponent<LayoutElement>().preferredHeight = product == null ? 96f : 230f;
            GameObject contentObject = new GameObject("CompactOrderTrackingContent");
            contentObject.transform.SetParent(section.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            contentObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            AddVerticalLayout(contentObject, 6f, 0, 0, 0, 0);

            orderTrackingContent = content;
            orderTrackingRows = null;
            orderTrackingRowsContent = null;
            orderTrackingSignature = null;
            RebuildOrderTracking(product);
        }

        private void AddExecutionTrackingSection(Transform parent, ProductDisplayGroup product)
        {
            GameObject section = CreateSubPanel(parent, "ExecutionOrderTracking", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TRACKING_TITLE), 0f, 0f, 1f);
            section.GetComponent<LayoutElement>().preferredHeight = product == null ? 110f : 230f;
            GameObject contentObject = new GameObject("ExecutionOrderTrackingContent");
            contentObject.transform.SetParent(section.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            contentObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            AddVerticalLayout(contentObject, 6f, 0, 0, 0, 0);

            RectTransform previousTrackingContent = orderTrackingContent;
            orderTrackingContent = content;
            orderTrackingRows = null;
            orderTrackingRowsContent = null;
            orderTrackingSignature = null;
            RebuildOrderTracking(product);
            orderTrackingContent = previousTrackingContent;
            orderTrackingRows = null;
            orderTrackingRowsContent = null;
            orderTrackingSignature = null;
        }

        private void AddTrackingCard(Transform parent, ProductionOrderRecord record)
        {
            bool abnormal = record.State == ProductionOrderState.Abnormal;
            bool active = IsTrackingActive(record);
            Color stateColor = GetOrderStateColor(record.State);
            GameObject card = CreateRoundedOrderImage("TrackingCard", parent, GetTrackingCardColor(record), "UISprite", "Background", "InputField");
            LayoutElement cardElement = card.AddComponent<LayoutElement>();
            cardElement.preferredWidth = TrackingContentWidth - 54f;
            cardElement.preferredHeight = 156f;
            cardElement.flexibleWidth = 0f;
            KButton cardButton = card.AddComponent<KButton>();
            cardButton.bgImage = card.GetComponent<KImage>();
            cardButton.additionalKImages = new KImage[0];
            cardButton.soundPlayer = new ButtonSoundPlayer();
            cardButton.onClick += () => ShowOrderTrackingDetail(record);

            HorizontalLayoutGroup cardLayout = card.AddComponent<HorizontalLayoutGroup>();
            cardLayout.padding = new RectOffset(10, 10, 10, 10);
            cardLayout.spacing = 8f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;

            GameObject main = new GameObject("TrackingMain");
            main.transform.SetParent(card.transform, false);
            main.AddComponent<RectTransform>();
            main.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(main, 6f, 0, 0, 0, 0);

            GameObject top = new GameObject("TrackingTop");
            top.transform.SetParent(main.transform, false);
            top.AddComponent<RectTransform>();
            top.AddComponent<LayoutElement>().preferredHeight = 64f;
            HorizontalLayoutGroup topLayout = top.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.childAlignment = TextAnchor.UpperLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;

            GameObject iconSlot = new GameObject("TrackingIconSlot");
            iconSlot.transform.SetParent(top.transform, false);
            iconSlot.AddComponent<RectTransform>();
            LayoutElement iconSlotLayout = iconSlot.AddComponent<LayoutElement>();
            iconSlotLayout.preferredWidth = 64f;
            iconSlotLayout.preferredHeight = 64f;
            AddIcon(iconSlot.transform, GetTagIcon(record.ProductTag), 50f);

            GameObject titleColumn = new GameObject("TrackingTitleColumn");
            titleColumn.transform.SetParent(top.transform, false);
            titleColumn.AddComponent<RectTransform>();
            titleColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(titleColumn, 4f, 0, 0, 0, 0);

            TextMeshProUGUI title = CreateOrderText("TrackingTitle", titleColumn.transform, string.Format("#{0} {1}", record.DisplayId, record.ProductName), 14, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.13f, 0.15f, 0.14f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            AddPlanLine(titleColumn.transform, GetTrackingSummaryLine(record), 9, FontStyles.Normal, MutedTextColor(), 18f);

            GameObject detailArea = CreatePlainImage("TrackingDetailArea", main.transform, new Color(0.78f, 0.78f, 0.71f, 0.42f));
            detailArea.AddComponent<LayoutElement>().preferredHeight = 54f;
            AddVerticalContainer(detailArea, 4f, 6, 6, 4, 4);
            AddTrackingProgressRow(detailArea.transform, record, stateColor);
            AddWrappedPlanLine(detailArea.transform, GetTrackingDetailLine(record), 10, abnormal ? FontStyles.Bold : FontStyles.Normal, abnormal ? DangerColor() : NeutralTextColor(), 17f, 2, 24);

            AddTrackingSeparator(card.transform, 1f);

            GameObject side = new GameObject("TrackingSide");
            side.transform.SetParent(card.transform, false);
            side.AddComponent<RectTransform>();
            side.AddComponent<LayoutElement>().preferredWidth = 96f;
            AddVerticalContainer(side, 7f, 0, 0, 0, 0);
            VerticalLayoutGroup sideLayout = side.GetComponent<VerticalLayoutGroup>();
            if (sideLayout != null)
            {
                sideLayout.childForceExpandWidth = false;
                sideLayout.childAlignment = TextAnchor.UpperRight;
            }

            AddTrackingStateBadge(side.transform, GetOrderStateLabel(record.State), stateColor, 52f, 94f);
            AddTrackingDottedLine(side.transform);
            AddPlanLine(side.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_VALUE), ProductionOrderFormatting.FormatCycle(record.CreatedCycle)), 10, FontStyles.Bold, NeutralTextColor(), 20f);
            AddPlanLine(side.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CREATED_CYCLE), 8, FontStyles.Normal, MutedTextColor(), 15f);

            if (record.MergeCount > 0)
            {
                AddPlanLine(main.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MERGED_ACTIVITY), record.MergeCount, ProductionOrderFormatting.FormatCycle(record.LastActivityCycle)), 8, FontStyles.Italic, MutedTextColor(), 15f);
                cardElement.preferredHeight += 18f;
            }

            if (active)
            {
                GameObject cancelButton = CreateTransparentIconButton("CancelOrderButton", card.transform, GetCancelActionSprite(), () => CancelTrackedOrder(record.Key));
                LayoutElement cancelLayout = cancelButton.AddComponent<LayoutElement>();
                cancelLayout.preferredWidth = 24f;
                cancelLayout.preferredHeight = 24f;
            }
        }

        private void UpdateTrackingHeaderRow(string productName, int activeCount, int recordCount)
        {
            GameObject row = orderTrackingRows.Use("header", () =>
            {
                TextMeshProUGUI created = CreateOrderText("TrackingHeader", orderTrackingContent, string.Empty, 11, TextAlignmentOptions.MidlineLeft);
                created.color = new Color(0.14f, 0.16f, 0.15f, 1f);
                created.fontStyle = FontStyles.Bold;
                created.richText = true;
                created.textWrappingMode = TextWrappingModes.Normal;
                created.overflowMode = TextOverflowModes.Ellipsis;
                created.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
                return created.gameObject;
            });

            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_SUMMARY), productName, activeCount, recordCount);
            }
        }

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
                .Where(assignment => assignment != null && assignment.Fabricator != null && assignment.Recipe != null)
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

            AddPlanLine(card.transform, GetOrderStateLabel(record.State), 10, FontStyles.Bold, GetOrderStateColor(record.State), 20f);
            AddPlanLine(card.transform, string.Format("{0} / {1}", GameUtil.GetFormattedMass(record.ProducedAtSubmit), GameUtil.GetFormattedMass(record.RequestedAmount)), 9, FontStyles.Bold, NeutralTextColor(), 18f);
            AddPlanLine(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_VALUE), ProductionOrderFormatting.FormatCycle(record.LastActivityCycle)), 8, FontStyles.Normal, MutedTextColor(), 16f);
        }

        private void AddTrackingDetailFabricatorNode(Transform parent, ProductionOrderRecord record, ProductionOrderQueueAssignment assignment, Vector2 position)
        {
            ComplexFabricator fabricator = assignment.Fabricator;
            int queued = fabricator.GetRecipeQueueCount(assignment.Recipe);
            bool working = fabricator.CurrentWorkingOrder == assignment.Recipe;
            float progress = working ? Mathf.Clamp01(fabricator.OrderProgress) : 0f;
            string queuedText = queued == ComplexFabricator.QUEUE_INFINITE ? "∞" : Mathf.Max(0, queued).ToString();
            Color stateColor = GetBuildingStateColor(assignment);

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
            AddPlanLine(text.transform, string.Format("{0}  {1}", GetBuildingStateLabel(assignment), assignment.Recipe != null ? assignment.Recipe.GetUIName(false) : "?"), 8, FontStyles.Bold, stateColor, 15f);
            AddPlanLine(text.transform, string.Format("{0} x{1}    队列 {2}", assignment.Primary ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_DISPATCH) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_AUTO), assignment.OrderCount, queuedText), 8, FontStyles.Normal, MutedTextColor(), 15f);
            AddPlanLine(text.transform, BuildBuildingDetailLine(assignment, progress), 8, FontStyles.Bold, stateColor, 15f);
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
            AddPlanLine(text.transform, assignment.Primary ? string.Format("目标 {0}", GameUtil.GetFormattedMass(record.RequestedAmount)) : string.Format("供给 {0}", string.IsNullOrEmpty(assignment.ConsumerName) ? record.ProductName : assignment.ConsumerName), 8, FontStyles.Normal, NeutralTextColor(), 15f);
            AddPlanLine(text.transform, BuildTrackingLeaseSummary(record, assignment), 8, FontStyles.Bold, MutedTextColor(), 28f);
        }

        private string BuildTrackingLeaseSummary(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (assignment.Primary)
            {
                float leased = (record.OutputLeases ?? new List<ProductionOrderOutputLease>())
                    .Where(lease => lease != null && lease.FabricatorInstanceId == assignment.Fabricator.GetInstanceID())
                    .Sum(lease => lease.Amount);
                return leased > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                    ? string.Format("产出预留 {0}", GameUtil.GetFormattedMass(leased))
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_DESC);
            }

            float materialLease = (record.MaterialLeases ?? new List<ProductionOrderMaterialLease>())
                .Where(lease => lease != null && lease.ConsumerName == assignment.ConsumerName)
                .Sum(lease => lease.Amount);
            return materialLease > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? string.Format("材料调拨 {0}", GameUtil.GetFormattedMass(materialLease))
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS);
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

        private void UpdateTrackingInfoRow(string key, string text, float height)
        {
            GameObject row = orderTrackingRows.Use("info:" + key, () =>
            {
                TextMeshProUGUI created = CreateOrderText("TrackingInfo", orderTrackingContent, string.Empty, 10, TextAlignmentOptions.MidlineLeft);
                created.color = MutedTextColor();
                created.fontStyle = FontStyles.Italic;
                created.richText = true;
                created.textWrappingMode = TextWrappingModes.Normal;
                created.overflowMode = TextOverflowModes.Ellipsis;
                created.gameObject.AddComponent<LayoutElement>();
                return created.gameObject;
            });

            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text;
            }

            LayoutElement layout = row.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = height;
            }
        }

        private void UpdateTrackingCard(Transform parent, ProductionOrderRecord record)
        {
            string key = "order:" + (record.Key ?? record.DisplayId.ToString());
            string signature = BuildTrackingCardSignature(record);
            bool recreate = orderTrackingRows.TryGetMetadata(key, out string oldSignature) && oldSignature != signature;
            orderTrackingRows.Use(key, () =>
            {
                AddTrackingCard(parent, record);
                return parent.GetChild(parent.childCount - 1).gameObject;
            }, recreate);
            orderTrackingRows.SetMetadata(key, signature);
        }

        private static string BuildTrackingCardSignature(ProductionOrderRecord record)
        {
            return string.Format(
                "{0}:{1}:{2:0.###}:{3:0.###}:{4}:{5}:{6:0.###}:{7}:{8}",
                record.DisplayId,
                record.State,
                record.ProducedAtSubmit,
                record.RequestedAmount,
                record.OrderCount,
                record.MergeCount,
                record.LastActivityCycle,
                record.AbnormalReason ?? string.Empty,
                string.Join(",", (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                    .Where(assignment => assignment != null)
                    .Select(assignment => string.Format("{0}:{1}",
                        assignment.Fabricator != null ? assignment.Fabricator.GetInstanceID() : 0,
                        assignment.Primary))));
        }

        private void AddTrackingProgressRow(Transform parent, ProductionOrderRecord record, Color color)
        {
            GameObject row = new GameObject("TrackingProgressRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 18f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddProgressBar(row.transform, Mathf.Clamp01(record.ProducedAtSubmit / Mathf.Max(record.RequestedAmount, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)), color, 82f);

            TextMeshProUGUI amount = CreateOrderText("TrackingAmount", row.transform, string.Format("{0} / {1}", GameUtil.GetFormattedMass(record.ProducedAtSubmit), GameUtil.GetFormattedMass(record.RequestedAmount)), 9, TextAlignmentOptions.MidlineLeft);
            amount.color = NeutralTextColor();
            amount.fontStyle = FontStyles.Bold;
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static string GetTrackingSummaryLine(ProductionOrderRecord record)
        {
            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ORDER_SOURCE_BATCH), GetOrderSourceLabel(record), record.OrderCount);
        }

        private static string GetTrackingDetailLine(ProductionOrderRecord record)
        {
            if (record.State == ProductionOrderState.Abnormal && !string.IsNullOrEmpty(record.AbnormalReason))
            {
                return record.AbnormalReason;
            }

            int primaryMachines = (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment != null && assignment.Primary)
                .Select(assignment => assignment.Fabricator)
                .Where(fabricator => fabricator != null)
                .Distinct()
                .Count();
            int materialMachines = (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment != null && !assignment.Primary)
                .Select(assignment => assignment.Fabricator)
                .Where(fabricator => fabricator != null)
                .Distinct()
                .Count();

            if (materialMachines > 0)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_WAITING_MATERIALS), Mathf.Max(1, primaryMachines), materialMachines);
            }

            if (primaryMachines > 0)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MACHINES_RUNNING), primaryMachines);
            }

            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_CREATED), GetOrderStateLabel(record.State), ProductionOrderFormatting.FormatCycle(record.CreatedCycle));
        }

        private void AddProgressBar(Transform parent, float progress, Color color, float width)
        {
            GameObject track = CreateRoundedOrderImage("ProgressTrack", parent, new Color(0.24f, 0.26f, 0.23f, 1f), "UISprite", "Background", "InputField");
            LayoutElement trackLayout = track.AddComponent<LayoutElement>();
            trackLayout.preferredWidth = width;
            trackLayout.preferredHeight = 10f;

            GameObject fillViewport = new GameObject("ProgressFillViewport", typeof(RectTransform));
            fillViewport.transform.SetParent(track.transform, false);
            RectTransform viewportRect = fillViewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            fillViewport.AddComponent<RectMask2D>();

            GameObject fill = CreateRoundedOrderImage("ProgressFill", fillViewport.transform, color, "UISprite", "Background", "InputField");
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        private void AddTrackingStateBadge(Transform parent, string text, Color color, float minWidth, float maxWidth)
        {
            GameObject badge = CreateRoundedOrderImage("TrackingStateBadge", parent, color, "UISprite", "Background");
            LayoutElement layout = badge.AddComponent<LayoutElement>();
            layout.preferredWidth = Mathf.Clamp(EstimateTextWidth(text, 10) + 18f, minWidth, maxWidth);
            layout.preferredHeight = 24f;
            TextMeshProUGUI label = CreateOrderText("TrackingStateText", badge.transform, text, 10, TextAlignmentOptions.Center);
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform(), 4f, 0f);
        }

        private static float EstimateTextWidth(string text, int size)
        {
            if (string.IsNullOrEmpty(text))
            {
                return size;
            }

            float width = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                width += text[i] <= 0x7f ? size * 0.55f : size;
            }

            return width;
        }

        private static void AddTrackingSeparator(Transform parent, float width)
        {
            GameObject holder = new GameObject("TrackingSeparatorHolder");
            holder.transform.SetParent(parent, false);
            holder.AddComponent<RectTransform>();
            LayoutElement holderLayout = holder.AddComponent<LayoutElement>();
            holderLayout.preferredWidth = width;
            holderLayout.preferredHeight = 112f;

            GameObject separator = CreatePlainImage("TrackingSeparator", holder.transform, new Color(0.50f, 0.50f, 0.46f, 0.75f));
            RectTransform rect = separator.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 72f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void AddTrackingDottedLine(Transform parent)
        {
            TextMeshProUGUI line = CreateOrderText("TrackingDottedLine", parent, "----------", 8, TextAlignmentOptions.Center);
            line.color = new Color(0.44f, 0.44f, 0.40f, 0.85f);
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 12f;
        }

        private static GameObject CreateRoundedOrderImage(string name, Transform parent, Color color, params string[] spriteNames)
        {
            GameObject gameObject = CreatePlainImage(name, parent, color);
            Image image = gameObject.GetComponent<Image>();
            foreach (string spriteName in spriteNames)
            {
                Sprite sprite = GetSpriteByName(spriteName);
                if (sprite == null)
                {
                    continue;
                }

                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.pixelsPerUnitMultiplier = 1f;
                break;
            }

            return gameObject;
        }

        private float AddTrackingLine(Transform parent, string text, int size, FontStyles style, Color color, float minHeight, int maxLines)
        {
            TextMeshProUGUI line = CreateOrderText("TrackingLine", parent, text, size, TextAlignmentOptions.TopLeft);
            line.color = color;
            line.fontStyle = style;
            line.richText = true;
            line.textWrappingMode = TextWrappingModes.Normal;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.maxVisibleLines = maxLines;

            float lineHeight = Mathf.Max(minHeight, size + 7f);
            float height = lineHeight * EstimateTextLineCount(text, maxLines, compactOrderWindow ? 18 : 24);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return height;
        }

        private static Color GetTrackingCardColor(ProductionOrderRecord record)
        {
            if (record.State == ProductionOrderState.Completed)
            {
                return new Color(0.82f, 0.84f, 0.76f, 1f);
            }

            if (record.State == ProductionOrderState.Abnormal)
            {
                return new Color(0.84f, 0.75f, 0.70f, 1f);
            }

            if (record.State == ProductionOrderState.WaitingMaterials)
            {
                return new Color(0.84f, 0.82f, 0.72f, 1f);
            }

            if (record.State == ProductionOrderState.Cancelled)
            {
                return new Color(0.76f, 0.76f, 0.70f, 1f);
            }

            return new Color(0.84f, 0.83f, 0.76f, 1f);
        }

        private static Sprite GetCancelActionSprite()
        {
            return GetSpriteByName("action_cancel") ??
                   GetSpriteByName("icon_action_cancel") ??
                   GetSpriteByName("action_cancel.png");
        }

        private static string GetOrderSourceLabel(ProductionOrderRecord record)
        {
            return record.IsAutomatic ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_SOURCE_KEEP) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_SOURCE_MANUAL);
        }

        private void CancelTrackedOrder(string orderKey)
        {
            lastOrderStatus = productionOrderService.CancelOrder(orderKey, GetCurrentCycleTime());
            productionOrderService.Refresh();
            RebuildOrderDetails();
        }

        private static bool IsTrackingActive(ProductionOrderRecord order)
        {
            return order.State != ProductionOrderState.Completed &&
                   order.State != ProductionOrderState.Abnormal &&
                   order.State != ProductionOrderState.Cancelled;
        }

        private static string GetOrderStateLabel(ProductionOrderState state)
        {
            switch (state)
            {
                case ProductionOrderState.Submitted:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_SUBMITTED);
                case ProductionOrderState.WaitingMaterials:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_WAITING);
                case ProductionOrderState.Producing:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_PRODUCING);
                case ProductionOrderState.Completed:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_COMPLETED);
                case ProductionOrderState.Abnormal:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_ABNORMAL);
                case ProductionOrderState.Cancelled:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_CANCELLED);
                default:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_TRACKING);
            }
        }

        private static Color GetOrderStateColor(ProductionOrderState state)
        {
            switch (state)
            {
                case ProductionOrderState.WaitingMaterials:
                    return WarningColor();
                case ProductionOrderState.Producing:
                    return NeutralBlue();
                case ProductionOrderState.Completed:
                    return PositiveColor();
                case ProductionOrderState.Abnormal:
                    return DangerColor();
                case ProductionOrderState.Cancelled:
                    return new Color(0.42f, 0.42f, 0.42f, 1f);
                default:
                    return NeutralTextColor();
            }
        }

        // 统一建筑运行状态颜色，订单树和详情树都走这里，避免同一状态在不同卡片上颜色不一致。
        private static Color GetBuildingStateColor(ProductionOrderQueueAssignment assignment)
        {
            switch (GetBuildingStateKind(assignment))
            {
                case BuildingStateKind.Running:
                    return PositiveColor();
                case BuildingStateKind.WaitingMaterials:
                    return WarningColor();
                case BuildingStateKind.Disabled:
                    return new Color(0.46f, 0.46f, 0.42f, 1f);
                case BuildingStateKind.NoRecipe:
                    return new Color(0.40f, 0.44f, 0.48f, 1f);
                case BuildingStateKind.Abnormal:
                    return DangerColor();
                default:
                    return NeutralBlue();
            }
        }

        private static string GetBuildingStateLabel(ProductionOrderQueueAssignment assignment)
        {
            switch (GetBuildingStateKind(assignment))
            {
                case BuildingStateKind.Running:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_RUNNING);
                case BuildingStateKind.WaitingMaterials:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_WAITING_MATERIALS);
                case BuildingStateKind.NoPower:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_NO_POWER);
                case BuildingStateKind.Disabled:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_DISABLED);
                case BuildingStateKind.NoRecipe:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_NO_RECIPE);
                case BuildingStateKind.Abnormal:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_ABNORMAL);
                default:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_QUEUED);
            }
        }

        private static string BuildBuildingDetailLine(ProductionOrderQueueAssignment assignment, float progress)
        {
            ComplexFabricator fabricator = assignment?.Fabricator;
            if (fabricator == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_MISSING);
            }

            if (fabricator.CurrentWorkingOrder == assignment.Recipe)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_PROGRESS), Mathf.Clamp01(progress));
            }

            string state = GetProductionStateText(fabricator);
            return string.IsNullOrEmpty(state) ? GetBuildingStateLabel(assignment) : state;
        }

        private static BuildingStateKind GetBuildingStateKind(ProductionOrderQueueAssignment assignment)
        {
            ComplexFabricator fabricator = assignment?.Fabricator;
            if (fabricator == null)
            {
                return BuildingStateKind.Abnormal;
            }

            if (assignment.Recipe == null)
            {
                return BuildingStateKind.NoRecipe;
            }

            Operational operational = fabricator.GetComponent<Operational>();
            if (operational != null && !operational.IsOperational)
            {
                return BuildingStateKind.Disabled;
            }

            if (fabricator.CurrentWorkingOrder == assignment.Recipe)
            {
                return BuildingStateKind.Running;
            }

            int queued = fabricator.GetRecipeQueueCount(assignment.Recipe);
            if (queued != 0)
            {
                return BuildingStateKind.WaitingMaterials;
            }

            return BuildingStateKind.NoRecipe;
        }

        private enum BuildingStateKind
        {
            Queued,
            Running,
            WaitingMaterials,
            NoPower,
            Disabled,
            NoRecipe,
            Abnormal
        }

    }
}
