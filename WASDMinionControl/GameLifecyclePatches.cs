using HarmonyLib;
using UnityEngine;

namespace WASDMinionControl
{
    [HarmonyPatch(typeof(Game), "OnSpawn")]
    internal static class GameOnSpawnPatch
    {
        private static void Postfix(Game __instance)
        {
            if (__instance != null && __instance.GetComponent<WASDMinionController>() == null)
            {
                __instance.gameObject.AddComponent<WASDMinionController>();
            }
        }
    }

    [HarmonyPatch(typeof(Game), "OnCleanUp")]
    internal static class GameOnCleanUpPatch
    {
        private static void Prefix(Game __instance)
        {
            if (__instance == null)
            {
                return;
            }

            WASDMinionController controller = __instance.GetComponent<WASDMinionController>();
            if (controller != null)
            {
                Object.Destroy(controller);
            }

            ManualControlState.ClearAll();
            ManualControlInput.Clear();
        }
    }
}
