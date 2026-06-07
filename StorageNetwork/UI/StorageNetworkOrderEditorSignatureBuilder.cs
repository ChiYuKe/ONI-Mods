using System.Linq;
using StorageNetwork.ProductionOrders;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkOrderEditorSignatureBuilder
    {
        public static string Build(
            ProductDisplayGroup product,
            RecipeDisplayInfo route,
            ProductionOrderDraft draft,
            ProductionKeepRule keepRule,
            int selectedRouteIndex,
            float requestedProductAmount,
            string lastOrderStatus)
        {
            string assignments = draft.Plan == null
                ? string.Empty
                : string.Join(",", draft.Plan.Assignments
                    .OrderBy(assignment => assignment.Fabricator != null ? assignment.Fabricator.GetInstanceID() : 0)
                    .Select(assignment => string.Format("{0}:{1}:{2:0.###}",
                        assignment.Fabricator != null ? assignment.Fabricator.GetInstanceID().ToString() : string.Empty,
                        assignment.OrderCount,
                        assignment.OutputAmount)));

            string requirements = draft.Plan == null
                ? string.Empty
                : BuildRequirementSignature(draft.Plan, 0);

            string validation = string.Join(",", draft.ValidationMessages);

            return string.Format(
                "{0}|{1}|{2}|{3:0.###}|{4}|{5}|{6}|{7}|{8}|{9}|{10}",
                product.ProductKey,
                ProductionRecipeCatalog.GetRecipeKey(route.Recipe),
                selectedRouteIndex,
                requestedProductAmount,
                keepRule != null ? keepRule.RecipeKey : string.Empty,
                keepRule != null ? keepRule.TargetAmount.ToString("0.###") : string.Empty,
                draft.RiskLevel,
                draft.DuplicateOrder != null ? draft.DuplicateOrder.Key : string.Empty,
                lastOrderStatus ?? string.Empty,
                assignments,
                requirements + "|" + validation);
        }

        private static string BuildRequirementSignature(ProductionPlanNode node, int depth)
        {
            if (node == null || depth > 4)
            {
                return string.Empty;
            }

            string requirements = string.Join(",", node.Requirements
                .OrderBy(requirement => requirement.Material.ToString())
                .Select(requirement => string.Format("{0}:{1:0.###}:{2:0.###}:{3}",
                    requirement.Material,
                    requirement.RequiredAmount,
                    requirement.AvailableAmount,
                    BuildRequirementSignature(requirement.Child, depth + 1))));

            return string.Format("{0}:{1}:{2:0.###}[{3}]",
                node.Recipe != null ? node.Recipe.id : string.Empty,
                node.OrderCount,
                node.OutputAmount,
                requirements);
        }
    }
}
