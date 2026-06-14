using UnityEngine;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkCycleTime
    {
        public static float GetCurrent()
        {
            return GameClock.Instance != null ? GameClock.Instance.GetTimeInCycles() : Time.time / 600f;
        }
    }
}
