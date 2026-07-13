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
        private static readonly Dictionary<OutputTargetCacheKey, CachedOutputTarget> OutputTargetCache =
            new Dictionary<OutputTargetCacheKey, CachedOutputTarget>();
        private static int outputTargetCacheRegistryVersion = -1;

        public static void ResetRuntimeState()
        {
            OutputTargetCache.Clear();
            outputTargetCacheRegistryVersion = -1;
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

            foreach (Tag tag in StorageItemUtility.GetStorageMatchTags(item))
            {
                if (allowedTags.Contains(tag))
                {
                    return true;
                }
            }

            return false;
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

            if (TryGetCachedOutputTarget(item, cacheMatchTags, excludedStorages, sourceWorldId, sourceStorage, false, out Storage cachedTarget))
            {
                return cachedTarget;
            }

            Storage target;
            if (snapshot == null && sourceWorldId >= 0)
            {
                target = FindOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages,
                    sourceWorldId,
                    sourceStorage);
                CacheOutputTarget(cacheMatchTags, sourceWorldId, false, target);
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
            CacheOutputTarget(cacheMatchTags, sourceWorldId, false, target);
            return target;
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

            if (TryGetCachedOutputTarget(item, matchTags, excludedStorages, sourceWorldId, sourceStorage, false, out Storage cachedTarget))
            {
                return cachedTarget;
            }

            Storage target;
            if (snapshot == null && sourceWorldId >= 0)
            {
                target = FindOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages,
                    sourceWorldId,
                    sourceStorage);
                CacheOutputTarget(matchTags, sourceWorldId, false, target);
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
            CacheOutputTarget(matchTags, sourceWorldId, false, target);
            return target;
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

            if (TryGetCachedOutputTarget(item, cacheMatchTags, excludedStorages, sourceWorldId, sourceStorage, true, out Storage cachedTarget))
            {
                return cachedTarget;
            }

            Storage coldTarget;
            if (snapshot == null && sourceWorldId >= 0)
            {
                coldTarget = FindColdStorageOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages,
                    sourceWorldId,
                    sourceStorage);
                if (coldTarget != null)
                {
                    CacheOutputTarget(cacheMatchTags, sourceWorldId, true, coldTarget);
                    return coldTarget;
                }

                return FindOutputTarget(item, matchTags, excludedStorages, null, null, sourceWorldId, sourceStorage);
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
            if (coldTarget != null)
            {
                CacheOutputTarget(cacheMatchTags, sourceWorldId, true, coldTarget);
                return coldTarget;
            }

            return FindOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId, sourceStorage);
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
            OutputTargetCacheKey key = new OutputTargetCacheKey(sourceWorldId, matchTags.TransferTag, coldStorage);
            if (OutputTargetCache.TryGetValue(key, out CachedOutputTarget cached) &&
                Time.unscaledTime - cached.CreatedAt <= OutputTargetCacheSeconds &&
                cached.Target != null &&
                (!coldStorage || StorageNetworkStorageRules.IsColdStorageServer(cached.Target)) &&
                IsUsableOutputTarget(cached.Target, item, matchTags, excludedStorages, sourceWorldId) &&
                !StorageNetworkInputTargetReservationService.IsReservedForAutoInput(cached.Target, sourceStorage) &&
                IsAutoOutputMatch(cached.Target, matchTags))
            {
                target = cached.Target;
                return true;
            }

            OutputTargetCache.Remove(key);
            target = null;
            return false;
        }

        private static void CacheOutputTarget(
            StorageItemUtility.StorageMatchTags matchTags,
            int sourceWorldId,
            bool coldStorage,
            Storage target)
        {
            if (target == null || matchTags.TransferTag == Tag.Invalid)
            {
                return;
            }

            RefreshOutputTargetCacheVersion();
            OutputTargetCache[new OutputTargetCacheKey(sourceWorldId, matchTags.TransferTag, coldStorage)] =
                new CachedOutputTarget(target, Time.unscaledTime);
        }

        private static void RefreshOutputTargetCacheVersion()
        {
            int registryVersion = StorageSceneRegistry.Version;
            if (outputTargetCacheRegistryVersion == registryVersion)
            {
                return;
            }

            OutputTargetCache.Clear();
            outputTargetCacheRegistryVersion = registryVersion;
        }

        private readonly struct OutputTargetCacheKey : System.IEquatable<OutputTargetCacheKey>
        {
            private readonly int worldId;
            private readonly Tag transferTag;
            private readonly bool coldStorage;

            public OutputTargetCacheKey(int worldId, Tag transferTag, bool coldStorage)
            {
                this.worldId = worldId;
                this.transferTag = transferTag;
                this.coldStorage = coldStorage;
            }

            public bool Equals(OutputTargetCacheKey other)
            {
                return worldId == other.worldId && transferTag == other.transferTag && coldStorage == other.coldStorage;
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
                    hashCode = (hashCode * 397) ^ transferTag.GetHashCode();
                    return (hashCode * 397) ^ coldStorage.GetHashCode();
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

            if (TryGetCachedOutputTarget(item, matchTags, excludedStorages, sourceWorldId, sourceStorage, true, out Storage cachedTarget))
            {
                return cachedTarget;
            }

            Storage coldTarget;
            if (snapshot == null && sourceWorldId >= 0)
            {
                coldTarget = FindColdStorageOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages,
                    sourceWorldId,
                    sourceStorage);
                if (coldTarget != null)
                {
                    CacheOutputTarget(matchTags, sourceWorldId, true, coldTarget);
                    return coldTarget;
                }

                return FindOutputTarget(item, matchTags, excludedStorages, null, null, sourceWorldId, sourceStorage);
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
            if (coldTarget != null)
            {
                CacheOutputTarget(matchTags, sourceWorldId, true, coldTarget);
                return coldTarget;
            }

            return FindOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId, sourceStorage);
        }

        private static Storage FindOutputTargetInStorages(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId,
            Storage sourceStorage)
        {
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
                float remaining = target.RemainingCapacity();
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting == bestFilterAccepting && remaining > bestRemaining))
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
                float remaining = target.RemainingCapacity();
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting == bestFilterAccepting && remaining > bestRemaining))
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
                float remaining = target.RemainingCapacity();
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting == bestFilterAccepting && remaining > bestRemaining))
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
                float remaining = target.RemainingCapacity();
                if (best == null ||
                    available > bestAvailable ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting && !bestFilterAccepting) ||
                    (Mathf.Approximately(available, bestAvailable) && filterAccepting == bestFilterAccepting && remaining > bestRemaining))
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

                lightweightTargets.Sort((left, right) => CompareElementTargets(right, left, tag));
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

            targets.Sort((left, right) => CompareElementTargets(right, left, tag));
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
            Element element = ElementLoader.FindElementByHash(elementHash);
            if (element == null)
            {
                return result;
            }

            Tag tag = elementHash.CreateTag();
            HashSet<Storage> excluded = excludedStorages ?? new HashSet<Storage>();
            if (specificTarget != null)
            {
                if (IsElementOutputTargetCandidate(specificTarget, element, tag, excluded, sourceWorldId, false))
                {
                    result.HasCandidateIgnoringCapacity = true;
                    if (IsUsableElementOutputTarget(specificTarget, element, tag, excluded, sourceWorldId))
                    {
                        result.Targets.Add(specificTarget);
                    }
                }

                return result;
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

            result.Targets.Sort((left, right) => CompareElementTargets(right, left, tag));
            return result;
        }

        private static void AddElementTargetQueryResult(
            Storage target,
            Element element,
            Tag tag,
            HashSet<Storage> excluded,
            int sourceWorldId,
            ElementOutputTargetQuery result)
        {
            if (!IsElementOutputTargetCandidate(target, element, tag, excluded, sourceWorldId, false))
            {
                return;
            }

            result.HasCandidateIgnoringCapacity = true;
            if (target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                result.Targets.Add(target);
            }
        }

    }

    internal sealed class ElementOutputTargetQuery
    {
        public readonly List<Storage> Targets = new List<Storage>();
        public bool HasCandidateIgnoringCapacity;
    }
}
