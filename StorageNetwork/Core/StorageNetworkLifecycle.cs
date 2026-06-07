using StorageNetwork.API;
using StorageNetwork.ModConfig;
using StorageNetwork.ProductionOrders;
using StorageNetwork.UI;

namespace StorageNetwork.Core
{
    /// <summary>
    /// Centralizes runtime-only cleanup so static state does not leak between save games.
    /// </summary>
    public static class StorageNetworkLifecycle
    {
        public static void ResetRuntimeState()
        {
            StorageNetworkPanel.ResetRuntimeState();
            ModConfigDialog.ResetRuntimeState();
            StorageSceneRegistry.ResetRuntimeState();
            StorageSceneCollector.ResetRuntimeState();
            ProductionOrderService.ResetRuntimeState();
            StorageNetworkWorldPanelRegistry.ResetRuntimeState();
            StorageNetworkWorldTextPanel.ResetRuntimeState();
        }
    }
}
