using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Trading;

internal static class TradingFleetChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var goods = new List<TradingGood> { new TradingGood { Id = "Sandstone", Category = "Mineral", BasePrice = 1 } };
        var market = new TradingMarket();
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
        check(!voyage.Outbound && !market.Suppliers.Any(s => s.Id == visitor.Id) && voyage.WorldId != world,
            "Departure routes the same merchant to another real asteroid and removes local trading");
        check(market.TradePorts.Any(p => p.WorldId == voyage.WorldId && p.Q == voyage.ToQ && p.R == voyage.ToR),
            "The next destination is an asteroid coordinate, never an empty hex");
        for (double cycle = 10; cycle < 220; cycle += .5)
        {
            market.Refresh(cycle, goods);
            check(market.VisitorsAt(1, cycle).Count <= 5 && market.VisitorsAt(2, cycle).Count <= 5, "Each asteroid respects its five-visitor maximum");
            check(market.Voyages.All(v => market.TradePorts.Any(p => p.WorldId == v.WorldId && p.Q == v.ToQ && p.R == v.ToR)),
                "Every continuing voyage terminates at its destination asteroid");
        }
        var legacy = new TradingMarket();
        legacy.Refresh(0, goods);
        var old = legacy.Suppliers[0];
        decimal limit = old.BuyLimit;
        legacy.ConfigureFleet(new List<TradingPort> { new TradingPort { WorldId = 1 } }, 1, .1);
        legacy.Refresh(.1, goods);
        check(legacy.Suppliers.Contains(old) && old.VisitWorldId == 1 && old.BuyLimit == limit, "Old visits migrate without resetting identities or limits");
        var oldOutbound = new TradingVoyage { Visitor = new TradingSupplier { Id = 100, Arrival = 0, Departure = 1 },
            Outbound = true, WorldId = 1, FromQ = 0, FromR = 0, ToQ = 99, ToR = 99, Start = 1, Arrival = 5 };
        market.Voyages.Clear(); market.Suppliers.Clear(); market.Voyages.Add(oldOutbound);
        market.NextFleetDispatch = 999;
        market.Refresh(221, goods);
        check(!oldOutbound.Outbound && oldOutbound.WorldId == 2 && oldOutbound.ToQ == 5 && oldOutbound.ToR == 0,
            "Old empty-space exit routes migrate to an available real planet");
        var full = new TradingMarket { FleetEnabled = true, NextFleetDispatch = 999,
            TradePorts = new List<TradingPort> { new TradingPort { WorldId = 1 }, new TradingPort { WorldId = 2, Q = 5 } } };
        for (int i = 0; i < 5; i++)
            full.Suppliers.Add(new TradingSupplier { Id = i + 200, VisitWorldId = 2, Arrival = 0, Departure = 100, Specialty = "Mineral" });
        var departing = new TradingSupplier { Id = 300, VisitWorldId = 1, Arrival = 0, Departure = 1, Specialty = "Mineral" };
        full.Suppliers.Add(departing);
        var held = new TradingVoyage { Visitor = departing, WorldId = 1, Arrival = 0, Start = -1 };
        full.Voyages.Add(held);
        full.Refresh(1, goods);
        check(held.WaitingForDestination && held.WorldId == 1 && held.ToQ == 0 && !full.Suppliers.Contains(departing),
            "A merchant waits in orbit when the only other planet has five docked visitors");
        full.Suppliers[0].Departure = 2;
        full.Refresh(2, goods);
        check(!held.WaitingForDestination && held.WorldId == 2 && held.ToQ == 5,
            "A waiting merchant resumes toward a real planet as soon as a berth is free");
    }
}
