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

        [HarmonyPatch(typeof(ComplexFabricator), "OnSpawn")]
        public static class OnSpawnPatch
        {
            public static void Prefix(ComplexFabricator __instance)
            {
                if (__instance is StorageNetworkOrderProductionCenterFabricator)
                {
                    return;
                }

                var queueCounts = AccessTools.Field(typeof(ComplexFabricator), "recipeQueueCounts")?.GetValue(__instance) as System.Collections.Generic.Dictionary<string, int>;
                if (queueCounts == null)
                {
                    return;
                }

                bool hasHundred = false;
                foreach (var kvp in queueCounts)
                {
                    if (kvp.Value == 100)
                    {
                        hasHundred = true;
                        break;
                    }
                }

                if (hasHundred)
                {
                    foreach (string key in System.Linq.Enumerable.ToList(queueCounts.Keys))
                    {
                        if (queueCounts[key] == 100)
                        {
                            queueCounts[key] = ComplexFabricator.QUEUE_INFINITE;
                        }
                    }
                }
            }
        }
    }
}
