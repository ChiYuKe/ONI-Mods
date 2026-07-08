using System;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkPowerReserveMetrics
    {
        public static float GetAvailableChargeCapacityJoules(float sharedAvailableCapacityJoules, float reserveAvailableCapacityJoules)
        {
            return MathfMax(sharedAvailableCapacityJoules) + MathfMax(reserveAvailableCapacityJoules);
        }

        public static float GetAutomationPercent(float sharedStoredJoules, float sharedCapacityJoules, float reserveStoredJoules, float reserveCapacityJoules)
        {
            float totalCapacity = MathfMax(sharedCapacityJoules) + MathfMax(reserveCapacityJoules);
            if (totalCapacity <= 0f)
            {
                return 0f;
            }

            float totalStored = MathfMax(sharedStoredJoules) + MathfMax(reserveStoredJoules);
            float percent = totalStored / totalCapacity * 100f;
            if (percent < 0f)
            {
                return 0f;
            }

            return percent > 100f ? 100f : percent;
        }

        private static float MathfMax(float value)
        {
            return value > 0f ? value : 0f;
        }
    }
}
