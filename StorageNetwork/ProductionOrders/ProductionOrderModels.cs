using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal enum ProductionOrderState
    {
        Submitted,
        WaitingMaterials,
        Producing,
        Completed,
        Abnormal,
        Cancelled
    }

    internal enum ProductionOrderRiskLevel
    {
        Ready,
        Warning,
        Blocked
    }

    internal enum ProductionOrderDuplicatePolicy
    {
        CreateNew,
        MergeIntoExisting
    }

    internal struct RecipeDisplayInfo
    {
        public RecipeDisplayInfo(string name, string fabricatorName, string details, ComplexRecipe recipe, List<ComplexFabricator> fabricators, Sprite icon, string productKey, string productName, Tag productTag)
        {
            Name = name;
            FabricatorName = fabricatorName;
            Details = details;
            Recipe = recipe;
            Fabricators = fabricators ?? new List<ComplexFabricator>();
            Icon = icon;
            ProductKey = productKey;
            ProductName = productName;
            ProductTag = productTag;
        }

        public string Name { get; }

        public string FabricatorName { get; }

        public string Details { get; }

        public ComplexRecipe Recipe { get; }

        public List<ComplexFabricator> Fabricators { get; }

        public Sprite Icon { get; }

        public string ProductKey { get; }

        public string ProductName { get; }

        public Tag ProductTag { get; }
    }

    internal sealed class ProductDisplayGroup
    {
        public ProductDisplayGroup(string productKey, List<RecipeDisplayInfo> routes)
        {
            ProductKey = productKey;
            Routes = routes ?? new List<RecipeDisplayInfo>();
        }

        public string ProductKey { get; }

        public List<RecipeDisplayInfo> Routes { get; }

        public string ProductName => Routes.Count > 0 ? Routes[0].ProductName : ProductKey;

        public Tag ProductTag => Routes.Count > 0 ? Routes[0].ProductTag : Tag.Invalid;

        public Sprite Icon => Routes.Count > 0 ? Routes[0].Icon : null;
    }

    internal sealed class ProductionPlanNode
    {
        public ProductionPlanNode(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float outputAmount, int orderCount)
        {
            Recipe = recipe;
            Fabricators = fabricators?.Where(fabricator => fabricator != null).ToList() ?? new List<ComplexFabricator>();
            ProductTag = productTag;
            OutputAmount = outputAmount;
            OrderCount = orderCount;
            Assignments = BuildAssignments(Recipe, Fabricators, outputAmount, orderCount);
        }

        public ComplexRecipe Recipe { get; }

        public List<ComplexFabricator> Fabricators { get; }

        public Tag ProductTag { get; }

        public float OutputAmount { get; }

        public int OrderCount { get; }

        public List<ProductionPlanAssignment> Assignments { get; }

        public string FabricatorName => ProductionOrderFormatting.FormatFabricatorGroupName(Fabricators);

        public List<ProductionPlanRequirement> Requirements { get; } = new List<ProductionPlanRequirement>();

        private static List<ProductionPlanAssignment> BuildAssignments(ComplexRecipe recipe, List<ComplexFabricator> fabricators, float outputAmount, int orderCount)
        {
            List<ProductionPlanAssignment> assignments = new List<ProductionPlanAssignment>();
            if (fabricators == null || fabricators.Count == 0 || orderCount <= 0)
            {
                return assignments;
            }

            List<ComplexFabricator> orderedFabricators = fabricators
                .OrderBy(fabricator => GetFiniteQueueCount(fabricator, recipe))
                .ThenBy(fabricator => fabricator.gameObject.GetProperName())
                .ToList();
            int baseCount = orderCount / orderedFabricators.Count;
            int remainder = orderCount % orderedFabricators.Count;
            for (int i = 0; i < orderedFabricators.Count; i++)
            {
                int count = baseCount + (i < remainder ? 1 : 0);
                if (count > 0)
                {
                    assignments.Add(new ProductionPlanAssignment(orderedFabricators[i], count, outputAmount * count));
                }
            }

            return assignments;
        }

        private static int GetFiniteQueueCount(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            int queued = fabricator != null && recipe != null ? fabricator.GetRecipeQueueCount(recipe) : 0;
            return queued == ComplexFabricator.QUEUE_INFINITE ? int.MaxValue : Mathf.Max(0, queued);
        }
    }

    internal sealed class ProductionPlanAssignment
    {
        public ProductionPlanAssignment(ComplexFabricator fabricator, int orderCount, float outputAmount)
        {
            Fabricator = fabricator;
            OrderCount = orderCount;
            OutputAmount = outputAmount;
        }

        public ComplexFabricator Fabricator { get; }

        public int OrderCount { get; }

        public float OutputAmount { get; }
    }

    internal sealed class ProductionPlanRequirement
    {
        public ProductionPlanRequirement(Tag material, float requiredAmount, float availableAmount)
        {
            Material = material;
            RequiredAmount = requiredAmount;
            AvailableAmount = availableAmount;
        }

        public Tag Material { get; }

        public float RequiredAmount { get; }

        public float AvailableAmount { get; }

        public ProductionPlanNode Child { get; set; }
    }

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

        public float CreatedCycle { get; }

        public float CompletedCycle { get; set; }

        public float LastActivityCycle { get; private set; }

        public float LastObservedProducedAmount { get; private set; }

        public float LastObservedQueueLoad { get; private set; }

        public string AbnormalReason { get; set; }

        public int MergeCount { get; private set; }

        public bool IsAutomatic { get; private set; }

        public ProductionOrderState State { get; set; }

        public void Merge(float requestedAmount, int orderCount, Dictionary<Tag, float> reservedMaterials, List<ProductionOrderQueueAssignment> queueAssignments, float currentCycle, bool isAutomatic = false)
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
        }

        public float GetReservedAmount(Tag tag)
        {
            return ReservedMaterials.TryGetValue(tag, out float amount) ? amount : 0f;
        }

        public void ObserveActivity(float currentCycle, float producedAmount, float queueLoad)
        {
            if (Mathf.Abs(producedAmount - LastObservedProducedAmount) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                Mathf.Abs(queueLoad - LastObservedQueueLoad) > 0.001f)
            {
                LastActivityCycle = currentCycle;
                LastObservedProducedAmount = producedAmount;
                LastObservedQueueLoad = queueLoad;
            }
        }
    }

    internal sealed class ProductionOrderQueueAssignment
    {
        public ProductionOrderQueueAssignment(ComplexFabricator fabricator, ComplexRecipe recipe, int orderCount)
        {
            Fabricator = fabricator;
            Recipe = recipe;
            OrderCount = orderCount;
        }

        public ComplexFabricator Fabricator { get; }

        public ComplexRecipe Recipe { get; }

        public int OrderCount { get; }
    }

    internal sealed class ProductionKeepRule
    {
        public ProductionKeepRule(Tag productTag, string productName, string recipeKey, float targetAmount)
        {
            ProductTag = productTag;
            ProductName = productName;
            RecipeKey = recipeKey;
            TargetAmount = targetAmount;
        }

        public Tag ProductTag { get; }

        public string ProductName { get; }

        public string RecipeKey { get; }

        public float TargetAmount { get; }
    }

    internal sealed class ProductionOrderDraft
    {
        public ProductDisplayGroup Product { get; set; }

        public RecipeDisplayInfo Route { get; set; }

        public float RequestedAmount { get; set; }

        public float NetworkAvailableAmount { get; set; }

        public float NetworkRawAmount { get; set; }

        public float ReservedOutputAmount { get; set; }

        public ProductionPlanNode Plan { get; set; }

        public ProductionOrderRecord DuplicateOrder { get; set; }

        public ProductionOrderDuplicatePolicy DuplicatePolicy { get; set; }

        public ProductionOrderRiskLevel RiskLevel { get; set; }

        public List<string> ValidationMessages { get; } = new List<string>();

        public bool CanSubmit => Plan != null && Plan.Assignments.Count > 0 && RiskLevel != ProductionOrderRiskLevel.Blocked;

        public int TotalRequirementCount => Plan?.Requirements.Count ?? 0;

        public int BlockedRequirementCount => Plan?.Requirements.Count(requirement =>
            requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
            requirement.Child == null) ?? 0;

        public int ProducedRequirementCount => Plan?.Requirements.Count(requirement =>
            requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
            requirement.Child != null) ?? 0;
    }
}
