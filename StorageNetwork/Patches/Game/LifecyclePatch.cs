using HarmonyLib;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class LifecyclePatch
    {
        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class GameOnSpawnPatch
        {
            public static void Prefix()
            {
                StorageNetworkLifecycle.ResetRuntimeState();
                StorageNetworkConstructionSupplyService.Reset();
                StorageNetworkBuildingRegistry.Clear();
            }

            public static void Postfix(Game __instance)
            {
                StorageNetworkFrameProfileTool.InstallIfEnabled(__instance);
                __instance.gameObject.AddOrGet<ProductionOrderBackgroundMaintenance>();
            }
        }

        [HarmonyPatch(typeof(Game), "OnCleanUp")]
        public static class GameOnCleanUpPatch
        {
            public static void Postfix()
            {
                StorageNetworkLifecycle.ResetRuntimeState();
                StorageNetworkConstructionSupplyService.Reset();
                StorageNetworkBuildingRegistry.Clear();
            }
        }

        [HarmonyPatch(typeof(Game), "OnDestroy")]
        public static class GameOnDestroyPatch
        {
            public static void Postfix()
            {
                StorageNetworkLifecycle.ResetRuntimeState();
                StorageNetworkConstructionSupplyService.Reset();
                StorageNetworkBuildingRegistry.Clear();
            }
        }
    }
}
