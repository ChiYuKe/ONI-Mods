using UnityEngine;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkLiquidInputPortIngressConduit
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
            consumer.conduitType = ConduitType.Liquid;
            consumer.capacityTag = GameTags.Liquid;
            consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            consumer.forceAlwaysSatisfied = true;
        }
    }
}
