using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class ConstructionNetworkMaterialService
    {
        private static readonly HashSet<int> ActiveNetworkPickups = new HashSet<int>();

        public static Pickupable FindNetworkConstructionMaterial(FetchChore fetchChore, ChoreConsumerState consumerState)
        {
            if (fetchChore == null || fetchChore.choreType != Db.Get().ChoreTypes.BuildFetch)
            {
                return null;
            }

            Storage destination = fetchChore.destination;
            if (destination == null || destination.GetComponent<Constructable>() == null)
            {
                return null;
            }

            int worldId = StorageTargetSelector.GetObjectWorldId(destination.gameObject);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return null;
            }

            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId);
            Pickupable best = null;
            int bestCost = int.MaxValue;
            foreach (StorageInfo info in snapshot.Storages)
            {
                Storage source = info?.Storage;
                if (!IsUsableNetworkSource(source))
                {
                    continue;
                }

                Pickupable candidate = FindBestPickupable(source, fetchChore, destination, consumerState, ref bestCost);
                if (candidate != null)
                {
                    best = candidate;
                }
            }

            return best;
        }

        public static bool IsNetworkConstructionPickup(Pickupable pickupable)
        {
            int instanceId = GetPickupableInstanceId(pickupable);
            return instanceId != KPrefabID.InvalidInstanceID && ActiveNetworkPickups.Contains(instanceId);
        }

        public static bool IsNetworkConstructionPickupAllowed(Pickupable pickupable, GameObject fetcher)
        {
            if (!IsNetworkConstructionSource(pickupable))
            {
                return true;
            }

            MinionIdentity minion = fetcher != null ? fetcher.GetComponent<MinionIdentity>() : null;
            return Config.Instance.IsMinionAllowedRequestMaterialsFromNetwork(minion);
        }

        public static void PrepareNetworkConstructionPickup(Pickupable pickupable)
        {
            Storage source = pickupable != null ? pickupable.storage : null;
            if (IsUsableNetworkSource(source))
            {
                int instanceId = GetPickupableInstanceId(pickupable);
                if (instanceId != KPrefabID.InvalidInstanceID)
                {
                    ActiveNetworkPickups.Add(instanceId);
                }

                pickupable.KPrefabID?.RemoveTag(GameTags.StoredPrivate);
            }
        }

        public static void RestoreNetworkConstructionPickup(Pickupable pickupable)
        {
            int instanceId = GetPickupableInstanceId(pickupable);
            if (instanceId == KPrefabID.InvalidInstanceID || !ActiveNetworkPickups.Contains(instanceId))
            {
                return;
            }

            ActiveNetworkPickups.Remove(instanceId);
            RestorePrivateTagIfStillInNetworkStorage(pickupable);
        }

        private static Pickupable FindBestPickupable(
            Storage source,
            FetchChore fetchChore,
            Storage destination,
            ChoreConsumerState consumerState,
            ref int bestCost)
        {
            if (source?.items == null)
            {
                return null;
            }

            Pickupable best = null;
            foreach (GameObject item in source.items)
            {
                Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
                if (pickupable == null || !FetchManager.IsFetchablePickup(pickupable, fetchChore, destination))
                {
                    continue;
                }

                int cost = 0;
                if (consumerState?.consumer != null && !consumerState.consumer.GetNavigationCost(pickupable, out cost))
                {
                    continue;
                }

                if (consumerState?.consumer == null || cost < bestCost)
                {
                    best = pickupable;
                    bestCost = cost;
                }
            }

            return best;
        }

        private static bool IsUsableNetworkSource(Storage source)
        {
            return source != null &&
                   StorageNetworkStorageRules.IsServerStorage(source) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(source) &&
                   !StorageNetworkStorageRules.IsMinionStorage(source) &&
                   !StorageNetworkStorageRules.IsProductionStorage(source);
        }

        private static bool IsNetworkConstructionSource(Pickupable pickupable)
        {
            return IsUsableNetworkSource(pickupable != null ? pickupable.storage : null);
        }

        private static void RestorePrivateTagIfStillInNetworkStorage(Pickupable pickupable)
        {
            Storage storage = pickupable != null ? pickupable.storage : null;
            if (storage != null && IsUsableNetworkSource(storage) && !storage.allowItemRemoval)
            {
                pickupable.KPrefabID?.AddTag(GameTags.StoredPrivate, false);
            }
        }

        private static int GetPickupableInstanceId(Pickupable pickupable)
        {
            KPrefabID prefabId = pickupable != null ? pickupable.KPrefabID : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }
    }
}
