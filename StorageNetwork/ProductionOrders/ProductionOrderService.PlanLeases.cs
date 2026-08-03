using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private readonly List<StorageSourceSortKey> materialLeaseSourceBuffer =
            new List<StorageSourceSortKey>();
        private readonly Dictionary<QueueAssignmentKey, QueueAssignmentAccumulator>
            queueAssignmentBuffer =
                new Dictionary<QueueAssignmentKey, QueueAssignmentAccumulator>();

        private static Dictionary<Tag, float> BuildReservedMaterials(ProductionPlanNode node)
        {
            Dictionary<Tag, float> reservations = new Dictionary<Tag, float>();
            AddReservations(node, reservations);
            return reservations;
        }

        private static void AddReservations(ProductionPlanNode node, Dictionary<Tag, float> reservations)
        {
            if (node == null)
            {
                return;
            }

            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Material != Tag.Invalid && requirement.RequiredAmount > 0f)
                {
                    float reserved = Mathf.Min(requirement.RequiredAmount, requirement.AvailableAmount);
                    reservations[requirement.Material] = reservations.TryGetValue(requirement.Material, out float existing) ? existing + reserved : reserved;
                }

                AddReservations(requirement.Child, reservations);
            }
        }

        private List<ProductionOrderMaterialLease> BuildMaterialLeases(
            ProductionPlanNode node,
            Dictionary<Tag, float> reservations)
        {
            List<ProductionOrderMaterialLease> leases = new List<ProductionOrderMaterialLease>();
            reservations = reservations ?? new Dictionary<Tag, float>();
            int leaseWorldId = node != null && node.WorldId >= 0
                ? node.WorldId
                : GetCurrentNetworkWorldId();
            ProductionNetworkInventoryCache scopedInventory =
                Runtime.GetNetworkInventory(leaseWorldId);
            foreach (KeyValuePair<Tag, float> pair in reservations)
            {
                float remaining = pair.Value;
                List<StorageSourceSortKey> sources = materialLeaseSourceBuffer;
                sources.Clear();
                foreach (Storage storage in scopedInventory.SourceStorages)
                {
                    if (!ProductionNetworkInventoryCache.IsUsableSource(
                            storage,
                            leaseWorldId))
                    {
                        continue;
                    }

                    float available = Mathf.Max(
                        0f,
                        StorageNetworkContentIndexService.GetStorageAmount(
                            storage,
                            pair.Key,
                            allowStaleContent: false));
                    if (available > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        sources.Add(new StorageSourceSortKey(
                            storage,
                            available,
                            ProductionNetworkInventoryCache.GetComponentInstanceId(storage)));
                    }
                }

                sources.Sort(StorageSourceSortKeyComparer.Instance);
                foreach (StorageSourceSortKey source in sources)
                {
                    if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    Storage storage = source.Storage;
                    if (!ProductionNetworkInventoryCache.IsUsableSource(storage, leaseWorldId))
                    {
                        continue;
                    }

                    // Native Storage remains authoritative at the lease boundary.
                    float amount = Mathf.Min(
                        remaining,
                        Mathf.Max(0f, storage.GetAmountAvailable(pair.Key)));
                    if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    leases.Add(new ProductionOrderMaterialLease(pair.Key, amount, ProductionNetworkInventoryCache.GetComponentInstanceId(storage), string.Empty));
                    remaining -= amount;
                }
            }

            return leases;
        }

        private static List<ProductionOrderOutputLease> BuildOutputLeases(List<ProductionOrderQueueAssignment> assignments, Tag productTag, float requestedAmount)
        {
            List<ProductionOrderOutputLease> leases = new List<ProductionOrderOutputLease>();
            int totalCount = 0;
            if (assignments != null)
            {
                for (int i = 0; i < assignments.Count; i++)
                {
                    ProductionOrderQueueAssignment assignment = assignments[i];
                    if (assignment != null &&
                        assignment.Primary &&
                        IsOrderProductionFabricator(assignment.Fabricator))
                    {
                        totalCount += Mathf.Max(0, assignment.OrderCount);
                    }
                }

                for (int i = 0; i < assignments.Count; i++)
                {
                    ProductionOrderQueueAssignment assignment = assignments[i];
                    if (assignment == null ||
                        !assignment.Primary ||
                        !IsOrderProductionFabricator(assignment.Fabricator))
                    {
                        continue;
                    }

                    float amount = totalCount > 0
                        ? requestedAmount * assignment.OrderCount / totalCount
                        : requestedAmount;
                    leases.Add(new ProductionOrderOutputLease(
                        productTag,
                        amount,
                        ProductionNetworkInventoryCache.GetComponentInstanceId(
                            assignment.Fabricator),
                        assignment.Fabricator.GetProperName()));
                }
            }

            return leases;
        }

        private List<ProductionOrderQueueAssignment> BuildQueueAssignments(ProductionPlanNode node)
        {
            Dictionary<QueueAssignmentKey, QueueAssignmentAccumulator> assignments =
                queueAssignmentBuffer;
            assignments.Clear();
            AddQueueAssignments(node, assignments, null, true);
            List<ProductionOrderQueueAssignment> result = new List<ProductionOrderQueueAssignment>(assignments.Count);
            foreach (QueueAssignmentAccumulator accumulator in assignments.Values)
            {
                result.Add(accumulator.ToAssignment());
            }

            return result;
        }

        private static void AddQueueAssignments(
            ProductionPlanNode node,
            Dictionary<QueueAssignmentKey, QueueAssignmentAccumulator> assignments,
            string consumerName,
            bool primary)
        {
            if (node == null)
            {
                return;
            }

            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                AddQueueAssignments(requirement.Child, assignments, node.FabricatorName, false);
            }

            Tag outputTag = GetPlanOutputTag(node);
            string outputName = ProductionOrderFormatting.GetTagDisplayName(outputTag);
            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                if (IsOrderProductionFabricator(assignment.Fabricator) && node.Recipe != null && assignment.OrderCount > 0)
                {
                    string assignmentConsumerName = primary
                        ? assignment.Fabricator.GetProperName()
                        : consumerName;
                    QueueAssignmentKey key = new QueueAssignmentKey(
                        assignment.Fabricator,
                        node.Recipe,
                        outputTag,
                        assignmentConsumerName,
                        primary);
                    if (assignments.TryGetValue(key, out QueueAssignmentAccumulator existing))
                    {
                        existing.OrderCount += assignment.OrderCount;
                        assignments[key] = existing;
                    }
                    else
                    {
                        assignments[key] = new QueueAssignmentAccumulator(
                            assignment.Fabricator,
                            node.Recipe,
                            assignment.OrderCount,
                            outputTag,
                            outputName,
                            assignmentConsumerName,
                            primary);
                    }
                }
            }
        }

        private static Tag GetPlanOutputTag(ProductionPlanNode node)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(node?.Recipe, node != null ? node.ProductTag : Tag.Invalid) ??
                                                 ProductionRecipeCatalog.GetPrimaryResult(node?.Recipe);
            return result != null && result.material != Tag.Invalid ? result.material : Tag.Invalid;
        }

        private struct QueueAssignmentAccumulator
        {
            public readonly ComplexFabricator Fabricator;
            public readonly ComplexRecipe Recipe;
            public readonly Tag OutputTag;
            public readonly string OutputName;
            public readonly string ConsumerName;
            public readonly bool Primary;
            public int OrderCount;

            public QueueAssignmentAccumulator(
                ComplexFabricator fabricator,
                ComplexRecipe recipe,
                int orderCount,
                Tag outputTag,
                string outputName,
                string consumerName,
                bool primary)
            {
                Fabricator = fabricator;
                Recipe = recipe;
                OrderCount = orderCount;
                OutputTag = outputTag;
                OutputName = outputName;
                ConsumerName = consumerName;
                Primary = primary;
            }

            public ProductionOrderQueueAssignment ToAssignment()
            {
                return new ProductionOrderQueueAssignment(Fabricator, Recipe, OrderCount, OutputTag, OutputName, ConsumerName, Primary);
            }
        }

        private readonly struct QueueAssignmentKey : IEquatable<QueueAssignmentKey>
        {
            private readonly ComplexFabricator fabricator;
            private readonly ComplexRecipe recipe;
            private readonly Tag outputTag;
            private readonly string consumerName;
            private readonly bool primary;

            public QueueAssignmentKey(
                ComplexFabricator fabricator,
                ComplexRecipe recipe,
                Tag outputTag,
                string consumerName,
                bool primary)
            {
                this.fabricator = fabricator;
                this.recipe = recipe;
                this.outputTag = outputTag;
                this.consumerName = consumerName ?? string.Empty;
                this.primary = primary;
            }

            public bool Equals(QueueAssignmentKey other)
            {
                return ReferenceEquals(fabricator, other.fabricator) &&
                       ReferenceEquals(recipe, other.recipe) &&
                       outputTag == other.outputTag &&
                       primary == other.primary &&
                       string.Equals(
                           consumerName,
                           other.consumerName,
                           StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is QueueAssignmentKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = fabricator != null
                        ? RuntimeHelpers.GetHashCode(fabricator)
                        : 0;
                    hashCode = (hashCode * 397) ^
                               (recipe != null ? RuntimeHelpers.GetHashCode(recipe) : 0);
                    hashCode = (hashCode * 397) ^ outputTag.GetHashCode();
                    hashCode = (hashCode * 397) ^
                               StringComparer.Ordinal.GetHashCode(consumerName);
                    return (hashCode * 397) ^ primary.GetHashCode();
                }
            }
        }

    }
}
