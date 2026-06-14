using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkPlanPreviewMetrics
    {
        public static float EstimateResearchTreeHeight(ProductionPlanNode node, int depth)
        {
            if (node == null)
            {
                return 0f;
            }

            List<ProductionPlanRequirement> requirements = node.Requirements
                .Where(requirement => requirement != null && requirement.Material != Tag.Invalid)
                .Take(depth == 0 ? 5 : 4)
                .ToList();
            if (requirements.Count == 0)
            {
                return 104f;
            }

            float height = 0f;
            foreach (ProductionPlanRequirement requirement in requirements)
            {
                height += requirement.Child != null && depth < 2
                    ? Mathf.Max(110f, EstimateResearchTreeHeight(requirement.Child, depth + 1))
                    : 74f;
            }

            return Mathf.Max(104f, height);
        }

        public static int EstimateResearchTreeDepth(ProductionPlanNode node, int depth)
        {
            if (node == null || depth >= 3)
            {
                return depth + 1;
            }

            int maxDepth = depth + 1;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement != null && requirement.Child != null)
                {
                    maxDepth = Mathf.Max(maxDepth, EstimateResearchTreeDepth(requirement.Child, depth + 1));
                }
            }

            return maxDepth;
        }

        public static float EstimateMaterialTreeWidth(ProductionPlanNode node)
        {
            return Mathf.Max(760f, 240f + EstimateMaterialTreeDepth(node, 0) * 360f);
        }

        private static int EstimateMaterialTreeDepth(ProductionPlanNode node, int depth)
        {
            if (node == null || depth >= 3)
            {
                return depth;
            }

            int maxDepth = depth;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement != null && requirement.Child != null)
                {
                    maxDepth = Mathf.Max(maxDepth, EstimateMaterialTreeDepth(requirement.Child, depth + 1));
                }
            }

            return maxDepth;
        }
    }
}
