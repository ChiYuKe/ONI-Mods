using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading;

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

	public TradingConversationState ConversationState = new TradingConversationState();

	internal List<int> Route(string text, IReadOnlyList<TradingSupplier> suppliers, double cycle, int probability, Func<double> random, IReadOnlyList<TradingGood> goods = null)
	{
		LatestQuestion = text;
		AddressedSuppliers = new List<int>();
		if (text.Contains("@"))
		{
			AddressedSuppliers = Responders(text, suppliers, cycle, probability, random);
			RoutingMode = "mention";
			FocusSupplierId = ((AddressedSuppliers.Count == 1) ? AddressedSuppliers[0] : (-1));
			TopicAnchor = text;
			return AddressedSuppliers.ToList();
		}
		List<int> list = (from s in suppliers
			where s.Departure > cycle && NaturallyAddressed(text, TradingText.Render(s.Name), s.Id)
			select s.Id).ToList();
		if (list.Count > 0)
		{
			RoutingMode = ((list.Count > 1) ? "ambiguous_name" : "name");
			FocusSupplierId = ((list.Count == 1) ? list[0] : (-1));
			TopicAnchor = text;
			AddressedSuppliers = list;
			return (list.Count == 1) ? list : new List<int>();
		}
		if (FocusSupplierId >= 0 && suppliers.Any((TradingSupplier s) => s.Id == FocusSupplierId && s.Departure > cycle) && (IsFollowUp(text) || TradingConversationState.IsShortSelection(text, goods)))
		{
			RoutingMode = "follow_up";
			AddressedSuppliers.Add(FocusSupplierId);
			List<int> list2 = Responders(text, suppliers, cycle, probability, random);
			list2.Remove(FocusSupplierId);
			list2.Insert(0, FocusSupplierId);
			return list2;
		}
		FocusSupplierId = -1;
		TopicAnchor = text;
		RoutingMode = "group";
		return Responders(text, suppliers, cycle, probability, random);
	}

	private static bool NaturallyAddressed(string text, string name, int id)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		text = text.Trim();
		string mention = name + "#" + id;
		if (ContainsMention(text, mention))
		{
			return true;
		}
		if (text.StartsWith(name + "#", StringComparison.Ordinal))
		{
			return false;
		}
		return text.StartsWith(name, StringComparison.Ordinal) || text.StartsWith("我想问" + name, StringComparison.Ordinal) || text.StartsWith("问一下" + name, StringComparison.Ordinal) || text.StartsWith("Hey " + name, StringComparison.OrdinalIgnoreCase) || text.StartsWith("Hi " + name, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsFollowUp(string text)
	{
		if (text.Length > 160)
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		if (new string[7] { "大家", "各位", "谁", "有没有人", "everyone", "anyone", "who " }.Any(text2.StartsWith))
		{
			return false;
		}
		string[] source = new string[25]
		{
			"便宜", "打折", "优惠", "多少钱", "多少", "价格", "报价", "能不能", "可以吗", "能吗",
			"那就", "这个", "这种", "刚才", "为什么", "你说", "继续", "cheaper", "discount", "how much",
			"that one", "your price", "can you", "why", "go on"
		};
		return source.Any(text2.Contains);
	}

	internal void Add(TradingGroupMessage message)
	{
		Messages.Add(message);
		while (Messages.Count > 128)
		{
			TradingGroupMessage tradingGroupMessage = Messages[0];
			EarlierContext = EarlierContext + "\n" + (tradingGroupMessage.Player ? "PLAYER" : (tradingGroupMessage.Name + "#" + tradingGroupMessage.SupplierId)) + ": " + tradingGroupMessage.Text;
			Messages.RemoveAt(0);
		}
		if (EarlierContext.Length > 12000)
		{
			EarlierContext = EarlierContext.Substring(EarlierContext.Length - 12000);
		}
		Version++;
	}

	internal static List<int> Responders(string text, IReadOnlyList<TradingSupplier> suppliers, double cycle, int probability, Func<double> random)
	{
		List<TradingSupplier> list = suppliers.Where((TradingSupplier s) => s.Departure > cycle).ToList();
		List<int> list2 = new List<int>();
		foreach (TradingSupplier item in list)
		{
			string text2 = TradingText.Render(item.Name);
			string mention = "@" + text2 + "#" + item.Id;
			if (ContainsMention(text, mention) || ContainsMention(text, "@#" + item.Id) || ContainsMention(text, "@" + text2))
			{
				list2.Add(item.Id);
			}
		}
		if (text.Contains("@"))
		{
			return list2;
		}
		probability = Math.Max(0, Math.Min(100, probability));
		return (from s in list
			where probability == 100 || (probability > 0 && random() < (double)probability / 100.0)
			select s.Id).ToList();
	}

	private static bool ContainsMention(string text, string mention)
	{
		int startIndex = 0;
		while ((startIndex = text.IndexOf(mention, startIndex, StringComparison.Ordinal)) >= 0)
		{
			int num = startIndex + mention.Length;
			if (num == text.Length || char.IsWhiteSpace(text[num]) || ",，:：!！?？。".IndexOf(text[num]) >= 0)
			{
				return true;
			}
			startIndex = num;
		}
		return false;
	}
}
