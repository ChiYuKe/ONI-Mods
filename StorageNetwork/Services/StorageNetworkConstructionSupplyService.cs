using System.Collections.Generic;
using StorageNetwork.API;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkConstructionSupplyService
    {
        private static readonly List<Constructable> Constructables = new List<Constructable>();
        private static int nextIndex;

        public static void Register(Constructable constructable)
        {
            if (constructable != null && !Constructables.Contains(constructable))
            {
                Constructables.Add(constructable);
            }
        }

        public static void Unregister(Constructable constructable)
        {
            if (constructable == null)
            {
                return;
            }

            Constructables.Remove(constructable);
            if (nextIndex > Constructables.Count)
            {
                nextIndex = 0;
            }

            ReconcileAllConstructionReservations();
        }

        public static void Reset()
        {
            Constructables.Clear();
            nextIndex = 0;
        }

        public static void ReconcileAllConstructionReservations()
        {
            StorageNetwork.Core.StorageSceneSnapshot snapshot = StorageNetwork.Core.StorageSceneCollector.Collect();
            if (snapshot?.Storages == null)
            {
                return;
            }

            foreach (StorageNetwork.Core.StorageInfo info in snapshot.Storages)
            {
                Storage storage = info?.Storage;
                if (storage == null ||
                    storage.GetComponent<StorageNetwork.Components.StorageNetworkSolidOutputPortEgress>() == null)
                {
                    continue;
                }

                int worldId = StorageTargetSelector.GetObjectWorldId(storage.gameObject);
                ReconcileConstructionReservations(storage, new HashSet<Tag>(), worldId);
            }
        }

        public static StorageTransferResult SupplyNextConstruction(
            Storage portStorage,
            Storage specificSource,
            IEnumerable<Tag> allowedTags)
        {
            if (portStorage == null ||
                Constructables.Count == 0)
            {
                return StorageTransferResult.Idle;
            }

            int portWorldId = StorageTargetSelector.GetObjectWorldId(portStorage.gameObject);
            if (!StorageNetwork.Core.StorageSceneRegistry.HasOnlineCoreInWorld(portWorldId))
            {
                return StorageTransferResult.Offline;
            }

            HashSet<Tag> allowed = BuildAllowedTagSet(allowedTags);
            ReconcileConstructionReservations(portStorage, allowed, portWorldId);

            int checkedCount = 0;
            while (checkedCount < Constructables.Count)
            {
                if (nextIndex >= Constructables.Count)
                {
                    nextIndex = 0;
                }

                Constructable constructable = Constructables[nextIndex++];
                checkedCount++;
                if (constructable == null || constructable.gameObject == null || !constructable.isActiveAndEnabled)
                {
                    Constructables.RemoveAt(--nextIndex);
                    continue;
                }

                Storage constructionStorage = constructable.GetComponent<Storage>();
                if (!IsEligibleTarget(portWorldId, constructionStorage))
                {
                    continue;
                }

                StorageTransferResult result = SupplyPortForConstructionTarget(portStorage, constructionStorage, constructable, specificSource, allowed);
                if (result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT || result.NetworkOffline || !string.IsNullOrEmpty(result.BlockedItem))
                {
                    return result;
                }
            }

            return StorageTransferResult.Idle;
        }

        private static StorageTransferResult SupplyPortForConstructionTarget(
            Storage portStorage,
            Storage constructionStorage,
            Constructable constructable,
            Storage specificSource,
            HashSet<Tag> allowedTags)
        {
            Recipe recipe = constructable.Recipe;
            IList<Tag> selectedTags = constructable.SelectedElementsTags;
            if (recipe == null || selectedTags == null || selectedTags.Count == 0)
            {
                return StorageTransferResult.Idle;
            }

            float moved = 0f;
            string blockedItem = null;
            foreach (Recipe.Ingredient ingredient in recipe.GetAllIngredients(selectedTags))
            {
                if (allowedTags.Count > 0 && !allowedTags.Contains(ingredient.tag))
                {
                    continue;
                }

                float remaining = ingredient.amount -
                    constructionStorage.GetAmountAvailable(ingredient.tag) -
                    GetReservedAmount(portStorage, ingredient.tag);
                if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                float request = remaining;
                float transferred = TransferConstructionMaterialToPort(ingredient.tag, request, portStorage, specificSource);
                if (transferred > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    moved += transferred;
                }
                else if (blockedItem == null)
                {
                    blockedItem = StorageItemUtility.GetTagDisplayName(ingredient.tag);
                }
            }

            return moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? new StorageTransferResult(moved, null)
                : StorageTransferResult.Blocked(blockedItem);
        }

        private static float TransferConstructionMaterialToPort(Tag tag, float amount, Storage portStorage, Storage specificSource)
        {
            if (tag == Tag.Invalid ||
                portStorage == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            int portWorldId = StorageTargetSelector.GetObjectWorldId(portStorage.gameObject);
            HashSet<Tag> wantedTags = new HashSet<Tag> { tag };
            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(null);
            excluded.Add(portStorage);
            List<Storage> sources = StorageTargetSelector.FindNetworkSources(wantedTags, excluded, specificSource, portWorldId);

            float moved = 0f;
            foreach (Storage source in sources)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float sourceAmount = source.GetAmountAvailable(tag);
                if (sourceAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                moved += TransferConstructionItems(source, portStorage, tag, Mathf.Min(amount - moved, sourceAmount));
            }

            return moved;
        }

        public static void ClearConstructionReservation(GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            if (prefabId != null && prefabId.HasTag(StorageNetworkTags.ReservedForConstruction))
            {
                prefabId.RemoveTag(StorageNetworkTags.ReservedForConstruction);
            }
        }

        public static void ClearSolidOutputBufferMarker(GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            if (prefabId != null && prefabId.HasTag(StorageNetworkTags.SolidOutputPortBufferedItem))
            {
                prefabId.RemoveTag(StorageNetworkTags.SolidOutputPortBufferedItem);
            }
        }

        public static bool IsConstructionReserved(GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            return prefabId != null && prefabId.HasTag(StorageNetworkTags.ReservedForConstruction);
        }

        private static void MarkConstructionReservedItem(GameObject item)
        {
            item?.GetComponent<KPrefabID>()?.AddTag(StorageNetworkTags.ReservedForConstruction, false);
            ClearSolidOutputBufferMarker(item);
        }

        private static float TransferConstructionItems(Storage source, Storage destination, Tag tag, float amount)
        {
            if (source == null ||
                destination == null ||
                source.items == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            List<GameObject> items = new List<GameObject>(source.items.Count);
            foreach (GameObject item in source.items)
            {
                if (item != null && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    items.Add(item);
                }
            }

            float moved = 0f;
            foreach (GameObject item in items)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    item == null ||
                    source.items == null ||
                    !source.items.Contains(item))
                {
                    break;
                }

                float itemMass = StorageItemUtility.GetMass(item);
                float transferAmount = Mathf.Min(amount - moved, itemMass);
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                GameObject movedItem = MoveStoredItem(source, destination, item, transferAmount);
                float movedMass = StorageItemUtility.GetMass(movedItem);
                if (movedMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                MarkConstructionReservedItem(movedItem);
                moved += movedMass;
            }

            return moved;
        }

        private static GameObject MoveStoredItem(Storage source, Storage destination, GameObject item, float amount)
        {
            float itemMass = StorageItemUtility.GetMass(item);
            if (itemMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return null;
            }

            if (amount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= itemMass)
            {
                return source.Transfer(item, destination, block_events: false, hide_popups: true) ? item : null;
            }

            Pickupable pickupable = item.GetComponent<Pickupable>();
            Pickupable taken = pickupable != null ? pickupable.Take(amount) : null;
            if (taken == null)
            {
                return null;
            }

            destination.Store(taken.gameObject, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
            source.Trigger(-1697596308, item);
            source.OnStorageChange?.Invoke(item);
            return taken.gameObject;
        }

        private static void ReconcileConstructionReservations(Storage portStorage, HashSet<Tag> allowedTags, int portWorldId)
        {
            if (portStorage?.items == null)
            {
                return;
            }

            Dictionary<Tag, float> demandByTag = GetConstructionDemandByTag(portWorldId, allowedTags);
            Dictionary<Tag, float> earlierReservedByTag = GetEarlierPortReservedByTag(portStorage, portWorldId, allowedTags);
            Dictionary<Tag, float> keptByTag = new Dictionary<Tag, float>();
            List<GameObject> reservedItems = GetReservedItems(portStorage);

            foreach (GameObject item in reservedItems)
            {
                Tag tag = StorageItemUtility.GetStorageTransferTag(item);
                if (tag == Tag.Invalid || !IsAllowed(tag, allowedTags))
                {
                    ReturnReservedItemToNetwork(portStorage, item);
                    continue;
                }

                float demand = GetDictionaryValue(demandByTag, tag);
                float alreadyReserved = GetDictionaryValue(earlierReservedByTag, tag) + GetDictionaryValue(keptByTag, tag);
                float remainingDemand = Mathf.Max(0f, demand - alreadyReserved);
                float itemMass = StorageItemUtility.GetMass(item);
                if (remainingDemand <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    ReturnReservedItemToNetwork(portStorage, item);
                    continue;
                }

                if (itemMass <= remainingDemand + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    AddDictionaryValue(keptByTag, tag, itemMass);
                    continue;
                }

                ReturnReservedItemExcessToNetwork(portStorage, item, itemMass - remainingDemand);
                AddDictionaryValue(keptByTag, tag, remainingDemand);
            }
        }

        private static Dictionary<Tag, float> GetConstructionDemandByTag(int worldId, HashSet<Tag> allowedTags)
        {
            Dictionary<Tag, float> demandByTag = new Dictionary<Tag, float>();
            for (int index = Constructables.Count - 1; index >= 0; index--)
            {
                Constructable constructable = Constructables[index];
                if (constructable == null || constructable.gameObject == null || !constructable.isActiveAndEnabled)
                {
                    Constructables.RemoveAt(index);
                    continue;
                }

                Storage constructionStorage = constructable.GetComponent<Storage>();
                if (!IsEligibleTarget(worldId, constructionStorage))
                {
                    continue;
                }

                Recipe recipe = constructable.Recipe;
                IList<Tag> selectedTags = constructable.SelectedElementsTags;
                if (recipe == null || selectedTags == null || selectedTags.Count == 0)
                {
                    continue;
                }

                foreach (Recipe.Ingredient ingredient in recipe.GetAllIngredients(selectedTags))
                {
                    if (!IsAllowed(ingredient.tag, allowedTags))
                    {
                        continue;
                    }

                    float needed = ingredient.amount - constructionStorage.GetAmountAvailable(ingredient.tag);
                    if (needed > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        AddDictionaryValue(demandByTag, ingredient.tag, needed);
                    }
                }
            }

            return demandByTag;
        }

        private static Dictionary<Tag, float> GetEarlierPortReservedByTag(Storage currentPortStorage, int worldId, HashSet<Tag> allowedTags)
        {
            Dictionary<Tag, float> reservedByTag = new Dictionary<Tag, float>();
            foreach (Storage storage in StorageNetwork.Core.StorageSceneCollector.CollectLightweightForWorld(worldId).Storages)
            {
                if (storage == null || storage == currentPortStorage)
                {
                    continue;
                }

                StorageNetwork.Components.StorageNetworkSolidOutputPortEgress egress =
                    storage.GetComponent<StorageNetwork.Components.StorageNetworkSolidOutputPortEgress>();
                if (egress == null || storage.items == null)
                {
                    continue;
                }

                int currentId = StorageItemUtility.GetStorageInstanceId(currentPortStorage);
                int otherId = StorageItemUtility.GetStorageInstanceId(storage);
                if (otherId > currentId)
                {
                    continue;
                }

                foreach (GameObject item in storage.items)
                {
                    if (!IsConstructionReserved(item))
                    {
                        continue;
                    }

                    Tag tag = StorageItemUtility.GetStorageTransferTag(item);
                    if (tag != Tag.Invalid && IsAllowed(tag, allowedTags))
                    {
                        AddDictionaryValue(reservedByTag, tag, StorageItemUtility.GetMass(item));
                    }
                }
            }

            return reservedByTag;
        }

        private static List<GameObject> GetReservedItems(Storage storage)
        {
            List<GameObject> items = new List<GameObject>();
            if (storage?.items == null)
            {
                return items;
            }

            foreach (GameObject item in storage.items)
            {
                if (IsConstructionReserved(item))
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private static float GetReservedAmount(Storage storage, Tag tag)
        {
            float amount = 0f;
            if (storage?.items == null)
            {
                return amount;
            }

            foreach (GameObject item in storage.items)
            {
                if (IsConstructionReserved(item) && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    amount += StorageItemUtility.GetMass(item);
                }
            }

            return amount;
        }

        private static void ReturnReservedItemExcessToNetwork(Storage portStorage, GameObject item, float excessAmount)
        {
            if (portStorage == null ||
                item == null ||
                excessAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            int portWorldId = StorageTargetSelector.GetObjectWorldId(portStorage.gameObject);
            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(new[] { portStorage });
            Storage target = StorageTargetSelector.FindOutputTarget(
                item,
                StorageItemUtility.GetStorageMatchTags(item),
                excluded,
                null,
                null,
                portWorldId);
            if (target == null || target.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            float returnAmount = Mathf.Min(excessAmount, Mathf.Max(0f, target.RemainingCapacity()));
            Pickupable pickupable = item.GetComponent<Pickupable>();
            Pickupable taken = pickupable != null ? pickupable.Take(returnAmount) : null;
            if (taken == null)
            {
                return;
            }

            ClearConstructionReservation(taken.gameObject);
            target.Store(taken.gameObject, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
            portStorage.Trigger(-1697596308, item);
            portStorage.OnStorageChange?.Invoke(item);
        }

        private static void ReturnReservedItemToNetwork(Storage portStorage, GameObject item)
        {
            ClearConstructionReservation(item);
            ClearSolidOutputBufferMarker(item);
            NetworkStorageTransferService.TransferStoredItemToNetwork(portStorage, item, new[] { portStorage });
        }

        private static bool IsAllowed(Tag tag, HashSet<Tag> allowedTags)
        {
            return allowedTags == null || allowedTags.Count == 0 || allowedTags.Contains(tag);
        }

        private static float GetDictionaryValue(Dictionary<Tag, float> dictionary, Tag tag)
        {
            return dictionary.TryGetValue(tag, out float value) ? value : 0f;
        }

        private static void AddDictionaryValue(Dictionary<Tag, float> dictionary, Tag tag, float value)
        {
            if (dictionary.ContainsKey(tag))
            {
                dictionary[tag] += value;
            }
            else
            {
                dictionary[tag] = value;
            }
        }

        private static bool IsEligibleTarget(int portWorldId, Storage targetStorage)
        {
            return targetStorage != null &&
                targetStorage.items != null &&
                StorageTargetSelector.GetObjectWorldId(targetStorage.gameObject) == portWorldId &&
                targetStorage.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static HashSet<Tag> BuildAllowedTagSet(IEnumerable<Tag> tags)
        {
            HashSet<Tag> allowed = new HashSet<Tag>();
            if (tags == null)
            {
                return allowed;
            }

            foreach (Tag tag in tags)
            {
                if (tag != Tag.Invalid)
                {
                    allowed.Add(tag);
                }
            }

            return allowed;
        }
    }
}
