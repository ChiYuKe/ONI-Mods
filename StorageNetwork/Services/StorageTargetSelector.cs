using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageTargetSelector
    {
        public static bool MatchesAllowedTags(GameObject item, HashSet<Tag> allowedTags)
        {
            if (allowedTags == null || allowedTags.Count == 0)
            {
                return true;
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

            snapshot = snapshot ?? StorageSceneCollector.Collect();
            Storage best = null;
            float bestAvailable = 0f;
            bool bestFilterAccepting = false;
            float bestRemaining = 0f;
            foreach (StorageInfo info in snapshot.Storages)
            {
                Storage target = info?.Storage;
                if (info?.Minion != null ||
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
                return IsUsableElementOutputTarget(specificTarget, tag, excluded, sourceWorldId)
                    ? new List<Storage> { specificTarget }
                    : new List<Storage>();
            }

            snapshot = snapshot ?? (sourceWorldId >= 0 ? StorageSceneCollector.CollectForWorld(sourceWorldId) : StorageSceneCollector.Collect());
            List<Storage> targets = new List<Storage>();
            foreach (StorageInfo info in snapshot.Storages)
            {
                Storage target = info?.Storage;
                if (info?.Minion == null && IsUsableElementOutputTarget(target, tag, excluded, sourceWorldId))
                {
                    targets.Add(target);
                }
            }

            targets.Sort((left, right) => CompareElementTargets(right, left, tag));
            return targets;
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

            foreach (StorageInfo info in StorageSceneCollector.CollectForWorld(destinationWorldId).Storages)
            {
                Storage storage = info?.Storage;
                if (info?.Minion == null && IsUsableNetworkSource(storage, wantedTags, excludedStorages, destinationWorldId))
                {
                    sources.Add(storage);
                }
            }

            sources.Sort((left, right) => GetAmountAvailableByAnyMatchTag(right, wantedTags).CompareTo(GetAmountAvailableByAnyMatchTag(left, wantedTags)));
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

        private static int CompareElementTargets(Storage left, Storage right, Tag tag)
        {
            float leftAvailable = left.GetAmountAvailable(tag);
            float rightAvailable = right.GetAmountAvailable(tag);
            int compare = leftAvailable.CompareTo(rightAvailable);
            if (compare != 0)
            {
                return compare;
            }

            compare = IsFilterAccepting(left, tag).CompareTo(IsFilterAccepting(right, tag));
            if (compare != 0)
            {
                return compare;
            }

            return left.RemainingCapacity().CompareTo(right.RemainingCapacity());
        }

        private static bool IsUsableOutputTarget(Storage target, GameObject item, HashSet<Tag> matchTags, HashSet<Storage> excludedStorages, int sourceWorldId = -1)
        {
            return target != null &&
                   IsStorageReachableFromWorld(target, sourceWorldId) &&
                   !excludedStorages.Contains(target) &&
                   StorageNetworkStorageRules.IsServerStorage(target) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(target) &&
                   !StorageNetworkStorageRules.IsMinionStorage(target) &&
                   !StorageNetworkStorageRules.IsProductionStorage(target) &&
                   target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                   IsStorageAccepting(target, matchTags) &&
                   (target.items == null || !target.items.Contains(item));
        }

        private static bool IsUsableElementOutputTarget(Storage target, Tag tag, HashSet<Storage> excludedStorages, int sourceWorldId = -1)
        {
            return target != null &&
                   tag != Tag.Invalid &&
                   IsStorageReachableFromWorld(target, sourceWorldId) &&
                   !excludedStorages.Contains(target) &&
                   StorageNetworkStorageRules.IsServerStorage(target) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(target) &&
                   !StorageNetworkStorageRules.IsMinionStorage(target) &&
                   !StorageNetworkStorageRules.IsProductionStorage(target) &&
                   target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                   IsStorageAccepting(target, tag) &&
                   (target.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    IsFilterAccepting(target, tag) ||
                    HasNoExplicitStorageFilter(target));
        }

        private static bool IsUsableNetworkSource(Storage source, IEnumerable<Tag> tags, HashSet<Storage> excludedStorages, int destinationWorldId)
        {
            return source != null &&
                   IsStorageReachableFromWorld(source, destinationWorldId) &&
                   !excludedStorages.Contains(source) &&
                   StorageNetworkStorageRules.IsServerStorage(source) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(source) &&
                   !StorageNetworkStorageRules.IsMinionStorage(source) &&
                   !StorageNetworkStorageRules.IsProductionStorage(source) &&
                   GetAmountAvailableByAnyMatchTag(source, tags) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static bool IsStorageReachableFromWorld(Storage storage, int worldId)
        {
            if (storage == null)
            {
                return false;
            }

            if (worldId < 0 || StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                return true;
            }

            return GetObjectWorldId(storage.gameObject) == worldId;
        }

        private static bool IsAutoOutputMatch(Storage target, HashSet<Tag> matchTags)
        {
            return GetAmountAvailableByAnyMatchTag(target, matchTags) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                   IsFilterAccepting(target, matchTags) ||
                   HasNoExplicitStorageFilter(target);
        }

        private static bool IsFilterAccepting(Storage target, HashSet<Tag> matchTags)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags);
            }

            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            if (IsEmptyFilteredPort(target, filterable))
            {
                return false;
            }

            return filterable != null && AnyTagAcceptedByTreeFilter(filterable, target.storageFilters, matchTags);
        }

        private static bool IsFilterAccepting(Storage target, Tag tag)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return IsStorageFilterAcceptingTag(target.storageFilters, tag);
            }

            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            if (IsEmptyFilteredPort(target, filterable))
            {
                return false;
            }

            return filterable != null && IsFilterAcceptingTag(filterable, tag, target.storageFilters);
        }

        private static bool IsStorageAccepting(Storage target, HashSet<Tag> matchTags)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags);
            }

            if (IsFilteredPort(target))
            {
                return IsFilterAccepting(target, matchTags);
            }

            return target != null &&
                   (IsFilterAccepting(target, matchTags) ||
                    target.storageFilters == null ||
                    target.storageFilters.Count == 0 ||
                   AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags));
        }

        private static bool IsStorageAccepting(Storage target, Tag tag)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return IsStorageFilterAcceptingTag(target.storageFilters, tag);
            }

            if (IsFilteredPort(target))
            {
                return IsFilterAccepting(target, tag);
            }

            return target != null &&
                   (IsFilterAccepting(target, tag) ||
                    target.storageFilters == null ||
                    target.storageFilters.Count == 0 ||
                    IsStorageFilterAcceptingTag(target.storageFilters, tag));
        }

        private static bool IsFilterAcceptingTag(TreeFilterable filterable, Tag tag, List<Tag> storageFilters)
        {
            return filterable != null &&
                   (filterable.ContainsTag(tag) ||
                    filterable.AcceptedTags.Any(accepted => IsDiscoveredCategoryAccepting(accepted, tag)) ||
                    (filterable.AcceptedTags.Count == 0 && IsStorageFilterAcceptingTag(storageFilters, tag)));
        }

        private static bool IsStorageFilterAcceptingTag(List<Tag> storageFilters, Tag tag)
        {
            return storageFilters == null ||
                   storageFilters.Count == 0 ||
                   storageFilters.Contains(tag) ||
                   storageFilters.Any(filter => IsDiscoveredCategoryAccepting(filter, tag));
        }

        private static bool IsDiscoveredCategoryAccepting(Tag categoryTag, Tag itemTag)
        {
            return categoryTag == itemTag ||
                   DiscoveredResources.Instance != null &&
                   DiscoveredResources.Instance.GetDiscoveredResourcesFromTag(categoryTag).Contains(itemTag);
        }

        private static bool AnyTagAcceptedByTreeFilter(TreeFilterable filterable, List<Tag> storageFilters, IEnumerable<Tag> tags)
        {
            if (tags == null)
            {
                return false;
            }

            foreach (Tag tag in tags)
            {
                if (IsFilterAcceptingTag(filterable, tag, storageFilters))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyTagAcceptedByStorageFilter(List<Tag> storageFilters, IEnumerable<Tag> tags)
        {
            if (tags == null)
            {
                return false;
            }

            foreach (Tag tag in tags)
            {
                if (IsStorageFilterAcceptingTag(storageFilters, tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetAmountAvailableByAnyMatchTag(Storage target, IEnumerable<Tag> matchTags)
        {
            if (target == null || matchTags == null)
            {
                return 0f;
            }

            float available = 0f;
            foreach (Tag tag in matchTags)
            {
                available = Mathf.Max(available, target.GetAmountAvailable(tag));
            }

            return available;
        }

        private static bool HasNoExplicitStorageFilter(Storage target)
        {
            if (IsFilteredPort(target))
            {
                return false;
            }

            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            return (filterable == null || filterable.GetTags() == null || filterable.GetTags().Count == 0) &&
                   (target.storageFilters == null || target.storageFilters.Count == 0);
        }

        private static bool IsFilteredPort(Storage target)
        {
            return target != null &&
                   target.GetComponent<StorageNetworkPort>() != null &&
                   target.GetComponent<TreeFilterable>() != null;
        }

        private static bool IsEmptyFilteredPort(Storage target, TreeFilterable filterable)
        {
            return IsFilteredPort(target) &&
                   (filterable == null || filterable.AcceptedTags == null || filterable.AcceptedTags.Count == 0);
        }
    }
}
