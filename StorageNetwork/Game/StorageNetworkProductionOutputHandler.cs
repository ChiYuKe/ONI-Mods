using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using UnityEngine;

namespace StorageNetwork.Gameplay
{
    internal static class StorageNetworkProductionOutputHandler
    {
        public static void ForceStoreProducedOutputs(ComplexFabricator fabricator, List<GameObject> products)
        {
            if (!StorageNetworkRuntimeCatalog.TryGetMaterialRequester(
                    fabricator,
                    out StorageNetworkMaterialRequester requester))
            {
                return;
            }

            requester.ForceStoreProducedOutputs(products);
            ProductionOrderService.NotifyFabricatorOutputChanged(fabricator);
        }
    }
}
