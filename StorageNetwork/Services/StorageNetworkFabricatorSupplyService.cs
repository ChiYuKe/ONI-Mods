using System.Collections.Generic;
using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkFabricatorSupplyService
    {
        private const float RetrySeconds = 2f;
        private const float DemandSnapshotSeconds = 0.75f;
        private static readonly Dictionary<int, float> NextTryByPort = new Dictionary<int, float>();
        private static readonly Dictionary<int, FabricatorDemandSnapshot> DemandSnapshotsByWorld = new Dictionary<int, FabricatorDemandSnapshot>();
        private static readonly List<GameObject> MatchingItems = new List<GameObject>();

        public static StorageTransferResult SupplyNextFabricator(Storage portStorage, Storage specificSource, IEnumerable<Tag> allowedTags)
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
            FabricatorDemandSnapshot snapshot = GetDemandSnapshot(worldId);
            ReconcileFabricatorReservations(portStorage, allowed, snapshot);

            FabricatorDemand demand = FindNearestDemand(portStorage, allowed, snapshot);
            if (!demand.IsValid)
            {
                SetRetry(portId);
                return StorageTransferResult.Idle;
            }

            float reserved = GetReservedAmount(portStorage, demand.Tag) + GetEarlierPortReservedAmount(portStorage, worldId, demand.Tag, allowed);
            float remaining = demand.Amount - reserved;
            if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                SetRetry(portId);
                return StorageTransferResult.Idle;
            }

            float moved = TransferMaterialToPort(demand.Tag, remaining, portStorage, specificSource);
            if (moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return new StorageTransferResult(moved, null);
            }

            SetRetry(portId);
            return StorageTransferResult.Blocked(StorageItemUtility.GetTagDisplayName(demand.Tag));
        }

        private static FabricatorDemand FindNearestDemand(Storage portStorage, HashSet<Tag> allowedTags, FabricatorDemandSnapshot snapshot)
        {
            FabricatorDemand best = FabricatorDemand.Invalid;
            float bestDistance = float.MaxValue;
            Vector3 portPosition = portStorage.transform.GetPosition();

            if (snapshot == null)
            {
                return best;
            }

            foreach (FabricatorDemand demand in snapshot.Demands)
            {
                if (!demand.IsValid || !IsAllowed(demand.Tag, allowedTags))
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(demand.Position - portPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = demand;
                }
            }

            return best;
        }

        private static bool IsEligibleFabricator(ComplexFabricator fabricator, int worldId)
        {
            return fabricator != null &&
                fabricator.gameObject != null &&
                fabricator.isActiveAndEnabled &&
                !(fabricator is StorageNetworkOrderProductionCenterFabricator) &&
                StorageTargetSelector.GetObjectWorldId(fabricator.gameObject) == worldId &&
                fabricator.inStorage != null &&
                fabricator.inStorage.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static IEnumerable<ComplexRecipe> GetQueuedRecipes(ComplexFabricator fabricator)
        {
            if (fabricator.CurrentWorkingOrder != null)
            {
                yield return fabricator.CurrentWorkingOrder;
            }

            if (fabricator.NextOrder != null && fabricator.NextOrder != fabricator.CurrentWorkingOrder)
            {
                yield return fabricator.NextOrder;
            }

            foreach (ComplexRecipe recipe in fabricator.GetRecipes())
            {
                if (recipe != null && fabricator.IsRecipeQueued(recipe))
                {
                    yield return recipe;
                }
            }
        }

        private static FabricatorDemand GetMissingIngredient(ComplexFabricator fabricator, ComplexRecipe recipe, HashSet<Tag> allowedTags)
        {
            if (fabricator == null || recipe?.ingredients == null)
            {
                return FabricatorDemand.Invalid;
            }

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                if (!IsAllowed(ingredient.material, allowedTags))
                {
                    continue;
                }

                float target = ingredient.amount * GetRequestOrderCount(fabricator, recipe);
                float available = GetAmountAvailable(fabricator.inStorage, ingredient.material) +
                    GetAmountAvailable(fabricator.buildStorage, ingredient.material);
                float missing = target - available;
                if (missing > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return new FabricatorDemand(fabricator, ingredient.material, missing, fabricator.transform.GetPosition());
                }
            }

            return FabricatorDemand.Invalid;
        }

        private static FabricatorDemandSnapshot GetDemandSnapshot(int worldId)
        {
            if (DemandSnapshotsByWorld.TryGetValue(worldId, out FabricatorDemandSnapshot snapshot) &&
                Time.time - snapshot.CreatedAt <= DemandSnapshotSeconds)
            {
                return snapshot;
            }

            snapshot = BuildDemandSnapshot(worldId);
            DemandSnapshotsByWorld[worldId] = snapshot;
            return snapshot;
        }

        private static FabricatorDemandSnapshot BuildDemandSnapshot(int worldId)
        {
            FabricatorDemandSnapshot snapshot = new FabricatorDemandSnapshot(Time.time);
            foreach (ComplexFabricator fabricator in Object.FindObjectsByType<ComplexFabricator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!IsEligibleFabricator(fabricator, worldId))
                {
                    continue;
                }

                foreach (ComplexRecipe recipe in GetQueuedRecipes(fabricator))
                {
                    FabricatorDemand demand = GetMissingIngredient(fabricator, recipe, null);
                    if (!demand.IsValid)
                    {
                        continue;
                    }

                    snapshot.Demands.Add(demand);
                    AddDictionaryValue(snapshot.DemandByTag, demand.Tag, demand.Amount);
                }
            }

            return snapshot;
        }

        private static int GetRequestOrderCount(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            int count = 0;
            if (fabricator.IsRecipeQueued(recipe))
            {
                count = StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(fabricator, recipe);
                if (count == ComplexFabricator.QUEUE_INFINITE)
                {
                    count = Config.Instance.InfiniteQueueRequestBatchCount;
                }
            }

            if (StorageNetworkFabricatorProgress.IsWorkingOnRecipe(fabricator, recipe) || fabricator.NextOrder == recipe)
            {
                count = Mathf.Max(count, 1);
            }

            return Mathf.Clamp(count, 1, Config.Instance.MaxRequestBatchCount);
        }

        private static float TransferMaterialToPort(Tag tag, float amount, Storage portStorage, Storage specificSource)
        {
            if (tag == Tag.Invalid ||
                portStorage == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                portStorage.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            int worldId = StorageTargetSelector.GetObjectWorldId(portStorage.gameObject);
            HashSet<Tag> wantedTags = new HashSet<Tag> { tag };
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
                GameObject movedItem = MoveFirstMatchingItem(source, portStorage, tag, transferable);
                float movedMass = StorageItemUtility.GetMass(movedItem);
                if (movedMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                MarkFabricatorReservedItem(movedItem);
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

        private static void ReconcileFabricatorReservations(Storage portStorage, HashSet<Tag> allowedTags, FabricatorDemandSnapshot snapshot)
        {
            if (portStorage?.items == null)
            {
                return;
            }

            Dictionary<Tag, float> demandByTag = GetDemandByTag(snapshot, allowedTags);
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

                float remainingDemand = GetDictionaryValue(demandByTag, tag) - GetDictionaryValue(keptByTag, tag);
                if (remainingDemand <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    ReturnReservedItemToNetwork(portStorage, item);
                    continue;
                }

                AddDictionaryValue(keptByTag, tag, Mathf.Min(StorageItemUtility.GetMass(item), remainingDemand));
            }
        }

        private static Dictionary<Tag, float> GetDemandByTag(FabricatorDemandSnapshot snapshot, HashSet<Tag> allowedTags)
        {
            if (snapshot == null)
            {
                return null;
            }

            if (allowedTags == null || allowedTags.Count == 0)
            {
                return snapshot.DemandByTag;
            }

            Dictionary<Tag, float> demandByTag = new Dictionary<Tag, float>();
            foreach (FabricatorDemand demand in snapshot.Demands)
            {
                if (demand.IsValid && IsAllowed(demand.Tag, allowedTags))
                {
                    AddDictionaryValue(demandByTag, demand.Tag, demand.Amount);
                }
            }

            return demandByTag;
        }

        private static float GetEarlierPortReservedAmount(Storage currentPortStorage, int worldId, Tag tag, HashSet<Tag> allowedTags)
        {
            float amount = 0f;
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
                    if (IsFabricatorReserved(item) && StorageItemUtility.MatchesStorageTag(item, tag) && IsAllowed(tag, allowedTags))
                    {
                        amount += StorageItemUtility.GetMass(item);
                    }
                }
            }

            return amount;
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
                if (IsFabricatorReserved(item))
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
                if (IsFabricatorReserved(item) && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    amount += StorageItemUtility.GetMass(item);
                }
            }

            return amount;
        }

        private static bool IsFabricatorReserved(GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            return prefabId != null && prefabId.HasTag(StorageNetworkTags.ReservedForFabricator);
        }

        private static void MarkFabricatorReservedItem(GameObject item)
        {
            item?.GetComponent<KPrefabID>()?.AddTag(StorageNetworkTags.ReservedForFabricator, true);
            StorageNetworkConstructionSupplyService.ClearSolidOutputBufferMarker(item);
        }

        private static void ReturnReservedItemToNetwork(Storage portStorage, GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            if (prefabId != null && prefabId.HasTag(StorageNetworkTags.ReservedForFabricator))
            {
                prefabId.RemoveTag(StorageNetworkTags.ReservedForFabricator);
            }

            NetworkStorageTransferService.TransferStoredItemToNetwork(portStorage, item, new[] { portStorage }, null, true);
        }

        private static float GetAmountAvailable(Storage storage, Tag tag)
        {
            if (storage?.items == null || tag == Tag.Invalid)
            {
                return 0f;
            }

            float amount = 0f;
            foreach (GameObject item in storage.items)
            {
                if (item != null && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    amount += StorageItemUtility.GetMass(item);
                }
            }

            return amount;
        }

        private static void SetRetry(int portId)
        {
            if (portId != KPrefabID.InvalidInstanceID)
            {
                NextTryByPort[portId] = Time.time + RetrySeconds;
            }
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

        private struct FabricatorDemand
        {
            public static readonly FabricatorDemand Invalid = new FabricatorDemand(null, Tag.Invalid, 0f, Vector3.zero);

            public FabricatorDemand(ComplexFabricator fabricator, Tag tag, float amount, Vector3 position)
            {
                Fabricator = fabricator;
                Tag = tag;
                Amount = amount;
                Position = position;
            }

            public ComplexFabricator Fabricator { get; }

            public Tag Tag { get; }

            public float Amount { get; }

            public Vector3 Position { get; }

            public bool IsValid => Tag != Tag.Invalid && Amount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private sealed class FabricatorDemandSnapshot
        {
            public FabricatorDemandSnapshot(float createdAt)
            {
                CreatedAt = createdAt;
            }

            public float CreatedAt { get; }

            public List<FabricatorDemand> Demands { get; } = new List<FabricatorDemand>();

            public Dictionary<Tag, float> DemandByTag { get; } = new Dictionary<Tag, float>();
        }
    }
}
