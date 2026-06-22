using HarmonyLib;

namespace WASDMinionControl
{
    [HarmonyPatch(typeof(Brain), "UpdateBrain")]
    internal static class BrainUpdatePatch
    {
        private static bool Prefix(Brain __instance)
        {
            return __instance == null || !ManualControlState.IsActive(__instance.gameObject);
        }
    }
}
