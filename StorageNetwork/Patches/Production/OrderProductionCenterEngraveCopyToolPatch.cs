using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    [HarmonyPatch(typeof(CopyBuildingSettings), nameof(CopyBuildingSettings.ApplyCopy))]
    public static class OrderProductionCenterEngraveCopyToolPatch
    {
        public static bool Prefix(KPrefabID other_id, GameObject sourceGameObject, ref bool __result)
        {
            GameObject target = other_id != null ? other_id.gameObject : null;
            if (!StorageNetworkOrderProductionCenterEngraveTool.TryHandleCopyTarget(sourceGameObject, target))
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(CopySettingsTool), "OnDeactivateTool")]
    public static class OrderProductionCenterEngraveCopyToolDeactivatePatch
    {
        public static void Prefix()
        {
            StorageNetworkOrderProductionCenterEngraveTool.HandleCopyToolDeactivated();
        }
    }
}
