using System.Collections.Generic;
using System.Reflection;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class StorageNetworkFabricatorProgress
    {
        private static readonly FieldInfo RecipeQueueCountsField = typeof(ComplexFabricator).GetField("recipeQueueCounts", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int GetRecipeQueueCountSafe(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            if (fabricator == null || recipe == null)
            {
                return 0;
            }

            Dictionary<string, int> queueCounts = RecipeQueueCountsField?.GetValue(fabricator) as Dictionary<string, int>;
            if (queueCounts != null && queueCounts.TryGetValue(recipe.id, out int count))
            {
                return count;
            }

            return 0;
        }

        public static int GetFiniteRecipeQueueCountSafe(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            int queued = GetRecipeQueueCountSafe(fabricator, recipe);
            return queued == ComplexFabricator.QUEUE_INFINITE ? ComplexFabricator.MAX_QUEUE_SIZE : Mathf.Max(0, queued);
        }

        public static bool IsWorkingOnRecipe(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            if (fabricator == null || recipe == null)
            {
                return false;
            }

            StorageNetworkOrderProductionCenterFabricator orderCenter = fabricator as StorageNetworkOrderProductionCenterFabricator;
            if (orderCenter != null)
            {
                return orderCenter.GetWorkingCountForRecipe(recipe) > 0;
            }

            return fabricator.CurrentWorkingOrder == recipe;
        }

        public static float GetRecipeProgress(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            if (fabricator == null || recipe == null)
            {
                return 0f;
            }

            StorageNetworkOrderProductionCenterFabricator orderCenter = fabricator as StorageNetworkOrderProductionCenterFabricator;
            if (orderCenter != null)
            {
                return orderCenter.GetProgressForRecipe(recipe);
            }

            return fabricator.CurrentWorkingOrder == recipe ? Mathf.Clamp01(fabricator.OrderProgress) : 0f;
        }

        public static int GetWorkingCountForRecipe(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            StorageNetworkOrderProductionCenterFabricator orderCenter = fabricator as StorageNetworkOrderProductionCenterFabricator;
            if (orderCenter != null)
            {
                return orderCenter.GetWorkingCountForRecipe(recipe);
            }

            return fabricator != null && recipe != null && fabricator.CurrentWorkingOrder == recipe ? 1 : 0;
        }
    }
}
