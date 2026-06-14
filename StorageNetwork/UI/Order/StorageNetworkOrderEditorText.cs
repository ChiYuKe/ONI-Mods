using StorageNetwork.ProductionOrders;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkOrderEditorText
    {
        public static string BuildTrackingStatus(string lastOrderStatus, ProductionOrderDraft draft, int activeOrderCount)
        {
            if (!string.IsNullOrEmpty(lastOrderStatus))
            {
                return lastOrderStatus;
            }

            if (draft.DuplicateOrder != null)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DUPLICATE_FOUND), draft.DuplicateOrder.DisplayId);
            }

            return activeOrderCount > 0
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ACTIVE_ORDERS_FOUND), activeOrderCount)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NO_ACTIVE_ORDERS);
        }

        public static string GetRiskLabel(ProductionOrderRiskLevel risk)
        {
            switch (risk)
            {
                case ProductionOrderRiskLevel.Blocked:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RISK_BLOCKED);
                case ProductionOrderRiskLevel.Warning:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RISK_WARNING);
                default:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RISK_READY);
            }
        }

        public static string BuildAutomationSummary(ProductionOrderDraft draft)
        {
            if (draft.RiskLevel == ProductionOrderRiskLevel.Blocked)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AUTOMATION_BLOCKED);
            }

            if (draft.ProducedRequirementCount > 0)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AUTOMATION_PRODUCE), draft.ProducedRequirementCount);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AUTOMATION_READY);
        }
    }
}
