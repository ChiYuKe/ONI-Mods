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
        private readonly List<Storage> sourceStorages = new List<Storage>();
        private readonly HashSet<Storage> sourceStorageSet = new HashSet<Storage>();

        public List<Storage> SourceStorages => sourceStorages;

        public void Refresh()
        {
            sourceStorages.Clear();
            sourceStorageSet.Clear();
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
                : sourceStorages.FirstOrDefault(storage => GetComponentInstanceId(storage) == instanceId);
        }

        public static Storage FindStorageByInstanceIdFromScene(int instanceId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

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
                        StorageNetworkStorageRules.IsServerStorage(contentStorage) &&
                        GetComponentInstanceId(contentStorage) == instanceId)
                    {
                        return contentStorage;
                    }
                }
            }

            return null;
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
