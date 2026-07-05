namespace StorageNetwork.Components
{
    internal static class StorageNetworkEnergySensorLogic
    {
        public static float GetPercent(float storedJoules, float capacityJoules)
        {
            if (capacityJoules <= 0f)
            {
                return 0f;
            }

            float percent = storedJoules / capacityJoules * 100f;
            if (percent < 0f)
            {
                return 0f;
            }

            return percent > 100f ? 100f : percent;
        }

        public static bool ShouldRequestPower(
            bool previousSignal,
            bool networkOnline,
            float storedJoules,
            float capacityJoules,
            float lowThreshold,
            float highThreshold)
        {
            if (!networkOnline || capacityJoules <= 0f)
            {
                return false;
            }

            float percent = GetPercent(storedJoules, capacityJoules);
            if (previousSignal)
            {
                return percent < highThreshold;
            }

            return percent <= lowThreshold;
        }
    }
}
