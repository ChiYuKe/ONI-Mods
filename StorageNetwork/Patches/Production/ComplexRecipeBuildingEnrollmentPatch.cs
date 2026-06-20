using HarmonyLib;
using StorageNetwork.Gameplay;

namespace StorageNetwork.Patches
{
    public static class ComplexRecipeBuildingEnrollmentPatch
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class LoadGeneratedBuildingsPatch
        {
            public static void Postfix()
            {
                StorageNetworkEnrollmentInstaller.InstallComplexRecipeBuildingPrefabs();
            }
        }
    }
}
