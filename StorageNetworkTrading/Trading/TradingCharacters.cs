using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    // Stable narrative identities are separate from the IDs of individual visits.
    internal sealed class TradingCharacter
    {
        public string Id, Name, Specialty, Description;
        public int AppearanceSeed;
        public decimal PriceFactor;
    }

    internal static class TradingCharacters
    {
        internal static readonly TradingCharacter[] Roster =
        {
            new TradingCharacter { Id = "greyfinch", Name = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_9A0978CADA18"), Specialty = "Metal", AppearanceSeed = 73013, PriceFactor = 0.78m,
                Description = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7FC1636FB23A") },
            new TradingCharacter { Id = "oldmi", Name = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E2F733CE5B02"), Specialty = "Industrial", AppearanceSeed = 91337, PriceFactor = 0.84m,
                Description = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FAC4810B54C7") },
            new TradingCharacter { Id = "lamplow", Name = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D0343657F859"), Specialty = "Food", AppearanceSeed = 57149, PriceFactor = 0.92m,
                Description = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F3BC96B5F906") }
        };

        internal static TradingCharacter Find(TradingSupplier supplier) =>
            supplier == null ? null : Roster.FirstOrDefault(c => c.Id == supplier.CharacterId);

        internal static string Description(TradingSupplier supplier) => Find(supplier)?.Description;

        internal static void Assign(TradingSupplier supplier, IEnumerable<TradingSupplier> visitors, double cycle)
        {
            var random = new Random(unchecked(supplier.Id * 65537 ^ 0x26f73));
            // Draw only on arrival. Ordinary categories continue to be sampled with replacement.
            if (random.Next(100) >= 18) return;
            TradingCharacter character = Roster.FirstOrDefault(c => c.Specialty == supplier.Specialty &&
                !visitors.Any(s => s.Departure > cycle && s.CharacterId == c.Id));
            if (character == null) return;
            supplier.CharacterId = character.Id;
            supplier.Name = character.Name;
            supplier.BuyLimit = random.Next(18, 41) * 1000m;
            supplier.SellLimit = random.Next(25, 61) * 1000m;
        }

        internal static decimal PriceFactor(TradingSupplier supplier, TradingGood good)
        {
            TradingCharacter character = Find(supplier);
            if (character == null) return 1m;
            // Batch-specific discounts and premiums, shared by bid and ask to retain the spread.
            uint hash = (uint)character.AppearanceSeed;
            foreach (char c in good.Id) hash = unchecked(hash * 31 + c);
            return character.PriceFactor * (0.88m + hash % 33 / 100m);
        }
    }

    internal sealed partial class TradingMarket
    {
        public Dictionary<string, TradingAppearance> CharacterAppearances = new Dictionary<string, TradingAppearance>();
        public Dictionary<string, TradingPersona> CharacterPersonas = new Dictionary<string, TradingPersona>();

        internal bool EnsurePersona(TradingSupplier supplier)
        {
            CharacterPersonas = CharacterPersonas ?? new Dictionary<string, TradingPersona>();
            TradingCharacter character = TradingCharacters.Find(supplier);
            bool changed = false;
            if (character != null && supplier.Persona == null && CharacterPersonas.TryGetValue(character.Id, out TradingPersona saved) &&
                saved?.Public != null && saved.Private != null)
            {
                supplier.Persona = saved;
                changed = true;
            }
            changed |= TradingPersonas.Ensure(supplier);
            if (character != null && (!CharacterPersonas.TryGetValue(character.Id, out TradingPersona cached) || cached == null))
            {
                CharacterPersonas[character.Id] = supplier.Persona;
                changed = true;
            }
            return changed;
        }

        internal bool EnsureIdentity(TradingSupplier supplier, IReadOnlyList<TradingIdentityTemplate> templates)
        {
            CharacterAppearances = CharacterAppearances ?? new Dictionary<string, TradingAppearance>();
            TradingCharacter character = TradingCharacters.Find(supplier);
            bool changed = false;
            if (character != null && supplier.Appearance == null &&
                CharacterAppearances.TryGetValue(character.Id, out TradingAppearance saved) && saved != null)
            {
                supplier.Appearance = saved;
                changed = true;
            }
            changed |= TradingIdentity.Ensure(supplier, templates, Suppliers);
            if (character != null && supplier.Appearance != null &&
                (!CharacterAppearances.TryGetValue(character.Id, out TradingAppearance cached) || cached == null))
            {
                CharacterAppearances[character.Id] = supplier.Appearance;
                changed = true;
            }
            return changed;
        }
    }
}
