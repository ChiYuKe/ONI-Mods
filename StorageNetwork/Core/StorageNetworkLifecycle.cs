using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.ModConfig;
using StorageNetwork.ProductionOrders;
using StorageNetwork.Services;
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
            StorageNetworkLiquidOutputPortSideScreen.ResetRuntimeStyles();
            ModConfigDialog.ResetRuntimeState();
            StorageSceneRegistry.ResetRuntimeState();
            StorageSceneCollector.ResetRuntimeState();
            ProductionOrderService.ResetRuntimeState();
            StorageNetworkWorldPanelRegistry.ResetRuntimeState();
            StorageNetworkWorldTextPanel.ResetRuntimeState();
            StorageNetworkModInfoResolver.ResetRuntimeState();
            StorageNetworkInterfaceResolver.ResetRuntimeState();
            StorageNetworkInputTargetReservationService.ResetRuntimeState();
            StorageNetworkRocketRelayService.ResetRuntimeState();
            StorageNetworkPowerService.InvalidateAggregate();
            StorageNetworkPowerOverlayBattery.ResetRuntimeState();
            StorageNetworkEngravingDisk.ResetRuntimeState();
            StorageNetworkFluidStorageCompactor.ResetRuntimeState();
            StorageTargetSelector.ResetRuntimeState();
            StorageNetworkPerformanceCounters.ResetRuntimeState();
            StorageNetworkFrameProfileTool.ResetRuntimeState();
        }
    }
}
