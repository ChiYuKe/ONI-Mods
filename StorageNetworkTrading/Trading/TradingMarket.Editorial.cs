using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    internal sealed partial class TradingMarket
    {
        private sealed class WeeklyCandidate
        {
            internal TradingWeeklyArticle Article;
            internal bool Once;
        }

        private static string WeeklySubject(TradingSupplier supplier) =>
            supplier.CharacterId ?? "visitor:" + supplier.Id;

        private List<TradingWeeklyArticle> RecentWeeklyArticles(int count) => WeeklyReports
            .Skip(Math.Max(0, WeeklyReports.Count - count)).SelectMany(r => r.Articles).ToList();

        private void AddWeeklyCandidate(List<WeeklyCandidate> pool, int edition, string column, string title,
            string wording, string topic, TradingSupplier subject, bool once, params string[] arguments)
        {
            if (once && WeeklyReports.Any(r => r.Articles.Any(a => a.Topic == topic))) return;
            int variants = wording.StartsWith("SKETCH_", StringComparison.Ordinal) ? 1 : 3;
            var recent = RecentWeeklyArticles(2);
            string chosen = wording;
            int start = (int)((WeeklyHash(topic) + (uint)edition) % (uint)variants);
            for (int offset = 0; offset < variants; offset++)
            {
                int variant = (start + offset) % variants;
                chosen = wording + (variant == 0 ? "" : "_ALT" + variant);
                if (!recent.Any(a => a.Wording == chosen)) break;
            }
            pool.Add(new WeeklyCandidate { Once = once, Article = new TradingWeeklyArticle {
                Column = column, Topic = topic, Subject = subject == null ? null : WeeklySubject(subject), Wording = chosen,
                Title = WeeklyText(title), Body = WeeklyText(chosen, arguments) } });
        }

        private static uint WeeklyHash(string value)
        {
            uint hash = 2166136261u;
            foreach (char c in value) hash = unchecked((hash ^ c) * 16777619u);
            return hash;
        }

        private List<TradingWeeklyArticle> SelectWeeklyArticles(List<WeeklyCandidate> pool, int edition)
        {
            var recent = RecentWeeklyArticles(3);
            var latest = RecentWeeklyArticles(1);
            var selected = new List<TradingWeeklyArticle>();
            // Prefer new subjects for three issues. In sparse markets, relax topic
            // cooldown to one issue; never repeat an identical paragraph or invent news.
            for (int pass = 0; pass < 2 && selected.Count < 8; pass++)
            {
                var cooldown = pass == 0 ? recent : latest;
                while (selected.Count < 8)
                {
                    var next = pool.Where(c => !selected.Any(a => a.Column == c.Article.Column) &&
                            !cooldown.Any(a => a.Topic == c.Article.Topic) && !recent.Any(a => a.Body == c.Article.Body))
                        .OrderBy(c => (c.Once ? -2000 : 0) +
                            (latest.Any(a => a.Column == c.Article.Column) ? 180 : 0) +
                            selected.Count(a => a.Subject != null && a.Subject == c.Article.Subject) * 350 +
                            (latest.Any(a => a.Subject != null && a.Subject == c.Article.Subject) ? 60 : 0) +
                            WeeklyHash(c.Article.Topic + ":" + edition) % 173)
                        .ThenBy(c => c.Article.Topic, StringComparer.Ordinal).FirstOrDefault();
                    if (next == null) break;
                    selected.Add(next.Article);
                }
            }
            return selected;
        }

        private List<WeeklyCandidate> BuildWeeklyCandidates(List<TradingSupplier> live,
            IReadOnlyList<TradingGood> catalog, double cycle, int edition)
        {
            var pool = new List<WeeklyCandidate>();
            TradingSupplier next = UpcomingVisit?.Visitor;
            if (next != null && FleetEnabled) AddWeeklyCandidate(pool, edition, "arrival", "ARRIVAL_TITLE", "ARRIVAL_PLAN", "arrival:" + UpcomingVisit.RouteKey,
                next, true, next.Name, UpcomingVisit.DestinationName ?? TradingText.Argument(UpcomingVisit.WorldId),
                TradingText.Argument(UpcomingVisit.LaunchCycle + 1, "0.0"), TradingText.Argument(UpcomingVisit.ExpectedCycle + 1, "0.0"),
                TradingPersonas.Specialty(next.Specialty));
            else if (next != null) AddWeeklyCandidate(pool, edition, "arrival", "ARRIVAL_TITLE", "ARRIVAL_NAMED", "arrival:" + next.Id,
                next, true, TradingText.Argument(UpcomingVisit.ExpectedCycle + 1, "0.0"),
                TradingPersonas.Specialty(next.Specialty), next.Persona.Public.Habit, next.Name);
            foreach (var s in live)
            {
                TradingPersonas.Ensure(s);
                string id = WeeklySubject(s);
                AddWeeklyCandidate(pool, edition, "portrait", "PORTRAIT_TITLE", "PORTRAIT", "portrait:" + id,
                    s, false, s.Name, s.Persona.Public.Habit, s.Persona.Public.Likes?.FirstOrDefault() ?? s.Persona.Public.Temperament);
                AddWeeklyCandidate(pool, edition, "sketch", "RUMOR_TITLE", "SKETCH_" + TradingRelationships.Temperament(s),
                    "sketch:" + id, s, false, s.Name);
                string contact = TradingRelationships.GrowthRate(s) >= 1.2m ? "CONTACT_FAST" :
                    TradingRelationships.GrowthRate(s) < 0.85m ? "CONTACT_SLOW" : "CONTACT";
                AddWeeklyCandidate(pool, edition, "contact", "CONTACT_TITLE", contact, "contact:" + id, s, false, s.Name);
                AddWeeklyCandidate(pool, edition, "route", "ROUTE_TITLE", "ROUTE", "route:" + s.Specialty,
                    s, false, TradingPersonas.Specialty(s.Specialty), s.Name);
                AddWeeklyCandidate(pool, edition, "procurement", "PROCUREMENT_TITLE", "PROCUREMENT", "procurement:" + id,
                    s, false, s.Name, TradingPersonas.Specialty(s.Specialty), TradingText.Argument(Math.Max(0, s.BuyLimit - s.Bought), "0.0"));
                AddWeeklyCandidate(pool, edition, "newcomer", "NEWCOMER_TITLE", "NEWCOMER", "newcomer:" + s.Id,
                    s, true, s.Name, TradingText.Argument(s.Arrival + 1, "0.0"), TradingPersonas.Specialty(s.Specialty));
            }
            var departing = live.OrderBy(s => s.Departure).First();
            AddWeeklyCandidate(pool, edition, "departure", "DEPARTURE_TITLE", "DEPARTURE", "departure:" + departing.Id,
                departing, true, departing.Name, TradingText.Argument(departing.Departure + 1, "0.0"), TradingPersonas.Specialty(departing.Specialty));
            var offers = live.SelectMany(s => catalog.Where(g => s.Stock.TryGetValue(g.Id, out double quantity) && quantity > 0)
                .Select(g => new { Supplier = s, Good = g, Price = Price(s, g, cycle, true) })).ToList();
            foreach (var offer in offers)
            {
                var s = offer.Supplier;
                var good = offer.Good;
                AddWeeklyCandidate(pool, edition, "cargo", "CARGO_TITLE", "CARGO", "cargo:" + good.Id, s, false,
                    s.Persona.Public.Habit, good.Name, TradingText.Argument(offer.Price, "0.00"));
                if (s.History == null || !s.History.TryGetValue(good.Id, out var points) || points.Count < 2) continue;
                decimal before = points[points.Count - 2].Buy, after = points.Last().Buy;
                if (before <= 0 || Math.Abs(after / before - 1) < 0.02m) continue;
                string direction = after > before ? "UP" : "DOWN";
                AddWeeklyCandidate(pool, edition, "movement", "MOVEMENT_TITLE", "MOVEMENT_" + direction,
                    "movement:" + good.Id + ":" + direction, s, false, good.Name, s.Name,
                    TradingText.Argument(before, "0.00"), TradingText.Argument(after, "0.00"));
            }
            foreach (var group in offers.GroupBy(o => o.Good.Id).Where(g => g.Count() > 1))
            {
                var low = group.OrderBy(o => o.Price).First();
                var high = group.OrderByDescending(o => o.Price).First();
                if (high.Price <= low.Price) continue;
                AddWeeklyCandidate(pool, edition, "comparison", "COMPARE_TITLE", "COMPARE", "comparison:" + low.Good.Id,
                    low.Supplier, false, low.Good.Name, low.Supplier.Name, TradingText.Argument(low.Price, "0.00"),
                    high.Supplier.Name, TradingText.Argument(high.Price, "0.00"));
            }
            return pool;
        }
    }
}
