using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class OrderProductionCenterCopySettingsPatch
    {
        [HarmonyPatch(typeof(ComplexFabricator), "OnCopySettings")]
        public static class ComplexFabricatorOnCopySettingsPatch
        {
            public static bool Prefix(ComplexFabricator __instance, object data)
            {
                StorageNetworkOrderProductionCenterFabricator fabricator = __instance as StorageNetworkOrderProductionCenterFabricator;
                if (fabricator == null)
                {
                    return true;
                }

                fabricator.CopyOrderCenterSettingsFrom(data as GameObject);
                return false;
            }
        }
    }
}
