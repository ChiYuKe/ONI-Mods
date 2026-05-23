using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;

namespace StorageNetwork.Patches
{
    [HarmonyPatch(typeof(KAnimGraphTileVisualizer), nameof(KAnimGraphTileVisualizer.UpdateConnections))]
    public static class StorageNetworkCableConnectionPatch
    {
        public static void Postfix(KAnimGraphTileVisualizer __instance)
        {
            StorageNetworkCable cable = __instance.GetComponent<StorageNetworkCable>();
            if (cable == null)
            {
                return;
            }

            StorageNetworkRegistry.MarkDirty();
            cable.RefreshSelfAndNeighbours();
        }
    }
}
