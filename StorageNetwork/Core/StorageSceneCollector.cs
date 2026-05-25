using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageSceneCollector
    {
        public static StorageSceneSnapshot Collect()
        {
            Storage[] storages = Object.FindObjectsByType<Storage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<StorageInfo> collected = storages
                .Where(IsSupportedStorage)
                .Select(storage => new StorageInfo(storage))
                .OrderBy(info => info.Name)
                .ToList();

            float totalStoredKg = collected.Sum(info => info.StoredKg);
            float totalCapacityKg = collected.Sum(info => info.CapacityKg);
            return new StorageSceneSnapshot(collected, totalStoredKg, totalCapacityKg);
        }

        private static bool IsSupportedStorage(Storage storage)
        {
            if (storage == null || storage.gameObject == null)
            {
                return false;
            }

            if (storage.GetComponent<SceneStorageBoxMarker>() != null ||
                storage.GetComponent<KPrefabID>()?.HasTag(StorageSceneTags.SceneStorageBox) == true)
            {
                return true;
            }

            StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            if (enrollment == null || !enrollment.IncludedInSceneNetwork)
            {
                return false;
            }

            if (enrollment.IsStorageLocker())
            {
                return true;
            }

            if (enrollment.IsComplexRecipeBuilding())
            {
                return IsPrimaryComplexFabricatorStorage(storage);
            }

            return false;
        }

        private static bool IsPrimaryComplexFabricatorStorage(Storage storage)
        {
            ComplexFabricator fabricator = storage.GetComponent<ComplexFabricator>();
            if (fabricator == null)
            {
                return false;
            }

            return fabricator.inStorage == null || fabricator.inStorage == storage;
        }
    }
}
