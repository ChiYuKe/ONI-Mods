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

        var chartSupplier = new TradingSupplier { Id = 7, Arrival = 10.8, Departure = 13 };
        check(TradingMarket.PriceHistory(chartSupplier, material, 10.7).Count == 0, "chart does not invent quotes before a supplier arrives");
        var arrivalHistory = TradingMarket.PriceHistory(chartSupplier, material, 10.8);
        check(arrivalHistory.Count == 1 && arrivalHistory[0].Cycle == 10, "fractional-cycle arrival starts with exactly one quote point");
        var history = TradingMarket.PriceHistory(chartSupplier, material, 12.9);
        check(history.Select(p => p.Cycle).SequenceEqual(new[] { 10, 11, 12 }), "chart contains one ordered quote per elapsed cycle");
        check(history.All(p => p.Buy == TradingMarket.Price(chartSupplier, material, p.Cycle, true) &&
            p.Sell == TradingMarket.Price(chartSupplier, material, p.Cycle, false)), "chart quotes match both executable trade prices");
        check(TradingMarket.PriceHistory(chartSupplier, material, 100).Last().Cycle == 12, "departed suppliers have no invented future quote points");
        chartSupplier.Departure = 13.5;
        check(TradingMarket.PriceHistory(chartSupplier, material, 100).Last().Cycle == 13, "fractional departure retains the last cycle in which a supplier traded");
        chartSupplier.Arrival = 0;
        chartSupplier.Departure = 100;
        check(TradingMarket.PriceHistory(chartSupplier, material, 80).Count == 30, "price chart sampling is bounded for legacy long visits");
        check(TradingMarket.PriceHistory(chartSupplier, material, double.NaN).Count == 0, "invalid chart time produces no points");

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
        VerifyMarketDepth(check);
        VerifyTransferPrecision(check);
        VerifySupplierIdentity(check);
    }

    private static void VerifySupplierIdentity(Action<bool, string> check)
    {
        var templates = Enumerable.Range(1, 12).Select(i => new TradingIdentityTemplate
        {
            PersonalityId = "Person" + i, Name = "Merchant" + i,
            HeadShape = "headshape_" + i.ToString("000"), Hair = "hair_" + i.ToString("000"),
            Eyes = "eyes_" + i.ToString("000"), Mouth = "mouth_" + i.ToString("000")
        }).ToArray();
        var legacy = new TradingSupplier { Id = 42, Arrival = 12.5, Departure = 20.5, Bought = 123.45m, Sold = 67.89m,
            BuyLimit = 1000, SellLimit = 2000, Stock = new Dictionary<string, double> { { "Iron", 125 } } };
        check(TradingIdentity.Ensure(legacy, templates), "old suppliers acquire a stable name and modular appearance");
        check(legacy.Bought == 123.45m && legacy.Sold == 67.89m && legacy.Stock["Iron"] == 125 && legacy.Departure == 20.5,
            "identity migration never changes turnover, inventory or departure");
        var appearance = legacy.Appearance;
        check(!TradingIdentity.Ensure(legacy, templates) && ReferenceEquals(appearance, legacy.Appearance), "reopening does not reroll a supplier face");
        var copy = new TradingSupplier { Id = 42 };
        TradingIdentity.Ensure(copy, templates);
        check(copy.Name == legacy.Name && copy.Appearance.HeadShape == appearance.HeadShape && copy.Appearance.Hair == appearance.Hair &&
            copy.Appearance.Eyes == appearance.Eyes && copy.Appearance.Mouth == appearance.Mouth, "supplier identity generation is deterministic for a fixed ID and resource pool");
        bool mixed = false;
        var combinations = new HashSet<string>();
        for (int id = 1; id <= 64; id++)
        {
            var supplier = new TradingSupplier { Id = id };
            TradingIdentity.Ensure(supplier, templates);
            var face = supplier.Appearance;
            check(templates.Any(t => t.PersonalityId == face.BasePersonalityId && t.HeadShape == face.HeadShape) &&
                templates.Any(t => t.Hair == face.Hair) && templates.Any(t => t.Eyes == face.Eyes) && templates.Any(t => t.Mouth == face.Mouth),
                "composed heads use only available game accessory IDs");
            mixed |= !templates.Any(t => t.HeadShape == face.HeadShape && t.Hair == face.Hair && t.Eyes == face.Eyes && t.Mouth == face.Mouth);
            combinations.Add(face.HeadShape + "|" + face.Hair + "|" + face.Eyes + "|" + face.Mouth);
        }
        check(mixed && combinations.Count > templates.Length, "portrait variation comes from mixing parts rather than selecting fixed mini-icons");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingSupplier>(JsonSerializer.Serialize(legacy, options), options);
        check(restored.Name == legacy.Name && restored.Appearance.BasePersonalityId == appearance.BasePersonalityId &&
            restored.Appearance.HeadShape == appearance.HeadShape && restored.Appearance.Hair == appearance.Hair &&
            restored.Appearance.Eyes == appearance.Eyes && restored.Appearance.Mouth == appearance.Mouth,
            "supplier names and each individual appearance part survive saving");
        check(!TradingIdentity.Ensure(restored, templates.Reverse().ToArray()), "saved identities are preserved if the available resource list changes order");
        var renamed = new TradingSupplier { Id = 43, Name = "Custom merchant name" };
        TradingIdentity.Ensure(renamed, templates);
        check(renamed.Name == "Custom merchant name" && renamed.Appearance != null, "missing portrait parts do not overwrite existing character names");
        var waiting = new TradingSupplier { Id = 44 };
        check(!TradingIdentity.Ensure(waiting, Array.Empty<TradingIdentityTemplate>()) && waiting.Appearance == null,
            "an unavailable game database does not persist invalid appearance IDs");
    }

    private static void VerifyTransferPrecision(Action<bool, string> check)
    {
        float oil = 299124.9f;
        check(oil - 0.002f == oil, "two grams cannot change the native float mass of a 299 tonne oil stack");
        double minimum = TradingPrecision.MinimumTransfer(oil, 1, false, 0.001f);
        check(minimum > 0.003 && oil - (float)minimum < oil, "minimum sale quantity must actually change a large native stack");
        check(TradingPrecision.MinimumTransfer(1, 1, false, 0.001f) == 0.002, "small stacks still support gram-scale transfers above the native minimum");
        check(TradingPrecision.MinimumTransfer(10000000, 1, false, 0.001f) >= 1, "very large stacks use their larger native mass quantum");
        check(TradingPrecision.MinimumTransfer(10000000, 0.5f, true, 0.001f) >= 2, "whole-item quantities account for both float precision and mass per unit");
        var good = new TradingGood { Id = "CrudeOil", Name = "Oil", Category = "Liquid", BasePrice = 3m };
        var market = new TradingMarket();
        market.Refresh(0, new[] { good });
        var supplier = market.Suppliers[0];
        supplier.BuyLimit = 68000m;
        supplier.Bought = 67999.988m;
        var quote = market.Quote(supplier, good, 0, false, 0.002, oil);
        check(quote.MaximumAmount < minimum && TradingText.Source(quote.LimitReason).Contains("收购") && TradingText.Source(quote.LimitReason).Contains("0.012"),
            "almost exhausted buy budgets explicitly explain why abundant oil has a tiny sale cap");
        TradingPrecision.ApplyMinimum(quote, minimum, market.Balance);
        check(!quote.CanTrade && quote.MaximumAmount == 0 && quote.Total == 0 && quote.BalanceAfter == market.Balance && TradingText.Source(quote.Reason).Contains("最小"),
            "the actual quote validation disables unrepresentable residual-budget trades without promising a payout");
        supplier.Bought = 0;
        quote = market.Quote(supplier, good, 0, false, 1, oil);
        check(quote.MaximumAmount > minimum, "a fresh supplier budget permits representable sales from a large oil stack");
        TradingPrecision.ApplyMinimum(quote, minimum, market.Balance);
        check(quote.CanTrade && quote.Amount == 1, "representable oil trades remain enabled");
        quote = market.Quote(supplier, good, 0, false, 0.002, oil);
        double maximum = quote.MaximumAmount;
        TradingPrecision.ApplyMinimum(quote, minimum, market.Balance);
        check(!quote.CanTrade && quote.MaximumAmount == maximum && TradingText.Source(quote.Reason).Contains("增加"), "too-small typed quantities do not erase an otherwise valid maximum");
    }

    private static void VerifyMarketDepth(Action<bool, string> check)
    {
        var good = new TradingGood { Id = "Iron", Name = "Iron", Category = "Metal", BasePrice = 10m, PreferenceTag = "RawMaterial" };
        var refined = new TradingGood { Id = "Gold", Name = "Gold", Category = "Metal", BasePrice = 20m, PreferenceTag = "RefinedMetal" };
        var catalog = new[] { good, refined };
        var market = new TradingMarket { Balance = 10000000m };
        market.Refresh(0, catalog);
        TradingSupplier supplier = market.Suppliers[0];
        supplier.Departure = 100;
        supplier.Stock[good.Id] = 100000;
        supplier.InitialStock[good.Id] = 100000;
        supplier.BuyLimit = supplier.SellLimit = 10000000m;
        var quote = market.Quote(supplier, good, 0, true, 4000, 100000);
        check(TradingMarket.ConfirmationProblem(quote, quote.UnitPrice, quote.Cycle, quote.Amount, true) == null, "unchanged confirmation remains valid");
        decimal originalPrice = quote.UnitPrice;
        decimal referenceBeforeTrade = market.ReferencePrice(good, 0);
        check(market.Settle(supplier, good, 0, true, 4000), "large purchase settles at the displayed quote");
        check(market.ReferencePrice(good, 0) == referenceBeforeTrade && market.Pressure.Count == 0, "purchases do not move the broad market signal");
        check(market.Receipts.Last().UnitPrice == originalPrice && market.Receipts.Last().Total == originalPrice * 4000m,
            "demand changes never reprice the transaction already delivered");
        var updated = market.Quote(supplier, good, 0, true, 4000, 100000);
        updated.UnitPrice = quote.UnitPrice + 0.01m;
        check(TradingText.Source(TradingMarket.ConfirmationProblem(updated, quote.UnitPrice, quote.Cycle, quote.Amount, true))?.Contains("报价") == true,
            "demand changes invalidate stale displayed prices before delivery");
        updated.Cycle = 1;
        updated.UnitPrice = quote.UnitPrice;
        check(TradingText.Source(TradingMarket.ConfirmationProblem(updated, quote.UnitPrice, quote.Cycle, quote.Amount, true))?.Contains("新周期") == true,
            "a new cycle invalidates confirmation even if rounded prices are equal");
        updated.Cycle = quote.Cycle;
        updated.Amount = 3;
        check(TradingText.Source(TradingMarket.ConfirmationProblem(updated, quote.UnitPrice, quote.Cycle, quote.Amount, true))?.Contains("数量") == true,
            "reduced capacity or quota requires a new full-quantity confirmation");
        updated.Amount = 0;
        updated.Reason = "供货商已离开";
        check(TradingMarket.ConfirmationProblem(updated, quote.UnitPrice, quote.Cycle, quote.Amount, true) == updated.Reason,
            "supplier departure and offline warehouses reject confirmation");

        decimal recordedDayZero = market.History[good.Id].Single().Buy;
        market.Pressure[good.Id] = 0.04m;
        decimal pressureBefore = market.Pressure[good.Id];
        supplier.Stock[good.Id] = 50000; // Leave enough headroom to observe several refill steps.
        market.Refresh(1, catalog);
        check(Math.Abs(market.Pressure[good.Id]) < Math.Abs(pressureBefore), "price pressure relaxes each cycle");
        check(market.History[good.Id].First().Buy == recordedDayZero, "past reference prices remain immutable after pressure relaxes");
        decimal bought = supplier.Bought, sold = supplier.Sold;
        double stock = supplier.Stock[good.Id];
        market.Refresh(2, catalog);
        check(supplier.Stock[good.Id] > stock && supplier.Stock[good.Id] <= supplier.InitialStock[good.Id], "stock replenishes gradually up to its visit's initial stock");
        check(supplier.Bought == bought && supplier.Sold == sold, "replenishment never resets either monetary budget");
        stock = supplier.Stock[good.Id];
        check(!market.Refresh(2.9, catalog) && supplier.Stock[good.Id] == stock, "reopening a panel within a cycle does not replenish stock");

        // The strongest preferred-material spread still exceeds the full allowed pressure swing.
        foreach (int id in new[] { 1, 2, 3 })
        {
            supplier.Id = id;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                decimal balance = market.Balance;
                check(market.Settle(supplier, good, 2, true, 2000) && market.Settle(supplier, good, 2, false, 2000), "wash-loop fixture executes both physical directions");
                check(market.Balance <= balance, "immediate buy and resale cannot create currency despite demand and preference bonuses");
            }
        }
        check(Math.Abs(market.Pressure[good.Id]) <= 0.04m, "repeated transactions cannot exceed the pressure bound");
        check(market.LastAmounts[good.Id + ":buy"] == 2000 && market.LastAmounts[good.Id + ":sell"] == 2000, "last successful quantities are remembered separately for buying and selling");
        market.Receipts.Last().Delivery = "Solid server · 2000 kg";
        market.Receipts.Last().StorageIds.Add(42);
        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
        check(restored.LastAmounts[good.Id + ":buy"] == 2000 &&
            restored.Pressure[good.Id] == market.Pressure[good.Id] && restored.History[good.Id].Count == market.History[good.Id].Count,
            "remembered quantities, pressure and recorded market history survive saves");
        check(restored.Receipts.Last().Delivery == market.Receipts.Last().Delivery && restored.Receipts.Last().StorageIds.Single() == 42,
            "actual receipt delivery and persistent building IDs survive saves");
        decimal wallet = restored.Balance;
        check(!restored.Refresh(2.9, catalog) && restored.Balance == wallet && restored.Suppliers[0].Bought == supplier.Bought,
            "loading saves does not reset wallet, visitor limits or refill the current cycle");
        for (int day = 3; day <= 80; day++) restored.Refresh(day, catalog);
        check(restored.History[good.Id].Count == 60 && restored.ReferenceHistory(good.Id, 30, 80).Count == 30 &&
            restored.History[good.Id].First().Cycle == 21, "market history spans suppliers and is capped at sixty cycles");
        restored.Refresh(1000, catalog);
        check(restored.ReferenceHistory(good.Id, 60, 1000).Count == 1, "long absences do not invent missed daily snapshots");

        var legacy = JsonSerializer.Deserialize<TradingMarket>("{\"Balance\":987.65,\"Pressure\":null,\"History\":null,\"Favorites\":[\"Iron\"],\"LastAmounts\":null}", options);
        legacy.Refresh(50, catalog);
        check(legacy.Balance == 987.65m && legacy.History[good.Id].Single().Cycle == 50 && legacy.LastAmounts.Count == 0,
            "legacy saves ignore removed favorites and initialize new features without resetting money or fabricating old history");
        var preferential = new TradingSupplier { Id = 2, Specialty = "Metal" };
        check(TradingMarket.Prefers(preferential, refined) && !TradingMarket.Prefers(preferential, good), "premium metal merchants distinguish refined-metal tags from raw ore");
        check(TradingText.Source(TradingMarket.EventName("Food", 3)).Contains("丰收") && TradingText.Source(TradingMarket.EventName("Metal", 11)).Contains("建设潮") &&
            TradingText.Source(            TradingMarket.EventName("Seed", 35)).Contains("播种季"), "market events have category-specific causes");
        var clear = new TradingMarket { Balance = 5000 };
        clear.Refresh(0, catalog);
        supplier = clear.Suppliers[0];
        var partial = clear.Quote(supplier, good, 0, true, 5, 5);
        check(clear.Settle(supplier, good, 0, true, 2.5) && clear.Receipts.Last().Amount == 2.5 &&
            clear.Receipts.Last().Total == partial.UnitPrice * 2.5m, "partial delivery settles only the physical amount at the validated price");
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
        check(!market.Quote(supplier, good, 0, true, 1, 100).CanTrade && TradingText.Source(market.Quote(supplier, good, 0, true, 1, 100).Reason).Contains("余额"),
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
        check(market.Suppliers.Count == 5 && market.Suppliers.All(s => catalog.Any(g => g.Category == s.Specialty)),
            "initial visitors independently choose from available game categories");
        var sameSeed = new TradingMarket();
        sameSeed.Refresh(0, catalog);
        check(sameSeed.Suppliers.Select(s => s.Specialty).SequenceEqual(market.Suppliers.Select(s => s.Specialty)),
            "independent category draws are stable for saved visitor IDs");
        bool sawThreeMetal = false;
        bool sawDifferentCatalogs = false;
        bool sawDifferentPrices = false;
        for (int batch = 0; batch < 512; batch++)
        {
            var visitors = new TradingMarket { NextId = batch * 5 + 1 };
            visitors.Refresh(0, catalog);
            var metals = visitors.Suppliers.Where(s => s.Specialty == "Metal").ToList();
            if (metals.Count < 3) continue;
            sawThreeMetal = true;
            sawDifferentCatalogs |= metals.Skip(1).Any(s => !s.Stock.Keys.OrderBy(k => k).SequenceEqual(metals[0].Stock.Keys.OrderBy(k => k)));
            var metalGood = catalog.First(g => g.Category == "Metal");
            sawDifferentPrices |= metals.Select(s => TradingMarket.Price(s, metalGood, 0, true)).Distinct().Count() > 1;
        }
        check(sawThreeMetal, "a five-visitor lineup can contain at least three metal merchants");
        check(sawDifferentCatalogs && sawDifferentPrices, "same-category visitors independently offer different assortments and prices");
        TradingSupplier supplier = market.Suppliers[0];
        TradingGood wrong = catalog.First(g => g.Category != supplier.Specialty);
        supplier.Stock[wrong.Id] = 100;
        check(market.Maximum(supplier, wrong, 0, true, 10) == 0 && market.Maximum(supplier, wrong, 0, false, 10) == 0 &&
            !market.Settle(supplier, wrong, 0, false, 1), "merchant specialization is enforced in trading, including forged stock");
        supplier.Stock.Remove(wrong.Id);
        for (int cycle = 0; cycle < 60; cycle++)
        {
            market.Refresh(cycle, catalog);
            check(market.Suppliers.Count == 5, "replacement keeps five visitors without enforcing category uniqueness");
            foreach (TradingSupplier merchant in market.Suppliers)
                check(merchant.Stock.Count == (TradingCharacters.Find(merchant) == null ? 12 : 6) && merchant.Stock.Keys.All(id => catalog.Single(g => g.Id == id).Category == merchant.Specialty),
                    "every offered good belongs to its supplier's exclusive specialty");
        }
        var old = new TradingMarket { Balance = 321m, NextId = 6 };
        for (int i = 0; i < 5; i++)
            old.Suppliers.Add(new TradingSupplier { Id = i + 1, Departure = 100, BuyLimit = 1000m, SellLimit = 2000m,
                Bought = 123m, Sold = 456m, Stock = new Dictionary<string, double> { { "oldMixedGood", 99 } } });
        check(old.Refresh(60, catalog), "old mixed merchants migrate to specialties");
        check(old.Balance == 321m && old.NextId == 6 && old.Suppliers.All(s => s.Departure == 100 && s.Bought == 123m && s.Sold == 456m &&
            s.BuyLimit == 1000m && s.SellLimit == 2000m), "specialty migration preserves wallet, turnover, limits and departures");
        check(old.Suppliers.All(s => !s.Stock.ContainsKey("oldMixedGood") && s.Stock.Keys.All(id => catalog.Single(g => g.Id == id).Category == s.Specialty)),
            "old mixed catalogs become independent specialized catalogs that may share categories");
        check(!old.Refresh(60.1, catalog), "specialty migration runs once and never refills stock on reopening");
        var savedVisitors = old.Suppliers.Select(s => s.Id + ":" + s.Specialty).ToArray();
        old.Refresh(60.9, catalog);
        check(old.Suppliers.Select(s => s.Id + ":" + s.Specialty).SequenceEqual(savedVisitors), "the new random lineup rule does not reroll existing visitors");
    }
}
