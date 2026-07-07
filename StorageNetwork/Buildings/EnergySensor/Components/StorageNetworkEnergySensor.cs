using System;
using KSerialization;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkEnergySensor : KMonoBehaviour, ISim200ms, IActivationRangeTarget
    {
        public static readonly HashedString PORT_ID = "StorageNetworkEnergySensorLogicPort";

        private const int DefaultLowThreshold = 20;
        private const int DefaultHighThreshold = 80;

        [Serialize]
        private int lowThresholdValue = DefaultLowThreshold;

        [Serialize]
        private int highThresholdValue = DefaultHighThreshold;

        [Serialize]
        private bool requestPower;

        [MyCmpGet]
        private LogicPorts logicPorts = null;

        private static StatusItem chargeStatusItem;
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkEnergySensor> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkEnergySensor>((component, data) => component.OnCopySettings(data));

        private Guid chargeStatusHandle = Guid.Empty;
        private int worldId = -1;
        private StorageNetworkPowerSnapshot snapshot = StorageNetworkPowerSnapshot.Offline;

        public float ActivateValue
        {
            get => highThresholdValue;
            set
            {
                int oldHighThresholdValue = highThresholdValue;
                highThresholdValue = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
                if (lowThresholdValue > highThresholdValue)
                {
                    lowThresholdValue = highThresholdValue;
                }

                RefreshSignal(highThresholdValue > oldHighThresholdValue);
            }
        }

        public float DeactivateValue
        {
            get => lowThresholdValue;
            set
            {
                lowThresholdValue = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
                if (highThresholdValue < lowThresholdValue)
                {
                    highThresholdValue = lowThresholdValue;
                }

                RefreshSignal();
            }
        }

        public float MinValue => 0f;

        public float MaxValue => 100f;

        public bool UseWholeNumbers => true;

        public string ActivationRangeTitleText => Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_SIDE_SCREEN_TITLE);

        public string ActivateSliderLabelText => Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_HIGH_THRESHOLD);

        public string DeactivateSliderLabelText => Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_LOW_THRESHOLD);

        public string ActivateTooltip => Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_HIGH_THRESHOLD_TOOLTIP);

        public string DeactivateTooltip => Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_LOW_THRESHOLD_TOOLTIP);

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            AddStatusItem();
            RefreshSignal();
        }

        protected override void OnCleanUp()
        {
            RemoveStatusItem();
            base.OnCleanUp();
        }

        public void Sim200ms(float dt)
        {
            RefreshSignal();
        }

        private void RefreshSignal(bool highThresholdIncreased = false)
        {
            snapshot = StorageNetworkPowerService.GetSnapshot(GetWorldId());
            requestPower = highThresholdIncreased
                ? StorageNetworkEnergySensorLogic.ShouldRequestPowerAfterHighThresholdIncrease(
                    requestPower,
                    snapshot.NetworkOnline,
                    snapshot.StoredJoules,
                    snapshot.CapacityJoules,
                    lowThresholdValue,
                    highThresholdValue)
                : StorageNetworkEnergySensorLogic.ShouldRequestPower(
                    requestPower,
                    snapshot.NetworkOnline,
                    snapshot.StoredJoules,
                    snapshot.CapacityJoules,
                    lowThresholdValue,
                    highThresholdValue);
            logicPorts?.SendSignal(PORT_ID, requestPower ? 1 : 0);
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            StorageNetworkEnergySensor source = sourceObject != null
                ? sourceObject.GetComponent<StorageNetworkEnergySensor>()
                : null;
            if (source == null || source == this)
            {
                return;
            }

            lowThresholdValue = Mathf.Clamp(source.lowThresholdValue, 0, 100);
            highThresholdValue = Mathf.Clamp(source.highThresholdValue, lowThresholdValue, 100);
            RefreshSignal();
        }

        private void AddStatusItem()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                chargeStatusHandle = selectable.AddStatusItem(GetChargeStatusItem(), this);
            }
        }

        private void RemoveStatusItem()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null && chargeStatusHandle != Guid.Empty)
            {
                selectable.RemoveStatusItem(chargeStatusHandle);
            }

            chargeStatusHandle = Guid.Empty;
        }

        private static StatusItem GetChargeStatusItem()
        {
            if (chargeStatusItem != null)
            {
                return chargeStatusItem;
            }

            chargeStatusItem = new StatusItem(
                "StorageNetworkEnergySensorCharge",
                Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_STATUS_TOOLTIP),
                "",
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.Logic.ID,
                129022,
                true);
            chargeStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkEnergySensor sensor = data as StorageNetworkEnergySensor;
                return sensor != null ? sensor.ResolveStatusText(text) : text;
            };

            return chargeStatusItem;
        }

        private string ResolveStatusText(string text)
        {
            float percent = StorageNetworkEnergySensorLogic.GetPercent(snapshot.StoredJoules, snapshot.CapacityJoules);
            return text
                .Replace("{StoredJoules}", GameUtil.GetFormattedJoules(snapshot.StoredJoules, "F2", GameUtil.TimeSlice.None))
                .Replace("{CapacityJoules}", GameUtil.GetFormattedJoules(snapshot.CapacityJoules, "F2", GameUtil.TimeSlice.None))
                .Replace("{Percent}", GameUtil.GetFormattedPercent(percent))
                .Replace("{Signal}", GetSignalStatusText());
        }

        private string GetSignalStatusText()
        {
            if (!snapshot.NetworkOnline)
            {
                return ColorizeSignal(Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_NETWORK_OFFLINE), false);
            }

            if (snapshot.CapacityJoules <= 0f)
            {
                return ColorizeSignal(Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_NO_CAPACITY), false);
            }

            return requestPower
                ? ColorizeSignal(Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_SIGNAL_GREEN), true)
                : ColorizeSignal(Loc.Get(Loc.UI.STORAGE_NETWORK.ENERGY_SENSOR_SIGNAL_RED), false);
        }

        private static string ColorizeSignal(string text, bool green)
        {
            return string.Format("<color={0}>{1}</color>", green ? "#55d17a" : "#d86a6a", text);
        }

        private int GetWorldId()
        {
            if (worldId >= 0)
            {
                return worldId;
            }

            worldId = gameObject.GetMyWorldId();
            if (worldId == byte.MaxValue || worldId < 0)
            {
                int cell = Grid.PosToCell(gameObject);
                worldId = Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
            }

            return worldId;
        }
    }
}
