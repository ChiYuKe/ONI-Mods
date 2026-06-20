using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkBatteryDescriptorPatch
    {
        [HarmonyPatch(typeof(Battery), nameof(Battery.GetDescriptors))]
        public static class BatteryGetDescriptorsPatch
        {
            public static bool Prefix(Battery __instance, ref List<Descriptor> __result)
            {
                if (__instance is StorageNetworkPowerOverlayBattery)
                {
                    __result = new List<Descriptor>();
                    return false;
                }

                return true;
            }
        }
    }
}
