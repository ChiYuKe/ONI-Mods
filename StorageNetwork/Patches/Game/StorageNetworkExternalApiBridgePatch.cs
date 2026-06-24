using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkExternalApiBridgePatch
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        [HarmonyPriority(Priority.Last)]
        private static class LoadGeneratedBuildingsPatch
        {
            public static void Postfix()
            {
                foreach (GameObject prefab in Assets.GetPrefabsWithComponent<KPrefabID>())
                {
                    StorageNetworkInterfaceResolver.InstallExternalApiBridgeIfNeeded(prefab);
                }
            }
        }
    }
}
