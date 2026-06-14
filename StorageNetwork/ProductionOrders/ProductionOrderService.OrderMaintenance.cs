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
            bool queued = EnsureProductionPlanQueued(plan, order.Key, materialLeases);
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

        private bool EnsureProductionPlanQueued(ProductionPlanNode node, string orderKey, List<ProductionOrderMaterialLease> materialLeases)
        {
            if (node == null)
            {
                return false;
            }

            bool changed = false;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                changed |= EnsureProductionPlanQueued(requirement.Child, orderKey, materialLeases);
            }

            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                if (assignment.Fabricator == null || node.Recipe == null || assignment.OrderCount <= 0)
                {
                    continue;
                }

                int deficit = GetQueueDeficit(assignment.Fabricator, node.Recipe, assignment.OrderCount);
                if (deficit <= 0)
                {
                    EnsureOrderAutomationEnabled(assignment.Fabricator, orderKey);
                    continue;
                }

                int queued = GetFiniteRecipeQueueCount(assignment.Fabricator, node.Recipe);
                assignment.Fabricator.SetRecipeQueueCount(node.Recipe, queued + deficit);
                EnsureOrderAutomationEnabled(assignment.Fabricator, orderKey);
                DispatchRecipeIngredients(node, new ProductionPlanAssignment(assignment.Fabricator, deficit, node.OutputAmount * deficit), materialLeases);
                changed = true;
            }

            return changed;
        }

        private static int GetQueueDeficit(ComplexFabricator fabricator, ComplexRecipe recipe, int desiredCount)
        {
            int activeCount = GetFiniteRecipeQueueCount(fabricator, recipe);
            if (fabricator.CurrentWorkingOrder == recipe)
            {
                activeCount++;
            }

            return Mathf.Max(0, desiredCount - activeCount);
        }
    }
}
