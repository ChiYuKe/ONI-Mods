using System;
using System.Linq;
using System.Text.Json;
using StorageNetwork.Trading;

internal static class TradingDialogueChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        VerifyConversations(check);
        var visitors = Enumerable.Range(1, 100).Select(id => new TradingSupplier { Id = id, Departure = 10 }).ToArray();
        foreach (var visitor in visitors) TradingDialogue.Assign(visitor);
        int pending = visitors.Count(s => TradingDialogue.Pending(s, 0));
        check(pending > 0 && pending < visitors.Length, "dialogue alerts appear on only some ordinary supplier visits");
        var speaker = visitors.First(s => TradingDialogue.Pending(s, 0));
        string eventId = speaker.DialogueEventId;
        check(!TradingDialogue.Assign(speaker) && speaker.DialogueEventId == eventId,
            "refreshing does not reroll a dialogue event");
        check(!TradingDialogue.Advance(speaker, 0, -1) && !TradingDialogue.Advance(speaker, 0, 2) && speaker.DialogueStage == 0,
            "invalid choices cannot change dialogue progress");
        check(TradingDialogue.Advance(speaker, 0, 1) && speaker.DialogueStage == 1 && speaker.DialogueChoice == 1,
            "a valid choice enters its reply node");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingSupplier>(JsonSerializer.Serialize(speaker, options), options);
        check(TradingDialogue.Pending(restored, 1) && restored.DialogueEventId == eventId &&
            restored.DialogueChoice == 1 && TradingDialogue.Reply(restored) == TradingDialogue.Reply(speaker),
            "closing or reloading retains the selected dialogue branch");
        check(!TradingDialogue.Advance(restored, 1, 1) && restored.DialogueStage == 1,
            "reply nodes accept only their finish choice");
        check(TradingDialogue.Advance(restored, 1, 0) && restored.DialogueStage == 2 && !TradingDialogue.Pending(restored, 1),
            "finishing removes the dialogue alert");
        check(!TradingDialogue.Assign(restored) && !TradingDialogue.Advance(restored, 1, 0),
            "finished events cannot be repeated during the same visit");
        check(!TradingDialogue.Pending(speaker, 10) && !TradingDialogue.Advance(speaker, 10, 0),
            "departure prevents stale dialogue callbacks from advancing an event");
        check(!TradingDialogue.Pending(null, 0) && !TradingDialogue.Advance(null, 0, 0),
            "missing suppliers cannot trigger dialogue");
        foreach (var character in TradingCharacters.Roster)
        {
            var visitor = new TradingSupplier { Id = 500, CharacterId = character.Id, Departure = 3 };
            TradingDialogue.Assign(visitor);
            check(visitor.DialogueEventId == character.Id && TradingDialogue.Pending(visitor, 0),
                "fixed characters receive their own conversation event");
            string opening = TradingDialogue.Opening(visitor);
            TradingDialogue.Advance(visitor, 0, 0);
            string first = TradingDialogue.Reply(visitor);
            visitor.DialogueChoice = 1;
            string second = TradingDialogue.Reply(visitor);
            check(first != second && !TradingText.Source((opening + first + second)).Contains("黑商"),
                "character branches provide distinct clues without explicit role labels");
        }
    }

    private static void VerifyConversations(Action<bool, string> check)
    {
        var good = new TradingGood { Id = "Iron", Name = "铁", Category = "Metal", BasePrice = 100 };
        var catalog = new[] { good };
        var market = new TradingMarket { Balance = 50000 };
        var speaker = new TradingSupplier { Id = 51, CharacterId = "greyfinch", Name = "灰雀", Specialty = "Metal",
            Departure = 5, BuyLimit = 50000, SellLimit = 50000 };
        speaker.Stock[good.Id] = 100;
        TradingDialogue.Assign(speaker);
        market.Suppliers.Add(speaker);
        decimal before = TradingMarket.Price(speaker, good, 1, true);
        check(!market.AdvanceConversation(speaker, 1, 99, catalog) && speaker.DialogueLines.Count == 0,
            "invalid conversation choices cannot append messages or apply effects");
        check(market.AdvanceConversation(speaker, 1, 2, catalog) && speaker.DialogueNode == "personal",
            "multi-round conversation enters the personal topic");
        check(market.AdvanceConversation(speaker, 1, 1, catalog) && market.Memory(speaker).Trust == 1,
            "respectful choices establish persistent character trust");
        market.AdvanceConversation(speaker, 1, 0, catalog);
        market.AdvanceConversation(speaker, 1, 2, catalog);
        market.AdvanceConversation(speaker, 1, 1, catalog);
        check(market.Memory(speaker).Trust == 1, "repeating a topic cannot farm trust during one visit");
        market.AdvanceConversation(speaker, 1, 0, catalog);
        market.AdvanceConversation(speaker, 1, 3, catalog);
        check(speaker.DialogueNode == "offer" && TradingMarket.Price(speaker, good, 1, true) == before,
            "discussing a price offer does not silently accept it");
        var oldQuote = market.Quote(speaker, good, 1, true, 1, 100);
        int version = market.QuoteVersion;
        market.AdvanceConversation(speaker, 1, 0, catalog);
        decimal discounted = TradingMarket.Price(speaker, good, 1, true);
        check(speaker.DialogueNegotiated && speaker.DialoguePriceFactor == 0.95m && discounted < before &&
            discounted > TradingMarket.Price(speaker, good, 1, false),
            "accepting a trusted cooperation offer changes real prices while preserving the spread");
        check(market.QuoteVersion > version && speaker.History[good.Id].Last().Buy == discounted,
            "conversation pricing effects refresh executable quote histories");
        var fresh = market.Quote(speaker, good, 1, true, 1, 100);
        check(TradingMarket.ConfirmationProblem(fresh, oldQuote.UnitPrice, oldQuote.Cycle, oldQuote.Amount, false) != null,
            "quotes opened before negotiation require revalidation");
        market.AdvanceConversation(speaker, 1, 0, catalog);
        market.AdvanceConversation(speaker, 1, 3, catalog);
        check(speaker.DialogueNode == "deal" && speaker.DialoguePriceFactor == 0.95m,
            "negotiation discounts cannot stack by reopening the same topic");
        for (int i = 0; i < 20; i++)
        {
            market.AdvanceConversation(speaker, 1, 0, catalog);
            market.AdvanceConversation(speaker, 1, 3, catalog);
        }
        check(speaker.DialogueLines.Count <= 24 && market.Memory(speaker).Visits == 1,
            "conversation transcripts are bounded and reopening does not add character visits");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var restored = JsonSerializer.Deserialize<TradingMarket>(JsonSerializer.Serialize(market, options), options);
        var saved = restored.Suppliers[0];
        check(saved.DialogueNode == speaker.DialogueNode && saved.DialogueLines.Count == speaker.DialogueLines.Count &&
            saved.DialogueTopics.Contains("respect") && saved.DialoguePriceFactor == 0.95m && restored.Memory(saved).Trust == 1,
            "save reload retains conversation nodes, transcript, topics, discounts and character memory");
        saved.Departure = 2;
        var returning = new TradingSupplier { Id = 99, CharacterId = "greyfinch", Name = "灰雀", Specialty = "Metal", Arrival = 2, Departure = 5 };
        TradingDialogue.Assign(returning);
        TradingConversations.Upgrade(returning);
        restored.Suppliers.Add(returning);
        check(TradingText.Source(TradingConversations.Text(restored, returning, catalog, 2)).Contains("上次") && returning.DialoguePriceFactor == 1,
            "returning characters recall prior interactions without inheriting an expired visit discount");
        restored.AdvanceConversation(returning, 2, 2, catalog);
        restored.AdvanceConversation(returning, 2, 0, catalog);
        check(restored.Memory(returning).Trust == 0 && restored.Memory(returning).Visits == 2,
            "character reactions and visit counts persist across appearances");
        check(!restored.AdvanceConversation(saved, 2, 0, catalog), "departed visitors cannot apply conversation effects");
        var legacy = new TradingSupplier { Id = 200, Departure = 5, DialogueAssigned = true, DialogueEventId = "cargo", DialogueStage = 1, DialogueChoice = 0 };
        string reply = TradingDialogue.Reply(legacy);
        check(TradingConversations.Upgrade(legacy) && legacy.DialogueNode == "hub" && legacy.DialogueLines[0].Text == reply,
            "unfinished older dialogue preserves the existing reply when upgraded");
        legacy.DialogueStage = 2;
        legacy.DialogueVersion = 0;
        check(!TradingConversations.Upgrade(legacy), "completed older conversations are not resurrected by migration");
    }
}
