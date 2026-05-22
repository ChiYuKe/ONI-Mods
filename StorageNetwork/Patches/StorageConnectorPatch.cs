using HarmonyLib;
using StorageNetwork.Components;

namespace StorageNetwork.Patches
{
    [HarmonyPatch(typeof(Storage), "OnSpawn")]
    public static class StorageConnectorPatch
    {
        public static void Postfix(Storage __instance)
        {
            if (__instance == null || __instance.GetComponent<BuildingComplete>() == null)
            {
                return;
            }

            __instance.gameObject.AddOrGet<StorageNetworkStorageConnector>();
        }
    }
}
