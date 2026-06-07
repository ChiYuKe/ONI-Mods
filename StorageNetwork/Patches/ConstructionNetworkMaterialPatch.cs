using System;
using HarmonyLib;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class ConstructionNetworkMaterialPatch
    {
        [HarmonyPatch(typeof(FetchList2), nameof(FetchList2.Submit))]
        public static class FetchListSubmitPatch
        {
            public static void Prefix(FetchList2 __instance)
            {
                if (!Config.Instance.HasAnyMinionAllowedRequestMaterialsFromNetwork())
                {
                    return;
                }

                try
                {
                    ConstructionNetworkMaterialService.TransferConstructionMaterials(__instance);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to request construction materials from network: " + ex);
                }
            }
        }
    }
}
