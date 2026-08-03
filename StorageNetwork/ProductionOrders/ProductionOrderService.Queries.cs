using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        public IReadOnlyList<RecipeDisplayInfo> GetCraftableRecipes()
        {
            return craftableRecipes;
        }

        public List<ProductDisplayGroup> GetProductGroups()
        {
            return ProductionRecipeCatalog.BuildProductGroups(craftableRecipes);
        }

        public float GetNetworkAvailableAmount(Tag tag)
        {
            return Mathf.Max(0f, GetNetworkRawAmount(tag) - GetReservedAmount(tag, ignoredReservationOrderKey));
        }

        public float GetNetworkRawAmount(Tag tag)
        {
            return networkInventory.GetRawAmount(tag);
        }

        private float GetProducedAmountForOrder(Tag productTag)
        {
            return GetNetworkRawAmount(productTag) + GetConnectedFabricatorOutputAmount(productTag);
        }

        private float GetConnectedFabricatorOutputAmount(Tag productTag)
        {
            return connectedFabricatorOutputAmounts.TryGetValue(productTag, out float amount) ? amount : 0f;
        }

        public ProductionOrderRecord FindDuplicateOrder(Tag productTag, ComplexRecipe recipe, float requestedAmount)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            int amountBucket = Mathf.RoundToInt(requestedAmount * 1000f);
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (IsOrderActive(order) &&
                    IsOrderInCurrentScope(order) &&
                    order.ProductTag == productTag &&
                    order.RecipeKey == recipeKey &&
                    Mathf.RoundToInt(order.LastSubmittedAmount * 1000f) == amountBucket)
                {
                    return order;
                }
            }

            return null;
        }

        private ProductionOrderRecord FindAutomaticDuplicateOrder(Tag productTag, ComplexRecipe recipe)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            ProductionOrderRecord result = null;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (IsOrderActive(order) &&
                    IsOrderInCurrentScope(order) &&
                    order.IsAutomatic &&
                    order.ProductTag == productTag &&
                    order.RecipeKey == recipeKey &&
                    (result == null || order.DisplayId < result.DisplayId))
                {
                    result = order;
                }
            }

            return result;
        }

        public IReadOnlyList<ProductionOrderRecord> GetActiveOrdersForProduct(Tag productTag, int limit)
        {
            return ActiveOrders.Values
                .Where(order => order.ProductTag == productTag && IsOrderActive(order))
                .Where(IsOrderInCurrentScope)
                .OrderByDescending(order => order.CreatedCycle)
                .Take(limit)
                .ToList();
        }

        public IReadOnlyList<ProductionOrderRecord> GetRecentOrdersForProduct(Tag productTag, int limit)
        {
            return ActiveOrders.Values
                .Where(order => order.ProductTag == productTag)
                .Where(IsOrderInCurrentScope)
                .OrderByDescending(order => order.State == ProductionOrderState.Completed ? order.CompletedCycle : float.MaxValue)
                .ThenByDescending(order => order.CreatedCycle)
                .Take(limit)
                .ToList();
        }

        public IReadOnlyList<ProductionOrderRecord> GetRecentOrders(int limit)
        {
            IEnumerable<ProductionOrderRecord> orders = ActiveOrders.Values
                .Where(IsOrderInCurrentScope)
                .OrderByDescending(order => order.State == ProductionOrderState.Completed ? order.CompletedCycle : float.MaxValue)
                .ThenByDescending(order => order.CreatedCycle);

            return limit > 0 ? orders.Take(limit).ToList() : orders.ToList();
        }

        public IReadOnlyList<string> GetActiveOrderUsagesForFabricator(ComplexFabricator fabricator, int limit)
        {
            if (!IsOrderProductionFabricator(fabricator))
            {
                return new List<string>();
            }

            EnsureOrdersLoaded();
            return ActiveOrders.Values
                .Where(order => IsOrderActive(order) && order.QueueAssignments.Any(assignment => assignment.Fabricator == fabricator))
                .OrderBy(order => order.DisplayId)
                .Take(limit)
                .Select(order => FormatOrderUsage(order, fabricator))
                .ToList();
        }

        private bool IsOrderInCurrentScope(ProductionOrderRecord order)
        {
            if (orderCenterScope == null)
            {
                return IsOrderReachableFromCurrentWorld(order);
            }

            ComplexFabricator scopedFabricator = orderCenterScope.GetComponent<ComplexFabricator>();
            return scopedFabricator != null &&
                   order != null &&
                   order.QueueAssignments.Any(assignment => assignment.Fabricator == scopedFabricator);
        }
    }
}
