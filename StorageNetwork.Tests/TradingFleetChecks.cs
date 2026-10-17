using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Trading;

internal static class TradingFleetChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var goods = new List<TradingGood> { new TradingGood { Id = "Sandstone", Category = "Mineral", BasePrice = 1 } };
        var market = new TradingMarket { ExitPort = new TradingPort { Q = 8, R = -3 } };
        market.ConfigureFleet(new List<TradingPort> {
            new TradingPort { WorldId = 1, Q = 0, R = 0 },
            new TradingPort { WorldId = 2, Q = 5, R = 0 }
        }, 1, 0);
        market.Refresh(0, goods);
        check(market.Suppliers.Count == 0 && market.Voyages.Count == 1, "Fleet starts with a traveling merchant, not five docked merchants");
        var voyage = market.Voyages[0];
        var visitor = voyage.Visitor;
        check(voyage.Start > 0 && market.UpcomingVisit?.Visitor == visitor &&
            market.UpcomingVisit.WorldId == voyage.WorldId && market.UpcomingVisit.ExpectedCycle == voyage.Arrival,
            "The saved itinerary is available for advance news before the ship spawns");
        string plannedRoute = market.UpcomingVisit.RouteKey;
        market.Refresh(1, goods);
        check(market.UpcomingVisit.RouteKey == plannedRoute && market.UpcomingVisit.Visitor.Id == visitor.Id,
            "Opening news and advancing time before launch do not reroll the planned visit");
        int world = voyage.WorldId;
        check(!market.IsVisiting(visitor, world, 0), "In-flight merchants cannot trade");
        check(voyage.Progress(voyage.Start) == 0 && voyage.Progress(voyage.Arrival) == 1, "Route progress agrees with arrival time");
        market.Refresh(voyage.Arrival, goods);
        check(market.VisitorsAt(world, voyage.Arrival).Any(s => s.Id == visitor.Id), "Merchant docks at the scheduled destination and time");
        check(!market.IsVisiting(visitor, world == 1 ? 2 : 1, voyage.Arrival), "Merchant cannot trade from another asteroid");
        visitor.Departure = voyage.Arrival + .5;
        market.Refresh(visitor.Departure, goods);
        check(voyage.Outbound && !market.Suppliers.Any(s => s.Id == visitor.Id) && voyage.ToQ == 8 && voyage.ToR == -3,
            "Departing merchants fly to the temporal tear and cannot trade");
        double exitTime = voyage.Arrival;
        market.NextFleetDispatch = 999;
        market.Refresh(exitTime, goods);
        check(!market.Voyages.Contains(voyage), "Merchants disappear after reaching the temporal tear");
        market.NextFleetDispatch = exitTime;
        for (double cycle = 10; cycle < 220; cycle += .5)
        {
            market.Refresh(cycle, goods);
            check(market.VisitorsAt(1, cycle).Count <= 5 && market.VisitorsAt(2, cycle).Count <= 5, "Each asteroid respects its five-visitor maximum");
            check(market.Voyages.All(v => v.Outbound ? v.ToQ == 8 && v.ToR == -3 : market.TradePorts.Any(p => p.WorldId == v.WorldId && p.Q == v.ToQ && p.R == v.ToR)),
                "Every continuing voyage terminates at its destination asteroid");
        }
        var legacy = new TradingMarket();
        legacy.Refresh(0, goods);
        var old = legacy.Suppliers[0];
        decimal limit = old.BuyLimit;
        legacy.ConfigureFleet(new List<TradingPort> { new TradingPort { WorldId = 1 } }, 1, .1);
        legacy.Refresh(.1, goods);
        check(legacy.Suppliers.Contains(old) && old.VisitWorldId == 1 && old.BuyLimit == limit, "Old visits migrate without resetting identities or limits");
        var gated = new TradingMarket { ExitPort = new TradingPort { Q = 8, R = -3 } };
        var gates = new List<TradingPort> {
            new TradingPort { WorldId = 1, Q = 0, R = 0, Eligible = false },
            new TradingPort { WorldId = 2, Q = 5, R = 0, Eligible = false } };
        gated.ConfigureFleet(gates, 1, 0);
        gated.Refresh(0, goods);
        check(gated.Voyages.Count == 0, "No discovered asteroid with a completed core means no merchant dispatch");
        gates[1] = new TradingPort { WorldId = 2, Q = 5, R = 0, Eligible = true };
        gated.ConfigureFleet(gates, 1, 5);
        gated.Refresh(5, goods);
        check(gated.Voyages.Count == 1 && gated.Voyages[0].WorldId == 2, "Only eligible asteroids receive planned visits");
        gates[1] = new TradingPort { WorldId = 2, Q = 5, R = 0, Eligible = false };
        gated.ConfigureFleet(gates, 1, 6);
        gated.Refresh(6, goods);
        check(gated.Voyages[0].Outbound, "A disabled destination sends an incoming merchant away when no alternative exists");
        var populous = new TradingMarket();
        populous.ConfigureFleet(new List<TradingPort> {
            new TradingPort { WorldId = 1, Population = 12 },
            new TradingPort { WorldId = 2, Q = 5, Population = 7 },
            new TradingPort { WorldId = 3, Q = -5, Population = 2 }
        }, 1, 0);
        populous.Refresh(0, goods);
        check(populous.Voyages[0].WorldId == 1, "Merchants prefer the eligible asteroid with the most living duplicants");
        for (int i = 0; i < 5; i++) populous.Suppliers.Add(new TradingSupplier {
            Id = 500 + i, VisitWorldId = 1, Arrival = 0, Departure = 100, Specialty = "Mineral" });
        populous.NextFleetDispatch = 1;
        populous.Refresh(1, goods);
        check(populous.Voyages.Last().WorldId == 2, "A full most-populated asteroid falls back to the next most populated");
        var tiedWorlds = new HashSet<int>();
        for (int seed = 1; seed <= 20; seed++)
        {
            var tied = new TradingMarket { NextId = seed };
            tied.ConfigureFleet(new List<TradingPort> {
                new TradingPort { WorldId = 1, Population = 5 },
                new TradingPort { WorldId = 2, Q = 5, Population = 5 },
                new TradingPort { WorldId = 3, Q = -5, Population = 1 }
            }, 1, 0);
            tied.Refresh(0, goods);
            tiedWorlds.Add(tied.Voyages[0].WorldId);
        }
        check(tiedWorlds.SetEquals(new[] { 1, 2 }), "Equal highest populations are randomly selected without picking less-populated worlds");
        var oldOutbound = new TradingVoyage { Visitor = new TradingSupplier { Id = 100, Arrival = 0, Departure = 1 },
            Outbound = true, WorldId = 1, FromQ = 0, FromR = 0, ToQ = 99, ToR = 99, Start = 1, Arrival = 5 };
        market.Voyages.Clear(); market.Suppliers.Clear(); market.Voyages.Add(oldOutbound);
        market.NextFleetDispatch = 999;
        market.Refresh(221, goods);
        check(oldOutbound.Outbound && oldOutbound.ToQ == 8 && oldOutbound.ToR == -3,
            "Old exit routes are corrected to the temporal tear");
    }
}
