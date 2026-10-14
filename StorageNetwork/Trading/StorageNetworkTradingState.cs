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
    internal sealed partial class StorageNetworkTradingState : KMonoBehaviour, ISim1000ms
    {
        [Serialize] private string marketJson;
        private TradingMarket market;
        private List<TradingGood> catalog;
        private bool tradingInProgress;
        private List<TradingIdentityTemplate> identityTemplates;
        private double lastRefreshTime = -1;
        private int lastRefreshWorld = -2;
        private readonly Dictionary<string, double> inventorySnapshotBuffer = new Dictionary<string, double>();
        internal static StorageNetworkTradingState Instance;
        internal TradingMarket Market => market;
        internal IReadOnlyList<TradingGood> Catalog => catalog;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Instance = this;
            TradingText.NativeLabels = LocalizeNativeLabels;
            catalog = BuildCatalog();
            try { market = string.IsNullOrEmpty(marketJson) ? new TradingMarket() : JsonConvert.DeserializeObject<TradingMarket>(marketJson); }
            catch (Exception ex) { Debug.LogWarning("[StorageNetwork] Cannot restore trading state: " + ex); }
            market = market ?? new TradingMarket();
            Refresh();
        }

        protected override void OnCleanUp()
        {
            CancelGroupChat();
            if (Instance == this) { Instance = null; TradingText.NativeLabels = null; }
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt) { Refresh(); }
        internal void Refresh(int worldId = -1)
        {
            if (tradingInProgress || market == null) return;
            double now = Cycle;
            int selectedWorld = worldId >= 0 ? worldId : ClusterManager.Instance?.activeWorldId ?? -1;
            if (lastRefreshTime == now && lastRefreshWorld == selectedWorld) return;
            lastRefreshTime = now;
            lastRefreshWorld = selectedWorld;
            bool changed = false;
            if (ClusterManager.Instance != null)
            {
                foreach (WorldContainer world in ClusterManager.Instance.WorldContainers)
                {
                    if (world == null || world.worldInventory == null || !market.NeedsInventorySnapshot(world.id, Cycle)) continue;
                    var snapshot = world.worldInventory.GetAccessibleAmounts();
                    var amounts = inventorySnapshotBuffer;
                    amounts.Clear();
                    foreach (TradingGood good in catalog)
                    {
                        snapshot.TryGetValue(good.Id.ToTag(), out float amount);
                        amounts[good.Id] = amount;
                    }
                    market.SampleInventory(world.id, Cycle, catalog, amounts);
                    changed = true;
                }
                market.SelectInventoryWorld(worldId >= 0 ? worldId : ClusterManager.Instance.activeWorldId);
            }
            changed |= market.Refresh(Cycle, catalog);
            bool initializeVisitors = changed || identityTemplates == null;
            if (identityTemplates == null) identityTemplates = BuildIdentityTemplates();
            if (initializeVisitors) foreach (TradingSupplier supplier in market.Suppliers)
            {
                changed |= market.EnsureIdentity(supplier, identityTemplates);
                changed |= market.EnsurePersona(supplier);
                TradingRelationships.Initialize(market, supplier);
                changed |= TradingConversations.Upgrade(supplier);
            }
            if (changed) Persist();
        }

        private static List<TradingIdentityTemplate> BuildIdentityTemplates()
        {
            var slots = Db.Get().AccessorySlots;
            var templates = new List<TradingIdentityTemplate>();
            foreach (Personality personality in Db.Get().Personalities.resources.OrderBy(p => p.Id, StringComparer.Ordinal))
            {
                if (personality.model != GameTags.Minions.Models.Standard) continue;
                string head = $"headshape_{personality.headShape:000}";
                string hair = $"hair_{personality.hair:000}";
                string eyes = $"eyes_{personality.eyes:000}";
                string mouth = $"mouth_{personality.mouth:000}";
                if (slots.HeadShape.Lookup(head) == null || slots.Hair.Lookup(hair) == null || slots.Eyes.Lookup(eyes) == null || slots.Mouth.Lookup(mouth) == null) continue;
                templates.Add(new TradingIdentityTemplate { PersonalityId = personality.Id, Name = personality.Name,
                    HeadShape = head, Hair = hair, Eyes = eyes, Mouth = mouth });
            }
            return templates;
        }
        private void Persist() { marketJson = JsonConvert.SerializeObject(market); }
        internal bool AdvanceDialogue(int supplierId, int choice)
        {
            TradingSupplier supplier = market?.Suppliers.Find(s => s.Id == supplierId);
            if (!market.AdvanceConversation(supplier, Cycle, choice, catalog)) return false;
            Persist();
            return true;
        }
        internal static double Cycle => GameClock.Instance != null ? GameClock.Instance.GetTimeInCycles() : 0;
        internal TradingDialogueContext GetDialogueContext(int supplierId)
        {
            Refresh();
            return TradingDialogueContexts.Build(market, market?.Suppliers.Find(s => s.Id == supplierId), catalog, Cycle);
        }
        internal static string DisplaySupplierName(TradingSupplier supplier)
        {
            if (supplier == null) return TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8A11D7DF26E"));
            if (TradingCharacters.Find(supplier) == null && !string.IsNullOrEmpty(supplier.Appearance?.BasePersonalityId))
            {
                Personality personality = Db.Get().Personalities.TryGet(supplier.Appearance.BasePersonalityId);
                if (personality != null) return personality.Name;
            }
            return supplier.Name ?? TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8A11D7DF26E"));
        }
        private static string LocalizeNativeLabels(string text)
        {
            if (Instance?.market?.Suppliers == null) return text;
            foreach (TradingSupplier supplier in Instance.market.Suppliers)
                if (!string.IsNullOrEmpty(supplier.Name) && TradingCharacters.Find(supplier) == null)
                {
                    string localized = DisplaySupplierName(supplier);
                    if (supplier.Name != localized) text = text.Replace(supplier.Name, localized);
                }
            return text;
        }

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
                goods.Add(new TradingGood { Id = id, Name = element.name, BasePrice = price, Category = GetCategory(prefab, element),
                    PreferenceTag = prefab.HasTag(GameTags.RefinedMetal) || element.HasTag(GameTags.RefinedMetal) ? "RefinedMetal" : "RawMaterial" });
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
                goods.Add(new TradingGood { Id = id, Name = prefab.GetProperName(), ByUnit = true, Category = category, BasePrice = price,
                    PreferenceTag = edible?.FoodInfo != null && edible.FoodInfo.Quality > 0 ? "CookedFood" : null });
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
                case "Food": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_1946C83BB3CF");
                case "Metal": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_19BF13FECB73");
                case "Mineral": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C6FE8AE92EEF");
                case "Seed": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B4C0F24EE475");
                case "Equipment": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5598CC53E785");
                case "Liquid": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_44732D8DA19E");
                case "Gas": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_735DF5E29C5A");
                case "Industrial": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7B6C64C04A34");
                default: return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_13E43251E905");
            }
        }

        internal static string ProfileName(TradingSupplier supplier)
        {
            switch (TradingMarket.Profile(supplier))
            {
                case "Bulk": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_84D2E8B7A88F");
                case "Clearance": return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_09AE01ADD7E8");
                default: return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_546623130AEC");
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
            Refresh(worldId);
            TradingSupplier supplier = market.Suppliers.Find(s => s.Id == supplierId);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
                return new TradingQuote { SupplierId = supplierId, Good = good, Buy = buy, Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A797050A89B7") };
            double available;
            if (buy)
            {
                double stock = supplier != null && supplier.Stock.TryGetValue(good.Id, out double value) ? value : 0;
                available = QuotePurchaseCapacity(good, worldId, stock);
            }
            else available = DisplayAvailable(good, worldId);
            TradingQuote quote = market.Quote(supplier, good, Cycle, buy, requested, available);
            double minimum = buy ? (good.ByUnit ? 1 : TradingPrecision.MinimumTransfer(0, 1, false, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT))
                : QuoteSaleMinimum(good, worldId);
            TradingPrecision.ApplyMinimum(quote, minimum, market.Balance);
            if (buy && supplier != null && supplier.Specialty == good.Category && available <= 0)
                quote.Reason = !supplier.Stock.TryGetValue(good.Id, out double stock) || stock <= 0
                    ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C43BEEC96533") : PurchaseUnavailableReason(good, worldId);
            if (quote.CanTrade && quote.Amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                quote.Amount = 0;
                quote.Total = 0;
                quote.Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B59B868B79CD");
            }
            return quote;
        }

        internal string Trade(int supplierId, TradingGood good, double requested, bool buy, int worldId, decimal expectedPrice = 0m, bool requireFull = false, double expectedCycle = -1)
        {
            if (tradingInProgress) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2B7CD9F92A76");
            ClearDisplayQueries();
            TradingQuote quote;
            bypassDisplayCache = true;
            try { quote = GetQuote(supplierId, good, requested, buy, worldId); }
            finally { bypassDisplayCache = false; }
            string confirmationProblem = TradingMarket.ConfirmationProblem(quote, expectedPrice, expectedCycle, requested, requireFull);
            if (confirmationProblem != null) return confirmationProblem;
            TradingSupplier supplier = market.Suppliers.Find(s => s.Id == supplierId);
            double cycle = quote.Cycle;
            double amount = quote.Amount;
            if (amount < (good.ByUnit ? 1 : Math.Max(0.001, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT))) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E5FE8C7E90EE");
            float transferAmount = (float)amount;
            if (transferAmount > amount)
                transferAmount = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(transferAmount) - 1);
            tradingInProgress = true;
            try
            {
                double moved = TransferObserved(good, transferAmount, buy, worldId, out string delivery, out List<int> storageIds, out bool interrupted, out Dictionary<int, double> worldDeltas);
                if (moved <= 0) return buy ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_1F191C094DB4")
                    : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_03603F80C59F");
                // Physical amounts are floats; normalize to the quoted milligram precision.
                moved = Math.Min(amount, good.ByUnit ? Math.Round(moved) : Math.Round(moved, 3));
                if (!market.Settle(supplier, good, cycle, buy, moved, worldId))
                    throw new InvalidOperationException("Trading settlement exceeded its prevalidated limit.");
                foreach (var delta in worldDeltas) market.RecordInventoryTrade(delta.Key, good.Id, delta.Value);
                TradingReceipt receipt = market.Receipts[market.Receipts.Count - 1];
                receipt.Delivery = delivery;
                receipt.StorageIds = storageIds;
                market.RefreshRelationshipPrices(cycle, catalog);
                Persist();
                return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_1A3FD60C3681", TradingText.Argument((object)((interrupted || moved + 0.001 < amount ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5A26850D2325") : "")), null, 0), TradingText.Argument((object)((buy ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A1557EC3B077") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FB28BA14091D"))), null, 0), TradingText.Argument((object)(good.Name), null, 0), TradingText.Argument((object)(moved), "0.###", 0), TradingText.Argument((object)((good.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_421F465DAEE8") : " kg")), null, 0), TradingText.Argument((object)(receipt.Total), "0.#####", 0), TradingText.Argument((object)(delivery), null, 0));
            }
            finally { tradingInProgress = false; }
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
                    if (take < TradingPrecision.MinimumTransfer(primary.Mass, primary.MassPerUnit, good.ByUnit, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)) continue;
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
            if (requested <= 0) return plan;
            // Materials use the network's element routing, not item-prefab tags.
            // An element's substance prefab need not be registered under its element ID.
            if (!good.ByUnit)
            {
                Element element = ElementLoader.FindElementByHash((SimHashes)good.Id.ToTag().GetHash());
                if (element == null) return plan;
                double materialRemaining = requested;
                foreach (Storage target in StorageTargetSelector.FindElementOutputTargets(element.id, sourceWorldId: worldId))
                {
                    double portion = Math.Min(materialRemaining, Math.Floor(Math.Max(0, target.RemainingCapacity()) * 1000d) / 1000d);
                    float amount = (float)portion;
                    if (amount > portion) amount = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(amount) - 1);
                    if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) continue;
                    plan.Add(new KeyValuePair<Storage, float>(target, amount));
                    materialRemaining -= amount;
                    if (materialRemaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) break;
                }
                return plan;
            }
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
                Storage target = good.Category == "Food"
                    ? StorageTargetSelector.FindFoodOutputTarget(prefab, tags, excluded, null, sourceWorldId: worldId)
                    : StorageTargetSelector.FindOutputTarget(prefab, tags, excluded, null, sourceWorldId: worldId);
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

        private string PurchaseUnavailableReason(TradingGood good, int worldId)
        {
            var candidates = InspectionStorages(good, worldId);
            if (candidates.Count == 0) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_4838DBEEA520");
            var online = candidates.Where(StorageNetworkStorageRules.IsConnectedNetworkStorage).ToList();
            if (online.Count == 0) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_74C710E7DD97");
            if (online.All(storage => storage.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT))
                return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DB59C623324B");
            if (!good.ByUnit)
            {
                SimHashes hash = (SimHashes)good.Id.ToTag().GetHash();
                if (ElementLoader.FindElementByHash(hash) == null) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_00DA415FB186");
                if (StorageTargetSelector.HasElementOutputCandidateIgnoringCapacity(hash, sourceWorldId: worldId))
                    return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_068D730B76D4");
                return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_9DDDF8E3FDE1");
            }
            if (Assets.GetPrefab(good.Id.ToTag()) == null) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A47F09D28DE2");
            float massPerUnit = Assets.GetPrefab(good.Id.ToTag()).GetComponent<PrimaryElement>()?.MassPerUnit ?? 0;
            if (massPerUnit <= 0) return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_473168F9D430");
            if (online.All(storage => storage.RemainingCapacity() < massPerUnit))
                return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_59B3CF59CB4A", TradingText.Argument((object)(massPerUnit), "0.###", 0));
            return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7FABB04454DE");
        }

        private static float PurchaseIntoStorage(TradingGood good, float amount, Storage target)
        {
            GameObject prefab = good.ByUnit ? Assets.GetPrefab(good.Id.ToTag()) : null;
            if (good.ByUnit && prefab == null) return 0;
            float massPerUnit = good.ByUnit ? prefab.GetComponent<PrimaryElement>()?.MassPerUnit ?? 0 : 1;
            if (massPerUnit <= 0) return 0;
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
                    StoreSpawnedResource(target, unit);
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
                // SpawnResource's manual_activation flag leaves Pickupable's injected
                // PrimaryElement reference uninitialized until the resource is activated.
                // Storage change listeners can read TotalAmount synchronously in Store.
                StoreSpawnedResource(target, item);
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
