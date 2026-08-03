using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class ProductionOrderRuntimeAllocation
    {
        private static readonly List<ProductionOrderRecord> OrderedOrders =
            new List<ProductionOrderRecord>();
        private static readonly Dictionary<ProductionOrderRecord, int> OrderIndexes =
            new Dictionary<ProductionOrderRecord, int>();
        private static readonly Dictionary<OrderQueueKey, int> EarlierDemandByOrderQueue =
            new Dictionary<OrderQueueKey, int>();
        private static readonly Dictionary<OrderQueueKey, int> DemandByOrderQueue =
            new Dictionary<OrderQueueKey, int>();
        private static readonly Dictionary<ProductionOrderRecord, List<QueueKey>> QueuesByOrder =
            new Dictionary<ProductionOrderRecord, List<QueueKey>>();
        private static readonly Dictionary<QueueKey, int> RunningDemandByQueue =
            new Dictionary<QueueKey, int>();
        private static readonly HashSet<QueueKey> AffectedQueues = new HashSet<QueueKey>();
        private static readonly List<ProductionOrderRecord> StaleOrderKeys =
            new List<ProductionOrderRecord>();
        private static bool maintenanceSnapshotActive;

        public static void BeginMaintenanceSnapshot()
        {
            maintenanceSnapshotActive = true;
            OrderedOrders.Clear();
            OrderIndexes.Clear();
            EarlierDemandByOrderQueue.Clear();
            DemandByOrderQueue.Clear();
            RunningDemandByQueue.Clear();
            foreach (ProductionOrderRecord order in ProductionOrderService.OrdersSnapshot())
            {
                if (ProductionOrderService.IsOrderActiveForRuntimeAllocation(order))
                {
                    OrderedOrders.Add(order);
                }
            }

            OrderedOrders.Sort(CompareOrders);
            for (int orderIndex = 0; orderIndex < OrderedOrders.Count; orderIndex++)
            {
                ProductionOrderRecord order = OrderedOrders[orderIndex];
                OrderIndexes[order] = orderIndex;
                RebuildOrderQueueList(order);
                List<QueueKey> queues = QueuesByOrder[order];
                for (int queueIndex = 0; queueIndex < queues.Count; queueIndex++)
                {
                    QueueKey queue = queues[queueIndex];
                    int earlier = RunningDemandByQueue.TryGetValue(queue, out int running)
                        ? running
                        : 0;
                    int demand = GetRemainingCountForOrderQueue(
                        order,
                        queue.Fabricator,
                        queue.Recipe);
                    OrderQueueKey key = new OrderQueueKey(order, queue);
                    EarlierDemandByOrderQueue[key] = earlier;
                    DemandByOrderQueue[key] = demand;
                    RunningDemandByQueue[queue] = earlier + demand;
                }
            }

            StaleOrderKeys.Clear();
            foreach (ProductionOrderRecord cachedOrder in QueuesByOrder.Keys)
            {
                if (!OrderIndexes.ContainsKey(cachedOrder))
                {
                    StaleOrderKeys.Add(cachedOrder);
                }
            }

            for (int i = 0; i < StaleOrderKeys.Count; i++)
            {
                QueuesByOrder.Remove(StaleOrderKeys[i]);
            }
        }

        public static void NotifyOrderAssignmentsChanged(ProductionOrderRecord order)
        {
            if (!maintenanceSnapshotActive || order == null || !OrderIndexes.ContainsKey(order))
            {
                return;
            }

            AffectedQueues.Clear();
            if (QueuesByOrder.TryGetValue(order, out List<QueueKey> previousQueues))
            {
                for (int i = 0; i < previousQueues.Count; i++)
                {
                    AffectedQueues.Add(previousQueues[i]);
                }
            }

            RebuildOrderQueueList(order);
            List<QueueKey> currentQueues = QueuesByOrder[order];
            for (int i = 0; i < currentQueues.Count; i++)
            {
                AffectedQueues.Add(currentQueues[i]);
            }

            foreach (QueueKey queue in AffectedQueues)
            {
                RebuildQueuePrefix(queue);
            }
        }

        public static void EndMaintenanceSnapshot()
        {
            maintenanceSnapshotActive = false;
        }

        public static void ResetRuntimeState()
        {
            maintenanceSnapshotActive = false;
            OrderedOrders.Clear();
            OrderIndexes.Clear();
            EarlierDemandByOrderQueue.Clear();
            DemandByOrderQueue.Clear();
            QueuesByOrder.Clear();
            RunningDemandByQueue.Clear();
            AffectedQueues.Clear();
            StaleOrderKeys.Clear();
        }

        public static int GetRunningCountForOrder(ProductionOrderRecord record)
        {
            if (record == null || record.QueueAssignments == null)
            {
                return 0;
            }

            int running = 0;
            for (int i = 0; i < record.QueueAssignments.Count; i++)
            {
                ProductionOrderQueueAssignment assignment = record.QueueAssignments[i];
                if (assignment != null && assignment.Primary)
                {
                    running += GetRunningCountForAssignment(record, assignment);
                }
            }

            return running;
        }

        public static int GetRunningCountForAssignment(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (record == null || !IsRuntimeFabricatorAvailable(assignment?.Fabricator) || assignment.Recipe == null)
            {
                return 0;
            }

            int totalRunning = StorageNetworkFabricatorProgress.GetWorkingCountForRecipe(assignment.Fabricator, assignment.Recipe);
            if (totalRunning <= 0)
            {
                return 0;
            }

            int earlierDemand = GetEarlierDemand(record, assignment);

            int ownDemand = GetRemainingCount(record, assignment);
            return Mathf.Clamp(totalRunning - earlierDemand, 0, ownDemand);
        }

        public static bool HasQueuedWorkForAssignment(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (record == null || !IsRuntimeFabricatorAvailable(assignment?.Fabricator) || assignment.Recipe == null)
            {
                return false;
            }

            int queued = StorageNetworkFabricatorProgress.GetFiniteRecipeQueueCountSafe(assignment.Fabricator, assignment.Recipe);
            int running = StorageNetworkFabricatorProgress.GetWorkingCountForRecipe(assignment.Fabricator, assignment.Recipe);
            int availableWork = queued + running;
            int earlierDemand = GetEarlierDemand(record, assignment);

            return availableWork - earlierDemand > 0 && GetRemainingCount(record, assignment) > 0;
        }

        public static int GetAllocatedWorkCountForAssignment(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (record == null || !IsRuntimeFabricatorAvailable(assignment?.Fabricator) || assignment.Recipe == null)
            {
                return 0;
            }

            int queued = StorageNetworkFabricatorProgress.GetFiniteRecipeQueueCountSafe(assignment.Fabricator, assignment.Recipe);
            int running = StorageNetworkFabricatorProgress.GetWorkingCountForRecipe(assignment.Fabricator, assignment.Recipe);
            int availableWork = queued + running;
            int earlierDemand = GetEarlierDemand(record, assignment);

            return Mathf.Clamp(availableWork - earlierDemand, 0, GetRemainingCount(record, assignment));
        }

        public static float GetProgressForAssignment(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            return GetRunningCountForAssignment(record, assignment) > 0
                ? StorageNetworkFabricatorProgress.GetRecipeProgress(assignment.Fabricator, assignment.Recipe)
                : 0f;
        }

        private static int GetEarlierDemand(
            ProductionOrderRecord record,
            ProductionOrderQueueAssignment assignment)
        {
            QueueKey queue = new QueueKey(assignment.Fabricator, assignment.Recipe);
            if (maintenanceSnapshotActive &&
                EarlierDemandByOrderQueue.TryGetValue(
                    new OrderQueueKey(record, queue),
                    out int cached))
            {
                return cached;
            }

            int earlierDemand = 0;
            foreach (ProductionOrderRecord order in ProductionOrderService.OrdersSnapshot())
            {
                if (ProductionOrderService.IsOrderActiveForRuntimeAllocation(order) &&
                    IsOrderAheadOf(order, record) &&
                    HasQueue(order, queue))
                {
                    earlierDemand += GetRemainingCountForOrderQueue(
                        order,
                        assignment.Fabricator,
                        assignment.Recipe);
                }
            }

            return earlierDemand;
        }

        private static int GetRemainingCountForOrderQueue(ProductionOrderRecord order, ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            if (order?.QueueAssignments == null)
            {
                return 0;
            }

            int remaining = 0;
            for (int i = 0; i < order.QueueAssignments.Count; i++)
            {
                ProductionOrderQueueAssignment assignment = order.QueueAssignments[i];
                if (assignment != null &&
                    assignment.Fabricator == fabricator &&
                    assignment.Recipe == recipe)
                {
                    remaining += GetRemainingCount(order, assignment);
                }
            }

            return remaining;
        }

        private static int GetRemainingCount(ProductionOrderRecord order, ProductionOrderQueueAssignment assignment)
        {
            float outputAmount = GetRecipeOutputAmount(assignment.Recipe, order.ProductTag);
            if (outputAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return assignment.OrderCount;
            }

            int totalAssigned = 0;
            for (int i = 0; i < order.QueueAssignments.Count; i++)
            {
                ProductionOrderQueueAssignment candidate = order.QueueAssignments[i];
                if (candidate != null &&
                    candidate.Recipe == assignment.Recipe &&
                    GetRecipeOutputAmount(candidate.Recipe, order.ProductTag) >
                    PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    totalAssigned += candidate.OrderCount;
                }
            }
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

        private static void RebuildOrderQueueList(ProductionOrderRecord order)
        {
            if (!QueuesByOrder.TryGetValue(order, out List<QueueKey> queues))
            {
                queues = new List<QueueKey>();
                QueuesByOrder.Add(order, queues);
            }

            queues.Clear();
            if (order?.QueueAssignments == null)
            {
                return;
            }

            for (int assignmentIndex = 0;
                 assignmentIndex < order.QueueAssignments.Count;
                 assignmentIndex++)
            {
                ProductionOrderQueueAssignment assignment =
                    order.QueueAssignments[assignmentIndex];
                if (assignment?.Fabricator == null || assignment.Recipe == null)
                {
                    continue;
                }

                QueueKey queue = new QueueKey(assignment.Fabricator, assignment.Recipe);
                bool duplicate = false;
                for (int queueIndex = 0; queueIndex < queues.Count; queueIndex++)
                {
                    if (queues[queueIndex].Equals(queue))
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    queues.Add(queue);
                }
            }
        }

        private static void RebuildQueuePrefix(QueueKey queue)
        {
            int running = 0;
            for (int orderIndex = 0; orderIndex < OrderedOrders.Count; orderIndex++)
            {
                ProductionOrderRecord order = OrderedOrders[orderIndex];
                OrderQueueKey key = new OrderQueueKey(order, queue);
                if (!ProductionOrderService.IsOrderActiveForRuntimeAllocation(order) ||
                    !HasQueue(order, queue))
                {
                    EarlierDemandByOrderQueue.Remove(key);
                    DemandByOrderQueue.Remove(key);
                    continue;
                }

                int demand = GetRemainingCountForOrderQueue(
                    order,
                    queue.Fabricator,
                    queue.Recipe);
                EarlierDemandByOrderQueue[key] = running;
                DemandByOrderQueue[key] = demand;
                running += demand;
            }

            RunningDemandByQueue[queue] = running;
        }

        private static bool HasQueue(ProductionOrderRecord order, QueueKey queue)
        {
            if (order?.QueueAssignments == null)
            {
                return false;
            }

            for (int i = 0; i < order.QueueAssignments.Count; i++)
            {
                ProductionOrderQueueAssignment assignment = order.QueueAssignments[i];
                if (assignment != null &&
                    ReferenceEquals(assignment.Fabricator, queue.Fabricator) &&
                    ReferenceEquals(assignment.Recipe, queue.Recipe))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CompareOrders(
            ProductionOrderRecord left,
            ProductionOrderRecord right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left.CreatedCycle < right.CreatedCycle - 0.001f)
            {
                return -1;
            }

            if (left.CreatedCycle > right.CreatedCycle + 0.001f)
            {
                return 1;
            }

            return left.DisplayId.CompareTo(right.DisplayId);
        }

        private static bool IsRuntimeFabricatorAvailable(ComplexFabricator fabricator)
        {
            if (!ProductionOrderCenterCatalog.IsOrderProductionFabricator(fabricator) ||
                !StorageSceneRegistry.IsLive(fabricator))
            {
                return false;
            }

            int worldId = StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject);
            return worldId >= 0 && StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
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

        private readonly struct QueueKey : System.IEquatable<QueueKey>
        {
            public QueueKey(ComplexFabricator fabricator, ComplexRecipe recipe)
            {
                Fabricator = fabricator;
                Recipe = recipe;
            }

            public ComplexFabricator Fabricator { get; }

            public ComplexRecipe Recipe { get; }

            public bool Equals(QueueKey other)
            {
                return ReferenceEquals(Fabricator, other.Fabricator) &&
                       ReferenceEquals(Recipe, other.Recipe);
            }

            public override bool Equals(object obj)
            {
                return obj is QueueKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                int fabricatorHash = Fabricator != null
                    ? RuntimeHelpers.GetHashCode(Fabricator)
                    : 0;
                int recipeHash = Recipe != null ? RuntimeHelpers.GetHashCode(Recipe) : 0;
                return (fabricatorHash * 397) ^ recipeHash;
            }
        }

        private readonly struct OrderQueueKey : System.IEquatable<OrderQueueKey>
        {
            private readonly ProductionOrderRecord order;
            private readonly QueueKey queue;

            public OrderQueueKey(ProductionOrderRecord order, QueueKey queue)
            {
                this.order = order;
                this.queue = queue;
            }

            public bool Equals(OrderQueueKey other)
            {
                return ReferenceEquals(order, other.order) && queue.Equals(other.queue);
            }

            public override bool Equals(object obj)
            {
                return obj is OrderQueueKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                int orderHash = order != null ? RuntimeHelpers.GetHashCode(order) : 0;
                return (orderHash * 397) ^ queue.GetHashCode();
            }
        }
    }
}
