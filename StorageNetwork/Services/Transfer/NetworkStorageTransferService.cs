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
            HashSet<Tag> allowedTags = null,
            bool skipPortReservedItems = false,
            bool preferColdStorageForFood = false)
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
                if (skipPortReservedItems && IsPortReservedItem(item))
                {
                    continue;
                }

                if (!StorageTargetSelector.MatchesAllowedTags(item, allowedTags))
                {
                    continue;
                }

                StorageTransferResult result = TransferStoredItem(source, item, excluded, specificTarget, null, sourceWorldId, preferColdStorageForFood);
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
            Storage target = StorageTargetSelector.FindOutputTarget(item, matchTags, excluded, specificTarget, null, sourceWorldId, null);
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

        public static StorageTransferResult TransferStoredItemToNetwork(
            Storage source,
            GameObject item,
            IEnumerable<Storage> excludedStorages,
            Storage specificTarget = null,
            bool preferColdStorageForFood = false)
        {
            if (source == null || item == null || source.items == null || !source.items.Contains(item))
            {
                return StorageTransferResult.Idle;
            }

            int sourceWorldId = StorageTargetSelector.GetObjectWorldId(source.gameObject);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(sourceWorldId))
            {
                return StorageTransferResult.Offline;
            }

            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(excludedStorages);
            return TransferStoredItem(source, item, excluded, specificTarget, null, sourceWorldId, preferColdStorageForFood);
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

            HashSet<Tag> wantedTags = tags as HashSet<Tag>;
            if (wantedTags == null)
            {
                wantedTags = new HashSet<Tag>();
                foreach (Tag tag in tags)
                {
                    if (tag != Tag.Invalid)
                    {
                        wantedTags.Add(tag);
                    }
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

        public static float TransferMatchingItemsFromStorage(
            Storage source,
            Storage destination,
            Tag tag,
            float amount)
        {
            if (source == null ||
                destination == null ||
                source.items == null ||
                tag == Tag.Invalid ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            float moved = 0f;
            List<GameObject> items = new List<GameObject>();
            foreach (GameObject item in source.items)
            {
                if (item != null && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    items.Add(item);
                }
            }

            foreach (GameObject item in items)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float transferAmount = Mathf.Min(
                    amount - moved,
                    StorageItemUtility.GetMass(item),
                    Mathf.Max(0f, destination.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                moved += TransferStoredObject(source, destination, item, transferAmount);
            }

            return moved;
        }

        public static StorageTransferResult TransferAnyLiquidFromNetworkToStorage(
            Storage destination,
            float amount,
            IEnumerable<Storage> excludedStorages = null,
            Storage specificSource = null,
            SimHashes? liquidFilter = null)
        {
            return TransferAnyElementStateFromNetworkToStorage(
                destination,
                amount,
                excludedStorages,
                specificSource,
                liquidFilter,
                IsLiquidItem,
                GameTags.Liquid.ProperName());
        }

        public static StorageTransferResult TransferAnyGasFromNetworkToStorage(
            Storage destination,
            float amount,
            IEnumerable<Storage> excludedStorages = null,
            Storage specificSource = null,
            SimHashes? gasFilter = null)
        {
            return TransferAnyElementStateFromNetworkToStorage(
                destination,
                amount,
                excludedStorages,
                specificSource,
                gasFilter,
                IsGasItem,
                GameTags.Gas.ProperName());
        }

        public static StorageTransferResult TransferAnySolidFromNetworkToStorage(
            Storage destination,
            float amount,
            IEnumerable<Storage> excludedStorages = null,
            Storage specificSource = null,
            IEnumerable<Tag> allowedTags = null)
        {
            if (destination == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return StorageTransferResult.Idle;
            }

            int destinationWorldId = StorageTargetSelector.GetObjectWorldId(destination.gameObject);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId))
            {
                return StorageTransferResult.Offline;
            }

            HashSet<Tag> allowed = BuildAllowedTagSet(allowedTags);
            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(excludedStorages);
            excluded.Add(destination);
            IEnumerable<Storage> sources = specificSource != null
                ? new[] { specificSource }
                : StorageSceneCollector.CollectLightweightForWorld(destinationWorldId).Storages;

            float moved = 0f;
            string blockedItem = null;
            List<GameObject> items = new List<GameObject>();
            foreach (Storage source in sources)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                if (source == null ||
                    excluded.Contains(source) ||
                    !StorageNetworkStorageRules.IsServerStorage(source) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(source) ||
                    source.items == null)
                {
                    continue;
                }

                items.Clear();
                if (items.Capacity < source.items.Count)
                {
                    items.Capacity = source.items.Count;
                }

                foreach (GameObject item in source.items)
                {
                    PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (IsSolidOutputItem(item, primaryElement))
                    {
                        StorageItemUtility.StorageMatchTags matchTags = StorageItemUtility.GetStorageMatchTagsNonAlloc(item);
                        if (StorageTargetSelector.MatchesAllowedTags(item, allowed, matchTags))
                        {
                            items.Add(item);
                        }
                    }
                }

                foreach (GameObject item in items)
                {
                    if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                        destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    float transferAmount = Mathf.Min(
                        amount - moved,
                        GetTransferableAmount(item),
                        Mathf.Max(0f, destination.RemainingCapacity()));
                    if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    float transferred = TransferStoredObject(source, destination, item, transferAmount);
                    if (transferred > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        moved += transferred;
                    }
                    else if (blockedItem == null)
                    {
                        blockedItem = StorageItemUtility.GetItemDisplayName(item, StorageItemUtility.GetStorageTransferTag(item));
                    }
                }
            }

            return moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? new StorageTransferResult(moved, null)
                : StorageTransferResult.Blocked(blockedItem ?? GameTags.Solid.ProperName());
        }

        private static bool IsPortReservedItem(GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            return prefabId != null &&
                (prefabId.HasTag(StorageNetwork.API.StorageNetworkTags.SolidOutputPortBufferedItem) ||
                 prefabId.HasTag(StorageNetwork.API.StorageNetworkTags.ReservedForConstruction) ||
                 prefabId.HasTag(StorageNetwork.API.StorageNetworkTags.ReservedForFarming) ||
                 prefabId.HasTag(StorageNetwork.API.StorageNetworkTags.ReservedForFabricator));
        }

        private static StorageTransferResult TransferAnyElementStateFromNetworkToStorage(
            Storage destination,
            float amount,
            IEnumerable<Storage> excludedStorages,
            Storage specificSource,
            SimHashes? elementFilter,
            System.Func<PrimaryElement, SimHashes?, bool> itemPredicate,
            string fallbackBlockedItem)
        {
            if (destination == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return StorageTransferResult.Idle;
            }

            int destinationWorldId = StorageTargetSelector.GetObjectWorldId(destination.gameObject);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId))
            {
                return StorageTransferResult.Offline;
            }

            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(excludedStorages);
            excluded.Add(destination);
            List<Storage> sources = new List<Storage>();
            if (specificSource != null)
            {
                sources.Add(specificSource);
            }
            else
            {
                foreach (Storage source in StorageSceneCollector.CollectLightweightForWorld(destinationWorldId).Storages)
                {
                    if (source != null)
                    {
                        sources.Add(source);
                    }
                }
            }

            float moved = 0f;
            string blockedItem = null;
            List<GameObject> items = new List<GameObject>();
            foreach (Storage source in sources)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                if (source == null ||
                    excluded.Contains(source) ||
                    !StorageNetworkStorageRules.IsServerStorage(source) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(source) ||
                    source.items == null)
                {
                    continue;
                }

                items.Clear();
                if (items.Capacity < source.items.Count)
                {
                    items.Capacity = source.items.Count;
                }

                foreach (GameObject item in source.items)
                {
                    PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (itemPredicate(primaryElement, elementFilter))
                    {
                        items.Add(item);
                    }
                }

                foreach (GameObject item in items)
                {
                    if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                        destination.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    float transferAmount = Mathf.Min(
                        amount - moved,
                        StorageItemUtility.GetMass(item),
                        Mathf.Max(0f, destination.RemainingCapacity()));
                    if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    float transferred = TransferStoredObject(source, destination, item, transferAmount);
                    if (transferred > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        moved += transferred;
                    }
                    else if (blockedItem == null)
                    {
                        blockedItem = StorageItemUtility.GetItemDisplayName(item, StorageItemUtility.GetStorageTransferTag(item));
                    }
                }
            }

            return moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? new StorageTransferResult(moved, null)
                : StorageTransferResult.Blocked(blockedItem ?? fallbackBlockedItem);
        }

        public static List<SimHashes> GetAvailableLiquidElementsInNetwork(Storage ownerStorage, Storage specificSource = null)
        {
            List<SimHashes> elements = new List<SimHashes>();
            HashSet<SimHashes> seen = new HashSet<SimHashes>();
            int worldId = StorageTargetSelector.GetObjectWorldId(ownerStorage?.gameObject);
            IEnumerable<Storage> sources = specificSource != null
                ? new[] { specificSource }
                : StorageSceneCollector.CollectLightweightForWorld(worldId).Storages;

            foreach (Storage source in sources)
            {
                if (source == null ||
                    source == ownerStorage ||
                    !StorageNetworkStorageRules.IsServerStorage(source) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(source) ||
                    source.items == null)
                {
                    continue;
                }

                foreach (GameObject item in source.items)
                {
                    PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (primaryElement == null || !IsLiquidItem(primaryElement, null) || primaryElement.Mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    if (seen.Add(primaryElement.ElementID))
                    {
                        elements.Add(primaryElement.ElementID);
                    }
                }
            }

            elements.Sort((left, right) => string.Compare(GetElementName(left), GetElementName(right), System.StringComparison.CurrentCulture));
            return elements;
        }

        public static List<SimHashes> GetAvailableGasElementsInNetwork(Storage ownerStorage, Storage specificSource = null)
        {
            List<SimHashes> elements = new List<SimHashes>();
            HashSet<SimHashes> seen = new HashSet<SimHashes>();
            int worldId = StorageTargetSelector.GetObjectWorldId(ownerStorage?.gameObject);
            IEnumerable<Storage> sources = specificSource != null
                ? new[] { specificSource }
                : StorageSceneCollector.CollectLightweightForWorld(worldId).Storages;

            foreach (Storage source in sources)
            {
                if (source == null ||
                    source == ownerStorage ||
                    !StorageNetworkStorageRules.IsServerStorage(source) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(source) ||
                    source.items == null)
                {
                    continue;
                }

                foreach (GameObject item in source.items)
                {
                    PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (primaryElement == null || !IsGasItem(primaryElement, null) || primaryElement.Mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    if (seen.Add(primaryElement.ElementID))
                    {
                        elements.Add(primaryElement.ElementID);
                    }
                }
            }

            elements.Sort((left, right) => string.Compare(GetElementName(left), GetElementName(right), System.StringComparison.CurrentCulture));
            return elements;
        }

        public static List<Tag> GetAvailableSolidItemTagsInNetwork(Storage ownerStorage, Storage specificSource = null)
        {
            List<Tag> tags = new List<Tag>();
            HashSet<Tag> seen = new HashSet<Tag>();
            int worldId = StorageTargetSelector.GetObjectWorldId(ownerStorage?.gameObject);
            IEnumerable<Storage> sources = specificSource != null
                ? new[] { specificSource }
                : StorageSceneCollector.CollectLightweightForWorld(worldId).Storages;

            foreach (Storage source in sources)
            {
                if (source == null ||
                    source == ownerStorage ||
                    !StorageNetworkStorageRules.IsServerStorage(source) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(source) ||
                    source.items == null)
                {
                    continue;
                }

                foreach (GameObject item in source.items)
                {
                    PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (primaryElement == null || !IsSolidOutputItem(item, primaryElement) || primaryElement.Mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    Tag tag = StorageItemUtility.GetStorageTransferTag(item);
                    if (tag != Tag.Invalid && seen.Add(tag))
                    {
                        tags.Add(tag);
                    }
                }
            }

            tags.Sort((left, right) => string.Compare(StorageItemUtility.GetTagDisplayName(left), StorageItemUtility.GetTagDisplayName(right), System.StringComparison.CurrentCulture));
            return tags;
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

        public static bool HasElementOutputCandidateIgnoringCapacity(
            SimHashes elementHash,
            HashSet<Storage> excludedStorages = null,
            Storage specificTarget = null,
            StorageSceneSnapshot snapshot = null,
            int sourceWorldId = -1)
        {
            return StorageTargetSelector.HasElementOutputCandidateIgnoringCapacity(elementHash, excludedStorages, specificTarget, snapshot, sourceWorldId);
        }

        private static StorageTransferResult TransferStoredItem(
            Storage source,
            GameObject item,
            HashSet<Storage> excludedStorages,
            Storage specificTarget,
            StorageSceneSnapshot snapshot,
            int sourceWorldId,
            bool preferColdStorageForFood = false)
        {
            float mass = GetTransferableAmount(item);
            if (mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return StorageTransferResult.Idle;
            }

            StorageItemUtility.StorageMatchTags matchTags = StorageItemUtility.GetStorageMatchTagsNonAlloc(item);
            Tag tag = matchTags.TransferTag;
            float remaining = mass;
            float moved = 0f;
            Storage target = preferColdStorageForFood && StorageItemUtility.IsFoodOrCookingIngredient(item)
                ? StorageTargetSelector.FindFoodOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot, sourceWorldId, source)
                : StorageTargetSelector.FindOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot, sourceWorldId, source);
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

            float mass = GetTransferableAmount(item);
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

            float moved = GetTransferableAmount(taken.gameObject);
            if (moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            target.Store(taken.gameObject, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
            source.Trigger(-1697596308, item);
            source.OnStorageChange?.Invoke(item);
            return moved;
        }

        private static bool IsLiquidItem(PrimaryElement primaryElement, SimHashes? liquidFilter)
        {
            if (primaryElement == null)
            {
                return false;
            }

            if (liquidFilter.HasValue && primaryElement.ElementID != liquidFilter.Value)
            {
                return false;
            }

            Element element = ElementLoader.FindElementByHash(primaryElement.ElementID);
            return element != null && element.IsLiquid;
        }

        private static bool IsGasItem(PrimaryElement primaryElement, SimHashes? gasFilter)
        {
            if (primaryElement == null)
            {
                return false;
            }

            if (gasFilter.HasValue && primaryElement.ElementID != gasFilter.Value)
            {
                return false;
            }

            Element element = ElementLoader.FindElementByHash(primaryElement.ElementID);
            return element != null && element.IsGas;
        }

        private static bool IsSolidItem(PrimaryElement primaryElement)
        {
            if (primaryElement == null)
            {
                return false;
            }

            Element element = ElementLoader.FindElementByHash(primaryElement.ElementID);
            return element != null && (element.IsSolid || !Mathf.Approximately(primaryElement.MassPerUnit, 1f));
        }

        private static bool IsSolidOutputItem(GameObject item, PrimaryElement primaryElement)
        {
            if (IsSolidItem(primaryElement))
            {
                return true;
            }

            return item != null &&
                   item.GetComponent<Pickupable>() != null &&
                   (item.HasTag(GameTags.Seed) ||
                    item.HasTag(GameTags.CropSeed) ||
                    item.GetComponent<PlantableSeed>() != null);
        }

        private static float GetTransferableAmount(GameObject item)
        {
            Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
            if (pickupable != null)
            {
                return Mathf.Max(0f, pickupable.TotalAmount);
            }

            return StorageItemUtility.GetMass(item);
        }

        private static HashSet<Tag> BuildAllowedTagSet(IEnumerable<Tag> tags)
        {
            if (tags == null)
            {
                return null;
            }

            HashSet<Tag> allowed = tags as HashSet<Tag>;
            if (allowed != null)
            {
                return allowed;
            }

            allowed = new HashSet<Tag>();
            foreach (Tag tag in tags)
            {
                if (tag != Tag.Invalid)
                {
                    allowed.Add(tag);
                }
            }

            return allowed;
        }

        private static string GetElementName(SimHashes elementHash)
        {
            Element element = ElementLoader.FindElementByHash(elementHash);
            return element != null ? element.name : elementHash.ToString();
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
