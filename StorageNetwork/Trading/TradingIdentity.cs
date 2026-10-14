using System;
using System.Collections.Generic;

namespace StorageNetwork.Trading
{
    // Save resource IDs, never Unity objects. This identity can also drive dialogue portraits.
    internal sealed class TradingAppearance
    {
        public string BasePersonalityId;
        public string HeadShape;
        public string Hair;
        public string Eyes;
        public string Mouth;
    }

    internal sealed class TradingIdentityTemplate
    {
        public string PersonalityId;
        public string Name;
        public string HeadShape, Hair, Eyes, Mouth;
    }

    internal static class TradingIdentity
    {
        internal static bool Ensure(TradingSupplier supplier, IReadOnlyList<TradingIdentityTemplate> templates, IReadOnlyList<TradingSupplier> visitors = null)
        {
            if (templates == null || templates.Count == 0 || (!string.IsNullOrEmpty(supplier.Name) && supplier.Appearance != null)) return false;
            TradingCharacter character = TradingCharacters.Find(supplier);
            var random = new Random(character?.AppearanceSeed ?? unchecked(supplier.Id * 104729 ^ 0x51b39f));
            TradingIdentityTemplate head = templates[random.Next(templates.Count)];
            if (character == null && string.IsNullOrEmpty(supplier.Name) && visitors != null)
            {
                var occupied = new HashSet<string>();
                foreach (var other in visitors)
                    if (other != supplier && other.Appearance != null)
                    {
                        occupied.Add(TradingText.Render(other.Name));
                        occupied.Add(other.Appearance.BasePersonalityId ?? "");
                    }
                int start = 0;
                for (int i = 0; i < templates.Count; i++) if (templates[i] == head) { start = i; break; }
                for (int i = 0; i < templates.Count; i++)
                {
                    var candidate = templates[(start + i) % templates.Count];
                    if (occupied.Contains(candidate.Name) || occupied.Contains(candidate.PersonalityId)) continue;
                    head = candidate;
                    break;
                }
            }
            if (string.IsNullOrEmpty(supplier.Name)) supplier.Name = character?.Name ?? head.Name;
            if (supplier.Appearance == null)
                supplier.Appearance = new TradingAppearance
                {
                    BasePersonalityId = head.PersonalityId,
                    HeadShape = head.HeadShape,
                    Hair = templates[random.Next(templates.Count)].Hair,
                    Eyes = templates[random.Next(templates.Count)].Eyes,
                    Mouth = templates[random.Next(templates.Count)].Mouth
                };
            return true;
        }
    }
}
