using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private readonly Dictionary<string, ResearchRequirementLiveView> orderResearchRequirementViews =
            new Dictionary<string, ResearchRequirementLiveView>();
        private readonly Dictionary<string, DispatchRequirementLiveView> orderDispatchRequirementViews =
            new Dictionary<string, DispatchRequirementLiveView>();
        private readonly Dictionary<string, ProductionPlanRequirement> orderRequirementLookup =
            new Dictionary<string, ProductionPlanRequirement>();
        private TextMeshProUGUI orderCurrentCycleMetric;
        private TextMeshProUGUI orderFinishCycleMetric;
        private TextMeshProUGUI orderAutoProduceMetric;
        private TextMeshProUGUI orderBlockedMetric;
        private TextMeshProUGUI orderKeepCurrentStockMetric;
        private TextMeshProUGUI orderKeepPendingMetric;
        private TextMeshProUGUI orderKeepShortageMetric;

        private void ResetOrderWorkspaceLiveViews()
        {
            orderResearchRequirementViews.Clear();
            orderDispatchRequirementViews.Clear();
            orderRequirementLookup.Clear();
            orderCurrentCycleMetric = null;
            orderFinishCycleMetric = null;
            orderAutoProduceMetric = null;
            orderBlockedMetric = null;
            orderKeepCurrentStockMetric = null;
            orderKeepPendingMetric = null;
            orderKeepShortageMetric = null;
        }

        private void UpdateOrderWorkspaceLive(ProductDisplayGroup product, ProductionOrderDraft draft)
        {
            if (product == null || draft == null)
            {
                return;
            }

            float currentCycle = StorageNetworkCycleTime.GetCurrent();
            float estimateSeconds = productionOrderService.EstimatePlanSeconds(draft.Plan, out bool infinite);
            SetTextIfChanged(orderCurrentCycleMetric, ProductionOrderFormatting.FormatCycleStamp(currentCycle));
            SetTextIfChanged(
                orderFinishCycleMetric,
                infinite
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_UNKNOWN)
                    : ProductionOrderFormatting.FormatCycleStamp(currentCycle + estimateSeconds / 600f));
            SetTextIfChanged(orderAutoProduceMetric, draft.ProducedRequirementCount.ToString());
            SetTextIfChanged(orderBlockedMetric, draft.BlockedRequirementCount.ToString());

            ProductionKeepRule rule = productionOrderService.GetKeepRule(product.ProductTag);
            float currentStock = productionOrderService.GetNetworkRawAmount(product.ProductTag);
            float automaticPending = productionOrderService.GetActiveOrdersForProduct(product.ProductTag, 100)
                .Where(order => order.IsAutomatic)
                .Sum(order => Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit));
            float shortage = rule != null
                ? Mathf.Max(0f, rule.TargetAmount - currentStock - automaticPending)
                : 0f;
            SetTextIfChanged(
                orderKeepCurrentStockMetric,
                string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_CURRENT_STOCK),
                    GameUtil.GetFormattedMass(currentStock)));
            SetTextIfChanged(
                orderKeepPendingMetric,
                string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_PENDING),
                    GameUtil.GetFormattedMass(automaticPending)));
            if (orderKeepShortageMetric != null)
            {
                SetTextIfChanged(
                    orderKeepShortageMetric,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_SHORTAGE),
                        GameUtil.GetFormattedMass(shortage)));
                orderKeepShortageMetric.color = shortage > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                    ? WarningColor()
                    : PositiveColor();
            }

            orderRequirementLookup.Clear();
            CollectResearchRequirements(draft.Plan, 0, "research");
            foreach (KeyValuePair<string, ResearchRequirementLiveView> pair in orderResearchRequirementViews)
            {
                if (orderRequirementLookup.TryGetValue(pair.Key, out ProductionPlanRequirement requirement))
                {
                    UpdateResearchRequirementLiveView(pair.Value, requirement);
                }
            }

            List<ProductionPlanRequirement> dispatchRequirements = draft.Plan?.Requirements
                .Where(requirement => requirement != null && requirement.Material != Tag.Invalid)
                .Take(3)
                .ToList() ?? new List<ProductionPlanRequirement>();
            for (int i = 0; i < dispatchRequirements.Count; i++)
            {
                if (orderDispatchRequirementViews.TryGetValue("dispatch:" + i, out DispatchRequirementLiveView view))
                {
                    UpdateDispatchRequirementLiveView(view, dispatchRequirements[i]);
                }
            }
        }

        private void CollectResearchRequirements(ProductionPlanNode node, int depth, string path)
        {
            if (node == null || depth > 2)
            {
                return;
            }

            List<ProductionPlanRequirement> requirements = node.Requirements
                .Where(requirement => requirement != null && requirement.Material != Tag.Invalid)
                .Take(depth == 0 ? 5 : 4)
                .ToList();
            for (int i = 0; i < requirements.Count; i++)
            {
                ProductionPlanRequirement requirement = requirements[i];
                string requirementPath = path + "." + i;
                if (requirement.Child != null && depth < 2)
                {
                    CollectResearchRequirements(requirement.Child, depth + 1, requirementPath);
                }

                orderRequirementLookup[requirementPath] = requirement;
            }
        }

        private static void UpdateResearchRequirementLiveView(
            ResearchRequirementLiveView view,
            ProductionPlanRequirement requirement)
        {
            bool covered = StorageNetworkPlanPreviewText.IsCoveredByNetwork(requirement);
            bool produced = StorageNetworkPlanPreviewText.CanProduceRequirement(requirement);
            Color color = covered ? PositiveColor() : produced ? WarningColor() : DangerColor();
            SetTextIfChanged(view.Amount, StorageNetworkPlanPreviewText.BuildResearchAmountText(requirement, false));
            SetTextIfChanged(
                view.Status,
                covered
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DIRECT)
                    : produced
                        ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_AUTO)
                        : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_NO_ROUTE));
            if (view.Name != null)
            {
                view.Name.color = color;
            }
            if (view.Status != null)
            {
                view.Status.color = color;
            }
        }

        private static void UpdateDispatchRequirementLiveView(
            DispatchRequirementLiveView view,
            ProductionPlanRequirement requirement)
        {
            bool covered = StorageNetworkPlanPreviewText.IsCoveredByNetwork(requirement);
            bool produced = StorageNetworkPlanPreviewText.CanProduceRequirement(requirement);
            Color color = covered ? PositiveColor() : produced ? WarningColor() : DangerColor();
            SetTextIfChanged(view.Material, StorageNetworkPlanPreviewText.BuildMaterialPillText(requirement));
            SetTextIfChanged(
                view.SourceStatus,
                covered
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_DIRECT)
                    : produced
                        ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_NEEDS_PRODUCTION)
                        : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_STATUS_BLOCKED));
            SetTextIfChanged(
                view.DestinationStatus,
                covered
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_SEND_FROM_NETWORK)
                    : produced
                        ? StorageNetworkPlanPreviewText.BuildAssignmentSummary(requirement.Child, 3)
                        : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DISPATCH_NO_ROUTE));
            if (view.Material != null)
            {
                view.Material.color = color;
            }
            if (view.SourceStatus != null)
            {
                view.SourceStatus.color = color;
            }
            if (view.DestinationStatus != null)
            {
                view.DestinationStatus.color = color;
            }
        }

        private sealed class ResearchRequirementLiveView
        {
            public ResearchRequirementLiveView(
                TextMeshProUGUI name,
                TextMeshProUGUI amount,
                TextMeshProUGUI status)
            {
                Name = name;
                Amount = amount;
                Status = status;
            }

            public TextMeshProUGUI Name { get; }
            public TextMeshProUGUI Amount { get; }
            public TextMeshProUGUI Status { get; }
        }

        private sealed class DispatchRequirementLiveView
        {
            public DispatchRequirementLiveView(
                TextMeshProUGUI material,
                TextMeshProUGUI sourceStatus,
                TextMeshProUGUI destinationStatus)
            {
                Material = material;
                SourceStatus = sourceStatus;
                DestinationStatus = destinationStatus;
            }

            public TextMeshProUGUI Material { get; }
            public TextMeshProUGUI SourceStatus { get; }
            public TextMeshProUGUI DestinationStatus { get; }
        }
    }
}
