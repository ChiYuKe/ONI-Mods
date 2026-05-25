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
            KPrefabID prefabId = storage.GetComponent<KPrefabID>();
            return enrollment != null &&
                   enrollment.IncludedInSceneNetwork &&
                   prefabId != null &&
                   prefabId.PrefabID().ToString() == "StorageLocker";
        }
    }
}
