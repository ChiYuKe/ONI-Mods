using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    internal sealed class TradingDialogueLine
    {
        public bool Player;
        public List<TradingLocalizedPart> Content = new List<TradingLocalizedPart>();
        [System.Runtime.Serialization.IgnoreDataMember]
        public string Text
        {
            get => TradingText.Compose(Content);
            set => Content = TradingText.Capture(value);
        }
    }

    internal sealed class TradingDialogueMemory
    {
        public int Trust;
        public int Visits;
        public int LastVisitId;
        public decimal Affection;
        public int SuccessfulTrades;
        public decimal TotalTradeValue;
    }

    internal static class TradingConversations
    {
        internal static bool Upgrade(TradingSupplier supplier)
        {
            if (supplier.DialogueVersion >= 2 || string.IsNullOrEmpty(supplier.DialogueEventId) || supplier.DialogueStage >= 2) return false;
            supplier.DialogueLines = supplier.DialogueLines ?? new List<TradingDialogueLine>();
            if (supplier.DialogueStage == 1)
            {
                supplier.DialogueLines.Add(new TradingDialogueLine { Text = TradingDialogue.Reply(supplier) });
                supplier.DialogueNode = "hub";
            }
            else supplier.DialogueNode = "greeting";
            supplier.DialogueStage = 0;
            supplier.DialogueVersion = 2;
            return true;
        }

        internal static string[] Choices(TradingSupplier supplier)
        {
            switch (supplier.DialogueNode)
            {
                case "cargo": return new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E213A3F4EEAA"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2CE1AA54DDBA"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CC121CE357DF") };
                case "market": return new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B73AA855A5A1"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2CE1AA54DDBA"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CC121CE357DF") };
                case "personal": return new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_4FD3D88204BD"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7D6AB165F5B4"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2CE1AA54DDBA"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CC121CE357DF") };
                case "offer": return new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6FD9995A7B4F"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_ABB3B99B4031"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CC121CE357DF") };
                case "batch": case "compare": case "origins": case "respect": case "deal":
                    return new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2CE1AA54DDBA"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CC121CE357DF") };
                default: return new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DE703B406DDF"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DBE18CB14AB2"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_09E9F29C9CD6"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_84D328FBC8EA"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CC121CE357DF") };
            }
        }

        internal static string Text(TradingMarket market, TradingSupplier supplier, IReadOnlyList<TradingGood> catalog, double cycle)
        {
            market.Memory(supplier);
            return TradingRelationships.Tone(supplier) + CoreText(market, supplier, catalog, cycle);
        }
        private static string CoreText(TradingMarket market, TradingSupplier supplier, IReadOnlyList<TradingGood> catalog, double cycle)
        {
            TradingDialogueMemory memory = market.Memory(supplier);
            TradingPersonas.Ensure(supplier);
            TradingPublicPersona persona = supplier.Persona.Public;
            switch (supplier.DialogueNode)
            {
                case "hub": return persona.HubReply;
                case "cargo": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_06BD48A64EB4", TradingText.Argument((object)(StorageCategory(supplier.Specialty)), null, 0), TradingText.Argument((object)(Math.Max(0, supplier.BuyLimit - supplier.Bought)), "0.0", 0), TradingText.Argument((object)(Math.Max(0, supplier.SellLimit - supplier.Sold)), "0.0", 0));
                case "market": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_4F0677357C55");
                case "personal": return persona.Habit + TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.SENTENCE_QUOTE") + persona.Background + TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_50B06EDB1623") + persona.Goal + TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.SENTENCE_CLOSE");
                case "origins":
                    if (supplier.CharacterId == "greyfinch") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_77C41FBE35EA");
                    if (supplier.CharacterId == "oldmi") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CACF2DF2D76C");
                    if (supplier.CharacterId == "lamplow") return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E9C597B194ED");
                    return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_62FB35E6A0F5");
                case "respect": return TradingCharacters.Find(supplier) != null ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_0CBA6D848680") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B3D88D69E37C");
                case "offer": return supplier.DialogueNegotiated ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_16EAEBAA3A8C") :
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_0263DB47DFD0", TradingText.Argument(TradingRelationships.CooperationFactor(supplier, memory) * 100, "0.##"));
                case "deal": return supplier.DialogueNegotiated ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_254E7ED8D3C2", TradingText.Argument((object)(supplier.DialoguePriceFactor * 100), "0.##", 0)) : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_563E9C427519");
                case "batch":
                    TradingGood good = catalog.Where(g => g.Category == supplier.Specialty && supplier.Stock.TryGetValue(g.Id, out double stock) && stock > 0)
                        .OrderByDescending(g => supplier.Stock[g.Id]).ThenBy(g => g.Id, StringComparer.Ordinal).FirstOrDefault();
                    return good == null ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_53E72FB2E66D") :
                        TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_0C46F7A9ABFF", TradingText.Argument((object)(good.Name), null, 0), TradingText.Argument((object)(supplier.Stock[good.Id]), "0.###", 0), TradingText.Argument((object)((good.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7C0025557708") : "kg")), null, 0), TradingText.Argument((object)(TradingMarket.Price(supplier, good, cycle, true)), "0.##", 0));
                case "compare":
                    TradingGood sample = catalog.FirstOrDefault(g => g.Category == supplier.Specialty && supplier.Stock.TryGetValue(g.Id, out double stock) && stock > 0);
                    if (sample == null) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E358576572EF");
                    var others = market.Suppliers.Where(s => s.Id != supplier.Id && s.Departure > cycle && s.Specialty == sample.Category && s.Stock.TryGetValue(sample.Id, out double stock) && stock > 0)
                        .OrderBy(s => TradingMarket.Price(s, sample, cycle, true)).ToList();
                    return others.Count == 0 ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_ED339CF1295B", TradingText.Argument((object)(sample.Name), null, 0)) :
                        TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2BF76BADCBFB", TradingText.Argument((object)(sample.Name), null, 0), TradingText.Argument((object)(TradingMarket.Price(supplier, sample, cycle, true)), "0.##", 0), TradingText.Argument((object)(others[0].Name ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A12EA4EBA6B0")), null, 0), TradingText.Argument((object)(TradingMarket.Price(others[0], sample, cycle, true)), "0.##", 0));
                default: return (memory.Visits > 0 && memory.LastVisitId != supplier.Id ?
                    memory.Trust > 0 ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_AE00B4548D6C") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_3A7F778E26A6") : "") + TradingDialogue.Opening(supplier);
            }
        }

        private static string StorageCategory(string category)
        {
            switch (category) { case "Metal": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_0F5534C945BE"); case "Food": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_AB2775F0D353"); case "Industrial": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6D93EDD1869D");
                case "Gas": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BC4FBDA3A12D"); case "Liquid": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_584EAEFAAE6E"); case "Seed": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5E20FC223736"); case "Equipment": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_144A327039FE"); default: return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_176247C0649D"); }
        }
    }

    internal sealed partial class TradingMarket
    {
        public Dictionary<string, TradingDialogueMemory> DialogueMemories = new Dictionary<string, TradingDialogueMemory>();

        internal TradingDialogueMemory Memory(TradingSupplier supplier)
        {
            DialogueMemories = DialogueMemories ?? new Dictionary<string, TradingDialogueMemory>();
            if (TradingCharacters.Find(supplier) == null) return supplier.Relationship = supplier.Relationship ?? new TradingDialogueMemory();
            if (!DialogueMemories.TryGetValue(supplier.CharacterId, out TradingDialogueMemory memory) || memory == null)
                DialogueMemories[supplier.CharacterId] = memory = supplier.Relationship ?? new TradingDialogueMemory();
            return supplier.Relationship = memory;
        }

        internal bool AdvanceConversation(TradingSupplier supplier, double cycle, int choice, IReadOnlyList<TradingGood> catalog)
        {
            if (!TradingDialogue.Pending(supplier, cycle) || !Suppliers.Contains(supplier)) return false;
            TradingConversations.Upgrade(supplier);
            string[] choices = TradingConversations.Choices(supplier);
            if (choice < 0 || choice >= choices.Length) return false;
            TradingDialogueMemory memory = Memory(supplier);
            supplier.DialogueLines = supplier.DialogueLines ?? new List<TradingDialogueLine>();
            supplier.DialogueTopics = supplier.DialogueTopics ?? new List<string>();
            if (supplier.DialogueLines.Count == 0) supplier.DialogueLines.Add(new TradingDialogueLine { Text = TradingConversations.Text(this, supplier, catalog, cycle) });
            if (memory.LastVisitId != supplier.Id) { memory.Visits++; memory.LastVisitId = supplier.Id; }
            supplier.DialogueLines.Add(new TradingDialogueLine { Player = true, Text = choices[choice] });
            string node = supplier.DialogueNode;
            string next;
            if (choice == choices.Length - 1) { supplier.DialogueStage = 2; next = "end"; }
            else if (node == "cargo") next = choice == 0 ? "batch" : "hub";
            else if (node == "market") next = choice == 0 ? "compare" : "hub";
            else if (node == "personal") next = choice == 0 ? "origins" : choice == 1 ? "respect" : "hub";
            else if (node == "offer")
            {
                next = "deal";
                if (choice == 0 && !supplier.DialogueNegotiated)
                {
                    supplier.DialogueNegotiated = true;
                    supplier.DialoguePriceFactor = TradingRelationships.CooperationFactor(supplier, memory);
                    RecordPrices(cycle, catalog, true);
                }
            }
            else if (node == "greeting" || node == "hub" || string.IsNullOrEmpty(node))
                next = choice == 0 ? "cargo" : choice == 1 ? "market" : choice == 2 ? "personal" : supplier.DialogueNegotiated ? "deal" : "offer";
            else next = "hub";
            if (next == "respect" && !supplier.DialogueTopics.Contains("respect")) memory.Trust = Math.Min(3, memory.Trust + 1);
            if (next == "origins" && !supplier.DialogueTopics.Contains("origins") && TradingCharacters.Find(supplier) != null) memory.Trust = Math.Max(-2, memory.Trust - 1);
            if (!supplier.DialogueTopics.Contains(next)) supplier.DialogueTopics.Add(next);
            supplier.DialogueNode = next;
            supplier.DialogueLines.Add(new TradingDialogueLine { Text = next == "end" ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D5C6936C5958") : TradingConversations.Text(this, supplier, catalog, cycle) });
            if (supplier.DialogueLines.Count > 24) supplier.DialogueLines.RemoveRange(0, supplier.DialogueLines.Count - 24);
            return true;
        }
    }
}
