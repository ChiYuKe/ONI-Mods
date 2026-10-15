using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    internal sealed class TradingMerchantTip
    {
        public int SupplierId, Window;
        public double Cycle, Expires;
        public bool Read;
        public List<TradingWeeklyArticle> Articles = new List<TradingWeeklyArticle>();
    }
    internal sealed partial class TradingMarket
    {
        public Dictionary<int, TradingMerchantTip> MerchantTips = new Dictionary<int, TradingMerchantTip>();
        public int LastTipDay = -1;
        public double LastMerchantTipCycle = -1;
        public int MerchantTipEdition;

        internal bool UpdateMerchantTips(double cycle, IReadOnlyList<TradingGood> catalog)
        {
            if (double.IsNaN(cycle) || double.IsInfinity(cycle) || cycle < 0) return false;
            int day = (int)Math.Floor(cycle);
            MerchantTips = MerchantTips ?? new Dictionary<int, TradingMerchantTip>();
            if (LastMerchantTipCycle < 0 && MerchantTips.Count > 0)
                LastMerchantTipCycle = MerchantTips.Values.Max(t => t.Cycle);
            bool due = LastMerchantTipCycle < 0 || cycle >= LastMerchantTipCycle + 7;
            if (!due && day == LastTipDay) return false;
            LastTipDay = day;
            bool changed = false;
            foreach (int id in MerchantTips.Keys.ToList())
                if (MerchantTips[id].Expires <= cycle || !Suppliers.Any(s => s.Id == id && s.Departure > cycle))
                { MerchantTips.Remove(id); changed = true; }
            if (!due) return changed;
            var live = Suppliers.Where(s => s.Departure > cycle).OrderBy(s => s.Id).ToList();
            if (live.Count == 0) return changed;
            LastMerchantTipCycle = cycle;
            int window = MerchantTipEdition++;
            MerchantTips.Clear();
            // One batch per seven elapsed cycles, with at most two speakers.
            foreach (int offset in new[] { 0, 2 }.Distinct())
            {
                var supplier = live[(window % live.Count * 3 + offset) % live.Count];
                if (MerchantTips.TryGetValue(supplier.Id, out var prior) && prior.Window == window) continue;
                if (MerchantTips.Values.Count(t => t.Window == window) >= 2) break;
                var tip = new TradingMerchantTip { SupplierId = supplier.Id, Window = window, Cycle = cycle,
                    Expires = Math.Min(supplier.Departure, cycle + 7) };
                if (UpcomingVisit?.Visitor != null)
                    tip.Articles.Add(new TradingWeeklyArticle { Title = WeeklyText("TIP_ROUTE_TITLE"),
                        Body = WeeklyText("TIP_ROUTE_" + window % 2, TradingPersonas.Specialty(UpcomingVisit.Visitor.Specialty),
                            TradingText.Argument(UpcomingVisit.ExpectedCycle + 1, "0.0")) });
                var goods = catalog.Where(g => supplier.Stock.TryGetValue(g.Id, out double amount) && amount > 0)
                    .OrderBy(g => g.Id, StringComparer.Ordinal).ToList();
                if (goods.Count > 0)
                {
                    var good = goods[window % goods.Count];
                    tip.Articles.Add(new TradingWeeklyArticle { Title = WeeklyText("TIP_CARGO_TITLE"),
                        Body = WeeklyText("TIP_CARGO_" + TradingRelationships.Temperament(supplier) % 3, good.Name,
                            TradingText.Argument(Price(supplier, good, cycle, true), "0.00"),
                            TradingText.Argument(supplier.Stock[good.Id], "0.#") + " " + WeeklyText(good.ByUnit ? "UNIT_ITEM" : "UNIT_KG")) });
                }
                string special = supplier.CharacterId;
                tip.Articles.Add(new TradingWeeklyArticle { Title = WeeklyText("TIP_ASIDE_TITLE"),
                    Body = special == "greyfinch" || special == "oldmi" || special == "lamplow"
                        ? WeeklyText("TIP_ASIDE_" + special.ToUpperInvariant())
                        : WeeklyText("TIP_ASIDE_" + TradingRelationships.Temperament(supplier) % 3) });
                MerchantTips[supplier.Id] = tip;
            }
            return true;
        }

        internal TradingMerchantTip PendingMerchantTip(int supplierId, double cycle) =>
            MerchantTips != null && MerchantTips.TryGetValue(supplierId, out var tip) && !tip.Read &&
            tip.Expires > cycle && Suppliers.Any(s => s.Id == supplierId && s.Departure > cycle) ? tip : null;
    }
}
