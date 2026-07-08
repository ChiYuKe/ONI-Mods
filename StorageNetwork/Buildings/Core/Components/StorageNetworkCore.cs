using KSerialization;
using StorageNetwork.Core;
using System;
using UnityEngine;

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
        private const float LowBatteryWarningThreshold = 0.30f;
        private const float LowBatteryResetThreshold = 0.35f;

        [Serialize]
        private float internalBatteryJoules;

        [Serialize]
        private bool internalBatteryInitialized;

        private static StatusItem backupPowerStatusItem;
        private static StatusItem internalBatteryStatusItem;

        private bool lowBatteryWarningActive;
        private bool backupPowerNotificationActive;
        private bool observedExternalPower;
        private Guid backupPowerStatusHandle = Guid.Empty;
        private Guid internalBatteryStatusHandle = Guid.Empty;

        public float InternalBatteryJoulesAvailable => InternalBattery.JoulesAvailable;

        public float InternalBatteryAvailableCapacityJoules => InternalBattery.AvailableCapacityJoules;

        public bool HasInternalBatteryPower => InternalBattery.HasEnergy;

        public bool HasExternalPower => GetComponent<Operational>()?.IsOperational == true;

        public bool IsNetworkOnline => HasExternalPower || HasInternalBatteryPower;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            InitializeInternalBattery();
            AddInternalBatteryStatus();
            StorageSceneRegistry.Register(gameObject);
        }

        protected override void OnCleanUp()
        {
            RemoveBackupPowerStatus();
            RemoveInternalBatteryStatus();
            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        public void Update()
        {
            RefreshBackupPowerStatus();
        }

        public void Sim1000ms(float dt)
        {
            InitializeInternalBattery();
            if (HasExternalPower)
            {
                ResetLowBatteryWarningIfRecovered();
                RefreshBackupPowerStatus();
                return;
            }

            StorageNetworkCoreInternalBattery battery = InternalBattery;
            battery.Drain(dt);
            internalBatteryJoules = battery.JoulesAvailable;
            ShowLowBatteryWarningIfNeeded();
            RefreshBackupPowerStatus();
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

            StorageNetworkNotifications.ShowError(
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

        private void RefreshBackupPowerStatus()
        {
            bool hasExternalPower = HasExternalPower;
            bool isUsingBackupPower = !hasExternalPower && HasInternalBatteryPower;

            if (hasExternalPower)
            {
                observedExternalPower = true;
                backupPowerNotificationActive = false;
            }

            if (isUsingBackupPower)
            {
                RemoveNativeNoPowerStatus();
                AddBackupPowerStatus();
                ShowBackupPowerNotificationIfNeeded();
            }
            else
            {
                RemoveBackupPowerStatus();
            }
        }

        private bool IsUsingBackupPower => !HasExternalPower && HasInternalBatteryPower;

        private void ShowBackupPowerNotificationIfNeeded()
        {
            if (backupPowerNotificationActive || !observedExternalPower)
            {
                return;
            }

            StorageNetworkNotifications.ShowError(
                gameObject,
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CORE_BACKUP_POWER_NOTIFICATION));
            backupPowerNotificationActive = true;
        }

        private void AddBackupPowerStatus()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable == null || backupPowerStatusHandle != Guid.Empty)
            {
                return;
            }

            backupPowerStatusHandle = selectable.AddStatusItem(GetBackupPowerStatusItem(), this);
        }

        private void RemoveBackupPowerStatus()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null && backupPowerStatusHandle != Guid.Empty)
            {
                selectable.RemoveStatusItem(backupPowerStatusHandle);
            }

            backupPowerStatusHandle = Guid.Empty;
        }

        private void RemoveNativeNoPowerStatus()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable == null)
            {
                return;
            }

            selectable.RemoveStatusItem(Db.Get().BuildingStatusItems.NeedPower, false);
            selectable.RemoveStatusItem(Db.Get().BuildingStatusItems.NotEnoughPower, false);
        }

        private static StatusItem GetBackupPowerStatusItem()
        {
            if (backupPowerStatusItem != null)
            {
                return backupPowerStatusItem;
            }

            backupPowerStatusItem = new StatusItem(
                "StorageNetworkCoreBackupPower",
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CORE_BACKUP_POWER_STATUS),
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CORE_BACKUP_POWER_STATUS_TOOLTIP),
                "",
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.Power.ID,
                129022,
                true);
            backupPowerStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkCore core = data as StorageNetworkCore;
                return core != null
                    ? text.Replace("{Battery}", GameUtil.GetFormattedJoules(core.InternalBatteryJoulesAvailable, "F1", GameUtil.TimeSlice.None))
                    : text;
            };
            backupPowerStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkCore core = data as StorageNetworkCore;
                return core != null
                    ? tooltip.Replace("{Battery}", GameUtil.GetFormattedJoules(core.InternalBatteryJoulesAvailable, "F1", GameUtil.TimeSlice.None))
                    : tooltip;
            };
            return backupPowerStatusItem;
        }

        private void AddInternalBatteryStatus()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable == null || internalBatteryStatusHandle != Guid.Empty)
            {
                return;
            }

            internalBatteryStatusHandle = selectable.AddStatusItem(GetInternalBatteryStatusItem(), this);
        }

        private void RemoveInternalBatteryStatus()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null && internalBatteryStatusHandle != Guid.Empty)
            {
                selectable.RemoveStatusItem(internalBatteryStatusHandle);
            }

            internalBatteryStatusHandle = Guid.Empty;
        }

        private static StatusItem GetInternalBatteryStatusItem()
        {
            if (internalBatteryStatusItem != null)
            {
                return internalBatteryStatusItem;
            }

            internalBatteryStatusItem = new StatusItem(
                "StorageNetworkCoreInternalBattery",
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CORE_INTERNAL_BATTERY_STATUS),
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CORE_INTERNAL_BATTERY_STATUS_TOOLTIP),
                "",
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.Power.ID,
                129022,
                true);
            internalBatteryStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkCore core = data as StorageNetworkCore;
                return core != null ? core.ResolveInternalBatteryStatus(text) : text;
            };
            internalBatteryStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkCore core = data as StorageNetworkCore;
                return core != null ? core.ResolveInternalBatteryStatus(tooltip) : tooltip;
            };
            return internalBatteryStatusItem;
        }

        private string ResolveInternalBatteryStatus(string text)
        {
            float available = InternalBatteryJoulesAvailable;
            float capacity = InternalBatteryCapacityJoules;
            float percent = capacity > 0f ? available / capacity * 100f : 0f;
            return text
                .Replace("{Battery}", GameUtil.GetFormattedJoules(available, "F1", GameUtil.TimeSlice.None))
                .Replace("{Capacity}", GameUtil.GetFormattedJoules(capacity, "F1", GameUtil.TimeSlice.None))
                .Replace("{Percent}", GameUtil.GetFormattedPercent(percent));
        }
    }
}
