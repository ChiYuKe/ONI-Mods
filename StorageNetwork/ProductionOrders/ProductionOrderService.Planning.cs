using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        public ProductionPlanNode BuildProductionPlan(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount)
        {
            return BuildProductionPlan(recipe, fabricators, productTag, requestedAmount, 0, new HashSet<string>(), null);
        }

        private ProductionPlanNode BuildProductionPlan(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount, int depth, HashSet<string> recipePath, HashSet<ComplexFabricator> reservedFabricators)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(recipe, productTag) ?? ProductionRecipeCatalog.GetPrimaryResult(recipe);
            float outputAmount = result != null ? Mathf.Max(result.amount, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) : 1f;
            int orderCount = Mathf.Max(1, Mathf.CeilToInt(requestedAmount / outputAmount));
            ProductionPlanNode node = new ProductionPlanNode(recipe, fabricators, productTag, outputAmount, orderCount);
            AssignPlan(node, reservedFabricators);
            string pathKey = BuildPlanPathKey(recipe, productTag);
            HashSet<string> childPath = recipePath != null ? new HashSet<string>(recipePath) : new HashSet<string>();
            childPath.Add(pathKey);
            if (recipe.ingredients == null || depth >= Config.Instance.ProductionPlanMaxDepth)
            {
                return node;
            }

            HashSet<ComplexFabricator> childReservedFabricators = MergeReservedFabricators(reservedFabricators, node.Assignments);

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                Tag tag = GetPreferredMaterial(ingredient, orderCount, depth, childPath, childReservedFabricators);
                float required = ingredient.amount * orderCount;
                float available = GetNetworkAvailableAmount(tag);
                ProductionPlanRequirement requirement = new ProductionPlanRequirement(tag, required, available);
                if (available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required)
                {
                    requirement.Child = BuildBestChildPlan(tag, required - available, depth + 1, childPath, childReservedFabricators);
                }

                node.Requirements.Add(requirement);
            }

            return node;
        }

        private static HashSet<ComplexFabricator> MergeReservedFabricators(HashSet<ComplexFabricator> reservedFabricators, List<ProductionPlanAssignment> assignments)
        {
            HashSet<ComplexFabricator> merged = reservedFabricators != null
                ? new HashSet<ComplexFabricator>(reservedFabricators)
                : new HashSet<ComplexFabricator>();
            foreach (ProductionPlanAssignment assignment in assignments ?? new List<ProductionPlanAssignment>())
            {
                if (assignment.Fabricator != null)
                {
                    merged.Add(assignment.Fabricator);
                }
            }

            return merged;
        }

        private static void AssignPlan(ProductionPlanNode node, HashSet<ComplexFabricator> reservedFabricators)
        {
            if (node == null)
            {
                return;
            }

            List<ComplexFabricator> available = node.Fabricators
                .Where(fabricator => fabricator != null && (reservedFabricators == null || !reservedFabricators.Contains(fabricator)))
                .ToList();
            if (available.Count == 0)
            {
                available = node.Fabricators.Where(fabricator => fabricator != null).ToList();
            }

            node.Assignments.Clear();
            node.Assignments.AddRange(BuildAssignmentsForFabricators(node.Recipe, available, node.OutputAmount, node.OrderCount));
        }

        private static List<ProductionPlanAssignment> BuildAssignmentsForFabricators(ComplexRecipe recipe, List<ComplexFabricator> fabricators, float outputAmount, int orderCount)
        {
            List<ProductionPlanAssignment> assignments = new List<ProductionPlanAssignment>();
            if (fabricators == null || fabricators.Count == 0 || orderCount <= 0)
            {
                return assignments;
            }

            List<ComplexFabricator> orderedFabricators = fabricators
                .Where(fabricator => fabricator != null)
                .OrderBy(GetFiniteTotalQueueCount)
                .ThenBy(fabricator => GetFiniteRecipeQueueCount(fabricator, recipe))
                .ThenBy(fabricator => fabricator.gameObject.GetProperName())
                .ToList();
            if (orderedFabricators.Count == 0)
            {
                return assignments;
            }

            int baseCount = orderCount / orderedFabricators.Count;
            int remainder = orderCount % orderedFabricators.Count;
            for (int i = 0; i < orderedFabricators.Count; i++)
            {
                int count = baseCount + (i < remainder ? 1 : 0);
                if (count > 0)
                {
                    assignments.Add(new ProductionPlanAssignment(orderedFabricators[i], count, outputAmount * count));
                }
            }

            return assignments;
        }

        private static int GetFiniteTotalQueueCount(ComplexFabricator fabricator)
        {
            int queued = GetTotalRecipeQueueCount(fabricator);
            return queued == ComplexFabricator.QUEUE_INFINITE ? int.MaxValue : Mathf.Max(0, queued);
        }

        private static int GetTotalRecipeQueueCount(ComplexFabricator fabricator)
        {
            if (fabricator == null || RecipeQueueCountsField == null)
            {
                return 0;
            }

            Dictionary<string, int> queueCounts = RecipeQueueCountsField.GetValue(fabricator) as Dictionary<string, int>;
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

        private Tag GetPreferredMaterial(ComplexRecipe.RecipeElement element, int orderCount, int depth, HashSet<string> recipePath, HashSet<ComplexFabricator> reservedFabricators)
        {
            if (element.material != Tag.Invalid)
            {
                return element.material;
            }

            if (element.possibleMaterials == null || element.possibleMaterials.Length == 0)
            {
                return Tag.Invalid;
            }

            float required = element.amount * orderCount;
            return element.possibleMaterials
                .Select(tag => new
                {
                    Tag = tag,
                    Available = GetNetworkAvailableAmount(tag),
                    Child = GetNetworkAvailableAmount(tag) + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required
                        ? BuildBestChildPlan(tag, required - GetNetworkAvailableAmount(tag), depth + 1, recipePath, reservedFabricators)
                        : null
                })
                .OrderBy(candidate => CountBlockedRequirements(candidate.Child))
                .ThenBy(candidate => candidate.Child == null && candidate.Available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required ? 1 : 0)
                .ThenBy(candidate => EstimateMissingAmount(candidate.Child))
                .ThenByDescending(candidate => candidate.Available)
                .ThenBy(candidate => ProductionOrderFormatting.GetTagDisplayName(candidate.Tag))
                .Select(candidate => candidate.Tag)
                .FirstOrDefault();
        }

        private ProductionPlanNode BuildBestChildPlan(Tag productTag, float missingAmount, int depth, HashSet<string> recipePath, HashSet<ComplexFabricator> reservedFabricators)
        {
            if (productTag == Tag.Invalid || missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT || depth > Config.Instance.ProductionPlanMaxDepth)
            {
                return null;
            }

            List<ProductionPlanNode> candidates = ProductionRecipeCatalog.FindConnectedRecipesProducing(craftableRecipes, productTag)
                .Where(route => route.Recipe != null && route.Fabricators.Count > 0 && !IsRecipeInPath(route.Recipe, productTag, recipePath))
                .Select(route => BuildProductionPlan(route.Recipe, route.Fabricators, productTag, missingAmount, depth, recipePath, reservedFabricators))
                .Where(plan => plan != null && plan.Assignments.Count > 0)
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates
                .OrderBy(CountBlockedRequirements)
                .ThenBy(EstimateMissingAmount)
                .ThenBy(CountProducedRequirements)
                .ThenBy(EstimateQueueLoad)
                .ThenBy(plan => plan.Recipe.GetUIName(false))
                .FirstOrDefault();
        }

        private static bool IsRecipeInPath(ComplexRecipe recipe, Tag productTag, HashSet<string> recipePath)
        {
            return recipePath != null && recipePath.Contains(BuildPlanPathKey(recipe, productTag));
        }

        private static string BuildPlanPathKey(ComplexRecipe recipe, Tag productTag)
        {
            return string.Format("{0}|{1}", ProductionRecipeCatalog.GetRecipeKey(recipe), productTag);
        }

        private static int GetFiniteRecipeQueueCount(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            int queued = fabricator != null && recipe != null ? fabricator.GetRecipeQueueCount(recipe) : 0;
            return queued == ComplexFabricator.QUEUE_INFINITE ? ComplexFabricator.MAX_QUEUE_SIZE : Mathf.Max(0, queued);
        }
    }
}
