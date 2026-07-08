using KSerialization;
using System;
using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkPowerInputPortConsumer : EnergyConsumer, ISingleSliderControl
    {
        public const float DefaultInputWatts = 1000f;
        public const float MinInputWatts = 0f;
        public const float MaxInputWatts = 10000f;
        private const float SimTickSeconds = 0.2f;
        private const float MinInputBatchJoules = 1f;
        private const float TransientStatusHoldSeconds = 0.8f;

        [Serialize]
        public float InputWatts = DefaultInputWatts;

        [Serialize]
        public int InputStoreModeValue;

        [Serialize]
        public int InputStorageInstanceId = KPrefabID.InvalidInstanceID;

        [Serialize]
        public float StoredJoules;

        private static StatusItem powerInputPortStatusItem;
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkPowerInputPortConsumer> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkPowerInputPortConsumer>((component, data) => component.OnCopySettings(data));

        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private Battery battery = null;

        private Guid powerInputPortStatusHandle = Guid.Empty;
        private int worldId = -1;
        private CircuitManager.ConnectionStatus lastConnectionStatus = CircuitManager.ConnectionStatus.NotConnected;
        private bool? lastOperationalActive;
        private string lastStatus;
        private float transientStatusRemaining;
        private string cachedStatusText;

        public float PortJoulesAvailable => battery != null
            ? Mathf.Clamp(battery.JoulesAvailable, 0f, PortCapacityJoules)
            : Mathf.Clamp(StoredJoules, 0f, PortCapacityJoules);

        public float PortCapacityJoules => Mathf.Max(0f, battery != null ? battery.Capacity : (storage != null ? storage.Capacity() : 0f));

        public float PortAvailableCapacityJoules => Mathf.Max(0f, PortCapacityJoules - PortJoulesAvailable);

        public StorageNetworkMaterialRequester.OutputStoreMode CurrentInputStoreMode
        {
            get => (StorageNetworkMaterialRequester.OutputStoreMode)Mathf.Clamp(InputStoreModeValue, 0, 1);
            set => InputStoreModeValue = (int)value;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            RefreshPowerInputPortStatus();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnCleanUp()
        {
            RemovePowerInputPortStatus();
            base.OnCleanUp();
        }

        public override void EnergySim200ms(float dt)
        {
            transientStatusRemaining = Mathf.Max(0f, transientStatusRemaining - Mathf.Max(0f, dt));
            RefreshPowerInputPortStatus();
            TransferStoredEnergyToNetwork();
            UpdateActiveState();
            base.EnergySim200ms(dt);
            PullExternalEnergyToNetwork();
            UpdateCachedStatusText();
        }

        public override void SetConnectionStatus(CircuitManager.ConnectionStatus connectionStatus)
        {
            lastConnectionStatus = connectionStatus;
            IsPowered = connectionStatus == CircuitManager.ConnectionStatus.Powered;
            if (connectionStatus != CircuitManager.ConnectionStatus.Powered || !IsPowered)
            {
                SetOperationalActive(false);
                SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_WAITING_EXTERNAL), true);
                return;
            }

            if (!HasExternalPowerSourceOnCircuit())
            {
                SetOperationalActive(false);
                SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_WAITING_EXTERNAL), true);
                return;
            }

            float portAvailableCapacity = PortAvailableCapacityJoules;
            if (portAvailableCapacity <= 0f)
            {
                SetOperationalActive(false);
                SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_BUFFER_FULL), true);
                return;
            }

            SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED), false);
        }

        private float GetInputWatts()
        {
            if (PortAvailableCapacityJoules <= 0f)
            {
                return 0f;
            }

            float requestedWatts = Mathf.Max(0f, InputWatts);
            float inputCapacity = Mathf.Max(0f, GetAvailableInputCapacityJoules() - PortJoulesAvailable);
            float requestedBatchJoules = requestedWatts * SimTickSeconds;
            if (inputCapacity <= 0f ||
                requestedBatchJoules <= 0f ||
                inputCapacity + MinInputBatchJoules < requestedBatchJoules)
            {
                return 0f;
            }

            return requestedWatts;
        }

        private void UpdateActiveState()
        {
            bool active = GetInputWatts() > 0f &&
                StorageNetworkPowerService.IsNetworkOnlineForWorld(GetWorldId()) &&
                GetAvailableInputCapacityJoules() > 0f;
            SetOperationalActive(active);
        }

        private void PullExternalEnergyToNetwork()
        {
            if (battery != null)
            {
                return;
            }

            if (!IsConnected ||
                CircuitID == ushort.MaxValue ||
                !IsPowered ||
                GetAvailableInputCapacityJoules() <= 0f)
            {
                return;
            }

            float requestedJoules = GetInputWatts() * SimTickSeconds;
            if (requestedJoules <= 0f)
            {
                return;
            }

            float pulled = PullExternalEnergy(requestedJoules);
            if (pulled <= 0f)
            {
                SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_WAITING_EXTERNAL), true);
                return;
            }

            StoredJoules = Mathf.Min(PortCapacityJoules, PortJoulesAvailable + pulled);
            SetTransientStatus(string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_CHARGED), FormatPowerRate(pulled / SimTickSeconds)));
        }

        private float PullExternalEnergy(float requestedJoules)
        {
            CircuitManager circuitManager = Game.Instance?.circuitManager;
            if (circuitManager == null)
            {
                return 0f;
            }

            float remaining = requestedJoules;
            System.Collections.Generic.List<Generator> generators = circuitManager.GetGeneratorsOnCircuit(CircuitID);
            if (generators != null)
            {
                foreach (Generator generator in generators)
                {
                    if (generator == null || generator is StorageNetworkPowerOutputPortGenerator)
                    {
                        continue;
                    }

                    float available = GetGeneratorAvailableThisTick(generator);
                    float taken = Mathf.Min(remaining, available);
                    if (taken > 0f)
                    {
                        if (generator.JoulesAvailable > 0f)
                        {
                            generator.ApplyDeltaJoules(-Mathf.Min(taken, generator.JoulesAvailable), false);
                        }

                        remaining -= taken;
                    }

                    if (remaining <= 0.01f)
                    {
                        return requestedJoules - remaining;
                    }
                }
            }

            remaining = PullExternalBatteryEnergy(circuitManager.GetBatteriesOnCircuit(CircuitID), remaining);
            if (remaining <= 0.01f)
            {
                return requestedJoules - remaining;
            }

            remaining = PullExternalBatteryEnergy(circuitManager.GetTransformersOnCircuit(CircuitID), remaining);
            return requestedJoules - remaining;
        }

        private static float GetGeneratorAvailableThisTick(Generator generator)
        {
            if (generator == null)
            {
                return 0f;
            }

            float available = Mathf.Max(0f, generator.JoulesAvailable);
            if (generator.IsProducingPower())
            {
                DevGenerator devGenerator = generator as DevGenerator;
                float watts = devGenerator != null ? devGenerator.wattageRating : generator.WattageRating;
                available = Mathf.Max(available, Mathf.Max(0f, watts) * SimTickSeconds);
            }

            return available;
        }

        private float PullExternalBatteryEnergy(System.Collections.Generic.List<Battery> batteries, float remaining)
        {
            if (batteries == null || remaining <= 0f)
            {
                return remaining;
            }

            foreach (Battery battery in batteries)
            {
                if (battery == null)
                {
                    continue;
                }

                float taken = Mathf.Min(remaining, battery.JoulesAvailable);
                if (taken > 0f)
                {
                    battery.ConsumeEnergy(taken);
                    remaining -= taken;
                }

                if (remaining <= 0.01f)
                {
                    break;
                }
            }

            return remaining;
        }

        private float GetAvailableInputCapacityJoules()
        {
            if (CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = ResolveInputStorage();
                StorageNetworkPowerStorage powerStorage = target != null ? target.GetComponent<StorageNetworkPowerStorage>() : null;
                return powerStorage != null ? powerStorage.AvailableCapacityJoules : 0f;
            }

            return StorageNetworkPowerService.GetAvailableChargeCapacityJoules(GetWorldId());
        }

        public float GetInputWattsSetting()
        {
            return Mathf.Clamp(InputWatts, MinInputWatts, GetMaxInputWatts());
        }

        public void SetInputWatts(float watts)
        {
            InputWatts = Mathf.Clamp(watts, MinInputWatts, GetMaxInputWatts());
        }

        public string SliderTitleKey => "STRINGS.UI.STORAGE_NETWORK.POWER_PORT_INPUT_RATE";

        public string SliderUnits => global::STRINGS.UI.UNITSUFFIXES.ELECTRICAL.WATT;

        public int SliderDecimalPlaces(int index)
        {
            return 0;
        }

        public float GetSliderMin(int index)
        {
            return MinInputWatts;
        }

        public float GetSliderMax(int index)
        {
            return GetMaxInputWatts();
        }

        public float GetSliderValue(int index)
        {
            return GetInputWattsSetting();
        }

        public void SetSliderValue(float value, int index)
        {
            SetInputWatts(value);
        }

        public string GetSliderTooltipKey(int index)
        {
            return "STRINGS.UI.STORAGE_NETWORK.POWER_INPUT_PORT_RATE_TOOLTIP";
        }

        public string GetSliderTooltip(int index)
        {
            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_INPUT_PORT_RATE_TOOLTIP),
                FormatPowerRate(GetInputWattsSetting()));
        }

        public void SetInputStorage(Storage target)
        {
            InputStorageInstanceId = GetStorageInstanceId(target);
            CurrentInputStoreMode = StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage;
        }

        public void UseAutomaticInputStorage()
        {
            CurrentInputStoreMode = StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork;
            InputStorageInstanceId = KPrefabID.InvalidInstanceID;
        }

        public Storage ResolveInputStorage()
        {
            if (InputStorageInstanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage target = info?.Storage;
                KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
                if (info?.Minion == null &&
                    prefabId != null &&
                    prefabId.InstanceID == InputStorageInstanceId &&
                    target.GetComponent<StorageNetworkPowerStorage>() != null &&
                    StorageNetworkStorageRules.IsConnectedNetworkStorage(target))
                {
                    return target;
                }
            }

            return null;
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            StorageNetworkPowerInputPortConsumer source = sourceObject != null
                ? sourceObject.GetComponent<StorageNetworkPowerInputPortConsumer>()
                : null;
            if (source == null || source == this)
            {
                return;
            }

            InputWatts = source.InputWatts;
            InputStoreModeValue = source.InputStoreModeValue;
            InputStorageInstanceId = source.InputStorageInstanceId;
            lastStatus = string.Empty;
            cachedStatusText = null;
        }

        private void TransferStoredEnergyToNetwork()
        {
            if (PortJoulesAvailable <= 0f ||
                !StorageNetworkPowerService.IsNetworkOnlineForWorld(GetWorldId()) ||
                GetAvailableInputCapacityJoules() <= 0f)
            {
                return;
            }

            float stored = CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage
                ? AddEnergyToSpecificStorage(PortJoulesAvailable)
                : StorageNetworkPowerService.AddEnergy(GetWorldId(), Mathf.Min(PortJoulesAvailable, StorageNetworkPowerService.GetAvailableChargeCapacityJoules(GetWorldId())));
            if (stored <= 0f)
            {
                SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_NO_CAPACITY), true);
                return;
            }

            StoredJoules = Mathf.Max(0f, PortJoulesAvailable - stored);
            if (battery != null)
            {
                battery.ConsumeEnergy(stored);
            }

            SetTransientStatus(string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_STORED), FormatPowerRate(stored / SimTickSeconds)));
        }

        private void SetOperationalActive(bool active)
        {
            if (lastOperationalActive.HasValue && lastOperationalActive.Value == active)
            {
                return;
            }

            lastOperationalActive = active;
            operational?.SetActive(active);
        }

        private void SetStableStatus(string status, bool force)
        {
            if (!force && transientStatusRemaining > 0f)
            {
                return;
            }

            SetStatus(status);
        }

        private void SetTransientStatus(string status)
        {
            transientStatusRemaining = TransientStatusHoldSeconds;
            SetStatus(status);
        }

        private void SetStatus(string status)
        {
            if (lastStatus == status)
            {
                return;
            }

            lastStatus = status;
        }

        private float AddEnergyToSpecificStorage(float joules)
        {
            Storage target = ResolveInputStorage();
            StorageNetworkPowerStorage powerStorage = target != null ? target.GetComponent<StorageNetworkPowerStorage>() : null;
            return powerStorage != null ? powerStorage.AddEnergy(joules) : 0f;
        }

        private void RefreshPowerInputPortStatus()
        {
            if (powerInputPortStatusHandle != Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                powerInputPortStatusHandle = selectable.AddStatusItem(GetPowerInputPortStatusItem(), this);
            }
        }

        private void RemovePowerInputPortStatus()
        {
            if (powerInputPortStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(powerInputPortStatusHandle);
            }

            powerInputPortStatusHandle = Guid.Empty;
        }

        private static StatusItem GetPowerInputPortStatusItem()
        {
            if (powerInputPortStatusItem != null)
            {
                return powerInputPortStatusItem;
            }

            powerInputPortStatusItem = new StatusItem(
                "StorageNetworkPowerInputPort",
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            powerInputPortStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkPowerInputPortConsumer input = data as StorageNetworkPowerInputPortConsumer;
                return input != null ? input.GetStatusText() : text;
            };
            powerInputPortStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkPowerInputPortConsumer input = data as StorageNetworkPowerInputPortConsumer;
                return input != null ? input.GetStatusText() : tooltip;
            };

            return powerInputPortStatusItem;
        }

        private string GetStatusText()
        {
            if (cachedStatusText == null)
            {
                UpdateCachedStatusText();
            }

            return cachedStatusText;
        }

        private void UpdateCachedStatusText()
        {
            cachedStatusText = BuildStatusText();
        }

        private string BuildStatusText()
        {
            return ColorizeInfo(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STATUS_ITEM),
                GetCurrentStatusText())) + "\n" + string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STATUS_TOOLTIP),
                ColorizeEnabled(GetInputWattsSetting() > 0f),
                ColorizeNetwork(StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId())),
                ColorizeInfo(GetInputStoreModeStatusText()),
                ColorizeAmount(FormatPowerRate(GetInputWattsSetting())),
                ColorizeAmount(GameUtil.GetFormattedJoules(PortJoulesAvailable, "F1", GameUtil.TimeSlice.None)),
                ColorizeAmount(GameUtil.GetFormattedJoules(PortCapacityJoules, "F1", GameUtil.TimeSlice.None)),
                ColorizeStatus(GetCurrentStatusText()));
        }

        private string GetInputStoreModeStatusText()
        {
            if (CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = ResolveInputStorage();
                return target != null
                    ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private string GetCurrentStatusText()
        {
            if (GetInputWattsSetting() <= 0f)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
            }

            if (!StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()))
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE);
            }

            return string.IsNullOrEmpty(lastStatus)
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED)
                : lastStatus;
        }

        private static string FormatPowerRate(float watts)
        {
            return GameUtil.GetFormattedWattage(watts, GameUtil.WattageFormatterUnit.Automatic, true);
        }

        public static float GetMaxInputWatts()
        {
            return Mathf.Max(MinInputWatts, Config.Instance.PowerInputMaxWatts);
        }

        private static string GetOnOffText(bool enabled)
        {
            return enabled ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED) : Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
        }

        private static string ColorizeEnabled(bool enabled)
        {
            return Colorize(GetOnOffText(enabled), enabled ? "#55d17a" : "#d86a6a");
        }

        private static string ColorizeNetwork(bool online)
        {
            return Colorize(online ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_ONLINE) : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE), online ? "#55d17a" : "#d86a6a");
        }

        private static string ColorizeInfo(string text)
        {
            return Colorize(text, "#8ec7ff");
        }

        private static string ColorizeAmount(string text)
        {
            return Colorize(text, "#f0c96a");
        }

        private static string ColorizeStatus(string text)
        {
            bool warning = text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_WAITING_EXTERNAL)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_NO_CAPACITY)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_BUFFER_FULL));
            return Colorize(text, warning ? "#d86a6a" : "#55d17a");
        }

        private static string Colorize(string text, string color)
        {
            return string.Format("<color={0}>{1}</color>", color, text);
        }

        private static int GetStorageInstanceId(Storage target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private bool HasExternalPowerSourceOnCircuit()
        {
            if (!IsConnected || CircuitID == ushort.MaxValue)
            {
                return false;
            }

            CircuitManager circuitManager = Game.Instance?.circuitManager;
            if (circuitManager == null)
            {
                return false;
            }

            System.Collections.Generic.List<Generator> generators = circuitManager.GetGeneratorsOnCircuit(CircuitID);
            if (generators != null)
            {
                foreach (Generator generator in generators)
                {
                    if (generator == null || generator is StorageNetworkPowerOutputPortGenerator)
                    {
                        continue;
                    }

                    if (generator.IsProducingPower() || generator.JoulesAvailable > 0f)
                    {
                        return true;
                    }
                }
            }

            System.Collections.Generic.List<Battery> batteries = circuitManager.GetBatteriesOnCircuit(CircuitID);
            if (HasChargedBattery(batteries))
            {
                return true;
            }

            System.Collections.Generic.List<Battery> transformers = circuitManager.GetTransformersOnCircuit(CircuitID);
            return HasChargedBattery(transformers);
        }

        private static bool HasChargedBattery(System.Collections.Generic.List<Battery> batteries)
        {
            if (batteries == null)
            {
                return false;
            }

            foreach (Battery battery in batteries)
            {
                if (battery != null && battery.JoulesAvailable > 0.01f)
                {
                    return true;
                }
            }

            return false;
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
