using System.Collections.Generic;

namespace StorageNetwork.ProductionOrders
{
    internal sealed class ProductionOrderDraft
    {
        public ProductDisplayGroup Product { get; set; }

        public RecipeDisplayInfo Route { get; set; }

        public float RequestedAmount { get; set; }

        public float NetworkAvailableAmount { get; set; }

        public float NetworkRawAmount { get; set; }

        public float ReservedOutputAmount { get; set; }

        public ProductionPlanNode Plan { get; set; }

        public ProductionOrderRecord DuplicateOrder { get; set; }

        public ProductionOrderDuplicatePolicy DuplicatePolicy { get; set; }

        public ProductionOrderRiskLevel RiskLevel { get; set; }

        public List<string> ValidationMessages { get; } = new List<string>();

        public bool CanSubmit => Plan != null && Plan.Assignments.Count > 0 && RiskLevel != ProductionOrderRiskLevel.Blocked;

        public int TotalRequirementCount => CountRequirements(Plan, _ => true);

        public int BlockedRequirementCount => CountRequirements(Plan, requirement =>
            requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
            requirement.Child == null);

        public int ProducedRequirementCount => CountRequirements(Plan, requirement =>
            requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
            requirement.Child != null);

        private static int CountRequirements(ProductionPlanNode node, System.Func<ProductionPlanRequirement, bool> predicate)
        {
            if (node == null || predicate == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (predicate(requirement))
                {
                    count++;
                }

                count += CountRequirements(requirement.Child, predicate);
            }

            return count;
        }
    }
}
