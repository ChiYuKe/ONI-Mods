using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using StorageNetwork.Trading;

internal static class TradingGroupChatChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var suppliers = new List<TradingSupplier>();
        for (int id = 1; id <= 5; id++) suppliers.Add(new TradingSupplier { Id = id, Name = id <= 2 ? "Ada" : "Merchant" + id, Departure = 20 });
        var picks = TradingGroupChat.Responders("@Ada#2 hello", suppliers, 1, 0, () => 0.99);
        check(picks.SequenceEqual(new[] { 2 }), "explicit mentions disambiguate visitors with identical names and bypass probability");
        picks = TradingGroupChat.Responders("@#1 hi @Merchant4 hi", suppliers, 1, 0, () => 0.99);
        check(picks.SequenceEqual(new[] { 1, 4 }), "one player message can address multiple active suppliers");
        check(TradingGroupChat.Responders("@Ada#20 hi", suppliers, 1, 100, () => 0).Count == 0, "partial numeric mentions do not route to the wrong visitor");
        check(TradingGroupChat.Responders("@Unknown hi", suppliers, 1, 100, () => 0).Count == 0, "unknown mentions do not silently trigger random agents");
        check(TradingGroupChat.Responders("hi", suppliers, 1, 0, () => 0).Count == 0, "zero probability disables unsolicited replies");
        check(TradingGroupChat.Responders("hi", suppliers, 1, 100, () => 0.99).Count == 5, "one hundred percent probability addresses all active suppliers");
        double[] random = { 0.1, 0.29, 0.3, 0.8, 0.2 };
        int index = 0;
        picks = TradingGroupChat.Responders("hi", suppliers, 1, 30, () => random[index++]);
        check(picks.SequenceEqual(new[] { 1, 2, 5 }) && index == 5, "each supplier independently receives the configured thirty percent chance");
        suppliers[1].Departure = 1;
        check(TradingGroupChat.Responders("@Ada#2 hi", suppliers, 1, 100, () => 0).Count == 0, "departed visitors never receive directed messages");
        var chat = new TradingGroupChat();
        chat.Route("@Merchant4 有石油卖吗？", suppliers, 1, 0, () => 0.99);
        picks = chat.Route("能不能便宜点", suppliers, 1, 0, () => 0.99);
        check(picks.SequenceEqual(new[] { 4 }) && chat.RoutingMode == "follow_up" && chat.TopicAnchor.Contains("石油"),
            "a bargaining follow-up always reaches the oil supplier despite zero spontaneous reply probability");
        picks = chat.Route("便宜点可以吗", suppliers, 1, 100, () => 0);
        check(picks[0] == 4 && chat.AddressedSuppliers.SequenceEqual(new[] { 4 }),
            "the conversation owner replies before bystanders and remains the sole addressed supplier");
        picks = chat.Route("Merchant3，你最近怎么样？", suppliers, 1, 0, () => 0.99);
        check(picks.SequenceEqual(new[] { 3 }) && chat.FocusSupplierId == 3 && chat.RoutingMode == "name", "natural direct address changes the conversation owner without @");
        picks = chat.Route("大家，你们价格如何", suppliers, 1, 0, () => 0.99);
        check(chat.RoutingMode == "group" && chat.FocusSupplierId == -1, "a group question resets the previous private conversation focus");
        suppliers[1].Departure = 20;
        picks = chat.Route("Ada，你好", suppliers, 1, 100, () => 0);
        check(picks.Count == 0 && chat.RoutingMode == "ambiguous_name", "ambiguous natural names require an explicit visitor number");
        chat.Route("@Ada#2 石油多少钱", suppliers, 1, 0, () => 0);
        suppliers[1].Departure = 1;
        picks = chat.Route("能便宜吗", suppliers, 1, 0, () => 0);
        check(picks.Count == 0 && chat.FocusSupplierId == -1, "follow-ups never revive departed suppliers");
        var templates = Enumerable.Range(1, 12).Select(i => new TradingIdentityTemplate { PersonalityId = "Person" + i, Name = "Name" + i }).ToArray();
        var visitors = new List<TradingSupplier>();
        for (int id = 100; id < 105; id++)
        {
            var visitor = new TradingSupplier { Id = id };
            visitors.Add(visitor);
            TradingIdentity.Ensure(visitor, templates, visitors);
        }
        check(visitors.Select(v => v.Name).Distinct().Count() == 5, "new active visitors avoid duplicate names when enough identity templates exist");
        string savedName = visitors[0].Name;
        check(!TradingIdentity.Ensure(visitors[0], templates, visitors) && visitors[0].Name == savedName,
            "distinct-name allocation never rerolls an existing supplier identity");
        for (int i = 0; i < 150; i++) chat.Add(new TradingGroupMessage { Player = i % 2 == 0, SupplierId = 3, Name = "Merchant3", Text = "message " + i, Cycle = i,
            Appearance = new TradingAppearance { Hair = "hair_001", HeadShape = "headshape_001" } });
        check(chat.Messages.Count == 128 && chat.Messages[0].Text == "message 22" && chat.EarlierContext.Contains("message 0"), "old group messages enter an archive instead of growing the save without bound");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(new TradingMarket { GroupChat = chat }, options), options);
        check(restored.GroupChat.Messages.Last().Text == "message 149" && restored.GroupChat.EarlierContext == chat.EarlierContext && restored.GroupChat.Messages.Last().Appearance.Hair == "hair_001",
            "group chat chronology and earlier context survive saves");
        for (int i = 0; i < 500; i++) chat.Add(new TradingGroupMessage { Text = new string('x', 2000) });
        check(chat.EarlierContext.Length <= 12000 && chat.Messages.Count == 128, "long conversations keep bounded archive and transcript storage");
    }
}
