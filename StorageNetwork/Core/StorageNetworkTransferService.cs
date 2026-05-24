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

        public static bool TryPullFabricatorQueuedIngredients(ComplexFabricator fabricator)
        {
            if (fabricator?.inStorage == null)
            {
                return false;
            }

            bool transferredAny = false;
            ComplexRecipe currentRecipe = fabricator.CurrentWorkingOrder;
            if (currentRecipe != null && TryPullRecipeIngredients(fabricator.inStorage, currentRecipe))
            {
                transferredAny = true;
            }

            ComplexRecipe[] recipes = fabricator.GetRecipes();
            if (recipes == null)
            {
                return transferredAny;
            }

            foreach (ComplexRecipe recipe in recipes)
            {
                int prefetchCount = fabricator.GetRecipePrefetchCount(recipe);
                if (prefetchCount <= 0)
                {
                    continue;
                }

                foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
                {
                    float targetAmount = ingredient.amount * prefetchCount;
                    float missingAmount = targetAmount - GetAmountAvailable(fabricator.inStorage, ingredient);
                    if (missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    if (TryPullIngredient(fabricator.inStorage, ingredient, missingAmount))
                    {
                        transferredAny = true;
                    }
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

        public static bool TryGetMissingNetworkRecipeIngredient(ComplexFabricator fabricator, out Tag missingTag)
        {
            missingTag = Tag.Invalid;
            StorageNetworkFabricatorSettings settings = fabricator != null
                ? fabricator.GetComponent<StorageNetworkFabricatorSettings>()
                : null;
            if (settings == null ||
                !settings.RequestIngredientsFromNetwork ||
                fabricator.inStorage == null ||
                fabricator.DebugFetchLists == null)
            {
                return false;
            }

            foreach (FetchList2 fetchList in fabricator.DebugFetchLists)
            {
                if (fetchList == null)
                {
                    continue;
                }

                foreach (KeyValuePair<Tag, float> missingAmount in fetchList.GetRemainingMinimum())
                {
                    if (missingAmount.Value >= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                        StorageNetworkRegistry.GetMassAvailable(fabricator.inStorage, missingAmount.Key) < PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        missingTag = missingAmount.Key;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryPullElementConverterInputs(Storage destination, ElementConverter converter)
        {
            if (destination == null || converter?.consumedElements == null)
            {
                return false;
            }

            bool transferredAny = false;
            foreach (ElementConverter.ConsumedElement consumed in converter.consumedElements)
            {
                if (!consumed.IsActive || !consumed.Tag.IsValid)
                {
                    continue;
                }

                float targetAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, consumed.MassConsumptionRate * 2f);
                float missingAmount = targetAmount - destination.GetAmountAvailable(consumed.Tag);
                if (missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                if (TryPull(destination, consumed.Tag, missingAmount, out _))
                {
                    transferredAny = true;
                }
            }

            return transferredAny;
        }

        public static bool TryGetMissingNetworkElementConverterInput(Storage destination, ElementConverter converter, out Tag missingTag)
        {
            missingTag = Tag.Invalid;
            if (destination == null || converter?.consumedElements == null)
            {
                return false;
            }

            foreach (ElementConverter.ConsumedElement consumed in converter.consumedElements)
            {
                if (!consumed.IsActive || !consumed.Tag.IsValid)
                {
                    continue;
                }

                if (destination.GetAmountAvailable(consumed.Tag) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                    StorageNetworkRegistry.GetMassAvailable(destination, consumed.Tag) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    missingTag = consumed.Tag;
                    return true;
                }
            }

            return false;
        }

        public static bool TryStoreElementConverterOutputs(Storage source, ElementConverter converter)
        {
            if (source == null || converter?.outputElements == null)
            {
                return false;
            }

            bool storedAny = false;
            foreach (ElementConverter.OutputElement output in converter.outputElements)
            {
                if (!output.IsActive || !output.storeOutput || output.elementHash == SimHashes.Vacuum)
                {
                    continue;
                }

                Tag outputTag = output.elementHash.CreateTag();
                while (TryStoreFromStorage(source, outputTag))
                {
                    storedAny = true;
                }
            }

            return storedAny;
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

        public static bool TryPullPlantingMaterials(Storage destination, SingleEntityReceptacle receptacle)
        {
            if (destination == null)
            {
                return false;
            }

            bool transferredAny = false;
            foreach (ManualDeliveryKG delivery in GetPlantingDeliveries(destination, receptacle))
            {
                if (TryPullManualDelivery(destination, delivery))
                {
                    transferredAny = true;
                }
            }

            if (TryPullReceptacleSeed(destination, receptacle))
            {
                transferredAny = true;
            }

            if (transferredAny)
            {
                destination.Trigger((int)GameHashes.OnStorageChange, destination);
            }

            return transferredAny;
        }


        private static IEnumerable<Storage> GetCandidateSources(Storage destination, Tag tag)
        {
            return StorageNetworkRegistry.GetSharedStorages(destination)
                .Where(storage => storage.allowItemRemoval)
                .Where(storage => storage.GetAmountAvailable(tag) > 0f);
        }

        private static IEnumerable<ManualDeliveryKG> GetPlantingDeliveries(Storage destination, SingleEntityReceptacle receptacle)
        {
            foreach (ManualDeliveryKG delivery in destination.GetComponents<ManualDeliveryKG>())
            {
                if (delivery != null)
                {
                    yield return delivery;
                }
            }

            GameObject occupant = receptacle?.Occupant;
            if (occupant == null)
            {
                yield break;
            }

            foreach (ManualDeliveryKG delivery in occupant.GetComponents<ManualDeliveryKG>())
            {
                if (delivery != null && delivery.DebugStorage == destination)
                {
                    yield return delivery;
                }
            }
        }

        private static bool TryPullManualDelivery(Storage destination, ManualDeliveryKG delivery)
        {
            if (delivery == null || delivery.DebugStorage != destination)
            {
                return false;
            }

            Tag requestedTag = delivery.RequestedItemTag;
            if (!requestedTag.IsValid)
            {
                return false;
            }

            float missingAmount = delivery.Capacity - destination.GetAmountAvailable(requestedTag);
            if (missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return false;
            }

            bool pulled = TryPull(destination, requestedTag, missingAmount, out _);
            if (pulled)
            {
                delivery.UpdateDeliveryState();
            }

            return pulled;
        }

        private static bool TryPullReceptacleSeed(Storage destination, SingleEntityReceptacle receptacle)
        {
            if (receptacle == null ||
                receptacle.Occupant != null ||
                receptacle.GetActiveRequest == null ||
                !receptacle.requestedEntityTag.IsValid ||
                receptacle.requestedEntityTag == GameTags.Empty)
            {
                return false;
            }

            GameObject seed = FindReceptacleSeed(destination, receptacle);
            if (seed == null)
            {
                float amount = GetPrefabMass(receptacle.requestedEntityTag);
                TryPull(destination, receptacle.requestedEntityTag, amount, out _);
                seed = FindReceptacleSeed(destination, receptacle);
            }

            if (seed == null)
            {
                return false;
            }

            receptacle.ForceDeposit(seed);
            return true;
        }

        private static GameObject FindReceptacleSeed(Storage destination, SingleEntityReceptacle receptacle)
        {
            Tag requestedTag = receptacle.requestedEntityTag;
            Tag additionalTag = receptacle.requestedEntityAdditionalFilterTag;
            foreach (GameObject item in destination.items)
            {
                if (item == null)
                {
                    continue;
                }

                KPrefabID prefabId = item.GetComponent<KPrefabID>();
                if (prefabId == null ||
                    !prefabId.HasTag(requestedTag) ||
                    (additionalTag.IsValid && additionalTag != GameTags.Empty && !prefabId.HasTag(additionalTag)) ||
                    !receptacle.IsValidEntity(item))
                {
                    continue;
                }

                return item;
            }

            return null;
        }

        private static float GetPrefabMass(Tag tag)
        {
            GameObject prefab = Assets.GetPrefab(tag);
            PrimaryElement element = prefab != null ? prefab.GetComponent<PrimaryElement>() : null;
            return Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, element != null ? element.MassPerUnit : 1f);
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

            return TryStoreProduct(fabricator.inStorage, product, fabricator.outStorage);
        }

        private static bool TryStoreFromStorage(Storage source, Tag tag)
        {
            GameObject product = source.FindFirst(tag);
            return product != null && TryStoreProduct(source, product, null);
        }

        private static bool TryStoreProduct(Storage source, GameObject product, Storage excludedStorage)
        {
            PrimaryElement element = product.GetComponent<PrimaryElement>();
            if (source == null || element == null)
            {
                return false;
            }

            Tag tag = product.PrefabID();
            float mass = element.Mass;
            List<Storage> destinations = StorageNetworkRegistry.GetSharedStorages(source)
                .Where(storage => storage != null && storage != source && storage != excludedStorage)
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

            if (source.items.Contains(product))
            {
                return source.Transfer(product, destination, block_events: false, hide_popups: true);
            }

            destination.Store(product, hide_popups: true);
            return true;
        }
    }
}
