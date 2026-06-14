using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed class ProductionOrderQueueAssignment
    {
        public ProductionOrderQueueAssignment(
            ComplexFabricator fabricator,
            ComplexRecipe recipe,
            int orderCount,
            Tag outputTag = default,
            string outputName = null,
            string consumerName = null,
            bool primary = false)
        {
            Fabricator = fabricator;
            Recipe = recipe;
            OrderCount = orderCount;
            OutputTag = outputTag == default ? Tag.Invalid : outputTag;
            OutputName = outputName ?? string.Empty;
            ConsumerName = consumerName ?? string.Empty;
            Primary = primary;
        }

        public ComplexFabricator Fabricator { get; }

        public ComplexRecipe Recipe { get; }

        public int OrderCount { get; }

        public Tag OutputTag { get; }

        public string OutputName { get; }

        public string ConsumerName { get; }

        public bool Primary { get; }
    }

    internal sealed class ProductionOrderMaterialLease
    {
        public ProductionOrderMaterialLease(Tag material, float amount, int sourceStorageInstanceId, string consumerName = null)
        {
            Material = material;
            Amount = Mathf.Max(0f, amount);
            SourceStorageInstanceId = sourceStorageInstanceId;
            ConsumerName = consumerName ?? string.Empty;
        }

        public Tag Material { get; }

        public float Amount { get; }

        public int SourceStorageInstanceId { get; }

        public string ConsumerName { get; }
    }

    internal sealed class ProductionOrderOutputLease
    {
        public ProductionOrderOutputLease(Tag productTag, float amount, int fabricatorInstanceId, string producerName = null)
        {
            ProductTag = productTag;
            Amount = Mathf.Max(0f, amount);
            FabricatorInstanceId = fabricatorInstanceId;
            ProducerName = producerName ?? string.Empty;
        }

        public Tag ProductTag { get; }

        public float Amount { get; }

        public int FabricatorInstanceId { get; }

        public string ProducerName { get; }
    }
}
