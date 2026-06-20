using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Gameplay;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class ComplexFabricatorOutputStorePatch
    {
        [HarmonyPatch(typeof(ComplexFabricator), "SpawnOrderProduct")]
        public static class SpawnOrderProductPatch
        {
            public static void Prefix(ComplexFabricator __instance, ref float ___heatedTemperature)
            {
                StorageNetworkOrderProductionCenterFabricator orderCenter = __instance as StorageNetworkOrderProductionCenterFabricator;
                if (orderCenter == null)
                {
                    return;
                }

                orderCenter.EnsureSafeOutputTemperature();
                if (!StorageNetworkOrderProductionCenterFabricator.IsValidOutputTemperature(___heatedTemperature))
                {
                    ___heatedTemperature = orderCenter.GetSafeOutputTemperature();
                }
            }

            public static void Postfix(ComplexFabricator __instance, List<GameObject> __result)
            {
                StorageNetworkProductionOutputHandler.ForceStoreProducedOutputs(__instance, __result);
            }
        }
    }
}
