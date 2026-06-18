using HarmonyLib;

namespace ONIVisualEnhancer.Patches
{
    [HarmonyPatch(typeof(MeterScreen), "OnSpawn")]
    public static class MeterScreenPatch
    {
        public static void Postfix(MeterScreen __instance)
        {
            VisualEnhancerToggleButton.Create(__instance);
            VisualEnhancerController.EnsureOverlay();
        }
    }
}

