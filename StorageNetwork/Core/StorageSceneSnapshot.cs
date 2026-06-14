using System.Collections.Generic;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Core
{
    public sealed class StorageSceneSnapshot
    {
        public static readonly StorageSceneSnapshot Empty =
            new StorageSceneSnapshot(new List<StorageInfo>(), 0f, 0f, true);

        public StorageSceneSnapshot(
            IReadOnlyList<StorageInfo> storages,
            float totalStoredKg,
            float totalCapacityKg,
            bool networkOnline)
        {
            Storages = storages;
            TotalStoredKg = totalStoredKg;
            TotalCapacityKg = totalCapacityKg;
            NetworkOnline = networkOnline;
        }

        public IReadOnlyList<StorageInfo> Storages { get; }

        public float TotalStoredKg { get; }

        public float TotalCapacityKg { get; }

        public bool NetworkOnline { get; }
    }

    public sealed class StorageInfo
    {
        public StorageInfo(Storage storage)
        {
            StorageNetworkPerformanceCounters.RecordStorageInfoConstruction();
            Storage = storage;
            GameObject = storage.gameObject;
            bool connected = StorageNetworkStorageRules.IsConnectedNetworkStorage(storage);
            ContentStorages = GetContentStorages(storage);
            List<GameObject> storedItems = new List<GameObject>();
            float storedKg = 0f;
            foreach (Storage contentStorage in connected ? ContentStorages : new List<Storage>())
            {
                if (contentStorage == null)
                {
                    continue;
                }

                storedKg += contentStorage.MassStored();
                if (contentStorage.items == null)
                {
                    continue;
                }

                foreach (GameObject item in contentStorage.items)
                {
                    if (item != null)
                    {
                        storedItems.Add(item);
                    }
                }
            }

            StoredItems = storedItems;
            StoredKg = storedKg;
            CapacityKg = connected ? storage.Capacity() : 0f;
            Name = GameObject.GetProperName();
            ConnectedToNetwork = connected;
        }

        public StorageInfo(Geyser geyser)
        {
            StorageNetworkPerformanceCounters.RecordStorageInfoConstruction();
            Geyser = geyser;
            GameObject = geyser.gameObject;
            Name = GameObject.GetProperName();
            ContentStorages = new List<Storage>();
            StoredItems = new List<GameObject>();
            StoredKg = 0f;
            CapacityKg = 0f;
            ConnectedToNetwork = false;
        }

        public StorageInfo(MinionIdentity minion)
            : this(minion.GetComponent<Storage>())
        {
            Minion = minion;
            GameObject = minion.gameObject;
            Name = minion.GetProperName();
        }

        public Storage Storage { get; }

        public Geyser Geyser { get; }

        public MinionIdentity Minion { get; }

        public GameObject GameObject { get; }

        public string Name { get; }

        public IReadOnlyList<Storage> ContentStorages { get; }

        public IReadOnlyList<GameObject> StoredItems { get; }

        public float StoredKg { get; }

        public float CapacityKg { get; }

        public bool ConnectedToNetwork { get; }

        private static IReadOnlyList<Storage> GetContentStorages(Storage storage)
        {
            ComplexFabricator fabricator = storage != null ? storage.GetComponent<ComplexFabricator>() : null;
            return new List<Storage>(StorageNetworkProductionStorageCollector.GetProductionStorages(storage, fabricator));
        }
    }

    public sealed class StorageSceneLightweightSnapshot
    {
        public static readonly StorageSceneLightweightSnapshot Empty =
            new StorageSceneLightweightSnapshot(new List<Storage>(), false);

        public StorageSceneLightweightSnapshot(IReadOnlyList<Storage> storages, bool networkOnline)
        {
            Storages = storages;
            NetworkOnline = networkOnline;
        }

        public IReadOnlyList<Storage> Storages { get; }

        public bool NetworkOnline { get; }
    }
}
