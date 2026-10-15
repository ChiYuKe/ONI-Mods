using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    // Pure economy state: no Unity objects, so saving and pricing cannot depend on UI lifetime.
    internal sealed partial class TradingMarket
    {
        public decimal Balance;
        public int NextId = 1;
        public int NextTradeId = 1;
        public int QuoteSignalVersion;
        public List<TradingSupplier> Suppliers = new List<TradingSupplier>();
        public List<TradingReceipt> Receipts = new List<TradingReceipt>();

        public bool Refresh(double cycle, IReadOnlyList<TradingGood> catalog)
        {
            bool changed = PrepareMarket(cycle);
            if (QuoteSignalVersion < 1) { QuoteSignalVersion = 1; changed = true; }
            for (int slot = 0; slot < 5; slot++)
            {
                if (slot < Suppliers.Count && Suppliers[slot].Departure > cycle && !string.IsNullOrEmpty(Suppliers[slot].Specialty))
                {
                    changed |= Restock(Suppliers[slot], cycle, catalog);
                    changed |= TradingDialogue.Assign(Suppliers[slot]);
                    Suppliers[slot].Pressure = Pressure;
                    continue;
                }
                int visitorId = slot < Suppliers.Count && Suppliers[slot].Departure > cycle ? Suppliers[slot].Id : NextId;
                string specialty = ChooseSpecialty(visitorId, catalog);
                if (slot < Suppliers.Count && Suppliers[slot].Departure > cycle)
                {
                    // Upgrade old mixed merchants without resetting their money limits or visit.
                    TradingSupplier existing = Suppliers[slot];
                    existing.Specialty = specialty;
                    existing.Stock.Clear();
                    FillStock(existing, catalog, new Random(existing.Id * 7919));
                    existing.InitialStock = new Dictionary<string, double>(existing.Stock);
                    existing.LastRestockCycle = (int)Math.Floor(cycle);
                    existing.Pressure = Pressure;
                    TradingDialogue.Assign(existing);
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
                    Departure = cycle,
                    BuyLimit = random.Next(20, 81) * 1000m,
                    SellLimit = random.Next(20, 81) * 1000m
                };
                if (UpcomingVisit?.Visitor?.Id == id)
                {
                    supplier = UpcomingVisit.Visitor;
                    UpcomingVisit = null;
                    supplier.Arrival = supplier.Departure = cycle;
                }
                else TradingCharacters.Assign(supplier, Suppliers, cycle);
                TradingRelationships.RollVisit(this, supplier);
                TradingRelationships.Initialize(this, supplier);
                TradingDialogue.Assign(supplier);
                FillStock(supplier, catalog, random);
                supplier.Pressure = Pressure;
                supplier.InitialStock = new Dictionary<string, double>(supplier.Stock);
                supplier.LastRestockCycle = (int)Math.Floor(cycle);
                if (slot < Suppliers.Count) Suppliers[slot] = supplier;
                else Suppliers.Add(supplier);
                changed = true;
            }
            foreach (TradingSupplier supplier in Suppliers) AttachInventory(supplier);
            if (changed) PruneInventoryHistory();
            return RecordPrices(cycle, catalog, changed) || changed;
        }

        private static string ChooseSpecialty(int supplierId, IReadOnlyList<TradingGood> catalog)
        {
            var categories = catalog.Select(g => g.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();
            if (categories.Count == 0) return "Mineral";
            // Each visitor draws independently with replacement. Mix consecutive
            // IDs so seeded Random's first draw does not impose a repeating lineup.
            uint seed = unchecked((uint)supplierId + 0x9e3779b9u);
            seed = unchecked((seed ^ (seed >> 16)) * 0x85ebca6bu);
            seed = unchecked((seed ^ (seed >> 13)) * 0xc2b2ae35u);
            seed ^= seed >> 16;
            return categories[new Random((int)(seed & 0x7fffffffu)).Next(categories.Count)];
        }

        private static void FillStock(TradingSupplier supplier, IReadOnlyList<TradingGood> catalog, Random random)
        {
            List<TradingGood> choices = catalog.Where(g => g.Category == supplier.Specialty).ToList();
            bool character = TradingCharacters.Find(supplier) != null;
            if (character)
                choices = choices.OrderByDescending(g => g.BasePrice).Take(Math.Max(6, (choices.Count + 1) / 2)).ToList();
            else if (Profile(supplier) == "Premium")
                choices = choices.OrderByDescending(g => g.BasePrice).Take(Math.Max(16, (choices.Count + 1) / 2)).ToList();
            else if (Profile(supplier) == "Clearance")
                choices = choices.OrderBy(g => g.BasePrice).Take(Math.Max(16, (choices.Count * 3 + 3) / 4)).ToList();
            for (int i = 0; i < Math.Min(character ? 6 : 12, choices.Count); i++)
            {
                int index = random.Next(i, choices.Count);
                TradingGood good = choices[index];
                choices[index] = choices[i];
                choices[i] = good;
                double quantity = good.ByUnit ? random.Next(5, 31) : random.Next(5, 31) * 100d;
                if (Profile(supplier) == "Bulk") quantity *= 2;
                else if (Profile(supplier) == "Premium") quantity *= 0.5;
                if (character) quantity *= 0.4;
                supplier.Stock[good.Id] = good.ByUnit ? Math.Max(1, Math.Floor(quantity)) : quantity;
            }
        }

        public static decimal Price(TradingSupplier supplier, TradingGood good, double cycle, bool playerBuys)
        {
            decimal marketFactor = TradingQuoteSignals.MarketFactor(good, cycle);
            decimal merchantFactor = (0.90m + (uint)(supplier.Id * 397) % 21 / 100m) * supplier.DialoguePriceFactor;
            decimal buySpread = Profile(supplier) == "Clearance" ? 1.03m : Profile(supplier) == "Premium" ? 1.15m : 1.07m;
            decimal sellSpread = Profile(supplier) == "Bulk" ? 0.95m : 0.89m;
            decimal pressure = supplier.Pressure != null && supplier.Pressure.TryGetValue(good.Id, out decimal value) ? value : 0;
            decimal common = good.BasePrice * marketFactor * merchantFactor * TradingCharacters.PriceFactor(supplier, good) * (1m + pressure) * EventFactor(good.Category, cycle);
            if (supplier.InventoryFactors != null && supplier.InventoryFactors.TryGetValue(good.Id, out decimal inventoryFactor)) common *= inventoryFactor;
            decimal baseAsk = common * buySpread * TradingQuoteSignals.Factor(supplier, good, cycle, true);
            decimal baseBid = common * sellSpread * (Prefers(supplier, good) ? 1.03m : 1m) * TradingQuoteSignals.Factor(supplier, good, cycle, false);
            // Reserve the spread before relationship benefits, so affection can
            // improve both quotes even when independent signals approach each other.
            baseBid = Math.Min(baseBid, baseAsk / 1.10m);
            decimal ask = Math.Max(0.02m, Math.Round(baseAsk * TradingRelationships.BuyFactor(supplier), 2));
            decimal bid = Math.Max(0.01m, Math.Round(baseBid * TradingRelationships.SellFactor(supplier), 2));
            bid = Math.Min(bid, Math.Floor(ask / 1.06m * 100m) / 100m);
            return playerBuys ? ask : Math.Max(0.01m, bid);
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

        public static List<TradingPricePoint> PriceHistory(TradingSupplier supplier, TradingGood good, double cycle)
        {
            var points = new List<TradingPricePoint>();
            if (supplier == null || good == null || double.IsNaN(cycle) || double.IsInfinity(cycle) || cycle < supplier.Arrival) return points;
            if (supplier.History != null && supplier.History.TryGetValue(good.Id, out List<TradingPricePoint> recorded))
                return recorded.Where(p => p.Cycle <= Math.Floor(cycle)).ToList();
            int first = (int)Math.Floor(Math.Max(0, supplier.Arrival));
            // A visitor that departs on a whole cycle has no quote for that cycle.
            int last = (int)Math.Floor(Math.Min(cycle, Math.Ceiling(supplier.Departure) - 1));
            first = Math.Max(first, last - 29);
            for (int day = first; day <= last; day++)
                points.Add(new TradingPricePoint { Cycle = day, Buy = Price(supplier, good, day, true), Sell = Price(supplier, good, day, false) });
            return points;
        }

        public TradingQuote Quote(TradingSupplier supplier, TradingGood good, double cycle, bool buy, double requested, double available)
        {
            var quote = new TradingQuote { SupplierId = supplier?.Id ?? -1, Good = good, Buy = buy, Requested = requested, Cycle = cycle };
            if (supplier == null || supplier.Departure <= cycle || !Suppliers.Contains(supplier)) quote.Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_9541A35779CD");
            else if (supplier.Specialty != good.Category) quote.Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_650AC3BB985E");
            else
            {
                quote.UnitPrice = Price(supplier, good, cycle, buy);
                quote.MaximumAmount = Maximum(supplier, good, cycle, buy, available);
                decimal visitBudget = buy ? supplier.SellLimit - supplier.Sold : supplier.BuyLimit - supplier.Bought;
                if ((double)(Math.Max(0m, visitBudget) / quote.UnitPrice) < available)
                    quote.LimitReason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_8CA5C4CA52A8", TradingText.Argument((object)((buy ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_019B711ECE85") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_36B3764D702D"))), null, 0), TradingText.Argument((object)(Math.Max(0m, visitBudget)), "0.0", 0));
                if (buy && Balance < visitBudget && (double)(Math.Max(0m, Balance) / quote.UnitPrice) < available)
                    quote.LimitReason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FD8C912CBE66", TradingText.Argument((object)(Balance), "0.0", 0));
                if (double.IsNaN(requested) || double.IsInfinity(requested) || requested <= 0) quote.Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_ED1268B4F8DE");
                else if (good.ByUnit && requested != Math.Floor(requested)) quote.Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_81A8777C307B");
                else if (quote.MaximumAmount <= 0)
                    quote.Reason = available <= 0 ? (buy ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6EB4DB9ED927") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CAEFF417B18A")) :
                        buy && Balance < quote.UnitPrice * (good.ByUnit ? 1m : 0.001m) ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F79C07F1F2AB") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_ECCDD2885283");
                else
                {
                    quote.Amount = Math.Min(requested, quote.MaximumAmount);
                    quote.Amount = good.ByUnit ? Math.Floor(quote.Amount) : Math.Floor(quote.Amount * 1000d) / 1000d;
                    quote.Total = quote.UnitPrice * (decimal)quote.Amount;
                    quote.BalanceAfter = buy ? Balance - quote.Total : Balance + quote.Total;
                    if (quote.Amount < requested) quote.Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5E02C8072A13", TradingText.Argument((object)(quote.Amount), "0.###", 0), TradingText.Argument((object)((good.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_421F465DAEE8") : " kg")), null, 0));
                    if (quote.Amount <= 0) quote.Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_9CCE2B30D8B1");
                    else if (quote.Reason == null) quote.Reason = quote.LimitReason;
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
            decimal executionPrice = Price(supplier, good, cycle, playerBuys);
            decimal total = executionPrice * (decimal)amount;
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
                SupplierName = supplier.Name,
                GoodId = good.Id, GoodName = good.Name, ByUnit = good.ByUnit, Cycle = cycle, Buy = playerBuys,
                Amount = amount, UnitPrice = executionPrice, Total = total, BalanceAfter = Balance, WorldId = worldId });
            if (Receipts.Count > 64) Receipts.RemoveRange(0, Receipts.Count - 64);
            TradingRelationships.OnSettled(this, supplier, total);
            LastAmounts[good.Id + (playerBuys ? ":buy" : ":sell")] = amount;
            RecordGood(good, (int)Math.Floor(cycle));
            QuoteVersion++;
            return true;
        }
    }

    internal sealed class TradingPricePoint
    {
        public int Cycle;
        public decimal Buy;
        public decimal Sell;
    }

    internal sealed class TradingSupplier
    {
        public int Id;
        public string Name;
        public string CharacterId;
        public bool DialogueAssigned;
        public string DialogueEventId;
        public int DialogueStage;
        public int DialogueChoice = -1;
        public int DialogueVersion;
        public string DialogueNode;
        public bool DialogueNegotiated;
        public decimal DialoguePriceFactor = 1m;
        public List<TradingDialogueLine> DialogueLines = new List<TradingDialogueLine>();
        public List<string> DialogueTopics = new List<string>();
        public TradingAppearance Appearance;
        public TradingPersona Persona;
        public TradingDialogueMemory Relationship;
        public bool RelationshipBaselineSet;
        public int RelationshipQuotaTier;
        public int VisitMinimumCycles = 1;
        public double BaseDeparture;
        public decimal BaseBuyLimit, BaseSellLimit;
        public string Specialty;
        public double Arrival;
        public double Departure;
        public decimal BuyLimit;
        public decimal SellLimit;
        public decimal Bought;
        public decimal Sold;
        public Dictionary<string, double> Stock = new Dictionary<string, double>();
        public Dictionary<string, double> InitialStock = new Dictionary<string, double>();
        public int LastRestockCycle = -1;
        public Dictionary<string, decimal> Pressure = new Dictionary<string, decimal>();
        [System.Runtime.Serialization.IgnoreDataMember] internal Dictionary<string, decimal> InventoryFactors;
        public Dictionary<string, List<TradingPricePoint>> History = new Dictionary<string, List<TradingPricePoint>>();
        // In native JSON the active history is an alias of the world's canonical history.
        public bool ShouldSerializeHistory() => InventoryFactors == null;
    }

    internal sealed class TradingGood
    {
        public string Id;
        public string Name;
        public string Category;
        public bool ByUnit;
        public decimal BasePrice;
        public string PreferenceTag;
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
        public string LimitReason;
        public double MinimumAmount;
        public bool CanTrade => Amount > 0;
    }

    internal sealed class TradingReceipt
    {
        public int Id;
        public int SupplierId;
        public string SupplierName;
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
        public string Delivery;
        public List<int> StorageIds = new List<int>();
    }
}
