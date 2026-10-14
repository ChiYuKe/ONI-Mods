using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    internal sealed class TradingGroupMessage
    {
        public int SupplierId;
        public string Name, Text, Specialty;
        public bool Player;
        public TradingAppearance Appearance;
        public double Cycle;
    }

    internal sealed class TradingGroupChat
    {
        public List<TradingGroupMessage> Messages = new List<TradingGroupMessage>();
        public string EarlierContext = "";
        public int Version;
        public int FocusSupplierId = -1;
        public string TopicAnchor = "";
        public string LatestQuestion = "";
        public string RoutingMode = "group";
        public List<int> AddressedSuppliers = new List<int>();

        internal List<int> Route(string text, IReadOnlyList<TradingSupplier> suppliers, double cycle, int probability, Func<double> random)
        {
            LatestQuestion = text;
            AddressedSuppliers = new List<int>();
            if (text.Contains("@"))
            {
                AddressedSuppliers = Responders(text, suppliers, cycle, probability, random);
                RoutingMode = "mention";
                FocusSupplierId = AddressedSuppliers.Count == 1 ? AddressedSuppliers[0] : -1;
                TopicAnchor = text;
                return AddressedSuppliers.ToList();
            }
            var named = suppliers.Where(s => s.Departure > cycle && NaturallyAddressed(text, TradingText.Render(s.Name), s.Id)).Select(s => s.Id).ToList();
            if (named.Count > 0)
            {
                RoutingMode = named.Count > 1 ? "ambiguous_name" : "name";
                FocusSupplierId = named.Count == 1 ? named[0] : -1;
                TopicAnchor = text;
                AddressedSuppliers = named;
                return named.Count == 1 ? named : new List<int>();
            }
            if (FocusSupplierId >= 0 && suppliers.Any(s => s.Id == FocusSupplierId && s.Departure > cycle) && IsFollowUp(text))
            {
                RoutingMode = "follow_up";
                AddressedSuppliers.Add(FocusSupplierId);
                var participants = Responders(text, suppliers, cycle, probability, random);
                participants.Remove(FocusSupplierId);
                participants.Insert(0, FocusSupplierId);
                return participants;
            }
            FocusSupplierId = -1;
            TopicAnchor = text;
            RoutingMode = "group";
            return Responders(text, suppliers, cycle, probability, random);
        }

        private static bool NaturallyAddressed(string text, string name, int id)
        {
            if (string.IsNullOrEmpty(name)) return false;
            text = text.Trim();
            string exact = name + "#" + id;
            if (ContainsMention(text, exact)) return true;
            // A different visit suffix must never be treated as a bare-name match.
            if (text.StartsWith(name + "#", StringComparison.Ordinal)) return false;
            return text.StartsWith(name, StringComparison.Ordinal) || text.StartsWith("我想问" + name, StringComparison.Ordinal) ||
                text.StartsWith("问一下" + name, StringComparison.Ordinal) || text.StartsWith("Hey " + name, StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("Hi " + name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFollowUp(string text)
        {
            if (text.Length > 160) return false;
            string lower = text.ToLowerInvariant();
            if (new[] { "大家", "各位", "谁", "有没有人", "everyone", "anyone", "who " }.Any(lower.StartsWith)) return false;
            string[] phrases = { "便宜", "打折", "优惠", "多少钱", "多少", "价格", "报价", "能不能", "可以吗", "能吗", "那就", "这个", "这种", "刚才", "为什么", "你说", "继续", "cheaper", "discount", "how much", "that one", "your price", "can you", "why", "go on" };
            return phrases.Any(lower.Contains);
        }
        internal void Add(TradingGroupMessage message)
        {
            Messages.Add(message);
            // Bound save and prompt growth while retaining an archive for continuity.
            while (Messages.Count > 128)
            {
                var old = Messages[0];
                EarlierContext += "\n" + (old.Player ? "PLAYER" : old.Name + "#" + old.SupplierId) + ": " + old.Text;
                Messages.RemoveAt(0);
            }
            if (EarlierContext.Length > 12000) EarlierContext = EarlierContext.Substring(EarlierContext.Length - 12000);
            Version++;
        }

        internal static List<int> Responders(string text, IReadOnlyList<TradingSupplier> suppliers, double cycle, int probability, Func<double> random)
        {
            var active = suppliers.Where(s => s.Departure > cycle).ToList();
            var mentioned = new List<int>();
            foreach (var supplier in active)
            {
                string name = TradingText.Render(supplier.Name);
                string exact = "@" + name + "#" + supplier.Id;
                if (ContainsMention(text, exact) || ContainsMention(text, "@#" + supplier.Id) || ContainsMention(text, "@" + name)) mentioned.Add(supplier.Id);
            }
            if (text.Contains("@")) return mentioned;
            probability = Math.Max(0, Math.Min(100, probability));
            return active.Where(s => probability == 100 || (probability > 0 && random() < probability / 100d)).Select(s => s.Id).ToList();
        }
        private static bool ContainsMention(string text, string mention)
        {
            int start = 0;
            while ((start = text.IndexOf(mention, start, StringComparison.Ordinal)) >= 0)
            {
                int end = start + mention.Length;
                if (end == text.Length || char.IsWhiteSpace(text[end]) || ",，:：!！?？。".IndexOf(text[end]) >= 0) return true;
                start = end;
            }
            return false;
        }
    }

    internal sealed partial class TradingMarket
    {
        public TradingGroupChat GroupChat = new TradingGroupChat();
    }
}
