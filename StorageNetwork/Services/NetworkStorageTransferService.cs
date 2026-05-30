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
            Storage specificTarget = null)
        {
            if (source == null || source.items == null)
            {
                return StorageTransferResult.Idle;
            }

            HashSet<Storage> excluded = BuildExclusionSet(excludedStorages);
            float totalMoved = 0f;
            string blockedItem = null;

            foreach (GameObject item in source.items.Where(item => item != null).ToList())
            {
                StorageTransferResult result = TransferStoredItem(source, item, excluded, specificTarget);
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
            Storage specificTarget = null)
        {
            if (item == null || StorageItemUtility.GetMass(item) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return StorageTransferResult.Idle;
            }

            Tag tag = StorageItemUtility.GetStorageTransferTag(item);
            HashSet<Storage> excluded = BuildExclusionSet(excludedStorages);
            Pickupable pickupable = item.GetComponent<Pickupable>();
            Storage target = FindOutputTarget(item, excluded, specificTarget);
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

        private static StorageTransferResult TransferStoredItem(
            Storage source,
            GameObject item,
            HashSet<Storage> excludedStorages,
            Storage specificTarget)
        {
            float mass = StorageItemUtility.GetMass(item);
            if (mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return StorageTransferResult.Idle;
            }

            Tag tag = StorageItemUtility.GetStorageTransferTag(item);
            float remaining = mass;
            float moved = 0f;
            Storage target = FindOutputTarget(item, excludedStorages, specificTarget);
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

        private static Storage FindOutputTarget(GameObject item, HashSet<Storage> excludedStorages, Storage specificTarget)
        {
            StorageSceneSnapshot snapshot = StorageSceneCollector.Collect();
            if (specificTarget != null)
            {
                return IsUsableOutputTarget(specificTarget, item, excludedStorages) ? specificTarget : null;
            }

            return snapshot.Storages
                .Select(info => info.Storage)
                .Where(target => IsUsableOutputTarget(target, item, excludedStorages))
                .Where(target => IsAutoOutputMatch(target, item))
                .OrderByDescending(target => GetAmountAvailableByAnyMatchTag(target, item))
                .ThenByDescending(target => IsFilterAccepting(target, item))
                .ThenByDescending(target => target.RemainingCapacity())
                .FirstOrDefault();
        }

        private static bool IsUsableOutputTarget(Storage target, GameObject item, HashSet<Storage> excludedStorages)
        {
            return target != null &&
                   !excludedStorages.Contains(target) &&
                   target.GetComponent<ComplexFabricator>() == null &&
                   target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                   IsStorageAccepting(target, item) &&
                   (target.items == null || !target.items.Contains(item));
        }

        private static bool IsAutoOutputMatch(Storage target, GameObject item)
        {
            return GetAmountAvailableByAnyMatchTag(target, item) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                   IsFilterAccepting(target, item) ||
                   HasNoExplicitStorageFilter(target);
        }

        private static bool IsFilterAccepting(Storage target, GameObject item)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return StorageItemUtility.GetStorageMatchTags(item).Any(tag => IsStorageFilterAcceptingTag(target.storageFilters, tag));
            }

            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            return filterable != null && StorageItemUtility.GetStorageMatchTags(item).Any(tag => IsFilterAcceptingTag(filterable, tag, target.storageFilters));
        }

        private static bool IsStorageAccepting(Storage target, GameObject item)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return StorageItemUtility.GetStorageMatchTags(item).Any(tag => IsStorageFilterAcceptingTag(target.storageFilters, tag));
            }

            return target != null &&
                   (IsFilterAccepting(target, item) ||
                    target.storageFilters == null ||
                    target.storageFilters.Count == 0 ||
                    StorageItemUtility.GetStorageMatchTags(item).Any(tag => IsStorageFilterAcceptingTag(target.storageFilters, tag)));
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

        private static float GetAmountAvailableByAnyMatchTag(Storage target, GameObject item)
        {
            return target == null
                ? 0f
                : StorageItemUtility.GetStorageMatchTags(item).Max(tag => target.GetAmountAvailable(tag));
        }

        private static bool HasNoExplicitStorageFilter(Storage target)
        {
            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            return (filterable == null || filterable.GetTags() == null || filterable.GetTags().Count == 0) &&
                   (target.storageFilters == null || target.storageFilters.Count == 0);
        }

        private static HashSet<Storage> BuildExclusionSet(IEnumerable<Storage> excludedStorages)
        {
            return new HashSet<Storage>((excludedStorages ?? Enumerable.Empty<Storage>()).Where(storage => storage != null));
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
