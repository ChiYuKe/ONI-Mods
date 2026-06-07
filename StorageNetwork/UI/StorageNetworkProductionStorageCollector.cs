using System.Collections.Generic;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkProductionStorageCollector
    {
        public static IEnumerable<Storage> GetProductionStorages(Storage storage, ComplexFabricator fabricator)
        {
            HashSet<Storage> storages = new HashSet<Storage>();
            AddStorage(storages, storage);
            if (fabricator != null)
            {
                AddStorage(storages, fabricator.inStorage);
                AddStorage(storages, fabricator.buildStorage);
                AddStorage(storages, fabricator.outStorage);
            }

            return storages;
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
