using HarmonyLib;

namespace TestMod
{
    [HarmonyPatch(typeof(MeterScreen), "OnSpawn")]
    internal static class MeterScreenOnSpawnPatch
    {
        private static void Postfix(MeterScreen __instance)
        {
            TestModToggleButton.Create(__instance);
        }
    }
}
