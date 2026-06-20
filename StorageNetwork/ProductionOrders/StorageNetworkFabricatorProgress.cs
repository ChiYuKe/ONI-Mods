using System.Collections.Generic;
using System.Reflection;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class StorageNetworkFabricatorProgress
    {
        private static readonly FieldInfo RecipeQueueCountsField = typeof(ComplexFabricator).GetField("recipeQueueCounts", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Dictionary<int, Dictionary<string, int>> QueueCountsByFabricator = new Dictionary<int, Dictionary<string, int>>();
        private static bool cacheEnabled;

        public static void BeginRefresh()
        {
            QueueCountsByFabricator.Clear();
            cacheEnabled = true;
        }

        public static void Invalidate(ComplexFabricator fabricator)
        {
            int instanceId = ProductionOrderCenterCatalog.GetInstanceId(fabricator);
            if (instanceId != KPrefabID.InvalidInstanceID)
            {
                QueueCountsByFabricator.Remove(instanceId);
            }
        }

        public static int GetRecipeQueueCountSafe(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            if (fabricator == null || recipe == null)
            {
                return 0;
            }

            Dictionary<string, int> queueCounts = GetQueueCounts(fabricator);
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

        public static int GetFiniteTotalQueueCountSafe(ComplexFabricator fabricator)
        {
            int queued = GetTotalRecipeQueueCountSafe(fabricator);
            return queued == ComplexFabricator.QUEUE_INFINITE ? int.MaxValue : Mathf.Max(0, queued);
        }

        public static int GetTotalRecipeQueueCountSafe(ComplexFabricator fabricator)
        {
            Dictionary<string, int> queueCounts = GetQueueCounts(fabricator);
            if (queueCounts == null)
            {
                return 0;
            }

            int total = 0;
            foreach (int count in queueCounts.Values)
            {
                if (count == ComplexFabricator.QUEUE_INFINITE)
                {
                    return ComplexFabricator.QUEUE_INFINITE;
                }

                total += Mathf.Max(0, count);
            }

            if (fabricator.CurrentWorkingOrder != null)
            {
                total++;
            }

            return total;
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

        private static Dictionary<string, int> GetQueueCounts(ComplexFabricator fabricator)
        {
            if (fabricator == null || RecipeQueueCountsField == null)
            {
                return null;
            }

            int instanceId = ProductionOrderCenterCatalog.GetInstanceId(fabricator);
            if (cacheEnabled && instanceId != KPrefabID.InvalidInstanceID)
            {
                if (!QueueCountsByFabricator.TryGetValue(instanceId, out Dictionary<string, int> cached))
                {
                    cached = RecipeQueueCountsField.GetValue(fabricator) as Dictionary<string, int>;
                    QueueCountsByFabricator[instanceId] = cached;
                }

                return cached;
            }

            return RecipeQueueCountsField.GetValue(fabricator) as Dictionary<string, int>;
        }
    }
}
