using System.Collections.Generic;
using System.Linq;
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
        private static readonly Dictionary<int, Storage> sceneStorageByInstanceId = new Dictionary<int, Storage>();
        private static int sceneStorageIndexVersion = -1;

        public List<Storage> SourceStorages => sourceStorages;

        public void Refresh()
        {
            sourceStorages.Clear();
            sourceStorageSet.Clear();
            sourceStorageByInstanceId.Clear();
            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                if (info?.ContentStorages == null || !StorageNetworkStorageRules.IsServerStorage(info.Storage))
                {
                    continue;
                }

                foreach (Storage storage in info.ContentStorages)
                {
                    if (storage != null &&
                        StorageNetworkStorageRules.IsServerStorage(storage) &&
                        sourceStorageSet.Add(storage))
                    {
                        sourceStorages.Add(storage);
                        AddStorageIndex(sourceStorageByInstanceId, storage);
                    }
                }
            }

            amounts.Clear();
            foreach (Storage storage in sourceStorages)
            {
                if (storage == null || StorageNetworkStorageRules.IsProductionStorage(storage) || storage.items == null)
                {
                    continue;
                }

                foreach (GameObject item in storage.items)
                {
                    AddItemAmount(item);
                }
            }
        }

        public float GetRawAmount(Tag tag)
        {
            return amounts.TryGetValue(tag, out float amount) ? amount : 0f;
        }

        public Storage FindStorageByInstanceId(int instanceId)
        {
            return instanceId == KPrefabID.InvalidInstanceID
                ? null
                : sourceStorageByInstanceId.TryGetValue(instanceId, out Storage storage)
                    ? storage
                    : null;
        }

        public static Storage FindStorageByInstanceIdFromScene(int instanceId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            EnsureSceneStorageIndex();
            return sceneStorageByInstanceId.TryGetValue(instanceId, out Storage storage) ? storage : null;
        }

        public static void InvalidateSceneStorageIndex()
        {
            sceneStorageIndexVersion = -1;
            sceneStorageByInstanceId.Clear();
        }

        private static void EnsureSceneStorageIndex()
        {
            int version = StorageSceneRegistry.Version;
            if (sceneStorageIndexVersion == version)
            {
                return;
            }

            sceneStorageByInstanceId.Clear();
            HashSet<Storage> visited = new HashSet<Storage>();
            int activeWorldId = ClusterManager.Instance != null ? ClusterManager.Instance.activeWorldId : -1;
            foreach (Storage storage in StorageSceneCollector.CollectLightweightForWorld(activeWorldId).Storages)
            {
                if (storage == null ||
                    !StorageNetworkStorageRules.IsServerStorage(storage) ||
                    !visited.Add(storage))
                {
                    continue;
                }

                foreach (Storage contentStorage in StorageNetworkProductionStorageCollector.GetProductionStorages(storage, storage.GetComponent<ComplexFabricator>()))
                {
                    if (contentStorage != null &&
                        StorageNetworkStorageRules.IsServerStorage(contentStorage))
                    {
                        AddStorageIndex(sceneStorageByInstanceId, contentStorage);
                    }
                }
            }

            sceneStorageIndexVersion = version;
        }

        public static int GetComponentInstanceId(Component component)
        {
            KPrefabID prefabId = component != null ? component.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private void AddItemAmount(GameObject item)
        {
            PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
            if (primaryElement == null)
            {
                return;
            }

            Tag storageTag = StorageItemUtility.GetStorageTransferTag(item);
            AddAmount(storageTag, primaryElement.Mass);
            Tag elementTag = primaryElement.ElementID.CreateTag();
            if (elementTag != Tag.Invalid && elementTag != storageTag)
            {
                AddAmount(elementTag, primaryElement.Mass);
            }
        }

        private static void AddStorageIndex(Dictionary<int, Storage> index, Storage storage)
        {
            int instanceId = GetComponentInstanceId(storage);
            if (instanceId != KPrefabID.InvalidInstanceID)
            {
                index[instanceId] = storage;
            }
        }

        private void AddAmount(Tag tag, float amount)
        {
            if (tag == Tag.Invalid || amount <= 0f)
            {
                return;
            }

            amounts[tag] = amounts.TryGetValue(tag, out float existing) ? existing + amount : amount;
        }
    }
}
