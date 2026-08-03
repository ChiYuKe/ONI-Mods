using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private const int TrackingVirtualizationThreshold = 16;
        private const int TrackingVirtualizationOverscan = 2;
        private const float TrackingRowSpacing = 6f;
        private const float TrackingVerticalPadding = 12f;

        private void RebuildOrderTracking(ProductDisplayGroup product)
        {
            if (orderTrackingContent == null ||
                !orderTrackingContent.gameObject.activeInHierarchy)
            {
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Tracking);
            int orderVersion = ProductionOrderService.OrderVersion;
            int capabilityVersion = StorageSceneRegistry.CapabilityVersion;
            if (orderTrackingSignature != null &&
                orderTrackingObservedOrderVersion == orderVersion &&
                orderTrackingObservedCapabilityVersion == capabilityVersion)
            {
                return;
            }

            IReadOnlyList<ProductionOrderRecord> sourceRecords = orderTrackingFilterMode == TrackingFilterMode.Current && product != null
                ? productionOrderService.GetRecentOrdersForProduct(product.ProductTag, MaxDisplayedTrackingRecords)
                : productionOrderService.GetRecentOrders(MaxDisplayedTrackingRecords);
            List<ProductionOrderRecord> records = orderTrackingRecordBuffer;
            records.Clear();
            foreach (ProductionOrderRecord record in sourceRecords)
            {
                if (StorageNetworkOrderTrackingRules.MatchesFilter(
                        record,
                        orderTrackingFilterMode,
                        orderTrackingSearchText))
                {
                    records.Add(record);
                }
            }

            bool structureChanged = orderTrackingSignature == null ||
                                    HasOrderTrackingStructureChanged(records);
            orderTrackingObservedOrderVersion = orderVersion;
            orderTrackingObservedCapabilityVersion = capabilityVersion;
            orderTrackingSignature = "ready";
            if (!structureChanged)
            {
                foreach (ProductionOrderRecord record in records)
                {
                    UpdateTrackingCardLive(record);
                }

                return;
            }

            CaptureOrderTrackingStructure(records);
            ReconcileOrderTrackingRows(records, product, requestLayout: true);
        }

        private void ReconcileOrderTrackingRows(
            List<ProductionOrderRecord> records,
            ProductDisplayGroup product,
            bool requestLayout)
        {
            if (orderTrackingContent == null ||
                !orderTrackingContent.gameObject.activeInHierarchy)
            {
                return;
            }

            EnsureOrderTrackingRows();
            orderTrackingRows.Begin();
            orderTrackingLiveViews.Clear();
            int activeCount = 0;
            foreach (ProductionOrderRecord record in records)
            {
                if (StorageNetworkOrderTrackingRules.IsActive(record))
                {
                    activeCount++;
                }
            }

            UpdateTrackingHeaderRow(GetTrackingScopeTitle(product), activeCount, records.Count);
            UpdateTrackingBulkActionsRow(records);
            if (records.Count == 0)
            {
                UpdateTrackingInfoRow("empty", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_EMPTY), 58f);
                orderTrackingRows.Commit();
                if (requestLayout)
                {
                    ForceOrderLayout(orderTrackingContent);
                }
                return;
            }

            GetTrackingVisibleRange(
                records,
                HasTrackingBulkActions(records),
                out int firstVisible,
                out int lastVisibleExclusive);
            if (firstVisible > 0)
            {
                UseTrackingSpacer(
                    "\0virtual-top",
                    GetTrackingHiddenHeight(records, 0, firstVisible));
            }

            for (int index = firstVisible; index < lastVisibleExclusive; index++)
            {
                UpdateTrackingCard(orderTrackingContent, records[index]);
            }

            int hiddenAfter = records.Count - lastVisibleExclusive;
            if (hiddenAfter > 0)
            {
                UseTrackingSpacer(
                    "\0virtual-bottom",
                    GetTrackingHiddenHeight(
                        records,
                        lastVisibleExclusive,
                        hiddenAfter));
            }

            orderTrackingRows.Commit();
            if (requestLayout)
            {
                ForceOrderLayout(orderTrackingContent);
            }
        }

        private bool HasOrderTrackingStructureChanged(
            List<ProductionOrderRecord> records)
        {
            if (orderTrackingStructures.Count != records.Count)
            {
                return true;
            }

            for (int index = 0; index < records.Count; index++)
            {
                if (!orderTrackingStructures[index].Matches(records[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private void GetTrackingVisibleRange(
            List<ProductionOrderRecord> records,
            bool hasBulkActions,
            out int firstVisible,
            out int lastVisibleExclusive)
        {
            firstVisible = 0;
            lastVisibleExclusive = records.Count;
            if (records.Count <= TrackingVirtualizationThreshold ||
                orderTrackingContent == null ||
                orderTrackingScrollRect?.viewport == null ||
                !ReferenceEquals(orderTrackingScrollRect.content, orderTrackingContent))
            {
                return;
            }

            float viewportHeight = orderTrackingScrollRect.viewport.rect.height;
            if (viewportHeight <= 1f)
            {
                viewportHeight = 600f;
            }

            float leadingHeight = 30f + TrackingRowSpacing;
            if (hasBulkActions)
            {
                leadingHeight += 24f + TrackingRowSpacing;
            }

            float startOffset = Mathf.Max(
                0f,
                orderTrackingContent.anchoredPosition.y -
                TrackingVerticalPadding * 0.5f -
                leadingHeight);
            float endOffset = startOffset + viewportHeight;
            float cursor = 0f;
            int first = 0;
            while (first < records.Count &&
                   cursor + GetTrackingCardHeight(records[first]) < startOffset)
            {
                cursor += GetTrackingCardHeight(records[first]) +
                          TrackingRowSpacing;
                first++;
            }

            int last = first;
            float visibleCursor = cursor;
            while (last < records.Count && visibleCursor < endOffset)
            {
                visibleCursor += GetTrackingCardHeight(records[last]) +
                                 TrackingRowSpacing;
                last++;
            }

            firstVisible = Mathf.Max(0, first - TrackingVirtualizationOverscan);
            lastVisibleExclusive = Mathf.Min(
                records.Count,
                last + TrackingVirtualizationOverscan);
        }

        private static float GetTrackingHiddenHeight(
            List<ProductionOrderRecord> records,
            int start,
            int count)
        {
            float height = 0f;
            int end = Mathf.Min(records.Count, start + count);
            for (int index = start; index < end; index++)
            {
                height += GetTrackingCardHeight(records[index]);
            }

            if (count > 1)
            {
                height += (count - 1) * TrackingRowSpacing;
            }

            return height;
        }

        private static float GetTrackingCardHeight(ProductionOrderRecord record)
        {
            return record != null && record.MergeCount > 0 ? 174f : 156f;
        }

        private void UseTrackingSpacer(string key, float height)
        {
            GameObject spacer = orderTrackingRows.Use(key, () =>
            {
                GameObject created = new GameObject("VirtualSpacer");
                created.transform.SetParent(orderTrackingContent, false);
                created.AddComponent<RectTransform>();
                created.AddComponent<LayoutElement>();
                return created;
            });
            LayoutElement layout = spacer.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = Mathf.Max(0f, height);
            }
        }

        private void OnOrderTrackingScroll(Vector2 _)
        {
            orderTrackingViewportDirty = true;
        }

        private void CaptureOrderTrackingStructure(
            List<ProductionOrderRecord> records)
        {
            orderTrackingStructures.Clear();
            foreach (ProductionOrderRecord record in records)
            {
                orderTrackingStructures.Add(new TrackingCardStructure(record));
            }
        }

        private string GetTrackingScopeTitle(ProductDisplayGroup product)
        {
            if (orderTrackingFilterMode == TrackingFilterMode.Current && product != null)
            {
                return product.ProductName;
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ALL_PRODUCTS);
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

        private GameObject AddTrackingCard(Transform parent, ProductionOrderRecord record)
        {
            bool abnormal = record.State == ProductionOrderState.Abnormal;
            bool active = StorageNetworkOrderTrackingRules.IsActive(record);
            Color stateColor = GetOrderStateColor(record.State);
            GameObject card = CreateRoundedOrderImage("TrackingCard", parent, GetTrackingCardColor(record), "UISprite", "Background", "InputField");
            LayoutElement cardElement = card.AddComponent<LayoutElement>();
            cardElement.preferredWidth = TrackingContentWidth - 54f;
            cardElement.preferredHeight = 156f;
            cardElement.flexibleWidth = 0f;
            cardElement.flexibleHeight = 0f;
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

            TextMeshProUGUI summary = AddPlanLine(titleColumn.transform, StorageNetworkOrderTrackingRules.GetSummaryLine(record), 9, FontStyles.Normal, MutedTextColor(), 18f);
            summary.gameObject.name = "TrackingSummary";

            GameObject detailArea = CreatePlainImage("TrackingDetailArea", main.transform, new Color(0.78f, 0.78f, 0.71f, 0.42f));
            detailArea.AddComponent<LayoutElement>().preferredHeight = 54f;
            AddVerticalContainer(detailArea, 4f, 6, 6, 4, 4);
            AddTrackingProgressRow(detailArea.transform, record, stateColor);
            TextMeshProUGUI detail = AddWrappedPlanLine(detailArea.transform, StorageNetworkOrderTrackingRules.GetDetailLine(record), 10, abnormal ? FontStyles.Bold : FontStyles.Normal, abnormal ? DangerColor() : NeutralTextColor(), 17f, 2, 24);
            detail.gameObject.name = "TrackingDetail";

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

            AddTrackingStateBadge(side.transform, StorageNetworkOrderTrackingRules.GetOrderStateLabel(record.State), stateColor, 52f, 94f);
            AddTrackingDottedLine(side.transform);
            AddTrackingCyclePair(
                side.transform,
                ProductionOrderFormatting.FormatCycleStamp(record.CreatedCycle),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CREATED_CYCLE));
            TextMeshProUGUI estimatedFinish = AddTrackingCyclePair(
                side.transform,
                GetTrackingEstimatedFinishCycle(record),
                record.State == ProductionOrderState.Completed
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_FINISHED_CYCLE)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ESTIMATED_FINISH_CYCLE));
            estimatedFinish.gameObject.name = "TrackingEstimatedFinish";

            if (record.MergeCount > 0)
            {
                TextMeshProUGUI merged = AddPlanLine(main.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MERGED_ACTIVITY), record.MergeCount, ProductionOrderFormatting.FormatCycleStamp(record.LastActivityCycle)), 8, FontStyles.Italic, MutedTextColor(), 15f);
                merged.gameObject.name = "TrackingMergedActivity";
                cardElement.preferredHeight += 18f;
            }

            if (active)
            {
                GameObject cancelButton = CreateTransparentIconButton("CancelOrderButton", card.transform, GetCancelActionSprite(), () => CancelTrackedOrder(record.Key));
                LayoutElement cancelLayout = cancelButton.AddComponent<LayoutElement>();
                cancelLayout.preferredWidth = 24f;
                cancelLayout.preferredHeight = 24f;
            }
            else if (record.State == ProductionOrderState.Abnormal)
            {
                AddTrackingRetryButton(card.transform, () => RetryTrackedOrder(record.Key));
            }

            return card;
        }

        private TextMeshProUGUI AddTrackingCyclePair(Transform parent, string value, string label)
        {
            TextMeshProUGUI valueText = AddPlanLine(parent, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_VALUE), value), 9, FontStyles.Bold, NeutralTextColor(), 17f);
            AddPlanLine(parent, label, 7, FontStyles.Normal, MutedTextColor(), 12f);
            return valueText;
        }

        private static string GetTrackingEstimatedFinishCycle(ProductionOrderRecord record)
        {
            if (record == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_UNKNOWN);
            }

            if (record.State == ProductionOrderState.Completed && record.CompletedCycle > 0f)
            {
                return ProductionOrderFormatting.FormatCycleStamp(record.CompletedCycle);
            }

            if (!StorageNetworkOrderTrackingRules.IsActive(record))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_UNKNOWN);
            }

            if (TryEstimateTotalOrderSeconds(record, out float totalSeconds))
            {
                return ProductionOrderFormatting.FormatCycleStamp(record.CreatedCycle + totalSeconds / 600f);
            }

            if (!TryEstimateRemainingSeconds(record, out float remainingSeconds))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_UNKNOWN);
            }

            float currentCycle = StorageNetworkCycleTime.GetCurrent();
            return ProductionOrderFormatting.FormatCycleStamp(currentCycle + remainingSeconds / 600f);
        }

        private static bool TryEstimateRemainingSeconds(ProductionOrderRecord record, out float remainingSeconds)
        {
            remainingSeconds = 0f;
            if (record?.QueueAssignments == null || record.QueueAssignments.Count == 0)
            {
                return false;
            }

            bool hasEstimate = false;
            foreach (IGrouping<ComplexFabricator, ProductionOrderQueueAssignment> group in record.QueueAssignments
                         .Where(assignment => assignment?.Fabricator != null && assignment.Recipe != null)
                         .GroupBy(assignment => assignment.Fabricator))
            {
                float fabricatorSeconds = 0f;
                foreach (ProductionOrderQueueAssignment assignment in group)
                {
                    int queued = StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(assignment.Fabricator, assignment.Recipe);
                    if (queued == ComplexFabricator.QUEUE_INFINITE)
                    {
                        return false;
                    }

                    int pending = assignment.Primary
                        ? GetRemainingPrimaryBatchCount(record, assignment)
                        : Mathf.Min(Mathf.Max(0, queued), Mathf.Max(0, assignment.OrderCount));
                    float recipeTime = Mathf.Max(0f, assignment.Recipe.time);
                    int workingCount = ProductionOrderRuntimeAllocation.GetRunningCountForAssignment(record, assignment);
                    float runningProgress = workingCount > 0
                        ? ProductionOrderRuntimeAllocation.GetProgressForAssignment(record, assignment)
                        : 0f;
                    int pendingNotRunning = Mathf.Max(0, pending - workingCount);
                    fabricatorSeconds += (pendingNotRunning + workingCount * Mathf.Max(0f, 1f - runningProgress)) * recipeTime;

                    StorageNetwork.Components.StorageNetworkOrderProductionCenterFabricator orderCenter = assignment.Fabricator as StorageNetwork.Components.StorageNetworkOrderProductionCenterFabricator;
                    if (orderCenter != null)
                    {
                        fabricatorSeconds /= Mathf.Max(1, orderCenter.ActiveCoreCount);
                    }

                    hasEstimate = true;
                }

                remainingSeconds = Mathf.Max(remainingSeconds, fabricatorSeconds);
            }

            return hasEstimate;
        }

        private static bool TryEstimateTotalOrderSeconds(ProductionOrderRecord record, out float totalSeconds)
        {
            totalSeconds = 0f;
            if (record?.QueueAssignments == null)
            {
                return false;
            }

            Dictionary<string, int> busiestAssignmentCounts = new Dictionary<string, int>();
            Dictionary<string, ComplexRecipe> recipesByKey = new Dictionary<string, ComplexRecipe>();
            foreach (ProductionOrderQueueAssignment assignment in record.QueueAssignments)
            {
                if (assignment == null || assignment.Fabricator == null || assignment.Recipe == null)
                {
                    continue;
                }

                int queued = StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(assignment.Fabricator, assignment.Recipe);
                if (queued == ComplexFabricator.QUEUE_INFINITE)
                {
                    return false;
                }

                string recipeKey = string.Format("{0}|{1}|{2}", ProductionRecipeCatalog.GetRecipeKey(assignment.Recipe), assignment.OutputTag.Name, assignment.Primary);
                recipesByKey[recipeKey] = assignment.Recipe;
                busiestAssignmentCounts[recipeKey] = busiestAssignmentCounts.TryGetValue(recipeKey, out int existing)
                    ? Mathf.Max(existing, Mathf.Max(0, assignment.OrderCount))
                    : Mathf.Max(0, assignment.OrderCount);
            }

            foreach (KeyValuePair<string, int> pair in busiestAssignmentCounts)
            {
                if (recipesByKey.TryGetValue(pair.Key, out ComplexRecipe recipe))
                {
                    totalSeconds += Mathf.Max(0f, recipe.time) * pair.Value;
                }
            }

            return totalSeconds > 0f;
        }

        private static int GetRemainingPrimaryBatchCount(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            float outputAmount = GetRecipeOutputAmount(assignment.Recipe, record.ProductTag);
            if (outputAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return Mathf.Max(0, assignment.OrderCount);
            }

            int totalAssigned = record.QueueAssignments
                .Where(candidate => candidate != null &&
                                    candidate.Primary &&
                                    candidate.Recipe == assignment.Recipe &&
                                    GetRecipeOutputAmount(candidate.Recipe, record.ProductTag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .Sum(candidate => Mathf.Max(0, candidate.OrderCount));
            if (totalAssigned <= 0)
            {
                return Mathf.Max(0, assignment.OrderCount);
            }

            float remainingAmount = Mathf.Max(0f, record.RequestedAmount - record.ProducedAtSubmit);
            int totalRemaining = Mathf.CeilToInt(remainingAmount / outputAmount);
            int remainingForAssignment = Mathf.CeilToInt(totalRemaining * assignment.OrderCount / (float)totalAssigned);
            return Mathf.Clamp(remainingForAssignment, 0, Mathf.Max(0, assignment.OrderCount));
        }

        private static float GetRecipeOutputAmount(ComplexRecipe recipe, Tag productTag)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(recipe, productTag);
            return result != null ? Mathf.Max(0f, result.amount) : 0f;
        }

        private void UpdateTrackingBulkActionsRow(List<ProductionOrderRecord> records)
        {
            GetTrackingBulkActionFlags(
                records,
                out bool hasAbnormal,
                out bool hasCompleted);
            if (!hasAbnormal && !hasCompleted)
            {
                return;
            }

            GameObject row = orderTrackingRows.Use("bulk-actions", () =>
            {
                GameObject created = new GameObject("TrackingBulkActions");
                created.transform.SetParent(orderTrackingContent, false);
                created.AddComponent<RectTransform>();
                created.AddComponent<LayoutElement>().preferredHeight = 24f;
                HorizontalLayoutGroup layout = created.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 5f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                return created;
            });

            string key = "bulk-actions";
            if (!orderTrackingRows.TryGetMetadata(
                    key,
                    out TrackingBulkActionsView view))
            {
                view = new TrackingBulkActionsView();
                orderTrackingRows.SetMetadata(key, view);
            }

            int fingerprint = (hasAbnormal ? 1 : 0) |
                              (hasCompleted ? 2 : 0);
            if (view.Fingerprint == fingerprint)
            {
                return;
            }

            view.Fingerprint = fingerprint;
            RectTransform rowRect = row.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                ClearChildren(rowRect);
            }
            if (hasAbnormal)
            {
                AddTrackingBulkButton(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ACTION_CLEAR_ABNORMAL), () => ClearTrackedOrders(ProductionOrderState.Abnormal));
            }

            if (hasCompleted)
            {
                AddTrackingBulkButton(row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ACTION_CLEAR_COMPLETED), () => ClearTrackedOrders(ProductionOrderState.Completed));
            }

            row.SetActive(true);
        }

        private static bool HasTrackingBulkActions(
            List<ProductionOrderRecord> records)
        {
            GetTrackingBulkActionFlags(records, out bool abnormal, out bool completed);
            return abnormal || completed;
        }

        private static void GetTrackingBulkActionFlags(
            List<ProductionOrderRecord> records,
            out bool abnormal,
            out bool completed)
        {
            abnormal = false;
            completed = false;
            if (records == null)
            {
                return;
            }

            for (int index = 0; index < records.Count; index++)
            {
                ProductionOrderRecord record = records[index];
                abnormal |= record?.State == ProductionOrderState.Abnormal;
                completed |= record?.State == ProductionOrderState.Completed;
                if (abnormal && completed)
                {
                    return;
                }
            }
        }

        private void AddTrackingBulkButton(Transform parent, string label, System.Action onClick)
        {
            GameObject button = CreateStyledButton("TrackingBulkButton", parent, label, onClick, KleiBlueStyle());
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = 62f;
            layout.preferredHeight = 22f;
        }

        private void AddTrackingRetryButton(Transform parent, System.Action onClick)
        {
            GameObject button = CreateStyledButton(
                "RetryOrderButton",
                parent,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ACTION_RETRY),
                onClick,
                KleiBlueStyle());
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = 42f;
            layout.preferredHeight = 24f;
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
            string orderKey = GetTrackingOrderKey(record);
            string key = "order:" + orderKey;
            bool hasView = orderTrackingRows.TryGetMetadata(key, out TrackingCardLiveView view);
            bool recreate = hasView &&
                            (view.StructuralState != record.State ||
                             view.HasMergedActivity != (record.MergeCount > 0));
            GameObject card = orderTrackingRows.Use(key, () =>
            {
                return AddTrackingCard(parent, record);
            }, recreate);
            if (!hasView || recreate || view?.Root == null)
            {
                view = TrackingCardLiveView.Create(card);
                orderTrackingRows.SetMetadata(key, view);
            }

            view.StructuralState = record.State;
            view.HasMergedActivity = record.MergeCount > 0;
            orderTrackingLiveViews[orderKey] = view;
            UpdateTrackingCardLive(view, record);
        }

        private void UpdateTrackingCardLive(ProductionOrderRecord record)
        {
            if (record != null &&
                orderTrackingLiveViews.TryGetValue(
                    GetTrackingOrderKey(record),
                    out TrackingCardLiveView view))
            {
                UpdateTrackingCardLive(view, record);
            }
        }

        private static string GetTrackingOrderKey(ProductionOrderRecord record)
        {
            return !string.IsNullOrEmpty(record?.Key)
                ? record.Key
                : (record?.DisplayId ?? 0).ToString();
        }

        private static void UpdateTrackingCardLive(TrackingCardLiveView view, ProductionOrderRecord record)
        {
            if (view == null || view.Root == null || record == null)
            {
                return;
            }

            view.CurrentRecord = record;
            int liveFingerprint = GetTrackingCardLiveFingerprint(record);
            if (view.LastLiveFingerprint == liveFingerprint)
            {
                return;
            }

            view.LastLiveFingerprint = liveFingerprint;
            if (view.ProgressViewport != null)
            {
                Vector2 anchorMax = view.ProgressViewport.anchorMax;
                anchorMax.x = Mathf.Clamp01(record.ProducedAtSubmit /
                    Mathf.Max(record.RequestedAmount, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT));
                if (view.ProgressViewport.anchorMax != anchorMax)
                {
                    view.ProgressViewport.anchorMax = anchorMax;
                }
            }

            SetTextIfChanged(
                view.Amount,
                string.Format(
                    "{0} / {1}",
                    GameUtil.GetFormattedMass(record.ProducedAtSubmit),
                    GameUtil.GetFormattedMass(record.RequestedAmount)));
            SetTextIfChanged(view.Summary, StorageNetworkOrderTrackingRules.GetSummaryLine(record));
            SetTextIfChanged(view.Detail, StorageNetworkOrderTrackingRules.GetDetailLine(record));
            SetTextIfChanged(
                view.EstimatedFinish,
                string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_VALUE),
                    GetTrackingEstimatedFinishCycle(record)));
            if (view.MergedActivity != null)
            {
                SetTextIfChanged(
                    view.MergedActivity,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MERGED_ACTIVITY),
                        record.MergeCount,
                        ProductionOrderFormatting.FormatCycleStamp(record.LastActivityCycle)));
            }
        }

        private static int GetTrackingCardLiveFingerprint(
            ProductionOrderRecord record)
        {
            unchecked
            {
                int fingerprint = record.DisplayId;
                fingerprint = (fingerprint * 397) ^ record.State.GetHashCode();
                fingerprint = (fingerprint * 397) ^ record.RequestedAmount.GetHashCode();
                fingerprint = (fingerprint * 397) ^ record.ProducedAtSubmit.GetHashCode();
                fingerprint = (fingerprint * 397) ^ record.OrderCount;
                fingerprint = (fingerprint * 397) ^ record.MergeCount;
                fingerprint = (fingerprint * 397) ^ record.LastActivityCycle.GetHashCode();
                fingerprint = (fingerprint * 397) ^ record.CompletedCycle.GetHashCode();
                fingerprint = (fingerprint * 397) ^
                              (record.AbnormalReason != null
                                  ? System.StringComparer.Ordinal.GetHashCode(record.AbnormalReason)
                                  : 0);
                if (record.QueueAssignments == null)
                {
                    return fingerprint;
                }

                fingerprint = (fingerprint * 397) ^ record.QueueAssignments.Count;
                foreach (ProductionOrderQueueAssignment assignment in record.QueueAssignments)
                {
                    if (assignment == null)
                    {
                        fingerprint *= 397;
                        continue;
                    }

                    fingerprint = (fingerprint * 397) ^ assignment.OrderCount;
                    fingerprint = (fingerprint * 397) ^ (assignment.Primary ? 1 : 0);
                    fingerprint = (fingerprint * 397) ^
                                  (assignment.Fabricator != null
                                      ? assignment.Fabricator.GetInstanceID()
                                      : 0);
                    fingerprint = (fingerprint * 397) ^
                                  ProductionOrderRuntimeAllocation
                                      .GetRunningCountForAssignment(record, assignment);
                    fingerprint = (fingerprint * 397) ^
                                  ProductionOrderRuntimeAllocation
                                      .GetProgressForAssignment(record, assignment)
                                      .GetHashCode();
                }

                return fingerprint;
            }
        }

        private void CancelTrackedOrder(string orderKey)
        {
            lastOrderStatus = productionOrderService.CancelOrder(orderKey, StorageNetworkCycleTime.GetCurrent());
            productionOrderService.Refresh();
            RebuildOrderDetails();
        }

        private void RetryTrackedOrder(string orderKey)
        {
            lastOrderStatus = productionOrderService.RetryOrder(orderKey, StorageNetworkCycleTime.GetCurrent());
            productionOrderService.Refresh();
            RebuildOrderDetails();
        }

        private void ClearTrackedOrders(ProductionOrderState state)
        {
            lastOrderStatus = productionOrderService.ClearOrdersByState(state);
            productionOrderService.Refresh();
            RebuildOrderDetails();
        }

        private sealed class TrackingCardLiveView
        {
            public GameObject Root { get; private set; }
            public RectTransform ProgressViewport { get; private set; }
            public TextMeshProUGUI Amount { get; private set; }
            public TextMeshProUGUI Summary { get; private set; }
            public TextMeshProUGUI Detail { get; private set; }
            public TextMeshProUGUI EstimatedFinish { get; private set; }
            public TextMeshProUGUI MergedActivity { get; private set; }
            public ProductionOrderState StructuralState { get; set; }
            public bool HasMergedActivity { get; set; }
            public int LastLiveFingerprint { get; set; } = int.MinValue;
            public ProductionOrderRecord CurrentRecord { get; set; }

            public static TrackingCardLiveView Create(GameObject root)
            {
                Transform progressRow = root != null
                    ? root.transform.Find("TrackingMain/TrackingDetailArea/TrackingProgressRow")
                    : null;
                return new TrackingCardLiveView
                {
                    Root = root,
                    ProgressViewport = progressRow?.Find("ProgressTrack/ProgressFillViewport") as RectTransform,
                    Amount = progressRow?.Find("TrackingAmount")?.GetComponent<TextMeshProUGUI>(),
                    Summary = root?.transform.Find("TrackingMain/TrackingTop/TrackingTitleColumn/TrackingSummary")?.GetComponent<TextMeshProUGUI>(),
                    Detail = root?.transform.Find("TrackingMain/TrackingDetailArea/TrackingDetail")?.GetComponent<TextMeshProUGUI>(),
                    EstimatedFinish = root?.transform.Find("TrackingSide/TrackingEstimatedFinish")?.GetComponent<TextMeshProUGUI>(),
                    MergedActivity = root?.transform.Find("TrackingMain/TrackingMergedActivity")?.GetComponent<TextMeshProUGUI>()
                };
            }
        }

        private sealed class TrackingBulkActionsView
        {
            public int Fingerprint { get; set; } = int.MinValue;
        }

        private readonly struct TrackingCardStructure
        {
            public TrackingCardStructure(ProductionOrderRecord record)
            {
                Key = record?.Key ?? string.Empty;
                DisplayId = record?.DisplayId ?? 0;
                State = record != null ? record.State : ProductionOrderState.Cancelled;
                HasMergedActivity = record != null && record.MergeCount > 0;
            }

            public string Key { get; }

            public int DisplayId { get; }

            public ProductionOrderState State { get; }

            public bool HasMergedActivity { get; }

            public bool Matches(ProductionOrderRecord record)
            {
                return record != null &&
                       string.Equals(Key, record.Key ?? string.Empty, System.StringComparison.Ordinal) &&
                       DisplayId == record.DisplayId &&
                       State == record.State &&
                       HasMergedActivity == (record.MergeCount > 0);
            }
        }

    }
}
