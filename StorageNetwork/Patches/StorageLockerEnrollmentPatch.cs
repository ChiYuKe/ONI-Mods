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
                AddPlainStorageEnrollment(go);
            }
        }

        [HarmonyPatch(typeof(LiquidReservoirConfig), nameof(LiquidReservoirConfig.ConfigureBuildingTemplate))]
        public static class LiquidReservoirConfigConfigureBuildingTemplatePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                AddPlainStorageEnrollment(go);
            }
        }

        [HarmonyPatch(typeof(GasReservoirConfig), nameof(GasReservoirConfig.ConfigureBuildingTemplate))]
        public static class GasReservoirConfigConfigureBuildingTemplatePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                AddPlainStorageEnrollment(go);
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
                    AddPlainStorageEnrollment(go);
                }

                foreach (Reservoir reservoir in Object.FindObjectsByType<Reservoir>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (reservoir == null)
                    {
                        continue;
                    }

                    AddPlainStorageEnrollment(reservoir.gameObject);
                }
            }
        }

        private static void AddPlainStorageEnrollment(UnityEngine.GameObject go)
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

    }
}
