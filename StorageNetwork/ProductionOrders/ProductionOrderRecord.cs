using System.Collections.Generic;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed class ProductionOrderRecord
    {
        public ProductionOrderRecord(
            string key,
            int displayId,
            Tag productTag,
            string productName,
            string recipeKey,
            float requestedAmount,
            int orderCount,
            float stockAtSubmit,
            float allocationOffsetAtSubmit,
            Dictionary<Tag, float> reservedMaterials,
            List<ProductionOrderQueueAssignment> queueAssignments,
            List<ProductionOrderMaterialLease> materialLeases,
            List<ProductionOrderOutputLease> outputLeases,
            float createdCycle,
            bool isAutomatic = false)
        {
            Key = key;
            DisplayId = displayId;
            ProductTag = productTag;
            ProductName = productName;
            RecipeKey = recipeKey;
            RequestedAmount = requestedAmount;
            LastSubmittedAmount = requestedAmount;
            OrderCount = orderCount;
            StockAtSubmit = stockAtSubmit;
            AllocationOffsetAtSubmit = allocationOffsetAtSubmit;
            ReservedMaterials = reservedMaterials ?? new Dictionary<Tag, float>();
            QueueAssignments = queueAssignments ?? new List<ProductionOrderQueueAssignment>();
            MaterialLeases = materialLeases ?? new List<ProductionOrderMaterialLease>();
            OutputLeases = outputLeases ?? new List<ProductionOrderOutputLease>();
            CreatedCycle = createdCycle;
            LastActivityCycle = createdCycle;
            IsAutomatic = isAutomatic;
            State = ProductionOrderState.Submitted;
        }

        public ProductionOrderRecord(
            string key,
            int displayId,
            Tag productTag,
            string productName,
            string recipeKey,
            float requestedAmount,
            float lastSubmittedAmount,
            int orderCount,
            float stockAtSubmit,
            float allocationOffsetAtSubmit,
            float producedAtSubmit,
            Dictionary<Tag, float> reservedMaterials,
            List<ProductionOrderQueueAssignment> queueAssignments,
            List<ProductionOrderMaterialLease> materialLeases,
            List<ProductionOrderOutputLease> outputLeases,
            float createdCycle,
            float completedCycle,
            float lastActivityCycle,
            float lastObservedProducedAmount,
            float lastObservedQueueLoad,
            string abnormalReason,
            int mergeCount,
            ProductionOrderState state,
            bool isAutomatic = false)
        {
            Key = key;
            DisplayId = displayId;
            ProductTag = productTag;
            ProductName = productName;
            RecipeKey = recipeKey;
            RequestedAmount = requestedAmount;
            LastSubmittedAmount = lastSubmittedAmount;
            OrderCount = orderCount;
            StockAtSubmit = stockAtSubmit;
            AllocationOffsetAtSubmit = allocationOffsetAtSubmit;
            ProducedAtSubmit = producedAtSubmit;
            ReservedMaterials = reservedMaterials ?? new Dictionary<Tag, float>();
            QueueAssignments = queueAssignments ?? new List<ProductionOrderQueueAssignment>();
            MaterialLeases = materialLeases ?? new List<ProductionOrderMaterialLease>();
            OutputLeases = outputLeases ?? new List<ProductionOrderOutputLease>();
            CreatedCycle = createdCycle;
            CompletedCycle = completedCycle;
            LastActivityCycle = lastActivityCycle;
            LastObservedProducedAmount = lastObservedProducedAmount;
            LastObservedQueueLoad = lastObservedQueueLoad;
            AbnormalReason = abnormalReason;
            MergeCount = mergeCount;
            IsAutomatic = isAutomatic;
            State = state;
        }

        public string Key { get; }

        public int DisplayId { get; }

        public Tag ProductTag { get; }

        public string ProductName { get; }

        public string RecipeKey { get; }

        public float RequestedAmount { get; private set; }

        public float LastSubmittedAmount { get; private set; }

        public int OrderCount { get; private set; }

        public float StockAtSubmit { get; private set; }

        public float AllocationOffsetAtSubmit { get; private set; }

        public float ProducedAtSubmit { get; set; }

        public Dictionary<Tag, float> ReservedMaterials { get; }

        public List<ProductionOrderQueueAssignment> QueueAssignments { get; }

        public List<ProductionOrderMaterialLease> MaterialLeases { get; }

        public List<ProductionOrderOutputLease> OutputLeases { get; }

        public float CreatedCycle { get; }

        public float CompletedCycle { get; set; }

        public float LastActivityCycle { get; private set; }

        public float LastObservedProducedAmount { get; private set; }

        public float LastObservedQueueLoad { get; private set; }

        public string AbnormalReason { get; set; }

        public int MergeCount { get; private set; }

        public bool IsAutomatic { get; private set; }

        public ProductionOrderState State { get; set; }

        public void Merge(
            float requestedAmount,
            int orderCount,
            Dictionary<Tag, float> reservedMaterials,
            List<ProductionOrderQueueAssignment> queueAssignments,
            List<ProductionOrderMaterialLease> materialLeases,
            List<ProductionOrderOutputLease> outputLeases,
            float currentCycle,
            bool isAutomatic = false)
        {
            RequestedAmount += requestedAmount;
            LastSubmittedAmount = requestedAmount;
            OrderCount += orderCount;
            MergeCount++;
            IsAutomatic = IsAutomatic || isAutomatic;
            State = ProductionOrderState.Submitted;
            LastActivityCycle = currentCycle;
            foreach (KeyValuePair<Tag, float> pair in reservedMaterials)
            {
                ReservedMaterials[pair.Key] = ReservedMaterials.TryGetValue(pair.Key, out float existing) ? existing + pair.Value : pair.Value;
            }

            if (queueAssignments != null)
            {
                QueueAssignments.AddRange(queueAssignments);
            }

            if (materialLeases != null)
            {
                MaterialLeases.AddRange(materialLeases);
            }

            if (outputLeases != null)
            {
                OutputLeases.AddRange(outputLeases);
            }
        }

        public bool RefreshPlan(
            int orderCount,
            Dictionary<Tag, float> reservedMaterials,
            List<ProductionOrderQueueAssignment> queueAssignments,
            List<ProductionOrderMaterialLease> materialLeases,
            List<ProductionOrderOutputLease> outputLeases)
        {
            bool changed = OrderCount != orderCount ||
                           !AreReservedMaterialsEqual(ReservedMaterials, reservedMaterials) ||
                           !AreQueueAssignmentsEqual(QueueAssignments, queueAssignments) ||
                           !AreMaterialLeasesEqual(MaterialLeases, materialLeases) ||
                           !AreOutputLeasesEqual(OutputLeases, outputLeases);
            OrderCount = orderCount;
            ReservedMaterials.Clear();
            foreach (KeyValuePair<Tag, float> pair in reservedMaterials ?? new Dictionary<Tag, float>())
            {
                ReservedMaterials[pair.Key] = pair.Value;
            }

            QueueAssignments.Clear();
            if (queueAssignments != null)
            {
                QueueAssignments.AddRange(queueAssignments);
            }

            MaterialLeases.Clear();
            if (materialLeases != null)
            {
                MaterialLeases.AddRange(materialLeases);
            }

            OutputLeases.Clear();
            if (outputLeases != null)
            {
                OutputLeases.AddRange(outputLeases);
            }

            return changed;
        }

        public float GetReservedAmount(Tag tag)
        {
            return ReservedMaterials.TryGetValue(tag, out float amount) ? amount : 0f;
        }

        public bool SetProducedAmount(float amount)
        {
            amount = Mathf.Clamp(amount, 0f, RequestedAmount);
            if (amount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < ProducedAtSubmit ||
                Mathf.Abs(amount - ProducedAtSubmit) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return false;
            }

            ProducedAtSubmit = amount;
            return true;
        }

        internal void RebaseProductionThreshold(float stockThreshold)
        {
            StockAtSubmit = Mathf.Max(0f, stockThreshold);
            AllocationOffsetAtSubmit = 0f;
        }

        public bool ObserveActivity(float currentCycle, float producedAmount, float queueLoad, bool forceActive = false)
        {
            bool valuesChanged =
                Mathf.Abs(producedAmount - LastObservedProducedAmount) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                Mathf.Abs(queueLoad - LastObservedQueueLoad) > 0.001f;
            if (forceActive || valuesChanged)
            {
                LastActivityCycle = currentCycle;
                LastObservedProducedAmount = producedAmount;
                LastObservedQueueLoad = queueLoad;
            }

            return valuesChanged;
        }

        private static bool AreReservedMaterialsEqual(Dictionary<Tag, float> left, Dictionary<Tag, float> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            foreach (KeyValuePair<Tag, float> pair in left)
            {
                if (right == null || !right.TryGetValue(pair.Key, out float value) || Mathf.Abs(value - pair.Value) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreQueueAssignmentsEqual(List<ProductionOrderQueueAssignment> left, List<ProductionOrderQueueAssignment> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                ProductionOrderQueueAssignment assignment = left[i];
                if (!IsValidQueueAssignment(assignment) ||
                    HasEarlierMatchingQueue(left, i, assignment))
                {
                    continue;
                }

                if (SumMatchingQueue(left, assignment) !=
                    SumMatchingQueue(right, assignment))
                {
                    return false;
                }
            }

            for (int i = 0; i < right.Count; i++)
            {
                ProductionOrderQueueAssignment assignment = right[i];
                if (IsValidQueueAssignment(assignment) &&
                    SumMatchingQueue(left, assignment) !=
                    SumMatchingQueue(right, assignment))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreMaterialLeasesEqual(List<ProductionOrderMaterialLease> left, List<ProductionOrderMaterialLease> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            if (left == null)
            {
                return true;
            }

            for (int i = 0; i < left.Count; i++)
            {
                ProductionOrderMaterialLease lease = left[i];
                if (HasEarlierMatchingMaterialLease(left, i, lease))
                {
                    continue;
                }

                if (CountMatchingMaterialLeases(left, lease) !=
                    CountMatchingMaterialLeases(right, lease))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreOutputLeasesEqual(List<ProductionOrderOutputLease> left, List<ProductionOrderOutputLease> right)
        {
            if ((left?.Count ?? 0) != (right?.Count ?? 0))
            {
                return false;
            }

            if (left == null)
            {
                return true;
            }

            for (int i = 0; i < left.Count; i++)
            {
                ProductionOrderOutputLease lease = left[i];
                if (HasEarlierMatchingOutputLease(left, i, lease))
                {
                    continue;
                }

                if (CountMatchingOutputLeases(left, lease) !=
                    CountMatchingOutputLeases(right, lease))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidQueueAssignment(ProductionOrderQueueAssignment assignment)
        {
            return assignment?.Fabricator != null && assignment.Recipe != null;
        }

        private static bool IsSameQueue(
            ProductionOrderQueueAssignment left,
            ProductionOrderQueueAssignment right)
        {
            return IsValidQueueAssignment(left) &&
                   IsValidQueueAssignment(right) &&
                   ProductionOrderCenterCatalog.GetInstanceId(left.Fabricator) ==
                   ProductionOrderCenterCatalog.GetInstanceId(right.Fabricator) &&
                   ProductionRecipeCatalog.GetRecipeKey(left.Recipe) ==
                   ProductionRecipeCatalog.GetRecipeKey(right.Recipe);
        }

        private static bool HasEarlierMatchingQueue(
            List<ProductionOrderQueueAssignment> assignments,
            int index,
            ProductionOrderQueueAssignment assignment)
        {
            for (int i = 0; i < index; i++)
            {
                if (IsSameQueue(assignments[i], assignment))
                {
                    return true;
                }
            }

            return false;
        }

        private static int SumMatchingQueue(
            List<ProductionOrderQueueAssignment> assignments,
            ProductionOrderQueueAssignment assignment)
        {
            int count = 0;
            for (int i = 0; i < assignments.Count; i++)
            {
                if (IsSameQueue(assignments[i], assignment))
                {
                    count += assignments[i].OrderCount;
                }
            }

            return count;
        }

        private static bool HasEarlierMatchingMaterialLease(
            List<ProductionOrderMaterialLease> leases,
            int index,
            ProductionOrderMaterialLease lease)
        {
            for (int i = 0; i < index; i++)
            {
                if (IsSameMaterialLease(leases[i], lease))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountMatchingMaterialLeases(
            List<ProductionOrderMaterialLease> leases,
            ProductionOrderMaterialLease lease)
        {
            int count = 0;
            for (int i = 0; i < leases.Count; i++)
            {
                if (IsSameMaterialLease(leases[i], lease))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsSameMaterialLease(
            ProductionOrderMaterialLease left,
            ProductionOrderMaterialLease right)
        {
            return left != null && right != null &&
                   left.Material == right.Material &&
                   left.SourceStorageInstanceId == right.SourceStorageInstanceId &&
                   Mathf.RoundToInt(left.Amount * 1000f) ==
                   Mathf.RoundToInt(right.Amount * 1000f);
        }

        private static bool HasEarlierMatchingOutputLease(
            List<ProductionOrderOutputLease> leases,
            int index,
            ProductionOrderOutputLease lease)
        {
            for (int i = 0; i < index; i++)
            {
                if (IsSameOutputLease(leases[i], lease))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountMatchingOutputLeases(
            List<ProductionOrderOutputLease> leases,
            ProductionOrderOutputLease lease)
        {
            int count = 0;
            for (int i = 0; i < leases.Count; i++)
            {
                if (IsSameOutputLease(leases[i], lease))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsSameOutputLease(
            ProductionOrderOutputLease left,
            ProductionOrderOutputLease right)
        {
            return left != null && right != null &&
                   left.ProductTag == right.ProductTag &&
                   left.FabricatorInstanceId == right.FabricatorInstanceId &&
                   Mathf.RoundToInt(left.Amount * 1000f) ==
                   Mathf.RoundToInt(right.Amount * 1000f);
        }
    }
}
