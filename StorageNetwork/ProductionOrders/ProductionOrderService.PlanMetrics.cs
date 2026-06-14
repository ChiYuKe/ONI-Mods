using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        public float EstimatePlanSeconds(ProductionPlanNode node, out bool hasInfiniteQueue)
        {
            hasInfiniteQueue = false;
            if (node == null || node.Recipe == null)
            {
                return 0f;
            }

            float seconds = 0f;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child == null)
                {
                    continue;
                }

                seconds += EstimatePlanSeconds(requirement.Child, out bool childHasInfiniteQueue);
                hasInfiniteQueue |= childHasInfiniteQueue;
            }

            seconds += EstimateQueuedSeconds(node, out bool fabricatorHasInfiniteQueue);
            hasInfiniteQueue |= fabricatorHasInfiniteQueue;
            int busiestAssignedCount = node.Assignments.Count == 0 ? node.OrderCount : node.Assignments.Max(assignment => assignment.OrderCount);
            seconds += Mathf.Max(0f, node.Recipe.time) * busiestAssignedCount;
            return seconds;
        }

        public List<string> FormatPlanLines(ProductionPlanNode node, int depth)
        {
            List<string> lines = new List<string>();
            string indent = new string(' ', depth * 4);
            lines.Add(string.Format("{0}<b>{1}</b> x{2} -> {3}", indent, node.Recipe.GetUIName(false), node.OrderCount, node.FabricatorName));
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
                lines.Add(string.Format(
                    "{0}{1}: {2}/{3}{4}",
                    indent,
                    ProductionOrderFormatting.GetTagDisplayName(requirement.Material),
                    GameUtil.GetFormattedMass(requirement.AvailableAmount),
                    GameUtil.GetFormattedMass(requirement.RequiredAmount),
                    missing > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? "  " + string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MISSING_PREFIX), GameUtil.GetFormattedMass(missing)) : string.Empty));

                if (requirement.Child != null)
                {
                    lines.AddRange(FormatPlanLines(requirement.Child, depth + 1));
                }
            }

            return lines;
        }

        private static int CountBlockedRequirements(ProductionPlanNode node)
        {
            return CountRequirements(node, requirement =>
                requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
                requirement.Child == null);
        }

        private static int CountProducedRequirements(ProductionPlanNode node)
        {
            return CountRequirements(node, requirement =>
                requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
                requirement.Child != null);
        }

        private static int CountRequirements(ProductionPlanNode node, System.Func<ProductionPlanRequirement, bool> predicate)
        {
            if (node == null || predicate == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (predicate(requirement))
                {
                    count++;
                }

                count += CountRequirements(requirement.Child, predicate);
            }

            return count;
        }

        private static float EstimateMissingAmount(ProductionPlanNode node)
        {
            if (node == null)
            {
                return 0f;
            }

            float missing = 0f;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child == null)
                {
                    missing += Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
                }

                missing += EstimateMissingAmount(requirement.Child);
            }

            return missing;
        }

        private static int EstimateQueueLoad(ProductionPlanNode node)
        {
            if (node == null)
            {
                return 0;
            }

            int load = node.Assignments.Sum(assignment => assignment.Fabricator != null && node.Recipe != null
                ? Mathf.Max(0, assignment.Fabricator.GetRecipeQueueCount(node.Recipe))
                : 0);
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                load += EstimateQueueLoad(requirement.Child);
            }

            return load;
        }

        private static float EstimateQueuedSeconds(ProductionPlanNode node, out bool hasInfiniteQueue)
        {
            hasInfiniteQueue = false;
            if (node == null || node.Recipe == null || node.Assignments.Count == 0)
            {
                return 0f;
            }

            float maxQueuedSeconds = 0f;
            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                int queued = assignment.Fabricator.GetRecipeQueueCount(node.Recipe);
                if (queued == ComplexFabricator.QUEUE_INFINITE)
                {
                    hasInfiniteQueue = true;
                    continue;
                }

                maxQueuedSeconds = Mathf.Max(maxQueuedSeconds, Mathf.Max(0, queued) * Mathf.Max(0f, node.Recipe.time));
            }

            return maxQueuedSeconds;
        }
    }
}
