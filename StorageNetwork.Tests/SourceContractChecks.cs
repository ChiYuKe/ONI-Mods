using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

internal static class SourceContractChecks
{
    public static void Verify(string repositoryRoot, Action<bool, string> check)
    {
        VerifyMainThreadOptimization(repositoryRoot, check);
        string contentIndex = Read(repositoryRoot,
            "StorageNetwork/Services/Inventory/StorageNetworkContentIndexService.cs");
        string targetSelector = Read(repositoryRoot,
            "StorageNetwork/Services/Filters/StorageTargetSelector.cs");
        string targetFilters = Read(repositoryRoot,
            "StorageNetwork/Services/Filters/StorageTargetSelector.Filters.cs");
        string transferService = Read(repositoryRoot,
            "StorageNetwork/Services/Transfer/NetworkStorageTransferService.cs");
        string keepRules = Read(repositoryRoot,
            "StorageNetwork/ProductionOrders/ProductionOrderService.KeepRules.cs");
        string orderService = Read(repositoryRoot,
            "StorageNetwork/ProductionOrders/ProductionOrderService.cs");
        string productionRuntime = Read(repositoryRoot,
            "StorageNetwork/ProductionOrders/ProductionOrderRuntime.cs");
        string planningShadow = Read(repositoryRoot,
            "StorageNetwork/ProductionOrders/ProductionOrderService.PlanningShadow.cs");
        string materialRequester = Read(repositoryRoot,
            "StorageNetwork/Buildings/Production/Components/StorageNetworkMaterialRequester.cs");
        string materialRequesterStorage = Read(repositoryRoot,
            "StorageNetwork/Buildings/Production/Components/StorageNetworkMaterialRequester.Storage.cs");
        string materialRequesterOutputs = Read(repositoryRoot,
            "StorageNetwork/Buildings/Production/Components/StorageNetworkMaterialRequester.Outputs.cs");
        string powerService = Read(repositoryRoot,
            "StorageNetwork/Buildings/Ports/Power/Components/StorageNetworkPowerService.cs");
        string mainListSignature = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/StorageNetworkPanelListSignature.cs");
        string categorySignature = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/CategorySummary/StorageNetworkCategorySummarySignature.cs");
        string categoryPanel = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/CategorySummary/StorageNetworkPanel.CategorySummary.cs");
        string categoryTrendSampler = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/CategorySummary/StorageNetworkCategorySummaryTrendSampler.cs");
        string enrollableSignature = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/Enrollable/StorageNetworkEnrollableWindowSignature.cs");
        string enrollableRows = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/Enrollable/StorageNetworkPanel.Enrollable.Rows.cs");
        string enrollablePanel = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/Enrollable/StorageNetworkPanel.Enrollable.cs");
        string productionSignature = Read(repositoryRoot,
            "StorageNetwork/UI/ProductionSettings/StorageNetworkProductionSettingsSignatureBuilder.cs");
        string trackingRules = Read(repositoryRoot,
            "StorageNetwork/UI/Order/StorageNetworkOrderTrackingRules.cs");
        string trackingView = Read(repositoryRoot,
            "StorageNetwork/UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Tracking.cs");
        string headerWindow = Read(repositoryRoot,
            "StorageNetwork/UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.cs");
        string headerShared = Read(repositoryRoot,
            "StorageNetwork/UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Shared.cs");
        string orderEditor = Read(repositoryRoot,
            "StorageNetwork/UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.OrderEditor.cs");
        string productList = Read(repositoryRoot,
            "StorageNetwork/UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Products.cs");
        string mainPanel = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/StorageNetworkPanel.cs");
        string mainPanelLiveRows = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/StorageNetworkPanel.LiveRows.cs");
        string mainPanelWorldFilter = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/StorageNetworkPanel.WorldFilter.cs");
        string mainPanelStatus = Read(repositoryRoot,
            "StorageNetwork/UI/Panel/StorageNetworkPanel.Status.cs");
        string keyedRowCache = Read(repositoryRoot,
            "StorageNetwork/UI/Common/StorageNetworkKeyedRowCache.cs");
        string logicDiy = Read(repositoryRoot,
            "StorageNetwork/Buildings/Automation/Components/StorageNetworkLogicDiy.cs");
        string liquidSideScreen = Read(repositoryRoot,
            "StorageNetwork/Buildings/Ports/LiquidOutput/UI/StorageNetworkLiquidOutputPortSideScreen.cs");

        string markDirty = ExtractMethod(contentIndex, "private static void MarkDirty(");
        check(Regex.IsMatch(
                markDirty,
                @"if\s*\(\s*DirtyRecords\.Add\(record\)\s*\)\s*\{\s*GetOrCreateWorld\(record.WorldId\).DirtyRecords.Add\(record\);\s*record\.DirtyFrame\s*=\s*Time\.frameCount\s*;\s*\}.*changeVersion\+\+\s*;",
                RegexOptions.Singleline),
            "content dirty window preserves the first dirty frame and advances every mutation");
        check(contentIndex.Contains("record.DirtyFrame != frame", StringComparison.Ordinal),
            "stale reads merge only until the next simulation frame");
        string scopedDisplayVersion = ExtractMethod(contentIndex,
            "internal static bool TryGetStorageDisplayVersion(");
        check(scopedDisplayVersion.Contains("record.DisplayVersion", StringComparison.Ordinal) &&
              scopedDisplayVersion.Contains("DirtyRecords.Contains(record)", StringComparison.Ordinal) &&
              scopedDisplayVersion.Contains("record.DirtyFrame == Time.frameCount", StringComparison.Ordinal),
            "scoped UI versions wake only for relevant storages and catch up a same-frame stale read");
        string compareSources = ExtractMethod(contentIndex, "private static int CompareSources(");
        check(ContainsInOrder(compareSources,
                "right.Value.CompareTo(left.Value)",
                "left.Key.InstanceId.CompareTo(right.Key.InstanceId)"),
            "source order is amount descending with stable InstanceID tie-break");
        string stableTarget = ExtractMethod(targetSelector, "private static bool IsStableTargetBefore(");
        check(stableTarget.Contains("GetStorageInstanceId(candidate) <", StringComparison.Ordinal) &&
              stableTarget.Contains("GetStorageInstanceId(current)", StringComparison.Ordinal),
            "target tie-break is stable ascending InstanceID");

        string includesAllWorlds = ExtractMethod(contentIndex, "private static bool IncludesAllWorlds(");
        check(includesAllWorlds.Contains("includeRelatedWorlds &&", StringComparison.Ordinal) &&
              includesAllWorlds.Contains("IsCrossPlanetRelayOnline()", StringComparison.Ordinal),
            "cross-world content is gated by both query scope and an online relay");
        string sumWorldAmount = ExtractMethod(contentIndex, "private static float SumWorldAmount(");
        check(sumWorldAmount.Contains("foreach (WorldContentState world in Worlds.Values)", StringComparison.Ordinal) &&
              sumWorldAmount.Contains("Worlds.TryGetValue(worldId", StringComparison.Ordinal),
            "content aggregation has distinct relay-merged and per-world paths");
        string resetContentIndex = ExtractMethod(contentIndex,
            "public static void ResetRuntimeState()");
        check(resetContentIndex.Contains("observedRegistryVersion = -1", StringComparison.Ordinal) &&
              resetContentIndex.Contains("observedCapabilityVersion = -1", StringComparison.Ordinal),
            "content index reset invalidates both membership and capability observations");

        string touch = ExtractMethod(contentIndex, "public void Touch(Storage storage, bool requireNativeRead)");
        check(Regex.IsMatch(
                touch,
                @"if\s*\(\s*touchedSet\.Add\(storage\)\s*\)\s*\{\s*TouchedStorages\.Add\(storage\)\s*;\s*\}",
                RegexOptions.Singleline),
            "transfer transaction deduplicates touched endpoints before commit");
        string recordTransfer = ExtractMethod(contentIndex, "public void RecordTransfer(");
        check(ContainsInOrder(recordTransfer,
                "Touch(source, requireNativeRead: false)",
                "Touch(target, requireNativeRead: false)",
                "AddStorageTags(source",
                "AddStorageTags(target",
                "AddMassDelta(source",
                "AddMassDelta(target"),
            "transfer transaction applies symmetric pending amount and mass deltas");
        check(ContainsInOrder(recordTransfer,
                "AddSourceMassTags(source",
                "AddSourceMassTags(target"),
            "production mass deltas are recorded symmetrically for both transfer endpoints");
        check(recordTransfer.Contains("forcedNativeRead.Contains(source)", StringComparison.Ordinal) &&
              recordTransfer.Contains("forcedNativeRead.Contains(target)", StringComparison.Ordinal) &&
              recordTransfer.Contains("nativeReadRequired.Remove(source)", StringComparison.Ordinal) &&
              recordTransfer.Contains("nativeReadRequired.Remove(target)", StringComparison.Ordinal),
            "explicit deltas replace provisional callbacks without cancelling forced native reads");
        string addStorageTags = ExtractMethod(contentIndex, "private void AddStorageTags(");
        check(addStorageTags.Contains("matchTags.PrefabIdTag", StringComparison.Ordinal) &&
              addStorageTags.Contains("matchTags.PrefabTag", StringComparison.Ordinal) &&
              addStorageTags.Contains("matchTags.ElementTag", StringComparison.Ordinal) &&
              addStorageTags.Contains("matchTags.TransferTag", StringComparison.Ordinal) &&
              addStorageTags.Contains("stateTag", StringComparison.Ordinal) &&
              addStorageTags.Contains("sourceOnly: true", StringComparison.Ordinal),
            "transfer deltas preserve four inventory tags and source-only element-state routing");
        check(contentIndex.Contains("indexedAmount = Mathf.Max(0f, indexedAmount + pendingAmount);", StringComparison.Ordinal) &&
              contentIndex.Contains("record.StoredKg + pendingMassKg", StringComparison.Ordinal),
            "same-transaction queries consume pending amount and capacity deltas");
        string inventoryAmountQuery = ExtractMethod(contentIndex,
            "public static float GetAmount(");
        string inventoryMetricsQuery = ExtractMethod(contentIndex,
            "public static StorageNetworkInventoryMetrics GetMetrics(");
        string hasAnyAmountQuery = ExtractMethod(contentIndex,
            "public static bool HasAnyAmount(");
        string fillAmountsQuery = ExtractMethod(contentIndex,
            "public static void FillAmounts(");
        string productionInventoryQuery = ExtractMethod(contentIndex,
            "internal static void FillProductionSourceInventory(");
        check(inventoryAmountQuery.Contains("GetInventoryAmountDelta", StringComparison.Ordinal) &&
              inventoryMetricsQuery.Contains("GetCapacityMassDelta", StringComparison.Ordinal) &&
              hasAnyAmountQuery.Contains("GetInventoryAmountDelta", StringComparison.Ordinal) &&
              fillAmountsQuery.Contains("ApplyInventoryAmountDeltas", StringComparison.Ordinal) &&
              productionInventoryQuery.Contains("ApplySourceMassDeltas", StringComparison.Ordinal),
            "all aggregate inventory views apply same-transaction amount and mass deltas");
        string applyDeltas = ExtractMethod(contentIndex, "public void ApplyDeltas(StorageContentRecord record)");
        check(ContainsInOrder(applyDeltas,
                "storageDeltas.TryGetValue(", "record.Storage", "out StorageTransferDeltas deltas",
                "CanApplyKnownDelta(record)", "RefreshRecord(record)",
                "deltas.InventoryAmounts", "ApplyAmountDelta(record.Amounts",
                "world.AmountsByTag", "deltas.SourceAmounts", "ApplyAmountDelta(record.SourceAmounts",
                "GetOrCreateSourceBucket(pair.Key).Set(record, amount)",
                "deltas.SourceMasses", "ApplyAmountDelta(record.SourceMasses",
                "world.SourceMassesByTag", "deltas.MassKg", "record.ItemDetailsDirty = true") &&
              !ContainsAny(applyDeltas, "RemoveRecordContributions(record)", "AddRecordContributions(record)"),
            "known delta commit updates only changed tags and preserves native fallback for changed eligibility");
        string scopedRefresh = ExtractMethod(contentIndex, "private static void EnsureFresh(");
        string freshStorage = ExtractMethod(contentIndex, "private static void EnsureStorageFresh(");
        string refreshDetails = ExtractMethod(contentIndex, "private static void RefreshItemDetails(");
        string worldDirty = ExtractMethod(contentIndex, "private static void AppendWorldDirtyRecords(");
        check(scopedRefresh.Contains("specificStorage", StringComparison.Ordinal) &&
              scopedRefresh.Contains("IncludesAllWorlds(includeRelatedWorlds)", StringComparison.Ordinal) &&
              scopedRefresh.Contains("GetLandedRocketWorldIds(parentId)", StringComparison.Ordinal) &&
              worldDirty.Contains("world.DirtyRecords", StringComparison.Ordinal) &&
              freshStorage.Contains("specificStorage: storage", StringComparison.Ordinal),
            "strict scoped refresh visits selected storage or related world dirty sets including landed rockets");
        check(refreshDetails.Contains("DirtyRecords.Contains(record)", StringComparison.Ordinal) &&
              refreshDetails.Contains("!CanApplyKnownDelta(record)", StringComparison.Ordinal) &&
              refreshDetails.Contains("RefreshRecord(record)", StringComparison.Ordinal) &&
              refreshDetails.Contains("detailsOnly: true", StringComparison.Ordinal) &&
              !ContainsAny(refreshDetails, "RemoveRecordContributions", "AddRecordContributions", "GetOrCreateSourceBucket"),
            "native detail reconciliation preserves committed amount/source aggregates with conservative full fallback");
        string transactionReset = ExtractMethod(contentIndex, "public void Reset()");
        string getStorageDeltas = ExtractMethod(contentIndex,
            "private StorageTransferDeltas GetOrCreateStorageDeltas(Storage storage)");
        string deltaBucket = ExtractType(contentIndex,
            "private sealed class StorageTransferDeltas");
        check(contentIndex.Contains(
                  "Dictionary<Storage, StorageTransferDeltas> storageDeltas",
                  StringComparison.Ordinal) &&
              contentIndex.Contains(
                  "List<StorageTransferDeltas> reusableDeltaBuckets",
                  StringComparison.Ordinal) &&
              !contentIndex.Contains("Dictionary<TransferStorageTagKey", StringComparison.Ordinal) &&
              !contentIndex.Contains("struct TransferStorageTagKey", StringComparison.Ordinal),
            "transfer deltas are partitioned by Storage instead of one global storage-tag table");
        check(!applyDeltas.Contains(
                  "foreach (KeyValuePair<Storage, StorageTransferDeltas>",
                  StringComparison.Ordinal) &&
              getStorageDeltas.Contains("storageDeltas.TryGetValue(storage", StringComparison.Ordinal) &&
              getStorageDeltas.Contains("reusableDeltaBuckets.RemoveAt(reusableIndex)", StringComparison.Ordinal) &&
              getStorageDeltas.Contains("new StorageTransferDeltas()", StringComparison.Ordinal) &&
              ContainsInOrder(transactionReset,
                  "foreach (StorageTransferDeltas deltas in storageDeltas.Values)",
                  "deltas.Reset()",
                  "reusableDeltaBuckets.Add(deltas)",
                  "storageDeltas.Clear()") &&
              deltaBucket.Contains("SourceAmounts.Clear()", StringComparison.Ordinal) &&
              deltaBucket.Contains("InventoryAmounts.Clear()", StringComparison.Ordinal) &&
              deltaBucket.Contains("SourceMasses.Clear()", StringComparison.Ordinal),
            "delta commit is O(tags for one touched Storage) and recycles cleared buckets across transactions");
        string addIndexedItem = ExtractMethod(contentIndex,
            "private static void AddItem(StorageContentRecord record, GameObject itemObject, bool detailsOnly = false)");
        string fillIndexedItemTotals = ExtractMethod(contentIndex,
            "internal static bool TryFillStorageItemTotals(");
        string addIndexedItemTotal = ExtractMethod(contentIndex,
            "private static void AddIndexedItemTotal(");
        string captureTransferItem = ExtractMethod(transferService,
            "public TransferItemSnapshot(GameObject item)");
        check(ContainsInOrder(addIndexedItem,
                "float massKg = primaryElement != null ? primaryElement.Mass : 0f",
                "pickupable != null",
                "pickupable.TotalAmount",
                ": massKg") &&
              ContainsInOrder(captureTransferItem,
                  "pickupable != null",
                  "pickupable.TotalAmount",
                  "primaryElement.Mass"),
            "non-Pickupable transfer deltas use the same native mass unit as the content index");
        check(fillIndexedItemTotals.Contains("record.ItemTotals", StringComparison.Ordinal) &&
              !fillIndexedItemTotals.Contains("storage.items", StringComparison.Ordinal) &&
              addIndexedItemTotal.Contains("matchTags.PrefabIdTag", StringComparison.Ordinal) &&
              addIndexedItemTotal.Contains("record.ItemTotalIndexes", StringComparison.Ordinal) &&
              !addIndexedItemTotal.Contains("GetStoredItemKey", StringComparison.Ordinal),
            "visible item summaries merge compact Tag-indexed records instead of rescanning native items");
        string observedLoss = ExtractMethod(transferService,
            "private static void GetObservedStorageLoss(");
        string directOwnership = ExtractMethod(transferService,
            "private static bool IsItemStoredIn(Storage storage, GameObject item)");
        check(observedLoss.Contains("IsItemStoredIn(source, originalItem)", StringComparison.Ordinal) &&
              directOwnership.Contains("item.transform.parent == storage.transform", StringComparison.Ordinal) &&
              !directOwnership.Contains("items.Contains", StringComparison.Ordinal) &&
              !Regex.IsMatch(transferService, @"source\s*(?:\?)?\.items\.Contains\s*\("),
            "observed transfer loss uses O(1) direct-parent ownership instead of source item-list scans");
        string commitTransaction = ExtractMethod(contentIndex,
            "private static void CommitTransferTransaction(");
        check(commitTransaction.Contains("if (transaction.HasNativeReadRequirements)", StringComparison.Ordinal) &&
              commitTransaction.Contains("StorageTargetSelector.InvalidateOutputTargetCache();", StringComparison.Ordinal) &&
              ContainsInOrder(commitTransaction,
                  "if (transaction.RequiresNativeRead(storage))",
                  "RefreshRecord(record)",
                  "else",
                  "transaction.ApplyDeltas(record)"),
            "transaction commit uses native fallback only for unexplained/forced endpoints");
        string endTransaction = ExtractMethod(contentIndex,
            "internal static void EndTransferTransaction()");
        check(ContainsInOrder(endTransaction,
                "transaction.Depth--;",
                "if (transaction.Depth > 0)",
                "CommitTransferTransaction(transaction)"),
            "nested transfer scopes commit once at the outermost service boundary");

        string recordMutation = ExtractMethod(contentIndex,
            "internal static void RecordTransferMutation(");
        check(ContainsInOrder(recordMutation,
                "transaction.RecordTransfer(",
                "StorageTargetSelector.NotifyTransferMutation(",
                "-movedAmount",
                "-movedMassKg",
                "StorageTargetSelector.NotifyTransferMutation(",
                "movedAmount",
                "movedMassKg") &&
              !recordMutation.Contains("InvalidateOutputTargetCache", StringComparison.Ordinal),
            "known transfer endpoints notify the output cache with symmetric selective deltas");
        string touchTransfer = ExtractMethod(contentIndex,
            "internal static void TouchTransferStorage(Storage storage)");
        string forceNativeRead = ExtractMethod(contentIndex,
            "public void ForceNativeRead(Storage storage)");
        bool forcesNativeTransferRead =
            touchTransfer.Contains("transaction.ForceNativeRead(storage)", StringComparison.Ordinal) &&
            forceNativeRead.Contains("forcedNativeRead.Add(storage)", StringComparison.Ordinal);
        check(forcesNativeTransferRead &&
              touchTransfer.IndexOf("StorageTargetSelector.InvalidateOutputTargetCache()", StringComparison.Ordinal) >
              touchTransfer.IndexOf("transaction.", StringComparison.Ordinal),
            "unknown transfer mutations conservatively invalidate the output cache");
        string forceNativeInvalidation = ExtractMethod(contentIndex,
            "private static void ForceNativeInvalidation(Storage storage)");
        string invalidateContentWorld = ExtractMethod(contentIndex,
            "internal static void InvalidateWorld(int worldId)");
        check(forceNativeInvalidation.Contains("InvalidateWorld(worldId)", StringComparison.Ordinal) &&
              invalidateContentWorld.Contains("transaction.ForceNativeRead(record.Storage)", StringComparison.Ordinal) &&
              contentIndex.Contains("ForceNativeInvalidation(storage);", StringComparison.Ordinal),
            "shadow mismatch rebuilds its world and cannot be overwritten by a later explicit delta");

        string ensureItemDetails = ExtractMethod(contentIndex,
            "private static void EnsureItemDetailsFresh(");
        bool marksItemDetailsBeforeCommit =
            recordTransfer.Contains("MarkItemDetailsDirty", StringComparison.Ordinal) ||
            recordTransfer.Contains("ItemDetailDirtyRecords.Add", StringComparison.Ordinal);
        bool directlyFlushesTouchedItemDetails =
            ensureItemDetails.Contains("RefreshTouched", StringComparison.Ordinal) ||
            ensureItemDetails.Contains("FlushTouched", StringComparison.Ordinal) ||
            ContainsInOrder(ensureItemDetails,
                "EnsureTransactionNativeBaseline(",
                "forceTouchedRecords: true",
                "ItemDetailDirtyRecords.Count == 0");
        check(marksItemDetailsBeforeCommit || directlyFlushesTouchedItemDetails,
            "strict item queries can observe completed native transfers before transaction commit");

        string notifyTransfer = ExtractMethod(targetSelector,
            "internal static void NotifyTransferMutation(");
        check(notifyTransfer.Contains("key.SourceStorageId == storageId", StringComparison.Ordinal) &&
              notifyTransfer.Contains("winnerStrengthened", StringComparison.Ordinal) &&
              notifyTransfer.Contains("key.MatchesExactly(matchTags)", StringComparison.Ordinal) &&
              notifyTransfer.Contains("competitorMayImprove", StringComparison.Ordinal) &&
              notifyTransfer.Contains("winnerMayWeaken", StringComparison.Ordinal) &&
              notifyTransfer.Contains("OutputTargetCacheRemovalWorkspace.Add(key)", StringComparison.Ordinal) &&
              !notifyTransfer.Contains("OutputTargetCache.Clear", StringComparison.Ordinal),
            "known transfers selectively evict only decisions that can change winner");
        bool sourceSkipRemoved =
            !notifyTransfer.Contains("key.SourceStorageId == storageId", StringComparison.Ordinal);
        bool sourceSkipTracksExclusion =
            (notifyTransfer.Contains("key.SourceIsExcluded", StringComparison.Ordinal) ||
             notifyTransfer.Contains("key.ExcludedSourceStorageId", StringComparison.Ordinal)) &&
            CacheKeyTracksExclusion(targetSelector);
        check(sourceSkipRemoved || sourceSkipTracksExclusion,
            "selective invalidation skips a source only when that source is actually excluded");
        string cacheKey = ExtractType(targetSelector,
            "private readonly struct OutputTargetCacheKey");
        check(ContainsInOrder(cacheKey,
                "worldId",
                "prefabIdTag",
                "prefabTag",
                "elementTag",
                "transferTag",
                "sourceStorageId",
                "coldStorage") &&
              cacheKey.Contains("public bool Matches", StringComparison.Ordinal),
            "output target cache key preserves world, four tags, source exclusion, and cold mode");
        string cacheVersions = ExtractMethod(targetSelector,
            "private static void RefreshOutputTargetCacheVersion()");
        check(cacheVersions.Contains("StorageSceneRegistry.CapabilityVersion", StringComparison.Ordinal) &&
              cacheVersions.Contains("StorageSceneRegistry.MembershipVersion", StringComparison.Ordinal) &&
              cacheVersions.Contains("StorageSceneRegistry.ConnectivityVersion", StringComparison.Ordinal) &&
              cacheVersions.Contains("StorageNetworkInputTargetReservationService.Version", StringComparison.Ordinal) &&
              !cacheVersions.Contains("ContentIndexService.Version", StringComparison.Ordinal),
            "output target cache invalidates by structural/reservation versions, not every content delta");
        string tryCachedTarget = ExtractMethod(targetSelector,
            "private static bool TryGetCachedOutputTarget(");
        check(tryCachedTarget.Contains("OutputTargetCacheSeconds", StringComparison.Ordinal) &&
              tryCachedTarget.Contains("IsUsableOutputTarget", StringComparison.Ordinal) &&
              tryCachedTarget.Contains("IsReservedForAutoInput", StringComparison.Ordinal) &&
              tryCachedTarget.Contains("IsAutoOutputMatch", StringComparison.Ordinal),
            "cached target reuse revalidates TTL, capacity/filter, reservation, and routing eligibility");
        string validateCachedTarget = ExtractMethod(targetSelector,
            "private static Storage ValidateCachedOutputTarget(");
        string nativeTargetComparator = ExtractMethod(targetSelector,
            "private static Storage FindNativeOutputTargetInStorages(");
        string nativeUsability = ExtractMethod(targetFilters,
            "private static bool IsUsableOutputTargetNative(");
        string nativeAutoMatch = ExtractMethod(targetFilters,
            "private static bool IsAutoOutputMatchNative(");
        string nativeAvailable = ExtractMethod(targetFilters,
            "private static float GetNativeAmountAvailableByAnyMatchTag(");
        check(validateCachedTarget.Contains("ValidateSelectedOutputTarget", StringComparison.Ordinal) &&
              validateCachedTarget.Contains("ValidateSelectedFoodOutputTarget", StringComparison.Ordinal) &&
              !validateCachedTarget.Contains("FindOutputTargetInStorages(", StringComparison.Ordinal),
            "cached target shadow delegates to the native normal or cold-first food comparator");
        string foodTargetValidator = ExtractMethod(targetSelector,
            "private static Storage ValidateSelectedFoodOutputTarget(");
        string targetValidationVersion = ExtractMethod(targetSelector,
            "private static int GetTargetSelectionValidationVersion()");
        check(foodTargetValidator.Contains("FindNativeOutputTargetInStorages", StringComparison.Ordinal) &&
              foodTargetValidator.Contains("if (nativeTarget == null)", StringComparison.Ordinal),
            "food target shadow validates the final cold-first/general-fallback decision once");
        check(targetValidationVersion.Contains("ContentIndexService.Version", StringComparison.Ordinal) &&
              targetValidationVersion.Contains("MembershipVersion", StringComparison.Ordinal) &&
              targetValidationVersion.Contains("CapabilityVersion", StringComparison.Ordinal) &&
              targetValidationVersion.Contains("ConnectivityVersion", StringComparison.Ordinal) &&
              targetValidationVersion.Contains("ReservationService.Version", StringComparison.Ordinal),
            "target shadow version advances for content, topology, capability, connectivity, and reservations");
        check(nativeTargetComparator.Contains("IsUsableOutputTargetNative", StringComparison.Ordinal) &&
              nativeTargetComparator.Contains("IsAutoOutputMatchNative", StringComparison.Ordinal) &&
              nativeTargetComparator.Contains("GetNativeAmountAvailableByAnyMatchTag", StringComparison.Ordinal) &&
              nativeTargetComparator.Contains("StorageNetworkContentShadowReader.GetRemainingCapacity", StringComparison.Ordinal) &&
              nativeTargetComparator.Contains("IsStableTargetBefore", StringComparison.Ordinal) &&
              !nativeTargetComparator.Contains("StorageNetworkContentIndexService", StringComparison.Ordinal) &&
              !nativeUsability.Contains("StorageNetworkContentIndexService", StringComparison.Ordinal) &&
              !nativeAutoMatch.Contains("StorageNetworkContentIndexService", StringComparison.Ordinal) &&
              !nativeAvailable.Contains("StorageNetworkContentIndexService", StringComparison.Ordinal),
            "native target shadow comparator is independent of indexed amount/capacity reads");
        string targetOwnership = ExtractMethod(targetFilters,
            "private static bool IsItemOwnedByStorage(Storage storage, GameObject item)");
        check(targetOwnership.Contains(
                  "item.transform.parent == storage.transform",
                  StringComparison.Ordinal) &&
              !targetOwnership.Contains("items.Contains", StringComparison.Ordinal) &&
              Regex.Matches(targetFilters, "!IsItemOwnedByStorage\\(target, item\\)").Count >= 3 &&
              !Regex.IsMatch(targetFilters, @"target\s*(?:\?)?\.items\.Contains\s*\("),
            "target candidate validation uses O(1) direct-parent ownership in indexed and native paths");
        string canCacheTarget = ExtractMethod(targetSelector,
            "private static bool CanCacheOutputTarget(");
        check(canCacheTarget.Contains("excludedStorages.Count == 0", StringComparison.Ordinal) &&
              canCacheTarget.Contains("excludedStorages.Count == 1", StringComparison.Ordinal) &&
              canCacheTarget.Contains("excludedStorages.Contains(sourceStorage)", StringComparison.Ordinal),
            "output target cache rejects arbitrary exclusion domains");
        check(targetSelector.Contains("private const int MaxOutputTargetCacheEntries = 512;", StringComparison.Ordinal) &&
              targetSelector.Contains("private const float OutputTargetCacheSeconds = 1f;", StringComparison.Ordinal),
            "output target cache has bounded cardinality and lifetime");

        string setKeepRule = ExtractMethod(keepRules,
            "public void SetKeepRule(");
        string clearKeepRule = ExtractMethod(keepRules,
            "public void ClearKeepRule(");
        string runKeepRules = ExtractMethod(keepRules,
            "private void RunKeepRules()");
        string cancelKeepRule = ExtractMethod(keepRules,
            "private static void CancelKeepRuleOrder(");
        check(!keepRules.Contains("using System.Linq", StringComparison.Ordinal) &&
              !ContainsAny(runKeepRules,
                  ".ToDictionary(", ".ToList(", ".FirstOrDefault(", ".Where(", ".Sum("),
            "steady Keep Rule maintenance has no LINQ/materialized collection pipeline");
        check(ContainsInOrder(runKeepRules,
                "GetCraftableRoutesProducing(rule.ProductTag)",
                "for (int routeIndex",
                "foreach (ProductionOrderRecord order in ActiveOrders.Values)") &&
              runKeepRules.Contains("IsOrderReachableFromCurrentWorld(order)", StringComparison.Ordinal),
            "Keep Rule maintenance reuses route index and world-scoped order iteration");
        check(ContainsInOrder(setKeepRule,
                "KeepRules.TryGetValue",
                "existing.TargetAmount == targetAmount",
                "return;",
                "KeepRules[product.ProductTag]",
                "MarkOrdersChanged()") &&
              clearKeepRule.Contains("if (KeepRules.Remove(productTag))", StringComparison.Ordinal) &&
              clearKeepRule.Contains("MarkOrdersChanged()", StringComparison.Ordinal) &&
              cancelKeepRule.Contains("MarkOrdersChanged()", StringComparison.Ordinal),
            "Keep Rule order version advances only for real set/clear/cancel mutations");
        string markOrdersChanged = ExtractMethod(orderService,
            "private static void MarkOrdersChanged()");
        check(orderService.Contains("internal static int OrderVersion => orderVersion", StringComparison.Ordinal) &&
              markOrdersChanged.Contains("orderVersion++", StringComparison.Ordinal) &&
              headerWindow.Contains("ProductionOrderService.OrderVersion", StringComparison.Ordinal) &&
              trackingView.Contains("ProductionOrderService.OrderVersion", StringComparison.Ordinal),
            "order and tracking UI refreshes are gated by the shared mutation version");
        check(productionRuntime.Contains(
                  "public IReadOnlyList<RecipeDisplayInfo> Recipes { get; }",
                  StringComparison.Ordinal) &&
              productionRuntime.Contains(".AsReadOnly()", StringComparison.Ordinal),
            "production recipe snapshots expose a versioned immutable recipe view");

        string getRecipeToRequest = ExtractMethod(materialRequester,
            "private ComplexRecipe GetRecipeToRequest()");
        string fillRequesterSources = ExtractMethod(materialRequester,
            "private void FillSourceStorages(");
        string forceStoreOutputs = ExtractMethod(materialRequesterOutputs,
            "public void ForceStoreProducedOutputs(");
        string getFabricatorStorages = ExtractMethod(materialRequesterStorage,
            "private HashSet<Storage> GetFabricatorStorages()");
        check(!materialRequester.Contains("using System.Linq", StringComparison.Ordinal) &&
              !ContainsAny(getRecipeToRequest, "new List<", "=>") &&
              getRecipeToRequest.Contains("queuedRecipeBuffer", StringComparison.Ordinal) &&
              getRecipeToRequest.Contains("queuedRecipeComparer", StringComparison.Ordinal),
            "material requester reuses its queue buffer and non-capturing comparer");
        check(fillRequesterSources.Contains("sourceWantedTagBuffer[0] = tag", StringComparison.Ordinal) &&
              fillRequesterSources.Contains("StorageNetworkSourceIndexService.FillSourceStorages", StringComparison.Ordinal) &&
              !ContainsAny(fillRequesterSources, "new Tag[", "new List<", "yield return"),
            "material requester fills indexed sources into reusable tag and storage buffers");
        check(forceStoreOutputs.Contains("producedOutputBuffer", StringComparison.Ordinal) &&
              !ContainsAny(forceStoreOutputs, ".ToList(", "new List<", "new HashSet<") &&
              !ContainsAny(getFabricatorStorages, "new HashSet<", "yield return"),
            "material output routing reuses product and exclusion collections");

        string validatePlanShadow = ExtractMethod(planningShadow,
            "private ProductionPlanNode ValidateProductionPlanShadow(");
        check(ContainsInOrder(validatePlanShadow,
                "if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)",
                "return optimized;",
                "StorageNetworkShadowValidationService.ShouldValidate") &&
              planningShadow.Contains("BuildProductionPlanArtifacts(", StringComparison.Ordinal) &&
              planningShadow.Contains("BuildNativeMaterialLeaseReference(", StringComparison.Ordinal) &&
              Regex.Matches(planningShadow, @"bool\[\]\s+matched\s*=\s*new bool\[right\.Count\]").Count == 3,
            "production shadow validation compares plans, derived artifacts, and exact multisets");

        string getPowerAggregate = ExtractMethod(powerService,
            "private static PowerAggregateCache GetAggregateCache()");
        string buildPowerAggregate = ExtractMethod(powerService,
            "private static void BuildAggregateCache(");
        string powerCacheType = ExtractType(powerService,
            "private sealed class PowerAggregateCache");
        check(powerService.Contains("private static readonly PowerAggregateCache AggregateCache", StringComparison.Ordinal) &&
              getPowerAggregate.Contains("BuildAggregateCache(now, registryVersion, AggregateCache)", StringComparison.Ordinal) &&
              !buildPowerAggregate.Contains("new PowerAggregateCache", StringComparison.Ordinal) &&
              powerCacheType.Contains("Global.Reset()", StringComparison.Ordinal) &&
              powerCacheType.Contains("state.Reset()", StringComparison.Ordinal),
            "power aggregation rebuilds the 200ms snapshot in reusable per-world buffers");

        string mainStructural = ExtractMethod(mainListSignature,
            "internal static string BuildStorageListSignature(");
        check(!ContainsAny(mainStructural,
                "MassStored", "StoredKg", "CapacityKg", "Temperature", "Joules", "Particles", "StoredItems"),
            "main list structural signature excludes live quantities and item membership");
        check(!mainStructural.Contains("isOfflineNetworkServer", StringComparison.Ordinal),
            "main list structural signature excludes live online/offline status");
        string mainPanelUpdate = ExtractMethod(mainPanel, "private void Update()");
        string mainPanelRefresh = ExtractMethod(mainPanel, "private void RefreshStoragePanelMain(");
        string mainPanelRefreshWrapper = ExtractMethod(mainPanel, "private void RefreshStoragePanel(");
        string mainPanelLiveMetrics = ExtractMethod(mainPanelLiveRows,
            "private void RefreshMainPanelLiveMetrics()");
        string liveItemRefresh = ExtractMethod(mainPanelLiveRows,
            "private void UpdateStoredItemLiveViews(");
        string reconcileStoredItems = ExtractMethod(mainPanelLiveRows,
            "private void ReconcileStoredItemSection(");
        string commitKeyedRows = ExtractMethod(keyedRowCache, "public void Commit()");
        check(!mainPanelUpdate.Contains("StructureRefreshSeconds", StringComparison.Ordinal) &&
              mainPanelUpdate.Contains("registryVersion != lastObservedRegistryVersion", StringComparison.Ordinal),
            "main panel structural refresh is version-driven rather than TTL-polled");
        check(mainPanelRefresh.Contains("mode != StoragePanelRefreshMode.Live", StringComparison.Ordinal) &&
              !Regex.IsMatch(mainPanelRefresh,
                  @"currentSnapshot\s*=\s*CollectMainSnapshot\(false\)\s*;"),
            "main panel live refresh reuses its structural snapshot");
        string validateMainWorldFilter = ExtractMethod(mainPanelWorldFilter,
            "private void EnsureValidMainWorldFilter()");
        string updateMainSummary = ExtractMethod(mainPanelStatus,
            "private void UpdateStorageSummaryText()");
        check(mainPanelRefreshWrapper.Contains("UpdateCategorySummaryPanel", StringComparison.Ordinal) &&
              !mainPanelRefresh.Contains("UpdateCategorySummaryPanel", StringComparison.Ordinal) &&
              validateMainWorldFilter.Contains("mainWorldFilterValidatedMembershipVersion", StringComparison.Ordinal) &&
              updateMainSummary.Contains("lastSummaryFingerprint", StringComparison.Ordinal),
            "main panel excludes nested category timing and caches unchanged world-filter/summary work");
        check(mainPanelLiveMetrics.Contains("ReferenceEquals(liveStorageInfoSnapshot, currentSnapshot)", StringComparison.Ordinal) &&
              mainPanelLiveMetrics.Contains("TryGetMainIndexedMetrics", StringComparison.Ordinal) &&
              mainPanelLiveRows.Contains("StorageNetworkInventoryIndexService.GetMetrics", StringComparison.Ordinal),
            "main live metrics reuse the structural lookup and single-world aggregate snapshot");
        check(mainPanelLiveRows.Contains("TryGetStorageMetrics", StringComparison.Ordinal) &&
              liveItemRefresh.Contains("PopulateStoredItemSectionFromIndex", StringComparison.Ordinal) &&
              liveItemRefresh.Contains("storageInfo.ContentStorages", StringComparison.Ordinal) &&
              liveItemRefresh.Contains("ReconcileStoredItemSection", StringComparison.Ordinal) &&
              !liveItemRefresh.Contains("RequestDeferredStoragePanelStructureRefresh", StringComparison.Ordinal) &&
              reconcileStoredItems.Contains("section.Rows.TryUse", StringComparison.Ordinal),
            "main panel updates live values and item membership through per-storage keyed rows");
        string storedItemStructureChanged = ExtractMethod(mainPanelLiveRows,
            "private static bool HasStoredItemStructureChanged(");
        string updateStoredItemRowLive = ExtractMethod(mainPanelLiveRows,
            "private static void UpdateStoredItemRowLive(");
        check(liveItemRefresh.Contains("HasStoredItemStructureChanged", StringComparison.Ordinal) &&
              storedItemStructureChanged.Contains("section.Aggregates.ContainsKey", StringComparison.Ordinal) &&
              updateStoredItemRowLive.Contains("LastMassFingerprint", StringComparison.Ordinal) &&
              updateStoredItemRowLive.Contains("LastTemperatureFingerprint", StringComparison.Ordinal),
            "unchanged expanded-item structures skip sorting and format only changed values");
        check(mainPanelLiveRows.Contains(
                  "private const int StoredItemVirtualizationThreshold = 32;",
                  StringComparison.Ordinal) &&
              reconcileStoredItems.Contains("GetStoredItemVisibleRange", StringComparison.Ordinal) &&
              reconcileStoredItems.Contains("UseStoredItemSpacer", StringComparison.Ordinal) &&
              mainPanelUpdate.Contains("mainListViewportDirty", StringComparison.Ordinal),
            "expanded item-heavy storage sections render only viewport rows with bounded overscan");
        check(keyedRowCache.Contains("public bool TryUse(", StringComparison.Ordinal) &&
              keyedRowCache.Contains("int maxInactiveEntries = 32", StringComparison.Ordinal) &&
              keyedRowCache.Contains("inactiveEntries", StringComparison.Ordinal) &&
              keyedRowCache.Contains("evictedKeys", StringComparison.Ordinal) &&
              commitKeyedRows.Contains("Math.Max(maxInactiveEntries, order * 2)", StringComparison.Ordinal) &&
              !commitKeyedRows.Contains("new List<", StringComparison.Ordinal),
            "keyed row reconciliation reuses commit buffers and bounds its inactive LRU");
        string categoryStructure = ExtractMethod(categorySignature,
            "public static string BuildStructure(");
        check(!ContainsAny(categoryStructure, "MassKg", "MassStored", "Capacity", "Temperature"),
            "category structural signature contains keys only");
        string categoryRefresh = ExtractMethod(categoryPanel,
            "private void UpdateCategorySummaryPanel()");
        string categoryValues = ExtractMethod(categoryPanel,
            "private void RebuildCategorySummaryValues(");
        string categoryReconcile = ExtractMethod(categoryPanel,
            "private void ReconcileCategorySummaryRows(");
        string categoryVisibleRange = ExtractMethod(categoryPanel,
            "private void GetCategorySummaryVisibleRange(");
        string stableDisplayOrder = Read(repositoryRoot, "StorageNetwork/UI/Common/StorageNetworkStableDisplayOrder.cs");
        check(categoryRefresh.Contains("categorySummaryDisplayOrder.Apply", StringComparison.Ordinal) &&
              stableDisplayOrder.Contains("comparer.Compare(name(a), name(b))", StringComparison.Ordinal) &&
              stableDisplayOrder.Contains("StringComparer.Ordinal.Compare(key(a), key(b))", StringComparison.Ordinal) &&
              !stableDisplayOrder.Contains("MassKg", StringComparison.Ordinal),
            "category rendering order is stable by key rather than live mass rank");
        check(!categoryRefresh.Contains("ForceRebuildLayoutImmediate", StringComparison.Ordinal) &&
              !categoryRefresh.Contains("ForceCategorySummaryLayout", StringComparison.Ordinal),
            "category keyed diffs defer one coalesced Unity layout instead of forcing it synchronously");
        check(categoryRefresh.Contains("TryGetStorageDisplayVersion", StringComparison.Ordinal) &&
              categoryRefresh.Contains("categorySummaryObservedStorageVersion", StringComparison.Ordinal),
            "category live aggregation is gated by its selected storages rather than global content churn");
        string incrementalCategory = Read(repositoryRoot, "StorageNetwork/UI/Panel/CategorySummary/StorageNetworkPanel.CategorySummary.Incremental.cs");
        check(categoryValues.Contains("TryUpdateCategorySummaryIndexedTotals", StringComparison.Ordinal) &&
              categoryValues.IndexOf("TryUpdateCategorySummaryIndexedTotals", StringComparison.Ordinal) <
              categoryValues.IndexOf("foreach (Storage storage in storages)", StringComparison.Ordinal),
            "category summary reads the content index first and keeps native traversal only as compatibility fallback");
        check(incrementalCategory.Contains("TryGetSingleStorageDisplayVersion", StringComparison.Ordinal) &&
              incrementalCategory.Contains("TryReadSingleStorageItemTotals", StringComparison.Ordinal) &&
              incrementalCategory.Contains("categorySummaryAggregation.SetSource", StringComparison.Ordinal),
            "category summary replaces only changed source contributions");
        check(categoryPanel.Contains(
                  "private const int CategorySummaryVirtualizationThreshold = 32;",
                  StringComparison.Ordinal) &&
              categoryReconcile.Contains("firstVisible", StringComparison.Ordinal) &&
              categoryReconcile.Contains("lastVisibleExclusive", StringComparison.Ordinal) &&
              categoryReconcile.Contains("UseCategorySummarySpacer", StringComparison.Ordinal) &&
              categoryVisibleRange.Contains("categorySummaryContent.anchoredPosition.y", StringComparison.Ordinal) &&
              categoryVisibleRange.Contains("CategorySummaryVirtualizationOverscan", StringComparison.Ordinal),
            "long category lists reconcile only a viewport-sized keyed-row window with spacers");
        string enforceTrendBudget = ExtractMethod(categoryTrendSampler,
            "private void EnforceMemoryBudget()");
        check(!categoryTrendSampler.Contains("using System.Linq", StringComparison.Ordinal) &&
              enforceTrendBudget.Contains("evictionWorkspace.Sort", StringComparison.Ordinal) &&
              !ContainsAny(enforceTrendBudget, ".OrderBy(", ".Take(", ".Select("),
            "trend-series eviction reuses a bounded workspace without a LINQ allocation pipeline");
        string requestOrderLayout = ExtractMethod(headerShared,
            "private void ForceOrderLayout(");
        string runOrderLayout = ExtractMethod(headerShared,
            "private void RunDeferredOrderLayout(");
        string viewportHeight = ExtractMethod(orderEditor,
            "private float GetOrderWorkspaceViewportHeight(");
        check(requestOrderLayout.Contains("deferredOrderLayoutFrame", StringComparison.Ordinal) &&
              !ContainsAny(requestOrderLayout,
                  "Canvas.ForceUpdateCanvases", "ForceRebuildLayoutImmediate") &&
              CountOccurrences(runOrderLayout, "ForceRebuildLayoutImmediate") == 1 &&
              !viewportHeight.Contains("Canvas.ForceUpdateCanvases", StringComparison.Ordinal),
            "order and tracking structures coalesce to one deferred root layout pass");
        string productRebuild = ExtractMethod(productList,
            "private void RebuildProductList(");
        string productVisibleRange = ExtractMethod(productList,
            "private void GetProductVisibleRange(");
        string productStyle = ExtractMethod(productList,
            "private static ColorStyleSetting CreateProductRowStyle(");
        check(productList.Contains(
                  "private const int ProductVirtualizationThreshold = 32;",
                  StringComparison.Ordinal) &&
              productRebuild.Contains("firstVisible", StringComparison.Ordinal) &&
              productRebuild.Contains("UseProductSpacer", StringComparison.Ordinal) &&
              productVisibleRange.Contains("productListContent.anchoredPosition.y", StringComparison.Ordinal) &&
              productStyle.Contains("productSelectedStyle", StringComparison.Ordinal) &&
              productStyle.Contains("productNormalStyle", StringComparison.Ordinal),
            "long product lists use viewport-sized keyed rows and shared immutable styles");
        check(enrollablePanel.Contains(
                  "private const int EnrollableVirtualizationThreshold = 32;",
                  StringComparison.Ordinal) &&
              enrollablePanel.Contains("UseEnrollableSpacer", StringComparison.Ordinal) &&
              enrollablePanel.Contains("OnEnrollableScroll", StringComparison.Ordinal) &&
              !ContainsAny(enrollablePanel,
                  "Canvas.ForceUpdateCanvases", "LayoutRebuilder.ForceRebuildLayoutImmediate"),
            "long enrollment lists use variable-height viewport virtualization without synchronous layout");
        check(trackingView.Contains(
                  "private const int TrackingVirtualizationThreshold = 16;",
                  StringComparison.Ordinal) &&
              trackingView.Contains("UseTrackingSpacer", StringComparison.Ordinal) &&
              trackingView.Contains("OnOrderTrackingScroll", StringComparison.Ordinal) &&
              !ContainsAny(trackingView,
                  "Canvas.ForceUpdateCanvases", "LayoutRebuilder.ForceRebuildLayoutImmediate"),
            "long tracking lists use variable-height viewport virtualization without synchronous layout");
        string worldLiquidMasses = ExtractMethod(contentIndex,
            "internal static void FillWorldLiquidMasses(");
        string liquidAmounts = ExtractMethod(liquidSideScreen,
            "private Dictionary<SimHashes, float> BuildAvailableLiquidAmounts(");
        string liquidOptions = ExtractMethod(liquidSideScreen,
            "private List<LiquidOption> BuildOptions(");
        check(worldLiquidMasses.Contains("world.ElementMasses", StringComparison.Ordinal) &&
              !ContainsAny(worldLiquidMasses, ".items", "GetComponent<PrimaryElement>") &&
              liquidAmounts.Contains("FillWorldLiquidMasses", StringComparison.Ordinal) &&
              liquidAmounts.Contains("TryFillStorageLiquidMasses", StringComparison.Ordinal) &&
              liquidAmounts.IndexOf("FillWorldLiquidMasses", StringComparison.Ordinal) <
              liquidAmounts.IndexOf("specificSource.items", StringComparison.Ordinal) &&
              !liquidSideScreen.Contains("using System.Linq", StringComparison.Ordinal) &&
              liquidOptions.Contains("optionWorkspace", StringComparison.Ordinal) &&
              !liquidOptions.Contains("new List<LiquidOption>", StringComparison.Ordinal),
            "liquid side screen uses one indexed element aggregate and reusable live-value workspaces");
        string getNetworkItemAmount = ExtractMethod(logicDiy,
            "private float GetNetworkItemAmountKg()");
        string compileConditionTag = ExtractMethod(logicDiy,
            "private void CompileConditionItemTag()");
        string evaluateRuntimeNumber = ExtractMethod(logicDiy,
            "private float EvaluateRuntimeNumber(");
        check(getNetworkItemAmount.Contains("compiledConditionItemTag", StringComparison.Ordinal) &&
              !getNetworkItemAmount.Contains("new Tag", StringComparison.Ordinal) &&
              !getNetworkItemAmount.Contains("string.Equals", StringComparison.Ordinal) &&
              compileConditionTag.Contains("new Tag(ConditionItemKey)", StringComparison.Ordinal) &&
              evaluateRuntimeNumber.Contains("GetNetworkItemAmountKg()", StringComparison.Ordinal) &&
              !evaluateRuntimeNumber.Contains("ConditionItemKey", StringComparison.Ordinal),
            "Logic DIY compiles the selected material Tag outside the integer-node tick evaluator");
        string nodeMaterialQuery = ExtractMethod(logicDiy,
            "private float GetNetworkItemAmountKg(RuntimeBlueprintNode node)");
        string nodeMaterialSetter = ExtractMethod(logicDiy, "public string SelectedMaterialKey");
        check(nodeMaterialQuery.Contains("node.SelectedMaterialTag", StringComparison.Ordinal) &&
              !nodeMaterialQuery.Contains("new Tag", StringComparison.Ordinal) &&
              nodeMaterialSetter.Contains("new Tag(value)", StringComparison.Ordinal),
            "per-node material selections compile on assignment, including edits to an existing blueprint node");
        string sideScreen = Read(repositoryRoot,
            "StorageNetwork/Buildings/Production/UI/StorageNetworkOrderProductionCenterSideScreen.cs");
        string sideEnable = ExtractMethod(sideScreen, "protected override void OnCmpEnable()");
        string liveSideRefresh = ExtractMethod(sideScreen, "private void RefreshPrefabPanel()");
        string liveSideText = ExtractMethod(sideScreen, "private void SetLiveText(");
        check(sideEnable.Contains("base.OnCmpEnable()", StringComparison.Ordinal) &&
              sideEnable.Contains("forceLiveRefresh = true", StringComparison.Ordinal) &&
              liveSideRefresh.Contains("lastLocaleCode != localeCode", StringComparison.Ordinal) &&
              !liveSideRefresh.Contains("ForceMeshUpdate", StringComparison.Ordinal) &&
              liveSideText.Contains("label.text != text", StringComparison.Ordinal),
            "side screen refresh preserves engine lifecycle and locale sync while skipping unchanged text meshes");
        string enrollableStructure = ExtractMethod(enrollableSignature,
            "public static string Build(");
        check(!ContainsAny(enrollableStructure,
                "MassStored", "Capacity()", "IncludedInSceneNetwork", "Studyable", "Studied"),
            "enrollable structural signature excludes live mass, capacity, and status");
        string enrollableLiveUpdate = ExtractMethod(enrollableRows,
            "private void UpdateEnrollableBuildingRow(");
        check(!enrollableLiveUpdate.Contains("Destroy(row)", StringComparison.Ordinal) &&
              !enrollableLiveUpdate.Contains("CreateEnrollableBuildingRow", StringComparison.Ordinal),
            "enrollment status updates its stable row in place");
        string productionStructure = ExtractMethod(productionSignature,
            "public static string BuildProduction(");
        check(!ContainsAny(productionStructure, "LastStatus", "LastOutputStatus"),
            "production settings structural signature excludes live status text");

        string trackingStructure = ExtractMethod(trackingRules,
            "public static string BuildCardSignature(");
        check(!ContainsAny(trackingStructure,
                "RequestedAmount", "OrderCount", "AbnormalReason", "QueueAssignments"),
            "tracking card structural signature excludes live amounts, details, and assignments");
        string trackingLive = ExtractMethod(trackingView,
            "private void UpdateTrackingCardLive(TrackingCardLiveView view");
        check(trackingLive.Contains("view.Summary", StringComparison.Ordinal) &&
              trackingLive.Contains("view.Amount", StringComparison.Ordinal) &&
              trackingLive.Contains("view.Detail", StringComparison.Ordinal),
            "tracking card updates summary, amount, and detail through stable live bindings");
    }

    private static void VerifyMainThreadOptimization(string root, Action<bool, string> check)
    {
        string category = Read(root, "StorageNetwork/UI/Panel/CategorySummary/StorageNetworkPanel.CategorySummary.cs");
        string categoryRebuild = ExtractMethod(category, "private void RebuildCategorySummaryValues(");
        check(!categoryRebuild.Contains("categorySummaryIndexedTotals.Clear()", StringComparison.Ordinal),
            "category refresh preserves unchanged material totals in its incremental destination");
        string registry = Read(root, "StorageNetwork/Core/StorageSceneRegistry.cs");
        string strictSeed = ExtractMethod(registry, "public static void EnsureSceneSeeded()");
        string advanceSeed = ExtractMethod(registry, "internal static bool AdvanceSceneSeed(");
        check(strictSeed.Contains("finishSynchronously: true", StringComparison.Ordinal) &&
              advanceSeed.Contains("processed < 32", StringComparison.Ordinal) &&
              advanceSeed.Contains("Elapsed.TotalMilliseconds < 0.75d", StringComparison.Ordinal) &&
              advanceSeed.Contains("sceneSeedWork.Dispose()", StringComparison.Ordinal),
            "scene warmup is bounded per frame while strict readers finish the same directory synchronously");
        string panel = Read(root, "StorageNetwork/UI/Panel/StorageNetworkPanel.cs");
        string refresh = ExtractMethod(panel, "private void RefreshStoragePanel(");
        check(ContainsInOrder(refresh, "!StorageSceneRegistry.IsSceneReady", "return;", "RefreshStoragePanelMain(mode)") &&
              panel.Contains("StorageSceneRegistry.AdvanceSceneSeed()", StringComparison.Ordinal),
            "main panel waits for complete scene registration before exposing inventory");
        string frameBuild = Read(root, "StorageNetwork/UI/Panel/StorageNetworkPanel.BuildQueue.cs");
        check(frameBuild.Contains("mainRowBuildQueue.Run(2, 0.75d)", StringComparison.Ordinal) &&
              frameBuild.Contains("owner.gameObject.activeInHierarchy", StringComparison.Ordinal) &&
              frameBuild.Contains("section.Aggregates.ContainsKey(itemKey)", StringComparison.Ordinal),
            "deferred rows have frame budgets and lifetime/visibility checks");
        foreach (string kind in new[] { "Solid", "Liquid", "Gas" })
        {
            foreach (bool input in new[] { false, true })
            {
                string direction = input ? "Input" : "Output";
                string component = "StorageNetwork" + kind + direction + "Port" + (input ? "Ingress" : "Egress");
                string source = Read(root, "StorageNetwork/Buildings/Ports/" + kind + direction + "/Components/" + component + ".cs");
                check(ContainsInOrder(source, "requestWake.ShouldAttempt", "RecordPortRequestAttempt", "requestWake.RecordResult") &&
                      source.Contains("requestWake.Dispose()", StringComparison.Ordinal),
                    component + " gates failed transfers before request counters and removes its owner subscription");
            }
        }
        string index = Read(root, "StorageNetwork/Services/Inventory/StorageNetworkContentIndexService.cs");
        string wakeVersion = ExtractMethod(index, "internal static long GetRequestWakeVersion(");
        check(!wakeVersion.Contains("EnsureFresh(", StringComparison.Ordinal) &&
              wakeVersion.Contains("UnknownRequestVersion", StringComparison.Ordinal) &&
              wakeVersion.Contains("bucket.Version", StringComparison.Ordinal),
            "sleeping requests inspect scoped material generations without forcing native refreshes");
    }

    public static string FindRepositoryRoot()
    {
        string configured = Environment.GetEnvironmentVariable("STORAGE_NETWORK_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(
                Path.Combine(configured, "StorageNetwork")))
        {
            return Path.GetFullPath(configured);
        }

        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "StorageNetwork")) &&
                Directory.Exists(Path.Combine(directory.FullName, "StorageNetwork.Tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root. Set STORAGE_NETWORK_REPOSITORY_ROOT.");
    }

    private static string Read(string root, string relativePath)
    {
        string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Required source contract file is missing.", path);
        }

        return File.ReadAllText(path);
    }

    private static string ExtractMethod(string source, string marker)
    {
        int markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return string.Empty;
        }

        int brace = source.IndexOf('{', markerIndex);
        if (brace < 0)
        {
            return string.Empty;
        }

        int depth = 0;
        for (int index = brace; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}' && --depth == 0)
            {
                return source.Substring(markerIndex, index - markerIndex + 1);
            }
        }

        return string.Empty;
    }

    private static string ExtractType(string source, string marker)
    {
        return ExtractMethod(source, marker);
    }

    private static bool ContainsInOrder(string source, params string[] values)
    {
        int position = 0;
        foreach (string value in values)
        {
            position = source.IndexOf(value, position, StringComparison.Ordinal);
            if (position < 0)
            {
                return false;
            }

            position += value.Length;
        }

        return true;
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        foreach (string value in values)
        {
            if (source.IndexOf(value, StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int position = 0;
        while (!string.IsNullOrEmpty(value) &&
               (position = source.IndexOf(value, position, StringComparison.Ordinal)) >= 0)
        {
            count++;
            position += value.Length;
        }

        return count;
    }

    private static bool CacheKeyTracksExclusion(string source)
    {
        string cacheKey = ExtractType(source, "private readonly struct OutputTargetCacheKey");
        return cacheKey.Contains("sourceIsExcluded", StringComparison.OrdinalIgnoreCase) ||
               cacheKey.Contains("excludedSourceStorageId", StringComparison.OrdinalIgnoreCase);
    }
}
