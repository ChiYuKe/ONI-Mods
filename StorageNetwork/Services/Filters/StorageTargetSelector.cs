using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static partial class StorageTargetSelector
    {
        private const float OutputTargetCacheSeconds = 1f;
        private const int MaxOutputTargetCacheEntries = 512;
        private static readonly Dictionary<OutputTargetCacheKey, CachedOutputTarget> OutputTargetCache =
            new Dictionary<OutputTargetCacheKey, CachedOutputTarget>();
        private static readonly List<OutputTargetCacheKey> OutputTargetCacheRemovalWorkspace =
            new List<OutputTargetCacheKey>();
        private static readonly List<Storage> ShadowTargetWorkspace = new List<Storage>();
        private static readonly HashSet<Storage> EmptyStorageExclusions = new HashSet<Storage>();
        private static readonly ElementTargetComparer ElementTargetsComparer =
            new ElementTargetComparer();
        private static int outputTargetCacheRegistryVersion = -1;
        private static int outputTargetCacheMembershipVersion = -1;
        private static int outputTargetCacheConnectivityVersion = -1;
        private static int outputTargetCacheReservationVersion = -1;
        private static int targetValidationContentVersion = -1;
        private static int targetValidationMembershipVersion = -1;
        private static int targetValidationCapabilityVersion = -1;
        private static int targetValidationConnectivityVersion = -1;
        private static int targetValidationReservationVersion = -1;
        private static int targetValidationVersion;
        private static float outputTargetCacheLastPruneTime = float.MinValue;

        public static void ResetRuntimeState()
        {
            OutputTargetCache.Clear();
            outputTargetCacheRegistryVersion = -1;
            outputTargetCacheMembershipVersion = -1;
            outputTargetCacheConnectivityVersion = -1;
            outputTargetCacheReservationVersion = -1;
            targetValidationContentVersion = -1;
            targetValidationMembershipVersion = -1;
            targetValidationCapabilityVersion = -1;
            targetValidationConnectivityVersion = -1;
            targetValidationReservationVersion = -1;
            targetValidationVersion = 0;
            outputTargetCacheLastPruneTime = float.MinValue;
            OutputTargetCacheRemovalWorkspace.Clear();
            ShadowTargetWorkspace.Clear();
        }

        internal static void InvalidateOutputTargetCache()
        {
            OutputTargetCache.Clear();
            OutputTargetCacheRemovalWorkspace.Clear();
        }

        /// <summary>
        /// Keeps a cached winner only when a known transfer makes that same winner
        /// strictly stronger for the cached item. Every mutation which could improve
        /// a competing target invalidates the affected decision before it is reused.
        /// </summary>
        internal static void NotifyTransferMutation(
            Storage storage,
            StorageItemUtility.StorageMatchTags matchTags,
            float amountDelta,
            float massDelta)
        {
            if (storage == null ||
                OutputTargetCache.Count == 0 ||
                !StorageNetworkStorageRules.IsNetworkStorageTarget(storage))
            {
                return;
            }

            int storageId = storage.GetInstanceID();
            bool gainsAmount = amountDelta > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool losesAmount = amountDelta < -PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            bool gainsCapacity = massDelta < -PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            OutputTargetCacheRemovalWorkspace.Clear();
            foreach (KeyValuePair<OutputTargetCacheKey, CachedOutputTarget> pair in OutputTargetCache)
            {
                OutputTargetCacheKey key = pair.Key;
                if (key.SourceStorageId == storageId && key.SourceIsExcluded)
                {
                    // The source encoded in this key is excluded from its candidate set.
                    continue;
                }

                bool sameTarget = object.ReferenceEquals(pair.Value.Target, storage);
                bool matchingAmount = key.Matches(matchTags);
                bool winnerStrengthened =
                    sameTarget && gainsAmount && key.MatchesExactly(matchTags);
                bool competitorMayImprove =
                    !sameTarget && ((gainsAmount && matchingAmount) || gainsCapacity);
                bool winnerMayWeaken =
                    sameTarget && !winnerStrengthened &&
                    (losesAmount || gainsCapacity || massDelta > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT);
                if (competitorMayImprove || winnerMayWeaken)
                {
                    OutputTargetCacheRemovalWorkspace.Add(key);
                }
            }

            foreach (OutputTargetCacheKey key in OutputTargetCacheRemovalWorkspace)
            {
                OutputTargetCache.Remove(key);
            }

            OutputTargetCacheRemovalWorkspace.Clear();
        }

        public static bool MatchesAllowedTags(GameObject item, HashSet<Tag> allowedTags)
        {
            if (allowedTags == null || allowedTags.Count == 0)
            {
                return true;
            }

            foreach (Tag tag in allowedTags)
            {
                if (tag != Tag.Invalid && item != null && item.HasTag(tag))
                {
                    return true;
                }
            }

            return StorageItemUtility.GetStorageMatchTagsNonAlloc(item)
                .AnyAcceptedBy(allowedTags);
        }

        public static bool MatchesAllowedTags(GameObject item, HashSet<Tag> allowedTags, StorageItemUtility.StorageMatchTags matchTags)
        {
            if (allowedTags == null || allowedTags.Count == 0)
            {
                return true;
            }

            foreach (Tag tag in allowedTags)
            {
                if (tag != Tag.Invalid && item != null && item.HasTag(tag))
                {
                    return true;
                }
            }

            return matchTags.AnyAcceptedBy(allowedTags);
        }

        public static Storage FindOutputTarget(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1,
            Storage sourceStorage = null)
        {
            StorageItemUtility.StorageMatchTags cacheMatchTags = StorageItemUtility.GetStorageMatchTagsNonAlloc(item);
            if (specificTarget != null)
            {
                return IsUsableOutputTarget(specificTarget, item, matchTags, excludedStorages, sourceWorldId) ? specificTarget : null;
            }

            if (snapshot == null &&
                CanCacheMatchTagSet(matchTags, cacheMatchTags) &&
                TryGetCachedOutputTarget(item, cacheMatchTags, excludedStorages, sourceWorldId, sourceStorage, false, out Storage cachedTarget))
            {
                return cachedTarget;
            }

            Storage target;
            if (snapshot == null && sourceWorldId >= 0)
            {
                IReadOnlyList<Storage> candidateStorages =
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages;
                target = FindOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage);
                target = ValidateSelectedOutputTarget(
                    target,
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage,
                    false);
                if (CanCacheMatchTagSet(matchTags, cacheMatchTags))
                {
                    CacheOutputTarget(
                        cacheMatchTags,
                        excludedStorages,
                        sourceWorldId,
                        sourceStorage,
                        false,
                        target);
                }
                return target;
            }

            snapshot = snapshot ?? StorageSceneCollector.Collect();
            List<Storage> storages = new List<Storage>();
            foreach (StorageInfo info in snapshot.Storages)
            {
                if (info?.Minion == null && info.Storage != null)
                {
                    storages.Add(info.Storage);
                }
            }

            target = FindOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId, sourceStorage);
            return ValidateSelectedOutputTarget(
                target,
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage,
                false);
        }

        public static Storage FindOutputTarget(
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1,
            Storage sourceStorage = null)
        {
            if (specificTarget != null)
            {
                return IsUsableOutputTarget(specificTarget, item, matchTags, excludedStorages, sourceWorldId) ? specificTarget : null;
            }

            if (snapshot == null &&
                TryGetCachedOutputTarget(item, matchTags, excludedStorages, sourceWorldId, sourceStorage, false, out Storage cachedTarget))
            {
                return cachedTarget;
            }

            Storage target;
            if (snapshot == null && sourceWorldId >= 0)
            {
                IReadOnlyList<Storage> candidateStorages =
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages;
                target = FindOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage);
                target = ValidateSelectedOutputTarget(
                    target,
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage,
                    false);
                CacheOutputTarget(
                    matchTags,
                    excludedStorages,
                    sourceWorldId,
                    sourceStorage,
                    false,
                    target);
                return target;
            }

            snapshot = snapshot ?? StorageSceneCollector.Collect();
            List<Storage> storages = new List<Storage>();
            foreach (StorageInfo info in snapshot.Storages)
            {
                if (info?.Minion == null && info.Storage != null)
                {
                    storages.Add(info.Storage);
                }
            }

            target = FindOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId, sourceStorage);
            return ValidateSelectedOutputTarget(
                target,
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage,
                false);
        }

        public static Storage FindFoodOutputTarget(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1,
            Storage sourceStorage = null)
        {
            StorageItemUtility.StorageMatchTags cacheMatchTags = StorageItemUtility.GetStorageMatchTagsNonAlloc(item);
            if (specificTarget != null)
            {
                return FindOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot, sourceWorldId, sourceStorage);
            }

            if (snapshot == null &&
                CanCacheMatchTagSet(matchTags, cacheMatchTags) &&
                TryGetCachedOutputTarget(item, cacheMatchTags, excludedStorages, sourceWorldId, sourceStorage, true, out Storage cachedTarget))
            {
                return cachedTarget;
            }

            Storage coldTarget;
            if (snapshot == null && sourceWorldId >= 0)
            {
                IReadOnlyList<Storage> candidateStorages =
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages;
                coldTarget = FindColdStorageOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage);
                Storage selectedTarget = coldTarget ?? FindOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage);
                selectedTarget = ValidateSelectedFoodOutputTarget(
                    selectedTarget,
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage);
                if (selectedTarget != null && CanCacheMatchTagSet(matchTags, cacheMatchTags))
                {
                    CacheOutputTarget(
                        cacheMatchTags,
                        excludedStorages,
                        sourceWorldId,
                        sourceStorage,
                        true,
                        selectedTarget);
                }
                return selectedTarget;
            }

            snapshot = snapshot ?? StorageSceneCollector.Collect();
            List<Storage> storages = new List<Storage>();
            foreach (StorageInfo info in snapshot.Storages)
            {
                if (info?.Minion == null && info.Storage != null)
                {
                    storages.Add(info.Storage);
                }
            }

            coldTarget = FindColdStorageOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId, sourceStorage);
            Storage target = coldTarget ?? FindOutputTargetInStorages(
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage);
            return ValidateSelectedFoodOutputTarget(
                target,
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage);
        }

        private static bool TryGetCachedOutputTarget(
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            int sourceWorldId,
            Storage sourceStorage,
            bool coldStorage,
            out Storage target)
        {
            RefreshOutputTargetCacheVersion();
            if (!CanCacheOutputTarget(excludedStorages, sourceStorage))
            {
                target = null;
                return false;
            }

            OutputTargetCacheKey key = new OutputTargetCacheKey(
                sourceWorldId,
                matchTags,
                GetCacheSourceId(sourceStorage),
                coldStorage,
                IsSourceExcluded(excludedStorages, sourceStorage));
            if (OutputTargetCache.TryGetValue(key, out CachedOutputTarget cached) &&
                Time.unscaledTime - cached.CreatedAt <= OutputTargetCacheSeconds &&
                cached.Target != null &&
                IsUsableOutputTarget(cached.Target, item, matchTags, excludedStorages, sourceWorldId) &&
                !StorageNetworkInputTargetReservationService.IsReservedForAutoInput(cached.Target, sourceStorage) &&
                IsAutoOutputMatch(cached.Target, matchTags))
            {
                target = StorageNetworkPerformanceMode.ShadowValidationEnabled
                    ? ValidateCachedOutputTarget(
                        cached.Target,
                        item,
                        matchTags,
                        excludedStorages,
                        sourceWorldId,
                        sourceStorage,
                        coldStorage)
                    : cached.Target;
                return true;
            }

            OutputTargetCache.Remove(key);
            target = null;
            return false;
        }

        private static Storage ValidateCachedOutputTarget(
            Storage cachedTarget,
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            int sourceWorldId,
            Storage sourceStorage,
            bool coldStorage)
        {
            IEnumerable<Storage> storages;
            if (sourceWorldId >= 0)
            {
                storages = StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages;
            }
            else
            {
                ShadowTargetWorkspace.Clear();
                foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
                {
                    if (info?.Minion == null && info.Storage != null)
                    {
                        ShadowTargetWorkspace.Add(info.Storage);
                    }
                }

                storages = ShadowTargetWorkspace;
            }

            Storage validated = coldStorage
                ? ValidateSelectedFoodOutputTarget(
                    cachedTarget,
                    item,
                    matchTags,
                    excludedStorages,
                    storages,
                    sourceWorldId,
                    sourceStorage)
                : ValidateSelectedOutputTarget(
                    cachedTarget,
                    item,
                    matchTags,
                    excludedStorages,
                    storages,
                    sourceWorldId,
                    sourceStorage,
                    false);
            ShadowTargetWorkspace.Clear();
            return validated;
        }

        private static Storage ValidateSelectedOutputTarget(
            Storage indexedTarget,
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage,
            bool coldStorage)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return indexedTarget;
            }

            int version = GetTargetSelectionValidationVersion();
            if (!ShouldValidateTargetSelection(sourceWorldId, version))
            {
                return indexedTarget;
            }

            Storage nativeTarget = FindNativeOutputTargetInStorages(
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage,
                coldStorage);
            return FinishTargetSelectionValidation(
                indexedTarget,
                nativeTarget,
                sourceWorldId,
                version,
                coldStorage);
        }

        private static Storage ValidateSelectedOutputTarget(
            Storage indexedTarget,
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage,
            bool coldStorage)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return indexedTarget;
            }

            int version = GetTargetSelectionValidationVersion();
            if (!ShouldValidateTargetSelection(sourceWorldId, version))
            {
                return indexedTarget;
            }

            Storage nativeTarget = FindNativeOutputTargetInStorages(
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage,
                coldStorage);
            return FinishTargetSelectionValidation(
                indexedTarget,
                nativeTarget,
                sourceWorldId,
                version,
                coldStorage);
        }

        private static Storage ValidateSelectedFoodOutputTarget(
            Storage indexedTarget,
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return indexedTarget;
            }

            int version = GetTargetSelectionValidationVersion();
            if (!ShouldValidateTargetSelection(sourceWorldId, version))
            {
                return indexedTarget;
            }

            Storage nativeTarget = FindNativeOutputTargetInStorages(
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage,
                true);
            if (nativeTarget == null)
            {
                nativeTarget = FindNativeOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    storages,
                    sourceWorldId,
                    sourceStorage,
                    false);
            }

            return FinishTargetSelectionValidation(
                indexedTarget,
                nativeTarget,
                sourceWorldId,
                version,
                true);
        }

        private static Storage ValidateSelectedFoodOutputTarget(
            Storage indexedTarget,
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return indexedTarget;
            }

            int version = GetTargetSelectionValidationVersion();
            if (!ShouldValidateTargetSelection(sourceWorldId, version))
            {
                return indexedTarget;
            }

            Storage nativeTarget = FindNativeOutputTargetInStorages(
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage,
                true);
            if (nativeTarget == null)
            {
                nativeTarget = FindNativeOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    storages,
                    sourceWorldId,
                    sourceStorage,
                    false);
            }

            return FinishTargetSelectionValidation(
                indexedTarget,
                nativeTarget,
                sourceWorldId,
                version,
                true);
        }

        private static bool ShouldValidateTargetSelection(int worldId, int version)
        {
            return StorageNetworkShadowValidationService.ShouldUseFallback(
                       StorageNetworkShadowArea.TargetSelection,
                       worldId,
                       version) ||
                   StorageNetworkShadowValidationService.ShouldValidate(
                       StorageNetworkShadowArea.TargetSelection,
                       worldId,
                       version);
        }

        private static int GetTargetSelectionValidationVersion()
        {
            int contentVersion = StorageNetworkContentIndexService.Version;
            int membershipVersion = StorageSceneRegistry.MembershipVersion;
            int capabilityVersion = StorageSceneRegistry.CapabilityVersion;
            int connectivityVersion = StorageSceneRegistry.ConnectivityVersion;
            int reservationVersion = StorageNetworkInputTargetReservationService.Version;
            if (targetValidationContentVersion == contentVersion &&
                targetValidationMembershipVersion == membershipVersion &&
                targetValidationCapabilityVersion == capabilityVersion &&
                targetValidationConnectivityVersion == connectivityVersion &&
                targetValidationReservationVersion == reservationVersion)
            {
                return targetValidationVersion;
            }

            targetValidationContentVersion = contentVersion;
            targetValidationMembershipVersion = membershipVersion;
            targetValidationCapabilityVersion = capabilityVersion;
            targetValidationConnectivityVersion = connectivityVersion;
            targetValidationReservationVersion = reservationVersion;
            unchecked
            {
                targetValidationVersion++;
                if (targetValidationVersion == 0)
                {
                    targetValidationVersion = 1;
                }
            }

            return targetValidationVersion;
        }

        private static Storage FinishTargetSelectionValidation(
            Storage indexedTarget,
            Storage nativeTarget,
            int sourceWorldId,
            int version,
            bool coldStorage)
        {
            if (object.ReferenceEquals(indexedTarget, nativeTarget))
            {
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.TargetSelection,
                    sourceWorldId,
                    version);
                return indexedTarget;
            }

            int indexedId = StorageItemUtility.GetStorageInstanceId(indexedTarget);
            int nativeId = StorageItemUtility.GetStorageInstanceId(nativeTarget);
            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.TargetSelection,
                sourceWorldId,
                version,
                unchecked(((indexedId * 397) ^ nativeId) * 397 ^
                          (coldStorage ? 1 : 0)),
                $"indexed={indexedId}, native={nativeId}, cold={coldStorage}");
            InvalidateOutputTargetCache();
            if (sourceWorldId < 0 || StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                StorageNetworkContentIndexService.InvalidateAll();
            }
            else
            {
                StorageNetworkContentIndexService.InvalidateWorld(sourceWorldId);
            }
            return nativeTarget;
        }

        private static Storage FindNativeOutputTargetInStorages(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage,
            bool coldStorage)
        {
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (Storage target in storages)
            {
                float remaining =
                    StorageNetworkContentShadowReader.GetRemainingCapacity(target);
                if (coldStorage && !StorageNetworkStorageRules.IsColdStorageServer(target) ||
                    !IsUsableOutputTargetNative(
                        target,
                        item,
                        matchTags,
                        excludedStorages,
                        sourceWorldId,
                        remaining) ||
                    StorageNetworkInputTargetReservationService.IsReservedForAutoInput(
                        target,
                        sourceStorage))
                {
                    continue;
                }

                float available = GetNativeAmountAvailableByAnyMatchTag(target, matchTags);
                if (!IsAutoOutputMatchNative(target, matchTags, available))
                {
                    continue;
                }

                bool filterAccepting = IsFilterAccepting(target, matchTags);
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting == bestFilterAccepting &&
                     remaining > bestRemaining) ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting == bestFilterAccepting &&
                     Mathf.Approximately(remaining, bestRemaining) &&
                     IsStableTargetBefore(target, best)))
                {
                    best = target;
                    bestAvailable = available;
                    bestFilterAccepting = filterAccepting;
                    bestRemaining = remaining;
                }
            }

            return best;
        }

        private static Storage FindNativeOutputTargetInStorages(
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage,
            bool coldStorage)
        {
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (Storage target in storages)
            {
                float remaining =
                    StorageNetworkContentShadowReader.GetRemainingCapacity(target);
                if (coldStorage && !StorageNetworkStorageRules.IsColdStorageServer(target) ||
                    !IsUsableOutputTargetNative(
                        target,
                        item,
                        matchTags,
                        excludedStorages,
                        sourceWorldId,
                        remaining) ||
                    StorageNetworkInputTargetReservationService.IsReservedForAutoInput(
                        target,
                        sourceStorage))
                {
                    continue;
                }

                float available = GetNativeAmountAvailableByAnyMatchTag(target, matchTags);
                if (!IsAutoOutputMatchNative(target, matchTags, available))
                {
                    continue;
                }

                bool filterAccepting = IsFilterAccepting(target, matchTags);
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting == bestFilterAccepting &&
                     remaining > bestRemaining) ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting == bestFilterAccepting &&
                     Mathf.Approximately(remaining, bestRemaining) &&
                     IsStableTargetBefore(target, best)))
                {
                    best = target;
                    bestAvailable = available;
                    bestFilterAccepting = filterAccepting;
                    bestRemaining = remaining;
                }
            }

            return best;
        }

        private static void CacheOutputTarget(
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            int sourceWorldId,
            Storage sourceStorage,
            bool coldStorage,
            Storage target)
        {
            if (target == null ||
                matchTags.TransferTag == Tag.Invalid ||
                !CanCacheOutputTarget(excludedStorages, sourceStorage))
            {
                return;
            }

            RefreshOutputTargetCacheVersion();
            PruneExpiredOutputTargets();
            OutputTargetCache[new OutputTargetCacheKey(
                    sourceWorldId,
                    matchTags,
                    GetCacheSourceId(sourceStorage),
                    coldStorage,
                    IsSourceExcluded(excludedStorages, sourceStorage))] =
                new CachedOutputTarget(target, Time.unscaledTime);
        }

        private static void RefreshOutputTargetCacheVersion()
        {
            int registryVersion = StorageSceneRegistry.CapabilityVersion;
            int membershipVersion = StorageSceneRegistry.MembershipVersion;
            int connectivityVersion = StorageSceneRegistry.ConnectivityVersion;
            int reservationVersion = StorageNetworkInputTargetReservationService.Version;
            if (outputTargetCacheRegistryVersion == registryVersion &&
                outputTargetCacheMembershipVersion == membershipVersion &&
                outputTargetCacheConnectivityVersion == connectivityVersion &&
                outputTargetCacheReservationVersion == reservationVersion)
            {
                return;
            }

            OutputTargetCache.Clear();
            outputTargetCacheRegistryVersion = registryVersion;
            outputTargetCacheMembershipVersion = membershipVersion;
            outputTargetCacheConnectivityVersion = connectivityVersion;
            outputTargetCacheReservationVersion = reservationVersion;
            outputTargetCacheLastPruneTime = Time.unscaledTime;
        }

        private static void PruneExpiredOutputTargets()
        {
            float now = Time.unscaledTime;
            if (OutputTargetCache.Count < MaxOutputTargetCacheEntries &&
                now - outputTargetCacheLastPruneTime < OutputTargetCacheSeconds)
            {
                return;
            }

            OutputTargetCacheRemovalWorkspace.Clear();
            foreach (KeyValuePair<OutputTargetCacheKey, CachedOutputTarget> pair in OutputTargetCache)
            {
                if (now - pair.Value.CreatedAt > OutputTargetCacheSeconds)
                {
                    OutputTargetCacheRemovalWorkspace.Add(pair.Key);
                }
            }

            foreach (OutputTargetCacheKey key in OutputTargetCacheRemovalWorkspace)
            {
                OutputTargetCache.Remove(key);
            }

            OutputTargetCacheRemovalWorkspace.Clear();
            outputTargetCacheLastPruneTime = now;
            if (OutputTargetCache.Count > MaxOutputTargetCacheEntries)
            {
                // An unusually diverse burst is cheaper and safer to rebuild than to
                // retain an unbounded per-source/per-tag decision table.
                OutputTargetCache.Clear();
            }
        }

        private static int GetCacheSourceId(Storage sourceStorage)
        {
            // Without explicit input reservations, a source that cannot itself be a
            // target does not change the candidate domain. This lets network ports in
            // the same sim frame reuse one ordered decision per tag.
            return sourceStorage != null &&
                   (StorageNetworkInputTargetReservationService.HasInputReservations ||
                    StorageNetworkStorageRules.IsNetworkStorageTarget(sourceStorage))
                ? sourceStorage.GetInstanceID()
                : 0;
        }

        private static bool CanCacheOutputTarget(
            HashSet<Storage> excludedStorages,
            Storage sourceStorage)
        {
            if (excludedStorages == null || excludedStorages.Count == 0)
            {
                return true;
            }

            // An arbitrary exclusion set changes the ordering domain. Cache only the
            // common case where the sole exclusion is represented by the source key.
            return sourceStorage != null &&
                   excludedStorages.Count == 1 &&
                   excludedStorages.Contains(sourceStorage);
        }

        private static bool CanCacheMatchTagSet(
            HashSet<Tag> tags,
            StorageItemUtility.StorageMatchTags matchTags)
        {
            if (tags == null)
            {
                return false;
            }

            int expectedCount = 0;
            if (matchTags.PrefabIdTag != Tag.Invalid)
            {
                expectedCount++;
            }

            if (matchTags.PrefabTag != Tag.Invalid &&
                matchTags.PrefabTag != matchTags.PrefabIdTag)
            {
                expectedCount++;
            }

            if (matchTags.ElementTag != Tag.Invalid &&
                matchTags.ElementTag != matchTags.PrefabIdTag &&
                matchTags.ElementTag != matchTags.PrefabTag)
            {
                expectedCount++;
            }

            if (matchTags.TransferTag != Tag.Invalid &&
                matchTags.TransferTag != matchTags.PrefabIdTag &&
                matchTags.TransferTag != matchTags.PrefabTag &&
                matchTags.TransferTag != matchTags.ElementTag)
            {
                expectedCount++;
            }

            if (tags.Count != expectedCount)
            {
                return false;
            }

            foreach (Tag tag in tags)
            {
                if (!matchTags.Contains(tag))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSourceExcluded(
            HashSet<Storage> excludedStorages,
            Storage sourceStorage)
        {
            return sourceStorage != null &&
                   excludedStorages != null &&
                   excludedStorages.Contains(sourceStorage);
        }

        private readonly struct OutputTargetCacheKey : System.IEquatable<OutputTargetCacheKey>
        {
            private readonly int worldId;
            private readonly Tag prefabIdTag;
            private readonly Tag prefabTag;
            private readonly Tag elementTag;
            private readonly Tag transferTag;
            private readonly int sourceStorageId;
            private readonly bool coldStorage;
            private readonly bool sourceIsExcluded;

            public OutputTargetCacheKey(
                int worldId,
                StorageItemUtility.StorageMatchTags matchTags,
                int sourceStorageId,
                bool coldStorage,
                bool sourceIsExcluded)
            {
                this.worldId = worldId;
                prefabIdTag = matchTags.PrefabIdTag;
                prefabTag = matchTags.PrefabTag;
                elementTag = matchTags.ElementTag;
                transferTag = matchTags.TransferTag;
                this.sourceStorageId = sourceStorageId;
                this.coldStorage = coldStorage;
                this.sourceIsExcluded = sourceIsExcluded;
            }

            public int SourceStorageId => sourceStorageId;

            public bool SourceIsExcluded => sourceIsExcluded;

            public bool Matches(StorageItemUtility.StorageMatchTags matchTags)
            {
                return Contains(matchTags.PrefabIdTag) ||
                       Contains(matchTags.PrefabTag) ||
                       Contains(matchTags.ElementTag) ||
                       Contains(matchTags.TransferTag);
            }

            public bool MatchesExactly(StorageItemUtility.StorageMatchTags matchTags)
            {
                return prefabIdTag == matchTags.PrefabIdTag &&
                       prefabTag == matchTags.PrefabTag &&
                       elementTag == matchTags.ElementTag &&
                       transferTag == matchTags.TransferTag;
            }

            private bool Contains(Tag tag)
            {
                return tag != Tag.Invalid &&
                       (tag == prefabIdTag ||
                        tag == prefabTag ||
                        tag == elementTag ||
                        tag == transferTag);
            }

            public bool Equals(OutputTargetCacheKey other)
            {
                return worldId == other.worldId &&
                       prefabIdTag == other.prefabIdTag &&
                       prefabTag == other.prefabTag &&
                       elementTag == other.elementTag &&
                       transferTag == other.transferTag &&
                       sourceStorageId == other.sourceStorageId &&
                       coldStorage == other.coldStorage &&
                       sourceIsExcluded == other.sourceIsExcluded;
            }

            public override bool Equals(object obj)
            {
                return obj is OutputTargetCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = worldId;
                    hashCode = (hashCode * 397) ^ prefabIdTag.GetHashCode();
                    hashCode = (hashCode * 397) ^ prefabTag.GetHashCode();
                    hashCode = (hashCode * 397) ^ elementTag.GetHashCode();
                    hashCode = (hashCode * 397) ^ transferTag.GetHashCode();
                    hashCode = (hashCode * 397) ^ sourceStorageId;
                    hashCode = (hashCode * 397) ^ coldStorage.GetHashCode();
                    return (hashCode * 397) ^ sourceIsExcluded.GetHashCode();
                }
            }
        }

        private readonly struct CachedOutputTarget
        {
            public CachedOutputTarget(Storage target, float createdAt)
            {
                Target = target;
                CreatedAt = createdAt;
            }

            public Storage Target { get; }

            public float CreatedAt { get; }
        }

        public static Storage FindFoodOutputTarget(
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1,
            Storage sourceStorage = null)
        {
            if (specificTarget != null)
            {
                return FindOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot, sourceWorldId, sourceStorage);
            }

            if (snapshot == null &&
                TryGetCachedOutputTarget(item, matchTags, excludedStorages, sourceWorldId, sourceStorage, true, out Storage cachedTarget))
            {
                return cachedTarget;
            }

            Storage coldTarget;
            if (snapshot == null && sourceWorldId >= 0)
            {
                IReadOnlyList<Storage> candidateStorages =
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages;
                coldTarget = FindColdStorageOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage);
                Storage selectedTarget = coldTarget ?? FindOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage);
                selectedTarget = ValidateSelectedFoodOutputTarget(
                    selectedTarget,
                    item,
                    matchTags,
                    excludedStorages,
                    candidateStorages,
                    sourceWorldId,
                    sourceStorage);
                if (selectedTarget != null)
                {
                    CacheOutputTarget(
                        matchTags,
                        excludedStorages,
                        sourceWorldId,
                        sourceStorage,
                        true,
                        selectedTarget);
                }
                return selectedTarget;
            }

            snapshot = snapshot ?? StorageSceneCollector.Collect();
            List<Storage> storages = new List<Storage>();
            foreach (StorageInfo info in snapshot.Storages)
            {
                if (info?.Minion == null && info.Storage != null)
                {
                    storages.Add(info.Storage);
                }
            }

            coldTarget = FindColdStorageOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId, sourceStorage);
            Storage target = coldTarget ?? FindOutputTargetInStorages(
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage);
            return ValidateSelectedFoodOutputTarget(
                target,
                item,
                matchTags,
                excludedStorages,
                storages,
                sourceWorldId,
                sourceStorage);
        }

        private static Storage FindOutputTargetInStorages(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage)
        {
            using StorageNetworkFrameProfileTool.WorkScope targetSelectionScope =
                StorageNetworkFrameProfileTool.BeginWork(
                    StorageNetworkPerformanceArea.TargetSelection);
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (Storage target in storages)
            {
                if (!IsUsableOutputTarget(target, item, matchTags, excludedStorages, sourceWorldId) ||
                    StorageNetworkInputTargetReservationService.IsReservedForAutoInput(target, sourceStorage) ||
                    !IsAutoOutputMatch(target, matchTags))
                {
                    continue;
                }

                float available = GetAmountAvailableByAnyMatchTag(target, matchTags);
                bool filterAccepting = IsFilterAccepting(target, matchTags);
                float remaining = StorageNetworkContentIndexService.GetRemainingCapacity(target);
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting == bestFilterAccepting && remaining > bestRemaining) ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting == bestFilterAccepting &&
                     Mathf.Approximately(remaining, bestRemaining) &&
                     IsStableTargetBefore(target, best)))
                {
                    best = target;
                    bestAvailable = available;
                    bestFilterAccepting = filterAccepting;
                    bestRemaining = remaining;
                }
            }

            return best;
        }

        private static Storage FindOutputTargetInStorages(
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage)
        {
            using StorageNetworkFrameProfileTool.WorkScope targetSelectionScope =
                StorageNetworkFrameProfileTool.BeginWork(
                    StorageNetworkPerformanceArea.TargetSelection);
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (Storage target in storages)
            {
                if (!IsUsableOutputTarget(target, item, matchTags, excludedStorages, sourceWorldId) ||
                    StorageNetworkInputTargetReservationService.IsReservedForAutoInput(target, sourceStorage) ||
                    !IsAutoOutputMatch(target, matchTags))
                {
                    continue;
                }

                float available = GetAmountAvailableByAnyMatchTag(target, matchTags);
                bool filterAccepting = IsFilterAccepting(target, matchTags);
                float remaining = StorageNetworkContentIndexService.GetRemainingCapacity(target);
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting == bestFilterAccepting && remaining > bestRemaining) ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting == bestFilterAccepting &&
                     Mathf.Approximately(remaining, bestRemaining) &&
                     IsStableTargetBefore(target, best)))
                {
                    best = target;
                    bestAvailable = available;
                    bestFilterAccepting = filterAccepting;
                    bestRemaining = remaining;
                }
            }

            return best;
        }

        private static Storage FindColdStorageOutputTargetInStorages(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage)
        {
            using StorageNetworkFrameProfileTool.WorkScope targetSelectionScope =
                StorageNetworkFrameProfileTool.BeginWork(
                    StorageNetworkPerformanceArea.TargetSelection);
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (Storage target in storages)
            {
                if (!StorageNetworkStorageRules.IsColdStorageServer(target) ||
                    !IsUsableOutputTarget(target, item, matchTags, excludedStorages, sourceWorldId) ||
                    StorageNetworkInputTargetReservationService.IsReservedForAutoInput(target, sourceStorage) ||
                    !IsAutoOutputMatch(target, matchTags))
                {
                    continue;
                }

                float available = GetAmountAvailableByAnyMatchTag(target, matchTags);
                bool filterAccepting = IsFilterAccepting(target, matchTags);
                float remaining = StorageNetworkContentIndexService.GetRemainingCapacity(target);
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting == bestFilterAccepting && remaining > bestRemaining) ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting == bestFilterAccepting &&
                     Mathf.Approximately(remaining, bestRemaining) &&
                     IsStableTargetBefore(target, best)))
                {
                    best = target;
                    bestAvailable = available;
                    bestFilterAccepting = filterAccepting;
                    bestRemaining = remaining;
                }
            }

            return best;
        }

        private static Storage FindColdStorageOutputTargetInStorages(
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage)
        {
            using StorageNetworkFrameProfileTool.WorkScope targetSelectionScope =
                StorageNetworkFrameProfileTool.BeginWork(
                    StorageNetworkPerformanceArea.TargetSelection);
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (Storage target in storages)
            {
                if (!StorageNetworkStorageRules.IsColdStorageServer(target) ||
                    !IsUsableOutputTarget(target, item, matchTags, excludedStorages, sourceWorldId) ||
                    StorageNetworkInputTargetReservationService.IsReservedForAutoInput(target, sourceStorage) ||
                    !IsAutoOutputMatch(target, matchTags))
                {
                    continue;
                }

                float available = GetAmountAvailableByAnyMatchTag(target, matchTags);
                bool filterAccepting = IsFilterAccepting(target, matchTags);
                float remaining = StorageNetworkContentIndexService.GetRemainingCapacity(target);
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting == bestFilterAccepting && remaining > bestRemaining) ||
                    (Mathf.Approximately(available, bestAvailable) &&
                     filterAccepting == bestFilterAccepting &&
                     Mathf.Approximately(remaining, bestRemaining) &&
                     IsStableTargetBefore(target, best)))
                {
                    best = target;
                    bestAvailable = available;
                    bestFilterAccepting = filterAccepting;
                    bestRemaining = remaining;
                }
            }

            return best;
        }

        public static Storage FindElementOutputTarget(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages = null,
            Storage specificTarget = null,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            List<Storage> targets = FindElementOutputTargets(elementHash, excludedStorages, specificTarget, snapshot, sourceWorldId);
            return targets.Count > 0 ? targets[0] : null;
        }

        public static List<Storage> FindElementOutputTargets(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages = null,
            Storage specificTarget = null,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            Element element = ElementLoader.FindElementByHash(elementHash);
            if (element == null)
            {
                return new List<Storage>();
            }

            Tag tag = elementHash.CreateTag();
            HashSet<Storage> excluded = excludedStorages ?? new HashSet<Storage>();
            if (specificTarget != null)
            {
                return IsUsableElementOutputTarget(specificTarget, element, tag, excluded, sourceWorldId)
                    ? new List<Storage> { specificTarget }
                    : new List<Storage>();
            }

            if (snapshot == null && sourceWorldId >= 0)
            {
                List<Storage> lightweightTargets = new List<Storage>();
                foreach (Storage target in StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages)
                {
                    if (IsUsableElementOutputTarget(target, element, tag, excluded, sourceWorldId))
                    {
                        lightweightTargets.Add(target);
                    }
                }

                SortElementTargets(lightweightTargets, tag);
                return lightweightTargets;
            }

            snapshot = snapshot ?? (sourceWorldId >= 0 ? StorageSceneCollector.CollectForWorld(sourceWorldId) : StorageSceneCollector.Collect());
            List<Storage> targets = new List<Storage>();
            foreach (StorageInfo info in snapshot.Storages)
            {
                Storage target = info?.Storage;
                if (info?.Minion == null && IsUsableElementOutputTarget(target, element, tag, excluded, sourceWorldId))
                {
                    targets.Add(target);
                }
            }

            SortElementTargets(targets, tag);
            return targets;
        }

        public static bool HasElementOutputCandidateIgnoringCapacity(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages = null,
            Storage specificTarget = null,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            Element element = ElementLoader.FindElementByHash(elementHash);
            if (element == null)
            {
                return false;
            }

            Tag tag = elementHash.CreateTag();
            HashSet<Storage> excluded = excludedStorages ?? new HashSet<Storage>();
            if (specificTarget != null)
            {
                return IsElementOutputTargetCandidate(specificTarget, element, tag, excluded, sourceWorldId, false);
            }

            if (snapshot == null && sourceWorldId >= 0)
            {
                foreach (Storage target in StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages)
                {
                    if (IsElementOutputTargetCandidate(target, element, tag, excluded, sourceWorldId, false))
                    {
                        return true;
                    }
                }

                return false;
            }

            snapshot = snapshot ?? (sourceWorldId >= 0 ? StorageSceneCollector.CollectForWorld(sourceWorldId) : StorageSceneCollector.Collect());
            foreach (StorageInfo info in snapshot.Storages)
            {
                Storage target = info?.Storage;
                if (info?.Minion == null && IsElementOutputTargetCandidate(target, element, tag, excluded, sourceWorldId, false))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<Storage> FindNetworkSources(
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            Storage specificSource,
            int destinationWorldId)
        {
            List<Storage> sources = new List<Storage>();
            if (specificSource != null)
            {
                if (IsUsableNetworkSource(specificSource, wantedTags, excludedStorages, destinationWorldId))
                {
                    sources.Add(specificSource);
                }

                return sources;
            }

            foreach (Storage storage in StorageNetworkSourceIndexService.GetSourceStorages(destinationWorldId, true, wantedTags, excludedStorages))
            {
                if (IsUsableNetworkSource(storage, wantedTags, excludedStorages, destinationWorldId))
                {
                    sources.Add(storage);
                }
            }

            return sources;
        }

        public static HashSet<Storage> BuildExclusionSet(IEnumerable<Storage> excludedStorages)
        {
            HashSet<Storage> excluded = new HashSet<Storage>();
            if (excludedStorages == null)
            {
                return excluded;
            }

            foreach (Storage storage in excludedStorages)
            {
                if (storage != null)
                {
                    excluded.Add(storage);
                }
            }

            return excluded;
        }

        public static int GetObjectWorldId(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return -1;
            }

            int worldId = gameObject.GetMyWorldId();
            if (worldId != byte.MaxValue && worldId >= 0)
            {
                return worldId;
            }

            int cell = Grid.PosToCell(gameObject);
            return Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
        }

        public static string DescribeElementOutputTargetFailure(
            SimHashes elementHash,
            Storage specificTarget = null,
            int sourceWorldId = -1)
        {
            Element element = ElementLoader.FindElementByHash(elementHash);
            if (element == null)
            {
                return "element not found";
            }

            Tag tag = elementHash.CreateTag();
            HashSet<Storage> excluded = new HashSet<Storage>();
            List<string> reasons = new List<string>();

            if (specificTarget != null)
            {
                reasons.Add(DescribeElementTargetCandidate(specificTarget, tag, excluded, sourceWorldId));
                return string.Join("; ", reasons);
            }

            IEnumerable<Storage> candidates = sourceWorldId >= 0
                ? StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages
                : StorageSceneCollector.Collect().Storages
                    .Where(info => info?.Minion == null && info.Storage != null)
                    .Select(info => info.Storage);

            foreach (Storage candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                reasons.Add(DescribeElementTargetCandidate(candidate, tag, excluded, sourceWorldId));
            }

            if (reasons.Count == 0)
            {
                return "no storage candidates collected";
            }

            return string.Join("; ", reasons);
        }

        public static ElementOutputTargetQuery FindElementOutputTargetsWithCapacityState(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages = null,
            Storage specificTarget = null,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            ElementOutputTargetQuery result = new ElementOutputTargetQuery();
            FillElementOutputTargetsWithCapacityState(
                elementHash,
                excludedStorages,
                specificTarget,
                snapshot,
                sourceWorldId,
                result);
            return result;
        }

        internal static void FillElementOutputTargetsWithCapacityState(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot,
            int sourceWorldId,
            ElementOutputTargetQuery result)
        {
            result.Reset();
            Element element = ElementLoader.FindElementByHash(elementHash);
            if (element == null)
            {
                return;
            }

            Tag tag = elementHash.CreateTag();
            HashSet<Storage> excluded = excludedStorages ?? EmptyStorageExclusions;
            if (specificTarget != null)
            {
                if (IsElementOutputTargetStructuralCandidate(
                        specificTarget,
                        element,
                        tag,
                        excluded,
                        sourceWorldId))
                {
                    result.Candidates.Add(specificTarget);
                    if (IsElementOutputTargetCandidate(
                            specificTarget,
                            element,
                            tag,
                            excluded,
                            sourceWorldId,
                            false))
                    {
                        result.HasCandidateIgnoringCapacity = true;
                        if (IsUsableElementOutputTarget(
                                specificTarget,
                                element,
                                tag,
                                excluded,
                                sourceWorldId))
                        {
                            result.Targets.Add(specificTarget);
                        }
                    }
                }

                return;
            }

            if (snapshot == null && sourceWorldId >= 0)
            {
                foreach (Storage target in StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages)
                {
                    AddElementTargetQueryResult(target, element, tag, excluded, sourceWorldId, result);
                }
            }
            else
            {
                snapshot = snapshot ?? (sourceWorldId >= 0 ? StorageSceneCollector.CollectForWorld(sourceWorldId) : StorageSceneCollector.Collect());
                foreach (StorageInfo info in snapshot.Storages)
                {
                    if (info?.Minion == null)
                    {
                        AddElementTargetQueryResult(info.Storage, element, tag, excluded, sourceWorldId, result);
                    }
                }
            }

            SortElementTargets(result.Targets, tag);
            SortElementTargets(result.Candidates, tag);
        }

        internal static void RefreshElementOutputTargetCapacities(
            SimHashes elementHash,
            ElementOutputTargetQuery result,
            HashSet<Storage> excludedStorages,
            int sourceWorldId)
        {
            if (result == null)
            {
                return;
            }

            Element element = ElementLoader.FindElementByHash(elementHash);
            Tag tag = elementHash.CreateTag();
            HashSet<Storage> excluded = excludedStorages ?? EmptyStorageExclusions;
            result.Targets.Clear();
            result.HasCandidateIgnoringCapacity = false;
            foreach (Storage target in result.Candidates)
            {
                if (!IsElementOutputTargetCandidate(
                        target,
                        element,
                        tag,
                        excluded,
                        sourceWorldId,
                        false))
                {
                    continue;
                }

                result.HasCandidateIgnoringCapacity = true;
                if (StorageNetworkContentIndexService.GetRemainingCapacity(target) >
                    PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    result.Targets.Add(target);
                }
            }

            SortElementTargets(result.Targets, tag);
        }

        private static void AddElementTargetQueryResult(
            Storage target,
            Element element,
            Tag tag,
            HashSet<Storage> excluded,
            int sourceWorldId,
            ElementOutputTargetQuery result)
        {
            if (!IsElementOutputTargetStructuralCandidate(
                    target,
                    element,
                    tag,
                    excluded,
                    sourceWorldId))
            {
                return;
            }

            result.Candidates.Add(target);
            if (IsElementOutputTargetCandidate(
                    target,
                    element,
                    tag,
                    excluded,
                    sourceWorldId,
                    false))
            {
                result.HasCandidateIgnoringCapacity = true;
                if (StorageNetworkContentIndexService.GetRemainingCapacity(target) >
                    PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    result.Targets.Add(target);
                }
            }
        }

        private static bool IsStableTargetBefore(Storage candidate, Storage current)
        {
            return StorageItemUtility.GetStorageInstanceId(candidate) <
                   StorageItemUtility.GetStorageInstanceId(current);
        }

        private static void SortElementTargets(List<Storage> targets, Tag tag)
        {
            if (targets == null || targets.Count <= 1)
            {
                return;
            }

            ElementTargetsComparer.Tag = tag;
            targets.Sort(ElementTargetsComparer);
        }

        private sealed class ElementTargetComparer : IComparer<Storage>
        {
            public Tag Tag { get; set; }

            public int Compare(Storage left, Storage right)
            {
                return CompareElementTargets(left, right, Tag);
            }
        }

    }

    internal sealed class ElementOutputTargetQuery
    {
        public readonly List<Storage> Targets = new List<Storage>();
        public readonly List<Storage> Candidates = new List<Storage>();
        public bool HasCandidateIgnoringCapacity;

        public void Reset()
        {
            Targets.Clear();
            Candidates.Clear();
            HasCandidateIgnoringCapacity = false;
        }
    }
}
