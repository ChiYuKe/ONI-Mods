using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private readonly PlannerContext shadowPlannerContext = new PlannerContext();

        private ProductionPlanNode ValidateProductionPlanShadow(
            ProductionPlanNode optimized,
            ComplexRecipe recipe,
            List<ComplexFabricator> fabricators,
            Tag productTag,
            float requestedAmount,
            int worldId)
        {
            // The production planner is a hot maintenance path.  When rollout
            // diagnostics are disabled it pays exactly one read-only branch.
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return optimized;
            }

            int version = GetProductionPlanValidationVersion();
            if (!StorageNetworkShadowValidationService.ShouldValidate(
                    StorageNetworkShadowArea.ProductionPlan,
                    worldId,
                    version))
            {
                return optimized;
            }

            shadowPlannerContext.Begin(
                worldId,
                enforceTickBudget: false,
                MaxPlannerNodeExpansions,
                MaxPlannerCandidateEvaluations,
                PlannerTickBudgetMilliseconds,
                useLegacyRouteScan: true);
            ProductionPlanNode legacy = BuildProductionPlan(
                recipe,
                fabricators,
                productTag,
                requestedAmount,
                0,
                shadowPlannerContext);
            bool plansEqual = AreProductionPlansEqual(optimized, legacy);
            bool artifactsEqual = ValidateProductionPlanArtifacts(
                optimized,
                legacy,
                productTag,
                requestedAmount,
                worldId,
                out int optimizedArtifactSignature,
                out int legacyArtifactSignature,
                out int nativeLeaseSignature);
            if (plansEqual && artifactsEqual)
            {
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.ProductionPlan,
                    worldId,
                    version);
                return StorageNetworkShadowValidationService.ShouldUseFallback(
                    StorageNetworkShadowArea.ProductionPlan,
                    worldId,
                    version)
                    ? legacy
                    : optimized;
            }

            int optimizedSignature = GetProductionPlanSignature(optimized);
            int legacySignature = GetProductionPlanSignature(legacy);
            int mismatchSignature = unchecked(
                ((((optimizedSignature * 397) ^ legacySignature) * 397) ^
                  optimizedArtifactSignature) * 397 ^
                 legacyArtifactSignature ^ nativeLeaseSignature);
            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.ProductionPlan,
                worldId,
                version,
                mismatchSignature,
                $"optimized={optimizedSignature}, legacy={legacySignature}, " +
                $"optimizedArtifacts={optimizedArtifactSignature}, " +
                $"legacyArtifacts={legacyArtifactSignature}, " +
                $"nativeLeases={nativeLeaseSignature}, version={version}");

            // Rebuild only the production recipe/index snapshot. Storage remains the
            // native source of truth and the current call falls back to the legacy plan.
            Runtime.InvalidateRecipeSnapshots();
            SetRecipeSnapshot(Runtime.GetRecipeSnapshot(orderCenterScope, true));
            return legacy;
        }

        private bool ValidateProductionPlanArtifacts(
            ProductionPlanNode optimized,
            ProductionPlanNode legacy,
            Tag productTag,
            float requestedAmount,
            int worldId,
            out int optimizedSignature,
            out int legacySignature,
            out int nativeLeaseSignature)
        {
            ProductionPlanArtifacts optimizedArtifacts = BuildProductionPlanArtifacts(
                optimized,
                productTag,
                requestedAmount);
            ProductionPlanArtifacts legacyArtifacts = BuildProductionPlanArtifacts(
                legacy,
                productTag,
                requestedAmount);
            List<ProductionOrderMaterialLease> nativeLeases =
                BuildNativeMaterialLeaseReference(
                    optimized,
                    optimizedArtifacts.ReservedMaterials,
                    worldId);

            optimizedSignature = GetProductionArtifactSignature(optimizedArtifacts);
            legacySignature = GetProductionArtifactSignature(legacyArtifacts);
            nativeLeaseSignature = GetMaterialLeaseSignature(nativeLeases);
            return AreProductionArtifactsEqual(optimizedArtifacts, legacyArtifacts) &&
                   AreMaterialLeasesEqual(
                       optimizedArtifacts.MaterialLeases,
                       nativeLeases);
        }

        private ProductionPlanArtifacts BuildProductionPlanArtifacts(
            ProductionPlanNode plan,
            Tag productTag,
            float requestedAmount)
        {
            Dictionary<Tag, float> reservedMaterials = BuildReservedMaterials(plan);
            List<ProductionOrderQueueAssignment> queueAssignments =
                BuildQueueAssignments(plan);
            List<ProductionOrderMaterialLease> materialLeases =
                BuildMaterialLeases(plan, reservedMaterials);
            List<ProductionOrderOutputLease> outputLeases =
                BuildOutputLeases(queueAssignments, productTag, requestedAmount);
            return new ProductionPlanArtifacts(
                reservedMaterials,
                queueAssignments,
                materialLeases,
                outputLeases);
        }

        private List<ProductionOrderMaterialLease> BuildNativeMaterialLeaseReference(
            ProductionPlanNode plan,
            Dictionary<Tag, float> reservations,
            int fallbackWorldId)
        {
            List<ProductionOrderMaterialLease> leases =
                new List<ProductionOrderMaterialLease>();
            int leaseWorldId = plan != null && plan.WorldId >= 0
                ? plan.WorldId
                : fallbackWorldId;
            ProductionNetworkInventoryCache scopedInventory =
                Runtime.GetNetworkInventory(leaseWorldId);
            if (reservations == null || scopedInventory == null)
            {
                return leases;
            }

            foreach (KeyValuePair<Tag, float> pair in reservations)
            {
                float remaining = Mathf.Max(0f, pair.Value);
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
                        storage.GetAmountAvailable(pair.Key));
                    if (available > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        sources.Add(new StorageSourceSortKey(
                            storage,
                            available,
                            ProductionNetworkInventoryCache.GetComponentInstanceId(
                                storage)));
                    }
                }

                sources.Sort(StorageSourceSortKeyComparer.Instance);
                for (int i = 0; i < sources.Count &&
                                remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
                     i++)
                {
                    Storage storage = sources[i].Storage;
                    if (!ProductionNetworkInventoryCache.IsUsableSource(
                            storage,
                            leaseWorldId))
                    {
                        continue;
                    }

                    float amount = Mathf.Min(
                        remaining,
                        Mathf.Max(0f, storage.GetAmountAvailable(pair.Key)));
                    if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    leases.Add(new ProductionOrderMaterialLease(
                        pair.Key,
                        amount,
                        ProductionNetworkInventoryCache.GetComponentInstanceId(
                            storage),
                        string.Empty));
                    remaining -= amount;
                }
            }

            materialLeaseSourceBuffer.Clear();
            return leases;
        }

        private static bool AreProductionArtifactsEqual(
            ProductionPlanArtifacts left,
            ProductionPlanArtifacts right)
        {
            return AreReservedMaterialsEqual(
                       left.ReservedMaterials,
                       right.ReservedMaterials) &&
                   AreQueueAssignmentsEqual(
                       left.QueueAssignments,
                       right.QueueAssignments) &&
                   AreMaterialLeasesEqual(
                       left.MaterialLeases,
                       right.MaterialLeases) &&
                   AreOutputLeasesEqual(
                       left.OutputLeases,
                       right.OutputLeases);
        }

        private static bool AreReservedMaterialsEqual(
            Dictionary<Tag, float> left,
            Dictionary<Tag, float> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            if (left == null)
            {
                return true;
            }

            foreach (KeyValuePair<Tag, float> pair in left)
            {
                if (right == null ||
                    !right.TryGetValue(pair.Key, out float amount) ||
                    !StorageNetworkShadowValidationService.ApproximatelyEqual(
                        pair.Value,
                        amount))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreQueueAssignmentsEqual(
            List<ProductionOrderQueueAssignment> left,
            List<ProductionOrderQueueAssignment> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            if (left == null)
            {
                return true;
            }

            bool[] matched = new bool[right.Count];
            for (int i = 0; i < left.Count; i++)
            {
                ProductionOrderQueueAssignment assignment = left[i];
                bool found = false;
                for (int j = 0; j < right.Count; j++)
                {
                    if (!matched[j] &&
                        AreQueueAssignmentsEqual(assignment, right[j]))
                    {
                        matched[j] = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreQueueAssignmentsEqual(
            ProductionOrderQueueAssignment left,
            ProductionOrderQueueAssignment right)
        {
            return left != null &&
                   right != null &&
                   ProductionOrderCenterCatalog.GetInstanceId(left.Fabricator) ==
                   ProductionOrderCenterCatalog.GetInstanceId(right.Fabricator) &&
                   ProductionRecipeCatalog.GetRecipeKey(left.Recipe) ==
                   ProductionRecipeCatalog.GetRecipeKey(right.Recipe) &&
                   left.OrderCount == right.OrderCount &&
                   left.OutputTag == right.OutputTag &&
                   left.Primary == right.Primary &&
                   string.Equals(
                       left.OutputName,
                       right.OutputName,
                       System.StringComparison.Ordinal) &&
                   string.Equals(
                       left.ConsumerName,
                       right.ConsumerName,
                       System.StringComparison.Ordinal);
        }

        private static bool AreMaterialLeasesEqual(
            List<ProductionOrderMaterialLease> left,
            List<ProductionOrderMaterialLease> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            if (left == null)
            {
                return true;
            }

            bool[] matched = new bool[right.Count];
            for (int i = 0; i < left.Count; i++)
            {
                ProductionOrderMaterialLease lease = left[i];
                bool found = false;
                for (int j = 0; j < right.Count; j++)
                {
                    ProductionOrderMaterialLease candidate = right[j];
                    if (!matched[j] &&
                        lease != null &&
                        candidate != null &&
                        lease.Material == candidate.Material &&
                        lease.SourceStorageInstanceId ==
                        candidate.SourceStorageInstanceId &&
                        string.Equals(
                            lease.ConsumerName,
                            candidate.ConsumerName,
                            System.StringComparison.Ordinal) &&
                        StorageNetworkShadowValidationService.ApproximatelyEqual(
                            lease.Amount,
                            candidate.Amount))
                    {
                        matched[j] = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreOutputLeasesEqual(
            List<ProductionOrderOutputLease> left,
            List<ProductionOrderOutputLease> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            if (left == null)
            {
                return true;
            }

            bool[] matched = new bool[right.Count];
            for (int i = 0; i < left.Count; i++)
            {
                ProductionOrderOutputLease lease = left[i];
                bool found = false;
                for (int j = 0; j < right.Count; j++)
                {
                    ProductionOrderOutputLease candidate = right[j];
                    if (!matched[j] &&
                        lease != null &&
                        candidate != null &&
                        lease.ProductTag == candidate.ProductTag &&
                        lease.FabricatorInstanceId ==
                        candidate.FabricatorInstanceId &&
                        string.Equals(
                            lease.ProducerName,
                            candidate.ProducerName,
                            System.StringComparison.Ordinal) &&
                        StorageNetworkShadowValidationService.ApproximatelyEqual(
                            lease.Amount,
                            candidate.Amount))
                    {
                        matched[j] = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetProductionArtifactSignature(
            ProductionPlanArtifacts artifacts)
        {
            unchecked
            {
                int signature = 17;
                int commutative = 0;
                foreach (KeyValuePair<Tag, float> pair in
                         artifacts.ReservedMaterials)
                {
                    commutative += (pair.Key.GetHashCode() * 397) ^
                                   QuantizePlanAmount(pair.Value);
                }

                signature = (signature * 397) ^ commutative;
                commutative = 0;
                for (int i = 0; i < artifacts.QueueAssignments.Count; i++)
                {
                    ProductionOrderQueueAssignment assignment =
                        artifacts.QueueAssignments[i];
                    int item = ProductionOrderCenterCatalog.GetInstanceId(
                        assignment?.Fabricator);
                    item = (item * 397) ^ GetStableStringHash(
                        ProductionRecipeCatalog.GetRecipeKey(
                            assignment?.Recipe));
                    item = (item * 397) ^ (assignment?.OrderCount ?? 0);
                    item = (item * 397) ^
                           (assignment?.OutputTag.GetHashCode() ?? 0);
                    item = (item * 397) ^
                           GetStableStringHash(assignment?.ConsumerName);
                    item = (item * 397) ^
                           GetStableStringHash(assignment?.OutputName);
                    item = (item * 397) ^
                           ((assignment?.Primary ?? false) ? 1 : 0);
                    commutative += item;
                }

                signature = (signature * 397) ^ commutative;
                signature = (signature * 397) ^
                            GetMaterialLeaseSignature(artifacts.MaterialLeases);
                commutative = 0;
                for (int i = 0; i < artifacts.OutputLeases.Count; i++)
                {
                    ProductionOrderOutputLease lease = artifacts.OutputLeases[i];
                    int item = lease?.ProductTag.GetHashCode() ?? 0;
                    item = (item * 397) ^
                           (lease?.FabricatorInstanceId ?? 0);
                    item = (item * 397) ^
                           QuantizePlanAmount(lease?.Amount ?? 0f);
                    item = (item * 397) ^
                           GetStableStringHash(lease?.ProducerName);
                    commutative += item;
                }

                return (signature * 397) ^ commutative;
            }
        }

        private static int GetMaterialLeaseSignature(
            List<ProductionOrderMaterialLease> leases)
        {
            unchecked
            {
                int signature = 0;
                if (leases == null)
                {
                    return signature;
                }

                for (int i = 0; i < leases.Count; i++)
                {
                    ProductionOrderMaterialLease lease = leases[i];
                    int item = lease?.Material.GetHashCode() ?? 0;
                    item = (item * 397) ^
                           (lease?.SourceStorageInstanceId ?? 0);
                    item = (item * 397) ^
                           QuantizePlanAmount(lease?.Amount ?? 0f);
                    item = (item * 397) ^
                           GetStableStringHash(lease?.ConsumerName);
                    signature += item;
                }

                return signature;
            }
        }

        private sealed class ProductionPlanArtifacts
        {
            public ProductionPlanArtifacts(
                Dictionary<Tag, float> reservedMaterials,
                List<ProductionOrderQueueAssignment> queueAssignments,
                List<ProductionOrderMaterialLease> materialLeases,
                List<ProductionOrderOutputLease> outputLeases)
            {
                ReservedMaterials = reservedMaterials;
                QueueAssignments = queueAssignments;
                MaterialLeases = materialLeases;
                OutputLeases = outputLeases;
            }

            public Dictionary<Tag, float> ReservedMaterials { get; }
            public List<ProductionOrderQueueAssignment> QueueAssignments { get; }
            public List<ProductionOrderMaterialLease> MaterialLeases { get; }
            public List<ProductionOrderOutputLease> OutputLeases { get; }
        }

        private static int GetProductionPlanValidationVersion()
        {
            unchecked
            {
                return (Runtime.GetPlanningVersion() * 397) ^ OrderVersion;
            }
        }

        private static bool AreProductionPlansEqual(
            ProductionPlanNode left,
            ProductionPlanNode right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null ||
                !ReferenceEquals(left.Recipe, right.Recipe) ||
                left.ProductTag != right.ProductTag ||
                left.OrderCount != right.OrderCount ||
                left.WorldId != right.WorldId ||
                !StorageNetworkShadowValidationService.ApproximatelyEqual(
                    left.OutputAmount,
                    right.OutputAmount) ||
                left.Assignments.Count != right.Assignments.Count ||
                left.Requirements.Count != right.Requirements.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Assignments.Count; i++)
            {
                ProductionPlanAssignment leftAssignment = left.Assignments[i];
                ProductionPlanAssignment rightAssignment = right.Assignments[i];
                if (!ReferenceEquals(leftAssignment?.Fabricator, rightAssignment?.Fabricator) ||
                    leftAssignment?.OrderCount != rightAssignment?.OrderCount ||
                    !StorageNetworkShadowValidationService.ApproximatelyEqual(
                        leftAssignment?.OutputAmount ?? 0f,
                        rightAssignment?.OutputAmount ?? 0f))
                {
                    return false;
                }
            }

            for (int i = 0; i < left.Requirements.Count; i++)
            {
                ProductionPlanRequirement leftRequirement = left.Requirements[i];
                ProductionPlanRequirement rightRequirement = right.Requirements[i];
                if (leftRequirement?.Material != rightRequirement?.Material ||
                    !StorageNetworkShadowValidationService.ApproximatelyEqual(
                        leftRequirement?.RequiredAmount ?? 0f,
                        rightRequirement?.RequiredAmount ?? 0f) ||
                    !StorageNetworkShadowValidationService.ApproximatelyEqual(
                        leftRequirement?.AvailableAmount ?? 0f,
                        rightRequirement?.AvailableAmount ?? 0f) ||
                    !AreProductionPlansEqual(
                        leftRequirement?.Child,
                        rightRequirement?.Child))
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetProductionPlanSignature(ProductionPlanNode node)
        {
            if (node == null)
            {
                return 0;
            }

            unchecked
            {
                int signature = GetStableStringHash(
                    ProductionRecipeCatalog.GetRecipeKey(node.Recipe));
                signature = (signature * 397) ^ node.ProductTag.GetHashCode();
                signature = (signature * 397) ^ node.OrderCount;
                signature = (signature * 397) ^ node.WorldId;
                signature = (signature * 397) ^ QuantizePlanAmount(node.OutputAmount);
                for (int i = 0; i < node.Assignments.Count; i++)
                {
                    ProductionPlanAssignment assignment = node.Assignments[i];
                    signature = (signature * 397) ^
                                (assignment?.Fabricator != null
                                    ? ProductionOrderCenterCatalog.GetInstanceId(
                                        assignment.Fabricator)
                                    : 0);
                    signature = (signature * 397) ^ (assignment?.OrderCount ?? 0);
                    signature = (signature * 397) ^
                                QuantizePlanAmount(assignment?.OutputAmount ?? 0f);
                }

                for (int i = 0; i < node.Requirements.Count; i++)
                {
                    ProductionPlanRequirement requirement = node.Requirements[i];
                    signature = (signature * 397) ^
                                (requirement?.Material.GetHashCode() ?? 0);
                    signature = (signature * 397) ^
                                QuantizePlanAmount(requirement?.RequiredAmount ?? 0f);
                    signature = (signature * 397) ^
                                QuantizePlanAmount(requirement?.AvailableAmount ?? 0f);
                    signature = (signature * 397) ^
                                GetProductionPlanSignature(requirement?.Child);
                }

                return signature;
            }
        }

        private static int GetStableStringHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash ^= value[i];
                        hash *= 16777619u;
                    }
                }

                return (int)hash;
            }
        }

        private static int QuantizePlanAmount(float amount)
        {
            if (float.IsNaN(amount))
            {
                return int.MinValue;
            }

            if (float.IsPositiveInfinity(amount))
            {
                return int.MaxValue;
            }

            if (float.IsNegativeInfinity(amount))
            {
                return int.MinValue + 1;
            }

            double quantized = System.Math.Round((double)amount * 1000d);
            long bits = quantized >= long.MaxValue
                ? long.MaxValue
                : quantized <= long.MinValue
                    ? long.MinValue
                    : (long)quantized;
            return unchecked((int)(bits ^ (bits >> 32)));
        }
    }
}
