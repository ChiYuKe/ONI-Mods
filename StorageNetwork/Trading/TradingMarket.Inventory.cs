using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StorageNetwork.Trading
{
    internal sealed class TradingWorldInventorySignal
    {
        public int Cycle = -1;
        public int HistoryCycle = -1;
        public Dictionary<string, double> Snapshot = new Dictionary<string, double>();
        public Dictionary<string, double> NeutralStock = new Dictionary<string, double>();
        public Dictionary<string, double> TradeDelta = new Dictionary<string, double>();
        public Dictionary<string, decimal> Factors = new Dictionary<string, decimal>();
        public Dictionary<string, List<TradingPricePoint>> History = new Dictionary<string, List<TradingPricePoint>>();
        public Dictionary<int, Dictionary<string, List<TradingPricePoint>>> SupplierHistory = new Dictionary<int, Dictionary<string, List<TradingPricePoint>>>();
    }

    internal sealed partial class TradingMarket
    {
        public Dictionary<int, TradingWorldInventorySignal> WorldInventorySignals = new Dictionary<int, TradingWorldInventorySignal>();
        [IgnoreDataMember] private int inventoryWorld = -1;

        internal bool NeedsInventorySnapshot(int worldId, double cycle)
        {
            return !WorldInventorySignals.TryGetValue(worldId, out var signal) || signal.Cycle < Math.Floor(cycle);
        }

        // Read the native cached totals once per cycle. Trades are removed from the
        // stock signal; production and consumption still contribute normally.
        internal void SampleInventory(int worldId, double cycle, IReadOnlyList<TradingGood> catalog, IReadOnlyDictionary<string, double> amounts)
        {
            if (!NeedsInventorySnapshot(worldId, cycle)) return;
            if (!WorldInventorySignals.TryGetValue(worldId, out var signal))
                WorldInventorySignals[worldId] = signal = new TradingWorldInventorySignal();
            bool first = signal.Cycle < 0;
            foreach (var good in catalog)
            {
                amounts.TryGetValue(good.Id, out double current);
                if (double.IsNaN(current) || double.IsInfinity(current)) current = 0;
                current = Math.Max(0, current);
                signal.Snapshot.TryGetValue(good.Id, out double previous);
                signal.NeutralStock.TryGetValue(good.Id, out double stock);
                signal.TradeDelta.TryGetValue(good.Id, out double trades);
                double neutral = first ? current : stock + current - previous - trades;
                // The first snapshot establishes a baseline. Its effect starts
                // next cycle, never when opening the trading window.
                decimal target = 1m - (decimal)(Math.Max(0, neutral) / (Math.Max(0, neutral) + (good.ByUnit ? 100d : 10000d))) * 0.06m;
                signal.Factors.TryGetValue(good.Id, out decimal factor);
                if (factor == 0) factor = 1m;
                signal.Factors[good.Id] = first ? 1m : factor + Math.Max(-0.005m, Math.Min(0.005m, target - factor));
                signal.NeutralStock[good.Id] = neutral;
                signal.Snapshot[good.Id] = current;
            }
            signal.TradeDelta.Clear();
            signal.Cycle = (int)Math.Floor(cycle);
        }

        internal void RecordInventoryTrade(int worldId, string goodId, double signedAmount)
        {
            if (!WorldInventorySignals.TryGetValue(worldId, out var signal)) return;
            signal.TradeDelta.TryGetValue(goodId, out double previous);
            signal.TradeDelta[goodId] = previous + signedAmount;
        }

        internal void SelectInventoryWorld(int worldId)
        {
            if (!WorldInventorySignals.ContainsKey(worldId)) return;
            if (inventoryWorld != worldId)
            {
                if (WorldInventorySignals.TryGetValue(inventoryWorld, out var old)) old.HistoryCycle = LastHistoryCycle;
                inventoryWorld = worldId;
                var signal = WorldInventorySignals[worldId];
                History = signal.History;
                LastHistoryCycle = signal.HistoryCycle;
                QuoteVersion++;
            }
            foreach (var supplier in Suppliers) AttachInventory(supplier);
        }

        private void AttachInventory(TradingSupplier supplier)
        {
            if (!WorldInventorySignals.TryGetValue(inventoryWorld, out var signal)) return;
            supplier.InventoryFactors = signal.Factors;
            if (!signal.SupplierHistory.TryGetValue(supplier.Id, out var history))
                signal.SupplierHistory[supplier.Id] = history = new Dictionary<string, List<TradingPricePoint>>();
            supplier.History = history;
        }

        private void PruneInventoryHistory()
        {
            var active = new HashSet<int>();
            foreach (var supplier in Suppliers) active.Add(supplier.Id);
            foreach (var signal in WorldInventorySignals.Values)
            {
                var expired = new List<int>();
                foreach (int id in signal.SupplierHistory.Keys)
                    if (!active.Contains(id)) expired.Add(id);
                foreach (int id in expired) signal.SupplierHistory.Remove(id);
            }
        }
    }
}
