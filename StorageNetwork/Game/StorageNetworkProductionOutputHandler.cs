using System.Collections.Generic;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Gameplay
{
    internal static class StorageNetworkProductionOutputHandler
    {
        public static void ForceStoreProducedOutputs(ComplexFabricator fabricator, List<GameObject> products)
        {
            StorageNetworkMaterialRequester requester = fabricator != null
                ? fabricator.GetComponent<StorageNetworkMaterialRequester>()
                : null;
            requester?.ForceStoreProducedOutputs(products);
        }
    }
}
