using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkOrderTrackingRules
    {
        public static string BuildListSignature(ProductDisplayGroup product, IEnumerable<ProductionOrderRecord> records, string searchText, StorageNetworkPanel.TrackingFilterMode filterMode)
        {
            string recordsSignature = string.Join("|", records.Select(record => string.Format(
                "{0}:{1}:{2}:{3:0.###}:{4:0.###}:{5}:{6}:{7:0.###}:{8}",
                record.Key,
                record.DisplayId,
                record.State,
                record.ProducedAtSubmit,
                record.RequestedAmount,
                record.OrderCount,
                record.MergeCount,
                record.LastActivityCycle,
                record.AbnormalReason ?? string.Empty)));

            return string.Format("{0}|{1}|{2}|{3}", product?.ProductKey ?? string.Empty, searchText ?? string.Empty, filterMode, recordsSignature);
        }

        public static string BuildCardSignature(ProductionOrderRecord record)
        {
            return string.Format(
                "{0}:{1}:{2:0.###}:{3:0.###}:{4}:{5}:{6:0.###}:{7}:{8}",
                record.DisplayId,
                record.State,
                record.ProducedAtSubmit,
                record.RequestedAmount,
                record.OrderCount,
                record.MergeCount,
                record.LastActivityCycle,
                record.AbnormalReason ?? string.Empty,
                string.Join(",", (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                    .Where(assignment => assignment != null)
                    .Select(assignment => string.Format("{0}:{1}",
                        assignment.Fabricator != null ? assignment.Fabricator.GetInstanceID() : 0,
                        assignment.Primary))));
        }

        public static bool MatchesFilter(ProductionOrderRecord record, StorageNetworkPanel.TrackingFilterMode filterMode, string searchText)
        {
            if (record == null)
            {
                return false;
            }

            switch (filterMode)
            {
                case StorageNetworkPanel.TrackingFilterMode.Current:
                case StorageNetworkPanel.TrackingFilterMode.All:
                    break;
                case StorageNetworkPanel.TrackingFilterMode.Abnormal:
                    if (record.State != ProductionOrderState.Abnormal)
                    {
                        return false;
                    }
                    break;
                case StorageNetworkPanel.TrackingFilterMode.Completed:
                    if (record.State != ProductionOrderState.Completed)
                    {
                        return false;
                    }
                    break;
                case StorageNetworkPanel.TrackingFilterMode.Running:
                    if (!IsActive(record))
                    {
                        return false;
                    }
                    break;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string needle = searchText.Trim();
            return ContainsIgnoreCase(record.DisplayId.ToString(), needle) ||
                   ContainsIgnoreCase(record.ProductName, needle) ||
                   ContainsIgnoreCase(record.AbnormalReason, needle) ||
                   (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>()).Any(assignment =>
                       assignment != null &&
                       (ContainsIgnoreCase(assignment.Fabricator != null ? assignment.Fabricator.GetProperName() : null, needle) ||
                        ContainsIgnoreCase(assignment.OutputName, needle) ||
                        ContainsIgnoreCase(assignment.ConsumerName, needle) ||
                        ContainsIgnoreCase(assignment.Recipe != null ? assignment.Recipe.GetUIName(false) : null, needle)));
        }

        public static bool IsActive(ProductionOrderRecord order)
        {
            return order.State != ProductionOrderState.Completed &&
                   order.State != ProductionOrderState.Abnormal &&
                   order.State != ProductionOrderState.Cancelled;
        }

        private static bool ContainsIgnoreCase(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack) &&
                   !string.IsNullOrEmpty(needle) &&
                   haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
