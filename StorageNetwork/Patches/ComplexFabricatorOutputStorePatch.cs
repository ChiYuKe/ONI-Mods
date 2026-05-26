using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Components;
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
                StorageNetworkMaterialRequester requester = __instance != null
                    ? __instance.GetComponent<StorageNetworkMaterialRequester>()
                    : null;
                requester?.ForceStoreProducedOutputs(__result);
            }
        }
    }
}
