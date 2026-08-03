using System.Collections.Generic;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed partial class StorageNetworkMaterialRequester
    {
        public void ForceStoreProducedOutputs(IEnumerable<GameObject> producedOutputs)
        {
            EnsureFabricator();
            if (!OutputStoreEnabled)
            {
                lastOutputStatus = string.Empty;
                lastRecipeResultTags.Clear();
                return;
            }

            HashSet<Tag> resultTags = GetCurrentRecipeResultTags();
            if (resultTags.Count > 0)
            {
                CopyTags(resultTags, lastRecipeResultTags);
            }
            else
            {
                resultTags = GetKnownRecipeResultTags();
            }

            float totalMoved = 0f;
            string lastBlockedItem = null;
            if (producedOutputs != null)
            {
                List<GameObject> outputs = producedOutputBuffer;
                outputs.Clear();
                foreach (GameObject output in producedOutputs)
                {
                    if (output != null)
                    {
                        outputs.Add(output);
                    }
                }

                for (int i = 0; i < outputs.Count; i++)
                {
                    GameObject output = outputs[i];
                    StorageTransferResult result = NetworkStorageTransferService.TransferLooseItemToNetwork(
                        output,
                        GetFabricatorStorages(),
                        GetSpecificOutputTarget(),
                        resultTags);
                    totalMoved += result.MovedKg;
                    if (!string.IsNullOrEmpty(result.BlockedItem))
                    {
                        lastBlockedItem = result.BlockedItem;
                    }
                }

                outputs.Clear();
            }

            StorageTransferResult outputStorageResult = StoreOutputsFromOutputStorage();
            totalMoved += outputStorageResult.MovedKg;
            if (!string.IsNullOrEmpty(outputStorageResult.BlockedItem))
            {
                lastBlockedItem = outputStorageResult.BlockedItem;
            }

            lastOutputStatus = NetworkStorageTransferService.FormatOutputStatus(
                new StorageTransferResult(totalMoved, lastBlockedItem),
                Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_OUTPUT));
        }

        private void StoreOutputsToNetwork()
        {
            if (!OutputStoreEnabled)
            {
                lastOutputStatus = string.Empty;
                outputStoreCooldown = 0f;
                return;
            }

            if (outputStoreCooldown > 0f)
            {
                outputStoreCooldown -= 1f;
                return;
            }

            StorageTransferResult result = StoreOutputsFromOutputStorage();
            lastOutputStatus = NetworkStorageTransferService.FormatOutputStatus(result, Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_PRODUCTS));
            outputStoreCooldown = result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? 0f : EmptyOutputRetrySeconds;
        }

        private StorageTransferResult StoreOutputsFromOutputStorage()
        {
            EnsureFabricator();
            if (fabricator.outStorage == null || fabricator.outStorage.items == null)
            {
                lastOutputStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_NO_OUTPUT_STORAGE);
                return StorageTransferResult.Idle;
            }

            return NetworkStorageTransferService.TransferStoredItemsToNetwork(
                fabricator.outStorage,
                GetFabricatorStorages(),
                GetSpecificOutputTarget(),
                GetAllowedOutputTags());
        }

        private HashSet<Tag> GetAllowedOutputTags()
        {
            HashSet<Tag> current = GetCurrentRecipeResultTags();
            if (current.Count > 0)
            {
                CopyTags(current, lastRecipeResultTags);
                return current;
            }

            return lastRecipeResultTags.Count > 0
                ? lastRecipeResultTags
                : GetKnownRecipeResultTags();
        }

        // 只把当前配方 results 里的真实产物入网，避免金属精炼器冷却液这类工艺介质被当作成品搬走。
        private HashSet<Tag> GetCurrentRecipeResultTags()
        {
            HashSet<Tag> tags = currentRecipeResultTagBuffer;
            tags.Clear();
            ComplexRecipe recipe = fabricator != null ? fabricator.CurrentWorkingOrder : null;
            if (recipe == null)
            {
                recipe = fabricator != null ? fabricator.NextOrder : null;
            }

            if (recipe?.results == null)
            {
                return tags;
            }

            foreach (ComplexRecipe.RecipeElement result in recipe.results)
            {
                if (result != null && result.material != Tag.Invalid)
                {
                    tags.Add(result.material);
                }
            }

            return tags;
        }

        private HashSet<Tag> GetKnownRecipeResultTags()
        {
            HashSet<Tag> tags = knownRecipeResultTags;
            if (knownRecipeResultTagsBuilt || fabricator == null)
            {
                return tags;
            }

            tags.Clear();

            foreach (ComplexRecipe recipe in fabricator.GetRecipes())
            {
                if (recipe?.results == null)
                {
                    continue;
                }

                foreach (ComplexRecipe.RecipeElement result in recipe.results)
                {
                    if (result != null && result.material != Tag.Invalid)
                    {
                        tags.Add(result.material);
                    }
                }
            }

            knownRecipeResultTagsBuilt = true;
            return tags;
        }

        private static void CopyTags(HashSet<Tag> source, HashSet<Tag> destination)
        {
            destination.Clear();
            foreach (Tag tag in source)
            {
                destination.Add(tag);
            }
        }

        private Storage GetSpecificOutputTarget()
        {
            return CurrentOutputStoreMode == OutputStoreMode.SpecificStorage
                ? ResolveOutputStorage()
                : null;
        }
    }
}
