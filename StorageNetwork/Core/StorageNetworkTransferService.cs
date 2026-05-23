using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
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
                float missingAmount = ingredient.amount - GetAmountAvailable(destination, ingredient);
                if (missingAmount <= 0f)
                {
                    continue;
                }

                if (TryPullIngredient(destination, ingredient, missingAmount))
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
                float localAmount = GetAmountAvailable(destination, ingredient);
                float networkAmount = GetNetworkAmountAvailable(destination, ingredient);
                if (ingredient.amount - localAmount - networkAmount >= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasMissingNetworkRecipeIngredients(ComplexFabricator fabricator)
        {
            StorageNetworkFabricatorSettings settings = fabricator != null
                ? fabricator.GetComponent<StorageNetworkFabricatorSettings>()
                : null;
            return settings != null &&
                settings.RequestIngredientsFromNetwork &&
                fabricator.DebugFetchLists != null &&
                fabricator.DebugFetchLists.Count > 0;
        }

        public static bool TryStoreProducedProducts(ComplexFabricator fabricator, IEnumerable<GameObject> products)
        {
            if (fabricator?.inStorage == null || products == null)
            {
                return false;
            }

            bool storedAny = false;
            foreach (GameObject product in products.Where(product => product != null).ToList())
            {
                if (TryStoreProducedProduct(fabricator, product))
                {
                    storedAny = true;
                }
            }

            return storedAny;
        }

        public static bool TryStoreRecipeResults(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            if (fabricator?.outStorage == null || recipe?.results == null)
            {
                return false;
            }

            bool storedAny = false;
            foreach (ComplexRecipe.RecipeElement result in recipe.results)
            {
                foreach (Tag tag in GetMaterialOptions(result))
                {
                    for (GameObject product = fabricator.outStorage.FindFirst(tag);
                        product != null && TryStoreProducedProduct(fabricator, product);
                        product = fabricator.outStorage.FindFirst(tag))
                    {
                        storedAny = true;
                    }
                }
            }

            return storedAny;
        }


        private static IEnumerable<Storage> GetCandidateSources(Storage destination, Tag tag)
        {
            return StorageNetworkRegistry.GetSharedStorages(destination)
                .Where(storage => storage.allowItemRemoval)
                .Where(storage => storage.GetAmountAvailable(tag) > 0f);
        }

        private static bool TryPullIngredient(Storage destination, ComplexRecipe.RecipeElement ingredient, float amount)
        {
            bool transferredAny = false;
            foreach (Tag tag in GetMaterialOptions(ingredient)
                .OrderByDescending(tag => destination.GetAmountAvailable(tag))
                .ThenByDescending(tag => StorageNetworkRegistry.GetMassAvailable(destination, tag)))
            {
                if (amount <= 0f)
                {
                    break;
                }

                if (TryPull(destination, tag, amount, out float transferred))
                {
                    transferredAny = true;
                    amount -= transferred;
                }
            }

            return transferredAny;
        }

        private static float GetAmountAvailable(Storage storage, ComplexRecipe.RecipeElement ingredient)
        {
            return GetMaterialOptions(ingredient).Sum(storage.GetAmountAvailable);
        }

        private static float GetNetworkAmountAvailable(Storage storage, ComplexRecipe.RecipeElement ingredient)
        {
            return GetMaterialOptions(ingredient).Sum(tag => StorageNetworkRegistry.GetMassAvailable(storage, tag));
        }

        private static IEnumerable<Tag> GetMaterialOptions(ComplexRecipe.RecipeElement ingredient)
        {
            if (ingredient.possibleMaterials != null && ingredient.possibleMaterials.Length > 0)
            {
                return ingredient.possibleMaterials.Where(tag => tag.IsValid);
            }

            return ingredient.material.IsValid
                ? new[] { ingredient.material }
                : Enumerable.Empty<Tag>();
        }

        private static bool TryStoreProducedProduct(ComplexFabricator fabricator, GameObject product)
        {
            PrimaryElement element = product.GetComponent<PrimaryElement>();
            if (element == null)
            {
                return false;
            }

            Tag tag = product.PrefabID();
            float mass = element.Mass;
            List<Storage> destinations = StorageNetworkRegistry.GetSharedStorages(fabricator.inStorage)
                .Where(storage => storage != null && storage != fabricator.inStorage && storage != fabricator.outStorage)
                .Where(storage => storage.RemainingCapacity() >= mass)
                .ToList();

            Storage destination = destinations
                .Where(storage => storage.GetAmountAvailable(tag) > 0f)
                .OrderByDescending(storage => storage.GetAmountAvailable(tag))
                .FirstOrDefault();

            destination = destination ?? destinations.FirstOrDefault();
            if (destination == null)
            {
                return false;
            }

            if (fabricator.outStorage != null && fabricator.outStorage.items.Contains(product))
            {
                return fabricator.outStorage.Transfer(product, destination, block_events: false, hide_popups: true);
            }

            destination.Store(product, hide_popups: true);
            return true;
        }
    }
}
