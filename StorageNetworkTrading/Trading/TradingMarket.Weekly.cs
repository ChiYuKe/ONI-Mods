using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    internal sealed class TradingWeeklyArticle
    {
        public string Title, Body;
        public string Topic, Subject, Column, Wording;
    }
    internal sealed class TradingWeeklyReport
    {
        public int Number;
        public double Cycle;
        public bool Read;
        public List<TradingWeeklyArticle> Articles = new List<TradingWeeklyArticle>();
    }
    internal sealed class TradingArrivalPlan
    {
        public double ExpectedCycle;
        public TradingSupplier Visitor;
    }
    internal sealed partial class TradingMarket
    {
        public List<TradingWeeklyReport> WeeklyReports = new List<TradingWeeklyReport>();
        public int LastBulletinWeek = -1;
        public TradingArrivalPlan UpcomingVisit;

        private static string WeeklyText(string key, params string[] args) =>
            TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.WEEKLY_" + key, args);

        public bool UpdateWeeklyReports(double cycle, IReadOnlyList<TradingGood> catalog,
            IReadOnlyList<TradingIdentityTemplate> templates)
        {
            if (double.IsNaN(cycle) || double.IsInfinity(cycle) || cycle < 0 || Suppliers.Count == 0) return false;
            WeeklyReports = WeeklyReports ?? new List<TradingWeeklyReport>();
            TradingWeeklyReport previous = WeeklyReports.LastOrDefault();
            // Count seven full cycles from publication, never from a calendar bucket.
            if (previous != null && cycle < previous.Cycle + 7) return false;
            int week = previous?.Number ?? 0;
            var live = Suppliers.Where(s => s.Departure > cycle).ToList();
            if (live.Count == 0) return false;
            // Reserve the next visitor's identity, not a slot or a visit duration.
            // Refresh consumes this exact visitor when the next departure occurs.
            if (UpcomingVisit?.Visitor == null || UpcomingVisit.Visitor.Id != NextId)
            {
                double arrival = live.Min(s => s.Departure);
                var random = new Random(NextId * 7919);
                var visitor = new TradingSupplier { Id = NextId, Specialty = ChooseSpecialty(NextId, catalog),
                    Arrival = arrival, Departure = arrival, BuyLimit = random.Next(20, 81) * 1000m,
                    SellLimit = random.Next(20, 81) * 1000m };
                TradingCharacters.Assign(visitor, Suppliers, arrival);
                EnsureIdentity(visitor, templates);
                TradingPersonas.Ensure(visitor);
                UpcomingVisit = new TradingArrivalPlan { ExpectedCycle = arrival, Visitor = visitor };
            }
            var report = new TradingWeeklyReport { Number = week + 1, Cycle = cycle };
            report.Articles = SelectWeeklyArticles(BuildWeeklyCandidates(live, catalog, cycle, week), week);
            WeeklyReports.Add(report);
            if (WeeklyReports.Count > 16) WeeklyReports.RemoveRange(0, WeeklyReports.Count - 16);
            LastBulletinWeek = week;
            return true;
        }

    }
}
