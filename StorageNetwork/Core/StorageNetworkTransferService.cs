using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkTransferService
    {
        public static bool TryPull(Storage destination, Tag tag, float amount, out float transferred)
        {
            transferred = 0f;
            if (destination == null || !tag.IsValid || amount <= 0f)
            {
                return false;
            }

            if (!StorageNetworkRegistry.CanPullFromNetwork(destination))
            {
                return false;
            }

            foreach (Storage source in GetCandidateSources(destination, tag))
            {
                if (transferred >= amount)
                {
                    break;
                }

                float remaining = amount - transferred;
                float moved = source.Transfer(destination, tag, remaining, block_events: false, hide_popups: true);
                transferred += moved;
            }

            return transferred > 0f;
        }

        public static bool TryPullRecipeIngredients(Storage destination, ComplexRecipe recipe)
        {
            if (destination == null || recipe == null)
            {
                return false;
            }

            bool transferredAny = false;
            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                float missingAmount = ingredient.amount - destination.GetAmountAvailable(ingredient.material);
                if (missingAmount <= 0f)
                {
                    continue;
                }

                if (TryPull(destination, ingredient.material, missingAmount, out _))
                {
                    transferredAny = true;
                }
            }

            return transferredAny;
        }

        public static bool TryPullMissingAmounts(Storage destination, IDictionary<Tag, float> missingAmounts)
        {
            if (destination == null || missingAmounts == null)
            {
                return false;
            }

            bool transferredAny = false;
            foreach (Tag tag in missingAmounts.Keys.ToList())
            {
                float missingAmount = missingAmounts[tag];
                if (missingAmount <= 0f)
                {
                    continue;
                }

                if (TryPull(destination, tag, missingAmount, out float transferred))
                {
                    transferredAny = true;
                    missingAmounts[tag] = Mathf.Max(0f, missingAmount - transferred);
                }
            }

            return transferredAny;
        }

        public static bool HasRecipeIngredientsAvailable(Storage destination, ComplexRecipe recipe)
        {
            if (destination == null || recipe == null)
            {
                return false;
            }

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                float localAmount = destination.GetAmountAvailable(ingredient.material);
                float networkAmount = StorageNetworkRegistry.GetMassAvailable(destination, ingredient.material);
                if (ingredient.amount - localAmount - networkAmount >= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<Storage> GetCandidateSources(Storage destination, Tag tag)
        {
            return StorageNetworkRegistry.GetSharedStorages(destination)
                .Where(storage => storage.allowItemRemoval)
                .Where(storage => storage.GetAmountAvailable(tag) > 0f);
        }
    }
}
