using System.Collections.Generic;

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
            StoredKg = storage.MassStored();
            CapacityKg = storage.Capacity();
        }

        public Storage Storage { get; }

        public string Name { get; }

        public float StoredKg { get; }

        public float CapacityKg { get; }
    }
}
