using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class ProductionRecipeCatalog
    {
        public static List<RecipeDisplayInfo> GetCraftableRecipeDisplayInfos()
        {
            return GetCraftableRecipeDisplayInfos(GetOrderProductionCenters());
        }

        public static List<RecipeDisplayInfo> GetCraftableRecipeDisplayInfos(IEnumerable<StorageNetworkOrderProductionCenter> centers)
        {
            Dictionary<string, List<ComplexFabricator>> recipeFabricators = new Dictionary<string, List<ComplexFabricator>>();
            Dictionary<string, ComplexRecipe> recipeByKey = new Dictionary<string, ComplexRecipe>();
            StorageSceneRegistry.EnsureSceneSeeded();
            foreach (StorageNetworkOrderProductionCenter center in centers ?? Enumerable.Empty<StorageNetworkOrderProductionCenter>())
            {
                ComplexFabricator fabricator = center != null ? center.GetComponent<ComplexFabricator>() : null;
                if (!ProductionOrderService.IsOrderProductionFabricator(fabricator))
                {
                    continue;
                }

                foreach (ComplexRecipe recipe in GetRecipesForOrderCenter(center))
                {
                    if (recipe == null || !IsRecipeVisible(fabricator, recipe))
                    {
                        continue;
                    }

                    string key = GetRecipeKey(recipe);
                    if (!recipeFabricators.TryGetValue(key, out List<ComplexFabricator> fabricators))
                    {
                        fabricators = new List<ComplexFabricator>();
                        recipeFabricators.Add(key, fabricators);
                        recipeByKey.Add(key, recipe);
                    }

                    fabricators.Add(fabricator);
                }
            }

            return recipeFabricators
                .Select(pair =>
                {
                    ComplexRecipe recipe = recipeByKey[pair.Key];
                    List<ComplexFabricator> fabricators = pair.Value.OrderBy(fabricator => fabricator.gameObject.GetProperName()).ToList();
                    return new RecipeDisplayInfo(
                        recipe.GetUIName(false),
                        ProductionOrderFormatting.FormatFabricatorGroupName(fabricators),
                        ProductionOrderFormatting.FormatRecipeDetails(recipe),
                        recipe,
                        fabricators,
                        recipe.GetUIIcon(),
                        GetProductKey(recipe),
                        GetProductDisplayName(recipe),
                        GetProductTag(recipe),
                        fabricators
                            .Select(fabricator => StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject))
                            .Where(worldId => worldId >= 0)
                            .Distinct()
                            .OrderBy(worldId => worldId)
                            .ToList());
                })
                .OrderBy(recipe => recipe.ProductName)
                .ThenBy(recipe => recipe.FabricatorName)
                .ThenBy(recipe => recipe.Name)
                .ToList();
        }

        public static List<RecipeDisplayInfo> GetCraftableRecipeDisplayInfos(StorageNetworkOrderProductionCenter center)
        {
            ComplexFabricator fabricator = center != null ? center.GetComponent<ComplexFabricator>() : null;
            if (center == null || !ProductionOrderService.IsOrderProductionFabricator(fabricator))
            {
                return new List<RecipeDisplayInfo>();
            }

            return center.EngravedRecipeIds
                .Select(id => ComplexRecipeManager.Get().GetRecipe(id))
                .Where(recipe => recipe != null && Game.IsCorrectDlcActiveForCurrentSave(recipe) && IsRecipeVisible(fabricator, recipe))
                .GroupBy(GetRecipeKey)
                .Select(group =>
                {
                    ComplexRecipe recipe = group.First();
                    List<ComplexFabricator> fabricators = new List<ComplexFabricator> { fabricator };
                    return new RecipeDisplayInfo(
                        recipe.GetUIName(false),
                        ProductionOrderFormatting.FormatFabricatorGroupName(fabricators),
                        ProductionOrderFormatting.FormatRecipeDetails(recipe),
                        recipe,
                        fabricators,
                        recipe.GetUIIcon(),
                        GetProductKey(recipe),
                        GetProductDisplayName(recipe),
                        GetProductTag(recipe),
                        new List<int> { StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject) }
                            .Where(worldId => worldId >= 0)
                            .ToList());
                })
                .OrderBy(recipe => recipe.ProductName)
                .ThenBy(recipe => recipe.Name)
                .ToList();
        }

        private static List<StorageNetworkOrderProductionCenter> GetOrderProductionCenters()
        {
            StorageSceneRegistry.EnsureSceneSeeded();
            return ProductionOrderCenterCatalog.GetCenters();
        }

        private static IEnumerable<ComplexRecipe> GetRecipesForOrderCenter(StorageNetworkOrderProductionCenter center)
        {
            return center != null
                ? center.EngravedRecipeIds
                    .Select(id => ComplexRecipeManager.Get().GetRecipe(id))
                    .Where(recipe => recipe != null)
                : Enumerable.Empty<ComplexRecipe>();
        }

        public static List<ProductDisplayGroup> BuildProductGroups(List<RecipeDisplayInfo> recipes)
        {
            return recipes
                .GroupBy(recipe => recipe.ProductKey)
                .Select(group => new ProductDisplayGroup(group.Key, group.OrderBy(recipe => recipe.FabricatorName).ThenBy(recipe => recipe.Name).ToList()))
                .OrderBy(group => group.ProductName)
                .ToList();
        }

        public static RecipeDisplayInfo FindConnectedRecipeProducing(IEnumerable<RecipeDisplayInfo> recipes, Tag tag)
        {
            return recipes
                .FirstOrDefault(info => info.Recipe != null && info.Recipe.results != null && info.Recipe.results.Any(result => result != null && result.material == tag));
        }

        public static List<RecipeDisplayInfo> FindConnectedRecipesProducing(IEnumerable<RecipeDisplayInfo> recipes, Tag tag)
        {
            return recipes == null
                ? new List<RecipeDisplayInfo>()
                : recipes
                    .Where(info => info.Recipe != null && info.Recipe.results != null && info.Recipe.results.Any(result => result != null && result.material == tag))
                    .ToList();
        }

        public static ComplexRecipe.RecipeElement GetPrimaryResult(ComplexRecipe recipe)
        {
            return recipe?.results?.FirstOrDefault();
        }

        public static ComplexRecipe.RecipeElement GetRecipeResultForProduct(ComplexRecipe recipe, Tag productTag)
        {
            if (recipe?.results == null || productTag == Tag.Invalid)
            {
                return null;
            }

            return recipe.results.FirstOrDefault(result => result != null && result.material == productTag);
        }

        public static string GetRecipeKey(ComplexRecipe recipe)
        {
            return recipe?.id ?? recipe?.GetUIName(false) ?? string.Empty;
        }

        private static Tag GetRecipeResultTag(ComplexRecipe.RecipeElement result)
        {
            return result != null && result.material != Tag.Invalid ? result.material : Tag.Invalid;
        }

        private static bool IsRecipeVisible(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            if (DebugHandler.InstantBuildMode)
            {
                return true;
            }

            if (!recipe.IsRequiredTechUnlocked())
            {
                return false;
            }

            return recipe.RequiresAllIngredientsDiscovered
                ? AreAllIngredientsDiscovered(recipe)
                : IsAnyIngredientDiscovered(recipe);
        }

        private static bool IsAnyIngredientDiscovered(ComplexRecipe recipe)
        {
            if (recipe?.ingredients == null || recipe.ingredients.Length == 0)
            {
                return true;
            }

            return recipe.ingredients.Any(IsIngredientDiscovered);
        }

        private static bool AreAllIngredientsDiscovered(ComplexRecipe recipe)
        {
            if (recipe?.ingredients == null || recipe.ingredients.Length == 0)
            {
                return true;
            }

            return recipe.ingredients.All(IsIngredientDiscovered);
        }

        private static bool IsIngredientDiscovered(ComplexRecipe.RecipeElement ingredient)
        {
            if (ingredient == null)
            {
                return false;
            }

            if (ingredient.material != Tag.Invalid && DiscoveredResources.Instance.IsDiscovered(ingredient.material))
            {
                return true;
            }

            return ingredient.possibleMaterials != null &&
                   ingredient.possibleMaterials.Any(tag => tag != Tag.Invalid && DiscoveredResources.Instance.IsDiscovered(tag));
        }

        private static string GetProductKey(ComplexRecipe recipe)
        {
            ComplexRecipe.RecipeElement result = GetPrimaryResult(recipe);
            if (result == null)
            {
                return recipe?.id ?? string.Empty;
            }

            return !string.IsNullOrEmpty(result.facadeID) ? result.facadeID : result.material.Name;
        }

        private static Tag GetProductTag(ComplexRecipe recipe)
        {
            return GetRecipeResultTag(GetPrimaryResult(recipe));
        }

        private static string GetProductDisplayName(ComplexRecipe recipe)
        {
            ComplexRecipe.RecipeElement result = GetPrimaryResult(recipe);
            if (result == null)
            {
                return recipe?.GetUIName(false) ?? string.Empty;
            }

            return !string.IsNullOrEmpty(result.facadeID)
                ? ProductionOrderFormatting.GetTagDisplayName(result.facadeID.ToTag())
                : ProductionOrderFormatting.GetTagDisplayName(result.material);
        }
    }
}
