using System;

namespace StorageNetwork.Trading
{
    internal static class TradingQuoteSignals
    {
        // Shared market direction plus separate smooth supply/procurement cycles.
        // Pure seeded signals are stable while opening or refreshing the UI.
        internal static decimal Factor(TradingSupplier supplier, TradingGood good, double cycle, bool playerBuys)
        {
            if (supplier.Id == 0) return 1m; // The market reference has no vendor-specific spread signal.
            uint seed = unchecked((uint)supplier.Id * 2654435761u);
            foreach (char character in good.Id) seed = unchecked(seed * 31 + character);
            uint side = playerBuys ? seed ^ 0x85ebca6bu : seed ^ 0xc2b2ae35u;
            int period = playerBuys ? 13 + (int)(side % 11) : 9 + (int)(side % 11);
            double phase = Math.Floor(cycle) + side % (uint)period;
            decimal wave = (decimal)Math.Sin(phase / period * Math.PI * 2) * (playerBuys ? 0.04m : 0.05m);
            decimal state = 0;
            if (playerBuys && supplier.InitialStock != null && supplier.Stock != null &&
                supplier.InitialStock.TryGetValue(good.Id, out double initial) && initial > 0)
            {
                supplier.Stock.TryGetValue(good.Id, out double current);
                decimal stockRatio = (decimal)Math.Max(0, Math.Min(2, current / initial));
                state = (1m - stockRatio) * 0.02m;
            }
            else if (!playerBuys && supplier.BuyLimit > 0)
                state = -Math.Max(0, Math.Min(1, supplier.Bought / supplier.BuyLimit)) * 0.02m;
            return 1m + wave + state;
        }
    }
}
