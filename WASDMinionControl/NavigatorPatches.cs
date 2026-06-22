using HarmonyLib;

namespace WASDMinionControl
{
    [HarmonyPatch(typeof(Navigator), "GoTo", typeof(int), typeof(CellOffset[]))]
    internal static class NavigatorGoToCellPatch
    {
        private static bool Prefix(Navigator __instance, ref bool __result)
        {
            if (ManualControlState.IsNavigationAllowed(__instance?.gameObject))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Navigator), "GoTo", typeof(int), typeof(CellOffset[]), typeof(NavTactic))]
    internal static class NavigatorGoToCellTacticPatch
    {
        private static bool Prefix(Navigator __instance, ref bool __result)
        {
            if (ManualControlState.IsNavigationAllowed(__instance?.gameObject))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Navigator), "GoTo", typeof(KMonoBehaviour), typeof(CellOffset[]), typeof(NavTactic))]
    internal static class NavigatorGoToTargetPatch
    {
        private static bool Prefix(Navigator __instance, ref bool __result)
        {
            if (ManualControlState.IsNavigationAllowed(__instance?.gameObject))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
