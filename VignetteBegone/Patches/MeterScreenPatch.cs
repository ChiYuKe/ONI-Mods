using HarmonyLib;

namespace VignetteBegone.Patches
{
    [HarmonyPatch(typeof(MeterScreen), "OnSpawn")]
    public static class MeterScreenPatch
    {
        public static void Postfix(MeterScreen __instance)
        {
            VignetteToggleButton.Create(__instance);
            VignetteController.LoadSavedState();
            VignetteController.ApplySavedState();
        }
    }
}
