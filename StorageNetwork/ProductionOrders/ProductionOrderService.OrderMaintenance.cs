using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private bool HasMissingReservedMaterial(ProductionOrderRecord order)
        {
            foreach (KeyValuePair<Tag, float> pair in order.ReservedMaterials)
            {
                if (GetNetworkRawAmount(pair.Key) + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < pair.Value)
                {
                    return true;
                }
            }

            return false;
        }

        private bool MaintainActiveOrderPlan(ProductionOrderRecord order)
        {
            if (order == null || order.RequestedAmount - order.ProducedAtSubmit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return false;
            }

            RecipeDisplayInfo route = FindRouteForOrder(order);
            if (route.Recipe == null || route.Fabricators.Count == 0)
            {
                return false;
            }

            ProductionPlanNode plan = BuildProductionPlanIgnoringOrderReservations(
                route.Recipe,
                route.Fabricators,
                order.ProductTag,
                Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit),
                order.Key);
            if (plan == null || plan.Assignments.Count == 0)
            {
                return false;
            }

            List<ProductionOrderQueueAssignment> queueAssignments = BuildQueueAssignments(plan);
            List<ProductionOrderMaterialLease> materialLeases = BuildMaterialLeases(plan);
            bool queued = EnsureProductionPlanQueued(plan, order, materialLeases);
            bool refreshed = order.RefreshPlan(
                plan.OrderCount,
                BuildReservedMaterials(plan),
                queueAssignments,
                materialLeases,
                BuildOutputLeases(queueAssignments, order.ProductTag, Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit)));
            return queued || refreshed;
        }

        private RecipeDisplayInfo FindRouteForOrder(ProductionOrderRecord order)
        {
            return craftableRecipes.FirstOrDefault(route =>
                route.ProductTag == order.ProductTag &&
                ProductionRecipeCatalog.GetRecipeKey(route.Recipe) == order.RecipeKey);
        }

        private ProductionPlanNode BuildProductionPlanIgnoringOrderReservations(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount, string orderKey)
        {
            string previousIgnoredReservationOrderKey = ignoredReservationOrderKey;
            ignoredReservationOrderKey = orderKey;
            try
            {
                return BuildProductionPlan(recipe, fabricators, productTag, requestedAmount);
            }
            finally
            {
                ignoredReservationOrderKey = previousIgnoredReservationOrderKey;
            }
        }

        private bool EnsureProductionPlanQueued(ProductionPlanNode node, ProductionOrderRecord order, List<ProductionOrderMaterialLease> materialLeases)
        {
            if (node == null)
            {
                return false;
            }

            bool changed = false;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                changed |= EnsureProductionPlanQueued(requirement.Child, order, materialLeases);
            }

            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                if (!IsOrderProductionFabricator(assignment.Fabricator) || node.Recipe == null || assignment.OrderCount <= 0)
                {
                    continue;
                }

                ProductionOrderQueueAssignment runtimeAssignment = new ProductionOrderQueueAssignment(assignment.Fabricator, node.Recipe, assignment.OrderCount, node.ProductTag, null, null, true);
                int activeCount = ProductionOrderRuntimeAllocation.GetAllocatedWorkCountForAssignment(order, runtimeAssignment);
                int deficit = Mathf.Max(0, assignment.OrderCount - activeCount);
                if (deficit <= 0)
                {
                    EnsureOrderAutomationEnabled(assignment.Fabricator, order.Key);
                    continue;
                }

                int queued = GetFiniteRecipeQueueCount(assignment.Fabricator, node.Recipe);
                assignment.Fabricator.SetRecipeQueueCount(node.Recipe, queued + deficit);
                EnsureOrderAutomationEnabled(assignment.Fabricator, order.Key);
                DispatchRecipeIngredients(node, new ProductionPlanAssignment(assignment.Fabricator, deficit, node.OutputAmount * deficit), materialLeases);
                changed = true;
            }

            return changed;
        }

    }
}
