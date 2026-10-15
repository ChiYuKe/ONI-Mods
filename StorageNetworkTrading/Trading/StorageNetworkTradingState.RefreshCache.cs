using NetworkInventory = StorageNetwork.API.StorageNetworkInventoryAccess;
using System;

namespace StorageNetwork.Trading
{
    internal sealed partial class StorageNetworkTradingState
    {
        private int displayRefreshDepth;
        private bool bypassDisplayCache;
        private readonly TradingRefreshCache<(int World, string Good), float> availableCache = new TradingRefreshCache<(int, string), float>();
        private readonly TradingRefreshCache<(int World, string Good), double> minimumCache = new TradingRefreshCache<(int, string), double>();
        private readonly TradingRefreshCache<(int World, string Good, double Stock), double> capacityCache = new TradingRefreshCache<(int, string, double), double>();

        internal IDisposable BeginDisplayRefresh()
        {
            if (displayRefreshDepth++ == 0) ClearDisplayQueries();
            return new DisplayRefreshScope(this);
        }

        private void ClearDisplayQueries()
        {
            availableCache.Clear();
            minimumCache.Clear();
            capacityCache.Clear();
        }

        internal float DisplayAvailable(TradingGood good, int worldId) => displayRefreshDepth > 0 && !bypassDisplayCache
            ? availableCache.Get((worldId, good.Id), () => Available(good, worldId)) : Available(good, worldId);

        private double QuoteSaleMinimum(TradingGood good, int worldId) => displayRefreshDepth > 0 && !bypassDisplayCache
            ? minimumCache.Get((worldId, good.Id), () => MinimumSaleAmount(good, worldId)) : MinimumSaleAmount(good, worldId);

        private double QuotePurchaseCapacity(TradingGood good, int worldId, double stock) => displayRefreshDepth > 0 && !bypassDisplayCache
            ? capacityCache.Get((worldId, good.Id, stock), () => PurchaseCapacity(good, worldId, stock)) : PurchaseCapacity(good, worldId, stock);

        private static double PurchaseCapacity(TradingGood good, int worldId, double stock)
        {
            double total = 0;
            foreach (var target in PlanPurchase(good, worldId, stock)) total += target.Value;
            return total;
        }

        private sealed class DisplayRefreshScope : IDisposable
        {
            private StorageNetworkTradingState owner;
            internal DisplayRefreshScope(StorageNetworkTradingState owner) { this.owner = owner; }
            public void Dispose()
            {
                if (owner == null) return;
                if (--owner.displayRefreshDepth <= 0) { owner.displayRefreshDepth = 0; owner.ClearDisplayQueries(); }
                owner = null;
            }
        }
    }
}
