using HarmonyLib;
using Klei.AI;

namespace MiniBox.MiscConfig
{
    [HarmonyPatch(typeof(AmountInstance), "GetDelta")]
    internal class StressPatch
    {
        private static void Postfix(AmountInstance __instance, ref float __result)
        {
            if (ModSettings.Current.DisableStress && __instance.amount.Id == "Stress" && __result > 0f)
                __result = 0f;
        }
    }
}
