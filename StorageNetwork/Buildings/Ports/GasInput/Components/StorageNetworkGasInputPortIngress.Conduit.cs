using UnityEngine;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkGasInputPortIngressConduit
    {
        public static void Configure(GameObject go, Storage storage, float capacityKg)
        {
            if (go == null || storage == null)
            {
                return;
            }

            ConduitConsumer consumer = go.AddOrGet<ConduitConsumer>();
            consumer.storage = storage;
            consumer.capacityKG = capacityKg;
            consumer.conduitType = ConduitType.Gas;
            consumer.capacityTag = GameTags.Gas;
            consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            consumer.forceAlwaysSatisfied = true;
        }
    }
}
