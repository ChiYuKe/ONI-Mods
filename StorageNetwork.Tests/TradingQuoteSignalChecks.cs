using System;
using System.Linq;
using StorageNetwork.Trading;

internal static class TradingQuoteSignalChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var good = new TradingGood { Id = "ChlorineGas", Category = "Gas", BasePrice = 100 };
        var supplier = new TradingSupplier { Id = 13, Specialty = "Gas", Arrival = 0, Departure = 150, BuyLimit = 100000, SellLimit = 100000 };
        supplier.Stock[good.Id] = supplier.InitialStock[good.Id] = 1000;
        var ask = Enumerable.Range(0, 120).Select(c => TradingMarket.Price(supplier, good, c, true)).ToArray();
        var bid = Enumerable.Range(0, 120).Select(c => TradingMarket.Price(supplier, good, c, false)).ToArray();
        check(ask.Zip(bid, (a, b) => Math.Round(a / b, 4)).Distinct().Count() > 20,
            "buy and sell histories do not remain scaled copies of one market wave");
        check(Enumerable.Range(1, 119).Any(i => (ask[i] - ask[i - 1]) * (bid[i] - bid[i - 1]) < 0),
            "independent supply and procurement cycles can move quotes in opposite directions");
        check(TradingMarket.Price(supplier, good, 5, true) == TradingMarket.Price(supplier, good, 5.99, true) &&
            TradingMarket.Price(supplier, good, 5, false) == TradingMarket.Price(supplier, good, 5.99, false),
            "separate quote signals remain stable within a cycle without rerolling on refresh");
        decimal supplyFull = TradingQuoteSignals.Factor(supplier, good, 5, true);
        decimal procurementFull = TradingQuoteSignals.Factor(supplier, good, 5, false);
        supplier.Stock[good.Id] = 100;
        check(TradingQuoteSignals.Factor(supplier, good, 5, true) > supplyFull &&
            TradingQuoteSignals.Factor(supplier, good, 5, false) == procurementFull,
            "stock depletion affects the supply signal independently from procurement");
        supplier.Bought = 90000;
        check(TradingQuoteSignals.Factor(supplier, good, 5, false) < procurementFull,
            "consumed procurement budgets damp the supplier's purchase demand");
        supplier.Stock[good.Id] = 2000;
        check(TradingQuoteSignals.Factor(supplier, good, 5, true) < supplyFull,
            "surplus stock lowers supply quotes relative to an otherwise identical batch");
        for (int cycle = 0; cycle < 120; cycle++)
        {
            supplier.Relationship = new TradingDialogueMemory { Affection = cycle % 101 };
            decimal buy = TradingMarket.Price(supplier, good, cycle, true);
            decimal sell = TradingMarket.Price(supplier, good, cycle, false);
            check(buy > sell && sell > 0, "independent signals preserve positive quotes and a safe spread");
        }
        check(TradingQuoteSignals.Factor(new TradingSupplier { Id = 0 }, good, 5, true) == 1,
            "market reference prices exclude visitor-specific supply signals");
        var market = new TradingMarket();
        market.Refresh(0, new[] { good });
        var recordedSupplier = market.Suppliers[0];
        decimal oldBuy = recordedSupplier.History[good.Id][0].Buy;
        market.Refresh(1, new[] { good });
        check(recordedSupplier.History[good.Id][0].Buy == oldBuy,
            "separate signal updates do not recompute earlier recorded quotes");
        check(recordedSupplier.History[good.Id].Last().Buy == TradingMarket.Price(recordedSupplier, good, 1, true) &&
            recordedSupplier.History[good.Id].Last().Sell == TradingMarket.Price(recordedSupplier, good, 1, false),
            "chart history records both actual executable quotes");
    }
}
