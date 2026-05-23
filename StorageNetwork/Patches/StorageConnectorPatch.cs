using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;

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

            KPrefabID prefabId = __instance.GetComponent<KPrefabID>();
            if (prefabId == null || !prefabId.HasTag(StorageNetworkTags.NetworkConnectable))
            {
                return;
            }

            StorageNetworkTags.EnsureStorageCategoryTag(__instance);
            __instance.gameObject.AddOrGet<StorageNetworkStorageConnector>();
            __instance.gameObject.AddOrGet<StorageNetworkPortVisualizer>();
        }
    }
}
