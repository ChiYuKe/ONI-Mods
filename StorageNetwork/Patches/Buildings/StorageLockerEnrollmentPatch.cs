using HarmonyLib;
using StorageNetwork.Gameplay;

namespace StorageNetwork.Patches
{
    public static class StorageLockerEnrollmentPatch
    {
        [HarmonyPatch(typeof(StorageLockerConfig), nameof(StorageLockerConfig.ConfigureBuildingTemplate))]
        public static class StorageLockerConfigConfigureBuildingTemplatePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                StorageNetworkEnrollmentInstaller.AddStorageLockerEnrollment(go);
            }
        }

        [HarmonyPatch(typeof(RefrigeratorConfig), nameof(RefrigeratorConfig.DoPostConfigureComplete))]
        public static class RefrigeratorConfigDoPostConfigureCompletePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                StorageNetworkEnrollmentInstaller.AddPlainStorageEnrollment(go);
            }
        }

        [HarmonyPatch(typeof(LiquidReservoirConfig), nameof(LiquidReservoirConfig.ConfigureBuildingTemplate))]
        public static class LiquidReservoirConfigConfigureBuildingTemplatePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                StorageNetworkEnrollmentInstaller.AddPlainStorageEnrollment(go);
            }
        }

        [HarmonyPatch(typeof(GasReservoirConfig), nameof(GasReservoirConfig.ConfigureBuildingTemplate))]
        public static class GasReservoirConfigConfigureBuildingTemplatePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                StorageNetworkEnrollmentInstaller.AddPlainStorageEnrollment(go);
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class GameOnSpawnPatch
        {
            public static void Postfix()
            {
                StorageNetworkEnrollmentInstaller.RefreshPlainStorageInstances();
            }
        }
    }
}
