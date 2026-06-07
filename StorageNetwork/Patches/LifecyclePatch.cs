using HarmonyLib;
using StorageNetwork.Core;

namespace StorageNetwork.Patches
{
    public static class LifecyclePatch
    {
        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class GameOnSpawnPatch
        {
            public static void Prefix()
            {
                StorageNetworkLifecycle.ResetRuntimeState();
            }
        }

        [HarmonyPatch(typeof(Game), "OnCleanUp")]
        public static class GameOnCleanUpPatch
        {
            public static void Postfix()
            {
                StorageNetworkLifecycle.ResetRuntimeState();
            }
        }

        [HarmonyPatch(typeof(Game), "OnDestroy")]
        public static class GameOnDestroyPatch
        {
            public static void Postfix()
            {
                StorageNetworkLifecycle.ResetRuntimeState();
            }
        }
    }
}
