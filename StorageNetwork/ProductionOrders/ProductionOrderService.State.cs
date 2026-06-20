using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Services;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private void UpdateProductionOrderStates()
        {
            if (ActiveOrders.Count == 0)
            {
                return;
            }

            EnsureActiveOrderAutomationLeases();
            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            UpdateProducedAmountsForActiveOrders();
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order))
                {
                    continue;
                }

                bool planChanged = MaintainActiveOrderPlan(order);
                float queueLoad = CalculateOrderQueueLoad(order);
                order.ObserveActivity(currentCycle, order.ProducedAtSubmit, queueLoad, planChanged || HasActiveOrderWork(order));
                if (order.ProducedAtSubmit + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= order.RequestedAmount)
                {
                    order.State = ProductionOrderState.Completed;
                    order.CompletedCycle = currentCycle;
                    CancelOrderQueues(order);
                    ReleaseOrderAutomation(order.Key);
                }
                else if (currentCycle - order.LastActivityCycle >= Config.Instance.AbnormalOrderTimeoutCycles)
                {
                    CancelAbnormalOrder(order, currentCycle);
                }
                else if (HasMissingReservedMaterial(order))
                {
                    order.State = ProductionOrderState.WaitingMaterials;
                }
                else if (ProductionOrderRuntimeAllocation.GetRunningCountForOrder(order) > 0)
                {
                    order.State = ProductionOrderState.Producing;
                }
                else
                {
                    order.State = ProductionOrderState.Submitted;
                }
            }
        }

        private static bool IsOrderActive(ProductionOrderRecord order)
        {
            return IsOrderActiveForRuntimeAllocation(order);
        }

        internal static bool IsOrderActiveForRuntimeAllocation(ProductionOrderRecord order)
        {
            return order != null &&
                   order.State != ProductionOrderState.Completed &&
                   order.State != ProductionOrderState.Abnormal &&
                   order.State != ProductionOrderState.Cancelled;
        }

        internal static IEnumerable<ProductionOrderRecord> OrdersSnapshot()
        {
            return ActiveOrders.Values;
        }

        private static string FormatOrderUsage(ProductionOrderRecord order, ComplexFabricator fabricator)
        {
            ProductionOrderQueueAssignment localAssignment = order.QueueAssignments.FirstOrDefault(assignment => assignment.Fabricator == fabricator);
            if (localAssignment == null || localAssignment.Recipe == null)
            {
                return string.Format("#{0} {1}", order.DisplayId, order.ProductName);
            }

            if (localAssignment.Primary)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_USAGE_PRIMARY), order.DisplayId, order.ProductName, localAssignment.OrderCount);
            }

            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_USAGE_SUPPLY),
                order.DisplayId,
                string.IsNullOrEmpty(localAssignment.ConsumerName) ? FormatPrimaryFabricators(order) : localAssignment.ConsumerName,
                string.IsNullOrEmpty(localAssignment.OutputName) ? GetRecipeOutputName(localAssignment.Recipe, order.ProductTag) : localAssignment.OutputName,
                localAssignment.OrderCount);
        }

        private static string FormatPrimaryFabricators(ProductionOrderRecord order)
        {
            List<string> names = order.QueueAssignments
                .Where(assignment => IsOrderProductionFabricator(assignment.Fabricator) &&
                                     assignment.Recipe != null &&
                                     assignment.Primary)
                .Select(assignment => assignment.Fabricator.GetProperName())
                .Distinct()
                .Take(2)
                .ToList();
            if (names.Count == 0)
            {
                return order.ProductName;
            }

            return names.Count == 1 ? names[0] : string.Join("+", names.ToArray());
        }

        private static string GetRecipeOutputName(ComplexRecipe recipe, Tag fallbackTag)
        {
            ComplexRecipe.RecipeElement result = recipe?.results?.FirstOrDefault();
            if (result != null)
            {
                if (result.material != Tag.Invalid)
                {
                    return ProductionOrderFormatting.GetTagDisplayName(result.material);
                }

                if (!string.IsNullOrEmpty(result.facadeID))
                {
                    return ProductionOrderFormatting.GetTagDisplayName(result.facadeID.ToTag());
                }
            }

            return ProductionOrderFormatting.GetTagDisplayName(fallbackTag);
        }

        private static void PurgeExpiredFinishedOrders()
        {
            if (ActiveOrders.Count == 0)
            {
                return;
            }

            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            List<string> expiredKeys = ActiveOrders.Values
                .Where(order => !IsOrderActive(order) &&
                                order.CompletedCycle > 0f &&
                                currentCycle - order.CompletedCycle > Config.Instance.FinishedOrderRecordLifetimeCycles)
                .Select(order => order.Key)
                .ToList();
            if (expiredKeys.Count == 0)
            {
                return;
            }

            foreach (string key in expiredKeys)
            {
                ActiveOrders.Remove(key);
            }
        }

        private static float CalculateOrderQueueLoad(ProductionOrderRecord order)
        {
            float load = 0f;
            if (order == null)
            {
                return load;
            }

            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (!IsOrderProductionFabricator(assignment.Fabricator) || assignment.Recipe == null)
                {
                    continue;
                }

                if (ProductionOrderRuntimeAllocation.HasQueuedWorkForAssignment(order, assignment))
                {
                    int queued = StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(assignment.Fabricator, assignment.Recipe);
                    load += queued == ComplexFabricator.QUEUE_INFINITE ? ComplexFabricator.MAX_QUEUE_SIZE : Mathf.Min(Mathf.Max(0, queued), assignment.OrderCount);
                }

                if (ProductionOrderRuntimeAllocation.GetRunningCountForAssignment(order, assignment) > 0)
                {
                    load += ProductionOrderRuntimeAllocation.GetProgressForAssignment(order, assignment);
                }

                load += GetRecipeIngredientLoad(assignment.Fabricator.inStorage, assignment.Recipe);
                load += GetRecipeIngredientLoad(assignment.Fabricator.buildStorage, assignment.Recipe);
            }

            return load;
        }

        private static bool HasActiveOrderWork(ProductionOrderRecord order)
        {
            if (order == null || order.QueueAssignments == null)
            {
                return false;
            }

            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (assignment == null || !IsOrderProductionFabricator(assignment.Fabricator) || assignment.Recipe == null)
                {
                    continue;
                }

                if (ProductionOrderRuntimeAllocation.HasQueuedWorkForAssignment(order, assignment) ||
                    ProductionOrderRuntimeAllocation.GetRunningCountForAssignment(order, assignment) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetRecipeIngredientLoad(Storage storage, ComplexRecipe recipe)
        {
            if (storage == null || recipe == null || recipe.ingredients == null)
            {
                return 0f;
            }

            float load = 0f;
            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                load += storage.GetAmountAvailable(ingredient.material);
            }

            return load;
        }

        private void UpdateProducedAmountsForActiveOrders()
        {
            foreach (IGrouping<Tag, ProductionOrderRecord> group in ActiveOrders.Values
                         .Where(order => order != null &&
                                         order.State != ProductionOrderState.Cancelled &&
                                         order.State != ProductionOrderState.Abnormal)
                         .GroupBy(order => order.ProductTag))
            {
                float availableProduct = GetProducedAmountForOrder(group.Key);
                float allocationThreshold = 0f;
                foreach (ProductionOrderRecord order in group.OrderBy(order => order.DisplayId))
                {
                    allocationThreshold = Mathf.Max(allocationThreshold, order.StockAtSubmit + order.AllocationOffsetAtSubmit);
                    if (IsOrderActive(order))
                    {
                        float leasedProduct = GetLeasedPrimaryOutputAmount(order);
                        float producedAfterThreshold = Mathf.Max(availableProduct, leasedProduct + allocationThreshold) - allocationThreshold;
                        order.SetProducedAmount(Mathf.Clamp(producedAfterThreshold, 0f, order.RequestedAmount));
                    }

                    allocationThreshold += order.RequestedAmount;
                }
            }
        }

        private static float GetLeasedPrimaryOutputAmount(ProductionOrderRecord order)
        {
            float amount = 0f;
            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments.Where(assignment => assignment.Primary))
            {
                if (!IsOrderProductionFabricator(assignment.Fabricator) || assignment.Fabricator.outStorage == null || assignment.Fabricator.outStorage.items == null)
                {
                    continue;
                }

                Tag outputTag = assignment.OutputTag != Tag.Invalid ? assignment.OutputTag : order.ProductTag;
                foreach (GameObject item in assignment.Fabricator.outStorage.items)
                {
                    PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (primaryElement != null && StorageItemUtility.MatchesStorageTag(item, outputTag))
                    {
                        amount += primaryElement.Mass;
                    }
                }
            }

            return amount;
        }
    }
}
