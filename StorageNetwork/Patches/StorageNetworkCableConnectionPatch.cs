using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;

namespace StorageNetwork.Patches
{
    [HarmonyPatch(typeof(KAnimGraphTileVisualizer), "OnSpawn")]
    public static class StorageNetworkCableVisualizerOnSpawnPatch
    {
        public static bool Prefix(KAnimGraphTileVisualizer __instance)
        {
            StorageNetworkCable cable = __instance.GetComponent<StorageNetworkCable>();
            if (cable == null)
            {
                return true;
            }

            __instance.connectionManager = null;
            cable.RefreshSelfAndNeighbours();
            return false;
        }
    }

    [HarmonyPatch(typeof(KAnimGraphTileVisualizer), "OnCleanUp")]
    public static class StorageNetworkCableVisualizerOnCleanUpPatch
    {
        public static bool Prefix(KAnimGraphTileVisualizer __instance)
        {
            return __instance.GetComponent<StorageNetworkCable>() == null;
        }
    }

    [HarmonyPatch(typeof(KAnimGraphTileVisualizer), nameof(KAnimGraphTileVisualizer.UpdateConnections))]
    public static class StorageNetworkCableConnectionPatch
    {
        public static bool Prefix(KAnimGraphTileVisualizer __instance, UtilityConnections new_connections)
        {
            StorageNetworkCable cable = __instance.GetComponent<StorageNetworkCable>();
            if (cable == null)
            {
                return true;
            }

            __instance.Connections = new_connections;
            StorageNetworkRegistry.MarkDirty();
            cable.RefreshSelfAndNeighbours();
            return false;
        }
    }
}
