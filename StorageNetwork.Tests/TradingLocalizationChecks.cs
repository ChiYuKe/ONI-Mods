using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using StorageNetwork.Trading;

internal static class TradingLocalizationChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var translations = LoadPo();
        string Lookup(string key) => translations.TryGetValue(key, out string value) ? value : key;
        TradingText.Configure("en", Lookup);
        TradingText.NativeLabels = value => value;
        foreach (var entry in TradingTextCatalog.Entries)
        {
            check(translations.TryGetValue(entry.Key, out string english) && !Regex.IsMatch(english, "[\\u4e00-\\u9fff]"), "every registered key has an English PO resource");
            int count = Regex.Matches(entry.Source, @"\{\d+\}").Count;
            string[] args = Enumerable.Range(0, count).Select(i => "VALUE" + i).ToArray();
            string reference = TradingText.Ref(entry.Key, args);
            check(TradingText.Render(reference) == (count == 0 ? english : string.Format(english, args)), "key lookup uses the PO translation and preserves parameters");
            check(TradingText.Source(reference) == (count == 0 ? entry.Source : string.Format(entry.Source, args)), "source-language resolution is independent of displayed language");
        }
        string Key(string source) => TradingTextCatalog.Entries.Single(e => e.Source == source).Key;
        string buy = TradingText.Ref(Key("买入"));
        check(TradingText.Render("买入") == "买入", "normal rendering never matches or replaces arbitrary Chinese prose");
        check(TradingText.Render("<color=#ffcc00>" + buy + "</color> 12.375 kg ¥25.90") == "<color=#ffcc00>Buy</color> 12.375 kg ¥25.90",
            "rich-text markup and financial values survive key-based localization");
        foreach (var category in new[] { "Metal", "Food", "Industrial", "Gas", "Liquid", "Seed", "Equipment", "Mineral" })
        {
            var supplier = new TradingSupplier { Id = 37, Specialty = category, Name = "Trader" };
            TradingPersonas.Ensure(supplier);
            string saved = JsonSerializer.Serialize(supplier, new JsonSerializerOptions { IncludeFields = true });
            check(!Regex.IsMatch(saved, "[\\u4e00-\\u9fff]"), "new identity saves contain references, not canonical Chinese sentences");
            check(!Regex.IsMatch(TradingText.Render(TradingPersonas.PublicCard(supplier)), "[\\u4e00-\\u9fff]"), "English cards resolve nested keys and arguments");
            check(JsonSerializer.Serialize(supplier, new JsonSerializerOptions { IncludeFields = true }) == saved, "rendering never rewrites saved identities");
        }
        var line = new TradingDialogueLine { Player = true, Text = buy };
        check(line.Content.Count == 1 && line.Content[0].Key == Key("买入") && line.Content[0].Arguments.Length == 0,
            "chat records persist explicit key and argument fields");
        var market = new TradingMarket();
        var visitor = new TradingSupplier { Id = 5, CharacterId = "greyfinch", Name = TradingCharacters.Roster[0].Name, Specialty = "Metal", Departure = 10 };
        market.Suppliers.Add(visitor);
        TradingDialogue.Assign(visitor);
        TradingConversations.Upgrade(visitor);
        var context = TradingDialogueContexts.Build(market, visitor, Array.Empty<TradingGood>(), 1);
        check(context.Language == "en" && context.Name == "Greyfinch" && context.Rules.All(r => !Regex.IsMatch(r, "[\\u4e00-\\u9fff]")), "AI context resolves keys using the current language");
        TradingText.Configure("zh-CN");
        check(TradingText.Render(buy) == "买入", "Chinese uses the registered default");
        TradingText.Configure("en", key => key);
        check(TradingText.Render(buy) == "买入", "missing PO translations fall back to the source, not a hardcoded English copy");
        TradingText.Configure("en", key => key == Key("买入") ? "CUSTOM PO TEXT" : Lookup(key));
        check(TradingText.Render(buy) == "CUSTOM PO TEXT", "editing a PO entry changes display without a second English code table");
        TradingText.Configure("zh");
        TradingText.NativeLabels = null;
    }
    private static Dictionary<string, string> LoadPo()
    {
        var values = new Dictionary<string, string>();
        string po = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "en.po"));
        foreach (Match match in Regex.Matches(po, "msgctxt (\"(?:\\\\.|[^\"])*\")\\s+msgid (\"(?:\\\\.|[^\"])*\")\\s+msgstr (\"(?:\\\\.|[^\"])*\")"))
            values[JsonSerializer.Deserialize<string>(match.Groups[1].Value)] = JsonSerializer.Deserialize<string>(match.Groups[3].Value);
        return values;
    }
}
