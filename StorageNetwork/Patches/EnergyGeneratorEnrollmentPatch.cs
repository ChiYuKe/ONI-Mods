using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class EnergyGeneratorEnrollmentPatch
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class LoadGeneratedBuildingsPatch
        {
            public static void Postfix()
            {
                foreach (GameObject prefab in Assets.GetPrefabsWithComponent<EnergyGenerator>())
                {
                    EnergyGenerator generator = prefab != null ? prefab.GetComponent<EnergyGenerator>() : null;
                    if (!StorageNetworkEnergyGeneratorRequester.HasFuelInputs(generator) || prefab.GetComponent<Storage>() == null)
                    {
                        continue;
                    }

                    prefab.AddOrGet<StorageNetworkEnrollment>();
                    prefab.AddOrGet<StorageNetworkEnergyGeneratorRequester>();
                }
            }
        }
    }
}
