using System;
using HarmonyLib;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class ConstructionNetworkMaterialPatch
    {
        [HarmonyPatch(typeof(FetchChore), nameof(FetchChore.FindFetchTarget))]
        public static class FetchChoreFindFetchTargetPatch
        {
            public static void Postfix(FetchChore __instance, ChoreConsumerState consumer_state, ref Pickupable __result)
            {
                if (__result != null)
                {
                    return;
                }

                MinionIdentity minion = consumer_state?.gameObject != null
                    ? consumer_state.gameObject.GetComponent<MinionIdentity>()
                    : null;
                if (!Config.Instance.IsMinionAllowedRequestMaterialsFromNetwork(minion))
                {
                    return;
                }

                try
                {
                    __result = ConstructionNetworkMaterialService.FindNetworkConstructionMaterial(__instance, consumer_state);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to request construction materials from network: " + ex);
                }
            }
        }

        [HarmonyPatch(typeof(FetchChore), "End")]
        public static class FetchChoreEndPatch
        {
            public static void Prefix(FetchChore __instance)
            {
                ConstructionNetworkMaterialService.RestoreNetworkConstructionPickup(__instance?.fetchTarget);
            }
        }

        [HarmonyPatch(typeof(Pickupable), "OnCompleteWork")]
        public static class PickupableOnCompleteWorkPatch
        {
            public static bool Prefix(Pickupable __instance, WorkerBase worker)
            {
                if (ConstructionNetworkMaterialService.IsNetworkConstructionPickupAllowed(__instance, worker?.gameObject))
                {
                    ConstructionNetworkMaterialService.PrepareNetworkConstructionPickup(__instance);
                    return true;
                }

                ConstructionNetworkMaterialService.RestoreNetworkConstructionPickup(__instance);
                return false;
            }

            public static void Postfix(Pickupable __instance)
            {
                ConstructionNetworkMaterialService.RestoreNetworkConstructionPickup(__instance);
            }
        }
    }
}
