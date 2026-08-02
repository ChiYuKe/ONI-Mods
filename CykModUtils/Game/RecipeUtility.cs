using System;
using System.Collections.Generic;

namespace CykModUtils.Game
{
    /// <summary>
    /// 创建 ONI ComplexRecipe 的常用辅助方法。
    /// </summary>
    public static class RecipeUtility
    {
        /// <summary>
        /// 创建一个配方；相同 ID 的配方已经存在时默认返回现有实例。
        /// </summary>
        public static ComplexRecipe Create(
            ComplexRecipe.RecipeElement[] ingredients,
            ComplexRecipe.RecipeElement[] results,
            string fabricatorId,
            float productionTime,
            string description,
            ComplexRecipe.RecipeNameDisplay nameDisplay =
                ComplexRecipe.RecipeNameDisplay.Result,
            int sortOrder = 0,
            string requiredTech = null,
            bool reuseExisting = true)
        {
            ValidateElements(ingredients, nameof(ingredients));
            ValidateElements(results, nameof(results));
            if (string.IsNullOrWhiteSpace(fabricatorId))
            {
                throw new ArgumentException("Fabricator ID cannot be empty.", nameof(fabricatorId));
            }

            string recipeId = ComplexRecipeManager.MakeRecipeID(
                fabricatorId,
                ingredients,
                results);
            if (reuseExisting)
            {
                ComplexRecipe existing = ComplexRecipeManager.Get().GetRecipe(recipeId);
                if (existing != null)
                {
                    return existing;
                }
            }

            return new ComplexRecipe(recipeId, ingredients, results)
            {
                time = Math.Max(0f, productionTime),
                description = description ?? string.Empty,
                nameDisplay = nameDisplay,
                fabricators = new List<Tag> { fabricatorId },
                sortOrder = sortOrder,
                requiredTech = requiredTech
            };
        }

        /// <summary>创建一个配方元素。</summary>
        public static ComplexRecipe.RecipeElement Element(Tag material, float amount)
        {
            if (!material.IsValid)
            {
                throw new ArgumentException("Recipe material tag is invalid.", nameof(material));
            }

            if (amount <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "Recipe amount must be greater than zero.");
            }

            return new ComplexRecipe.RecipeElement(material, amount);
        }

        private static void ValidateElements(
            ComplexRecipe.RecipeElement[] elements,
            string parameterName)
        {
            if (elements == null || elements.Length == 0)
            {
                throw new ArgumentException(
                    "Recipe elements cannot be null or empty.",
                    parameterName);
            }

            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == null)
                {
                    throw new ArgumentException(
                        "Recipe elements cannot contain null values.",
                        parameterName);
                }
            }
        }
    }
}
