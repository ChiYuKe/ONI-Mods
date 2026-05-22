using System.Collections.Generic;

namespace StorageNetwork.Core
{
    public sealed class StorageNetworkSnapshot
    {
        public static readonly StorageNetworkSnapshot Empty =
            new StorageNetworkSnapshot(new List<StorageNetworkStorageInfo>(), 0f, 0f);

        public StorageNetworkSnapshot(
            IReadOnlyList<StorageNetworkStorageInfo> storages,
            float totalStoredKg,
            float totalCapacityKg)
        {
            Storages = storages;
            TotalStoredKg = totalStoredKg;
            TotalCapacityKg = totalCapacityKg;
        }

        public IReadOnlyList<StorageNetworkStorageInfo> Storages { get; }

        public float TotalStoredKg { get; }

        public float TotalCapacityKg { get; }
    }

    public sealed class StorageNetworkStorageInfo
    {
        public StorageNetworkStorageInfo(Storage storage)
        {
            Storage = storage;
            Name = storage.GetProperName();
            StoredKg = storage.MassStored();
            CapacityKg = storage.Capacity();
        }

        public Storage Storage { get; }

        public string Name { get; }

        public float StoredKg { get; }

        public float CapacityKg { get; }
    }
}
