using System;

namespace StorageNetwork.Trading
{
    internal static class TradingQuoteSignals
    {
        // Shared market direction plus independent supply/procurement signals.
        // Pure seeded signals are stable while opening or refreshing the UI.
        internal static decimal MarketFactor(TradingGood good, double cycle)
        {
            uint seed = 2166136261u;
            foreach (char character in good.Id) seed = unchecked(seed * 31u + character);
            int day = (int)Math.Floor(cycle);
            int period = 32 + (int)(seed % 33);
            double position = (day + seed % (uint)period) % period;
            decimal trend = (decimal)(1d - 4d * Math.Abs(position / period - 0.5d)) * 0.20m;
            // Three-cycle moves create runs and reversals; daily news adds smaller jolts.
            decimal shortMove = Interpolate(seed ^ 0x68bc21ebu, day, 3) * 0.14m;
            decimal dailyNews = Noise(seed ^ 0x02e5be93u, day) * 0.045m;
            // An occasional five-cycle supply/demand shock, tied to the good rather
            // than a visitor, refresh order, or player trading volume.
            int window = day / 20;
            uint shockSeed = Mix(seed ^ unchecked((uint)window * 0x9e3779b9u));
            int start = 3 + (int)(shockSeed % 12);
            int age = day % 20 - start;
            decimal shock = 0;
            if ((shockSeed >> 8) % 3 != 0 && age >= 0 && age < 5)
            {
                decimal envelope = age == 0 || age == 4 ? 0.3m : age == 2 ? 1m : 0.7m;
                decimal magnitude = 0.14m + ((shockSeed >> 16) % 11) / 100m;
                shock = ((shockSeed & 0x80000000u) == 0 ? 1 : -1) * magnitude * envelope;
            }
            return Math.Max(0.45m, Math.Min(1.70m, 1m + trend + shortMove + dailyNews + shock));
        }

        private static uint Mix(uint value)
        {
            value = unchecked((value ^ (value >> 16)) * 0x85ebca6bu);
            value = unchecked((value ^ (value >> 13)) * 0xc2b2ae35u);
            return value ^ (value >> 16);
        }
        private static decimal Noise(uint seed, int day) =>
            (Mix(seed ^ unchecked((uint)day * 0x9e3779b9u)) % 20001) / 10000m - 1m;
        private static decimal Interpolate(uint seed, int day, int length)
        {
            int block = day / length;
            decimal fraction = (day % length) / (decimal)length;
            return Noise(seed, block) * (1m - fraction) + Noise(seed, block + 1) * fraction;
        }

        internal static decimal Factor(TradingSupplier supplier, TradingGood good, double cycle, bool playerBuys)
        {
            if (supplier.Id == 0) return 1m; // The market reference has no vendor-specific spread signal.
            uint seed = unchecked((uint)supplier.Id * 2654435761u);
            foreach (char character in good.Id) seed = unchecked(seed * 31 + character);
            uint side = playerBuys ? seed ^ 0x85ebca6bu : seed ^ 0xc2b2ae35u;
            int period = playerBuys ? 13 + (int)(side % 11) : 9 + (int)(side % 11);
            double phase = Math.Floor(cycle) + side % (uint)period;
            decimal wave = (decimal)Math.Sin(phase / period * Math.PI * 2) * (playerBuys ? 0.075m : 0.085m);
            wave += Interpolate(side ^ 0xa5139b47u, (int)Math.Floor(cycle), 2) * 0.045m;
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
