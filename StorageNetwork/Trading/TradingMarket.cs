using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    // Pure economy state: no Unity objects, so saving and pricing cannot depend on UI lifetime.
    internal sealed class TradingMarket
    {
        private static readonly string[] InitialSpecialties = { "Food", "Metal", "Mineral", "Seed", "Equipment" };
        public decimal Balance;
        public int NextId = 1;
        public int NextTradeId = 1;
        public List<TradingSupplier> Suppliers = new List<TradingSupplier>();
        public List<TradingReceipt> Receipts = new List<TradingReceipt>();

        public bool Refresh(double cycle, IReadOnlyList<TradingGood> catalog)
        {
            bool changed = false;
            for (int slot = 0; slot < 5; slot++)
            {
                if (slot < Suppliers.Count && Suppliers[slot].Departure > cycle && !string.IsNullOrEmpty(Suppliers[slot].Specialty))
                    continue;
                string specialty = ChooseSpecialty(slot, catalog);
                if (slot < Suppliers.Count && Suppliers[slot].Departure > cycle)
                {
                    // Upgrade old mixed merchants without resetting their money limits or visit.
                    TradingSupplier existing = Suppliers[slot];
                    existing.Specialty = specialty;
                    existing.Stock.Clear();
                    FillStock(existing, catalog, new Random(existing.Id * 7919));
                    changed = true;
                    continue;
                }
                // Expired visitors are replaced at the current time, including after long absences.
                int id = NextId++;
                Random random = new Random(id * 7919);
                TradingSupplier supplier = new TradingSupplier
                {
                    Id = id,
                    Specialty = specialty,
                    Arrival = cycle,
                    Departure = cycle + random.Next(1, 16),
                    BuyLimit = random.Next(20, 81) * 1000m,
                    SellLimit = random.Next(20, 81) * 1000m
                };
                FillStock(supplier, catalog, random);
                if (slot < Suppliers.Count) Suppliers[slot] = supplier;
                else Suppliers.Add(supplier);
                changed = true;
            }
            return changed;
        }

        private string ChooseSpecialty(int slot, IReadOnlyList<TradingGood> catalog)
        {
            var categories = catalog.Select(g => g.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();
            var used = Suppliers.Where((s, i) => i != slot).Select(s => s.Specialty).ToList();
            var choices = categories.Where(c => !used.Contains(c)).ToList();
            if (choices.Count == 0) choices = categories;
            if (choices.Count == 0) return "Mineral";
            if ((slot >= Suppliers.Count || string.IsNullOrEmpty(Suppliers[slot].Specialty)) && choices.Contains(InitialSpecialties[slot]))
                return InitialSpecialties[slot];
            return choices[new Random(NextId * 3571).Next(choices.Count)];
        }

        private static void FillStock(TradingSupplier supplier, IReadOnlyList<TradingGood> catalog, Random random)
        {
            List<TradingGood> choices = catalog.Where(g => g.Category == supplier.Specialty).ToList();
            if (Profile(supplier) == "Premium")
                choices = choices.OrderByDescending(g => g.BasePrice).Take(Math.Max(12, (choices.Count + 1) / 2)).ToList();
            else if (Profile(supplier) == "Clearance")
                choices = choices.OrderBy(g => g.BasePrice).Take(Math.Max(12, (choices.Count * 3 + 3) / 4)).ToList();
            for (int i = 0; i < Math.Min(12, choices.Count); i++)
            {
                int index = random.Next(i, choices.Count);
                TradingGood good = choices[index];
                choices[index] = choices[i];
                choices[i] = good;
                double quantity = good.ByUnit ? random.Next(5, 31) : random.Next(5, 31) * 100d;
                if (Profile(supplier) == "Bulk") quantity *= 2;
                else if (Profile(supplier) == "Premium") quantity *= 0.5;
                supplier.Stock[good.Id] = good.ByUnit ? Math.Max(1, Math.Floor(quantity)) : quantity;
            }
        }

        public static decimal Price(TradingSupplier supplier, TradingGood good, double cycle, bool playerBuys)
        {
            // Market movement is shared by goods and changes gradually rather than rerolling daily.
            uint hash = 2166136261u;
            foreach (char c in good.Id) hash = unchecked(hash * 31u + c);
            int period = 16 + (int)(hash % 17);
            double position = (Math.Floor(cycle) + hash % (uint)period) % period;
            decimal wave = (decimal)(1d - 4d * Math.Abs(position / period - 0.5d));
            decimal marketFactor = 1m + wave * 0.20m;
            decimal merchantFactor = 0.90m + (uint)(supplier.Id * 397) % 21 / 100m;
            decimal buySpread = Profile(supplier) == "Clearance" ? 1.03m : Profile(supplier) == "Premium" ? 1.15m : 1.07m;
            decimal sellSpread = Profile(supplier) == "Bulk" ? 0.95m : 0.89m;
            return Math.Max(0.01m, Math.Round(good.BasePrice * marketFactor * merchantFactor * (playerBuys ? buySpread : sellSpread), 2));
        }

        public static string Profile(TradingSupplier supplier)
        {
            switch (supplier.Id % 3)
            {
                case 0: return "Bulk";
                case 1: return "Clearance";
                default: return "Premium";
            }
        }

        public TradingQuote Quote(TradingSupplier supplier, TradingGood good, double cycle, bool buy, double requested, double available)
        {
            var quote = new TradingQuote { SupplierId = supplier?.Id ?? -1, Good = good, Buy = buy, Requested = requested, Cycle = cycle };
            if (supplier == null || supplier.Departure <= cycle || !Suppliers.Contains(supplier)) quote.Reason = "供货商已离开，请重新选择。";
            else if (supplier.Specialty != good.Category) quote.Reason = "该供货商不经营此类商品。";
            else
            {
                quote.UnitPrice = Price(supplier, good, cycle, buy);
                quote.MaximumAmount = Maximum(supplier, good, cycle, buy, available);
                if (double.IsNaN(requested) || double.IsInfinity(requested) || requested <= 0) quote.Reason = "请输入有效的交易数量。";
                else if (good.ByUnit && requested != Math.Floor(requested)) quote.Reason = "物品必须按整件交易。";
                else if (quote.MaximumAmount <= 0)
                    quote.Reason = available <= 0 ? (buy ? "供货商无库存，或网络没有可接收容量。" : "网络中没有可出售的库存。") :
                        buy && Balance < quote.UnitPrice * (good.ByUnit ? 1m : 0.001m) ? "货币余额不足。" : "供货商本次停留的交易额度已耗尽。";
                else
                {
                    quote.Amount = Math.Min(requested, quote.MaximumAmount);
                    quote.Amount = good.ByUnit ? Math.Floor(quote.Amount) : Math.Floor(quote.Amount * 1000d) / 1000d;
                    quote.Total = quote.UnitPrice * (decimal)quote.Amount;
                    quote.BalanceAfter = buy ? Balance - quote.Total : Balance + quote.Total;
                    if (quote.Amount < requested) quote.Reason = $"受库存、余额、容量或额度限制，本次最多成交 {quote.Amount:0.###}{(good.ByUnit ? " 件" : " kg")}。";
                    if (quote.Amount <= 0) quote.Reason = "交易数量小于最小成交量。";
                }
            }
            return quote;
        }

        public double Maximum(TradingSupplier supplier, TradingGood good, double cycle, bool playerBuys, double available)
        {
            if (supplier == null || supplier.Departure <= cycle || !Suppliers.Contains(supplier) || good.Category != supplier.Specialty ||
                double.IsNaN(available) || available <= 0) return 0;
            decimal price = Price(supplier, good, cycle, playerBuys);
            decimal budget = playerBuys ? Math.Min(Balance, supplier.SellLimit - supplier.Sold) : supplier.BuyLimit - supplier.Bought;
            double amount = Math.Min(available, (double)(Math.Max(0m, budget) / price));
            if (playerBuys)
                amount = Math.Min(amount, supplier.Stock.TryGetValue(good.Id, out double stock) ? stock : 0);
            return good.ByUnit ? Math.Floor(amount) : Math.Floor(amount * 1000d) / 1000d;
        }

        public bool Settle(TradingSupplier supplier, TradingGood good, double cycle, bool playerBuys, double amount, int worldId = -1)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0 ||
                (good.ByUnit && amount != Math.Floor(amount)) ||
                amount > Maximum(supplier, good, cycle, playerBuys, amount) + 0.000001d) return false;
            decimal total = Price(supplier, good, cycle, playerBuys) * (decimal)amount;
            if (playerBuys ? total > Balance || total > supplier.SellLimit - supplier.Sold : total > supplier.BuyLimit - supplier.Bought)
                return false;
            if (playerBuys)
            {
                Balance -= total;
                supplier.Sold += total;
                supplier.Stock[good.Id] -= amount;
            }
            else
            {
                Balance += total;
                supplier.Bought += total;
                supplier.Stock.TryGetValue(good.Id, out double stock);
                supplier.Stock[good.Id] = stock + amount;
            }
            Receipts.Add(new TradingReceipt { Id = NextTradeId++, SupplierId = supplier.Id, Specialty = supplier.Specialty,
                GoodId = good.Id, GoodName = good.Name, ByUnit = good.ByUnit, Cycle = cycle, Buy = playerBuys,
                Amount = amount, UnitPrice = Price(supplier, good, cycle, playerBuys), Total = total, BalanceAfter = Balance, WorldId = worldId });
            if (Receipts.Count > 64) Receipts.RemoveRange(0, Receipts.Count - 64);
            return true;
        }
    }

    internal sealed class TradingSupplier
    {
        public int Id;
        public string Specialty;
        public double Arrival;
        public double Departure;
        public decimal BuyLimit;
        public decimal SellLimit;
        public decimal Bought;
        public decimal Sold;
        public Dictionary<string, double> Stock = new Dictionary<string, double>();
    }

    internal sealed class TradingGood
    {
        public string Id;
        public string Name;
        public string Category;
        public bool ByUnit;
        public decimal BasePrice;
    }

    internal sealed class TradingQuote
    {
        public int SupplierId;
        public TradingGood Good;
        public bool Buy;
        public double Cycle;
        public double Requested;
        public double MaximumAmount;
        public double Amount;
        public decimal UnitPrice;
        public decimal Total;
        public decimal BalanceAfter;
        public string Reason;
        public bool CanTrade => Amount > 0;
    }

    internal sealed class TradingReceipt
    {
        public int Id;
        public int SupplierId;
        public string Specialty;
        public string GoodId;
        public string GoodName;
        public bool ByUnit;
        public double Cycle;
        public bool Buy;
        public double Amount;
        public decimal UnitPrice;
        public decimal Total;
        public decimal BalanceAfter;
        public int WorldId;
    }
}
