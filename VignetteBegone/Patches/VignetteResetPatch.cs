using HarmonyLib;

namespace VignetteBegone.Patches
{
    [HarmonyPatch(typeof(Vignette), nameof(Vignette.Reset))]
    public static class VignetteResetPatch
    {
        public static bool Prefix(Vignette __instance)
        {
            if (!VignetteController.IsHidden)
            {
                return true;
            }

            VignetteController.SuppressAlertVignette(__instance);
            return false;
        }
    }
}
