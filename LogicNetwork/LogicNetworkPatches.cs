using HarmonyLib;
using LogicNetwork.UI.Installers;

namespace LogicNetwork
{
    internal static class LogicNetworkPatches
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        private static class GeneratedBuildingsLoadPatch
        {
            public static void Prefix()
            {
                LogicNetworkStrings.RegisterBuildingStrings();
                LogicNetworkBuildingPlanInstaller.Install();
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        private static class DetailsScreenPrefabInitPatch
        {
            public static void Postfix(DetailsScreen __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                LogicNetworkSideScreenInstaller.Install(__instance);
            }
        }
    }
}
