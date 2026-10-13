using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using KSerialization;
using Newtonsoft.Json;
using StorageNetwork.Services;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using UnityEngine;

namespace StorageNetwork.Trading
{
    [SerializationConfig(KSerialization.MemberSerialization.OptIn)]
    internal sealed class StorageNetworkTradingState : KMonoBehaviour, ISim1000ms
    {
        [Serialize] private string marketJson;
        private TradingMarket market;
        private List<TradingGood> catalog;
        internal static StorageNetworkTradingState Instance;
        internal TradingMarket Market => market;
        internal IReadOnlyList<TradingGood> Catalog => catalog;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Instance = this;
            catalog = BuildCatalog();
            try { market = string.IsNullOrEmpty(marketJson) ? new TradingMarket() : JsonConvert.DeserializeObject<TradingMarket>(marketJson); }
            catch (Exception ex) { Debug.LogWarning("[StorageNetwork] Cannot restore trading state: " + ex); }
            market = market ?? new TradingMarket();
            Refresh();
        }

        protected override void OnCleanUp()
        {
            if (Instance == this) Instance = null;
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt) { Refresh(); }
        internal void Refresh()
        {
            if (market != null && market.Refresh(Cycle, catalog)) Persist();
        }
        private void Persist() { marketJson = JsonConvert.SerializeObject(market); }
        internal static double Cycle => GameClock.Instance != null ? GameClock.Instance.GetTimeInCycles() : 0;

        private static List<TradingGood> BuildCatalog()
        {
            List<TradingGood> goods = new List<TradingGood>();
            foreach (Element element in ElementLoader.elements)
            {
                if ((!element.IsSolid && !element.IsLiquid && !element.IsGas) ||
                    element.id == SimHashes.Unobtanium || Assets.GetPrefab(element.tag) == null) continue;
                string id = element.tag.Name;
                GameObject prefab = Assets.GetPrefab(element.tag);
                decimal price = MaterialBasePrice(id, GetCategory(prefab, element));
                goods.Add(new TradingGood { Id = id, Name = element.name, BasePrice = price, Category = GetCategory(prefab, element) });
            }
            var known = new HashSet<string>();
            foreach (TradingGood good in goods) known.Add(good.Id);
            foreach (GameObject prefab in Assets.GetPrefabsWithComponent<Pickupable>())
            {
                if (prefab == null || prefab.GetComponent<PrimaryElement>() == null || prefab.GetComponent<KPrefabID>() == null) continue;
                string id = prefab.GetComponent<KPrefabID>().PrefabTag.Name;
                if (known.Contains(id)) continue;
                string category = GetCategory(prefab, null);
                if (category == null) continue;
                known.Add(id);
                decimal price = category == "Equipment" ? (prefab.HasTag(GameTags.Suit) ? 1500m : 250m) : category == "Seed" ? 100m : 30m;
                Edible edible = prefab.GetComponent<Edible>();
                if (edible?.FoodInfo != null)
                    price = Math.Max(5m, (decimal)(edible.FoodInfo.CaloriesPerUnit / 1000000f) * (12 + Math.Max(-2, edible.FoodInfo.Quality) * 4));
                goods.Add(new TradingGood { Id = id, Name = prefab.GetProperName(), ByUnit = true, Category = category, BasePrice = price });
            }
            goods.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCulture));
            return goods;
        }

        private static decimal MaterialBasePrice(string id, string category)
        {
            switch (id)
            {
                case "Thermium": return 180m;
                case "Fullerene": return 150m;
                case "SuperCoolant": case "EnrichedUranium": return 120m;
                case "Niobium": return 90m;
                case "Isoresin": return 80m;
                case "ViscoGel": return 75m;
                case "Insulation": return 60m;
                case "Diamond": return 40m;
                case "Steel": return 28m;
                case "Tungsten": return 20m;
                case "Gold": case "Ceramic": return 18m;
                case "Plastic": return 14m;
                case "Glass": return 12m;
                case "Aluminum": case "Cobalt": return 10m;
                case "Iron": case "Copper": case "RefinedCarbon": case "UraniumOre": return 8m;
                case "Lead": return 6m;
                case "Petroleum": case "TungstenOre": return 5m;
                case "GoldAmalgam": case "Ethanol": return 4m;
                case "CrudeOil": case "AluminumOre": case "Methane": return 3m;
                case "Water": return 1m;
                case "Brine": case "SaltWater": return 0.8m;
                case "DirtyWater": return 0.6m;
                case "CarbonDioxide": return 0.25m;
                default: return category == "Metal" ? 3m : category == "Industrial" ? 8m : category == "Liquid" ? 2m : 1m;
            }
        }

        private static string GetCategory(GameObject prefab, Element element)
        {
            // Priority makes categories disjoint even when the game assigns several tags.
            if (prefab.HasTag(GameTags.Edible) || prefab.HasTag(GameTags.CookingIngredient)) return "Food";
            if (prefab.HasTag(GameTags.Seed) || prefab.HasTag(GameTags.CropSeed)) return "Seed";
            if (prefab.HasTag(GameTags.Suit) || prefab.HasTag(GameTags.Clothes)) return "Equipment";
            if (prefab.HasTag(GameTags.Metal) || prefab.HasTag(GameTags.RefinedMetal) ||
                (element != null && (element.HasTag(GameTags.Metal) || element.HasTag(GameTags.RefinedMetal)))) return "Metal";
            if (prefab.HasTag(GameTags.Liquid) || (element != null && element.IsLiquid)) return "Liquid";
            if (prefab.HasTag(GameTags.Gas) || (element != null && element.IsGas)) return "Gas";
            if (prefab.HasTag(GameTags.BuildableRaw) || prefab.HasTag(GameTags.Ore) || prefab.HasTag(GameTags.ConsumableOre) ||
                (element != null && (element.HasTag(GameTags.BuildableRaw) || element.HasTag(GameTags.Ore) || element.HasTag(GameTags.ConsumableOre)))) return "Mineral";
            if (prefab.HasTag(GameTags.ManufacturedMaterial) || prefab.HasTag(GameTags.IndustrialIngredient)) return "Industrial";
            if (element != null && element.IsSolid) return "Mineral";
            return null;
        }

        internal static string SpecialtyName(string category)
        {
            switch (category)
            {
                case "Food": return "食物商";
                case "Metal": return "金属商";
                case "Mineral": return "矿物商";
                case "Seed": return "种子商";
                case "Equipment": return "装备商";
                case "Liquid": return "液体商";
                case "Gas": return "气体商";
                case "Industrial": return "工业材料商";
                default: return "供货商";
            }
        }

        internal static string ProfileName(TradingSupplier supplier)
        {
            switch (TradingMarket.Profile(supplier))
            {
                case "Bulk": return "大宗采购 · 库存充足";
                case "Clearance": return "折扣供货 · 薄利销售";
                default: return "精品供货 · 少量现货";
            }
        }

        internal static float Available(TradingGood good, int worldId)
        {
            var sources = GetSources(good, worldId);
            float amount = 0;
            foreach (Storage source in sources)
                foreach (GameObject item in source.items)
                    if (item != null && StorageItemUtility.GetStoredItemKey(item) == good.Id)
                    {
                        PrimaryElement primary = item.GetComponent<PrimaryElement>();
                        if (primary != null && item.GetComponent<Pickupable>() != null)
                            amount += good.ByUnit ? primary.Units : primary.Mass;
                    }
            // Do not sell materials committed to production orders.
            float reserved = ProductionOrderService.GetTradingReservedAmount(good.Id.ToTag(), sources);
            GameObject prefab = good.ByUnit ? Assets.GetPrefab(good.Id.ToTag()) : null;
            float massPerUnit = prefab != null ? prefab.GetComponent<PrimaryElement>().MassPerUnit : 1f;
            amount -= reserved / Mathf.Max(0.000001f, massPerUnit);
            return Mathf.Max(0, amount);
        }

        private static List<Storage> GetSources(TradingGood good, int worldId)
        {
            List<Storage> sources = new List<Storage>();
            StorageNetworkContentIndexService.FillSourceStorages(worldId, false, new[] { good.Id.ToTag() }, null, null, sources, false);
            return sources;
        }

        internal TradingQuote GetQuote(int supplierId, TradingGood good, double requested, bool buy, int worldId)
        {
            Refresh();
            TradingSupplier supplier = market.Suppliers.Find(s => s.Id == supplierId);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
                return new TradingQuote { SupplierId = supplierId, Good = good, Buy = buy, Reason = "当前星球的存储网络未在线。" };
            double available;
            if (buy)
            {
                double stock = supplier != null && supplier.Stock.TryGetValue(good.Id, out double value) ? value : 0;
                available = PlanPurchase(good, worldId, stock).Sum(entry => (double)entry.Value);
            }
            else available = Available(good, worldId);
            TradingQuote quote = market.Quote(supplier, good, Cycle, buy, requested, available);
            if (buy && supplier != null && supplier.Specialty == good.Category && available <= 0)
                quote.Reason = !supplier.Stock.TryGetValue(good.Id, out double stock) || stock <= 0
                    ? "供应商的该商品已售罄。" : "网络没有符合过滤器且有容量的接收仓库。";
            if (quote.CanTrade && quote.Amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                quote.Amount = 0;
                quote.Total = 0;
                quote.Reason = "可成交数量低于游戏的最小转移量。";
            }
            return quote;
        }

        internal string Trade(int supplierId, TradingGood good, double requested, bool buy, int worldId, decimal expectedPrice = 0m, bool requireFull = false)
        {
            TradingQuote quote = GetQuote(supplierId, good, requested, buy, worldId);
            if (!quote.CanTrade) return quote.Reason;
            if (expectedPrice > 0 && quote.UnitPrice != expectedPrice) return "报价已变化，请检查新的单价和总价后重新确认。";
            if (requireFull && quote.Amount + 0.000001 < requested) return "库存、容量或额度已变化，请检查新的可成交数量后重新确认。";
            TradingSupplier supplier = market.Suppliers.Find(s => s.Id == supplierId);
            double cycle = Cycle;
            double amount = quote.Amount;
            if (amount < (good.ByUnit ? 1 : Math.Max(0.001, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT))) return "无法交易：供货商已离开、余额/额度不足，或没有可交易库存。";
            float transferAmount = (float)amount;
            if (transferAmount > amount)
                transferAmount = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(transferAmount) - 1);
            double moved = buy ? Purchase(good, transferAmount, worldId) : Sell(good, transferAmount, worldId);
            if (moved <= 0) return "交易未完成：没有符合过滤器且容量足够的网络仓库，或材料无法取出。";
            // Physical amounts are floats; normalize to the quoted milligram precision.
            moved = Math.Min(amount, good.ByUnit ? Math.Round(moved) : Math.Round(moved, 3));
            if (!market.Settle(supplier, good, cycle, buy, moved, worldId))
                throw new InvalidOperationException("Trading settlement exceeded its prevalidated limit.");
            Persist();
            return $"{(buy ? "买入" : "卖出")} {good.Name} {moved:0.###}{(good.ByUnit ? " 件" : " kg")}，金额 {TradingMarket.Price(supplier, good, cycle, buy) * (decimal)moved:0.#####}。";
        }

        private static float Sell(TradingGood good, float amount, int worldId)
        {
            float moved = 0;
            foreach (Storage source in GetSources(good, worldId))
            {
                float sourceAvailable = 0;
                float massPerUnit = 1;
                foreach (GameObject storedItem in source.items)
                {
                    if (storedItem == null || StorageItemUtility.GetStoredItemKey(storedItem) != good.Id) continue;
                    PrimaryElement primary = storedItem.GetComponent<PrimaryElement>();
                    if (primary == null || storedItem.GetComponent<Pickupable>() == null) continue;
                    sourceAvailable += good.ByUnit ? primary.Units : primary.Mass;
                    if (good.ByUnit) massPerUnit = primary.MassPerUnit;
                }
                sourceAvailable -= ProductionOrderService.GetTradingReservedAmount(good.Id.ToTag(), new[] { source }) / Mathf.Max(0.000001f, massPerUnit);
                if (sourceAvailable <= 0) continue;
                foreach (GameObject item in new List<GameObject>(source.items))
                {
                    if (item == null || StorageItemUtility.GetStoredItemKey(item) != good.Id) continue;
                    Pickupable pickup = item.GetComponent<Pickupable>();
                    PrimaryElement primary = item.GetComponent<PrimaryElement>();
                    if (pickup == null || primary == null) continue;
                    float take = Mathf.Min(sourceAvailable, Mathf.Min(amount - moved, good.ByUnit ? primary.Units : primary.Mass));
                    if (good.ByUnit) take = Mathf.Floor(take);
                    if (take <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) continue;
                    Pickupable portion = good.ByUnit ? pickup.TakeUnit(take) : pickup.Take(take);
                    if (portion == null) continue;
                    PrimaryElement removed = portion.GetComponent<PrimaryElement>();
                    float taken = good.ByUnit ? removed.Units : removed.Mass;
                    moved += taken;
                    sourceAvailable -= taken;
                    Util.KDestroyGameObject(portion.gameObject);
                    source.Trigger(-1697596308, item);
                    source.OnStorageChange?.Invoke(item);
                    StorageNetworkContentIndexService.Invalidate(source);
                    if (moved + 0.00001f >= amount) return moved;
                }
            }
            return moved;
        }

        private static float Purchase(TradingGood good, float amount, int worldId)
        {
            float moved = 0;
            foreach (KeyValuePair<Storage, float> entry in PlanPurchase(good, worldId, amount))
                moved += PurchaseIntoStorage(good, entry.Value, entry.Key);
            return Mathf.Min(amount, moved);
        }

        private static List<KeyValuePair<Storage, float>> PlanPurchase(TradingGood good, int worldId, double requested)
        {
            var plan = new List<KeyValuePair<Storage, float>>();
            GameObject prefab = Assets.GetPrefab(good.Id.ToTag());
            if (prefab == null || requested <= 0) return plan;
            PrimaryElement template = prefab.GetComponent<PrimaryElement>();
            float massPerUnit = good.ByUnit ? template?.MassPerUnit ?? 0 : 1;
            if (massPerUnit <= 0) return plan;
            var excluded = new HashSet<Storage>();
            var tags = StorageItemUtility.GetStorageMatchTagsNonAlloc(prefab);
            double remaining = requested;
            while (remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                Storage target = StorageTargetSelector.FindOutputTarget(prefab, tags, excluded, null, sourceWorldId: worldId);
                if (target == null || !excluded.Add(target)) break;
                double capacity = Math.Max(0, target.RemainingCapacity() / massPerUnit);
                double portion = Math.Min(remaining, good.ByUnit ? Math.Floor(capacity) : Math.Floor(capacity * 1000d) / 1000d);
                float amount = (float)portion;
                if (amount > portion) amount = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(amount) - 1);
                if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) continue;
                plan.Add(new KeyValuePair<Storage, float>(target, amount));
                remaining -= amount;
            }
            return plan;
        }

        private static float PurchaseIntoStorage(TradingGood good, float amount, Storage target)
        {
            GameObject prefab = Assets.GetPrefab(good.Id.ToTag());
            PrimaryElement template = prefab.GetComponent<PrimaryElement>();
            float massPerUnit = good.ByUnit ? template.MassPerUnit : 1;
            amount = Mathf.Min(amount, Mathf.Floor(target.RemainingCapacity() / massPerUnit * 1000f) / 1000f);
            if (good.ByUnit) amount = Mathf.Floor(amount);
            if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) return 0;
            if (good.ByUnit)
            {
                // Equipment and food use separate instances; never create one multi-unit suit.
                int stored = 0;
                for (int i = 0; i < (int)amount; i++)
                {
                    GameObject unit = GameUtil.KInstantiate(prefab, target.transform.GetPosition(), Grid.SceneLayer.Ore);
                    if (unit == null) break;
                    unit.GetComponent<PrimaryElement>().Mass = massPerUnit;
                    unit.SetActive(true);
                    target.Store(unit, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
                    stored++;
                }
                StorageNetworkContentIndexService.Invalidate(target);
                return stored;
            }
            Element element = ElementLoader.FindElementByHash((SimHashes)good.Id.ToTag().GetHash());
            if (element == null) return 0;
            if (element.IsGas || element.IsLiquid)
            {
                double before = StoredElementMass(target, element.id);
                if (element.IsGas) target.AddGasChunk(element.id, amount, 293.15f, byte.MaxValue, 0, false, true);
                else target.AddLiquid(element.id, amount, 293.15f, byte.MaxValue, 0, false, true);
                float stored = (float)Math.Max(0, Math.Min(amount, StoredElementMass(target, element.id) - before));
                StorageNetworkContentIndexService.Invalidate(target);
                return stored;
            }
            else
            {
                GameObject item = element.substance.SpawnResource(target.transform.GetPosition(), amount, 293.15f, byte.MaxValue, 0, true, false, true);
                if (item == null) return 0;
                target.Store(item, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
            }
            StorageNetworkContentIndexService.Invalidate(target);
            return amount;
        }

        private static double StoredElementMass(Storage storage, SimHashes id)
        {
            double mass = 0;
            foreach (GameObject item in storage.items)
            {
                PrimaryElement primary = item != null ? item.GetComponent<PrimaryElement>() : null;
                if (primary != null && primary.ElementID == id) mass += primary.Mass;
            }
            return mass;
        }
    }

    [HarmonyPatch(typeof(SaveGame), "OnPrefabInit")]
    internal static class TradingSaveGamePatch
    {
        private static void Postfix(SaveGame __instance) { __instance.gameObject.AddOrGet<StorageNetworkTradingState>(); }
    }
}
