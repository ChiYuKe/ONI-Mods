using System;
using System.Collections.Generic;
using System.Text.Json;
using StorageNetwork.Trading;

internal static class TradingInventorySignalChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var good = new TradingGood { Id = "Iron", Category = "Metal", BasePrice = 100m };
        var catalog = new[] { good };
        var market = new TradingMarket();
        var snapshot = new Dictionary<string, double> { [good.Id] = 100000 };
        market.SampleInventory(0, 0, catalog, snapshot);
        market.SelectInventoryWorld(0);
        market.Refresh(0, catalog);
        var signal = market.WorldInventorySignals[0];
        check(signal.Factors[good.Id] == 1m, "initial resource snapshot affects only the next cycle");
        market.RecordInventoryTrade(0, good.Id, 50000);
        snapshot[good.Id] = 150000;
        market.SampleInventory(0, 1, catalog, snapshot);
        check(signal.NeutralStock[good.Id] == 100000 && signal.Factors[good.Id] == 0.995m, "imported goods do not add broad market supply and cycle movement is bounded");
        market.RecordInventoryTrade(0, good.Id, -30000);
        snapshot[good.Id] = 120000;
        market.SampleInventory(0, 2, catalog, snapshot);
        check(signal.NeutralStock[good.Id] == 100000, "reselling goods does not raise the broad market price");
        snapshot[good.Id] += 20000;
        market.SampleInventory(0, 3, catalog, snapshot);
        check(signal.NeutralStock[good.Id] == 120000, "natural production contributes to the next-cycle supply signal");
        decimal locked = signal.Factors[good.Id];
        snapshot[good.Id] = 900000;
        market.SampleInventory(0, 3.9, catalog, snapshot);
        check(signal.Factors[good.Id] == locked && signal.Snapshot[good.Id] == 140000, "opening panels cannot resample inventory or change prices within the cycle");
        market.SampleInventory(1, 3, catalog, new Dictionary<string, double>());
        market.SelectInventoryWorld(1);
        market.Refresh(3, catalog);
        decimal neutralReference = market.ReferencePrice(good, 3);
        market.SelectInventoryWorld(0);
        market.Refresh(3, catalog);
        check(market.ReferencePrice(good, 3) < neutralReference, "world inventory signals remain isolated");
        check(signal.History[good.Id].Count == 2 && market.WorldInventorySignals[1].History[good.Id].Count == 1, "each world keeps its own price history");
        for (int day = 4; day < 100; day++) market.SampleInventory(0, day, catalog, snapshot);
        check(signal.Factors[good.Id] >= 0.94m && signal.Factors[good.Id] <= 1m, "large stock cannot depress prices beyond six percent");
        var a = new TradingSupplier { Id = 1, InventoryFactors = signal.Factors };
        var b = new TradingSupplier { Id = 2, InventoryFactors = signal.Factors };
        var plainA = new TradingSupplier { Id = 1 };
        var plainB = new TradingSupplier { Id = 2 };
        decimal beforeRatio = TradingMarket.Price(plainA, good, 6, false) / TradingMarket.Price(plainB, good, 6, true);
        decimal afterRatio = TradingMarket.Price(a, good, 6, false) / TradingMarket.Price(b, good, 6, true);
        check(Math.Abs(beforeRatio - afterRatio) < 0.001m, "the common inventory factor preserves inter-merchant arbitrage ratios apart from rounding");
        market.RecordInventoryTrade(1, good.Id, 12);
        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
        check(restored.WorldInventorySignals[1].TradeDelta[good.Id] == 12 && restored.WorldInventorySignals[0].Factors[good.Id] == signal.Factors[good.Id], "inventory baselines and pending trade corrections survive saves");
        check(TradingMarket.EventFactor("Food", 3) < 1m && TradingMarket.EventFactor("Industrial", 19) < 1m && TradingMarket.EventFactor("Metal", 11) > 1m,
            "event direction uses economic categories rather than localized text");
    }
}
