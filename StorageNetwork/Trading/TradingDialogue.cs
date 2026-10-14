using System;

namespace StorageNetwork.Trading
{
    // Narrative state belongs to a visit, not to the lifetime of a window.
    internal static class TradingDialogue
    {
        internal static bool Assign(TradingSupplier supplier)
        {
            if (supplier.DialogueAssigned) return false;
            supplier.DialogueAssigned = true;
            TradingCharacter character = TradingCharacters.Find(supplier);
            if (character != null) supplier.DialogueEventId = character.Id;
            else if (new Random(unchecked(supplier.Id * 32452843 ^ 0x6741)).Next(100) < 30)
                supplier.DialogueEventId = supplier.Id % 2 == 0 ? "cargo" : "route";
            return true;
        }

        internal static bool Pending(TradingSupplier supplier, double cycle) =>
            supplier != null && supplier.Departure > cycle && !string.IsNullOrEmpty(supplier.DialogueEventId) &&
            supplier.DialogueStage < 2;

        internal static string Opening(TradingSupplier supplier)
        {
            TradingPersonas.Ensure(supplier);
            if (TradingCharacters.Find(supplier) == null)
                return supplier.Persona.Public.Greeting + "\n" + (supplier.DialogueEventId == "cargo" ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_28EE5AF3A556") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_614DD88225F4"));
            switch (supplier.DialogueEventId)
            {
                case "greyfinch": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_4BA3D0B086B4");
                case "oldmi": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_81E7B99834F2");
                case "lamplow": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BB4FE277C2F8");
                case "cargo": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_31250C25328E");
                default: return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CF8C28A3FA20");
            }
        }

        internal static string[] Choices(TradingSupplier supplier) =>
            supplier.DialogueEventId == "cargo" ? new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DE703B406DDF"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6ACE7E8EAE48") } :
            supplier.DialogueEventId == "route" ? new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2CD36E726201"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B3B517B1ADD1") } :
            new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D0008F5C35E4"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_AAC6E4A3A642") };

        internal static string Reply(TradingSupplier supplier)
        {
            bool first = supplier.DialogueChoice == 0;
            switch (supplier.DialogueEventId)
            {
                case "greyfinch": return first ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2D3FD4069A35") :
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_95010B7859F1");
                case "oldmi": return first ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_0F1C7A97F325") :
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_493FD77935B2");
                case "lamplow": return first ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_49A4BA2C7212") :
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F04710FC0460");
                case "cargo": return first ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_45652C25CC3A") :
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_3215D059738C");
                default: return first ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FE0AB9C57403") :
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FFA946E89B92", TradingText.Argument((object)(supplier.Departure - supplier.Arrival), "0.#", 0));
            }
        }

        internal static bool Advance(TradingSupplier supplier, double cycle, int choice)
        {
            if (!Pending(supplier, cycle)) return false;
            if (supplier.DialogueStage == 0)
            {
                if (choice < 0 || choice >= Choices(supplier).Length) return false;
                supplier.DialogueChoice = choice;
                supplier.DialogueStage = 1;
            }
            else
            {
                if (choice != 0) return false;
                supplier.DialogueStage = 2;
            }
            return true;
        }
    }
}
