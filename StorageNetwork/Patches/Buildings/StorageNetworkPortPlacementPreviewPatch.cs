using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StorageNetwork.Buildings;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkPortPlacementPreviewPatch
    {
        private static readonly HashSet<string> PortIds = new HashSet<string>(StorageNetworkPortSpecs.AllIds);

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
                GameObject source_go,
                int cell,
                Orientation orientation,
                bool replace_tile,
                ref string fail_reason,
                bool restrictToActiveWorld,
                ref bool __result)
            {
                if (!__result || __instance == null || !PortIds.Contains(__instance.PrefabID))
                {
                    return;
                }

                // BuildTool colors previews from IsValidPlaceLocation, while OnFloor foundation
                // checks live in IsValidBuildLocation. Mirror that build check for 1x1 ports.
                if (!__instance.IsValidBuildLocation(source_go, cell, orientation, replace_tile, out string buildFailReason))
                {
                    fail_reason = buildFailReason;
                    __result = false;
                }
            }
        }
    }
}
