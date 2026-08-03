using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private static readonly Dictionary<string, ProductionOrderRecord> ActiveOrders = new Dictionary<string, ProductionOrderRecord>();
        private static readonly Dictionary<int, OrderAutomationLease> AutomationLeases = new Dictionary<int, OrderAutomationLease>();
        private static readonly Dictionary<Tag, ProductionKeepRule> KeepRules = new Dictionary<Tag, ProductionKeepRule>();
        private static string loadedStorePath;
        private static int observedOrderAccountingConnectivityVersion = -1;
        private static int orderVersion;

        private static readonly ProductionOrderRuntime Runtime = new ProductionOrderRuntime();

        private ProductionNetworkInventoryCache networkInventory =>
            Runtime.GetNetworkInventory(GetCurrentNetworkWorldId());
        private Dictionary<Tag, float> connectedFabricatorOutputAmounts =>
            Runtime.GetConnectedFabricatorOutputAmounts(GetCurrentNetworkWorldId());
        private ProductionRecipeSnapshot recipeSnapshot = ProductionRecipeSnapshot.Empty;
        private IReadOnlyList<RecipeDisplayInfo> craftableRecipes = new RecipeDisplayInfo[0];
        private string ignoredReservationOrderKey;
        private StorageNetworkOrderProductionCenter orderCenterScope;
        private int networkWorldId = -1;

        public IReadOnlyCollection<ProductionOrderRecord> Orders => ActiveOrders.Values;

        internal static int OrderVersion => orderVersion;

        private static void MarkOrdersChanged()
        {
            unchecked
            {
                orderVersion++;
            }
        }

        public List<Storage> NetworkSourceStorages => networkInventory.SourceStorages;

        internal static bool IsOrderProductionFabricator(ComplexFabricator fabricator)
        {
            return ProductionOrderCenterCatalog.IsOrderProductionFabricator(fabricator);
        }

        internal static void NotifyFabricatorOutputChanged(ComplexFabricator fabricator)
        {
            int worldId = fabricator != null
                ? StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject)
                : -1;
            if (worldId >= 0)
            {
                Runtime.InvalidateInventorySnapshot(worldId);
            }
        }

        public void SetOrderCenterScope(StorageNetworkOrderProductionCenter center)
        {
            int desiredWorldId = center != null
                ? StorageNetworkWorldUtility.GetObjectWorldId(center.gameObject)
                : GetActiveWorldId();
            if (orderCenterScope == center && networkWorldId == desiredWorldId)
            {
                return;
            }

            orderCenterScope = center;
            networkWorldId = desiredWorldId;
            recipeSnapshot = ProductionRecipeSnapshot.Empty;
            craftableRecipes = recipeSnapshot.Recipes;
        }

        public void LoadOrdersForDisplay()
        {
            EnsureOrdersLoaded();
        }

        public void Refresh()
        {
            EnsureOrdersLoaded();
            Runtime.GetNetworkInventory(GetCurrentNetworkWorldId());
            SetRecipeSnapshot(Runtime.GetRecipeSnapshot(orderCenterScope, false));
        }

        public void RefreshBackground(bool rebuildRecipeCatalog)
        {
            using (StorageNetworkFrameProfileTool.BeginWork(
                       StorageNetworkPerformanceArea.ProductionMaintenance))
            {
                EnsureOrdersLoaded();
                if (observedOrderAccountingConnectivityVersion < 0)
                {
                    observedOrderAccountingConnectivityVersion =
                        StorageSceneRegistry.ConnectivityVersion;
                }
                if (KeepRules.Count == 0 && ActiveOrders.Count == 0)
                {
                    return;
                }

                networkWorldId = GetActiveWorldId();
                Runtime.RefreshInventorySnapshot(networkWorldId);
                SetRecipeSnapshot(Runtime.GetRecipeSnapshot(null, rebuildRecipeCatalog));

                BeginMaintenancePlanningTick();
                try
                {
                    UpdateProductionOrderStates();
                }
                finally
                {
                    EndMaintenancePlanningTick();
                }
                PurgeExpiredFinishedOrders();
                networkWorldId = GetActiveWorldId();
                Runtime.GetNetworkInventory(networkWorldId);
                RunKeepRules();
            }
        }

        private void SetRecipeSnapshot(ProductionRecipeSnapshot snapshot)
        {
            if (snapshot == null || ReferenceEquals(recipeSnapshot, snapshot))
            {
                return;
            }

            recipeSnapshot = snapshot;
            craftableRecipes = snapshot.Recipes;
        }

        private RecipeDisplayInfo[] GetCraftableRoutesProducing(Tag productTag)
        {
            return recipeSnapshot.GetRoutes(productTag);
        }
    }
}
