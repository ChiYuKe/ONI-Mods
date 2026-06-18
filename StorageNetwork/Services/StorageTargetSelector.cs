using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static partial class StorageTargetSelector
    {
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

        public static Storage FindOutputTarget(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            if (specificTarget != null)
            {
                return IsUsableOutputTarget(specificTarget, item, matchTags, excludedStorages, sourceWorldId) ? specificTarget : null;
            }

            if (snapshot == null && sourceWorldId >= 0)
            {
                return FindOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages,
                    sourceWorldId);
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

            return FindOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId);
        }

        public static Storage FindFoodOutputTarget(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            if (specificTarget != null)
            {
                return FindOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot, sourceWorldId);
            }

            Storage coldTarget;
            if (snapshot == null && sourceWorldId >= 0)
            {
                coldTarget = FindColdStorageOutputTargetInStorages(
                    item,
                    matchTags,
                    excludedStorages,
                    StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages,
                    sourceWorldId);
                return coldTarget ?? FindOutputTarget(item, matchTags, excludedStorages, null, null, sourceWorldId);
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

            coldTarget = FindColdStorageOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId);
            return coldTarget ?? FindOutputTargetInStorages(item, matchTags, excludedStorages, storages, sourceWorldId);
        }

        private static Storage FindOutputTargetInStorages(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            IEnumerable<Storage> storages,
            int sourceWorldId)
        {
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (Storage target in storages)
            {
                if (!IsUsableOutputTarget(target, item, matchTags, excludedStorages, sourceWorldId) ||
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
            int sourceWorldId)
        {
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (Storage target in storages)
            {
                if (!StorageNetworkStorageRules.IsColdStorageServer(target) ||
                    !IsUsableOutputTarget(target, item, matchTags, excludedStorages, sourceWorldId) ||
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

    }
}
