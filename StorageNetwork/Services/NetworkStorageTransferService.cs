using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.Services
{
    internal static class NetworkStorageTransferService
    {
        public static StorageTransferResult TransferStoredItemsToNetwork(
            Storage source,
            IEnumerable<Storage> excludedStorages,
            Storage specificTarget = null,
            HashSet<Tag> allowedTags = null)
        {
            if (source == null || source.items == null)
            {
                return StorageTransferResult.Idle;
            }

            HashSet<Storage> excluded = BuildExclusionSet(excludedStorages);
            float totalMoved = 0f;
            string blockedItem = null;
            StorageSceneSnapshot snapshot = specificTarget == null ? StorageSceneCollector.Collect() : null;
            List<GameObject> items = new List<GameObject>(source.items.Count);
            foreach (GameObject item in source.items)
            {
                if (item != null)
                {
                    items.Add(item);
                }
            }

            foreach (GameObject item in items)
            {
                if (!MatchesAllowedTags(item, allowedTags))
                {
                    continue;
                }

                StorageTransferResult result = TransferStoredItem(source, item, excluded, specificTarget, snapshot);
                totalMoved += result.MovedKg;
                if (!string.IsNullOrEmpty(result.BlockedItem))
                {
                    blockedItem = result.BlockedItem;
                }
            }

            return new StorageTransferResult(totalMoved, blockedItem);
        }

        public static StorageTransferResult TransferLooseItemToNetwork(
            GameObject item,
            IEnumerable<Storage> excludedStorages,
            Storage specificTarget = null,
            HashSet<Tag> allowedTags = null)
        {
            if (item == null || StorageItemUtility.GetMass(item) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return StorageTransferResult.Idle;
            }

            Tag tag = StorageItemUtility.GetStorageTransferTag(item);
            if (!MatchesAllowedTags(item, allowedTags))
            {
                return StorageTransferResult.Idle;
            }

            HashSet<Tag> matchTags = StorageItemUtility.GetStorageMatchTags(item);
            HashSet<Storage> excluded = BuildExclusionSet(excludedStorages);
            Pickupable pickupable = item.GetComponent<Pickupable>();
            Storage target = FindOutputTarget(item, matchTags, excluded, specificTarget);
            if (pickupable == null || target == null)
            {
                return StorageTransferResult.Blocked(StorageItemUtility.GetItemDisplayName(item, tag));
            }

            float moved = 0f;
            while (target != null && item != null && StorageItemUtility.GetMass(item) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                float transferAmount = Mathf.Min(StorageItemUtility.GetMass(item), Mathf.Max(0f, target.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                Pickupable taken = pickupable.Take(transferAmount);
                if (taken == null)
                {
                    break;
                }

                float takenMass = StorageItemUtility.GetMass(taken.gameObject);
                if (takenMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                target.Store(taken.gameObject, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
                moved += takenMass;
                if (taken.gameObject == item)
                {
                    break;
                }
            }

            return moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? new StorageTransferResult(moved, null)
                : StorageTransferResult.Blocked(StorageItemUtility.GetItemDisplayName(item, tag));
        }

        public static float TransferFromNetworkToStorage(
            IEnumerable<Tag> tags,
            float amount,
            Storage destination,
            IEnumerable<Storage> excludedStorages = null)
        {
            if (tags == null ||
                destination == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            HashSet<Tag> wantedTags = new HashSet<Tag>();
            foreach (Tag tag in tags)
            {
                if (tag != Tag.Invalid)
                {
                    wantedTags.Add(tag);
                }
            }

            if (wantedTags.Count == 0)
            {
                return 0f;
            }

            HashSet<Storage> excluded = BuildExclusionSet(excludedStorages);
            excluded.Add(destination);
            List<Storage> sources = new List<Storage>();
            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage storage = info?.Storage;
                if (info?.Minion == null && IsUsableNetworkSource(storage, wantedTags, excluded))
                {
                    sources.Add(storage);
                }
            }

            sources.Sort((left, right) => GetAmountAvailableByAnyMatchTag(right, wantedTags).CompareTo(GetAmountAvailableByAnyMatchTag(left, wantedTags)));

            float moved = 0f;
            foreach (Storage source in sources)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                foreach (Tag tag in wantedTags)
                {
                    if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                        destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    float sourceAmount = source.GetAmountAvailable(tag);
                    if (sourceAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    float transferAmount = Mathf.Min(amount - moved, sourceAmount, Mathf.Max(0f, destination.RemainingCapacity()));
                    if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    moved += source.Transfer(destination, tag, transferAmount, block_events: false, hide_popups: true);
                }
            }

            return moved;
        }

        public static string FormatOutputStatus(StorageTransferResult result, string idleText)
        {
            if (result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER_STATUS_MOVED), GameUtil.GetFormattedMass(result.MovedKg));
            }

            if (!string.IsNullOrEmpty(result.BlockedItem))
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER_STATUS_BLOCKED), result.BlockedItem);
            }

            return idleText;
        }

        public static Storage FindElementOutputTarget(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages = null,
            Storage specificTarget = null,
            StorageSceneSnapshot snapshot = null)
        {
            List<Storage> targets = FindElementOutputTargets(elementHash, excludedStorages, specificTarget, snapshot);
            return targets.Count > 0 ? targets[0] : null;
        }

        public static List<Storage> FindElementOutputTargets(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages = null,
            Storage specificTarget = null,
            StorageSceneSnapshot snapshot = null)
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
                return IsUsableElementOutputTarget(specificTarget, tag, excluded)
                    ? new List<Storage> { specificTarget }
                    : new List<Storage>();
            }

            snapshot = snapshot ?? StorageSceneCollector.Collect();
            List<Storage> targets = new List<Storage>();
            foreach (StorageInfo info in snapshot.Storages)
            {
                Storage target = info?.Storage;
                if (info?.Minion == null && IsUsableElementOutputTarget(target, tag, excluded))
                {
                    targets.Add(target);
                }
            }

            targets.Sort((left, right) => CompareElementTargets(right, left, tag));
            return targets;
        }

        private static StorageTransferResult TransferStoredItem(
            Storage source,
            GameObject item,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot)
        {
            float mass = StorageItemUtility.GetMass(item);
            if (mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return StorageTransferResult.Idle;
            }

            Tag tag = StorageItemUtility.GetStorageTransferTag(item);
            HashSet<Tag> matchTags = StorageItemUtility.GetStorageMatchTags(item);
            float remaining = mass;
            float moved = 0f;
            Storage target = FindOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot);
            if (target == null)
            {
                return StorageTransferResult.Blocked(StorageItemUtility.GetItemDisplayName(item, tag));
            }

            while (target != null && remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                float transferAmount = Mathf.Min(remaining, Mathf.Max(0f, target.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float transferred = TransferStoredObject(source, target, item, transferAmount);
                if (transferred <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                moved += transferred;
                remaining -= transferred;
                if (item == null || !source.items.Contains(item))
                {
                    break;
                }
            }

            return remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? new StorageTransferResult(moved, StorageItemUtility.GetItemDisplayName(item, tag))
                : new StorageTransferResult(moved, null);
        }

        private static bool MatchesAllowedTags(GameObject item, HashSet<Tag> allowedTags)
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

        private static float TransferStoredObject(Storage source, Storage target, GameObject item, float amount)
        {
            if (source == null || target == null || item == null || !source.items.Contains(item))
            {
                return 0f;
            }

            float mass = StorageItemUtility.GetMass(item);
            if (mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            float transferAmount = Mathf.Min(amount, mass);
            if (transferAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= mass)
            {
                return source.Transfer(item, target, block_events: false, hide_popups: true) ? mass : 0f;
            }

            Pickupable pickupable = item.GetComponent<Pickupable>();
            Pickupable taken = pickupable != null ? pickupable.Take(transferAmount) : null;
            if (taken == null)
            {
                return 0f;
            }

            float moved = StorageItemUtility.GetMass(taken.gameObject);
            if (moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            target.Store(taken.gameObject, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
            source.Trigger(-1697596308, item);
            source.OnStorageChange?.Invoke(item);
            return moved;
        }

        private static Storage FindOutputTarget(
            GameObject item,
            HashSet<Tag> matchTags,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot = null)
        {
            if (specificTarget != null)
            {
                return IsUsableOutputTarget(specificTarget, item, matchTags, excludedStorages) ? specificTarget : null;
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
                    !IsUsableOutputTarget(target, item, matchTags, excludedStorages) ||
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

        private static bool IsUsableOutputTarget(Storage target, GameObject item, HashSet<Tag> matchTags, HashSet<Storage> excludedStorages)
        {
            return target != null &&
                   !excludedStorages.Contains(target) &&
                   !StorageNetworkStorageRules.IsMinionStorage(target) &&
                   target.GetComponent<ComplexFabricator>() == null &&
                   target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                   IsStorageAccepting(target, matchTags) &&
                   (target.items == null || !target.items.Contains(item));
        }

        private static bool IsUsableElementOutputTarget(Storage target, Tag tag, HashSet<Storage> excludedStorages)
        {
            return target != null &&
                   tag != Tag.Invalid &&
                   !excludedStorages.Contains(target) &&
                   !StorageNetworkStorageRules.IsMinionStorage(target) &&
                   target.GetComponent<ComplexFabricator>() == null &&
                   target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                   IsStorageAccepting(target, tag) &&
                   (target.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    IsFilterAccepting(target, tag) ||
                    HasNoExplicitStorageFilter(target));
        }

        private static bool IsUsableNetworkSource(Storage source, IEnumerable<Tag> tags, HashSet<Storage> excludedStorages)
        {
            return source != null &&
                   !excludedStorages.Contains(source) &&
                   !StorageNetworkStorageRules.IsMinionStorage(source) &&
                   source.GetComponent<ComplexFabricator>() == null &&
                   GetAmountAvailableByAnyMatchTag(source, tags) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
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
            return filterable != null && AnyTagAcceptedByTreeFilter(filterable, target.storageFilters, matchTags);
        }

        private static bool IsFilterAccepting(Storage target, Tag tag)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return IsStorageFilterAcceptingTag(target.storageFilters, tag);
            }

            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            return filterable != null && IsFilterAcceptingTag(filterable, tag, target.storageFilters);
        }

        private static bool IsStorageAccepting(Storage target, HashSet<Tag> matchTags)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags);
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
            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            return (filterable == null || filterable.GetTags() == null || filterable.GetTags().Count == 0) &&
                   (target.storageFilters == null || target.storageFilters.Count == 0);
        }

        private static HashSet<Storage> BuildExclusionSet(IEnumerable<Storage> excludedStorages)
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
    }

    internal sealed class StorageTransferResult
    {
        public static readonly StorageTransferResult Idle = new StorageTransferResult(0f, null);

        public StorageTransferResult(float movedKg, string blockedItem)
        {
            MovedKg = movedKg;
            BlockedItem = blockedItem;
        }

        public float MovedKg { get; }

        public string BlockedItem { get; }

        public static StorageTransferResult Blocked(string itemName)
        {
            return new StorageTransferResult(0f, itemName);
        }
    }
}
