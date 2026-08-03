using System.Collections.Generic;
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

            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(route.Recipe);
            if (KeepRules.TryGetValue(product.ProductTag, out ProductionKeepRule existing) &&
                existing.ProductName == product.ProductName &&
                existing.RecipeKey == recipeKey &&
                existing.TargetAmount == targetAmount)
            {
                return;
            }

            KeepRules[product.ProductTag] = new ProductionKeepRule(
                product.ProductTag,
                product.ProductName,
                recipeKey,
                targetAmount);
            MarkOrdersChanged();
        }

        public void ClearKeepRule(Tag productTag)
        {
            if (KeepRules.Remove(productTag))
            {
                MarkOrdersChanged();
            }
        }

        private void RunKeepRules()
        {
            if (KeepRules.Count == 0 || craftableRecipes.Count == 0)
            {
                return;
            }

            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            foreach (ProductionKeepRule rule in KeepRules.Values)
            {
                if (rule.TargetAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                int keepWorldId = GetCurrentNetworkWorldId();
                RecipeDisplayInfo[] routes = GetCraftableRoutesProducing(rule.ProductTag);
                RecipeDisplayInfo route = default;
                for (int routeIndex = 0; routeIndex < routes.Length; routeIndex++)
                {
                    RecipeDisplayInfo candidate = routes[routeIndex];
                    if (ProductionRecipeCatalog.GetRecipeKey(candidate.Recipe) == rule.RecipeKey &&
                        IsRouteReachableFromWorld(candidate, keepWorldId))
                    {
                        route = candidate;
                        break;
                    }
                }

                if (route.Recipe == null)
                {
                    for (int routeIndex = 0; routeIndex < routes.Length; routeIndex++)
                    {
                        RecipeDisplayInfo candidate = routes[routeIndex];
                        if (IsRouteReachableFromWorld(candidate, keepWorldId))
                        {
                            route = candidate;
                            break;
                        }
                    }
                }

                if (route.Recipe == null)
                {
                    continue;
                }

                ProductionOrderRecord automaticOrder = FindAutomaticDuplicateOrder(rule.ProductTag, route.Recipe);
                float stockAmount = GetProducedAmountForOrder(rule.ProductTag);
                float otherCommittedAmount = 0f;
                foreach (ProductionOrderRecord order in ActiveOrders.Values)
                {
                    if (order != automaticOrder &&
                        order.ProductTag == rule.ProductTag &&
                        IsOrderActive(order) &&
                        IsOrderReachableFromCurrentWorld(order))
                    {
                        otherCommittedAmount +=
                            Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit);
                    }
                }
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

                // Keep-rule submission is an exceptional path. Constructing the
                // single-row display group here keeps the no-op maintenance tick
                // allocation free without changing the public submission model.
                List<RecipeDisplayInfo> selectedRoutes = new List<RecipeDisplayInfo>(1)
                {
                    route
                };
                ProductDisplayGroup product =
                    new ProductDisplayGroup(route.ProductKey, selectedRoutes);
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
            MarkOrdersChanged();
        }
    }
}
