using UnityEngine;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkLiquidOutputPortEgressConduit
    {
        public static void Configure(GameObject go, Storage storage)
        {
            if (go == null || storage == null)
            {
                return;
            }

            ConduitDispenser dispenser = go.AddOrGet<ConduitDispenser>();
            dispenser.storage = storage;
            dispenser.conduitType = ConduitType.Liquid;
            dispenser.elementFilter = null;
            dispenser.alwaysDispense = true;
        }
    }
}
