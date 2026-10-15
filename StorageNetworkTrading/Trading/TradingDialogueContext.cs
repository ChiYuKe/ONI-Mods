using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    // Detached, versioned data for a future AI adapter. No Unity objects or economy mutation authority.
    internal sealed class TradingDialogueContext
    {
        public int SchemaVersion = 1;
        public string Language;
        public string IdentityId, Name, Specialty, CurrentNode;
        public int VisitId;
        public int NextVisitMinimumCycles, MaximumVisitCycles = TradingRelationships.MaximumVisitCycles;
        public TradingPublicPersona PublicPersona;
        public TradingPrivatePersona AuthorKnowledge;
        public TradingDialogueMemory Relationship;
        public double Cycle, Departure;
        public decimal Wallet, RemainingPurchaseBudget, RemainingSupplyBudget, CooperationFactor;
        public List<TradingContextGood> Goods;
        public List<TradingDialogueLine> Conversation;
        public string[] AllowedChoices, Rules;
    }

    internal sealed class TradingContextGood
    {
        public string Id, Name, Unit;
        public double Stock;
        public decimal PlayerBuyPrice, PlayerSellPrice;
        public bool OffersForPurchase;
        public decimal BuyChangePercent, SellChangePercent;
    }

    internal static class TradingDialogueContexts
    {
        internal static TradingDialogueContext Build(TradingMarket market, TradingSupplier supplier, IReadOnlyList<TradingGood> catalog, double cycle)
        {
            if (market == null || supplier == null || !market.Suppliers.Contains(supplier) || supplier.Departure <= cycle) return null;
            TradingPersonas.Ensure(supplier);
            TradingPublicPersona p = supplier.Persona.Public;
            TradingPrivatePersona notes = supplier.Persona.Private;
            TradingDialogueMemory memory = supplier.Relationship ?? new TradingDialogueMemory();
            if (!string.IsNullOrEmpty(supplier.CharacterId) && market.DialogueMemories != null &&
                market.DialogueMemories.TryGetValue(supplier.CharacterId, out TradingDialogueMemory saved) && saved != null) memory = saved;
            return new TradingDialogueContext
            {
                Language = TradingText.English ? "en" : "zh", IdentityId = supplier.Persona.IdentityId, VisitId = supplier.Id, Name = TradingText.Render(supplier.Name), Specialty = supplier.Specialty,
                CurrentNode = supplier.DialogueNode, Cycle = cycle, Departure = supplier.Departure, Wallet = market.Balance,
                NextVisitMinimumCycles = TradingRelationships.MinimumVisitCycles(supplier),
                RemainingPurchaseBudget = Math.Max(0, supplier.BuyLimit - supplier.Bought),
                RemainingSupplyBudget = Math.Max(0, supplier.SellLimit - supplier.Sold), CooperationFactor = supplier.DialoguePriceFactor,
                PublicPersona = new TradingPublicPersona { Temperament = TradingText.Render(p.Temperament), SpeechStyle = TradingText.Render(p.SpeechStyle),
                    Background = TradingText.Render(p.Background), Goal = TradingText.Render(p.Goal), TradeStyle = TradingText.Render(p.TradeStyle), Habit = TradingText.Render(p.Habit), Greeting = TradingText.Render(p.Greeting),
                    HubReply = TradingText.Render(p.HubReply), Likes = p.Likes?.Select(TradingText.Render).ToArray(), Dislikes = p.Dislikes?.Select(TradingText.Render).ToArray(), Boundaries = p.Boundaries?.Select(TradingText.Render).ToArray() },
                AuthorKnowledge = new TradingPrivatePersona { Background = TradingText.Render(notes.Background), Motivation = TradingText.Render(notes.Motivation), DisclosureRule = TradingText.Render(notes.DisclosureRule) },
                Relationship = new TradingDialogueMemory { Trust = memory.Trust, Visits = memory.Visits, LastVisitId = memory.LastVisitId,
                    Affection = memory.Affection, SuccessfulTrades = memory.SuccessfulTrades, TotalTradeValue = memory.TotalTradeValue },
                Goods = (catalog ?? Array.Empty<TradingGood>()).Where(g => g.Category == supplier.Specialty)
                    .Select(g => new TradingContextGood { Id = g.Id, Name = TradingText.Render(g.Name), Unit = g.ByUnit ? TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7C0025557708")) : "kg",
                        Stock = supplier.Stock.TryGetValue(g.Id, out double stock) ? stock : 0,
                        OffersForPurchase = supplier.InitialStock.ContainsKey(g.Id) || (supplier.Stock.TryGetValue(g.Id, out double currentStock) && currentStock > 0),
                        BuyChangePercent = PriceChange(supplier, g, cycle, true), SellChangePercent = PriceChange(supplier, g, cycle, false),
                        PlayerBuyPrice = TradingMarket.Price(supplier, g, cycle, true), PlayerSellPrice = TradingMarket.Price(supplier, g, cycle, false) }).ToList(),
                Conversation = (supplier.DialogueLines ?? new List<TradingDialogueLine>()).Select(l => new TradingDialogueLine { Player = l.Player, Text = TradingText.Render(l.Text) }).ToList(),
                AllowedChoices = TradingDialogue.Pending(supplier, cycle) ? TradingConversations.Choices(supplier).Select(TradingText.Render).ToArray() : Array.Empty<string>(),
                Rules = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_844E41354D42"),
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_10E05F1551F5"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_45F0AE2749C2"),
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2DF10EF13444"),
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2AB28769EA70"),
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_10F0573B8AD7") }.Select(TradingText.Render).ToArray()
            };
        }
        private static decimal PriceChange(TradingSupplier supplier, TradingGood good, double cycle, bool buy)
        {
            var previous = supplier.History.TryGetValue(good.Id, out var history)
                ? history.LastOrDefault(p => p.Cycle == Math.Floor(cycle) - 1) : null;
            decimal old = previous == null ? 0 : buy ? previous.Buy : previous.Sell;
            return old <= 0 ? 0 : Math.Round((TradingMarket.Price(supplier, good, cycle, buy) / old - 1m) * 100m, 2);
        }
    }
}
