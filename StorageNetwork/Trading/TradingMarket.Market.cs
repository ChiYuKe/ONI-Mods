using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    internal sealed partial class TradingMarket
    {
        public Dictionary<string, decimal> Pressure = new Dictionary<string, decimal>();
        public Dictionary<string, List<TradingPricePoint>> History = new Dictionary<string, List<TradingPricePoint>>();
        public Dictionary<string, double> LastAmounts = new Dictionary<string, double>();
        public int LastPressureCycle = -1;
        public int LastHistoryCycle = -1;
        public int QuoteVersion;
        public bool ShouldSerializeHistory() => inventoryWorld < 0;

        private bool PrepareMarket(double cycle)
        {
            Suppliers = Suppliers ?? new List<TradingSupplier>();
            Receipts = Receipts ?? new List<TradingReceipt>();
            Pressure = Pressure ?? new Dictionary<string, decimal>();
            History = History ?? new Dictionary<string, List<TradingPricePoint>>();
            LastAmounts = LastAmounts ?? new Dictionary<string, double>();
            Suppliers.RemoveAll(supplier => supplier == null);
            foreach (TradingSupplier supplier in Suppliers)
            {
                supplier.Stock = supplier.Stock ?? new Dictionary<string, double>();
                supplier.History = supplier.History ?? new Dictionary<string, List<TradingPricePoint>>();
            }
            int day = (int)Math.Floor(cycle);
            if (LastPressureCycle < 0) { LastPressureCycle = day; return true; }
            if (day <= LastPressureCycle) return false;
            decimal decay = Math.Min(8, day - LastPressureCycle) * 0.005m;
            foreach (string id in Pressure.Keys.ToList())
            {
                decimal value = Pressure[id];
                Pressure[id] = Math.Sign(value) * Math.Max(0, Math.Abs(value) - decay);
                if (Pressure[id] == 0) Pressure.Remove(id);
            }
            LastPressureCycle = day;
            return true;
        }

        private static bool Restock(TradingSupplier supplier, double cycle, IReadOnlyList<TradingGood> catalog)
        {
            supplier.Stock = supplier.Stock ?? new Dictionary<string, double>();
            supplier.InitialStock = supplier.InitialStock ?? new Dictionary<string, double>();
            supplier.History = supplier.History ?? new Dictionary<string, List<TradingPricePoint>>();
            int day = (int)Math.Floor(cycle);
            if (supplier.LastRestockCycle < 0)
            {
                supplier.InitialStock = new Dictionary<string, double>(supplier.Stock);
                supplier.LastRestockCycle = day;
                return true;
            }
            int elapsed = day - supplier.LastRestockCycle;
            if (elapsed <= 0) return false;
            supplier.LastRestockCycle = day;
            foreach (TradingGood good in catalog)
            {
                if (!supplier.InitialStock.TryGetValue(good.Id, out double initial) || initial <= 0) continue;
                supplier.Stock.TryGetValue(good.Id, out double stock);
                if (stock >= initial) continue;
                double increment = good.ByUnit ? Math.Max(1, Math.Floor(initial * 0.1)) : initial * 0.1;
                supplier.Stock[good.Id] = Math.Min(initial, stock + increment * elapsed);
            }
            // This never resets Bought/Sold or either visit's monetary limit.
            return true;
        }

        internal static bool Prefers(TradingSupplier supplier, TradingGood good)
        {
            return (Profile(supplier) == "Premium" && (good.PreferenceTag == "RefinedMetal" || good.PreferenceTag == "CookedFood" ||
                       (good.Category == "Mineral" && good.BasePrice >= 40m) || (good.ByUnit && good.BasePrice >= 100m))) ||
                   (Profile(supplier) == "Clearance" && (good.PreferenceTag == "CookedFood" ||
                       (good.Category == "Metal" && good.PreferenceTag == "RawMaterial"))) ||
                   (Profile(supplier) == "Bulk" && good.PreferenceTag == "RawMaterial");
        }

        internal static string PreferenceName(TradingSupplier supplier)
        {
            if (Profile(supplier) == "Bulk") return supplier.Specialty == "Food" || supplier.Specialty == "Seed" || supplier.Specialty == "Equipment"
                ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A39F9A83DA37") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5D4C020E4990");
            if (supplier.Specialty == "Food") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_1EAFDB69F655");
            if (supplier.Specialty == "Metal") return Profile(supplier) == "Premium" ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5ADEC8CCD295") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_360BF49A7351");
            if (Profile(supplier) == "Premium" && supplier.Specialty == "Mineral") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D670A6E2568C");
            if (Profile(supplier) == "Premium" && (supplier.Specialty == "Equipment" || supplier.Specialty == "Seed")) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2DCB224BB615");
            return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A39F9A83DA37");
        }

        internal static string ConfirmationProblem(TradingQuote quote, decimal expectedPrice, double expectedCycle, double requested, bool requireFull)
        {
            if (!quote.CanTrade) return quote.Reason;
            if (expectedCycle >= 0 && Math.Floor(expectedCycle) != Math.Floor(quote.Cycle)) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B6B91975CE17");
            if (expectedPrice > 0 && quote.UnitPrice != expectedPrice) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7E3F4B650360");
            if (requireFull && quote.Amount + 0.000001 < requested) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E67853F9B56B");
            return null;
        }

        internal static string EventName(string category, double cycle)
        {
            int block = (int)Math.Floor(cycle / 8) % 8;
            if (block == 0 && category == "Food") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D868FC75D6AB");
            if (block == 1 && category == "Metal") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5D39E98256F8");
            if (block == 2 && category == "Industrial") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E3AC87C512D6");
            if (block == 3 && (category == "Gas" || category == "Liquid")) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_EF6873CA2C27");
            if (block == 4 && category == "Seed") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_805A3E11A917");
            if (block == 5 && category == "Mineral") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C2AFF06ED028");
            return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_984F0E9A7180");
        }

        internal static decimal EventFactor(string category, double cycle)
        {
            string name = EventName(category, cycle);
            if (name == TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_984F0E9A7180")) return 1m;
            int phase = (int)Math.Floor(cycle) % 8;
            decimal strength = Math.Min(phase, 7 - phase) / 3m;
            int block = (int)Math.Floor(cycle / 8) % 8;
            bool down = (block == 0 && category == "Food") || (block == 2 && category == "Industrial") || (block == 5 && category == "Mineral");
            return 1m + (down ? -0.04m : 0.04m) * strength;
        }

        internal decimal ReferencePrice(TradingGood good, double cycle)
        {
            // Neutral merchant and spread; comparable across different visitors.
            var neutral = new TradingSupplier { Id = 0, Pressure = Pressure,
                InventoryFactors = WorldInventorySignals.TryGetValue(inventoryWorld, out var signal) ? signal.Factors : null };
            return Math.Max(0.01m, Math.Round(Price(neutral, good, cycle, true) / (0.90m * 1.07m), 2));
        }

        private bool RecordPrices(double cycle, IReadOnlyList<TradingGood> catalog, bool force)
        {
            int day = (int)Math.Floor(cycle);
            if (!force && LastHistoryCycle == day) return false;
            foreach (TradingGood good in catalog) RecordGood(good, day);
            // Sampling begins when the feature exists in the save. Missing days
            // are not backfilled using today's pressure or a different supplier.
            LastHistoryCycle = day;
            if (WorldInventorySignals.TryGetValue(inventoryWorld, out var signal)) signal.HistoryCycle = day;
            QuoteVersion++;
            return true;
        }

        private void RecordGood(TradingGood good, int day)
        {
            decimal reference = ReferencePrice(good, day);
            AddPoint(History, good.Id, new TradingPricePoint { Cycle = day, Buy = reference, Sell = reference }, 60);
            foreach (TradingSupplier supplier in Suppliers)
                if (supplier.Specialty == good.Category)
                    AddPoint(supplier.History, good.Id, new TradingPricePoint { Cycle = day,
                        Buy = Price(supplier, good, day, true), Sell = Price(supplier, good, day, false) }, 30);
        }

        private static void AddPoint(Dictionary<string, List<TradingPricePoint>> history, string id, TradingPricePoint point, int limit)
        {
            if (!history.TryGetValue(id, out List<TradingPricePoint> points) || points == null)
                history[id] = points = new List<TradingPricePoint>();
            if (points.Count > 0 && points[points.Count - 1].Cycle == point.Cycle) points[points.Count - 1] = point;
            else points.Add(point);
            if (points.Count > limit) points.RemoveRange(0, points.Count - limit);
        }

        internal List<TradingPricePoint> ReferenceHistory(string goodId, int cycles, double now)
        {
            int first = (int)Math.Floor(now) - Math.Max(1, Math.Min(60, cycles)) + 1;
            return History.TryGetValue(goodId, out List<TradingPricePoint> points)
                ? points.Where(p => p.Cycle >= first && p.Cycle <= Math.Floor(now)).ToList() : new List<TradingPricePoint>();
        }
    }
}
