using HarmonyLib;

namespace WASDMinionControl
{
    [HarmonyPatch(typeof(ChoreDriver), "SetChore")]
    internal static class ChoreDriverSetChorePatch
    {
        private static bool Prefix(ChoreDriver __instance)
        {
            return __instance == null || !ManualControlState.IsActive(__instance.gameObject);
        }
    }
}
