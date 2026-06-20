using HarmonyLib;
using StorageNetwork.Components;

namespace StorageNetwork.Patches
{
    public static class OrderProductionCenterParallelFabricatorPatch
    {
        [HarmonyPatch(typeof(ComplexFabricator), "Sim200ms")]
        public static class Sim200msPatch
        {
            public static bool Prefix(ComplexFabricator __instance, float dt)
            {
                StorageNetworkOrderProductionCenterFabricator fabricator = __instance as StorageNetworkOrderProductionCenterFabricator;
                if (fabricator == null)
                {
                    return true;
                }

                fabricator.TickParallelCores(dt);
                return false;
            }
        }

        [HarmonyPatch(typeof(ComplexFabricator), "SetRecipeQueueCount")]
        public static class SetRecipeQueueCountPatch
        {
            public static bool Prefix(ComplexFabricator __instance, ComplexRecipe recipe, int count)
            {
                StorageNetworkOrderProductionCenterFabricator fabricator = __instance as StorageNetworkOrderProductionCenterFabricator;
                if (fabricator == null)
                {
                    return true;
                }

                fabricator.SetOrderCenterRecipeQueueCount(recipe, count);
                return false;
            }
        }
    }
}
