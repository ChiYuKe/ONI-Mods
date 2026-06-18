using HarmonyLib;

namespace ONIVisualEnhancer.Patches
{
    [HarmonyPatch(typeof(Game), "OnSpawn")]
    public static class GameOnSpawnPatch
    {
        public static void Postfix()
        {
            VisualEnhancerController.EnsureOverlay();
        }
    }

    [HarmonyPatch(typeof(Game), "OnCleanUp")]
    public static class GameOnCleanUpPatch
    {
        public static void Postfix()
        {
            VisualEnhancerController.ResetRuntimeState();
        }
    }
}

