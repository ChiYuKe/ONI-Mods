using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StorageNetwork.Buildings;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkPortPlacementPatch
    {
        private static readonly HashSet<string> PortIds = new HashSet<string>(StorageNetworkPortSpecs.AllIds);

        private static readonly CellOffset BottomOffset = new CellOffset(0, -1);

        private static bool IsStorageNetworkPort(BuildingDef def)
        {
            return def != null && PortIds.Contains(def.PrefabID);
        }

        private static bool HasBottomFoundation(int cell, Orientation orientation)
        {
            CellOffset rotatedBottomOffset = Rotatable.GetRotatedCellOffset(BottomOffset, orientation);
            if (!Grid.IsCellOffsetValid(cell, rotatedBottomOffset))
            {
                return false;
            }

            int bottomCell = Grid.OffsetCell(cell, rotatedBottomOffset);
            return Grid.IsValidBuildingCell(bottomCell) &&
                   (Grid.Solid[bottomCell] || HasFoundationUnderConstruction(bottomCell));
        }

        private static bool HasFoundationUnderConstruction(int cell)
        {
            GameObject buildingObject = Grid.Objects[cell, (int)ObjectLayer.Building];
            BuildingUnderConstruction underConstruction = buildingObject?.GetComponent<BuildingUnderConstruction>();
            return underConstruction?.Def != null && underConstruction.Def.IsFoundation;
        }

        private static void ValidatePortPlacement(BuildingDef def, int cell, Orientation orientation, ref bool result, ref string failReason)
        {
            if (!result || !IsStorageNetworkPort(def))
            {
                return;
            }

            if (!HasBottomFoundation(cell, orientation))
            {
                result = false;
                failReason = global::STRINGS.UI.TOOLTIPS.HELP_BUILDLOCATION_FLOOR;
            }
        }

        [HarmonyPatch]
        public static class IsValidBuildLocationPatch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(BuildingDef),
                    nameof(BuildingDef.IsValidBuildLocation),
                    new[]
                    {
                        typeof(GameObject),
                        typeof(int),
                        typeof(Orientation),
                        typeof(bool),
                        typeof(string).MakeByRefType()
                    });
            }

            public static void Postfix(BuildingDef __instance, int cell, Orientation orientation, ref bool __result, ref string fail_reason)
            {
                ValidatePortPlacement(__instance, cell, orientation, ref __result, ref fail_reason);
            }
        }

        [HarmonyPatch]
        public static class IsValidPlaceLocationPatch
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

            public static void Postfix(BuildingDef __instance, int cell, Orientation orientation, ref bool __result, ref string fail_reason)
            {
                ValidatePortPlacement(__instance, cell, orientation, ref __result, ref fail_reason);
            }
        }
    }
}
