using System;
using System.Linq;
using System.Text.Json;
using StorageNetwork.Trading;

internal static class TradingRelationshipChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var good = new TradingGood { Id = "Iron", Name = "Iron", Category = "Metal", BasePrice = 100, PreferenceTag = "RawMaterial" };
        var market = new TradingMarket { Balance = 1000000 };
        var supplier = New(1);
        market.Suppliers.Add(supplier);
        supplier.Stock[good.Id] = 1000;
        TradingRelationships.Initialize(market, supplier);
        check(market.Settle(supplier, good, 1, true, 1) && supplier.Relationship.Affection > 0 && supplier.Relationship.SuccessfulTrades == 1,
            "successful purchases increase affection only after settlement");
        decimal favor = supplier.Relationship.Affection;
        check(!market.Settle(supplier, good, 1, true, 0) && supplier.Relationship.Affection == favor,
            "failed and zero-quantity trades do not grant affection");
        check(market.Settle(supplier, good, 1, false, 1) && supplier.Relationship.Affection > favor && supplier.Relationship.SuccessfulTrades == 2,
            "successful sales also grant affection");
        check(Enumerable.Range(1, 64).Select(i => TradingRelationships.GrowthRate(New(i))).Distinct().Count() == 8,
            "ordinary trader personalities have different affection growth rates");
        check(TradingRelationships.GrowthRate(new TradingSupplier { CharacterId = "greyfinch" }) <
            TradingRelationships.GrowthRate(new TradingSupplier { CharacterId = "oldmi" }), "fixed characters have individual growth rates");
        var whole = New(10);
        var split = New(10);
        TradingRelationships.OnSettled(new TradingMarket(), whole, 10000m);
        var splitMarket = new TradingMarket();
        for (int i = 0; i < 10; i++) TradingRelationships.OnSettled(splitMarket, split, 1000m);
        check(whole.Relationship.Affection == split.Relationship.Affection && split.Relationship.SuccessfulTrades == 10,
            "splitting equal turnover does not accelerate affection");
        var limits = New(2);
        limits.Arrival = 0; limits.Departure = 13; limits.BuyLimit = 1000; limits.SellLimit = 2000;
        limits.Bought = 300; limits.Sold = 700;
        var limitsMarket = new TradingMarket();
        limitsMarket.Suppliers.Add(limits);
        TradingRelationships.Initialize(limitsMarket, limits);
        limits.Relationship.Affection = 100;
        TradingRelationships.Initialize(limitsMarket, limits);
        check(limits.Departure == 13 && limits.BuyLimit == 1500 && limits.SellLimit == 3000 && limits.Bought == 300 && limits.Sold == 700,
            "affection preserves the current departure while increasing independent budgets without resetting turnover");
        for (int i = 0; i < 20; i++) TradingRelationships.Initialize(limitsMarket, limits);
        check(limits.Departure == 13 && limits.BuyLimit == 1500 && limits.SellLimit == 3000,
            "refreshing cannot repeatedly extend visits or multiply unlocked budgets");
        decimal cap = limits.Relationship.Affection;
        TradingRelationships.OnSettled(limitsMarket, limits, 100000000);
        check(limits.Relationship.Affection == 100 && limits.Departure == 13, "affection gains never extend the current visit");
        check(TradingText.Source(TradingRelationships.Card(limits)).Contains("5～30 周期"),
            "identity card renders the actual next-visit range at maximum affection");
        var visitContext = TradingDialogueContexts.Build(limitsMarket, limits, new[] { good }, 1);
        check(visitContext.MaximumVisitCycles == 30 && visitContext.NextVisitMinimumCycles == 5,
            "AI dialogue facts agree with the actual next-visit range");
        for (int id = 1; id <= 3; id++)
        {
            var trader = New(id);
            trader.Relationship = new TradingDialogueMemory();
            decimal ask = TradingMarket.Price(trader, good, 1, true), bid = TradingMarket.Price(trader, good, 1, false);
            trader.Relationship.Affection = 100;
            check(TradingMarket.Price(trader, good, 1, true) < ask && TradingMarket.Price(trader, good, 1, false) > bid,
                "higher affection improves both purchase and sale quotes");
            for (int score = 0; score <= 100; score += 5)
            {
                trader.Relationship.Affection = score;
                trader.Pressure.Clear();
                decimal purchase = TradingMarket.Price(trader, good, 1, true);
                trader.Pressure[good.Id] = 0.04m;
                check(TradingMarket.Price(trader, good, 1, false) < purchase,
                    "affection preserves a spread even after maximum immediate purchase pressure");
            }
        }
        var returningMarket = new TradingMarket();
        var first = New(3); first.CharacterId = "greyfinch";
        TradingRelationships.Initialize(returningMarket, first);
        first.Relationship.Affection = 70;
        first.Relationship.SuccessfulTrades = 15;
        first.Relationship.Trust = 1;
        var returning = New(40); returning.CharacterId = "greyfinch";
        TradingRelationships.Initialize(returningMarket, returning);
        check(returning.Relationship.Affection == 70 && returning.Relationship.SuccessfulTrades == 15 &&
            returning.Departure == returning.BaseDeparture && returning.BuyLimit > returning.BaseBuyLimit,
            "fixed characters remember affection and start later visits with earned benefits");
        for (int score = 0; score <= 100; score += 20)
        {
            returning.Relationship.Affection = score;
            int minimum = 1 + Math.Min(4, score / 20);
            check(TradingRelationships.MinimumVisitCycles(returning) == minimum,
                "relationship stages increase the visit's lower bound from one through five cycles");
            for (int visit = 1; visit <= 40; visit++)
            {
                var visitor = New(visit + 100); visitor.CharacterId = "greyfinch";
                TradingRelationships.RollVisit(returningMarket, visitor);
                check(visitor.Departure - visitor.Arrival >= minimum && visitor.Departure - visitor.Arrival <= 30,
                    "arrival draws duration within the relationship-adjusted range and a fixed 30-cycle maximum");
            }
        }
        returning.Relationship.Affection = 70;
        check(TradingRelationships.CooperationFactor(returning, returning.Relationship) < 0.95m,
            "affection improves the negotiable discount alongside conversational trust");
        returningMarket.Suppliers.Add(returning);
        TradingDialogue.Assign(returning);
        TradingConversations.Upgrade(returning);
        string friendly = TradingText.Source(TradingConversations.Text(returningMarket, returning, new[] { good }, 1));
        returning.Relationship.Affection = 0;
        string neutral = TradingText.Source(TradingConversations.Text(returningMarket, returning, new[] { good }, 1));
        check(friendly != neutral, "relationship stages alter the character's conversational tone");
        returning.Relationship.Affection = 70;
        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(returningMarket, options), options);
        var saved = restored.Suppliers[0];
        restored.Memory(saved);
        check(saved.Relationship.Affection == 70 && saved.RelationshipQuotaTier == returning.RelationshipQuotaTier &&
            saved.BaseDeparture == returning.BaseDeparture && saved.BaseBuyLimit == returning.BaseBuyLimit,
            "save reload preserves affection and visit benefit baselines");
        var context = TradingDialogueContexts.Build(restored, saved, new[] { good }, 1);
        context.Relationship.Affection = 0;
        check(saved.Relationship.Affection == 70, "AI relationship snapshots cannot modify affection");
        var charts = new TradingMarket();
        var second = new TradingGood { Id = "Copper", Category = "Metal", BasePrice = 200 };
        charts.Refresh(1, new[] { good, second });
        var chartTrader = charts.Suppliers[0];
        chartTrader.Relationship.Affection = 100;
        charts.RefreshRelationshipPrices(1, new[] { good, second });
        check(chartTrader.History[second.Id].Last().Buy == TradingMarket.Price(chartTrader, second, 1, true),
            "relationship repricing refreshes histories for all affected goods, not only the traded item");
    }
    private static TradingSupplier New(int id) => new TradingSupplier { Id = id, Specialty = "Metal", Arrival = 0,
        Departure = 5, BuyLimit = 100000, SellLimit = 100000 };
}
