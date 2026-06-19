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

                if (FindAutomaticDuplicateOrder(rule.ProductTag, route.Recipe) != null)
                {
                    continue;
                }

                float committedAmount = GetProducedAmountForOrder(rule.ProductTag) + GetPendingProducedAmountAhead(rule.ProductTag);
                float missingAmount = rule.TargetAmount - committedAmount;
                if (missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                SubmitOrder(product, route, missingAmount, currentCycle, true);
            }
        }
    }
}
