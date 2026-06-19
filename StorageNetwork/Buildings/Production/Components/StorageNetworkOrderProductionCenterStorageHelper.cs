using System.Linq;
using UnityEngine;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkOrderProductionCenterStorageHelper
    {
        public const float FabricatorStorageCapacityKg = 20000f;
        public const float DiskInstallStorageCapacityKg = 3f;

        public static void RestoreFabricatorStorageCapacity(ComplexFabricator fabricator)
        {
            if (fabricator == null)
            {
                return;
            }

            RestoreStorage(fabricator.inStorage);
            RestoreStorage(fabricator.buildStorage);
            RestoreStorage(fabricator.outStorage);
        }

        public static Storage GetDiskInstallStorage(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return null;
            }

            ComplexFabricator fabricator = gameObject.GetComponent<ComplexFabricator>();
            Storage[] storages = gameObject.GetComponents<Storage>();
            Storage diskInstallStorage = storages.FirstOrDefault(storage =>
                storage != null &&
                storage != fabricator?.inStorage &&
                storage != fabricator?.buildStorage &&
                storage != fabricator?.outStorage);
            if (diskInstallStorage != null)
            {
                return diskInstallStorage;
            }

            diskInstallStorage = gameObject.AddComponent<Storage>();
            diskInstallStorage.capacityKg = DiskInstallStorageCapacityKg;
            diskInstallStorage.showInUI = false;
            diskInstallStorage.allowItemRemoval = false;
            diskInstallStorage.allowUIItemRemoval = false;
            diskInstallStorage.fetchCategory = Storage.FetchCategory.Building;
            diskInstallStorage.storageFilters = new System.Collections.Generic.List<Tag> { StorageNetwork.Buildings.StorageNetworkEngravingDiskConfig.ID };
            diskInstallStorage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            return diskInstallStorage;
        }

        private static void RestoreStorage(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            storage.capacityKg = FabricatorStorageCapacityKg;
            storage.showInUI = true;
            storage.SetDefaultStoredItemModifiers(Storage.StandardFabricatorStorage);
        }
    }
}
