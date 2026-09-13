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
                if (__instance is StorageNetworkPowerOverlayBattery ||
                    (__instance != null && __instance.GetComponent<StorageNetworkPowerInputPortConsumer>() != null))
                {
                    __result = new List<Descriptor>();
                    return false;
                }

                return true;
            }
        }
        [HarmonyPatch(typeof(Battery), nameof(Battery.SetConnectionStatus))]
        public static class BatterySetConnectionStatusPatch
        {
            public static bool Prefix(Battery __instance)
            {
                if (__instance != null && __instance.GetComponent<StorageNetworkPowerInputPortConsumer>() != null)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
