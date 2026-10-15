using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace StorageNetwork.Trading;

internal sealed class TradingConversationState
{
	public int SupplierId = -1;

	public string GoodId;

	public string GoodName;

	public double? Quantity;

	public string Intent = "small_talk";

	public int Turns;

	internal void Observe(string text, int supplierId, IReadOnlyList<TradingGood> goods)
	{
		if (supplierId != SupplierId)
		{
			GoodId = (GoodName = null);
			Quantity = null;
			Turns = 0;
		}
		SupplierId = supplierId;
		Turns++;
		TradingGood tradingGood = FindGood(text, goods);
		if (tradingGood != null)
		{
			if (GoodId != tradingGood.Id)
			{
				Quantity = null;
			}
			GoodId = tradingGood.Id;
			GoodName = tradingGood.Name;
		}
		string text2 = text.ToLowerInvariant();
		Match match = Regex.Match(text2, "(?<![\\d.])(\\d+(?:\\.\\d+)?)\\s*(?:kg\\b|千克|公斤|件|个)");
		if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result > 0.0)
		{
			Quantity = result;
		}
		if (Has(text2, "玩过", "最近怎么样", "喜欢什么", "旅程", "原神", "play", "how are you", "favorite", "journey"))
		{
			Intent = "small_talk";
		}
		else if (Has(text2, "送我", "免费", "白送", "赠", "give me", "for free", "freebie"))
		{
			Intent = "gift_request";
		}
		else if (Has(text2, "便宜", "打折", "优惠", "讲价", "砍价", "cheaper", "discount", "bargain"))
		{
			Intent = "bargain";
		}
		else if (Has(text2, "多少", "价格", "报价", "how much", "price", "quote"))
		{
			Intent = "quote";
		}
		else if (Has(text2, "其他", "还有什么", "有什么", "货单", "other", "what do you sell", "catalog"))
		{
			Intent = "browse_goods";
		}
		else if (Has(text2, "有没有", "有卖", "库存", "现货", "do you have", "in stock", "available"))
		{
			Intent = "availability";
		}
		else if (tradingGood != null && text2.Length < 80)
		{
			Intent = "select_good";
		}
		else if (Has(text2, "买", "卖", "要", "buy", "sell", "take", "order") && GoodId != null)
		{
			Intent = "trade_interest";
		}
		else
		{
			Intent = "small_talk";
		}
	}

	internal static bool IsShortSelection(string text, IReadOnlyList<TradingGood> goods)
	{
		return text.Length <= 80 && (FindGood(text, goods) != null || Has(text.ToLowerInvariant(), "其他液体", "还有别的", "送我", "免费", "give me", "for free", "other liquids", "anything else") || Regex.IsMatch(text, "\\d+\\s*(?:kg|千克|公斤|件|个)", RegexOptions.IgnoreCase));
	}

	internal static TradingGood FindGood(string text, IReadOnlyList<TradingGood> goods)
	{
		if (goods == null)
		{
			return null;
		}
		string lower = text.ToLowerInvariant();
		TradingGood tradingGood = (from g in goods
			where !string.IsNullOrEmpty(g.Name) && lower.Contains(TradingText.Render(g.Name).ToLowerInvariant())
			orderby g.Name.Length descending
			select g).FirstOrDefault();
		if (tradingGood != null)
		{
			return tradingGood;
		}
		if (Has(lower, "原油", "crude oil"))
		{
			return goods.FirstOrDefault((TradingGood g) => g.Id == "CrudeOil");
		}
		if (Has(lower, "石油", "petroleum", "oil"))
		{
			return goods.FirstOrDefault((TradingGood g) => g.Id == "Petroleum");
		}
		return null;
	}

	private static bool Has(string text, params string[] words)
	{
		return words.Any(text.Contains);
	}
}
