namespace StorageNetwork.Services
{
    internal static class StorageNetworkFetchTargetResolver
    {
        public static bool PreparePickupable(Pickupable pickupable, ChoreConsumer consumer)
        {
            return TryPreparePickupable(pickupable, consumer, out _);
        }

        public static bool TryPreparePickupable(Pickupable pickupable, ChoreConsumer consumer, out int cost)
        {
            cost = int.MaxValue;
            return false;
        }
    }
}
