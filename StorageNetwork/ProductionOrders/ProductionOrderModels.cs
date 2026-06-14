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
        public RecipeDisplayInfo(string name, string fabricatorName, string details, ComplexRecipe recipe, List<ComplexFabricator> fabricators, Sprite icon, string productKey, string productName, Tag productTag, List<int> worldIds)
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
            WorldIds = worldIds ?? new List<int>();
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

        public List<int> WorldIds { get; }
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
            Assignments = new List<ProductionPlanAssignment>();
        }

        public ComplexRecipe Recipe { get; }

        public List<ComplexFabricator> Fabricators { get; }

        public Tag ProductTag { get; }

        public float OutputAmount { get; }

        public int OrderCount { get; }

        public List<ProductionPlanAssignment> Assignments { get; }

        public string FabricatorName => ProductionOrderFormatting.FormatFabricatorGroupName(Fabricators);

        public List<ProductionPlanRequirement> Requirements { get; } = new List<ProductionPlanRequirement>();

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

}
