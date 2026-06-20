using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class HighEnergyParticleDirectionSideScreenPatch
    {
        [HarmonyPatch(typeof(HighEnergyParticleDirectionSideScreen), nameof(HighEnergyParticleDirectionSideScreen.IsValidForTarget))]
        public static class IsValidForTargetPatch
        {
            public static void Postfix(GameObject target, ref bool __result)
            {
                if (__result || target == null)
                {
                    return;
                }

                __result = target.GetComponent<StorageNetworkParticleOutputPortEgress>() != null &&
                    target.GetComponent<IHighEnergyParticleDirection>() != null;
            }
        }
    }
}
