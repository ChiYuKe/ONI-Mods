using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private static readonly List<ProductionOrderRecord> LeasedMaterialOrderBuffer =
            new List<ProductionOrderRecord>();
        private static readonly List<int> EmptyAutomationLeaseBuffer = new List<int>();
        private readonly List<StorageSourceSortKey> transferSourceBuffer =
            new List<StorageSourceSortKey>();
        private readonly HashSet<Storage> transferSourceSeenBuffer = new HashSet<Storage>();

        public static float RequestLeasedMaterial(ComplexFabricator fabricator, ComplexRecipe recipe, Tag tag, float amount, Storage target)
        {
            if (fabricator == null ||
                !IsOrderProductionFabricator(fabricator) ||
                recipe == null ||
                tag == Tag.Invalid ||
                target == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            int destinationWorldId = StorageNetworkWorldUtility.GetObjectWorldId(target.gameObject);
            if (destinationWorldId < 0 ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId) ||
                !StorageSceneRegistry.IsLive(target))
            {
                return 0f;
            }

            EnsureOrdersLoaded();
            float moved = 0f;
            LeasedMaterialOrderBuffer.Clear();
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (IsOrderActive(order))
                {
                    LeasedMaterialOrderBuffer.Add(order);
                }
            }

            LeasedMaterialOrderBuffer.Sort(ProductionOrderDisplayIdComparer.Instance);
            foreach (ProductionOrderRecord order in LeasedMaterialOrderBuffer)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                bool hasMatchingQueue = false;
                foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
                {
                    if (assignment.Fabricator == fabricator &&
                        assignment.Recipe == recipe &&
                        GetRemainingQueueCount(order, assignment) > 0)
                    {
                        hasMatchingQueue = true;
                        break;
                    }
                }

                if (!hasMatchingQueue)
                {
                    continue;
                }

                foreach (ProductionOrderMaterialLease lease in order.MaterialLeases)
                {
                    if (lease.Material != tag)
                    {
                        continue;
                    }

                    if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    Storage source = ProductionNetworkInventoryCache.FindStorageByInstanceIdFromScene(
                        lease.SourceStorageInstanceId,
                        destinationWorldId);
                    if (!IsSourceUsableForDestination(source, target, tag))
                    {
                        continue;
                    }

                    float sourceAmount = source.GetAmountAvailable(tag);
                    float transferAmount = Mathf.Min(amount - moved, lease.Amount, sourceAmount, Mathf.Max(0f, target.RemainingCapacity()));
                    if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    // Connectivity can change between lease resolution and mutation. Revalidate
                    // at the exact transfer boundary so a stale relay/storage snapshot cannot
                    // move material across a disconnected world.
                    if (!IsSourceUsableForDestination(source, target, tag))
                    {
                        continue;
                    }

                    float transferred =
                        NetworkStorageTransferService.TransferMatchingItemsFromStorage(
                            source,
                            target,
                            tag,
                            transferAmount);
                    if (transferred > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        moved += transferred;
                    }
                }
            }

            return moved;
        }

        private void ApplyProductionPlan(ProductionPlanNode node, string orderKey, List<ProductionOrderMaterialLease> materialLeases)
        {
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child != null)
                {
                    ApplyProductionPlan(requirement.Child, orderKey, materialLeases);
                }
            }

            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                if (!IsFabricatorReachableFromWorld(assignment.Fabricator, node.WorldId) ||
                    node.Recipe == null)
                {
                    continue;
                }

                int queued = StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(assignment.Fabricator, node.Recipe);
                assignment.Fabricator.SetRecipeQueueCount(node.Recipe, (queued == ComplexFabricator.QUEUE_INFINITE ? 0 : Mathf.Max(0, queued)) + assignment.OrderCount);
                StorageNetworkFabricatorProgress.Invalidate(assignment.Fabricator);
                EnsureOrderAutomationEnabled(assignment.Fabricator, orderKey);
                DispatchRecipeIngredients(node, assignment, materialLeases);
            }
        }

        private static void EnsureOrderAutomationEnabled(ComplexFabricator fabricator, string orderKey)
        {
            int worldId = fabricator != null
                ? StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject)
                : -1;
            if (!IsFabricatorReachableFromWorld(fabricator, worldId))
            {
                return;
            }

            StorageNetworkMaterialRequester requester = fabricator != null ? fabricator.GetComponent<StorageNetworkMaterialRequester>() : null;
            if (requester != null)
            {
                int instanceId = StorageNetworkMaterialRequester.GetStorageInstanceId(fabricator.inStorage);
                if (instanceId != KPrefabID.InvalidInstanceID)
                {
                    if (!AutomationLeases.TryGetValue(instanceId, out OrderAutomationLease lease))
                    {
                        lease = new OrderAutomationLease(requester);
                        AutomationLeases[instanceId] = lease;
                    }

                    lease.OrderKeys.Add(orderKey);
                }

                requester.RequestEnabled = true;
                requester.CurrentMode = StorageNetworkMaterialRequester.RequestMode.SearchNetwork;
            }
        }

        private static void EnsureActiveOrderAutomationLeases()
        {
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order))
                {
                    continue;
                }

                foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
                {
                    int worldId = assignment?.Fabricator != null
                        ? StorageNetworkWorldUtility.GetObjectWorldId(
                            assignment.Fabricator.gameObject)
                        : -1;
                    if (IsFabricatorReachableFromWorld(assignment?.Fabricator, worldId))
                    {
                        EnsureOrderAutomationEnabled(assignment.Fabricator, order.Key);
                    }
                }
            }
        }

        private static void ReleaseOrderAutomation(string orderKey)
        {
            EmptyAutomationLeaseBuffer.Clear();
            foreach (KeyValuePair<int, OrderAutomationLease> pair in AutomationLeases)
            {
                if (!pair.Value.OrderKeys.Remove(orderKey) || pair.Value.OrderKeys.Count > 0)
                {
                    continue;
                }

                pair.Value.Restore();
                EmptyAutomationLeaseBuffer.Add(pair.Key);
            }

            foreach (int instanceId in EmptyAutomationLeaseBuffer)
            {
                AutomationLeases.Remove(instanceId);
            }
        }

        private void DispatchRecipeIngredients(ProductionPlanNode node, ProductionPlanAssignment assignment, List<ProductionOrderMaterialLease> materialLeases)
        {
            Storage target = assignment.Fabricator.inStorage;
            if (target == null)
            {
                return;
            }

            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                float required = requirement.RequiredAmount * assignment.OrderCount / Mathf.Max(1, node.OrderCount);
                float needed = Mathf.Max(0f, required - target.GetAmountAvailable(requirement.Material));
                TransferMaterialToStorage(requirement.Material, target, needed, materialLeases);
            }
        }

        private float TransferMaterialToStorage(Tag tag, Storage target, float amount, List<ProductionOrderMaterialLease> materialLeases)
        {
            float moved = 0f;
            if (target == null || amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return moved;
            }

            int destinationWorldId = StorageNetworkWorldUtility.GetObjectWorldId(target.gameObject);
            if (destinationWorldId < 0 ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId) ||
                !StorageSceneRegistry.IsLive(target))
            {
                return moved;
            }

            ProductionNetworkInventoryCache scopedInventory =
                Runtime.GetNetworkInventory(destinationWorldId);

            transferSourceBuffer.Clear();
            transferSourceSeenBuffer.Clear();
            if (materialLeases != null)
            {
                foreach (ProductionOrderMaterialLease lease in materialLeases)
                {
                    if (lease.Material != tag || lease.Amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    AddTransferSource(
                        transferSourceBuffer,
                        transferSourceSeenBuffer,
                        ProductionNetworkInventoryCache.FindStorageByInstanceIdFromScene(
                            lease.SourceStorageInstanceId,
                            destinationWorldId),
                        target,
                        tag);
                }
            }

            foreach (Storage storage in scopedInventory.SourceStorages)
            {
                AddTransferSource(
                    transferSourceBuffer,
                    transferSourceSeenBuffer,
                    storage,
                    target,
                    tag);
            }

            transferSourceBuffer.Sort(StorageSourceSortKeyComparer.Instance);
            for (int sourceIndex = 0;
                 sourceIndex < transferSourceBuffer.Count;
                 sourceIndex++)
            {
                Storage source = transferSourceBuffer[sourceIndex].Storage;
                if (!IsSourceUsableForDestination(source, target, tag))
                {
                    continue;
                }

                float transferAmount = Mathf.Min(amount - moved, source.GetAmountAvailable(tag), Mathf.Max(0f, target.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                if (!IsSourceUsableForDestination(source, target, tag))
                {
                    continue;
                }

                float transferred =
                    NetworkStorageTransferService.TransferMatchingItemsFromStorage(
                        source,
                        target,
                        tag,
                        transferAmount);
                if (transferred > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    moved += transferred;
                }
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }
            }

            return moved;
        }

        private static void AddTransferSource(
            List<StorageSourceSortKey> sources,
            HashSet<Storage> seen,
            Storage storage,
            Storage target,
            Tag tag)
        {
            if (!IsSourceUsableForDestination(storage, target, tag) || seen.Contains(storage))
            {
                return;
            }

            seen.Add(storage);
            sources.Add(new StorageSourceSortKey(
                storage,
                storage.GetAmountAvailable(tag),
                StorageItemUtility.GetStorageInstanceId(storage)));
        }

        private readonly struct StorageSourceSortKey
        {
            public StorageSourceSortKey(Storage storage, float amount, int instanceId)
            {
                Storage = storage;
                Amount = amount;
                InstanceId = instanceId;
            }

            public Storage Storage { get; }

            public float Amount { get; }

            public int InstanceId { get; }
        }

        private sealed class StorageSourceSortKeyComparer : IComparer<StorageSourceSortKey>
        {
            public static readonly StorageSourceSortKeyComparer Instance =
                new StorageSourceSortKeyComparer();

            public int Compare(StorageSourceSortKey left, StorageSourceSortKey right)
            {
                int compare = right.Amount.CompareTo(left.Amount);
                return compare != 0
                    ? compare
                    : left.InstanceId.CompareTo(right.InstanceId);
            }
        }

        private float GetReservedAmount(Tag tag, string ignoredOrderKey = null)
        {
            float reserved = 0f;
            int worldId = GetCurrentNetworkWorldId();
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order) ||
                    order.Key == ignoredOrderKey ||
                    !IsOrderReachableFromCurrentWorld(order))
                {
                    continue;
                }

                if (order.MaterialLeases.Count > 0)
                {
                    foreach (ProductionOrderMaterialLease lease in order.MaterialLeases)
                    {
                        if (lease.Material == tag &&
                            ProductionNetworkInventoryCache.FindStorageByInstanceIdFromScene(
                                lease.SourceStorageInstanceId,
                                worldId) != null)
                        {
                            reserved += lease.Amount;
                        }
                    }
                }
                else
                {
                    reserved += order.GetReservedAmount(tag);
                }
            }

            return reserved;
        }

        private float GetPendingProducedAmountAhead(Tag productTag)
        {
            float pending = 0f;
            int worldId = GetCurrentNetworkWorldId();
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order) ||
                    order.ProductTag != productTag ||
                    !IsOrderReachableFromCurrentWorld(order))
                {
                    continue;
                }

                float leased = 0f;
                if (order.OutputLeases.Count > 0)
                {
                    foreach (ProductionOrderOutputLease lease in order.OutputLeases)
                    {
                        ComplexFabricator producer =
                            ProductionOrderCenterCatalog.FindFabricatorByInstanceId(
                                lease.FabricatorInstanceId);
                        if (lease.ProductTag == productTag &&
                            IsFabricatorReachableFromWorld(producer, worldId))
                        {
                            leased += lease.Amount;
                        }
                    }
                }
                else
                {
                    leased = order.RequestedAmount;
                }

                pending += Mathf.Max(0f, leased - order.ProducedAtSubmit);
            }

            return pending;
        }
    }
}
