using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkRationTrackerPatch
    {
        [HarmonyPatch]
        public static class CountAmountPatch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(WorldResourceAmountTracker<RationTracker>),
                    nameof(WorldResourceAmountTracker<RationTracker>.CountAmount),
                    new[] { typeof(Dictionary<string, float>), typeof(float).MakeByRefType(), typeof(WorldInventory), typeof(bool) });
            }

            public static void Postfix(Dictionary<string, float> unitCountByID, WorldInventory inventory, bool excludeUnreachable, ref float __result, ref float totalUnitsFound)
            {
                float calories = StorageNetworkWorldInventoryMirrorService.GetMirroredEdibleCalories(inventory, true, unitCountByID);
                if (calories <= 0f)
                {
                    return;
                }

                __result += calories;
                totalUnitsFound += calories;
            }
        }

        [HarmonyPatch(typeof(RationTracker), nameof(RationTracker.CountAmountForItemWithID))]
        public static class CountAmountForItemWithIDPatch
        {
            public static void Postfix(string ID, WorldInventory inventory, bool excludeUnreachable, ref float __result)
            {
                __result += StorageNetworkWorldInventoryMirrorService.GetMirroredEdibleCaloriesForId(inventory, true, ID);
            }
        }
    }
}
