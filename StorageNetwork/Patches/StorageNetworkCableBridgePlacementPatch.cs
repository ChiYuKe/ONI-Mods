using HarmonyLib;
using System;
using System.Reflection;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkCableBridgePlacementPatch
    {
        [HarmonyPatch]
        public static class BuildingDefIsValidPlaceLocationPatch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(BuildingDef),
                    nameof(BuildingDef.IsValidPlaceLocation),
                    new[]
                    {
                        typeof(GameObject),
                        typeof(int),
                        typeof(Orientation),
                        typeof(bool),
                        typeof(string).MakeByRefType(),
                        typeof(bool)
                    });
            }

            public static void Postfix(
                BuildingDef __instance,
                int cell,
                Orientation orientation,
                ref string fail_reason,
                ref bool __result)
            {
                if (!__result || __instance == null || __instance.PrefabID != StorageNetworkCableBridgeConfig.ID)
                {
                    return;
                }

                int link1Cell = Grid.OffsetCell(cell, Rotatable.GetRotatedCellOffset(new CellOffset(-1, 0), orientation));
                int link2Cell = Grid.OffsetCell(cell, Rotatable.GetRotatedCellOffset(new CellOffset(1, 0), orientation));
                if (StorageNetworkRegistry.IsStorageNetworkPortCell(link1Cell) ||
                    StorageNetworkRegistry.IsStorageNetworkPortCell(link2Cell))
                {
                    fail_reason = global::STRINGS.UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
                    __result = false;
                }
            }
        }
    }
}
