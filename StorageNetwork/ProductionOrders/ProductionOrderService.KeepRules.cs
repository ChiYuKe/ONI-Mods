using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        public ProductionKeepRule GetKeepRule(Tag productTag)
        {
            return KeepRules.TryGetValue(productTag, out ProductionKeepRule rule) ? rule : null;
        }

        public void SetKeepRule(ProductDisplayGroup product, RecipeDisplayInfo route, float targetAmount)
        {
            if (product == null || route.Recipe == null || targetAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            KeepRules[product.ProductTag] = new ProductionKeepRule(
                product.ProductTag,
                product.ProductName,
                ProductionRecipeCatalog.GetRecipeKey(route.Recipe),
                targetAmount);
        }

        public void ClearKeepRule(Tag productTag)
        {
            KeepRules.Remove(productTag);
        }

        private void RunKeepRules()
        {
            if (KeepRules.Count == 0 || craftableRecipes.Count == 0)
            {
                return;
            }

            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            Dictionary<Tag, ProductDisplayGroup> products = GetProductGroups().ToDictionary(product => product.ProductTag);
            foreach (ProductionKeepRule rule in KeepRules.Values.ToList())
            {
                if (rule.TargetAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    !products.TryGetValue(rule.ProductTag, out ProductDisplayGroup product))
                {
                    continue;
                }

                RecipeDisplayInfo route = product.Routes.FirstOrDefault(candidate => ProductionRecipeCatalog.GetRecipeKey(candidate.Recipe) == rule.RecipeKey);
                if (route.Recipe == null)
                {
                    route = product.Routes.FirstOrDefault();
                }

                if (route.Recipe == null)
                {
                    continue;
                }

                ProductionOrderRecord automaticOrder = FindAutomaticDuplicateOrder(rule.ProductTag, route.Recipe);
                float stockAmount = GetProducedAmountForOrder(rule.ProductTag);
                float otherCommittedAmount = ActiveOrders.Values
                    .Where(order => order != automaticOrder &&
                                    order.ProductTag == rule.ProductTag &&
                                    IsOrderActive(order))
                    .Sum(order => Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit));
                float missingAmount = Mathf.Max(0f, rule.TargetAmount - stockAmount - otherCommittedAmount);
                if (automaticOrder != null)
                {
                    // Keep the existing order stable while stock remains below the target.
                    // Replanning for every partial deposit causes cancel/recreate thrashing.
                    if (missingAmount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    CancelKeepRuleOrder(automaticOrder, currentCycle);
                }

                if (missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                SubmitOrder(product, route, missingAmount, currentCycle, true);
            }
        }

        private static void CancelKeepRuleOrder(ProductionOrderRecord order, float currentCycle)
        {
            if (order == null || !order.IsAutomatic || !IsOrderActive(order))
            {
                return;
            }

            CancelOrderQueues(order);
            ReleaseOrderAutomation(order.Key);
            order.State = ProductionOrderState.Cancelled;
            order.CompletedCycle = currentCycle;
            order.AbnormalReason = string.Empty;
        }
    }
}
