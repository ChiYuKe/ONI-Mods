using System;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkCoreInternalBattery(float capacityJoules, float drainWatts, float joulesAvailable)
    {
        private readonly float capacityJoules = Math.Max(0f, capacityJoules);
        private readonly float drainWatts = Math.Max(0f, drainWatts);
        private float joulesAvailable = Clamp(joulesAvailable, 0f, Math.Max(0f, capacityJoules));

        public StorageNetworkCoreInternalBattery(float capacityJoules, float drainWatts)
            : this(capacityJoules, drainWatts, capacityJoules)
        {
        }

        public float CapacityJoules => capacityJoules;

        public float JoulesAvailable => Clamp(joulesAvailable, 0f, capacityJoules);

        public float AvailableCapacityJoules => Math.Max(0f, CapacityJoules - JoulesAvailable);

        public bool HasEnergy => JoulesAvailable > 0.01f;

        public float Drain(float dt)
        {
            if (dt <= 0f || drainWatts <= 0f)
            {
                return 0f;
            }

            float drained = Math.Min(JoulesAvailable, drainWatts * dt);
            joulesAvailable = Math.Max(0f, JoulesAvailable - drained);
            return drained;
        }

        public float Recharge(float joules)
        {
            if (joules <= 0f)
            {
                return 0f;
            }

            float accepted = Math.Min(joules, AvailableCapacityJoules);
            joulesAvailable = Math.Min(CapacityJoules, JoulesAvailable + accepted);
            return accepted;
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
