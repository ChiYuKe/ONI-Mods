using UnityEngine;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkGasOutputPortEgressConduit
    {
        public static void Configure(GameObject go, Storage storage)
        {
            if (go == null || storage == null)
            {
                return;
            }

            ConduitDispenser dispenser = go.AddOrGet<ConduitDispenser>();
            dispenser.storage = storage;
            dispenser.conduitType = ConduitType.Gas;
            dispenser.elementFilter = null;
            dispenser.alwaysDispense = true;
        }
    }
}
