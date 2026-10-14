using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using StorageNetwork.Services;
using StorageNetwork.ProductionOrders;
using UnityEngine;

namespace StorageNetwork.Trading
{
    internal sealed partial class StorageNetworkTradingState
    {
        internal double LastAmount(TradingGood good, bool buy, double fallback)
        {
            return market.LastAmounts.TryGetValue(good.Id + (buy ? ":buy" : ":sell"), out double amount) &&
                !double.IsNaN(amount) && !double.IsInfinity(amount) && amount > 0 ? amount : fallback;
        }

        internal List<TradingDeliveryTarget> DeliveryPreview(TradingGood good, int worldId, double requested)
        {
            return PlanPurchase(good, worldId, requested).Select(pair => new TradingDeliveryTarget { Storage = pair.Key, Amount = pair.Value }).ToList();
        }

        internal List<Storage> InspectionStorages(TradingGood good, int worldId)
        {
            Element element = good.ByUnit ? null : ElementLoader.FindElementByHash((SimHashes)good.Id.ToTag().GetHash());
            return StorageSceneRegistry.GetStorages().Where(storage => storage != null &&
                StorageNetworkStorageRules.IsServerStorage(storage) && StorageTargetSelector.IsStorageReachableFromWorld(storage, worldId) &&
                (element != null ? StorageNetworkStorageRules.MatchesElementState(storage, element) :
                    StorageNetworkStorageRules.IsSolidStorageServer(storage) || StorageNetworkStorageRules.IsColdStorageServer(storage))).ToList();
        }

        private static double StoredGoodAmount(Storage storage, TradingGood good)
        {
            double amount = 0;
            foreach (GameObject item in storage.items)
            {
                if (item == null || StorageItemUtility.GetStoredItemKey(item) != good.Id) continue;
                PrimaryElement primary = item.GetComponent<PrimaryElement>();
                if (primary != null) amount += good.ByUnit ? primary.Units : primary.Mass;
            }
            return amount;
        }

        private static double MinimumSaleAmount(TradingGood good, int worldId)
        {
            double minimum = double.PositiveInfinity;
            foreach (Storage storage in GetSources(good, worldId))
            {
                double stored = StoredGoodAmount(storage, good);
                float reservedMass = ProductionOrderService.GetTradingReservedAmount(good.Id.ToTag(), new[] { storage });
                foreach (GameObject item in storage.items)
                {
                    if (item == null || StorageItemUtility.GetStoredItemKey(item) != good.Id || item.GetComponent<Pickupable>() == null) continue;
                    PrimaryElement primary = item.GetComponent<PrimaryElement>();
                    if (primary == null) continue;
                    double required = TradingPrecision.MinimumTransfer(primary.Mass, primary.MassPerUnit, good.ByUnit, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT);
                    double stock = good.ByUnit ? primary.Units : primary.Mass;
                    double available = stored - reservedMass / (good.ByUnit ? Math.Max(0.000001, primary.MassPerUnit) : 1);
                    if (stock >= required && available >= required) minimum = Math.Min(minimum, required);
                }
            }
            return double.IsInfinity(minimum) ? (good.ByUnit ? 1 : 0.002) : minimum;
        }

        // Observe native inventories even if a synchronous storage listener throws
        // after moving goods. Settlement then covers only the physical amount moved.
        private static double TransferObserved(TradingGood good, float amount, bool buy, int worldId, out string delivery, out List<int> storageIds, out bool interrupted, out Dictionary<int, double> worldDeltas)
        {
            List<Storage> storages = buy ? PlanPurchase(good, worldId, amount).Select(pair => pair.Key).ToList() : GetSources(good, worldId);
            var before = storages.ToDictionary(storage => storage, storage => StoredGoodAmount(storage, good));
            interrupted = false;
            try
            {
                if (buy) Purchase(good, amount, worldId);
                else Sell(good, amount, worldId);
            }
            catch (Exception ex)
            {
                interrupted = true;
                Debug.LogWarning("[StorageNetwork] Trading physical transfer interrupted; reconciling delivered amount: " + ex);
            }
            double moved = 0;
            var locations = new List<string>();
            storageIds = new List<int>();
            worldDeltas = new Dictionary<int, double>();
            foreach (Storage storage in storages)
            {
                if (storage == null) continue;
                double delta = (StoredGoodAmount(storage, good) - before[storage]) * (buy ? 1 : -1);
                if (delta <= 0) continue;
                moved += delta;
                int actualWorld = storage.gameObject.GetMyWorldId();
                worldDeltas.TryGetValue(actualWorld, out double previousDelta);
                worldDeltas[actualWorld] = previousDelta + delta * (buy ? 1 : -1);
                locations.Add(storage.gameObject.GetProperName() + " " + delta.ToString("#,0.###") + (good.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_421F465DAEE8") : " kg"));
                storageIds.Add(StorageItemUtility.GetStorageInstanceId(storage));
                StorageNetworkContentIndexService.Invalidate(storage);
            }
            delivery = string.Join(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.BOUNDARY_SEPARATOR"), locations);
            return Math.Min(amount, moved);
        }

        private static void StoreSpawnedResource(Storage target, GameObject resource)
        {
            try
            {
                resource.SetActive(true);
                target.Store(resource, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
                // Store may merge the instance into an existing stack. The outer
                // audit measures actual received quantity once for the whole batch.
            }
            finally
            {
                // A spawn that failed before being received must not become free
                // loose goods on the ground. Stored goods are audited and charged.
                if (resource != null && !target.items.Contains(resource)) Util.KDestroyGameObject(resource);
            }
        }
    }

    internal sealed class TradingDeliveryTarget
    {
        internal Storage Storage;
        internal double Amount;
    }
}
