using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class ComplexRecipeBuildingEnrollmentPatch
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class LoadGeneratedBuildingsPatch
        {
            public static void Postfix()
            {
                foreach (GameObject prefab in Assets.GetPrefabsWithComponent<ComplexFabricator>())
                {
                    if (prefab != null && prefab.GetComponent<Storage>() != null)
                    {
                        prefab.AddOrGet<StorageNetworkEnrollment>();
                        prefab.AddOrGet<StorageNetworkMaterialRequester>();
                    }
                }
            }
        }
    }
}
