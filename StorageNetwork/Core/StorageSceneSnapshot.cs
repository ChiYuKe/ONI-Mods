using System.Collections.Generic;
using UnityEngine;

namespace StorageNetwork.Core
{
    public sealed class StorageSceneSnapshot
    {
        public static readonly StorageSceneSnapshot Empty =
            new StorageSceneSnapshot(new List<StorageInfo>(), 0f, 0f);

        public StorageSceneSnapshot(
            IReadOnlyList<StorageInfo> storages,
            float totalStoredKg,
            float totalCapacityKg)
        {
            Storages = storages;
            TotalStoredKg = totalStoredKg;
            TotalCapacityKg = totalCapacityKg;
        }

        public IReadOnlyList<StorageInfo> Storages { get; }

        public float TotalStoredKg { get; }

        public float TotalCapacityKg { get; }
    }

    public sealed class StorageInfo
    {
        public StorageInfo(Storage storage)
        {
            Storage = storage;
            GameObject = storage.gameObject;
            ContentStorages = GetContentStorages(storage);
            List<GameObject> storedItems = new List<GameObject>();
            float storedKg = 0f;
            foreach (Storage contentStorage in ContentStorages)
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
            CapacityKg = storage.Capacity();
            Name = GameObject.GetProperName();
        }

        public StorageInfo(Geyser geyser)
        {
            Geyser = geyser;
            GameObject = geyser.gameObject;
            Name = GameObject.GetProperName();
            ContentStorages = new List<Storage>();
            StoredItems = new List<GameObject>();
            StoredKg = 0f;
            CapacityKg = 0f;
        }

        public Storage Storage { get; }

        public Geyser Geyser { get; }

        public GameObject GameObject { get; }

        public string Name { get; }

        public IReadOnlyList<Storage> ContentStorages { get; }

        public IReadOnlyList<GameObject> StoredItems { get; }

        public float StoredKg { get; }

        public float CapacityKg { get; }

        private static IReadOnlyList<Storage> GetContentStorages(Storage storage)
        {
            HashSet<Storage> storages = new HashSet<Storage>();
            AddStorage(storages, storage);

            ComplexFabricator fabricator = storage.GetComponent<ComplexFabricator>();
            if (fabricator != null)
            {
                AddStorage(storages, fabricator.inStorage);
                AddStorage(storages, fabricator.buildStorage);
                AddStorage(storages, fabricator.outStorage);
            }

            List<Storage> result = new List<Storage>(storages.Count);
            foreach (Storage contentStorage in storages)
            {
                result.Add(contentStorage);
            }

            return result;
        }

        private static void AddStorage(HashSet<Storage> storages, Storage storage)
        {
            if (storage != null)
            {
                storages.Add(storage);
            }
        }
    }
}
