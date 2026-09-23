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
            ForceStoreProducedOutputs(fabricator, fabricator?.CurrentWorkingOrder, products);
        }

        public static void ForceStoreProducedOutputs(ComplexFabricator fabricator, ComplexRecipe recipe, List<GameObject> products)
        {
            if (fabricator == null)
            {
                return;
            }

            if (StorageNetworkRuntimeCatalog.TryGetMaterialRequester(
                    fabricator,
                    out StorageNetworkMaterialRequester requester))
            {
                requester.ForceStoreProducedOutputs(products);
            }

            if (recipe != null)
            {
                ProductionOrderService.NotifyProductFinished(fabricator, recipe, products);
            }
            else
            {
                ProductionOrderService.NotifyFabricatorOutputChanged(fabricator);
            }
        }
    }
}
