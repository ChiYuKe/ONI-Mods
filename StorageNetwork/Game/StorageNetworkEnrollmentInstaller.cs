using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.Services;
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
            StorageNetworkFilterConfigurator.Configure(go.GetComponent<TreeFilterable>());
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

        public static void InstallComplexRecipeBuildingPrefabs()
        {
            foreach (GameObject prefab in Assets.GetPrefabsWithComponent<ComplexFabricator>())
            {
                if (prefab != null && prefab.GetComponent<Storage>() != null)
                {
                    prefab.AddOrGet<StorageNetworkEnrollment>();
                    prefab.AddOrGet<StorageNetworkMaterialRequester>();
                }
            }
        }

        public static void InstallEnergyGeneratorPrefabs()
        {
            foreach (GameObject prefab in Assets.GetPrefabsWithComponent<EnergyGenerator>())
            {
                EnergyGenerator generator = prefab != null ? prefab.GetComponent<EnergyGenerator>() : null;
                if (!StorageNetworkEnergyGeneratorRequester.HasFuelInputs(generator) || prefab.GetComponent<Storage>() == null)
                {
                    continue;
                }

                prefab.AddOrGet<StorageNetworkEnrollment>();
                prefab.AddOrGet<StorageNetworkEnergyGeneratorRequester>();
            }
        }
    }
}
