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
                // LiquidCooledRefinery calls base.SpawnOrderProduct first and only
                // heats its coolant after that call returns. The base postfix would
                // therefore run too early and interfere with the refinery's coolant
                // processing. Its derived-method patch below handles it after the
                // complete vanilla override has finished.
                if (__instance is LiquidCooledRefinery)
                {
                    return;
                }

                StorageNetworkProductionOutputHandler.ForceStoreProducedOutputs(__instance, __result);
            }
        }

        [HarmonyPatch(typeof(LiquidCooledRefinery), "SpawnOrderProduct")]
        public static class LiquidCooledRefinerySpawnOrderProductPatch
        {
            public static void Postfix(LiquidCooledRefinery __instance, List<GameObject> __result)
            {
                StorageNetworkProductionOutputHandler.ForceStoreProducedOutputs(__instance, __result);
            }
        }
    }
}
