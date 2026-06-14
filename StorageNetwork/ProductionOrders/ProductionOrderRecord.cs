using System.Collections.Generic;
using System.Linq;
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
            IsAutomatic = IsAutomatic && isAutomatic;
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

        public void ObserveActivity(float currentCycle, float producedAmount, float queueLoad, bool forceActive = false)
        {
            if (forceActive ||
                Mathf.Abs(producedAmount - LastObservedProducedAmount) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                Mathf.Abs(queueLoad - LastObservedQueueLoad) > 0.001f)
            {
                LastActivityCycle = currentCycle;
                LastObservedProducedAmount = producedAmount;
                LastObservedQueueLoad = queueLoad;
            }
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

            Dictionary<string, int> leftCounts = BuildQueueAssignmentCounts(left);
            Dictionary<string, int> rightCounts = BuildQueueAssignmentCounts(right);
            if (leftCounts.Count != rightCounts.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, int> pair in leftCounts)
            {
                if (!rightCounts.TryGetValue(pair.Key, out int value) || value != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, int> BuildQueueAssignmentCounts(List<ProductionOrderQueueAssignment> assignments)
        {
            return (assignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment?.Fabricator != null && assignment.Recipe != null)
                .GroupBy(assignment => string.Format("{0}|{1}", assignment.Fabricator.GetInstanceID(), ProductionRecipeCatalog.GetRecipeKey(assignment.Recipe)))
                .ToDictionary(group => group.Key, group => group.Sum(assignment => assignment.OrderCount));
        }

        private static bool AreMaterialLeasesEqual(List<ProductionOrderMaterialLease> left, List<ProductionOrderMaterialLease> right)
        {
            return AreLeaseKeysEqual(
                (left ?? new List<ProductionOrderMaterialLease>())
                    .Select(lease => string.Format("{0}|{1}|{2:0.###}", lease.Material.Name, lease.SourceStorageInstanceId, lease.Amount)),
                (right ?? new List<ProductionOrderMaterialLease>())
                    .Select(lease => string.Format("{0}|{1}|{2:0.###}", lease.Material.Name, lease.SourceStorageInstanceId, lease.Amount)));
        }

        private static bool AreOutputLeasesEqual(List<ProductionOrderOutputLease> left, List<ProductionOrderOutputLease> right)
        {
            return AreLeaseKeysEqual(
                (left ?? new List<ProductionOrderOutputLease>())
                    .Select(lease => string.Format("{0}|{1}|{2:0.###}", lease.ProductTag.Name, lease.FabricatorInstanceId, lease.Amount)),
                (right ?? new List<ProductionOrderOutputLease>())
                    .Select(lease => string.Format("{0}|{1}|{2:0.###}", lease.ProductTag.Name, lease.FabricatorInstanceId, lease.Amount)));
        }

        private static bool AreLeaseKeysEqual(IEnumerable<string> left, IEnumerable<string> right)
        {
            List<string> leftList = left.OrderBy(value => value).ToList();
            List<string> rightList = right.OrderBy(value => value).ToList();
            return leftList.Count == rightList.Count && !leftList.Where((value, index) => value != rightList[index]).Any();
        }
    }
}
