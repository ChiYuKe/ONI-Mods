using System.Collections.Generic;
using System.Linq;
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
            Name = storage.GetProperName();
            ContentStorages = GetContentStorages(storage);
            StoredItems = ContentStorages
                .SelectMany(contentStorage => contentStorage.items.Where(item => item != null))
                .ToList();
            StoredKg = ContentStorages.Sum(contentStorage => contentStorage.MassStored());
            CapacityKg = storage.Capacity();
        }

        public Storage Storage { get; }

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

            return storages.ToList();
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
