using System.Collections.Generic;
using StorageNetwork.Components;

namespace StorageNetwork.ProductionOrders
{
    internal sealed class ProductionOrderSubmitResult
    {
        private ProductionOrderSubmitResult(bool success, bool merged, ProductionOrderRecord order, ProductionPlanNode plan, string message)
        {
            Success = success;
            Merged = merged;
            Order = order;
            Plan = plan;
            Message = message;
        }

        public bool Success { get; }

        public bool Merged { get; }

        public ProductionOrderRecord Order { get; }

        public ProductionPlanNode Plan { get; }

        public string Message { get; }

        public static ProductionOrderSubmitResult Fail(string message)
        {
            return new ProductionOrderSubmitResult(false, false, null, null, message);
        }

        public static ProductionOrderSubmitResult Created(ProductionOrderRecord order, ProductionPlanNode plan, string message)
        {
            return new ProductionOrderSubmitResult(true, false, order, plan, message);
        }

        public static ProductionOrderSubmitResult MergeSuccess(ProductionOrderRecord order, ProductionPlanNode plan, string message)
        {
            return new ProductionOrderSubmitResult(true, true, order, plan, message);
        }
    }

    internal sealed class OrderAutomationLease
    {
        private readonly StorageNetworkMaterialRequester requester;
        private readonly bool requestEnabled;
        private readonly int mode;
        private readonly int sourceStorageInstanceId;
        private readonly bool limitEnabled;
        private readonly float limitKg;
        private readonly float requestedKg;
        private readonly bool outputStoreEnabled;
        private readonly int outputStoreModeValue;
        private readonly int outputStorageInstanceId;

        public OrderAutomationLease(StorageNetworkMaterialRequester requester)
        {
            this.requester = requester;
            requestEnabled = requester.RequestEnabled;
            mode = requester.Mode;
            sourceStorageInstanceId = requester.SourceStorageInstanceId;
            limitEnabled = requester.LimitEnabled;
            limitKg = requester.LimitKg;
            requestedKg = requester.RequestedKg;
            outputStoreEnabled = requester.OutputStoreEnabled;
            outputStoreModeValue = requester.OutputStoreModeValue;
            outputStorageInstanceId = requester.OutputStorageInstanceId;
        }

        public HashSet<string> OrderKeys { get; } = new HashSet<string>();

        public void Restore()
        {
            if (requester == null)
            {
                return;
            }

            requester.RequestEnabled = requestEnabled;
            requester.Mode = mode;
            requester.SourceStorageInstanceId = sourceStorageInstanceId;
            requester.LimitEnabled = limitEnabled;
            requester.LimitKg = limitKg;
            requester.RequestedKg = requestedKg;
            requester.OutputStoreEnabled = outputStoreEnabled;
            requester.OutputStoreModeValue = outputStoreModeValue;
            requester.OutputStorageInstanceId = outputStorageInstanceId;
        }
    }

    internal sealed class QueueCancellationTarget
    {
        public QueueCancellationTarget(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            Fabricator = fabricator;
            Recipe = recipe;
        }

        public ComplexFabricator Fabricator { get; }

        public ComplexRecipe Recipe { get; }

        public int OwnedCount { get; set; }
    }
}
