using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Gameplay;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class ComplexFabricatorOutputStorePatch
    {
        [HarmonyPatch(typeof(ComplexFabricator), "SpawnOrderProduct")]
        public static class SpawnOrderProductPatch
        {
            public static void Postfix(ComplexFabricator __instance, List<GameObject> __result)
            {
                StorageNetworkProductionOutputHandler.ForceStoreProducedOutputs(__instance, __result);
            }
        }
    }
}
