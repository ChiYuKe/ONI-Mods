using StorageNetwork.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetworkTrading.UI
{
    public sealed partial class TradingPanel
    {
        private static string StarMapText(string key, params string[] args) =>
            TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.STARMAP_" + key, args);

        private void ShowTradingStarMap()
        {
            if (!DlcManager.FeatureClusterSpaceEnabled() || ManagementMenu.Instance == null) return;
            CloseTradingWindow();
            ManagementMenu.Instance.OpenClusterMap();
        }

        internal static void ShowNativeTradingSupplier(int supplierId)
        {
            var state = StorageNetworkTradingState.Instance;
            state?.Refresh();
            var visitors = state?.LocalVisitors(StorageNetwork.API.StorageNetworkUiAccess.CurrentWorldId);
            int slot = visitors?.FindIndex(s => s.Id == supplierId) ?? -1;
            if (slot < 0) return;
            ManagementMenu.Instance?.CloseClusterMap();
            if (!IsOpen()) Show();
            instance.CloseModal();
            instance.tradingLocalSuppliers = visitors;
            instance.tradingSlot = slot;
            instance.tradingPage = 0;
            instance.tradingFilter = string.Empty;
            instance.tradingQuickFilter = 0;
            if (instance.tradingSearchInput != null) instance.tradingSearchInput.text = string.Empty;
            instance.tradingQuoteGood = null;
            instance.tradingRowsSignature = null;
            instance.RefreshTradingWindow();
            instance.RequestTradingLayout(true);
        }
    }
}
