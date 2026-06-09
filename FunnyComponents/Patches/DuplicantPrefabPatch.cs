using FunnyComponents.Components;
using HarmonyLib;
using UnityEngine;

namespace FunnyComponents.Patches
{
    internal static class DuplicantPrefabPatch
    {
        [HarmonyPatch(typeof(BaseMinionConfig), "BaseMinion")]
        private static class BaseMinionConfig_BaseMinion_Patch
        {
            private static void Postfix(GameObject __result)
            {
                if (__result == null)
                {
                    return;
                }

                __result.AddOrGet<ColorPulseMarker>();
                __result.AddOrGet<PeriodicDuplicantPoke>();
                __result.AddOrGet<MiniBlackHole>();
            }
        }
    }
}
