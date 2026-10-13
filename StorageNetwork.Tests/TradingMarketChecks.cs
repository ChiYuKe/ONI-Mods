using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using StorageNetwork.Trading;

internal static class TradingMarketChecks
{
    public static void Verify(Action<bool, string> check)
    {
        // The small economy fixture uses one category; specialization has a separate full catalog below.
        var material = new TradingGood { Id = "Iron", Name = "Iron", BasePrice = 2m, Category = "Metal" };
        var item = new TradingGood { Id = "Suit", Name = "Suit", BasePrice = 100m, ByUnit = true, Category = "Metal" };
        var catalog = new[] { material, item };
        var market = new TradingMarket();
        check(market.Refresh(40, catalog) && market.Suppliers.Count == 5, "market creates exactly five suppliers");
        check(market.Suppliers.All(s => s.Departure - s.Arrival >= 1 && s.Departure - s.Arrival <= 15), "supplier visits last 1 to 15 cycles");
        var ids = market.Suppliers.Select(s => s.Id).ToArray();
        check(!market.Refresh(40.9, catalog) && market.Suppliers.Select(s => s.Id).SequenceEqual(ids), "opening the panel does not reroll suppliers");
        var supplier = market.Suppliers[0];
        var copy = new TradingSupplier { Id = supplier.Id };
        check(TradingMarket.Price(supplier, material, 40, true) == TradingMarket.Price(copy, material, 40.99, true), "quotes are deterministic within a cycle");
        check(Enumerable.Range(41, 20).Any(c => TradingMarket.Price(supplier, material, c, true) != TradingMarket.Price(supplier, material, 40, true)), "quotes change over time");
        check(TradingMarket.Price(supplier, material, 40, true) > TradingMarket.Price(supplier, material, 40, false), "one merchant has a buy sell spread");
        check(Enumerable.Range(40, 40).Max(c => TradingMarket.Price(supplier, material, c, false)) >
            Enumerable.Range(40, 40).Min(c => TradingMarket.Price(supplier, material, c, true)), "market cycles permit buying low and selling high");

        supplier.BuyLimit = 100m;
        supplier.SellLimit = 200m;
        double sell = market.Maximum(supplier, material, 40, false, 1000);
        check(sell > 0 && sell <= 1000, "sale is bounded by supplier buy budget");
        check(market.Settle(supplier, material, 40, false, sell), "sale settles successfully");
        check(market.Balance == supplier.Bought && supplier.Sold == 0 && supplier.Bought <= supplier.BuyLimit, "sale credits wallet and only supplier buy turnover");
        decimal balance = market.Balance;
        check(!market.Settle(supplier, material, 40, false, 1) && market.Balance == balance, "exhausted supplier buy budget cannot be exceeded");
        double buy = market.Maximum(supplier, material, 40, true, 1000);
        check(market.Settle(supplier, material, 40, true, buy), "purchase settles from earned currency");
        check(market.Balance >= 0 && supplier.Sold <= supplier.SellLimit && supplier.Bought <= supplier.BuyLimit, "buying cannot overdraw wallet or either independent budget");
        supplier.Stock[item.Id] = 3;
        market.Balance = 10000m;
        supplier.SellLimit = 10000m;
        check(market.Maximum(supplier, item, 40, true, 100) == 3, "purchases cannot exceed item stock");
        check(!market.Settle(supplier, item, 40, true, 0.5), "fractional equipment is rejected");
        check(market.Settle(supplier, item, 40, true, 3) && supplier.Stock[item.Id] == 0, "whole item purchase depletes stock");
        check(market.Maximum(supplier, item, 40, true, 1) == 0, "depleted goods cannot be bought repeatedly");
        balance = market.Balance;
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, -1d, 0d })
            check(!market.Settle(supplier, material, 40, false, invalid) && market.Balance == balance, "invalid quantities never alter the wallet");
        check(market.Maximum(new TradingSupplier { Departure = 99 }, material, 40, false, 10) == 0, "unregistered supplier cannot trade");
        double expiry = supplier.Departure;
        check(!market.Settle(supplier, material, expiry, false, 1), "expired suppliers reject stale UI actions");

        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
        check(restored.Balance == market.Balance && restored.NextId == market.NextId, "save round trip preserves wallet and supplier sequence");
        check(restored.Suppliers[0].Bought == supplier.Bought && restored.Suppliers[0].Sold == supplier.Sold && restored.Suppliers[0].Stock[item.Id] == 0, "save round trip preserves both turnovers and stock");
        check(restored.Suppliers[0].Specialty == supplier.Specialty, "save round trip preserves merchant specialization");
        check(!restored.Refresh(40, catalog), "reload does not replenish budgets or reroll visitors");
        check(restored.Refresh(1000, catalog) && restored.Suppliers.Count == 5 && restored.Suppliers.All(s => s.Arrival == 1000 && s.Bought == 0 && s.Sold == 0), "expired slots replenish after a long absence");

        var stress = new TradingMarket { Balance = 1000000m };
        stress.Refresh(0, catalog);
        for (int cycle = 0; cycle < 100; cycle++)
        {
            stress.Refresh(cycle, catalog);
            foreach (TradingSupplier merchant in stress.Suppliers)
            {
                double soldAmount = stress.Maximum(merchant, material, cycle, false, 50000);
                if (soldAmount > 0) stress.Settle(merchant, material, cycle, false, soldAmount);
                double boughtAmount = stress.Maximum(merchant, material, cycle, true, 50000);
                if (boughtAmount > 0) stress.Settle(merchant, material, cycle, true, boughtAmount);
                check(stress.Balance >= 0 && merchant.Bought <= merchant.BuyLimit && merchant.Sold <= merchant.SellLimit && merchant.Stock[material.Id] >= 0,
                    "repeated trades preserve monetary and stock invariants");
            }
        }
        VerifySpecialization(check);
        VerifyQuotesAndReceipts(check);
    }

    private static void VerifyQuotesAndReceipts(Action<bool, string> check)
    {
        var good = new TradingGood { Id = "Iron", Name = "Iron", Category = "Metal", BasePrice = 10m };
        var item = new TradingGood { Id = "Suit", Name = "Suit", Category = "Metal", BasePrice = 100m, ByUnit = true };
        var market = new TradingMarket { Balance = 500m };
        market.Refresh(0, new[] { good, item });
        TradingSupplier supplier = market.Suppliers[0];
        supplier.Stock[good.Id] = 100;
        TradingQuote quote = market.Quote(supplier, good, 0, true, 1000, 7.5);
        check(quote.Amount == 7.5 && quote.Total == quote.UnitPrice * 7.5m && quote.BalanceAfter == market.Balance - quote.Total &&
            !string.IsNullOrEmpty(quote.Reason), "quote exposes capacity-limited quantity, total, projected balance and warning");
        check(market.Quote(supplier, item, 0, true, 0.5, 10).CanTrade == false, "quote rejects fractional equipment");
        market.Balance = 0;
        check(!market.Quote(supplier, good, 0, true, 1, 100).CanTrade && market.Quote(supplier, good, 0, true, 1, 100).Reason.Contains("余额"),
            "quote explains an empty wallet before physical transfer");
        var sell = market.Quote(supplier, good, 0, false, 2, 2);
        check(sell.CanTrade && sell.BalanceAfter == sell.Total && market.Settle(supplier, good, 0, false, sell.Amount, 3),
            "sale settlement matches its preview");
        TradingReceipt receipt = market.Receipts.Single();
        check(receipt.SupplierId == supplier.Id && receipt.GoodId == good.Id && receipt.Amount == 2 && receipt.UnitPrice == sell.UnitPrice &&
            receipt.Total == sell.Total && receipt.BalanceAfter == market.Balance && receipt.WorldId == 3 && !receipt.Buy,
            "receipt records exact supplier, material, quantity, price, total, balance and world");
        int previousCount = market.Receipts.Count;
        check(!market.Settle(supplier, good, 0, false, -1) && market.Receipts.Count == previousCount,
            "rejected trades never create receipts");
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, new JsonSerializerOptions { IncludeFields = true }),
            new JsonSerializerOptions { IncludeFields = true });
        check(restored.Receipts.Count == 1 && restored.Receipts[0].Total == receipt.Total && restored.NextTradeId == market.NextTradeId,
            "save round trip preserves receipt history and sequence");
        for (int i = 0; i < 100; i++) market.Settle(supplier, good, 0, false, 0.1);
        check(market.Receipts.Count == 64 && market.Receipts[0].Id == market.NextTradeId - 64,
            "history retains only the newest 64 trades with stable chronological IDs");
        foreach (int id in new[] { 1, 2, 3 })
        {
            var visitor = new TradingSupplier { Id = id };
            for (int cycle = 1; cycle < 100; cycle++)
            {
                decimal before = TradingMarket.Price(visitor, good, cycle - 1, true);
                decimal after = TradingMarket.Price(visitor, good, cycle, true);
                check(Math.Abs(after / before - 1m) < 0.07m, "daily quotes move gradually for each supplier profile");
            }
        }
        check(TradingMarket.Profile(new TradingSupplier { Id = 3 }) == "Bulk" &&
            TradingMarket.Profile(new TradingSupplier { Id = 1 }) == "Clearance" &&
            TradingMarket.Profile(new TradingSupplier { Id = 2 }) == "Premium", "merchant profiles remain stable across reloads");
    }

    private static void VerifySpecialization(Action<bool, string> check)
    {
        var catalog = new List<TradingGood>();
        foreach (string category in new[] { "Food", "Metal", "Mineral", "Seed", "Equipment", "Liquid", "Gas", "Industrial" })
            for (int i = 0; i < 16; i++)
                catalog.Add(new TradingGood { Id = category + i, Name = category + i, Category = category, BasePrice = 10m,
                    ByUnit = category == "Seed" || category == "Equipment" || category == "Food" });
        var market = new TradingMarket { Balance = 10000m };
        market.Refresh(0, catalog);
        check(market.Suppliers.Select(s => s.Specialty).SequenceEqual(new[] { "Food", "Metal", "Mineral", "Seed", "Equipment" }),
            "initial merchants cover food, metals, minerals, seeds and equipment");
        check(market.Suppliers.SelectMany(s => s.Stock.Keys).Distinct().Count() == market.Suppliers.Sum(s => s.Stock.Count),
            "concurrent supplier inventories have no overlapping goods");
        TradingSupplier supplier = market.Suppliers[0];
        TradingGood wrong = catalog.First(g => g.Category != supplier.Specialty);
        supplier.Stock[wrong.Id] = 100;
        check(market.Maximum(supplier, wrong, 0, true, 10) == 0 && market.Maximum(supplier, wrong, 0, false, 10) == 0 &&
            !market.Settle(supplier, wrong, 0, false, 1), "merchant specialization is enforced in trading, including forged stock");
        supplier.Stock.Remove(wrong.Id);
        for (int cycle = 0; cycle < 60; cycle++)
        {
            market.Refresh(cycle, catalog);
            check(market.Suppliers.Select(s => s.Specialty).Distinct().Count() == 5, "supplier replacement keeps five different specialties");
            foreach (TradingSupplier merchant in market.Suppliers)
                check(merchant.Stock.Count == 12 && merchant.Stock.Keys.All(id => catalog.Single(g => g.Id == id).Category == merchant.Specialty),
                    "every offered good belongs to its supplier's exclusive specialty");
        }
        var old = new TradingMarket { Balance = 321m, NextId = 6 };
        for (int i = 0; i < 5; i++)
            old.Suppliers.Add(new TradingSupplier { Id = i + 1, Departure = 100, BuyLimit = 1000m, SellLimit = 2000m,
                Bought = 123m, Sold = 456m, Stock = new Dictionary<string, double> { { "oldMixedGood", 99 } } });
        check(old.Refresh(60, catalog), "old mixed merchants migrate to specialties");
        check(old.Balance == 321m && old.NextId == 6 && old.Suppliers.All(s => s.Departure == 100 && s.Bought == 123m && s.Sold == 456m &&
            s.BuyLimit == 1000m && s.SellLimit == 2000m), "specialty migration preserves wallet, turnover, limits and departures");
        check(old.Suppliers.Select(s => s.Specialty).Distinct().Count() == 5 && old.Suppliers.All(s => !s.Stock.ContainsKey("oldMixedGood")),
            "old mixed catalogs become five disjoint specialist catalogs");
        check(!old.Refresh(60.1, catalog), "specialty migration runs once and never refills stock on reopening");
    }
}
