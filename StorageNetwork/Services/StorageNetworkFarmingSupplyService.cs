using System.Collections.Generic;
using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkFarmingSupplyService
    {
        private const float RetrySeconds = 2f;
        private static readonly Dictionary<int, float> NextTryByPort = new Dictionary<int, float>();
        private static readonly List<GameObject> MatchingItems = new List<GameObject>();

        public static StorageTransferResult SupplyNextPlanting(Storage portStorage, Storage specificSource, IEnumerable<Tag> allowedTags)
        {
            if (portStorage == null || portStorage.items == null)
            {
                return StorageTransferResult.Idle;
            }

            int portId = StorageItemUtility.GetStorageInstanceId(portStorage);
            if (portId != KPrefabID.InvalidInstanceID &&
                NextTryByPort.TryGetValue(portId, out float nextTry) &&
                Time.time < nextTry)
            {
                return StorageTransferResult.Idle;
            }

            int worldId = StorageTargetSelector.GetObjectWorldId(portStorage.gameObject);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return StorageTransferResult.Offline;
            }

            HashSet<Tag> allowed = BuildAllowedTagSet(allowedTags);
            ReconcileFarmingReservations(portStorage, allowed, worldId);

            Dictionary<Tag, float> demandByTag = GetPlantingDemandByTag(worldId, allowed);
            if (demandByTag.Count == 0)
            {
                SetRetry(portId);
                return StorageTransferResult.Idle;
            }

            Dictionary<Tag, float> earlierReservedByTag = GetEarlierPortReservedByTag(portStorage, worldId, allowed);
            foreach (KeyValuePair<Tag, float> demand in demandByTag)
            {
                Tag tag = demand.Key;
                float alreadyReserved = GetDictionaryValue(earlierReservedByTag, tag) + GetReservedAmount(portStorage, tag);
                float remaining = demand.Value - alreadyReserved;
                if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                float transferred = TransferSeedToPort(tag, remaining, portStorage, specificSource);
                if (transferred > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return new StorageTransferResult(transferred, null);
                }
            }

            SetRetry(portId);
            return StorageTransferResult.Idle;
        }

        private static void SetRetry(int portId)
        {
            if (portId != KPrefabID.InvalidInstanceID)
            {
                NextTryByPort[portId] = Time.time + RetrySeconds;
            }
        }

        private static Dictionary<Tag, float> GetPlantingDemandByTag(int worldId, HashSet<Tag> allowedTags)
        {
            Dictionary<Tag, float> demandByTag = new Dictionary<Tag, float>();
            foreach (PlantablePlot plot in global::Components.PlantablePlots.GetItems(worldId))
            {
                if (plot == null || plot.gameObject == null || !plot.isActiveAndEnabled || plot.Occupant != null)
                {
                    continue;
                }

                FetchChore request = plot.GetActiveRequest;
                if (request == null)
                {
                    continue;
                }

                Tag seedTag = request.tagsFirst;
                if (seedTag == Tag.Invalid || !IsAllowed(seedTag, allowedTags))
                {
                    continue;
                }

                AddDictionaryValue(demandByTag, seedTag, Mathf.Max(1f, request.originalAmount));
            }

            return demandByTag;
        }

        private static float TransferSeedToPort(Tag seedTag, float amount, Storage portStorage, Storage specificSource)
        {
            if (seedTag == Tag.Invalid ||
                portStorage == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                portStorage.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            int worldId = StorageTargetSelector.GetObjectWorldId(portStorage.gameObject);
            HashSet<Tag> wantedTags = new HashSet<Tag> { seedTag };
            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(null);
            excluded.Add(portStorage);
            List<Storage> sources = StorageTargetSelector.FindNetworkSources(wantedTags, excluded, specificSource, worldId);

            float moved = 0f;
            foreach (Storage source in sources)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    portStorage.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float transferable = Mathf.Min(amount - moved, Mathf.Max(0f, portStorage.RemainingCapacity()));
                GameObject movedItem = MoveFirstMatchingItem(source, portStorage, seedTag, transferable);
                float movedMass = StorageItemUtility.GetMass(movedItem);
                if (movedMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                MarkFarmingReservedItem(movedItem);
                moved += movedMass;
            }

            return moved;
        }

        private static GameObject MoveFirstMatchingItem(Storage source, Storage destination, Tag tag, float amount)
        {
            if (source == null || destination == null || source.items == null)
            {
                return null;
            }

            MatchingItems.Clear();
            foreach (GameObject item in source.items)
            {
                if (item != null && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    MatchingItems.Add(item);
                }
            }

            foreach (GameObject item in MatchingItems)
            {
                if (item == null || !source.items.Contains(item))
                {
                    continue;
                }

                float itemMass = StorageItemUtility.GetMass(item);
                if (itemMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                if (amount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= itemMass)
                {
                    return source.Transfer(item, destination, block_events: false, hide_popups: true) ? item : null;
                }

                Pickupable pickupable = item.GetComponent<Pickupable>();
                Pickupable taken = pickupable != null ? pickupable.Take(amount) : null;
                if (taken == null)
                {
                    continue;
                }

                destination.Store(taken.gameObject, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
                source.Trigger(-1697596308, item);
                source.OnStorageChange?.Invoke(item);
                return taken.gameObject;
            }

            return null;
        }

        private static void ReconcileFarmingReservations(Storage portStorage, HashSet<Tag> allowedTags, int worldId)
        {
            if (portStorage?.items == null)
            {
                return;
            }

            Dictionary<Tag, float> demandByTag = GetPlantingDemandByTag(worldId, allowedTags);
            Dictionary<Tag, float> earlierReservedByTag = GetEarlierPortReservedByTag(portStorage, worldId, allowedTags);
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

                float remainingDemand = GetDictionaryValue(demandByTag, tag) -
                    GetDictionaryValue(earlierReservedByTag, tag) -
                    GetDictionaryValue(keptByTag, tag);
                if (remainingDemand <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    ReturnReservedItemToNetwork(portStorage, item);
                    continue;
                }

                float mass = StorageItemUtility.GetMass(item);
                AddDictionaryValue(keptByTag, tag, Mathf.Min(mass, remainingDemand));
            }
        }

        private static Dictionary<Tag, float> GetEarlierPortReservedByTag(Storage currentPortStorage, int worldId, HashSet<Tag> allowedTags)
        {
            Dictionary<Tag, float> reservedByTag = new Dictionary<Tag, float>();
            int currentId = StorageItemUtility.GetStorageInstanceId(currentPortStorage);
            foreach (Storage storage in StorageSceneCollector.CollectLightweightForWorld(worldId).Storages)
            {
                if (storage == null || storage == currentPortStorage || storage.items == null)
                {
                    continue;
                }

                StorageNetworkSolidOutputPortEgress egress = storage.GetComponent<StorageNetworkSolidOutputPortEgress>();
                if (egress == null || !egress.AllowManualOperation)
                {
                    continue;
                }

                int otherId = StorageItemUtility.GetStorageInstanceId(storage);
                if (otherId > currentId)
                {
                    continue;
                }

                foreach (GameObject item in storage.items)
                {
                    if (!IsFarmingReserved(item))
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
                if (IsFarmingReserved(item))
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private static float GetReservedAmount(Storage storage, Tag tag)
        {
            float amount = 0f;
            if (storage?.items == null || tag == Tag.Invalid)
            {
                return amount;
            }

            foreach (GameObject item in storage.items)
            {
                if (IsFarmingReserved(item) && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    amount += StorageItemUtility.GetMass(item);
                }
            }

            return amount;
        }

        private static bool IsFarmingReserved(GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            return prefabId != null && prefabId.HasTag(StorageNetworkTags.ReservedForFarming);
        }

        private static void MarkFarmingReservedItem(GameObject item)
        {
            item?.GetComponent<KPrefabID>()?.AddTag(StorageNetworkTags.ReservedForFarming, true);
            StorageNetworkConstructionSupplyService.ClearSolidOutputBufferMarker(item);
        }

        private static void ReturnReservedItemToNetwork(Storage portStorage, GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            if (prefabId != null && prefabId.HasTag(StorageNetworkTags.ReservedForFarming))
            {
                prefabId.RemoveTag(StorageNetworkTags.ReservedForFarming);
            }

            NetworkStorageTransferService.TransferStoredItemToNetwork(portStorage, item, new[] { portStorage });
        }

        private static bool IsAllowed(Tag tag, HashSet<Tag> allowedTags)
        {
            return allowedTags == null || allowedTags.Count == 0 || allowedTags.Contains(tag);
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

        private static void AddDictionaryValue(Dictionary<Tag, float> values, Tag tag, float amount)
        {
            if (tag == Tag.Invalid || amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            values.TryGetValue(tag, out float current);
            values[tag] = current + amount;
        }

        private static float GetDictionaryValue(Dictionary<Tag, float> values, Tag tag)
        {
            return values != null && values.TryGetValue(tag, out float value) ? value : 0f;
        }
    }
}
