using StorageNetwork.API;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Gameplay
{
    internal static class StorageNetworkEnrollmentInstaller
    {
        public static void AddStorageLockerEnrollment(GameObject go)
        {
            if (go != null)
            {
                go.AddOrGet<StorageNetworkEnrollment>();
            }
        }

        public static void AddPlainStorageEnrollment(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            go.AddOrGet<StorageNetworkEnrollment>();
            if (go.GetComponent<Reservoir>() != null)
            {
                go.AddOrGet<UserNameable>();
            }

            KPrefabID prefabId = go.GetComponent<KPrefabID>();
            prefabId?.RemoveTag(StorageNetworkTags.ShowSettingsButton);
        }

        public static void AddGeyserEnrollment(GameObject go)
        {
            if (go == null || go.GetComponent<Geyser>() == null)
            {
                return;
            }

            go.AddOrGet<StorageNetworkEnrollment>();
            go.AddOrGet<StorageNetworkGeyserOutput>();
        }

        public static void RefreshPlainStorageInstances()
        {
            foreach (Refrigerator refrigerator in Object.FindObjectsByType<Refrigerator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (refrigerator != null)
                {
                    AddPlainStorageEnrollment(refrigerator.gameObject);
                }
            }

            foreach (Reservoir reservoir in Object.FindObjectsByType<Reservoir>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (reservoir != null)
                {
                    AddPlainStorageEnrollment(reservoir.gameObject);
                }
            }
        }

        public static void RefreshGeyserInstances()
        {
            foreach (Geyser geyser in Object.FindObjectsByType<Geyser>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (geyser != null)
                {
                    AddGeyserEnrollment(geyser.gameObject);
                }
            }
        }
    }
}
