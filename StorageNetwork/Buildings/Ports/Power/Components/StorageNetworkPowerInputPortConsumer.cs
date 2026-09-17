using KSerialization;
using System;
using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkPowerInputPortConsumer : EnergyConsumer
    {
        public const float DefaultInputWatts = 1000f;
        private const float SimTickSeconds = 0.2f;
        private const float MinInputBatchJoules = 1f;
        private const float TransientStatusHoldSeconds = 0.8f;

        [Serialize]
        public bool InputStoreEnabled = true;

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
        private float lastTransferWatts;

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
            RequireInputs requireInputs = GetComponent<RequireInputs>();
            if (requireInputs != null)
            {
                Destroy(requireInputs);
            }

            ClearBogusStatusItems();

            if (InputWatts <= 0f)
            {
                InputStoreEnabled = false;
            }

            DetachNativeBatteryUi();
            RefreshPowerInputPortStatus();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnCleanUp()
        {
            RemovePowerInputPortStatus();
            base.OnCleanUp();
        }

        private void DetachNativeBatteryUi()
        {
            if (battery != null)
            {
                global::Components.Batteries.Remove(battery);
            }

            ClearBogusStatusItems();
        }

        private void ClearBogusStatusItems()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable == null)
            {
                return;
            }

            StatusItemGroup group = selectable.GetStatusItemGroup();
            if (group == null)
            {
                return;
            }

            Database.BuildingStatusItems buildingStatus = Db.Get()?.BuildingStatusItems;
            System.Collections.Generic.List<Guid> guidsToRemove = null;

            foreach (StatusItemGroup.Entry entry in group)
            {
                if (entry.item == null)
                {
                    continue;
                }

                bool shouldRemove =
                    (buildingStatus != null && (
                        entry.item == buildingStatus.NoWireConnected ||
                        entry.item == buildingStatus.NeedPower ||
                        entry.item == buildingStatus.BatteryJoulesAvailable
                    )) ||
                    entry.item.Id == "NoWireConnected" ||
                    entry.item.Id == "NeedPower" ||
                    entry.item.Id == "JoulesAvailable";

                if (shouldRemove)
                {
                    if (guidsToRemove == null)
                    {
                        guidsToRemove = new System.Collections.Generic.List<Guid>();
                    }

                    guidsToRemove.Add(entry.id);
                }
            }

            if (guidsToRemove != null)
            {
                for (int i = 0; i < guidsToRemove.Count; i++)
                {
                    selectable.RemoveStatusItem(guidsToRemove[i]);
                }
            }
        }

        public override void EnergySim200ms(float dt)
        {
            transientStatusRemaining = Mathf.Max(0f, transientStatusRemaining - Mathf.Max(0f, dt));
            DetachNativeBatteryUi();
            RefreshPowerInputPortStatus();
            TransferStoredEnergyToNetwork();
            UpdateActiveState();
            base.EnergySim200ms(dt);
            UpdateCachedStatusText();
        }

        public override void SetConnectionStatus(CircuitManager.ConnectionStatus connectionStatus)
        {
            lastConnectionStatus = connectionStatus;
            IsPowered = connectionStatus == CircuitManager.ConnectionStatus.Powered;
            if (!InputStoreEnabled)
            {
                SetOperationalActive(false);
                SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED), true);
                return;
            }

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

            if (GetAvailableInputCapacityJoules() <= 0f)
            {
                SetOperationalActive(false);
                SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_NO_CAPACITY), true);
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

        private void UpdateActiveState()
        {
            bool canAcceptEnergy = InputStoreEnabled &&
                StorageNetworkPowerService.IsNetworkOnlineForWorld(GetWorldId()) &&
                GetAvailableInputCapacityJoules() > 0f;

            if (battery != null)
            {
                battery.chargeWattage = canAcceptEnergy ? float.PositiveInfinity : 0f;
            }

            bool active = canAcceptEnergy && (PortJoulesAvailable > 0f || HasExternalPowerSourceOnCircuit());
            SetOperationalActive(active);
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

        public void SetInputStoreEnabled(bool enabled)
        {
            if (InputStoreEnabled == enabled)
            {
                return;
            }

            InputStoreEnabled = enabled;
            InputWatts = enabled ? DefaultInputWatts : 0f;
            lastStatus = string.Empty;
            cachedStatusText = null;
            UpdateActiveState();
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

            foreach (Storage target in StorageSceneCollector.CollectLightweightForWorld(GetWorldId()).Storages)
            {
                KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
                if (prefabId != null &&
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

            InputStoreEnabled = source.InputStoreEnabled;
            InputWatts = source.InputWatts;
            InputStoreModeValue = source.InputStoreModeValue;
            InputStorageInstanceId = source.InputStorageInstanceId;
            lastStatus = string.Empty;
            cachedStatusText = null;
            UpdateActiveState();
        }

        private void TransferStoredEnergyToNetwork()
        {
            if (!InputStoreEnabled ||
                PortJoulesAvailable <= 0f ||
                !StorageNetworkPowerService.IsNetworkOnlineForWorld(GetWorldId()) ||
                GetAvailableInputCapacityJoules() <= 0f)
            {
                lastTransferWatts = 0f;
                return;
            }

            float stored = CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage
                ? AddEnergyToSpecificStorage(PortJoulesAvailable)
                : StorageNetworkPowerService.AddEnergy(GetWorldId(), Mathf.Min(PortJoulesAvailable, StorageNetworkPowerService.GetAvailableChargeCapacityJoules(GetWorldId())));
            if (stored <= 0f)
            {
                lastTransferWatts = 0f;
                SetStableStatus(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_NO_CAPACITY), true);
                return;
            }

            lastTransferWatts = stored / SimTickSeconds;
            StoredJoules = Mathf.Max(0f, PortJoulesAvailable - stored);
            if (battery != null)
            {
                battery.ConsumeEnergy(stored);
            }

            SetTransientStatus(string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_STORED), FormatPowerRate(lastTransferWatts)));
        }

        private void SetOperationalActive(bool active)
        {
            lastOperationalActive = active;
            if (operational != null && operational.IsActive != active)
            {
                operational.SetActive(active);
            }
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
            return StorageNetworkPowerService.AddEnergy(powerStorage, joules);
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
                cachedStatusText = BuildStatusText();
            }

            return cachedStatusText;
        }

        private void UpdateCachedStatusText()
        {
            cachedStatusText = null;
        }

        private string BuildStatusText()
        {
            return ColorizeInfo(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STATUS_ITEM),
                GetCurrentStatusText())) + "\n" + string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STATUS_TOOLTIP),
                ColorizeEnabled(InputStoreEnabled),
                ColorizeNetwork(StorageNetworkPowerService.IsNetworkOnlineForWorld(GetWorldId())),
                ColorizeInfo(GetInputStoreModeStatusText()),
                ColorizeAmount(FormatPowerRate(lastTransferWatts)),
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
            if (!InputStoreEnabled)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
            }

            if (!StorageNetworkPowerService.IsNetworkOnlineForWorld(GetWorldId()))
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE);
            }

            if (GetAvailableInputCapacityJoules() <= 0f)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_NO_CAPACITY);
            }

            return string.IsNullOrEmpty(lastStatus)
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED)
                : lastStatus;
        }

        private static string FormatPowerRate(float watts)
        {
            return GameUtil.GetFormattedWattage(watts, GameUtil.WattageFormatterUnit.Automatic, true);
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
