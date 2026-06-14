using System.Collections.Generic;
using System.Reflection;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private static readonly Dictionary<string, ProductionOrderRecord> ActiveOrders = new Dictionary<string, ProductionOrderRecord>();
        private static readonly Dictionary<int, OrderAutomationLease> AutomationLeases = new Dictionary<int, OrderAutomationLease>();
        private static readonly Dictionary<Tag, ProductionKeepRule> KeepRules = new Dictionary<Tag, ProductionKeepRule>();
        private static readonly FieldInfo RecipeQueueCountsField = typeof(ComplexFabricator).GetField("recipeQueueCounts", BindingFlags.Instance | BindingFlags.NonPublic);
        private static string loadedStorePath;

        private readonly ProductionNetworkInventoryCache networkInventory = new ProductionNetworkInventoryCache();
        private List<RecipeDisplayInfo> craftableRecipes = new List<RecipeDisplayInfo>();
        private string ignoredReservationOrderKey;

        public IReadOnlyCollection<ProductionOrderRecord> Orders => ActiveOrders.Values;

        public List<Storage> NetworkSourceStorages => networkInventory.SourceStorages;

        public void LoadOrdersForDisplay()
        {
            EnsureOrdersLoaded();
        }

        public void Refresh()
        {
            EnsureOrdersLoaded();
            networkInventory.Refresh();
            craftableRecipes = ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos();
            UpdateProductionOrderStates();
            PurgeExpiredFinishedOrders();
            RunKeepRules();
        }
    }
}
