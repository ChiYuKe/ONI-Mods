using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private readonly Dictionary<OrderAccountingKey, List<ProductionOrderRecord>>
            producedAmountGroups =
                new Dictionary<OrderAccountingKey, List<ProductionOrderRecord>>();

        private void UpdateProductionOrderStates()
        {
            if (ActiveOrders.Count == 0)
            {
                return;
            }

            EnsureActiveOrderAutomationLeases();
            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            if (UpdateProducedAmountsForActiveOrders())
            {
                MarkOrdersChanged();
            }
            ProductionOrderRuntimeAllocation.BeginMaintenanceSnapshot();
            try
            {
                foreach (ProductionOrderRecord order in ActiveOrders.Values)
                {
                    if (!IsOrderActive(order))
                    {
                        continue;
                    }

                    networkWorldId = GetOrderNetworkWorldId(order);
                    Runtime.GetNetworkInventory(networkWorldId);

                    ProductionOrderState previousState = order.State;
                    bool planChanged = MaintainActiveOrderPlan(order);
                    float queueLoad = CalculateOrderQueueLoad(order);
                    if (order.ObserveActivity(
                            currentCycle,
                            order.ProducedAtSubmit,
                            queueLoad,
                            planChanged || HasActiveOrderWork(order)))
                    {
                        MarkOrdersChanged();
                    }
                    if (order.ProducedAtSubmit + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= order.RequestedAmount)
                    {
                        order.State = ProductionOrderState.Completed;
                        order.CompletedCycle = currentCycle;
                        CancelOrderQueues(order);
                        ReleaseOrderAutomation(order.Key);
                        MarkOrdersChanged();
                        ProductionOrderRuntimeAllocation.NotifyOrderAssignmentsChanged(order);
                    }
                    else if (currentCycle - order.LastActivityCycle >= Config.Instance.AbnormalOrderTimeoutCycles)
                    {
                        CancelAbnormalOrder(order, currentCycle);
                        ProductionOrderRuntimeAllocation.NotifyOrderAssignmentsChanged(order);
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

                    if (order.State != previousState)
                    {
                        MarkOrdersChanged();
                    }
                }
            }
            finally
            {
                ProductionOrderRuntimeAllocation.EndMaintenanceSnapshot();
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

            MarkOrdersChanged();
        }

        private float CalculateOrderQueueLoad(ProductionOrderRecord order)
        {
            float load = 0f;
            if (order == null)
            {
                return load;
            }

            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (!IsFabricatorReachableFromWorld(
                        assignment.Fabricator,
                        GetCurrentNetworkWorldId()) ||
                    assignment.Recipe == null)
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

        private bool HasActiveOrderWork(ProductionOrderRecord order)
        {
            if (order == null || order.QueueAssignments == null)
            {
                return false;
            }

            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (assignment == null ||
                    !IsFabricatorReachableFromWorld(
                        assignment.Fabricator,
                        GetCurrentNetworkWorldId()) ||
                    assignment.Recipe == null)
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

        private bool UpdateProducedAmountsForActiveOrders()
        {
            bool changed = false;
            int connectivityVersion = StorageSceneRegistry.ConnectivityVersion;
            bool rebaseThresholds =
                observedOrderAccountingConnectivityVersion >= 0 &&
                observedOrderAccountingConnectivityVersion != connectivityVersion;
            bool relayOnline = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            foreach (List<ProductionOrderRecord> cachedOrders in producedAmountGroups.Values)
            {
                cachedOrders.Clear();
            }

            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (order == null ||
                    order.State == ProductionOrderState.Cancelled ||
                    order.State == ProductionOrderState.Abnormal)
                {
                    continue;
                }

                int orderWorldId = GetOrderNetworkWorldId(order);
                int partitionId = relayOnline ? -1 : orderWorldId;
                OrderAccountingKey key = new OrderAccountingKey(
                    partitionId,
                    order.ProductTag);
                if (!producedAmountGroups.TryGetValue(
                        key,
                        out List<ProductionOrderRecord> orders))
                {
                    orders = new List<ProductionOrderRecord>();
                    producedAmountGroups.Add(key, orders);
                }

                orders.Add(order);
            }

            foreach (KeyValuePair<OrderAccountingKey, List<ProductionOrderRecord>> pair in
                     producedAmountGroups)
            {
                List<ProductionOrderRecord> orders = pair.Value;
                if (orders.Count == 0)
                {
                    continue;
                }

                orders.Sort(ProductionOrderDisplayIdComparer.Instance);
                networkWorldId = GetOrderNetworkWorldId(orders[0]);
                float availableProduct = GetProducedAmountForOrder(pair.Key.ProductTag);
                if (rebaseThresholds)
                {
                    float preservedProduced = 0f;
                    foreach (ProductionOrderRecord order in orders)
                    {
                        preservedProduced += Mathf.Clamp(
                            order.ProducedAtSubmit,
                            0f,
                            order.RequestedAmount);
                    }

                    float rebasedThreshold = Mathf.Max(
                        0f,
                        availableProduct - preservedProduced);
                    foreach (ProductionOrderRecord order in orders)
                    {
                        order.RebaseProductionThreshold(rebasedThreshold);
                        rebasedThreshold += order.RequestedAmount;
                        changed = true;
                    }
                }

                float allocationThreshold = 0f;
                foreach (ProductionOrderRecord order in orders)
                {
                    allocationThreshold = Mathf.Max(
                        allocationThreshold,
                        order.StockAtSubmit + order.AllocationOffsetAtSubmit);
                    if (IsOrderActive(order))
                    {
                        float producedAfterThreshold = availableProduct - allocationThreshold;
                        changed |= order.SetProducedAmount(Mathf.Clamp(
                            producedAfterThreshold,
                            0f,
                            order.RequestedAmount));
                    }

                    allocationThreshold += order.RequestedAmount;
                }
            }

            observedOrderAccountingConnectivityVersion = connectivityVersion;
            return changed;
        }

        private readonly struct OrderAccountingKey : System.IEquatable<OrderAccountingKey>
        {
            public OrderAccountingKey(int partitionId, Tag productTag)
            {
                PartitionId = partitionId;
                ProductTag = productTag;
            }

            public int PartitionId { get; }

            public Tag ProductTag { get; }

            public bool Equals(OrderAccountingKey other)
            {
                return PartitionId == other.PartitionId && ProductTag == other.ProductTag;
            }

            public override bool Equals(object obj)
            {
                return obj is OrderAccountingKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (PartitionId * 397) ^ ProductTag.GetHashCode();
            }
        }

        private sealed class ProductionOrderDisplayIdComparer :
            IComparer<ProductionOrderRecord>
        {
            public static readonly ProductionOrderDisplayIdComparer Instance =
                new ProductionOrderDisplayIdComparer();

            public int Compare(ProductionOrderRecord left, ProductionOrderRecord right)
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (left == null)
                {
                    return -1;
                }

                return right == null ? 1 : left.DisplayId.CompareTo(right.DisplayId);
            }
        }

    }
}
