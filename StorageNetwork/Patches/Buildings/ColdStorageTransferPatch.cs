using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    [HarmonyPatch(typeof(Storage), nameof(Storage.Transfer), new[] { typeof(GameObject), typeof(Storage), typeof(bool), typeof(bool) })]
    internal static class ColdStorageTransferPatch
    {
        public static void Postfix(GameObject go, Storage target, bool __result)
        {
            if (!__result || go == null || target == null)
            {
                return;
            }

            // Storage.Transfer notifies the source AFTER storing in the target.
            // The source refrigerator's adjuster can therefore clear the target's
            // cooling. Restore it once both storage notifications have completed.
            target.GetComponent<StorageNetworkColdStorageCooling>()?.RefreshStoredItem(go);
        }
    }
}
