using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed class ProductionNetworkInventoryCache
    {
        private readonly Dictionary<Tag, float> amounts = new Dictionary<Tag, float>();
        private readonly Dictionary<int, Storage> sourceStorageByInstanceId = new Dictionary<int, Storage>();
        private readonly List<Storage> sourceStorages = new List<Storage>();
        private readonly HashSet<Storage> sourceStorageSet = new HashSet<Storage>();
        private int worldId = -1;

        public List<Storage> SourceStorages => sourceStorages;

        public void Clear()
        {
            amounts.Clear();
            sourceStorages.Clear();
            sourceStorageSet.Clear();
            sourceStorageByInstanceId.Clear();
            worldId = -1;
        }

        public int WorldId => worldId;

        public void Refresh(int destinationWorldId)
        {
            Clear();
            worldId = destinationWorldId;
            if (destinationWorldId < 0 || !StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId))
            {
                return;
            }

            StorageNetworkContentIndexService.FillProductionSourceInventory(
                destinationWorldId,
                includeRelatedWorlds: true,
                amounts,
                sourceStorages,
                allowStaleContent: false);
            for (int index = sourceStorages.Count - 1; index >= 0; index--)
            {
                Storage storage = sourceStorages[index];
                if (!IsUsableSource(storage, destinationWorldId) || !sourceStorageSet.Add(storage))
                {
                    sourceStorages.RemoveAt(index);
                    continue;
                }

                AddStorageIndex(sourceStorageByInstanceId, storage);
            }
        }

        public float GetRawAmount(Tag tag)
        {
            return tag != Tag.Invalid && amounts.TryGetValue(tag, out float amount)
                ? amount
                : 0f;
        }

        public Storage FindStorageByInstanceId(int instanceId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID ||
                !sourceStorageByInstanceId.TryGetValue(instanceId, out Storage storage) ||
                !IsUsableSource(storage, worldId))
            {
                return null;
            }

            return storage;
        }

        public static Storage FindStorageByInstanceIdFromScene(int instanceId)
        {
            int activeWorldId = ClusterManager.Instance != null
                ? ClusterManager.Instance.activeWorldId
                : -1;
            return FindStorageByInstanceIdFromScene(instanceId, activeWorldId);
        }

        public static Storage FindStorageByInstanceIdFromScene(int instanceId, int destinationWorldId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID || destinationWorldId < 0)
            {
                return null;
            }

            if (!StorageSceneRegistry.TryGetReachableStorage(
                    instanceId,
                    destinationWorldId,
                    out Storage storage) ||
                !IsUsableSource(storage, destinationWorldId))
            {
                return null;
            }

            return storage;
        }

        public static void InvalidateSceneStorageIndex()
        {
            // Instance resolution is owned by StorageSceneRegistry. Kept as a
            // compatibility hook for callers that reset all production runtime state.
        }

        public static int GetComponentInstanceId(Component component)
        {
            KPrefabID prefabId = component != null ? component.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private static void AddStorageIndex(Dictionary<int, Storage> index, Storage storage)
        {
            int instanceId = GetComponentInstanceId(storage);
            if (instanceId != KPrefabID.InvalidInstanceID)
            {
                index[instanceId] = storage;
            }
        }

        internal static bool IsUsableSource(Storage storage, int destinationWorldId)
        {
            if (destinationWorldId < 0 ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId) ||
                !StorageSceneRegistry.IsLive(storage) ||
                !StorageTargetSelector.IsStorageReachableFromWorld(storage, destinationWorldId) ||
                !StorageNetworkStorageRules.IsServerStorage(storage) ||
                !StorageNetworkStorageRules.IsConnectedNetworkStorage(storage) ||
                StorageNetworkStorageRules.IsMinionStorage(storage) ||
                StorageNetworkStorageRules.IsProductionStorage(storage))
            {
                return false;
            }

            int sourceWorldId = StorageTargetSelector.GetObjectWorldId(storage.gameObject);
            return sourceWorldId >= 0 && StorageSceneRegistry.HasOnlineCoreInWorld(sourceWorldId);
        }

    }
}
