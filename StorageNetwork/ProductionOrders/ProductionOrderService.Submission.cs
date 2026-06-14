using System.Collections.Generic;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        public ProductionOrderDraft BuildDraft(ProductDisplayGroup product, RecipeDisplayInfo route, float requestedAmount)
        {
            ProductionOrderDraft draft = new ProductionOrderDraft
            {
                Product = product,
                Route = route,
                RequestedAmount = Mathf.Max(0f, requestedAmount),
                NetworkRawAmount = product != null ? GetNetworkRawAmount(product.ProductTag) : 0f,
                NetworkAvailableAmount = product != null ? GetNetworkAvailableAmount(product.ProductTag) : 0f,
                ReservedOutputAmount = product != null ? GetReservedAmount(product.ProductTag) : 0f,
                DuplicatePolicy = ProductionOrderDuplicatePolicy.CreateNew
            };
            if (product == null || route.Recipe == null)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Blocked;
                draft.ValidationMessages.Add(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_MISSING_PRODUCT));
                return draft;
            }

            if (requestedAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Blocked;
                draft.ValidationMessages.Add(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_AMOUNT_POSITIVE));
                return draft;
            }

            draft.Plan = BuildProductionPlan(route.Recipe, route.Fabricators, product.ProductTag, requestedAmount);
            draft.DuplicateOrder = FindDuplicateOrder(product.ProductTag, route.Recipe, requestedAmount);
            draft.DuplicatePolicy = draft.DuplicateOrder == null ? ProductionOrderDuplicatePolicy.CreateNew : ProductionOrderDuplicatePolicy.MergeIntoExisting;
            if (draft.Plan.Assignments.Count == 0)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Blocked;
                draft.ValidationMessages.Add(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_NO_EQUIPMENT));
            }

            if (draft.BlockedRequirementCount > 0)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Blocked;
                draft.ValidationMessages.Add(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_BLOCKED_REQUIREMENTS), draft.BlockedRequirementCount));
            }
            else if (draft.ProducedRequirementCount > 0 || draft.DuplicateOrder != null)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Warning;
            }
            else if (draft.RiskLevel != ProductionOrderRiskLevel.Blocked)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Ready;
            }

            if (draft.DuplicateOrder != null)
            {
                draft.ValidationMessages.Add(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_DUPLICATE_MERGE), draft.DuplicateOrder.DisplayId));
            }

            if (draft.ProducedRequirementCount > 0)
            {
                draft.ValidationMessages.Add(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_AUTO_PRODUCE), draft.ProducedRequirementCount));
            }

            if (draft.ValidationMessages.Count == 0)
            {
                draft.ValidationMessages.Add(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_VALIDATION_READY_BODY));
            }

            return draft;
        }

        public ProductionOrderSubmitResult SubmitOrder(ProductDisplayGroup product, RecipeDisplayInfo route, float requestedAmount, float currentCycle, bool isAutomatic = false)
        {
            ProductionOrderDraft draft = BuildDraft(product, route, requestedAmount);
            ProductionPlanNode plan = draft.Plan;
            if (plan == null || !draft.CanSubmit)
            {
                return ProductionOrderSubmitResult.Fail(string.Join(" ", draft.ValidationMessages.ToArray()));
            }

            if (plan.Assignments.Count == 0)
            {
                return ProductionOrderSubmitResult.Fail(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_SUBMIT_NO_EQUIPMENT));
            }

            ProductionOrderRecord duplicate = FindDuplicateOrder(product.ProductTag, route.Recipe, requestedAmount);
            Dictionary<Tag, float> reservedMaterials = BuildReservedMaterials(plan);
            List<ProductionOrderQueueAssignment> queueAssignments = BuildQueueAssignments(plan);
            List<ProductionOrderMaterialLease> materialLeases = BuildMaterialLeases(plan);
            List<ProductionOrderOutputLease> outputLeases = BuildOutputLeases(queueAssignments, product.ProductTag, requestedAmount);
            if (duplicate != null)
            {
                ApplyProductionPlan(plan, duplicate.Key, materialLeases);
                duplicate.Merge(requestedAmount, plan.OrderCount, reservedMaterials, queueAssignments, materialLeases, outputLeases, currentCycle, isAutomatic);
                duplicate.ObserveActivity(currentCycle, duplicate.ProducedAtSubmit, CalculateOrderQueueLoad(duplicate));
                return ProductionOrderSubmitResult.MergeSuccess(duplicate, plan, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_SUBMIT_MERGED), duplicate.DisplayId, plan.OrderCount));
            }

            string orderKey = BuildOrderKey(product.ProductTag, route.Recipe, requestedAmount, currentCycle);
            float stockAtSubmit = GetProducedAmountForOrder(product.ProductTag);
            float allocationOffsetAtSubmit = GetPendingProducedAmountAhead(product.ProductTag);
            ApplyProductionPlan(plan, orderKey, materialLeases);
            ProductionOrderRecord record = new ProductionOrderRecord(
                orderKey,
                ActiveOrders.Count + 1,
                product.ProductTag,
                product.ProductName,
                ProductionRecipeCatalog.GetRecipeKey(route.Recipe),
                requestedAmount,
                plan.OrderCount,
                stockAtSubmit,
                allocationOffsetAtSubmit,
                reservedMaterials,
                queueAssignments,
                materialLeases,
                outputLeases,
                currentCycle,
                isAutomatic);
            ActiveOrders[orderKey] = record;
            record.ObserveActivity(currentCycle, record.ProducedAtSubmit, CalculateOrderQueueLoad(record));
            return ProductionOrderSubmitResult.Created(record, plan, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_SUBMIT_CREATED), record.DisplayId, plan.OrderCount));
        }

        private static string BuildOrderKey(Tag productTag, ComplexRecipe recipe, float requestedAmount, float createdCycle)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            int amountBucket = Mathf.RoundToInt(requestedAmount * 1000f);
            int cycleBucket = Mathf.RoundToInt(createdCycle * 100f);
            return string.Format("{0}|{1}|{2}|{3}", productTag, recipeKey, amountBucket, cycleBucket);
        }
    }
}
