using System;
using System.Linq;

namespace StorageNetwork.Trading
{
    internal sealed class TradingPersona
    {
        public int Version = 2;
        public string IdentityId;
        public TradingPublicPersona Public;
        public TradingPrivatePersona Private;
    }

    // Only this part is allowed in the player's identity card and tooltips.
    internal sealed class TradingPublicPersona
    {
        public string Temperament, SpeechStyle, Background, Goal, TradeStyle, Habit, Greeting, HubReply;
        public string[] Likes, Dislikes, Boundaries;
    }

    // Author knowledge for a future dialogue provider, never a player-facing description.
    internal sealed class TradingPrivatePersona
    {
        public string Background, Motivation, DisclosureRule;
    }

    internal static class TradingPersonas
    {
        private static readonly string[] Temperaments = { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_024E555A440B"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_213B48F0F738"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_233EBEEC7C12"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_59CEF42F2E52"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BF05F3300826"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FAF6D142E80C"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A3B104845342"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_1A4101991A86") };
        private static readonly string[] Voices = { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DC46311BEEF5"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_977C5DE8E30A"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F7B9F2F226CB"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_78180AB6DF03"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BDAF8E8D597C"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_65902F7B4D6A"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A59F5624A181"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E2935B4C9B8A") };
        private static readonly string[] Greetings = { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_EE6510B61B5F"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_3521E40AF494"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2C492158B178"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_239CF7FF77F8"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B9713D5F8C65"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6557F12E4B05"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DF59DF959679"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_43C77D4105CF") };
        private static readonly string[] HubReplies = { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_139D1520BEF8"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F44D5FADB33A"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E71ECF517B17"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B9E5F7055429"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DF51BEB28A20"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_11583608B093"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A65088932EB0"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F86E6E618B9D") };
        private static readonly string[] Habits = { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C513E9ED18E9"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_02D0627CF9AF"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_ACAE8CD5675C"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F43A63ACA3B4"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_981258BAAACE"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F97A59EA3512"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_686C2F4CC220"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_EB48CD0B293D") };
        private static readonly string[] Origins = { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_81960354F5BE"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_9AFF4758EA04"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_64F3BBBF12A9"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_9906FCC51E85"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_68B3B9276623"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_EF66D8F8A650") };

        internal static bool Ensure(TradingSupplier supplier)
        {
            if (supplier.Persona?.Public != null && supplier.Persona.Private != null) return false;
            TradingCharacter character = TradingCharacters.Find(supplier);
            var random = new Random(character?.AppearanceSeed ?? unchecked(supplier.Id * 15485863 ^ 0x56af));
            int type = random.Next(Temperaments.Length);
            string specialty = Specialty(supplier.Specialty);
            var persona = new TradingPersona
            {
                IdentityId = character?.Id ?? "visitor:" + supplier.Id,
                Public = new TradingPublicPersona
                {
                    Temperament = Temperaments[type], SpeechStyle = Voices[type],
                    Background = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_18D410AF9EDE", TradingText.Argument((object)(Origins[random.Next(Origins.Length)]), null, 0), TradingText.Argument((object)(specialty), null, 0)),
                    Goal = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A2188DD9178F"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_43D7E1DBC1AF"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E069D15B44B7"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_566C483CD373") }[random.Next(4)],
                    TradeStyle = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_302849133797", TradingText.Argument((object)(specialty), null, 0), TradingText.Argument((object)((TradingMarket.Profile(supplier) == "Bulk" ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_EE882050A8F4") : TradingMarket.Profile(supplier) == "Premium" ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E95108929F89") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_4D4D10B89173"))), null, 0)),
                    Habit = Habits[type], Greeting = Greetings[type], HubReply = HubReplies[type],
                    Likes = new[] { new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_8ADD53ACD114"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A91EC0306DCF"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D82896B8E6FF"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_9CDE7DB15FD8"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_287CD15F53BB"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FEC1E30FCC62"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5362B7DB09FB"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_88612A0F490D") }[type], TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_46281B21EAA5") },
                    Dislikes = new[] { new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_62C787ED88DE"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_824B38CF588C"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A8E62CBD14C6"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_127C16127CB0"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DD365C39694E"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_81926FC2B232"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_64DFD010E6DA"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FFDEE4269A62") }[type], TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DF64D9B9DC33") },
                    Boundaries = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CD8320E6E420"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_11D5B7E071BD"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_49A35BC58414") }
                },
                Private = new TradingPrivatePersona
                {
                    Background = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E0291499361D"),
                    Motivation = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5543709CD344"),
                    DisclosureRule = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_1D41AEE9DC0B")
                }
            };
            if (character != null) ConfigureCharacter(persona, character.Id);
            supplier.Persona = persona;
            return true;
        }

        private static void ConfigureCharacter(TradingPersona persona, string id)
        {
            TradingPublicPersona p = persona.Public;
            p.Boundaries = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_44D65DDD7DFD"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DA8F6D2288E6"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_94C1466140F6") };
            if (id == "greyfinch")
            {
                p.Temperament = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_EE4C3BCFFB6B"); p.SpeechStyle = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B9A51435F3F0");
                p.Background = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_3A8B49E554FE");
                p.Goal = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B5F1C275805C"); p.Habit = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D478FF6BE6E9");
                p.TradeStyle = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_24B8915AF7BA");
                p.Likes = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_48FDCF537B69"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_186CB2120F16"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_458F15FAC955") }; p.Dislikes = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_13963A40FD10"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_85D96D356AE6"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F15D5AFE237C") };
                p.Greeting = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F0F881DA4408"); p.HubReply = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_49AEE6C13416");
                persona.Private.Background = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C07479680B59");
                persona.Private.Motivation = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6F025EEA3352");
            }
            else if (id == "oldmi")
            {
                p.Temperament = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_144F21680B93"); p.SpeechStyle = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_9609B2545D4C");
                p.Background = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E530F30AA2F3");
                p.Goal = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_41B8D23C65F8"); p.Habit = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A7E483D8298D");
                p.TradeStyle = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A920F2535DC9");
                p.Likes = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BE8245CF10F9"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_96F81916A514"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B08377C801CE") }; p.Dislikes = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_34FAAFC32255"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_ECC03AADFD87"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_AE3EB2FB4CB8") };
                p.Greeting = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_ED7668E5B38A"); p.HubReply = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FF8217B76AEC");
                persona.Private.Background = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_24EFCAECDB34");
                persona.Private.Motivation = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_686F18C797B6");
            }
            else
            {
                p.Temperament = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A164432E5B8A"); p.SpeechStyle = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_91F4C95C5D2D");
                p.Background = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7B03B9C65A70");
                p.Goal = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7B2432878C57"); p.Habit = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_4A7D1297E15D");
                p.TradeStyle = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7392DCA8CD7D");
                p.Likes = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_4F6AB0E6CA71"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_3483068ADBB0"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_0C865D3E8297") }; p.Dislikes = new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_3832B08D02C6"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_039AB11143AE"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_ABFA9497015A") };
                p.Greeting = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_534359DEFF7F"); p.HubReply = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_82A9CBABB707");
                persona.Private.Background = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2915E346FED3");
                persona.Private.Motivation = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F0414C169A43");
            }
            persona.Private.DisclosureRule = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_3412A30636EC");
        }

        internal static string Specialty(string category)
        {
            switch (category) { case "Metal": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_0F5534C945BE"); case "Food": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_AB2775F0D353"); case "Industrial": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6D93EDD1869D");
                case "Gas": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BC4FBDA3A12D"); case "Liquid": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_584EAEFAAE6E"); case "Seed": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5E20FC223736"); case "Equipment": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_144A327039FE"); default: return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_176247C0649D"); }
        }

        internal static string PublicCard(TradingSupplier supplier)
        {
            Ensure(supplier);
            TradingPublicPersona p = supplier.Persona.Public;
            return $"<b>{supplier.Name ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8A11D7DF26E")} · {Specialty(supplier.Specialty)}</b>\n" +
                TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B2A09DED49FD", TradingText.Argument((object)(p.Temperament), null, 0), TradingText.Argument((object)(p.SpeechStyle), null, 0), TradingText.Argument((object)(p.Background), null, 0)) +
                TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_AF603EFD709E", TradingText.Argument((object)(p.Goal), null, 0), TradingText.Argument((object)(p.TradeStyle), null, 0), TradingText.Argument((object)(p.Habit), null, 0)) +
                TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B50453F5A9B9", TradingText.Argument((object)(string.Join(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.LIST_SEPARATOR"), p.Likes)), null, 0), TradingText.Argument((object)(string.Join(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.LIST_SEPARATOR"), p.Dislikes)), null, 0), TradingText.Argument((object)(string.Join(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.BOUNDARY_SEPARATOR"), p.Boundaries)), null, 0));
        }
    }
}
