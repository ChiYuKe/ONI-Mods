using System;
using System.Linq;
using StorageNetwork.Services;
using StorageNetwork.Trading;
using UnityEngine;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void RefreshTradingDeliveryPreview(StorageNetworkTradingState state)
        {
            if (tradingDeliveryText == null || tradingQuote == null) return;
            tradingLocateStorages.Clear();
            string unit = tradingQuoteGood.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7C0025557708") : "kg";
            if (tradingQuoteBuy)
            {
                var targets = state.DeliveryPreview(tradingQuoteGood, tradingQuoteWorldId, tradingQuote.Amount);
                tradingLocateStorages.AddRange(targets.Select(target => target.Storage));
                string details = string.Join("\n", targets.Take(2).Select(target =>
                    target.Storage.gameObject.GetProperName() + TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DB40DE77E4A8", TradingText.Argument((object)(target.Amount), "#,0.###", 0), TradingText.Argument((object)(unit), null, 0))));
                tradingDeliveryText.text = TradingText.Render(targets.Count > 0
                    ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_62F98E846C6F", TradingText.Argument((object)(targets.Count), null, 0)) + (targets.Count > 2 ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D21B81A2B3E7") : "") + "\n" + details
                    : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DB8CE41D0560") + (tradingQuote.Reason ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_69643FA9C069")));
                tradingDeliveryText.GetComponent<ToolTip>().toolTip = TradingText.Render(string.Join("\n", targets.Select(target =>
                    target.Storage.gameObject.GetProperName() + TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DB40DE77E4A8", TradingText.Argument((object)(target.Amount), "#,0.###", 0), TradingText.Argument((object)(unit), null, 0)))));
            }
            else
            {
                tradingLocateStorages.AddRange(state.InspectionStorages(tradingQuoteGood, tradingQuoteWorldId)
                    .Where(storage => storage.items.Any(item => item != null && StorageItemUtility.GetStoredItemKey(item) == tradingQuoteGood.Id)));
                tradingDeliveryText.text = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FB67EF22D3B4", TradingText.Argument((object)(StorageNetworkTradingState.Available(tradingQuoteGood, tradingQuoteWorldId)), "#,0.###", 0), TradingText.Argument((object)(unit), null, 0)));
            }
            if (tradingLocateStorages.Count == 0)
                tradingLocateStorages.AddRange(state.InspectionStorages(tradingQuoteGood, tradingQuoteWorldId));
            tradingLocateButton.isInteractable = tradingLocateStorages.Count > 0;
            if (tradingLocateIndex >= tradingLocateStorages.Count) tradingLocateIndex = 0;
        }

        private void LocateTradingStorage()
        {
            if (tradingLocateStorages.Count == 0) return;
            tradingLocateIndex %= tradingLocateStorages.Count;
            Storage storage = tradingLocateStorages[tradingLocateIndex++];
            if (storage != null) FocusStorage(storage, 350);
        }

        private void LocateTradingReceipt(TradingReceipt receipt)
        {
            if (receipt.StorageIds == null) return;
            var target = Core.StorageSceneRegistry.GetStorages().FirstOrDefault(storage => storage != null &&
                receipt.StorageIds.Contains(StorageItemUtility.GetStorageInstanceId(storage)));
            if (target != null) FocusStorage(target, 350);
            else tradingMessage = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_31393907B495");
        }
    }
}
