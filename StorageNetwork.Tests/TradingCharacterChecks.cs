using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using StorageNetwork.Trading;

internal static class TradingCharacterChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var catalog = TradingCharacters.Roster.SelectMany(c => Enumerable.Range(1, 24).Select(i =>
            new TradingGood { Id = c.Specialty + i, Category = c.Specialty, BasePrice = i * 10m, ByUnit = c.Specialty == "Food" })).ToArray();
        var seen = new HashSet<string>();
        bool ordinary = false;
        for (int batch = 0; batch < 100; batch++)
        {
            var market = new TradingMarket { NextId = batch * 5 + 1 };
            market.Refresh(20, catalog);
            var characters = market.Suppliers.Where(s => TradingCharacters.Find(s) != null).ToArray();
            check(characters.Select(s => s.CharacterId).Distinct().Count() == characters.Length,
                "one narrative character cannot occupy two active supplier slots");
            ordinary |= market.Suppliers.Any(s => string.IsNullOrEmpty(s.CharacterId));
            foreach (var supplier in characters)
            {
                seen.Add(supplier.CharacterId);
                var character = TradingCharacters.Find(supplier);
                check(supplier.Name == character.Name && supplier.Specialty == character.Specialty &&
                    supplier.Departure - supplier.Arrival >= 1 && supplier.Departure - supplier.Arrival <= 15,
                    "recurring characters retain names and specialties within the common visit range");
                check(supplier.Stock.Count == 6 && supplier.Stock.Keys.All(id => catalog.Single(g => g.Id == id).BasePrice >= 130),
                    "character stock samples the valuable half of its own specialty");
                check(supplier.BuyLimit > 0 && supplier.SellLimit > 0 && supplier.Bought == 0 && supplier.Sold == 0,
                    "character visits start with independent finite turnover limits");
                check(!TradingText.Source(character.Description).Contains("黑商") && !TradingText.Source(character.Description).Contains("非法"),
                    "player-facing clues do not expose a hidden role label");
                check(catalog.Where(g => g.Category == supplier.Specialty).All(g =>
                    TradingMarket.Price(supplier, g, 20, true) > TradingMarket.Price(supplier, g, 20, false)),
                    "unusual character prices preserve the bid ask spread");
            }
        }
        check(seen.Count == 3 && ordinary, "all three built-in characters can appear among ordinary merchants");
        var templates = Enumerable.Range(0, 10).Select(i => new TradingIdentityTemplate
            { PersonalityId = "P" + i, Name = "N" + i, HeadShape = "H" + i, Hair = "R" + i, Eyes = "E" + i, Mouth = "M" + i }).ToArray();
        var identityMarket = new TradingMarket();
        var first = new TradingSupplier { Id = 10, CharacterId = TradingCharacters.Roster[0].Id };
        identityMarket.Suppliers.Add(first);
        check(identityMarket.EnsureIdentity(first, templates), "a character's first appearance is cached for later visits");
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(identityMarket, jsonOptions), jsonOptions);
        var next = new TradingSupplier { Id = 300, CharacterId = first.CharacterId };
        restored.EnsureIdentity(next, templates.Reverse().ToArray());
        check(next.Name == first.Name && next.Appearance.HeadShape == first.Appearance.HeadShape &&
            next.Appearance.Hair == first.Appearance.Hair && next.Appearance.Eyes == first.Appearance.Eyes &&
            next.Appearance.Mouth == first.Appearance.Mouth && restored.Suppliers[0].CharacterId == first.CharacterId,
            "saved character faces remain stable across visits, reload and resource reorder");
        var direct = new TradingSupplier { Id = 500, CharacterId = first.CharacterId };
        TradingIdentity.Ensure(direct, templates);
        check(direct.Appearance.Hair == first.Appearance.Hair && direct.Appearance.Eyes == first.Appearance.Eyes,
            "character appearance generation uses identity rather than visit number");
        var legacy = new TradingMarket { NextId = 500 };
        legacy.Suppliers.Add(new TradingSupplier { Id = 100, Specialty = "Metal", Name = "Existing", Departure = 100, Bought = 9m });
        legacy.Refresh(20, catalog);
        check(legacy.Suppliers[0].Name == "Existing" && legacy.Suppliers[0].Bought == 9m && legacy.Suppliers[0].CharacterId == null,
            "existing visitors are not retroactively converted into narrative characters");
    }
}
