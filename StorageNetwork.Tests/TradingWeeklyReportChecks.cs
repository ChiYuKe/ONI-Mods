using System;
using System.Linq;
using System.Text.Json;
using StorageNetwork.Trading;

internal static class TradingWeeklyReportChecks
{
    public static void Verify(Action<bool, string> check)
    {
        var catalog = new[] { new TradingGood { Id = "Iron", Name = "Iron", Category = "Metal", BasePrice = 2m } };
        var templates = Enumerable.Range(0, 20).Select(i => new TradingIdentityTemplate {
            PersonalityId = "Person" + i, Name = "Visitor" + i, HeadShape = "Head", Hair = "Hair", Eyes = "Eyes", Mouth = "Mouth" }).ToArray();
        var market = new TradingMarket { Balance = 1234m };
        market.Refresh(0, catalog);
        foreach (var s in market.Suppliers) { market.EnsureIdentity(s, templates); market.EnsurePersona(s); }
        var options = new JsonSerializerOptions { IncludeFields = true };
        string inventory = JsonSerializer.Serialize(market.Suppliers, options);
        int nextId = market.NextId;
        check(market.UpdateWeeklyReports(0, catalog, templates), "first weekly report is published");
        check(market.WeeklyReports.Count == 1 && market.WeeklyReports[0].Articles.Count == 8 && !market.WeeklyReports[0].Read,
            "weekly issue contains eight distinct intelligence columns with unread state");
        check(market.NextId == nextId && market.Balance == 1234m && inventory == JsonSerializer.Serialize(market.Suppliers, options),
            "publication does not alter current visitors, stock, money or next visitor ID");
        string report = JsonSerializer.Serialize(market.WeeklyReports, options);
        check(!market.UpdateWeeklyReports(6.99, catalog, templates) && report == JsonSerializer.Serialize(market.WeeklyReports, options),
            "opening within a week cannot reroll intelligence");
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
        check(!restored.UpdateWeeklyReports(1, catalog, templates) && report == JsonSerializer.Serialize(restored.WeeklyReports, options),
            "saving and loading preserves issue contents and publication schedule");
        var predicted = restored.UpcomingVisit.Visitor;
        string predictedName = predicted.Name, predictedSpecialty = predicted.Specialty, predictedCharacter = predicted.CharacterId;
        double expected = restored.UpcomingVisit.ExpectedCycle;
        restored.Refresh(expected, catalog);
        var arrived = restored.Suppliers.Single(s => s.Id == predicted.Id);
        check(arrived.Name == predictedName && arrived.Specialty == predictedSpecialty && arrived.CharacterId == predictedCharacter,
            "forecast visitor actually arrives with the announced identity");
        check(restored.UpcomingVisit == null && restored.Suppliers.Count == 5 && arrived.Departure > expected,
            "arrival consumes the plan and receives a real visit without adding a sixth merchant");
        check(market.UpdateWeeklyReports(7, catalog, templates) && market.WeeklyReports.Last().Number == 2,
            "new weekly issue is published at seven cycles");
        check(market.WeeklyReports.All(r => r.Articles.All(a => !TradingText.Source(a.Body).Contains("STRINGS.UI."))),
            "weekly prose resolves every localization reference");
        market.WeeklyReports[0].Read = true;
        var readCopy = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
        check(readCopy.WeeklyReports[0].Read, "read status survives saving");
        for (int week = 2; week < 22; week++)
        {
            market.Refresh(week * 7, catalog);
            market.UpdateWeeklyReports(week * 7, catalog, templates);
        }
        check(market.WeeklyReports.Count == 16 && market.WeeklyReports.First().Number == 7, "weekly archive keeps only the latest sixteen issues");
        int before = market.WeeklyReports.Last().Number;
        market.Refresh(1400, catalog);
        market.UpdateWeeklyReports(1400, catalog, templates);
        check(market.WeeklyReports.Last().Number == before + 1 && market.WeeklyReports[14].Number == before,
            "long absences publish current intelligence without fabricating missed issues");
        check(!market.UpdateWeeklyReports(double.NaN, catalog, templates), "invalid bulletin time is rejected");
        var late = new TradingMarket();
        late.Refresh(6.7, catalog);
        late.UpdateWeeklyReports(6.7, catalog, templates);
        check(!late.UpdateWeeklyReports(7, catalog, templates) && !late.UpdateWeeklyReports(13.699, catalog, templates),
            "a report first shown at displayed cycle 7.7 cannot refresh at 8.0 or before seven full cycles");
        var lateCopy = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(late, options), options);
        check(lateCopy.UpdateWeeklyReports(6.7 + 7, catalog, templates) && lateCopy.WeeklyReports.Last().Number == 2,
            "seven elapsed publication cycles trigger exactly one next issue after reload");
        VerifyMerchantTips(check, catalog, templates, options);
        VerifyEditorialRotation(check, templates, options);
    }

    private static void VerifyEditorialRotation(Action<bool, string> check, TradingIdentityTemplate[] templates,
        JsonSerializerOptions options)
    {
        var goods = Enumerable.Range(0, 12).Select(i => new TradingGood {
            Id = "Resource" + i, Name = "Resource " + i, Category = "Metal", BasePrice = 30m + i }).ToArray();
        var market = new TradingMarket();
        market.Refresh(0, goods);
        foreach (var supplier in market.Suppliers)
        {
            supplier.Departure = 200;
            market.EnsureIdentity(supplier, templates);
            market.EnsurePersona(supplier);
        }
        market.UpdateWeeklyReports(0, goods, templates);
        string arrivalTopic = "arrival:" + market.UpcomingVisit.Visitor.Id;
        for (int edition = 1; edition <= 8; edition++)
        {
            double cycle = edition * 7;
            market.Refresh(cycle, goods);
            var recent = market.WeeklyReports.Skip(Math.Max(0, market.WeeklyReports.Count - 3)).SelectMany(r => r.Articles).ToList();
            var previous = market.WeeklyReports.Last();
            market.UpdateWeeklyReports(cycle, goods, templates);
            var issue = market.WeeklyReports.Last();
            check(issue.Articles.Count >= 6 && issue.Articles.Count <= 8, "editorial selection fills an issue without padding duplicate stories");
            check(issue.Articles.Select(a => a.Column).Distinct().Count() == issue.Articles.Count,
                "each issue selects at most one story per column");
            check(!issue.Articles.Any(a => previous.Articles.Any(p => p.Topic == a.Topic)),
                "the immediately preceding issue's topics are not reprinted");
            check(!issue.Articles.Any(a => recent.Any(p => p.Body == a.Body)),
                "identical paragraphs are suppressed across three issues");
            check(issue.Articles.All(a => !TradingText.Source(a.Body).Contains("STRINGS.UI.")),
                "every editorial variant and factual argument resolves");
            if (edition == 3)
            {
                var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
                var original = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
                restored.Refresh(cycle + 7, goods);
                original.Refresh(cycle + 7, goods);
                restored.UpdateWeeklyReports(cycle + 7, goods, templates);
                original.UpdateWeeklyReports(cycle + 7, goods, templates);
                check(JsonSerializer.Serialize(restored.WeeklyReports, options) == JsonSerializer.Serialize(original.WeeklyReports, options),
                    "persisted editorial history deterministically preserves the next issue's selection");
            }
        }
        check(market.WeeklyReports.Sum(r => r.Articles.Count(a => a.Topic == arrivalTopic)) == 1,
            "a ship waiting several weeks is only previewed once");
        check(market.WeeklyReports.Select(r => string.Join(",", r.Articles.Select(a => a.Column).OrderBy(c => c))).Distinct().Count() >= 3,
            "successive issues rotate column combinations rather than repeating one fixed layout");
        check(market.WeeklyReports.SelectMany(r => r.Articles).Where(a => a.Column == "cargo").Select(a => a.Topic).Distinct().Count() >= 3,
            "cargo reporting covers different actual goods over time");
    }

    private static void VerifyMerchantTips(Action<bool, string> check, TradingGood[] catalog,
        TradingIdentityTemplate[] templates, JsonSerializerOptions options)
    {
        var market = new TradingMarket();
        market.Refresh(0, catalog);
        // Long-lived fixtures isolate tip scheduling from departures.
        foreach (var supplier in market.Suppliers)
        {
            supplier.Departure = 100;
            market.EnsureIdentity(supplier, templates);
            market.EnsurePersona(supplier);
        }
        market.UpdateWeeklyReports(0, catalog, templates);
        market.UpdateMerchantTips(6.7, catalog);
        check(market.MerchantTips.Count == 2 && market.MerchantTips.Values.All(t => t.Articles.Count == 3),
            "only two merchants offer route, cargo and personal intelligence");
        var tip = market.MerchantTips.Values.First();
        check(market.PendingMerchantTip(tip.SupplierId, 6.7) == tip, "unread merchant intelligence is available through the alert");
        tip.Read = true;
        check(market.PendingMerchantTip(tip.SupplierId, 6.7) == null, "reading intelligence clears the corresponding alert");
        string snapshot = JsonSerializer.Serialize(market.MerchantTips, options);
        check(!market.UpdateMerchantTips(7, catalog) && !market.UpdateMerchantTips(13.699, catalog) && snapshot == JsonSerializer.Serialize(market.MerchantTips, options),
            "refreshes cannot reroll or resurrect a read merchant message");
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
        check(restored.MerchantTips[tip.SupplierId].Read && !restored.UpdateMerchantTips(13.699, catalog),
            "merchant intelligence and its read state survive a reload");
        check(restored.PendingMerchantTip(tip.SupplierId, 13) == null, "a new day in the same window does not restore read alerts");
        check(restored.PendingMerchantTip(tip.SupplierId, 6.7 + 7) == null, "old messages expire after seven cycles even before cleanup runs");
        restored.UpdateMerchantTips(6.7 + 7, catalog);
        check(restored.MerchantTips.Count == 2 && restored.MerchantTips.Values.All(t => t.Window == 1),
            "later conversations produce a fresh batch of intelligence");
        int departed = restored.MerchantTips.Keys.First();
        restored.Suppliers.RemoveAll(s => s.Id == departed);
        check(restored.PendingMerchantTip(departed, 13.8) == null, "departed merchants cannot deliver stale intelligence");
        restored.UpdateMerchantTips(14, catalog);
        check(restored.MerchantTips.Count <= 2 && !restored.MerchantTips.ContainsKey(departed),
            "lineup changes remove departed tips and never light more than two alerts");
        check(restored.MerchantTips.Values.All(t => t.Articles.All(a => !TradingText.Source(a.Body).Contains("STRINGS.UI."))),
            "merchant messages resolve all bilingual text references");
    }
}
