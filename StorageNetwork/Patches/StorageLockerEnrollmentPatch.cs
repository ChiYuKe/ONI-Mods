using HarmonyLib;
using StorageNetwork.API;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageLockerEnrollmentPatch
    {
        [HarmonyPatch(typeof(StorageLockerConfig), nameof(StorageLockerConfig.ConfigureBuildingTemplate))]
        public static class StorageLockerConfigConfigureBuildingTemplatePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                go.AddOrGet<StorageNetworkEnrollment>();
            }
        }

        [HarmonyPatch(typeof(RefrigeratorConfig), nameof(RefrigeratorConfig.DoPostConfigureComplete))]
        public static class RefrigeratorConfigDoPostConfigureCompletePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                go.AddOrGet<StorageNetworkEnrollment>();
                go.AddOrGet<StorageNetworkStorageConnector>();
                KPrefabID prefabId = go.GetComponent<KPrefabID>();
                prefabId?.AddTag(StorageNetworkTags.ShowSettingsButton);
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class GameOnSpawnPatch
        {
            public static void Postfix()
            {
                foreach (Refrigerator refrigerator in Object.FindObjectsByType<Refrigerator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (refrigerator == null)
                    {
                        continue;
                    }

                    GameObject go = refrigerator.gameObject;
                    go.AddOrGet<StorageNetworkEnrollment>();
                    go.AddOrGet<StorageNetworkStorageConnector>();
                    KPrefabID prefabId = go.GetComponent<KPrefabID>();
                    prefabId?.AddTag(StorageNetworkTags.ShowSettingsButton);
                }
            }
        }

    }
}
