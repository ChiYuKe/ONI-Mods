using HarmonyLib;

namespace ONIVisualEnhancer.Patches
{
    [HarmonyPatch(typeof(Vignette), nameof(Vignette.Reset))]
    public static class VignetteResetPatch
    {
        public static bool Prefix(Vignette __instance)
        {
            if (!VisualEnhancerSettings.HideGameVignette)
            {
                return true;
            }

            GameVignetteController.SuppressAlertVignette(__instance);
            return false;
        }
    }
}

