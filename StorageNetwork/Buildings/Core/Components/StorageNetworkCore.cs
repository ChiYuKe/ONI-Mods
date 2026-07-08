using KSerialization;
using StorageNetwork.Core;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 储存网络核心。每个星球只能建造一个；该星球的核心在线时，本地储存网络才可用。
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkCore : KMonoBehaviour, ISim1000ms
    {
        public const float PowerWatts = 240f;
        public const float InternalBatterySeconds = 600f;
        public const float InternalBatteryCapacityJoules = PowerWatts * InternalBatterySeconds;
        private const float LowBatteryWarningThreshold = 0.25f;
        private const float LowBatteryResetThreshold = 0.35f;

        [Serialize]
        private float internalBatteryJoules;

        [Serialize]
        private bool internalBatteryInitialized;

        private bool lowBatteryWarningActive;

        public float InternalBatteryJoulesAvailable => InternalBattery.JoulesAvailable;

        public float InternalBatteryAvailableCapacityJoules => InternalBattery.AvailableCapacityJoules;

        public bool HasInternalBatteryPower => InternalBattery.HasEnergy;

        public bool HasExternalPower => GetComponent<Operational>()?.IsOperational == true;

        public bool IsNetworkOnline => HasExternalPower || HasInternalBatteryPower;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            InitializeInternalBattery();
            StorageSceneRegistry.Register(gameObject);
        }

        protected override void OnCleanUp()
        {
            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            InitializeInternalBattery();
            if (HasExternalPower)
            {
                ResetLowBatteryWarningIfRecovered();
                return;
            }

            StorageNetworkCoreInternalBattery battery = InternalBattery;
            battery.Drain(dt);
            internalBatteryJoules = battery.JoulesAvailable;
            ShowLowBatteryWarningIfNeeded();
        }

        public float AddInternalBatteryEnergy(float joules)
        {
            InitializeInternalBattery();
            StorageNetworkCoreInternalBattery battery = InternalBattery;
            float accepted = battery.Recharge(joules);
            internalBatteryJoules = battery.JoulesAvailable;
            ResetLowBatteryWarningIfRecovered();
            return accepted;
        }

        private StorageNetworkCoreInternalBattery InternalBattery =>
            new StorageNetworkCoreInternalBattery(InternalBatteryCapacityJoules, PowerWatts, internalBatteryJoules);

        private void InitializeInternalBattery()
        {
            if (!internalBatteryInitialized)
            {
                internalBatteryJoules = InternalBatteryCapacityJoules;
                internalBatteryInitialized = true;
                return;
            }

            internalBatteryJoules = InternalBattery.JoulesAvailable;
        }

        private void ShowLowBatteryWarningIfNeeded()
        {
            if (lowBatteryWarningActive || InternalBatteryPercent > LowBatteryWarningThreshold)
            {
                return;
            }

            StorageNetworkNotifications.ShowWarning(
                gameObject,
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CORE_INTERNAL_BATTERY_LOW_NOTIFICATION));
            lowBatteryWarningActive = true;
        }

        private void ResetLowBatteryWarningIfRecovered()
        {
            if (InternalBatteryPercent >= LowBatteryResetThreshold)
            {
                lowBatteryWarningActive = false;
            }
        }

        private float InternalBatteryPercent =>
            InternalBatteryCapacityJoules > 0f ? InternalBatteryJoulesAvailable / InternalBatteryCapacityJoules : 0f;
    }
}
