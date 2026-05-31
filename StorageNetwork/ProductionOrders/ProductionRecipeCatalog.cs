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
            Dictionary<string, List<ComplexFabricator>> recipeFabricators = new Dictionary<string, List<ComplexFabricator>>();
            Dictionary<string, ComplexRecipe> recipeByKey = new Dictionary<string, ComplexRecipe>();
            StorageSceneRegistry.EnsureSceneSeeded();
            foreach (StorageNetworkEnrollment enrollment in StorageSceneRegistry.GetEnrollments())
            {
                if (enrollment == null || !enrollment.IncludedInSceneNetwork)
                {
                    continue;
                }

                ComplexFabricator fabricator = enrollment.GetComponent<ComplexFabricator>();
                if (fabricator == null)
                {
                    continue;
                }

                foreach (ComplexRecipe recipe in fabricator.GetRecipes())
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
                        GetProductTag(recipe));
                })
                .OrderBy(recipe => recipe.ProductName)
                .ThenBy(recipe => recipe.FabricatorName)
                .ThenBy(recipe => recipe.Name)
                .ToList();
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
