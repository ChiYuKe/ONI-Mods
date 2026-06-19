using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class ProductionOrderRuntimeAllocation
    {
        public static int GetRunningCountForOrder(ProductionOrderRecord record)
        {
            if (record == null || record.QueueAssignments == null)
            {
                return 0;
            }

            int running = 0;
            foreach (ProductionOrderQueueAssignment assignment in record.QueueAssignments.Where(assignment => assignment != null && assignment.Primary))
            {
                running += GetRunningCountForAssignment(record, assignment);
            }

            return running;
        }

        public static int GetRunningCountForAssignment(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (record == null || assignment?.Fabricator == null || assignment.Recipe == null)
            {
                return 0;
            }

            int totalRunning = StorageNetworkFabricatorProgress.GetWorkingCountForRecipe(assignment.Fabricator, assignment.Recipe);
            if (totalRunning <= 0)
            {
                return 0;
            }

            int earlierDemand = 0;
            foreach (ProductionOrderRecord order in GetActiveOrdersForSameQueue(record, assignment))
            {
                if (IsOrderAheadOf(order, record))
                {
                    earlierDemand += GetRemainingCountForOrderQueue(order, assignment.Fabricator, assignment.Recipe);
                }
            }

            int ownDemand = GetRemainingCount(record, assignment);
            return Mathf.Clamp(totalRunning - earlierDemand, 0, ownDemand);
        }

        public static bool HasQueuedWorkForAssignment(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (record == null || assignment?.Fabricator == null || assignment.Recipe == null)
            {
                return false;
            }

            int queued = StorageNetworkFabricatorProgress.GetFiniteRecipeQueueCountSafe(assignment.Fabricator, assignment.Recipe);
            int running = StorageNetworkFabricatorProgress.GetWorkingCountForRecipe(assignment.Fabricator, assignment.Recipe);
            int availableWork = queued + running;
            int earlierDemand = 0;
            foreach (ProductionOrderRecord order in GetActiveOrdersForSameQueue(record, assignment))
            {
                if (IsOrderAheadOf(order, record))
                {
                    earlierDemand += GetRemainingCountForOrderQueue(order, assignment.Fabricator, assignment.Recipe);
                }
            }

            return availableWork - earlierDemand > 0 && GetRemainingCount(record, assignment) > 0;
        }

        public static int GetAllocatedWorkCountForAssignment(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (record == null || assignment?.Fabricator == null || assignment.Recipe == null)
            {
                return 0;
            }

            int queued = StorageNetworkFabricatorProgress.GetFiniteRecipeQueueCountSafe(assignment.Fabricator, assignment.Recipe);
            int running = StorageNetworkFabricatorProgress.GetWorkingCountForRecipe(assignment.Fabricator, assignment.Recipe);
            int availableWork = queued + running;
            int earlierDemand = 0;
            foreach (ProductionOrderRecord order in GetActiveOrdersForSameQueue(record, assignment))
            {
                if (IsOrderAheadOf(order, record))
                {
                    earlierDemand += GetRemainingCountForOrderQueue(order, assignment.Fabricator, assignment.Recipe);
                }
            }

            return Mathf.Clamp(availableWork - earlierDemand, 0, GetRemainingCount(record, assignment));
        }

        public static float GetProgressForAssignment(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            return GetRunningCountForAssignment(record, assignment) > 0
                ? StorageNetworkFabricatorProgress.GetRecipeProgress(assignment.Fabricator, assignment.Recipe)
                : 0f;
        }

        private static IEnumerable<ProductionOrderRecord> GetActiveOrdersForSameQueue(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            return ProductionOrderService.OrdersSnapshot()
                .Where(order => ProductionOrderService.IsOrderActiveForRuntimeAllocation(order) &&
                                order.QueueAssignments.Any(candidate => IsSameQueue(candidate, assignment)));
        }

        private static int GetRemainingCountForOrderQueue(ProductionOrderRecord order, ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            return (order.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment.Fabricator == fabricator && assignment.Recipe == recipe)
                .Sum(assignment => GetRemainingCount(order, assignment));
        }

        private static int GetRemainingCount(ProductionOrderRecord order, ProductionOrderQueueAssignment assignment)
        {
            float outputAmount = GetRecipeOutputAmount(assignment.Recipe, order.ProductTag);
            if (outputAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return assignment.OrderCount;
            }

            int totalAssigned = order.QueueAssignments
                .Where(candidate => candidate.Recipe == assignment.Recipe && GetRecipeOutputAmount(candidate.Recipe, order.ProductTag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .Sum(candidate => candidate.OrderCount);
            if (totalAssigned <= 0)
            {
                return assignment.OrderCount;
            }

            float remainingAmount = Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit);
            int totalRemaining = Mathf.CeilToInt(remainingAmount / outputAmount);
            int remainingForAssignment = Mathf.CeilToInt(totalRemaining * assignment.OrderCount / (float)totalAssigned);
            return Mathf.Clamp(remainingForAssignment, 0, assignment.OrderCount);
        }

        private static float GetRecipeOutputAmount(ComplexRecipe recipe, Tag productTag)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(recipe, productTag);
            return result != null ? Mathf.Max(0f, result.amount) : 0f;
        }

        private static bool IsSameQueue(ProductionOrderQueueAssignment left, ProductionOrderQueueAssignment right)
        {
            return left != null &&
                   right != null &&
                   left.Fabricator == right.Fabricator &&
                   left.Recipe == right.Recipe;
        }

        private static bool IsOrderAheadOf(ProductionOrderRecord candidate, ProductionOrderRecord order)
        {
            if (candidate.CreatedCycle < order.CreatedCycle - 0.001f)
            {
                return true;
            }

            return Mathf.Abs(candidate.CreatedCycle - order.CreatedCycle) <= 0.001f &&
                   candidate.DisplayId < order.DisplayId;
        }
    }
}
