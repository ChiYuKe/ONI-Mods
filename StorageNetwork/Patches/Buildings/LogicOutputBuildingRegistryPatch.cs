using HarmonyLib;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class LogicOutputBuildingRegistryPatch
    {
        [HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
        public static class BuildingCompleteOnSpawnPatch
        {
            public static void Postfix(BuildingComplete __instance)
            {
                StorageNetworkBuildingRegistry.Register(__instance != null ? __instance.gameObject : null);
            }
        }

        [HarmonyPatch(typeof(BuildingComplete), "OnCleanUp")]
        public static class BuildingCompleteOnCleanUpPatch
        {
            public static void Prefix(BuildingComplete __instance)
            {
                StorageNetworkBuildingRegistry.Unregister(__instance != null ? __instance.gameObject : null);
            }
        }
    }
}
