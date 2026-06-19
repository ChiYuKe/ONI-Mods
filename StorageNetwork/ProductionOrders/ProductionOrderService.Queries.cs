using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        public List<RecipeDisplayInfo> GetCraftableRecipes()
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

        private static float GetConnectedFabricatorOutputAmount(Tag productTag)
        {
            float amount = 0f;
            foreach (ComplexFabricator fabricator in ProductionOrderCenterCatalog.GetFabricators())
            {
                if (fabricator.outStorage == null || fabricator.outStorage.items == null)
                {
                    continue;
                }

                StorageNetworkEnrollment enrollment = fabricator.GetComponent<StorageNetworkEnrollment>();
                if (enrollment == null || !enrollment.IncludedInSceneNetwork)
                {
                    continue;
                }

                foreach (GameObject item in fabricator.outStorage.items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                    if (primaryElement == null || !StorageNetworkMaterialRequester.MatchesStorageTag(item, productTag))
                    {
                        continue;
                    }

                    amount += primaryElement.Mass;
                }
            }

            return amount;
        }

        public ProductionOrderRecord FindDuplicateOrder(Tag productTag, ComplexRecipe recipe, float requestedAmount)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            int amountBucket = Mathf.RoundToInt(requestedAmount * 1000f);
            return ActiveOrders.Values
                .Where(IsOrderActive)
                .Where(IsOrderInCurrentScope)
                .FirstOrDefault(order =>
                    order.ProductTag == productTag &&
                    order.RecipeKey == recipeKey &&
                    Mathf.RoundToInt(order.LastSubmittedAmount * 1000f) == amountBucket);
        }

        private ProductionOrderRecord FindAutomaticDuplicateOrder(Tag productTag, ComplexRecipe recipe)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            return ActiveOrders.Values
                .Where(IsOrderActive)
                .Where(IsOrderInCurrentScope)
                .Where(order => order.IsAutomatic)
                .OrderBy(order => order.DisplayId)
                .FirstOrDefault(order =>
                    order.ProductTag == productTag &&
                    order.RecipeKey == recipeKey);
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
                return true;
            }

            ComplexFabricator scopedFabricator = orderCenterScope.GetComponent<ComplexFabricator>();
            return scopedFabricator != null &&
                   order != null &&
                   order.QueueAssignments.Any(assignment => assignment.Fabricator == scopedFabricator);
        }
    }
}
