using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;

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

                target = FindOutputTarget(item, excluded, specificTarget);
            }

            return moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? new StorageTransferResult(moved, null)
                : StorageTransferResult.Blocked(StorageItemUtility.GetItemDisplayName(item, tag));
        }

        public static string FormatOutputStatus(StorageTransferResult result, string idleText)
        {
            if (result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return string.Format("已入网 {0}", GameUtil.GetFormattedMass(result.MovedKg));
            }

            if (!string.IsNullOrEmpty(result.BlockedItem))
            {
                return string.Format("无法存入 {0}：没有匹配箱子或容量不足", result.BlockedItem);
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

                float transferred = source.Transfer(target, tag, transferAmount, block_events: false, hide_popups: true);
                if (transferred <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                moved += transferred;
                remaining -= transferred;
                target = FindOutputTarget(item, excludedStorages, specificTarget);
            }

            return remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? new StorageTransferResult(moved, StorageItemUtility.GetItemDisplayName(item, tag))
                : new StorageTransferResult(moved, null);
        }

        private static Storage FindOutputTarget(GameObject item, HashSet<Storage> excludedStorages, Storage specificTarget)
        {
            Tag tag = StorageItemUtility.GetStorageTransferTag(item);
            if (specificTarget != null)
            {
                return IsUsableOutputTarget(specificTarget, item, excludedStorages) ? specificTarget : null;
            }

            return StorageSceneCollector.Collect().Storages
                .Select(info => info.Storage)
                .Where(target => IsUsableOutputTarget(target, item, excludedStorages))
                .Where(target => IsAutoOutputMatch(target, tag))
                .OrderByDescending(target => target.GetAmountAvailable(tag))
                .ThenByDescending(target => IsFilterAccepting(target, tag))
                .ThenByDescending(target => target.RemainingCapacity())
                .FirstOrDefault();
        }

        private static bool IsUsableOutputTarget(Storage target, GameObject item, HashSet<Storage> excludedStorages)
        {
            return target != null &&
                   !excludedStorages.Contains(target) &&
                   target.GetComponent<ComplexFabricator>() == null &&
                   target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                   (target.items == null || !target.items.Contains(item));
        }

        private static bool IsAutoOutputMatch(Storage target, Tag tag)
        {
            return target.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                   IsFilterAccepting(target, tag) ||
                   HasNoExplicitStorageFilter(target);
        }

        private static bool IsFilterAccepting(Storage target, Tag tag)
        {
            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            return filterable != null && filterable.ContainsTag(tag);
        }

        private static bool HasNoExplicitStorageFilter(Storage target)
        {
            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            return filterable == null || filterable.GetTags() == null || filterable.GetTags().Count == 0;
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
