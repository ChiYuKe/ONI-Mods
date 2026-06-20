using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
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

        private List<ProductionOrderMaterialLease> BuildMaterialLeases(ProductionPlanNode node)
        {
            List<ProductionOrderMaterialLease> leases = new List<ProductionOrderMaterialLease>();
            Dictionary<Tag, float> reservations = BuildReservedMaterials(node);
            List<Storage> sources = new List<Storage>();
            foreach (Storage storage in networkInventory.SourceStorages)
            {
                if (storage != null && !StorageNetworkStorageRules.IsProductionStorage(storage))
                {
                    sources.Add(storage);
                }
            }

            foreach (KeyValuePair<Tag, float> pair in reservations)
            {
                float remaining = pair.Value;
                sources.Sort((left, right) => right.GetAmountAvailable(pair.Key).CompareTo(left.GetAmountAvailable(pair.Key)));
                foreach (Storage storage in sources)
                {
                    if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    float amount = Mathf.Min(remaining, Mathf.Max(0f, storage.GetAmountAvailable(pair.Key)));
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
            List<ProductionOrderQueueAssignment> primaryAssignments = (assignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment != null && assignment.Primary && IsOrderProductionFabricator(assignment.Fabricator))
                .ToList();
            int totalCount = primaryAssignments.Sum(assignment => Mathf.Max(0, assignment.OrderCount));
            foreach (ProductionOrderQueueAssignment assignment in primaryAssignments)
            {
                float amount = totalCount > 0 ? requestedAmount * assignment.OrderCount / totalCount : requestedAmount;
                leases.Add(new ProductionOrderOutputLease(productTag, amount, ProductionNetworkInventoryCache.GetComponentInstanceId(assignment.Fabricator), assignment.Fabricator.GetProperName()));
            }

            return leases;
        }

        private static List<ProductionOrderQueueAssignment> BuildQueueAssignments(ProductionPlanNode node)
        {
            Dictionary<string, QueueAssignmentAccumulator> assignments = new Dictionary<string, QueueAssignmentAccumulator>();
            AddQueueAssignments(node, assignments, null, true);
            List<ProductionOrderQueueAssignment> result = new List<ProductionOrderQueueAssignment>(assignments.Count);
            foreach (QueueAssignmentAccumulator accumulator in assignments.Values)
            {
                result.Add(accumulator.ToAssignment());
            }

            return result;
        }

        private static void AddQueueAssignments(ProductionPlanNode node, Dictionary<string, QueueAssignmentAccumulator> assignments, string consumerName, bool primary)
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
                    ProductionOrderQueueAssignment queueAssignment = new ProductionOrderQueueAssignment(
                        assignment.Fabricator,
                        node.Recipe,
                        assignment.OrderCount,
                        outputTag,
                        outputName,
                        primary ? assignment.Fabricator.GetProperName() : consumerName,
                        primary);
                    string key = string.Format(
                        "{0}|{1}|{2}|{3}|{4}",
                        queueAssignment.Fabricator.GetInstanceID(),
                        queueAssignment.Recipe.id,
                        queueAssignment.OutputTag.Name,
                        queueAssignment.ConsumerName,
                        queueAssignment.Primary);
                    if (assignments.TryGetValue(key, out QueueAssignmentAccumulator existing))
                    {
                        existing.OrderCount += queueAssignment.OrderCount;
                    }
                    else
                    {
                        assignments[key] = new QueueAssignmentAccumulator(queueAssignment);
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

        private sealed class QueueAssignmentAccumulator
        {
            public readonly ComplexFabricator Fabricator;
            public readonly ComplexRecipe Recipe;
            public readonly Tag OutputTag;
            public readonly string OutputName;
            public readonly string ConsumerName;
            public readonly bool Primary;
            public int OrderCount;

            public QueueAssignmentAccumulator(ProductionOrderQueueAssignment assignment)
            {
                Fabricator = assignment.Fabricator;
                Recipe = assignment.Recipe;
                OrderCount = assignment.OrderCount;
                OutputTag = assignment.OutputTag;
                OutputName = assignment.OutputName;
                ConsumerName = assignment.ConsumerName;
                Primary = assignment.Primary;
            }

            public ProductionOrderQueueAssignment ToAssignment()
            {
                return new ProductionOrderQueueAssignment(Fabricator, Recipe, OrderCount, OutputTag, OutputName, ConsumerName, Primary);
            }
        }
    }
}
