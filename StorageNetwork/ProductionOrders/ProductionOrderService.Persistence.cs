using System.Linq;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private static void EnsureOrdersLoaded()
        {
            string storePath = ProductionOrderPersistence.GetStorePath();
            if (loadedStorePath == storePath)
            {
                return;
            }

            ActiveOrders.Clear();
            AutomationLeases.Clear();
            KeepRules.Clear();
            loadedStorePath = storePath;
            foreach (ProductionOrderRecord order in ProductionOrderPersistence.Load())
            {
                ActiveOrders[order.Key] = order;
            }

            foreach (ProductionKeepRule rule in ProductionOrderPersistence.LoadKeepRules())
            {
                KeepRules[rule.ProductTag] = rule;
            }
        }

        public static void SaveOrders()
        {
            ProductionOrderPersistence.Save(ActiveOrders.Values.ToList(), KeepRules.Values.ToList());
        }

        public static void ResetRuntimeState()
        {
            ActiveOrders.Clear();
            AutomationLeases.Clear();
            KeepRules.Clear();
            loadedStorePath = null;
        }
    }
}
