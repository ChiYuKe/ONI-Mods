using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkPlanPreviewText
    {
        public static float GetMissingAmount(ProductionPlanRequirement requirement)
        {
            return Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
        }

        public static bool IsCoveredByNetwork(ProductionPlanRequirement requirement)
        {
            return GetMissingAmount(requirement) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        public static bool CanProduceRequirement(ProductionPlanRequirement requirement)
        {
            return !IsCoveredByNetwork(requirement) && requirement.Child != null;
        }

        public static string BuildAssignmentSummary(ProductionPlanNode node, int maxItems)
        {
            if (node == null || node.Assignments == null || node.Assignments.Count == 0)
            {
                return node != null ? node.FabricatorName : "?";
            }

            List<string> names = node.Assignments
                .Take(maxItems)
                .Select(assignment => string.Format("{0} x{1}", assignment.Fabricator != null ? assignment.Fabricator.GetProperName() : "?", assignment.OrderCount))
                .ToList();
            if (node.Assignments.Count > maxItems)
            {
                names.Add("+" + (node.Assignments.Count - maxItems));
            }

            return string.Join(" / ", names.ToArray());
        }

        public static string BuildResearchAmountText(ProductionPlanRequirement requirement, bool spacedRatio)
        {
            if (IsCoveredByNetwork(requirement))
            {
                string separator = spacedRatio ? " / " : "/";
                return string.Format("{0}{1}{2}", GameUtil.GetFormattedMass(requirement.AvailableAmount), separator, GameUtil.GetFormattedMass(requirement.RequiredAmount));
            }

            return string.Format(
                StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RESEARCH_MISSING_STOCK),
                GameUtil.GetFormattedMass(GetMissingAmount(requirement)),
                GameUtil.GetFormattedMass(requirement.AvailableAmount));
        }

        public static string BuildRequirementStockLine(ProductionPlanRequirement requirement)
        {
            if (IsCoveredByNetwork(requirement))
            {
                return string.Format(
                    StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_REQUIREMENT_STOCK),
                    GameUtil.GetFormattedMass(requirement.RequiredAmount),
                    GameUtil.GetFormattedMass(requirement.AvailableAmount));
            }

            return string.Format(
                StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_REQUIREMENT_MISSING),
                GameUtil.GetFormattedMass(requirement.RequiredAmount),
                GameUtil.GetFormattedMass(GetMissingAmount(requirement)));
        }

        public static string BuildRequirementActionLine(ProductionPlanRequirement requirement, int maxAssignments)
        {
            if (IsCoveredByNetwork(requirement))
            {
                return StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ACTION_DIRECT);
            }

            if (requirement.Child != null)
            {
                return string.Format(
                    StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ACTION_AUTO),
                    BuildAssignmentSummary(requirement.Child, maxAssignments));
            }

            return StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ACTION_BLOCKED);
        }

        public static string BuildLedgerStockText(ProductionPlanRequirement requirement)
        {
            if (IsCoveredByNetwork(requirement))
            {
                return GameUtil.GetFormattedMass(requirement.AvailableAmount);
            }

            return string.Format(
                StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MISSING_PREFIX),
                GameUtil.GetFormattedMass(GetMissingAmount(requirement)));
        }

        public static string GetDispatchStatusLabel(ProductionPlanRequirement requirement)
        {
            if (IsCoveredByNetwork(requirement))
            {
                return StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DIRECT);
            }

            return requirement.Child != null
                ? StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_AUTO)
                : StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_BLOCKED);
        }

        public static string GetDispatchFlowStatusLabel(ProductionPlanRequirement requirement)
        {
            return IsCoveredByNetwork(requirement)
                ? StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DIRECT)
                : StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STILL_MISSING);
        }

        public static string BuildMaterialPillText(ProductionPlanRequirement requirement)
        {
            string materialName = ProductionOrderFormatting.GetTagDisplayName(requirement.Material);
            if (IsCoveredByNetwork(requirement))
            {
                return string.Format("{0}  {1}/{2}", materialName, GameUtil.GetFormattedMass(requirement.AvailableAmount), GameUtil.GetFormattedMass(requirement.RequiredAmount));
            }

            return string.Format(
                "{0}  {1}",
                materialName,
                string.Format(
                    StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_MISSING_PREFIX),
                    GameUtil.GetFormattedMass(GetMissingAmount(requirement))));
        }
    }
}
