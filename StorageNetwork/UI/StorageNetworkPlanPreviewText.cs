using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkPlanPreviewText
    {
        public static string BuildAssignmentSummary(ProductionPlanNode node, int maxItems)
        {
            if (node == null || node.Assignments == null || node.Assignments.Count == 0)
            {
                return node != null ? node.FabricatorName : "?";
            }

            List<string> names = node.Assignments
                .Take(maxItems)
                .Select(assignment => string.Format("{0} x{1}", assignment.Fabricator != null ? assignment.Fabricator.GetProperName() : "?", assignment.OrderCount))
                .ToList();
            if (node.Assignments.Count > maxItems)
            {
                names.Add("+" + (node.Assignments.Count - maxItems));
            }

            return string.Join(" / ", names.ToArray());
        }
    }
}
