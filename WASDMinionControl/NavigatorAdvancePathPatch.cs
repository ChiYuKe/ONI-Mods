using HarmonyLib;

namespace WASDMinionControl
{
    [HarmonyPatch(typeof(Navigator), "AdvancePath")]
    internal static class NavigatorAdvancePathPatch
    {
        private static bool Prefix(Navigator __instance)
        {
            if (__instance == null || ManualControlState.IsNavigationAllowed(__instance.gameObject))
            {
                return true;
            }

            if (!ManualControlState.ConsumeManualTransition(__instance.gameObject))
            {
                return false;
            }

            ManualMinionControlStateMachine.FinishManualMove(__instance);

            return false;
        }
    }
}
