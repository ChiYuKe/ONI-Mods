using UnityEngine;

namespace StorageNetwork.Core
{
    /// <summary>
    /// Shared batching policy for material input ports. Inputs flush on a bounded
    /// interval or earlier when their local buffer starts to fill. Output ports
    /// deliberately preserve their configured per-second request cap.
    /// </summary>
    internal static class StorageNetworkPortTransferPolicy
    {
        public const float InputFlushIntervalSeconds = 2f;
        public const float InputHighWatermarkFraction = 0.25f;

        public static bool ShouldFlushInput(float storedKg, float capacityKg, float elapsedSeconds)
        {
            if (storedKg <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return false;
            }

            float highWatermarkKg = Mathf.Max(0f, capacityKg) * InputHighWatermarkFraction;
            return elapsedSeconds >= InputFlushIntervalSeconds ||
                   storedKg >= highWatermarkKg;
        }
    }
}
