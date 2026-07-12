using System.Collections.Generic;
using StorageNetwork.Components;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private static readonly Dictionary<string, ProductionOrderRecord> ActiveOrders = new Dictionary<string, ProductionOrderRecord>();
        private static readonly Dictionary<int, OrderAutomationLease> AutomationLeases = new Dictionary<int, OrderAutomationLease>();
        private static readonly Dictionary<Tag, ProductionKeepRule> KeepRules = new Dictionary<Tag, ProductionKeepRule>();
        private static string loadedStorePath;

        private readonly ProductionNetworkInventoryCache networkInventory = new ProductionNetworkInventoryCache();
        private readonly Dictionary<Tag, float> connectedFabricatorOutputAmounts = new Dictionary<Tag, float>();
        private List<RecipeDisplayInfo> craftableRecipes = new List<RecipeDisplayInfo>();
        private string ignoredReservationOrderKey;
        private StorageNetworkOrderProductionCenter orderCenterScope;

        public IReadOnlyCollection<ProductionOrderRecord> Orders => ActiveOrders.Values;

        public List<Storage> NetworkSourceStorages => networkInventory.SourceStorages;

        internal static bool IsOrderProductionFabricator(ComplexFabricator fabricator)
        {
            return ProductionOrderCenterCatalog.IsOrderProductionFabricator(fabricator);
        }

        public void SetOrderCenterScope(StorageNetworkOrderProductionCenter center)
        {
            if (orderCenterScope == center)
            {
                return;
            }

            orderCenterScope = center;
            craftableRecipes = new List<RecipeDisplayInfo>();
        }

        public void LoadOrdersForDisplay()
        {
            EnsureOrdersLoaded();
        }

        public void Refresh()
        {
            EnsureOrdersLoaded();
            StorageNetworkFabricatorProgress.BeginRefresh();
            networkInventory.Refresh();
            RefreshConnectedFabricatorOutputAmounts();
            craftableRecipes = orderCenterScope != null
                ? ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos(orderCenterScope)
                : ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos();
            UpdateProductionOrderStates();
            PurgeExpiredFinishedOrders();
            RunKeepRules();
        }

        public void RefreshBackground(bool rebuildRecipeCatalog)
        {
            EnsureOrdersLoaded();
            if (KeepRules.Count == 0 && ActiveOrders.Count == 0)
            {
                return;
            }

            StorageNetworkFabricatorProgress.BeginRefresh();
            networkInventory.Refresh();
            RefreshConnectedFabricatorOutputAmounts();
            if (rebuildRecipeCatalog || craftableRecipes.Count == 0)
            {
                craftableRecipes = ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos();
            }

            UpdateProductionOrderStates();
            PurgeExpiredFinishedOrders();
            RunKeepRules();
        }
    }
}
