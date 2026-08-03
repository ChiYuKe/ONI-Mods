using System;
using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    /// <summary>
    /// Derived, non-serialized view of native Storage contents.
    /// Storage.items remains authoritative; this service only refreshes records
    /// whose Storage.OnStorageChange event (or topology invalidation) marked them dirty.
    /// </summary>
    internal static class StorageNetworkContentIndexService
    {
        private static readonly Dictionary<Storage, StorageContentRecord> Records =
            new Dictionary<Storage, StorageContentRecord>();
        private static readonly Dictionary<int, WorldContentState> Worlds =
            new Dictionary<int, WorldContentState>();
        private static readonly HashSet<StorageContentRecord> DirtyRecords =
            new HashSet<StorageContentRecord>();
        private static readonly List<StorageContentRecord> DirtyWorkspace =
            new List<StorageContentRecord>();
        private static readonly HashSet<StorageContentRecord> ItemDetailDirtyRecords =
            new HashSet<StorageContentRecord>();
        private static readonly List<StorageContentRecord> ItemDetailWorkspace =
            new List<StorageContentRecord>();
        private static readonly HashSet<Storage> RegistryStorages =
            new HashSet<Storage>();
        private static readonly List<Storage> RemovalWorkspace =
            new List<Storage>();
        private static readonly Dictionary<StorageContentRecord, float> SourceMergeAmounts =
            new Dictionary<StorageContentRecord, float>();
        private static readonly List<KeyValuePair<StorageContentRecord, float>> SourceMergeWorkspace =
            new List<KeyValuePair<StorageContentRecord, float>>();
        private static readonly List<Tag> PendingSourceTags = new List<Tag>();
        private static readonly List<Storage> ShadowSourceWorkspace = new List<Storage>();
        private static readonly StableStorageComparer ProductionSourceComparer =
            new StableStorageComparer();

        [ThreadStatic]
        private static TransferTransactionContext transferTransaction;

        private static int observedRegistryVersion = -1;
        private static int observedCapabilityVersion = -1;
        private static int contentVersion;
        private static int changeVersion;
        private static bool synchronizingRegistry;
        private static bool flushingDirtyRecords;

        public static int Version => contentVersion;
        public static int ChangeVersion => changeVersion;

        /// <summary>
        /// Builds a stable mutation fingerprint for a caller-selected Storage set
        /// without flushing unrelated dirty records. UI panels use this to avoid
        /// rebuilding an expanded server/category when a different server changed.
        /// </summary>
        internal static bool TryGetStorageDisplayVersion(
            IReadOnlyList<Storage> storages,
            out int version)
        {
            version = 17;
            if (storages == null)
            {
                return true;
            }

            EnsureRegistrySynchronized();
            unchecked
            {
                version = (version * 397) ^ storages.Count;
                for (int index = 0; index < storages.Count; index++)
                {
                    Storage storage = storages[index];
                    if (storage == null)
                    {
                        version = (version * 397) ^ index;
                        continue;
                    }

                    if (!Records.TryGetValue(storage, out StorageContentRecord record))
                    {
                        Register(storage);
                        Records.TryGetValue(storage, out record);
                    }

                    if (record == null)
                    {
                        version = 0;
                        return false;
                    }

                    version = (version * 397) ^ record.InstanceId;
                    version = (version * 397) ^ record.DisplayVersion;
                    if (DirtyRecords.Contains(record))
                    {
                        // A same-frame stale UI read may deliberately leave this
                        // record dirty. Change the scoped fingerprint on the next
                        // frame so that consumer performs the required catch-up
                        // refresh even when no further mutation arrives.
                        version = (version * 397) ^
                                  (record.DirtyFrame == Time.frameCount ? 1 : 2);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Starts an ambient transfer transaction on the current simulation thread.
        /// The index is made strict once up front so every pending delta has a stable
        /// baseline. Native OnStorageChange callbacks are then folded into the
        /// transaction instead of repeatedly rebuilding the same Storage record.
        /// </summary>
        internal static void BeginTransferTransaction()
        {
            TransferTransactionContext transaction = transferTransaction ??
                (transferTransaction = new TransferTransactionContext());
            if (transaction.Depth == 0)
            {
                EnsureFresh(false);
                transaction.Reset();
            }

            transaction.Depth++;
        }

        /// <summary>
        /// Commits the outermost transfer transaction. Each touched Storage is
        /// refreshed from native Storage.items at most once, which also makes the
        /// final snapshot authoritative if an engine transfer merged or split an
        /// object differently from the pending delta.
        /// </summary>
        internal static void EndTransferTransaction()
        {
            TransferTransactionContext transaction = transferTransaction;
            if (transaction == null || transaction.Depth <= 0)
            {
                return;
            }

            transaction.Depth--;
            if (transaction.Depth > 0)
            {
                return;
            }

            try
            {
                CommitTransferTransaction(transaction);
            }
            finally
            {
                transaction.Reset();
            }
        }

        /// <summary>
        /// Explicitly records both sides of a successful native transfer. The
        /// amount delta is expressed in Pickupable units (the same value indexed
        /// for tags), while massKg drives RemainingCapacity.
        /// </summary>
        internal static void RecordTransferMutation(
            Storage source,
            Storage target,
            StorageItemUtility.StorageMatchTags matchTags,
            Tag stateTag,
            float amount,
            float massKg)
        {
            TransferTransactionContext transaction = transferTransaction;
            if (transaction == null || transaction.Depth <= 0)
            {
                Invalidate(source, target);
                return;
            }

            float movedAmount = Mathf.Max(0f, amount);
            float movedMassKg = Mathf.Max(0f, massKg);
            transaction.RecordTransfer(
                source,
                target,
                matchTags,
                stateTag,
                movedAmount,
                movedMassKg);
            if (!ReferenceEquals(source, null))
            {
                StorageTargetSelector.NotifyTransferMutation(
                    source,
                    matchTags,
                    -movedAmount,
                    -movedMassKg);
            }

            if (!ReferenceEquals(target, null))
            {
                StorageTargetSelector.NotifyTransferMutation(
                    target,
                    matchTags,
                    movedAmount,
                    movedMassKg);
            }
        }

        /// <summary>
        /// Marks a Storage as explicitly touched when the caller cannot describe a
        /// safe delta. Queries during the transaction fall back to native values for
        /// that Storage, and the final commit still refreshes it only once.
        /// </summary>
        internal static void TouchTransferStorage(Storage storage)
        {
            TransferTransactionContext transaction = transferTransaction;
            if (transaction == null || transaction.Depth <= 0)
            {
                Invalidate(storage);
                return;
            }

            transaction.ForceNativeRead(storage);
            StorageTargetSelector.InvalidateOutputTargetCache();
        }

        public static void Register(Storage storage)
        {
            if (storage == null || storage.gameObject == null || Records.ContainsKey(storage))
            {
                return;
            }

            if (!StorageNetworkMembership.IsCollectableStorage(storage) ||
                !StorageNetworkStorageRules.IsServerStorage(storage))
            {
                return;
            }

            int worldId = StorageTargetSelector.GetObjectWorldId(storage.gameObject);
            StorageContentRecord record = new StorageContentRecord(storage, worldId);
            record.ChangeHandler = _ => Invalidate(storage);
            storage.OnStorageChange += record.ChangeHandler;
            Records.Add(storage, record);
            GetOrCreateWorld(worldId).Records.Add(record);
            MarkDirty(record);
        }

        internal static void AcceptRegistryVersions(
            int registryVersion,
            int capabilityVersion)
        {
            observedRegistryVersion = registryVersion;
            observedCapabilityVersion = capabilityVersion;
        }

        public static void Unregister(Storage storage)
        {
            if (ReferenceEquals(storage, null) ||
                !Records.TryGetValue(storage, out StorageContentRecord record))
            {
                return;
            }

            if (storage != null && record.ChangeHandler != null)
            {
                storage.OnStorageChange -= record.ChangeHandler;
            }

            RemoveRecordContributions(record);
            if (Worlds.TryGetValue(record.WorldId, out WorldContentState world))
            {
                world.Records.Remove(record);
                if (world.Records.Count == 0)
                {
                    Worlds.Remove(record.WorldId);
                }
            }

            DirtyRecords.Remove(record);
            ItemDetailDirtyRecords.Remove(record);
            Records.Remove(storage);
            unchecked
            {
                contentVersion++;
                changeVersion++;
            }
        }

        public static void Invalidate(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            TransferTransactionContext transaction = transferTransaction;
            if (transaction != null && transaction.Depth > 0)
            {
                // Storage events can fire several times for one logical move. Keep
                // the record clean until the explicit transfer delta arrives, then
                // reconcile once at outermost commit.
                transaction.Touch(storage, requireNativeRead: true);
                return;
            }

            StorageTargetSelector.InvalidateOutputTargetCache();

            if (!Records.TryGetValue(storage, out StorageContentRecord record))
            {
                Register(storage);
                Records.TryGetValue(storage, out record);
            }

            if (record != null)
            {
                MarkDirty(record);
            }
        }

        public static void Invalidate(Storage first, Storage second)
        {
            Invalidate(first);
            if (second != first)
            {
                Invalidate(second);
            }
        }

        public static void InvalidateAll()
        {
            observedRegistryVersion = -1;
            observedCapabilityVersion = -1;
            TransferTransactionContext transaction = transferTransaction;
            if (transaction != null && transaction.Depth > 0)
            {
                foreach (StorageContentRecord record in Records.Values)
                {
                    // A world/global invalidation is not a provisional
                    // OnStorageChange callback. It represents an unknown mutation
                    // (or a shadow mismatch), so an explicit transfer delta must
                    // not be allowed to clear the native rebuild requirement.
                    transaction.ForceNativeRead(record.Storage);
                }

                StorageTargetSelector.InvalidateOutputTargetCache();
                return;
            }

            StorageTargetSelector.InvalidateOutputTargetCache();

            foreach (StorageContentRecord record in Records.Values)
            {
                MarkDirty(record);
            }
        }

        internal static void InvalidateWorld(int worldId)
        {
            if (!Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                return;
            }

            TransferTransactionContext transaction = transferTransaction;
            if (transaction != null && transaction.Depth > 0)
            {
                foreach (StorageContentRecord record in world.Records)
                {
                    transaction.ForceNativeRead(record.Storage);
                }

                StorageTargetSelector.InvalidateOutputTargetCache();
                return;
            }

            StorageTargetSelector.InvalidateOutputTargetCache();

            foreach (StorageContentRecord record in world.Records)
            {
                MarkDirty(record);
            }
        }

        public static void ResetRuntimeState()
        {
            foreach (StorageContentRecord record in Records.Values)
            {
                if (record.Storage != null && record.ChangeHandler != null)
                {
                    record.Storage.OnStorageChange -= record.ChangeHandler;
                }
            }

            Records.Clear();
            Worlds.Clear();
            DirtyRecords.Clear();
            DirtyWorkspace.Clear();
            ItemDetailDirtyRecords.Clear();
            ItemDetailWorkspace.Clear();
            RegistryStorages.Clear();
            RemovalWorkspace.Clear();
            SourceMergeAmounts.Clear();
            SourceMergeWorkspace.Clear();
            PendingSourceTags.Clear();
            ShadowSourceWorkspace.Clear();
            observedRegistryVersion = -1;
            observedCapabilityVersion = -1;
            contentVersion = 0;
            changeVersion = 0;
            synchronizingRegistry = false;
            flushingDirtyRecords = false;
            transferTransaction?.Reset();
            StorageNetworkShadowValidationService.ResetRuntimeState();
        }

        public static float GetAmount(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag[] forbiddenTags,
            bool allowStaleContent)
        {
            if (worldId < 0 || tag == Tag.Invalid || !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0f;
            }

            EnsureFresh(allowStaleContent);
            EnsureTransactionNativeBaseline(worldId, includeRelatedWorlds);
            float amount;
            if (forbiddenTags == null || forbiddenTags.Length == 0)
            {
                amount = SumWorldAmount(worldId, includeRelatedWorlds, tag);
                TransferTransactionContext transaction = transferTransaction;
                if (!StorageNetworkPerformanceMode.LegacyFullScanEnabled &&
                    transaction != null &&
                    transaction.Depth > 0)
                {
                    amount = Mathf.Max(
                        0f,
                        amount + transaction.GetInventoryAmountDelta(
                            worldId,
                            IncludesAllWorlds(includeRelatedWorlds),
                            tag));
                }
            }
            else
            {
                EnsureItemDetailsFresh(worldId, includeRelatedWorlds);
                amount = 0f;
                if (IncludesAllWorlds(includeRelatedWorlds))
                {
                    foreach (WorldContentState world in Worlds.Values)
                    {
                        amount += GetFilteredAmount(world, tag, forbiddenTags);
                    }
                }
                else if (Worlds.TryGetValue(worldId, out WorldContentState world))
                {
                    amount = GetFilteredAmount(world, tag, forbiddenTags);
                }
            }

            return ValidateAmount(
                worldId,
                includeRelatedWorlds,
                tag,
                forbiddenTags,
                amount);
        }

        public static StorageNetworkInventoryMetrics GetMetrics(
            int worldId,
            bool includeRelatedWorlds,
            bool allowStaleContent)
        {
            if (worldId < 0 || !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return default;
            }

            EnsureFresh(allowStaleContent);
            EnsureTransactionNativeBaseline(worldId, includeRelatedWorlds);
            float storedKg = 0f;
            float capacityKg = 0f;
            if (IncludesAllWorlds(includeRelatedWorlds))
            {
                foreach (WorldContentState world in Worlds.Values)
                {
                    storedKg += world.TotalStoredKg;
                    capacityKg += world.TotalCapacityKg;
                }
            }
            else if (Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                storedKg = world.TotalStoredKg;
                capacityKg = world.TotalCapacityKg;
            }

            TransferTransactionContext transaction = transferTransaction;
            if (!StorageNetworkPerformanceMode.LegacyFullScanEnabled &&
                transaction != null &&
                transaction.Depth > 0)
            {
                storedKg = Mathf.Max(
                    0f,
                    storedKg + transaction.GetCapacityMassDelta(
                        worldId,
                        IncludesAllWorlds(includeRelatedWorlds)));
            }

            StorageNetworkInventoryMetrics metrics =
                new StorageNetworkInventoryMetrics(true, storedKg, capacityKg);
            return ValidateMetrics(worldId, includeRelatedWorlds, metrics);
        }

        public static bool HasAnyAmount(
            int worldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> tags)
        {
            if (worldId < 0 || tags == null || !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return false;
            }

            EnsureFresh(false);
            EnsureTransactionNativeBaseline(worldId, includeRelatedWorlds);
            foreach (Tag tag in tags)
            {
                if (tag == Tag.Invalid)
                {
                    continue;
                }

                float amount = SumWorldAmount(worldId, includeRelatedWorlds, tag);
                TransferTransactionContext transaction = transferTransaction;
                if (!StorageNetworkPerformanceMode.LegacyFullScanEnabled &&
                    transaction != null &&
                    transaction.Depth > 0)
                {
                    amount += transaction.GetInventoryAmountDelta(
                        worldId,
                        IncludesAllWorlds(includeRelatedWorlds),
                        tag);
                }

                if (amount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetCountWithAdditionalTag(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag additionalTag)
        {
            if (worldId < 0 || tag == Tag.Invalid || !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0;
            }

            EnsureFresh(false);
            EnsureItemDetailsFresh(worldId, includeRelatedWorlds);
            int count = 0;
            if (IncludesAllWorlds(includeRelatedWorlds))
            {
                foreach (WorldContentState world in Worlds.Values)
                {
                    count += GetCountWithAdditionalTag(world, tag, additionalTag);
                }
            }
            else if (Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                count = GetCountWithAdditionalTag(world, tag, additionalTag);
            }

            return count;
        }

        public static bool HasPlantableSeed(
            int worldId,
            bool includeRelatedWorlds,
            Tag seedTag,
            Tag additionalTag)
        {
            if (worldId < 0 || seedTag == Tag.Invalid || !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return false;
            }

            EnsureFresh(false);
            EnsureItemDetailsFresh(worldId, includeRelatedWorlds);
            if (IncludesAllWorlds(includeRelatedWorlds))
            {
                foreach (WorldContentState world in Worlds.Values)
                {
                    if (WorldHasPlantableSeed(world, seedTag, additionalTag))
                    {
                        return true;
                    }
                }

                return false;
            }

            return Worlds.TryGetValue(worldId, out WorldContentState localWorld) &&
                   WorldHasPlantableSeed(localWorld, seedTag, additionalTag);
        }

        public static float GetEdibleCalories(
            int worldId,
            bool includeRelatedWorlds,
            string foodId,
            Dictionary<string, float> unitsById)
        {
            if (worldId < 0 || !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0f;
            }

            EnsureFresh(false);
            EnsureItemDetailsFresh(worldId, includeRelatedWorlds);
            float calories = 0f;
            if (IncludesAllWorlds(includeRelatedWorlds))
            {
                foreach (WorldContentState world in Worlds.Values)
                {
                    calories += GetEdibleCalories(world, foodId, unitsById);
                }
            }
            else if (Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                calories = GetEdibleCalories(world, foodId, unitsById);
            }

            return calories;
        }

        public static void FillSourceStorages(
            int worldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            Storage specificSource,
            List<Storage> result,
            bool allowStaleContent)
        {
            if (result == null)
            {
                return;
            }

            result.Clear();
            if (worldId < 0 || wantedTags == null || !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return;
            }

            EnsureFresh(allowStaleContent);
            if (specificSource != null)
            {
                if (TryGetUsableSource(
                    specificSource,
                    worldId,
                    includeRelatedWorlds,
                    wantedTags,
                    excludedStorages,
                    out _))
                {
                    result.Add(specificSource);
                }

                ValidateSources(
                    worldId,
                    includeRelatedWorlds,
                    wantedTags,
                    excludedStorages,
                    specificSource,
                    result);
                return;
            }

            TransferTransactionContext transaction = transferTransaction;
            if (!StorageNetworkPerformanceMode.LegacyFullScanEnabled &&
                transaction != null &&
                transaction.Depth > 0 &&
                transaction.HasPendingChanges)
            {
                FillPendingTransferSources(
                    worldId,
                    includeRelatedWorlds,
                    wantedTags,
                    excludedStorages,
                    result);
                ValidateSources(
                    worldId,
                    includeRelatedWorlds,
                    wantedTags,
                    excludedStorages,
                    null,
                    result);
                return;
            }

            bool hasTag = TryGetSingleTag(wantedTags, out Tag singleTag, out bool multipleTags);
            if (!hasTag)
            {
                ValidateSources(
                    worldId,
                    includeRelatedWorlds,
                    wantedTags,
                    excludedStorages,
                    null,
                    result);
                return;
            }

            if (!multipleTags && !IncludesAllWorlds(includeRelatedWorlds))
            {
                if (Worlds.TryGetValue(worldId, out WorldContentState world) &&
                    world.SourceBuckets.TryGetValue(singleTag, out SourceBucket bucket))
                {
                    bucket.Fill(excludedStorages, result);
                }

                ValidateSources(
                    worldId,
                    includeRelatedWorlds,
                    wantedTags,
                    excludedStorages,
                    null,
                    result);
                return;
            }

            SourceMergeAmounts.Clear();
            MergeSources(worldId, includeRelatedWorlds, wantedTags, excludedStorages);
            SourceMergeWorkspace.Clear();
            foreach (KeyValuePair<StorageContentRecord, float> pair in SourceMergeAmounts)
            {
                SourceMergeWorkspace.Add(pair);
            }

            SourceMergeWorkspace.Sort(CompareSources);
            foreach (KeyValuePair<StorageContentRecord, float> pair in SourceMergeWorkspace)
            {
                result.Add(pair.Key.Storage);
            }

            ValidateSources(
                worldId,
                includeRelatedWorlds,
                wantedTags,
                excludedStorages,
                null,
                result);
        }

        public static float GetStorageAmount(Storage storage, Tag tag, bool allowStaleContent = false)
        {
            if (storage == null || tag == Tag.Invalid)
            {
                return 0f;
            }

            EnsureFresh(allowStaleContent);
            TransferTransactionContext transaction = transferTransaction;
            bool usePendingTransferView = !StorageNetworkPerformanceMode.LegacyFullScanEnabled;
            if (usePendingTransferView &&
                transaction != null &&
                transaction.Depth > 0 &&
                transaction.RequiresNativeRead(storage))
            {
                return StorageNetworkContentShadowReader.GetStorageAmount(storage, tag);
            }

            if (!Records.TryGetValue(storage, out StorageContentRecord record))
            {
                Register(storage);
                EnsureFresh(false);
                Records.TryGetValue(storage, out record);
            }

            if (usePendingTransferView &&
                transaction != null &&
                transaction.Depth > 0 &&
                transaction.RequiresNativeRead(storage))
            {
                return StorageNetworkContentShadowReader.GetStorageAmount(storage, tag);
            }

            float indexedAmount =
                record != null && record.SourceAmounts.TryGetValue(tag, out float amount)
                    ? amount
                    : 0f;
            if (record != null &&
                usePendingTransferView &&
                transaction != null &&
                transaction.Depth > 0 &&
                transaction.TryGetAmountDelta(storage, tag, out float pendingAmount))
            {
                indexedAmount = Mathf.Max(0f, indexedAmount + pendingAmount);
            }

            int worldId = StorageTargetSelector.GetObjectWorldId(storage.gameObject);
            if (!StorageNetworkShadowValidationService.ShouldValidate(
                    StorageNetworkShadowArea.StorageAmount,
                    worldId,
                    contentVersion))
            {
                return indexedAmount;
            }

            float nativeAmount = StorageNetworkContentShadowReader.GetStorageAmount(storage, tag);
            if (StorageNetworkShadowValidationService.ApproximatelyEqual(indexedAmount, nativeAmount))
            {
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.StorageAmount,
                    worldId,
                    contentVersion);
                return indexedAmount;
            }

            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.StorageAmount,
                worldId,
                contentVersion,
                tag.GetHash(),
                $"tag={tag}, indexed={indexedAmount:0.###}, native={nativeAmount:0.###}");
            ForceNativeInvalidation(storage);
            return nativeAmount;
        }

        /// <summary>
        /// Reads the derived mass/capacity snapshot for one registered server.
        /// UI callers use the stale-content allowance so same-frame storage events
        /// are coalesced, while the next frame still observes the native contents.
        /// </summary>
        internal static bool TryGetStorageMetrics(
            Storage storage,
            bool allowStaleContent,
            out float storedKg,
            out float capacityKg)
        {
            storedKg = 0f;
            capacityKg = 0f;
            if (storage == null)
            {
                return false;
            }

            EnsureFresh(allowStaleContent);
            if (!Records.TryGetValue(storage, out StorageContentRecord record))
            {
                Register(storage);
                EnsureFresh(false);
                Records.TryGetValue(storage, out record);
            }

            TransferTransactionContext transaction = transferTransaction;
            bool usePendingTransferView = !StorageNetworkPerformanceMode.LegacyFullScanEnabled;
            if (record == null ||
                (usePendingTransferView &&
                 transaction != null &&
                 transaction.Depth > 0 &&
                 transaction.RequiresNativeRead(storage)))
            {
                if (!StorageNetworkStorageRules.IsConnectedNetworkStorage(storage))
                {
                    return record != null;
                }

                storedKg = storage.MassStored();
                capacityKg = storage.Capacity();
                return true;
            }

            storedKg = record.StoredKg;
            capacityKg = record.CapacityKg;
            if (usePendingTransferView &&
                transaction != null &&
                transaction.Depth > 0 &&
                transaction.TryGetMassDelta(storage, out float pendingMassKg))
            {
                storedKg = Mathf.Max(
                    0f,
                    Mathf.Round((storedKg + pendingMassKg) * 1000f) / 1000f);
            }

            return true;
        }

        public static float GetRemainingCapacity(Storage storage, bool allowStaleContent = false)
        {
            if (storage == null)
            {
                return 0f;
            }

            EnsureFresh(allowStaleContent);
            TransferTransactionContext transaction = transferTransaction;
            bool usePendingTransferView = !StorageNetworkPerformanceMode.LegacyFullScanEnabled;
            if (usePendingTransferView &&
                transaction != null &&
                transaction.Depth > 0 &&
                transaction.RequiresNativeRead(storage))
            {
                return Mathf.Max(0f, storage.RemainingCapacity());
            }

            if (!Records.TryGetValue(storage, out StorageContentRecord record))
            {
                Register(storage);
                EnsureFresh(false);
                Records.TryGetValue(storage, out record);
            }

            if (usePendingTransferView &&
                transaction != null &&
                transaction.Depth > 0 &&
                transaction.RequiresNativeRead(storage))
            {
                return Mathf.Max(0f, storage.RemainingCapacity());
            }

            float indexedCapacity = record != null
                ? Mathf.Max(0f, record.CapacityKg - record.StoredKg)
                : Mathf.Max(0f, storage.RemainingCapacity());
            if (record != null &&
                usePendingTransferView &&
                transaction != null &&
                transaction.Depth > 0 &&
                transaction.TryGetMassDelta(storage, out float pendingMassKg))
            {
                float pendingStoredKg =
                    Mathf.Round((record.StoredKg + pendingMassKg) * 1000f) / 1000f;
                indexedCapacity = Mathf.Max(0f, record.CapacityKg - pendingStoredKg);
            }

            int worldId = StorageTargetSelector.GetObjectWorldId(storage.gameObject);
            if (!StorageNetworkShadowValidationService.ShouldValidate(
                    StorageNetworkShadowArea.TargetCapacity,
                    worldId,
                    contentVersion))
            {
                return indexedCapacity;
            }

            float nativeCapacity =
                StorageNetworkContentShadowReader.GetRemainingCapacity(storage);
            if (StorageNetworkShadowValidationService.ApproximatelyEqual(
                    indexedCapacity,
                    nativeCapacity))
            {
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.TargetCapacity,
                    worldId,
                    contentVersion);
                return indexedCapacity;
            }

            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.TargetCapacity,
                worldId,
                contentVersion,
                StorageItemUtility.GetStorageInstanceId(storage),
                $"storage={storage.name}, indexed={indexedCapacity:0.###}, " +
                $"native={nativeCapacity:0.###}");
            ForceNativeInvalidation(storage);
            return nativeCapacity;
        }

        public static void FillAmounts(
            int worldId,
            bool includeRelatedWorlds,
            Dictionary<Tag, float> destination,
            bool allowStaleContent = true)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();
            if (worldId < 0 || !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return;
            }

            EnsureFresh(allowStaleContent);
            EnsureTransactionNativeBaseline(worldId, includeRelatedWorlds);
            if (IncludesAllWorlds(includeRelatedWorlds))
            {
                foreach (WorldContentState world in Worlds.Values)
                {
                    MergeAmounts(world.AmountsByTag, destination);
                }
            }
            else if (Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                MergeAmounts(world.AmountsByTag, destination);
            }

            TransferTransactionContext transaction = transferTransaction;
            if (!StorageNetworkPerformanceMode.LegacyFullScanEnabled &&
                transaction != null &&
                transaction.Depth > 0)
            {
                transaction.ApplyInventoryAmountDeltas(
                    worldId,
                    IncludesAllWorlds(includeRelatedWorlds),
                    destination);
            }
        }

        /// <summary>
        /// Fills an exact per-prefab item view for a small, caller-selected set of
        /// storages. The expensive GameObject/component walk is paid only when a
        /// selected storage is dirty; unchanged UI refreshes merge the compact
        /// per-storage summaries kept by the content index.
        /// </summary>
        internal static bool TryFillStorageItemTotals(
            IReadOnlyList<Storage> storages,
            Dictionary<Tag, StorageNetworkIndexedItemTotal> destination,
            bool allowStaleContent,
            out float storedKg)
        {
            storedKg = 0f;
            if (destination == null)
            {
                return false;
            }

            destination.Clear();
            if (storages == null || storages.Count == 0)
            {
                return true;
            }

            EnsureFresh(allowStaleContent);
            int frame = Time.frameCount;
            for (int storageIndex = 0; storageIndex < storages.Count; storageIndex++)
            {
                Storage storage = storages[storageIndex];
                if (storage == null)
                {
                    continue;
                }

                if (!Records.TryGetValue(storage, out StorageContentRecord record))
                {
                    Register(storage);
                    EnsureFresh(false);
                    Records.TryGetValue(storage, out record);
                }

                if (record == null)
                {
                    destination.Clear();
                    storedKg = 0f;
                    return false;
                }

                if (record.HasUnindexedDisplayItems)
                {
                    destination.Clear();
                    storedKg = 0f;
                    return false;
                }

                if (record.ItemDetailsDirty &&
                    (!allowStaleContent || record.DirtyFrame != frame))
                {
                    DirtyRecords.Remove(record);
                    RefreshRecord(record);
                }

                storedKg += record.StoredKg;
                for (int itemIndex = 0; itemIndex < record.ItemTotals.Count; itemIndex++)
                {
                    StorageNetworkIndexedItemTotal item = record.ItemTotals[itemIndex];
                    if (destination.TryGetValue(item.KeyTag, out StorageNetworkIndexedItemTotal total))
                    {
                        total.Merge(item);
                        destination[item.KeyTag] = total;
                    }
                    else
                    {
                        destination.Add(item.KeyTag, item);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Copies the per-world liquid mass aggregate maintained alongside the
        /// content index. Dirty native storages are refreshed before the aggregate
        /// is exposed, so the side-screen query is O(dirty items + distinct
        /// elements) instead of walking every item in every server.
        /// </summary>
        internal static void FillWorldLiquidMasses(
            int worldId,
            Dictionary<SimHashes, float> destination)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();
            EnsureFresh(false);
            EnsureItemDetailsFresh(worldId, includeRelatedWorlds: false);
            if (!Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                return;
            }

            CopyLiquidMasses(world.ElementMasses, destination);
        }

        /// <summary>
        /// Copies liquid masses for one explicitly selected server. Returning
        /// false lets compatibility callers fall back to that one native Storage
        /// when a third-party storage is not represented by the runtime catalog.
        /// </summary>
        internal static bool TryFillStorageLiquidMasses(
            Storage storage,
            Dictionary<SimHashes, float> destination)
        {
            if (destination == null)
            {
                return false;
            }

            destination.Clear();
            if (storage == null)
            {
                return true;
            }

            EnsureFresh(false);
            if (!Records.TryGetValue(storage, out StorageContentRecord record))
            {
                Register(storage);
                EnsureFresh(false);
                Records.TryGetValue(storage, out record);
            }

            if (record == null)
            {
                return false;
            }

            if (record.ItemDetailsDirty)
            {
                DirtyRecords.Remove(record);
                RefreshRecord(record);
            }

            CopyLiquidMasses(record.ElementMasses, destination);
            return true;
        }

        private static void CopyLiquidMasses(
            Dictionary<SimHashes, float> source,
            Dictionary<SimHashes, float> destination)
        {
            foreach (KeyValuePair<SimHashes, float> pair in source)
            {
                if (pair.Value <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                Element element = ElementLoader.FindElementByHash(pair.Key);
                if (element != null && element.IsLiquid)
                {
                    destination[pair.Key] = pair.Value;
                }
            }
        }

        internal static void FillProductionSourceInventory(
            int worldId,
            bool includeRelatedWorlds,
            Dictionary<Tag, float> massByTag,
            List<Storage> sourceStorages,
            bool allowStaleContent = false)
        {
            massByTag?.Clear();
            sourceStorages?.Clear();
            if (massByTag == null ||
                sourceStorages == null ||
                worldId < 0 ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return;
            }

            EnsureFresh(allowStaleContent);
            EnsureTransactionNativeBaseline(worldId, includeRelatedWorlds);
            bool allWorlds = IncludesAllWorlds(includeRelatedWorlds);
            if (allWorlds)
            {
                foreach (KeyValuePair<int, WorldContentState> pair in Worlds)
                {
                    if (StorageSceneRegistry.HasOnlineCoreInWorld(pair.Key))
                    {
                        AddProductionSourceWorld(pair.Value, massByTag, sourceStorages);
                    }
                }
            }
            else if (Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                AddProductionSourceWorld(world, massByTag, sourceStorages);
            }

            sourceStorages.Sort(ProductionSourceComparer);

            TransferTransactionContext transaction = transferTransaction;
            if (!StorageNetworkPerformanceMode.LegacyFullScanEnabled &&
                transaction != null &&
                transaction.Depth > 0)
            {
                transaction.ApplySourceMassDeltas(worldId, allWorlds, massByTag);
            }
        }

        private static void EnsureFresh(bool allowStaleContent)
        {
            EnsureRegistrySynchronized();
            if (StorageNetworkPerformanceMode.LegacyFullScanEnabled)
            {
                foreach (StorageContentRecord record in Records.Values)
                {
                    MarkDirty(record);
                }

                allowStaleContent = false;
            }

            if (flushingDirtyRecords || DirtyRecords.Count == 0)
            {
                return;
            }

            flushingDirtyRecords = true;
            try
            {
                DirtyWorkspace.Clear();
                int frame = Time.frameCount;
                foreach (StorageContentRecord record in DirtyRecords)
                {
                    if (!allowStaleContent || !record.HasSnapshot || record.DirtyFrame != frame)
                    {
                        DirtyWorkspace.Add(record);
                    }
                }

                if (DirtyWorkspace.Count == 0)
                {
                    return;
                }

                foreach (StorageContentRecord record in DirtyWorkspace)
                {
                    DirtyRecords.Remove(record);
                }

                using (StorageNetworkFrameProfileTool.BeginWork(
                    StorageNetworkPerformanceArea.ContentIndex))
                {
                    foreach (StorageContentRecord record in DirtyWorkspace)
                    {
                        RefreshRecord(record);
                    }
                }

                StorageNetworkPerformanceCounters.RecordInventoryIndexRebuild();
            }
            finally
            {
                DirtyWorkspace.Clear();
                flushingDirtyRecords = false;
            }
        }

        private static void EnsureRegistrySynchronized()
        {
            if (synchronizingRegistry)
            {
                return;
            }

            StorageSceneRegistry.EnsureSceneSeeded();
            int registryVersion = StorageSceneRegistry.MembershipVersion;
            int capabilityVersion = StorageSceneRegistry.CapabilityVersion;
            if (registryVersion == observedRegistryVersion &&
                capabilityVersion == observedCapabilityVersion)
            {
                return;
            }

            // Capability-only changes (for example a TreeFilterable update) do not
            // alter indexed contents. Operational components explicitly invalidate
            // their own Storage, while conservative third-party scene invalidation
            // also advances MembershipVersion. Avoid turning every filter edit into
            // a full-network content refresh.
            if (registryVersion == observedRegistryVersion)
            {
                observedCapabilityVersion = capabilityVersion;
                return;
            }

            synchronizingRegistry = true;
            try
            {
                RegistryStorages.Clear();
                foreach (Storage storage in StorageSceneRegistry.GetStorages())
                {
                    if (!StorageSceneRegistry.IsLive(storage) ||
                        !StorageNetworkMembership.IsCollectableStorage(storage) ||
                        !StorageNetworkStorageRules.IsServerStorage(storage))
                    {
                        continue;
                    }

                    RegistryStorages.Add(storage);
                    Register(storage);
                    if (Records.TryGetValue(storage, out StorageContentRecord record))
                    {
                        MarkDirty(record);
                    }
                }

                RemovalWorkspace.Clear();
                foreach (Storage storage in Records.Keys)
                {
                    if (!RegistryStorages.Contains(storage))
                    {
                        RemovalWorkspace.Add(storage);
                    }
                }

                foreach (Storage storage in RemovalWorkspace)
                {
                    Unregister(storage);
                }

                observedRegistryVersion = registryVersion;
                observedCapabilityVersion = capabilityVersion;
            }
            finally
            {
                RegistryStorages.Clear();
                RemovalWorkspace.Clear();
                synchronizingRegistry = false;
            }
        }

        private static float ValidateAmount(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag[] forbiddenTags,
            float indexedAmount)
        {
            if (!StorageNetworkShadowValidationService.ShouldValidate(
                    StorageNetworkShadowArea.InventoryAmount,
                    worldId,
                    contentVersion))
            {
                return indexedAmount;
            }

            float nativeAmount = StorageNetworkContentShadowReader.GetAmount(
                worldId,
                includeRelatedWorlds,
                tag,
                forbiddenTags);
            if (StorageNetworkShadowValidationService.ApproximatelyEqual(
                    indexedAmount,
                    nativeAmount))
            {
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.InventoryAmount,
                    worldId,
                    contentVersion);
                return indexedAmount;
            }

            int signature = tag.GetHash();
            if (forbiddenTags != null)
            {
                unchecked
                {
                    foreach (Tag forbiddenTag in forbiddenTags)
                    {
                        signature = (signature * 397) ^ forbiddenTag.GetHash();
                    }
                }
            }

            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.InventoryAmount,
                worldId,
                contentVersion,
                signature,
                $"tag={tag}, indexed={indexedAmount:0.###}, native={nativeAmount:0.###}");
            if (includeRelatedWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                InvalidateAll();
            }
            else
            {
                InvalidateWorld(worldId);
            }
            return nativeAmount;
        }

        private static StorageNetworkInventoryMetrics ValidateMetrics(
            int worldId,
            bool includeRelatedWorlds,
            StorageNetworkInventoryMetrics indexed)
        {
            if (!StorageNetworkShadowValidationService.ShouldValidate(
                    StorageNetworkShadowArea.InventoryMetrics,
                    worldId,
                    contentVersion))
            {
                return indexed;
            }

            StorageNetworkInventoryMetrics native =
                StorageNetworkContentShadowReader.GetMetrics(worldId, includeRelatedWorlds);
            if (StorageNetworkShadowValidationService.ApproximatelyEqual(
                    indexed.TotalStoredKg,
                    native.TotalStoredKg) &&
                StorageNetworkShadowValidationService.ApproximatelyEqual(
                    indexed.TotalCapacityKg,
                    native.TotalCapacityKg))
            {
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.InventoryMetrics,
                    worldId,
                    contentVersion);
                return indexed;
            }

            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.InventoryMetrics,
                worldId,
                contentVersion,
                includeRelatedWorlds ? 1 : 0,
                $"indexed={indexed.TotalStoredKg:0.###}/{indexed.TotalCapacityKg:0.###}, " +
                $"native={native.TotalStoredKg:0.###}/{native.TotalCapacityKg:0.###}");
            if (includeRelatedWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                InvalidateAll();
            }
            else
            {
                InvalidateWorld(worldId);
            }
            return native;
        }

        private static void ValidateSources(
            int worldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            Storage specificSource,
            List<Storage> indexed)
        {
            if (!StorageNetworkShadowValidationService.ShouldValidate(
                    StorageNetworkShadowArea.SourceOrder,
                    worldId,
                    contentVersion))
            {
                return;
            }

            StorageNetworkContentShadowReader.FillSourceStorages(
                worldId,
                includeRelatedWorlds,
                wantedTags,
                excludedStorages,
                specificSource,
                ShadowSourceWorkspace);

            bool equal = indexed.Count == ShadowSourceWorkspace.Count;
            if (equal)
            {
                for (int index = 0; index < indexed.Count; index++)
                {
                    if (indexed[index] != ShadowSourceWorkspace[index])
                    {
                        equal = false;
                        break;
                    }
                }
            }

            if (equal)
            {
                ShadowSourceWorkspace.Clear();
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.SourceOrder,
                    worldId,
                    contentVersion);
                return;
            }

            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.SourceOrder,
                worldId,
                contentVersion,
                specificSource != null
                    ? StorageItemUtility.GetStorageInstanceId(specificSource)
                    : 0,
                $"indexedCount={indexed.Count}, nativeCount={ShadowSourceWorkspace.Count}");
            indexed.Clear();
            indexed.AddRange(ShadowSourceWorkspace);
            ShadowSourceWorkspace.Clear();
            if (includeRelatedWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                InvalidateAll();
            }
            else
            {
                InvalidateWorld(worldId);
            }
        }

        private static void RefreshRecord(StorageContentRecord record)
        {
            Storage storage = record.Storage;
            if (!StorageSceneRegistry.IsLive(storage) ||
                !StorageNetworkMembership.IsCollectableStorage(storage) ||
                !StorageNetworkStorageRules.IsServerStorage(storage))
            {
                Unregister(storage);
                return;
            }

            RemoveRecordContributions(record);

            int worldId = StorageTargetSelector.GetObjectWorldId(storage.gameObject);
            if (record.WorldId != worldId)
            {
                if (Worlds.TryGetValue(record.WorldId, out WorldContentState oldWorld))
                {
                    oldWorld.Records.Remove(record);
                }

                record.WorldId = worldId;
                GetOrCreateWorld(worldId).Records.Add(record);
            }

            record.Amounts.Clear();
            record.SourceAmounts.Clear();
            record.SourceMasses.Clear();
            record.Items.Clear();
            record.ItemTotals.Clear();
            record.ItemTotalIndexes.Clear();
            record.ElementMasses.Clear();
            record.HasUnindexedDisplayItems = false;
            record.StoredKg = 0f;
            record.CapacityKg = 0f;
            record.CountsTowardCapacity = false;
            record.SourceEligible = false;

            bool connected = StorageNetworkStorageRules.IsConnectedNetworkStorage(storage);
            if (connected && storage.items != null)
            {
                record.StoredKg = storage.MassStored();
                record.CapacityKg = storage.Capacity();
                record.CountsTowardCapacity =
                    StorageNetworkStorageRules.CountsTowardNetworkCapacity(storage);

                record.SourceEligible =
                    !StorageNetworkStorageRules.IsMinionStorage(storage) &&
                    !StorageNetworkStorageRules.IsProductionStorage(storage);

                foreach (GameObject itemObject in storage.items)
                {
                    AddItem(record, itemObject);
                }
            }

            record.HasSnapshot = true;
            record.ItemDetailsDirty = false;
            ItemDetailDirtyRecords.Remove(record);
            AddRecordContributions(record);
            unchecked
            {
                record.DisplayVersion++;
                contentVersion++;
            }
        }

        private static void CommitTransferTransaction(TransferTransactionContext transaction)
        {
            if (transaction == null || transaction.TouchedStorages.Count == 0)
            {
                return;
            }

            EnsureRegistrySynchronized();
            if (transaction.HasNativeReadRequirements)
            {
                StorageTargetSelector.InvalidateOutputTargetCache();
            }
            bool refreshedAny = false;
            using (StorageNetworkFrameProfileTool.BeginWork(
                StorageNetworkPerformanceArea.ContentIndex))
            {
                foreach (Storage storage in transaction.TouchedStorages)
                {
                    if (!Records.TryGetValue(storage, out StorageContentRecord record))
                    {
                        if (storage == null)
                        {
                            continue;
                        }

                        Register(storage);
                        Records.TryGetValue(storage, out record);
                    }

                    if (record == null)
                    {
                        continue;
                    }

                    // A record may already have been dirty before a non-querying
                    // transfer entry point. Its native contents now include every
                    // completed operation in this transaction, so one refresh is
                    // both sufficient and authoritative.
                    DirtyRecords.Remove(record);
                    if (transaction.RequiresNativeRead(storage))
                    {
                        RefreshRecord(record);
                    }
                    else
                    {
                        transaction.ApplyDeltas(record);
                    }
                    refreshedAny = true;
                }
            }

            if (!refreshedAny)
            {
                return;
            }

            unchecked
            {
                // One logical public transfer invalidates selection caches once,
                // regardless of item count or the number of native callbacks.
                changeVersion++;
            }

            StorageNetworkPerformanceCounters.RecordInventoryIndexRebuild();
        }

        private static void EnsureItemDetailsFresh(
            int worldId,
            bool includeRelatedWorlds)
        {
            EnsureTransactionNativeBaseline(
                worldId,
                includeRelatedWorlds,
                forceTouchedRecords: true);
            if (ItemDetailDirtyRecords.Count == 0)
            {
                return;
            }

            ItemDetailWorkspace.Clear();
            bool allWorlds = IncludesAllWorlds(includeRelatedWorlds);
            foreach (StorageContentRecord record in ItemDetailDirtyRecords)
            {
                if (allWorlds || record.WorldId == worldId)
                {
                    ItemDetailWorkspace.Add(record);
                }
            }

            foreach (StorageContentRecord record in ItemDetailWorkspace)
            {
                if (record != null)
                {
                    RefreshRecord(record);
                }
            }

            ItemDetailWorkspace.Clear();
        }

        private static void EnsureTransactionNativeBaseline(
            int worldId,
            bool includeRelatedWorlds,
            bool forceTouchedRecords = false)
        {
            TransferTransactionContext transaction = transferTransaction;
            if (transaction == null ||
                transaction.Depth <= 0 ||
                !forceTouchedRecords && !transaction.HasNativeReadRequirements)
            {
                return;
            }

            transaction.RequireNativeReadsForTouchedStorages();
            bool allWorlds = IncludesAllWorlds(includeRelatedWorlds);
            foreach (Storage storage in transaction.TouchedStorages)
            {
                if (!Records.TryGetValue(storage, out StorageContentRecord record) ||
                    !allWorlds && record.WorldId != worldId)
                {
                    continue;
                }

                DirtyRecords.Remove(record);
                RefreshRecord(record);
            }
        }

        private static void ForceNativeInvalidation(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            int worldId = Records.TryGetValue(storage, out StorageContentRecord record)
                ? record.WorldId
                : StorageTargetSelector.GetObjectWorldId(storage.gameObject);
            if (worldId >= 0)
            {
                // A sampled mismatch invalidates the complete derived world view,
                // including sibling records that may share the missed mutation.
                InvalidateWorld(worldId);
                return;
            }

            Invalidate(storage);
        }

        private static void AddItem(StorageContentRecord record, GameObject itemObject)
        {
            if (itemObject == null)
            {
                return;
            }

            PrimaryElement primaryElement = itemObject.GetComponent<PrimaryElement>();
            float massKg = primaryElement != null ? primaryElement.Mass : 0f;
            Pickupable pickupable = itemObject.GetComponent<Pickupable>();
            float amount = pickupable != null
                ? pickupable.TotalAmount
                : massKg;
            if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            StorageItemUtility.StorageMatchTags matchTags =
                StorageItemUtility.GetStorageMatchTagsNonAlloc(itemObject);
            KPrefabID prefabId = itemObject.GetComponent<KPrefabID>();
            Edible edible = itemObject.GetComponent<Edible>();
            record.Items.Add(new IndexedItem(matchTags, prefabId, amount, edible));
            AddIndexedItemTotal(record, itemObject, matchTags, primaryElement, massKg);
            if (primaryElement != null && massKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                AddAmount(record.ElementMasses, primaryElement.ElementID, massKg);
            }

            AddUniqueTag(record.Amounts, matchTags.PrefabIdTag, amount);
            AddUniqueTag(record.Amounts, matchTags.PrefabTag, amount, matchTags.PrefabIdTag);
            AddUniqueTag(
                record.Amounts,
                matchTags.ElementTag,
                amount,
                matchTags.PrefabIdTag,
                matchTags.PrefabTag);
            AddUniqueTag(
                record.Amounts,
                matchTags.TransferTag,
                amount,
                matchTags.PrefabIdTag,
                matchTags.PrefabTag,
                matchTags.ElementTag);

            AddUniqueTag(record.SourceAmounts, matchTags.PrefabIdTag, amount);
            AddUniqueTag(record.SourceAmounts, matchTags.PrefabTag, amount, matchTags.PrefabIdTag);
            AddUniqueTag(
                record.SourceAmounts,
                matchTags.ElementTag,
                amount,
                matchTags.PrefabIdTag,
                matchTags.PrefabTag);
            AddUniqueTag(
                record.SourceAmounts,
                matchTags.TransferTag,
                amount,
                matchTags.PrefabIdTag,
                matchTags.PrefabTag,
                matchTags.ElementTag);

            AddUniqueTag(record.SourceMasses, matchTags.TransferTag, massKg);
            AddUniqueTag(
                record.SourceMasses,
                matchTags.ElementTag,
                massKg,
                matchTags.TransferTag);

            Element element = primaryElement != null
                ? ElementLoader.FindElementByHash(primaryElement.ElementID)
                : null;
            if (element != null)
            {
                Tag stateTag = element.IsLiquid
                    ? GameTags.Liquid
                    : element.IsGas
                        ? GameTags.Gas
                        : GameTags.Solid;
                AddUniqueTag(
                    record.SourceAmounts,
                    stateTag,
                    amount,
                    matchTags.PrefabIdTag,
                    matchTags.PrefabTag,
                    matchTags.ElementTag,
                    matchTags.TransferTag);
            }
        }

        private static void AddIndexedItemTotal(
            StorageContentRecord record,
            GameObject itemObject,
            StorageItemUtility.StorageMatchTags matchTags,
            PrimaryElement primaryElement,
            float massKg)
        {
            Tag keyTag = matchTags.PrefabIdTag != Tag.Invalid
                ? matchTags.PrefabIdTag
                : matchTags.ElementTag != Tag.Invalid
                    ? matchTags.ElementTag
                    : matchTags.TransferTag != Tag.Invalid
                        ? matchTags.TransferTag
                        : matchTags.PrefabTag;
            if (keyTag == Tag.Invalid)
            {
                record.HasUnindexedDisplayItems = true;
                return;
            }

            if (record.ItemTotalIndexes.TryGetValue(keyTag, out int index))
            {
                StorageNetworkIndexedItemTotal total = record.ItemTotals[index];
                total.Add(itemObject, primaryElement, massKg);
                record.ItemTotals[index] = total;
                return;
            }

            StorageNetworkIndexedItemTotal created =
                new StorageNetworkIndexedItemTotal(keyTag);
            created.Add(itemObject, primaryElement, massKg);
            record.ItemTotalIndexes.Add(keyTag, record.ItemTotals.Count);
            record.ItemTotals.Add(created);
        }

        private static void AddRecordContributions(StorageContentRecord record)
        {
            WorldContentState world = GetOrCreateWorld(record.WorldId);
            if (record.CountsTowardCapacity)
            {
                world.TotalStoredKg += record.StoredKg;
                world.TotalCapacityKg += record.CapacityKg;
            }
            foreach (KeyValuePair<Tag, float> pair in record.Amounts)
            {
                AddAmount(world.AmountsByTag, pair.Key, pair.Value);
            }
            foreach (KeyValuePair<SimHashes, float> pair in record.ElementMasses)
            {
                AddAmount(world.ElementMasses, pair.Key, pair.Value);
            }

            if (!record.SourceEligible)
            {
                return;
            }

            foreach (KeyValuePair<Tag, float> pair in record.SourceAmounts)
            {
                world.GetOrCreateSourceBucket(pair.Key).Set(record, pair.Value);
            }

            foreach (KeyValuePair<Tag, float> pair in record.SourceMasses)
            {
                AddAmount(world.SourceMassesByTag, pair.Key, pair.Value);
            }
        }

        private static void RemoveRecordContributions(StorageContentRecord record)
        {
            if (!record.HasSnapshot || !Worlds.TryGetValue(record.WorldId, out WorldContentState world))
            {
                return;
            }

            if (record.CountsTowardCapacity)
            {
                world.TotalStoredKg = Mathf.Max(0f, world.TotalStoredKg - record.StoredKg);
                world.TotalCapacityKg = Mathf.Max(0f, world.TotalCapacityKg - record.CapacityKg);
            }
            foreach (KeyValuePair<Tag, float> pair in record.Amounts)
            {
                AddAmount(world.AmountsByTag, pair.Key, -pair.Value);
            }
            foreach (KeyValuePair<SimHashes, float> pair in record.ElementMasses)
            {
                AddAmount(world.ElementMasses, pair.Key, -pair.Value);
            }

            foreach (Tag tag in record.SourceAmounts.Keys)
            {
                if (world.SourceBuckets.TryGetValue(tag, out SourceBucket bucket))
                {
                    bucket.Remove(record);
                }
            }

            if (record.SourceEligible)
            {
                foreach (KeyValuePair<Tag, float> pair in record.SourceMasses)
                {
                    AddAmount(world.SourceMassesByTag, pair.Key, -pair.Value);
                }
            }
        }

        private static void MarkDirty(StorageContentRecord record)
        {
            if (record == null)
            {
                return;
            }

            TransferTransactionContext transaction = transferTransaction;
            if (transaction != null && transaction.Depth > 0)
            {
                transaction.Touch(record.Storage, requireNativeRead: true);
                return;
            }

            if (DirtyRecords.Add(record))
            {
                record.DirtyFrame = Time.frameCount;
            }

            unchecked
            {
                // This is a mutation generation, not merely a dirty-set generation.
                // Consumers such as production snapshots may refresh directly from
                // native Storage.items without removing this record from DirtyRecords.
                record.DisplayVersion++;
                changeVersion++;
            }
        }

        private static WorldContentState GetOrCreateWorld(int worldId)
        {
            if (!Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                world = new WorldContentState();
                Worlds.Add(worldId, world);
            }

            return world;
        }

        private static float SumWorldAmount(int worldId, bool includeRelatedWorlds, Tag tag)
        {
            float amount = 0f;
            if (IncludesAllWorlds(includeRelatedWorlds))
            {
                foreach (WorldContentState world in Worlds.Values)
                {
                    if (world.AmountsByTag.TryGetValue(tag, out float worldAmount))
                    {
                        amount += worldAmount;
                    }
                }
            }
            else if (Worlds.TryGetValue(worldId, out WorldContentState world) &&
                     world.AmountsByTag.TryGetValue(tag, out float worldAmount))
            {
                amount = worldAmount;
            }

            return amount;
        }

        private static float GetFilteredAmount(
            WorldContentState world,
            Tag tag,
            Tag[] forbiddenTags)
        {
            float amount = 0f;
            foreach (StorageContentRecord record in world.Records)
            {
                foreach (IndexedItem item in record.Items)
                {
                    if (item.MatchTags.Contains(tag) &&
                        !item.HasAnyForbiddenTag(forbiddenTags))
                    {
                        amount += item.Amount;
                    }
                }
            }

            return amount;
        }

        private static int GetCountWithAdditionalTag(
            WorldContentState world,
            Tag tag,
            Tag additionalTag)
        {
            int count = 0;
            foreach (StorageContentRecord record in world.Records)
            {
                foreach (IndexedItem item in record.Items)
                {
                    if (item.MatchTags.Contains(tag) &&
                        (!additionalTag.IsValid || item.HasTag(additionalTag)))
                    {
                        count += Mathf.CeilToInt(item.Amount);
                    }
                }
            }

            return count;
        }

        private static bool WorldHasPlantableSeed(
            WorldContentState world,
            Tag seedTag,
            Tag additionalTag)
        {
            foreach (StorageContentRecord record in world.Records)
            {
                foreach (IndexedItem item in record.Items)
                {
                    if (item.IsPlantableSeed &&
                        item.MatchTags.Contains(seedTag) &&
                        (!additionalTag.IsValid || item.HasTag(additionalTag)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static float GetEdibleCalories(
            WorldContentState world,
            string foodId,
            Dictionary<string, float> unitsById)
        {
            float calories = 0f;
            foreach (StorageContentRecord record in world.Records)
            {
                foreach (IndexedItem item in record.Items)
                {
                    if (item.EdibleCalories <= 0f ||
                        string.IsNullOrEmpty(item.FoodId) ||
                        !string.IsNullOrEmpty(foodId) && item.FoodId != foodId)
                    {
                        continue;
                    }

                    calories += item.EdibleCalories;
                    if (unitsById == null)
                    {
                        continue;
                    }

                    if (unitsById.TryGetValue(item.FoodId, out float units))
                    {
                        unitsById[item.FoodId] = units + item.EdibleUnits;
                    }
                    else
                    {
                        unitsById[item.FoodId] = item.EdibleUnits;
                    }
                }
            }

            return calories;
        }

        private static bool IncludesAllWorlds(bool includeRelatedWorlds)
        {
            return includeRelatedWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline();
        }

        private static bool TryGetUsableSource(
            Storage storage,
            int destinationWorldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> tags,
            HashSet<Storage> excludedStorages,
            out float amount)
        {
            amount = 0f;
            if (storage == null ||
                excludedStorages != null && excludedStorages.Contains(storage) ||
                !Records.TryGetValue(storage, out StorageContentRecord record) ||
                !record.SourceEligible ||
                !StorageSceneRegistry.IsLive(storage) ||
                !IncludesAllWorlds(includeRelatedWorlds) && record.WorldId != destinationWorldId)
            {
                return false;
            }

            foreach (Tag tag in tags)
            {
                if (tag == Tag.Invalid)
                {
                    continue;
                }

                float tagAmount = record.SourceAmounts.TryGetValue(tag, out float indexedAmount)
                    ? indexedAmount
                    : 0f;
                TransferTransactionContext transaction = transferTransaction;
                if (!StorageNetworkPerformanceMode.LegacyFullScanEnabled &&
                    transaction != null &&
                    transaction.Depth > 0)
                {
                    if (transaction.RequiresNativeRead(storage))
                    {
                        tagAmount = StorageNetworkContentShadowReader.GetStorageAmount(
                            storage,
                            tag);
                    }
                    else if (transaction.TryGetAmountDelta(
                        storage,
                        tag,
                        out float pendingAmount))
                    {
                        tagAmount = Mathf.Max(0f, tagAmount + pendingAmount);
                    }
                }

                amount = Mathf.Max(amount, tagAmount);
            }

            return amount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static void FillPendingTransferSources(
            int worldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> tags,
            HashSet<Storage> excludedStorages,
            List<Storage> result)
        {
            PendingSourceTags.Clear();
            foreach (Tag tag in tags)
            {
                if (tag != Tag.Invalid)
                {
                    PendingSourceTags.Add(tag);
                }
            }

            SourceMergeWorkspace.Clear();
            bool includeAllWorlds = IncludesAllWorlds(includeRelatedWorlds);
            if (includeAllWorlds)
            {
                foreach (WorldContentState world in Worlds.Values)
                {
                    AddPendingTransferSources(
                        world,
                        worldId,
                        includeRelatedWorlds,
                        PendingSourceTags,
                        excludedStorages);
                }
            }
            else if (Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                AddPendingTransferSources(
                    world,
                    worldId,
                    includeRelatedWorlds,
                    PendingSourceTags,
                    excludedStorages);
            }

            SourceMergeWorkspace.Sort(CompareSources);
            foreach (KeyValuePair<StorageContentRecord, float> pair in SourceMergeWorkspace)
            {
                result.Add(pair.Key.Storage);
            }

            SourceMergeWorkspace.Clear();
            PendingSourceTags.Clear();
        }

        private static void AddPendingTransferSources(
            WorldContentState world,
            int destinationWorldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> tags,
            HashSet<Storage> excludedStorages)
        {
            foreach (StorageContentRecord record in world.Records)
            {
                if (TryGetUsableSource(
                    record.Storage,
                    destinationWorldId,
                    includeRelatedWorlds,
                    tags,
                    excludedStorages,
                    out float amount))
                {
                    SourceMergeWorkspace.Add(
                        new KeyValuePair<StorageContentRecord, float>(record, amount));
                }
            }
        }

        private static void MergeSources(
            int worldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> tags,
            HashSet<Storage> excludedStorages)
        {
            if (IncludesAllWorlds(includeRelatedWorlds))
            {
                foreach (WorldContentState world in Worlds.Values)
                {
                    MergeWorldSources(world, tags, excludedStorages);
                }
            }
            else if (Worlds.TryGetValue(worldId, out WorldContentState world))
            {
                MergeWorldSources(world, tags, excludedStorages);
            }
        }

        private static void MergeWorldSources(
            WorldContentState world,
            IEnumerable<Tag> tags,
            HashSet<Storage> excludedStorages)
        {
            foreach (Tag tag in tags)
            {
                if (tag == Tag.Invalid ||
                    !world.SourceBuckets.TryGetValue(tag, out SourceBucket bucket))
                {
                    continue;
                }

                foreach (KeyValuePair<StorageContentRecord, float> pair in bucket.Amounts)
                {
                    StorageContentRecord record = pair.Key;
                    if (!StorageSceneRegistry.IsLive(record.Storage) ||
                        excludedStorages != null && excludedStorages.Contains(record.Storage))
                    {
                        continue;
                    }

                    if (SourceMergeAmounts.TryGetValue(record, out float amount))
                    {
                        SourceMergeAmounts[record] = Mathf.Max(amount, pair.Value);
                    }
                    else
                    {
                        SourceMergeAmounts.Add(record, pair.Value);
                    }
                }
            }
        }

        private static bool TryGetSingleTag(
            IEnumerable<Tag> tags,
            out Tag singleTag,
            out bool multipleTags)
        {
            singleTag = Tag.Invalid;
            multipleTags = false;
            bool found = false;
            foreach (Tag tag in tags)
            {
                if (tag == Tag.Invalid)
                {
                    continue;
                }

                if (found && tag != singleTag)
                {
                    multipleTags = true;
                }
                else if (!found)
                {
                    singleTag = tag;
                    found = true;
                }
            }

            return found;
        }

        private static int CompareSources(
            KeyValuePair<StorageContentRecord, float> left,
            KeyValuePair<StorageContentRecord, float> right)
        {
            int amountComparison = right.Value.CompareTo(left.Value);
            return amountComparison != 0
                ? amountComparison
                : left.Key.InstanceId.CompareTo(right.Key.InstanceId);
        }

        private static void AddUniqueTag(
            Dictionary<Tag, float> amounts,
            Tag tag,
            float amount,
            Tag existing1 = default,
            Tag existing2 = default,
            Tag existing3 = default,
            Tag existing4 = default)
        {
            if (tag == Tag.Invalid ||
                tag == existing1 ||
                tag == existing2 ||
                tag == existing3 ||
                tag == existing4)
            {
                return;
            }

            AddAmount(amounts, tag, amount);
        }

        private static void AddAmount(Dictionary<Tag, float> amounts, Tag tag, float delta)
        {
            if (tag == Tag.Invalid || Mathf.Abs(delta) <= float.Epsilon)
            {
                return;
            }

            if (amounts.TryGetValue(tag, out float amount))
            {
                float updated = amount + delta;
                if (updated <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    amounts.Remove(tag);
                }
                else
                {
                    amounts[tag] = updated;
                }
            }
            else if (delta > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                amounts.Add(tag, delta);
            }
        }

        private static void AddAmount(
            Dictionary<SimHashes, float> amounts,
            SimHashes element,
            float delta)
        {
            if (element == SimHashes.Void || Mathf.Abs(delta) <= float.Epsilon)
            {
                return;
            }

            if (amounts.TryGetValue(element, out float amount))
            {
                float updated = amount + delta;
                if (updated <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    amounts.Remove(element);
                }
                else
                {
                    amounts[element] = updated;
                }
            }
            else if (delta > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                amounts.Add(element, delta);
            }
        }

        private static void MergeAmounts(
            Dictionary<Tag, float> source,
            Dictionary<Tag, float> destination)
        {
            foreach (KeyValuePair<Tag, float> pair in source)
            {
                AddAmount(destination, pair.Key, pair.Value);
            }
        }

        private static void AddProductionSourceWorld(
            WorldContentState world,
            Dictionary<Tag, float> massByTag,
            List<Storage> sourceStorages)
        {
            MergeAmounts(world.SourceMassesByTag, massByTag);
            foreach (StorageContentRecord record in world.Records)
            {
                if (record.SourceEligible && StorageSceneRegistry.IsLive(record.Storage))
                {
                    sourceStorages.Add(record.Storage);
                }
            }

        }

        private sealed class TransferTransactionContext
        {
            private readonly HashSet<Storage> touchedSet = new HashSet<Storage>();
            private readonly HashSet<Storage> nativeReadRequired = new HashSet<Storage>();
            private readonly HashSet<Storage> forcedNativeRead = new HashSet<Storage>();
            private readonly Dictionary<Storage, StorageTransferDeltas> storageDeltas =
                new Dictionary<Storage, StorageTransferDeltas>();
            private readonly List<StorageTransferDeltas> reusableDeltaBuckets =
                new List<StorageTransferDeltas>();

            public int Depth { get; set; }

            public bool HasPendingChanges => TouchedStorages.Count > 0;

            public bool HasNativeReadRequirements => nativeReadRequired.Count > 0;

            public List<Storage> TouchedStorages { get; } = new List<Storage>();

            public void Touch(Storage storage, bool requireNativeRead)
            {
                if (ReferenceEquals(storage, null))
                {
                    return;
                }

                if (touchedSet.Add(storage))
                {
                    TouchedStorages.Add(storage);
                }

                if (requireNativeRead)
                {
                    nativeReadRequired.Add(storage);
                }
            }

            public void RecordTransfer(
                Storage source,
                Storage target,
                StorageItemUtility.StorageMatchTags matchTags,
                Tag stateTag,
                float amount,
                float massKg)
            {
                Touch(source, requireNativeRead: false);
                Touch(target, requireNativeRead: false);

                if (amount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    AddStorageTags(source, matchTags, stateTag, -amount);
                    AddStorageTags(target, matchTags, stateTag, amount);
                }

                if (massKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    AddMassDelta(source, -massKg);
                    AddMassDelta(target, massKg);
                    AddSourceMassTags(source, matchTags, -massKg);
                    AddSourceMassTags(target, matchTags, massKg);
                }

                // Explicit deltas describe the completed operation and supersede
                // the provisional native-event fallback for both endpoints.
                if (!ReferenceEquals(source, null) && !forcedNativeRead.Contains(source))
                {
                    nativeReadRequired.Remove(source);
                }

                if (!ReferenceEquals(target, null) && !forcedNativeRead.Contains(target))
                {
                    nativeReadRequired.Remove(target);
                }
            }

            public void RequireNativeReadsForTouchedStorages()
            {
                foreach (Storage storage in TouchedStorages)
                {
                    if (ReferenceEquals(storage, null))
                    {
                        continue;
                    }

                    nativeReadRequired.Add(storage);
                    forcedNativeRead.Add(storage);
                }
            }

            public void ForceNativeRead(Storage storage)
            {
                Touch(storage, requireNativeRead: true);
                if (!ReferenceEquals(storage, null))
                {
                    forcedNativeRead.Add(storage);
                }
            }

            public bool RequiresNativeRead(Storage storage)
            {
                return !ReferenceEquals(storage, null) &&
                       nativeReadRequired.Contains(storage);
            }

            public bool TryGetAmountDelta(Storage storage, Tag tag, out float delta)
            {
                if (storageDeltas.TryGetValue(storage, out StorageTransferDeltas deltas))
                {
                    return deltas.SourceAmounts.TryGetValue(tag, out delta);
                }

                delta = 0f;
                return false;
            }

            public bool TryGetMassDelta(Storage storage, out float delta)
            {
                if (storageDeltas.TryGetValue(storage, out StorageTransferDeltas deltas))
                {
                    delta = deltas.MassKg;
                    return Mathf.Abs(delta) > float.Epsilon;
                }

                delta = 0f;
                return false;
            }

            public float GetInventoryAmountDelta(int worldId, bool allWorlds, Tag tag)
            {
                float delta = 0f;
                foreach (KeyValuePair<Storage, StorageTransferDeltas> storagePair in storageDeltas)
                {
                    if (IsDeltaRecordInScope(storagePair.Key, worldId, allWorlds, out _) &&
                        storagePair.Value.InventoryAmounts.TryGetValue(tag, out float amount))
                    {
                        delta += amount;
                    }
                }

                return delta;
            }

            public float GetCapacityMassDelta(int worldId, bool allWorlds)
            {
                float delta = 0f;
                foreach (KeyValuePair<Storage, StorageTransferDeltas> pair in storageDeltas)
                {
                    if (IsDeltaRecordInScope(pair.Key, worldId, allWorlds, out StorageContentRecord record) &&
                        record.CountsTowardCapacity)
                    {
                        delta += pair.Value.MassKg;
                    }
                }

                return delta;
            }

            public void ApplyInventoryAmountDeltas(
                int worldId,
                bool allWorlds,
                Dictionary<Tag, float> destination)
            {
                foreach (KeyValuePair<Storage, StorageTransferDeltas> storagePair in storageDeltas)
                {
                    if (!IsDeltaRecordInScope(storagePair.Key, worldId, allWorlds, out _))
                    {
                        continue;
                    }

                    foreach (KeyValuePair<Tag, float> pair in storagePair.Value.InventoryAmounts)
                    {
                        AddAmount(destination, pair.Key, pair.Value);
                    }
                }
            }

            public void ApplySourceMassDeltas(
                int worldId,
                bool allWorlds,
                Dictionary<Tag, float> destination)
            {
                foreach (KeyValuePair<Storage, StorageTransferDeltas> storagePair in storageDeltas)
                {
                    if (IsDeltaRecordInScope(
                            storagePair.Key,
                            worldId,
                            allWorlds,
                            out StorageContentRecord record) &&
                        record.SourceEligible &&
                        (!allWorlds || StorageSceneRegistry.HasOnlineCoreInWorld(record.WorldId)))
                    {
                        foreach (KeyValuePair<Tag, float> pair in storagePair.Value.SourceMasses)
                        {
                            AddAmount(destination, pair.Key, pair.Value);
                        }
                    }
                }
            }

            public void ApplyDeltas(StorageContentRecord record)
            {
                if (record == null)
                {
                    return;
                }

                if (!storageDeltas.TryGetValue(
                        record.Storage,
                        out StorageTransferDeltas deltas))
                {
                    return;
                }

                RemoveRecordContributions(record);

                foreach (KeyValuePair<Tag, float> pair in deltas.InventoryAmounts)
                {
                    AddAmount(record.Amounts, pair.Key, pair.Value);
                }

                foreach (KeyValuePair<Tag, float> pair in deltas.SourceAmounts)
                {
                    AddAmount(record.SourceAmounts, pair.Key, pair.Value);
                }

                foreach (KeyValuePair<Tag, float> pair in deltas.SourceMasses)
                {
                    AddAmount(record.SourceMasses, pair.Key, pair.Value);
                }

                if (Mathf.Abs(deltas.MassKg) > float.Epsilon)
                {
                    record.StoredKg = Mathf.Max(
                        0f,
                        Mathf.Round((record.StoredKg + deltas.MassKg) * 1000f) / 1000f);
                }

                Storage storage = record.Storage;
                if (!ReferenceEquals(storage, null))
                {
                    record.CapacityKg = storage.Capacity();
                }

                record.HasSnapshot = true;
                record.ItemDetailsDirty = true;
                record.DirtyFrame = Time.frameCount;
                ItemDetailDirtyRecords.Add(record);
                AddRecordContributions(record);
                unchecked
                {
                    record.DisplayVersion++;
                    contentVersion++;
                }
            }

            public void Reset()
            {
                Depth = 0;
                touchedSet.Clear();
                nativeReadRequired.Clear();
                forcedNativeRead.Clear();
                foreach (StorageTransferDeltas deltas in storageDeltas.Values)
                {
                    deltas.Reset();
                    reusableDeltaBuckets.Add(deltas);
                }

                storageDeltas.Clear();
                TouchedStorages.Clear();
            }

            private bool IsDeltaRecordInScope(
                Storage storage,
                int worldId,
                bool allWorlds,
                out StorageContentRecord record)
            {
                record = null;
                return !forcedNativeRead.Contains(storage) &&
                       Records.TryGetValue(storage, out record) &&
                       (allWorlds || record.WorldId == worldId);
            }

            private void AddStorageTags(
                Storage storage,
                StorageItemUtility.StorageMatchTags matchTags,
                Tag stateTag,
                float delta)
            {
                if (ReferenceEquals(storage, null))
                {
                    return;
                }

                StorageTransferDeltas deltas = GetOrCreateStorageDeltas(storage);
                AddMatchTagDelta(deltas, matchTags.PrefabIdTag, delta);
                AddUniqueTagDelta(
                    deltas,
                    matchTags.PrefabTag,
                    delta,
                    matchTags.PrefabIdTag);
                AddUniqueTagDelta(
                    deltas,
                    matchTags.ElementTag,
                    delta,
                    matchTags.PrefabIdTag,
                    matchTags.PrefabTag);
                AddUniqueTagDelta(
                    deltas,
                    matchTags.TransferTag,
                    delta,
                    matchTags.PrefabIdTag,
                    matchTags.PrefabTag,
                    matchTags.ElementTag);
                AddUniqueTagDelta(
                    deltas,
                    stateTag,
                    delta,
                    matchTags.PrefabIdTag,
                    matchTags.PrefabTag,
                    matchTags.ElementTag,
                    matchTags.TransferTag,
                    sourceOnly: true);
            }

            private void AddUniqueTagDelta(
                StorageTransferDeltas deltas,
                Tag tag,
                float delta,
                Tag existing1 = default,
                Tag existing2 = default,
                Tag existing3 = default,
                Tag existing4 = default,
                bool sourceOnly = false)
            {
                if (tag == Tag.Invalid ||
                    tag == existing1 ||
                    tag == existing2 ||
                    tag == existing3 ||
                    tag == existing4)
                {
                    return;
                }

                if (sourceOnly)
                {
                    AddTagDelta(deltas.SourceAmounts, tag, delta);
                }
                else
                {
                    AddMatchTagDelta(deltas, tag, delta);
                }
            }

            private static void AddMatchTagDelta(
                StorageTransferDeltas deltas,
                Tag tag,
                float delta)
            {
                AddTagDelta(deltas.SourceAmounts, tag, delta);
                AddTagDelta(deltas.InventoryAmounts, tag, delta);
            }

            private void AddSourceMassTags(
                Storage storage,
                StorageItemUtility.StorageMatchTags matchTags,
                float delta)
            {
                if (ReferenceEquals(storage, null))
                {
                    return;
                }

                StorageTransferDeltas deltas = GetOrCreateStorageDeltas(storage);
                AddTagDelta(deltas.SourceMasses, matchTags.TransferTag, delta);
                if (matchTags.ElementTag != matchTags.TransferTag)
                {
                    AddTagDelta(deltas.SourceMasses, matchTags.ElementTag, delta);
                }
            }

            private static void AddTagDelta(
                Dictionary<Tag, float> deltas,
                Tag tag,
                float delta)
            {
                if (tag == Tag.Invalid || Mathf.Abs(delta) <= float.Epsilon)
                {
                    return;
                }

                if (deltas.TryGetValue(tag, out float current))
                {
                    float updated = current + delta;
                    if (Mathf.Abs(updated) <= float.Epsilon)
                    {
                        deltas.Remove(tag);
                    }
                    else
                    {
                        deltas[tag] = updated;
                    }
                }
                else
                {
                    deltas.Add(tag, delta);
                }
            }

            private void AddMassDelta(Storage storage, float delta)
            {
                if (ReferenceEquals(storage, null) || Mathf.Abs(delta) <= float.Epsilon)
                {
                    return;
                }

                StorageTransferDeltas deltas = GetOrCreateStorageDeltas(storage);
                deltas.MassKg += delta;
                if (Mathf.Abs(deltas.MassKg) <= float.Epsilon)
                {
                    deltas.MassKg = 0f;
                }
            }

            private StorageTransferDeltas GetOrCreateStorageDeltas(Storage storage)
            {
                if (storageDeltas.TryGetValue(storage, out StorageTransferDeltas deltas))
                {
                    return deltas;
                }

                int reusableIndex = reusableDeltaBuckets.Count - 1;
                if (reusableIndex >= 0)
                {
                    deltas = reusableDeltaBuckets[reusableIndex];
                    reusableDeltaBuckets.RemoveAt(reusableIndex);
                }
                else
                {
                    deltas = new StorageTransferDeltas();
                }

                deltas.Storage = storage;
                storageDeltas.Add(storage, deltas);
                return deltas;
            }

            private sealed class StorageTransferDeltas
            {
                public Storage Storage { get; set; }
                public float MassKg { get; set; }
                public Dictionary<Tag, float> SourceAmounts { get; } =
                    new Dictionary<Tag, float>();
                public Dictionary<Tag, float> InventoryAmounts { get; } =
                    new Dictionary<Tag, float>();
                public Dictionary<Tag, float> SourceMasses { get; } =
                    new Dictionary<Tag, float>();

                public void Reset()
                {
                    Storage = null;
                    MassKg = 0f;
                    SourceAmounts.Clear();
                    InventoryAmounts.Clear();
                    SourceMasses.Clear();
                }
            }
        }

        private sealed class StorageContentRecord
        {
            public StorageContentRecord(Storage storage, int worldId)
            {
                Storage = storage;
                WorldId = worldId;
                KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
                InstanceId = prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
            }

            public Storage Storage { get; }
            public int InstanceId { get; }
            public int WorldId { get; set; }
            public Action<GameObject> ChangeHandler { get; set; }
            public bool HasSnapshot { get; set; }
            public bool CountsTowardCapacity { get; set; }
            public bool SourceEligible { get; set; }
            public bool ItemDetailsDirty { get; set; }
            public int DirtyFrame { get; set; }
            public int DisplayVersion { get; set; }
            public float StoredKg { get; set; }
            public float CapacityKg { get; set; }
            public bool HasUnindexedDisplayItems { get; set; }
            public Dictionary<Tag, float> Amounts { get; } = new Dictionary<Tag, float>();
            public Dictionary<Tag, float> SourceAmounts { get; } = new Dictionary<Tag, float>();
            public Dictionary<Tag, float> SourceMasses { get; } = new Dictionary<Tag, float>();
            public Dictionary<SimHashes, float> ElementMasses { get; } =
                new Dictionary<SimHashes, float>();
            public List<IndexedItem> Items { get; } = new List<IndexedItem>();
            public List<StorageNetworkIndexedItemTotal> ItemTotals { get; } =
                new List<StorageNetworkIndexedItemTotal>();
            public Dictionary<Tag, int> ItemTotalIndexes { get; } =
                new Dictionary<Tag, int>();
        }

        private sealed class WorldContentState
        {
            public HashSet<StorageContentRecord> Records { get; } =
                new HashSet<StorageContentRecord>();
            public Dictionary<Tag, float> AmountsByTag { get; } =
                new Dictionary<Tag, float>();
            public Dictionary<Tag, SourceBucket> SourceBuckets { get; } =
                new Dictionary<Tag, SourceBucket>();
            public Dictionary<Tag, float> SourceMassesByTag { get; } =
                new Dictionary<Tag, float>();
            public Dictionary<SimHashes, float> ElementMasses { get; } =
                new Dictionary<SimHashes, float>();
            public float TotalStoredKg { get; set; }
            public float TotalCapacityKg { get; set; }

            public SourceBucket GetOrCreateSourceBucket(Tag tag)
            {
                if (!SourceBuckets.TryGetValue(tag, out SourceBucket bucket))
                {
                    bucket = new SourceBucket();
                    SourceBuckets.Add(tag, bucket);
                }

                return bucket;
            }
        }

        private sealed class SourceBucket
        {
            private readonly List<KeyValuePair<StorageContentRecord, float>> sorted =
                new List<KeyValuePair<StorageContentRecord, float>>();
            private bool sortedDirty = true;

            public Dictionary<StorageContentRecord, float> Amounts { get; } =
                new Dictionary<StorageContentRecord, float>();

            public void Set(StorageContentRecord record, float amount)
            {
                if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    Remove(record);
                    return;
                }

                Amounts[record] = amount;
                sortedDirty = true;
            }

            public void Remove(StorageContentRecord record)
            {
                if (Amounts.Remove(record))
                {
                    sortedDirty = true;
                }
            }

            public void Fill(HashSet<Storage> excludedStorages, List<Storage> result)
            {
                EnsureSorted();
                foreach (KeyValuePair<StorageContentRecord, float> pair in sorted)
                {
                    Storage storage = pair.Key.Storage;
                    if (StorageSceneRegistry.IsLive(storage) &&
                        (excludedStorages == null || !excludedStorages.Contains(storage)))
                    {
                        result.Add(storage);
                    }
                }
            }

            private void EnsureSorted()
            {
                if (!sortedDirty)
                {
                    return;
                }

                sorted.Clear();
                foreach (KeyValuePair<StorageContentRecord, float> pair in Amounts)
                {
                    sorted.Add(pair);
                }

                sorted.Sort(CompareSources);
                sortedDirty = false;
            }
        }

        private sealed class StableStorageComparer : IComparer<Storage>
        {
            public int Compare(Storage left, Storage right)
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (ReferenceEquals(left, null))
                {
                    return 1;
                }

                if (ReferenceEquals(right, null))
                {
                    return -1;
                }

                return StorageItemUtility.GetStorageInstanceId(left)
                    .CompareTo(StorageItemUtility.GetStorageInstanceId(right));
            }
        }

        private readonly struct IndexedItem
        {
            private readonly KPrefabID prefabId;

            public IndexedItem(
                StorageItemUtility.StorageMatchTags matchTags,
                KPrefabID prefabId,
                float amount,
                Edible edible)
            {
                MatchTags = matchTags;
                this.prefabId = prefabId;
                Amount = amount;
                IsPlantableSeed = prefabId != null &&
                                  prefabId.GetComponent<PlantableSeed>() != null;
                FoodId = edible != null ? edible.FoodID : null;
                EdibleCalories = edible != null ? edible.Calories : 0f;
                EdibleUnits = edible != null ? edible.Units : 0f;
            }

            public StorageItemUtility.StorageMatchTags MatchTags { get; }
            public float Amount { get; }
            public bool IsPlantableSeed { get; }
            public string FoodId { get; }
            public float EdibleCalories { get; }
            public float EdibleUnits { get; }

            public bool HasAnyForbiddenTag(Tag[] forbiddenTags)
            {
                return prefabId != null &&
                       forbiddenTags != null &&
                       prefabId.HasAnyTags(forbiddenTags);
            }

            public bool HasTag(Tag tag)
            {
                return tag != Tag.Invalid && prefabId != null && prefabId.HasTag(tag);
            }
        }
    }

    /// <summary>
    /// Compact item-display aggregate stored alongside the routing index. It uses
    /// Tag identity internally; UI string keys are produced only for visible rows.
    /// </summary>
    internal struct StorageNetworkIndexedItemTotal
    {
        private float weightedTemperature;
        private float simpleTemperature;
        private float temperatureMass;
        private int temperatureCount;

        public StorageNetworkIndexedItemTotal(Tag keyTag)
        {
            KeyTag = keyTag;
            MassKg = 0f;
            Representative = null;
            weightedTemperature = 0f;
            simpleTemperature = 0f;
            temperatureMass = 0f;
            temperatureCount = 0;
        }

        public Tag KeyTag { get; }
        public float MassKg { get; private set; }
        public GameObject Representative { get; private set; }
        public bool HasTemperature => temperatureCount > 0;
        public float AverageTemperature => temperatureMass > 0f
            ? weightedTemperature / temperatureMass
            : temperatureCount > 0
                ? simpleTemperature / temperatureCount
                : 0f;

        public void Add(
            GameObject representative,
            PrimaryElement primaryElement,
            float massKg)
        {
            if (Representative == null && representative != null)
            {
                Representative = representative;
            }

            MassKg += massKg;
            if (primaryElement == null)
            {
                return;
            }

            float temperature = primaryElement.Temperature;
            float temperatureWeight = Mathf.Max(0f, primaryElement.Mass);
            if (temperatureWeight > 0f)
            {
                weightedTemperature += temperature * temperatureWeight;
                temperatureMass += temperatureWeight;
            }

            simpleTemperature += temperature;
            temperatureCount++;
        }

        public void Merge(StorageNetworkIndexedItemTotal other)
        {
            if (Representative == null && other.Representative != null)
            {
                Representative = other.Representative;
            }

            MassKg += other.MassKg;
            weightedTemperature += other.weightedTemperature;
            simpleTemperature += other.simpleTemperature;
            temperatureMass += other.temperatureMass;
            temperatureCount += other.temperatureCount;
        }
    }
}
