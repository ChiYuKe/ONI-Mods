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
            Runtime.Reset();
            observedOrderAccountingConnectivityVersion = -1;
            loadedStorePath = storePath;
            foreach (ProductionOrderRecord order in ProductionOrderPersistence.Load())
            {
                ActiveOrders[order.Key] = order;
            }

            foreach (ProductionKeepRule rule in ProductionOrderPersistence.LoadKeepRules())
            {
                KeepRules[rule.ProductTag] = rule;
            }

            MarkOrdersChanged();
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
            Runtime.Reset();
            ProductionOrderCenterCatalog.ResetRuntimeState();
            ProductionOrderRuntimeAllocation.ResetRuntimeState();
            StorageNetworkFabricatorProgress.ResetRuntimeState();
            LeasedMaterialOrderBuffer.Clear();
            EmptyAutomationLeaseBuffer.Clear();
            observedOrderAccountingConnectivityVersion = -1;
            orderVersion = 0;
            loadedStorePath = null;
        }
    }
}
