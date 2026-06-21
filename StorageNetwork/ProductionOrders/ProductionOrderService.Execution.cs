using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
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

            EnsureOrdersLoaded();
            float moved = 0f;
            List<ProductionOrderRecord> orders = new List<ProductionOrderRecord>();
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (IsOrderActive(order))
                {
                    orders.Add(order);
                }
            }

            orders.Sort((left, right) => left.DisplayId.CompareTo(right.DisplayId));
            foreach (ProductionOrderRecord order in orders)
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

                    Storage source = ProductionNetworkInventoryCache.FindStorageByInstanceIdFromScene(lease.SourceStorageInstanceId);
                    if (source == null || source == target || StorageNetworkStorageRules.IsProductionStorage(source))
                    {
                        continue;
                    }

                    float sourceAmount = source.GetAmountAvailable(tag);
                    float transferAmount = Mathf.Min(amount - moved, lease.Amount, sourceAmount, Mathf.Max(0f, target.RemainingCapacity()));
                    if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    moved += source.Transfer(target, tag, transferAmount, block_events: false, hide_popups: true);
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
                if (!IsOrderProductionFabricator(assignment.Fabricator) || node.Recipe == null)
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
            if (!IsOrderProductionFabricator(fabricator))
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
                    if (IsOrderProductionFabricator(assignment.Fabricator))
                    {
                        EnsureOrderAutomationEnabled(assignment.Fabricator, order.Key);
                    }
                }
            }
        }

        private static void ReleaseOrderAutomation(string orderKey)
        {
            List<int> emptyLeases = new List<int>();
            foreach (KeyValuePair<int, OrderAutomationLease> pair in AutomationLeases)
            {
                if (!pair.Value.OrderKeys.Remove(orderKey) || pair.Value.OrderKeys.Count > 0)
                {
                    continue;
                }

                pair.Value.Restore();
                emptyLeases.Add(pair.Key);
            }

            foreach (int instanceId in emptyLeases)
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

            List<Storage> sources = new List<Storage>();
            HashSet<Storage> seen = new HashSet<Storage>();
            if (materialLeases != null)
            {
                foreach (ProductionOrderMaterialLease lease in materialLeases)
                {
                    if (lease.Material != tag || lease.Amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    AddTransferSource(sources, seen, networkInventory.FindStorageByInstanceId(lease.SourceStorageInstanceId), target, tag);
                }
            }

            foreach (Storage storage in networkInventory.SourceStorages)
            {
                AddTransferSource(sources, seen, storage, target, tag);
            }

            sources.Sort((left, right) => right.GetAmountAvailable(tag).CompareTo(left.GetAmountAvailable(tag)));
            foreach (Storage source in sources)
            {
                float transferAmount = Mathf.Min(amount - moved, source.GetAmountAvailable(tag), Mathf.Max(0f, target.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                moved += source.Transfer(target, tag, transferAmount, block_events: false, hide_popups: true);
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }
            }

            return moved;
        }

        private static void AddTransferSource(List<Storage> sources, HashSet<Storage> seen, Storage storage, Storage target, Tag tag)
        {
            if (storage == null ||
                storage == target ||
                seen.Contains(storage) ||
                StorageNetworkStorageRules.IsProductionStorage(storage) ||
                storage.GetAmountAvailable(tag) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            seen.Add(storage);
            sources.Add(storage);
        }

        private static float GetReservedAmount(Tag tag, string ignoredOrderKey = null)
        {
            float reserved = 0f;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order) || order.Key == ignoredOrderKey)
                {
                    continue;
                }

                if (order.MaterialLeases.Count > 0)
                {
                    foreach (ProductionOrderMaterialLease lease in order.MaterialLeases)
                    {
                        if (lease.Material == tag)
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

        private static float GetPendingProducedAmountAhead(Tag productTag)
        {
            float pending = 0f;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order) || order.ProductTag != productTag)
                {
                    continue;
                }

                float leased = 0f;
                if (order.OutputLeases.Count > 0)
                {
                    foreach (ProductionOrderOutputLease lease in order.OutputLeases)
                    {
                        if (lease.ProductTag == productTag)
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
