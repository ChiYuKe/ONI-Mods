using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private static void CancelAbnormalOrder(ProductionOrderRecord order, float currentCycle)
        {
            order.State = ProductionOrderState.Abnormal;
            order.CompletedCycle = currentCycle;
            order.AbnormalReason = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ABNORMAL_TIMEOUT_REASON), Config.Instance.AbnormalOrderTimeoutCycles, ProductionOrderFormatting.FormatCycle(order.LastActivityCycle));
            CancelOrderQueues(order);
            ReleaseOrderAutomation(order.Key);
            StorageNetworkNotifications.ShowAbnormalOrder(order);
        }

        public string CancelOrder(string orderKey, float currentCycle)
        {
            if (string.IsNullOrEmpty(orderKey) || !ActiveOrders.TryGetValue(orderKey, out ProductionOrderRecord order))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CANCEL_MISSING);
            }

            if (!IsOrderActive(order))
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CANCEL_ALREADY_DONE), order.DisplayId);
            }

            CancelOrderQueues(order);
            ReleaseOrderAutomation(order.Key);
            order.State = ProductionOrderState.Cancelled;
            order.CompletedCycle = currentCycle;
            order.AbnormalReason = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CANCEL_REASON_MANUAL);
            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CANCEL_SUCCESS), order.DisplayId);
        }

        private static void CancelOrderQueues(ProductionOrderRecord order)
        {
            foreach (QueueCancellationTarget target in BuildQueueCancellationTargets(order))
            {
                if (target.Fabricator == null || target.Recipe == null || target.OwnedCount <= 0)
                {
                    continue;
                }

                int queued = target.Fabricator.GetRecipeQueueCount(target.Recipe);
                if (queued == ComplexFabricator.QUEUE_INFINITE)
                {
                    queued = ComplexFabricator.MAX_QUEUE_SIZE;
                }

                bool cancelCurrentWorkingOrder = ShouldCancelCurrentWorkingOrder(order, target);
                int protectedQueued = GetProtectedQueueCount(order, target);
                int activeOwnedCount = Mathf.Max(0, target.OwnedCount) + (cancelCurrentWorkingOrder ? 1 : 0);
                int removableQueued = Mathf.Max(0, queued - protectedQueued);
                int cancelCount = Mathf.Min(removableQueued + (cancelCurrentWorkingOrder ? 1 : 0), activeOwnedCount);
                if (cancelCount <= 0)
                {
                    continue;
                }

                if (cancelCurrentWorkingOrder)
                {
                    target.Fabricator.SetRecipeQueueCount(target.Recipe, 0);
                }

                int finalQueued = Mathf.Max(protectedQueued, queued - Mathf.Max(0, cancelCount - (cancelCurrentWorkingOrder ? 1 : 0)));
                target.Fabricator.SetRecipeQueueCount(target.Recipe, finalQueued);
            }
        }

        private static List<QueueCancellationTarget> BuildQueueCancellationTargets(ProductionOrderRecord order)
        {
            Dictionary<string, QueueCancellationTarget> targets = new Dictionary<string, QueueCancellationTarget>();
            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (assignment?.Fabricator == null || assignment.Recipe == null)
                {
                    continue;
                }

                string key = BuildQueueKey(assignment.Fabricator, assignment.Recipe);
                if (!targets.TryGetValue(key, out QueueCancellationTarget target))
                {
                    target = new QueueCancellationTarget(assignment.Fabricator, assignment.Recipe);
                    targets[key] = target;
                }

                target.OwnedCount += GetRemainingQueueCount(order, assignment);
            }

            return targets.Values.ToList();
        }

        private static bool ShouldCancelCurrentWorkingOrder(ProductionOrderRecord cancelledOrder, QueueCancellationTarget cancelledTarget)
        {
            if (cancelledTarget.Fabricator == null ||
                cancelledTarget.Recipe == null ||
                cancelledTarget.Fabricator.CurrentWorkingOrder != cancelledTarget.Recipe)
            {
                return false;
            }

            return !ActiveOrders.Values.Any(order =>
                IsOrderActive(order) &&
                order.Key != cancelledOrder.Key &&
                IsOrderAheadOf(order, cancelledOrder) &&
                order.QueueAssignments.Any(assignment => IsSameQueue(assignment, cancelledTarget) && GetRemainingQueueCount(order, assignment) > 0));
        }

        private static bool IsOrderAheadOf(ProductionOrderRecord candidate, ProductionOrderRecord order)
        {
            if (candidate.CreatedCycle < order.CreatedCycle - 0.001f)
            {
                return true;
            }

            return Mathf.Abs(candidate.CreatedCycle - order.CreatedCycle) <= 0.001f &&
                   candidate.DisplayId < order.DisplayId;
        }

        private static int GetProtectedQueueCount(ProductionOrderRecord cancelledOrder, QueueCancellationTarget cancelledTarget)
        {
            int protectedCount = 0;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order) || order.Key == cancelledOrder.Key)
                {
                    continue;
                }

                foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
                {
                    if (IsSameQueue(assignment, cancelledTarget))
                    {
                        protectedCount += GetRemainingQueueCount(order, assignment);
                    }
                }
            }

            return Mathf.Max(0, protectedCount);
        }

        private static int GetRemainingQueueCount(ProductionOrderRecord order, ProductionOrderQueueAssignment assignment)
        {
            float outputAmount = GetRecipeOutputAmount(assignment.Recipe, order.ProductTag);
            if (outputAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return assignment.OrderCount;
            }

            int totalAssigned = order.QueueAssignments
                .Where(candidate => candidate.Recipe == assignment.Recipe && GetRecipeOutputAmount(candidate.Recipe, order.ProductTag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .Sum(candidate => candidate.OrderCount);
            if (totalAssigned <= 0)
            {
                return assignment.OrderCount;
            }

            float remainingAmount = Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit);
            int totalRemaining = Mathf.CeilToInt(remainingAmount / outputAmount);
            int remainingForAssignment = Mathf.CeilToInt(totalRemaining * assignment.OrderCount / (float)totalAssigned);
            return Mathf.Clamp(remainingForAssignment, 0, assignment.OrderCount);
        }

        private static float GetRecipeOutputAmount(ComplexRecipe recipe, Tag productTag)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(recipe, productTag);
            return result != null ? Mathf.Max(0f, result.amount) : 0f;
        }

        private static bool IsSameQueue(ProductionOrderQueueAssignment left, ProductionOrderQueueAssignment right)
        {
            return left != null &&
                   right != null &&
                   left.Fabricator == right.Fabricator &&
                   left.Recipe == right.Recipe;
        }

        private static bool IsSameQueue(ProductionOrderQueueAssignment assignment, QueueCancellationTarget target)
        {
            return assignment != null &&
                   target != null &&
                   assignment.Fabricator == target.Fabricator &&
                   assignment.Recipe == target.Recipe;
        }

        private static string BuildQueueKey(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            return string.Format("{0}|{1}", fabricator.GetInstanceID(), recipe.id);
        }
    }
}
