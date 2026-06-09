using System.Collections.Generic;
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

            int sourceWorldId = StorageTargetSelector.GetObjectWorldId(source.gameObject);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(sourceWorldId))
            {
                return StorageTransferResult.Offline;
            }

            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(excludedStorages);
            float totalMoved = 0f;
            string blockedItem = null;
            StorageSceneSnapshot snapshot = specificTarget == null ? StorageSceneCollector.CollectForWorld(sourceWorldId) : null;
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
                if (!StorageTargetSelector.MatchesAllowedTags(item, allowedTags))
                {
                    continue;
                }

                StorageTransferResult result = TransferStoredItem(source, item, excluded, specificTarget, snapshot, sourceWorldId);
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

            int sourceWorldId = StorageTargetSelector.GetObjectWorldId(item);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(sourceWorldId))
            {
                return StorageTransferResult.Offline;
            }

            Tag tag = StorageItemUtility.GetStorageTransferTag(item);
            if (!StorageTargetSelector.MatchesAllowedTags(item, allowedTags))
            {
                return StorageTransferResult.Idle;
            }

            HashSet<Tag> matchTags = StorageItemUtility.GetStorageMatchTags(item);
            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(excludedStorages);
            Pickupable pickupable = item.GetComponent<Pickupable>();
            Storage target = StorageTargetSelector.FindOutputTarget(item, matchTags, excluded, specificTarget, StorageSceneCollector.CollectForWorld(sourceWorldId), sourceWorldId);
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
            IEnumerable<Storage> excludedStorages = null,
            Storage specificSource = null)
        {
            if (tags == null ||
                destination == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            int destinationWorldId = StorageTargetSelector.GetObjectWorldId(destination.gameObject);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId))
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

            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(excludedStorages);
            excluded.Add(destination);
            List<Storage> sources = StorageTargetSelector.FindNetworkSources(wantedTags, excluded, specificSource, destinationWorldId);

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

            if (result.NetworkOffline)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_OFFLINE_TITLE);
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
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            return StorageTargetSelector.FindElementOutputTarget(elementHash, excludedStorages, specificTarget, snapshot, sourceWorldId);
        }

        public static List<Storage> FindElementOutputTargets(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages = null,
            Storage specificTarget = null,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            return StorageTargetSelector.FindElementOutputTargets(elementHash, excludedStorages, specificTarget, snapshot, sourceWorldId);
        }

        private static StorageTransferResult TransferStoredItem(
            Storage source,
            GameObject item,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot,
            int sourceWorldId)
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
            Storage target = StorageTargetSelector.FindOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot, sourceWorldId);
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

    }

    internal sealed class StorageTransferResult
    {
        public static readonly StorageTransferResult Idle = new StorageTransferResult(0f, null);
        public static readonly StorageTransferResult Offline = new StorageTransferResult(0f, null, true);

        public StorageTransferResult(float movedKg, string blockedItem, bool networkOffline = false)
        {
            MovedKg = movedKg;
            BlockedItem = blockedItem;
            NetworkOffline = networkOffline;
        }

        public float MovedKg { get; }

        public string BlockedItem { get; }

        public bool NetworkOffline { get; }

        public static StorageTransferResult Blocked(string itemName)
        {
            return new StorageTransferResult(0f, itemName);
        }
    }
}
