using KSerialization;
using System;
using Klei.AI;
using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkPowerOutputPortGenerator : Generator, ISingleSliderControl
    {
        public const float DefaultOutputWatts = 1000f;
        public const float MinOutputWatts = 0f;
        public const float MaxOutputWatts = 100000f;
        private const float SimTickSeconds = 0.2f;
        private const float PoweredVisualHoldSeconds = 0.35f;

        [Serialize]
        public float OutputWatts = DefaultOutputWatts;

        [Serialize]
        public int SourceModeValue;

        [Serialize]
        public int SourceStorageInstanceId = KPrefabID.InvalidInstanceID;

        [Serialize]
        public bool OutputLimitEnabled;

        [Serialize]
        public float OutputLimitJoules;

        [Serialize]
        public float OutputLimitUsedJoules;

        [Serialize]
        public float StoredJoules;

        [MyCmpGet]
        private Storage storage = null;

        private static StatusItem powerOutputPortStatusItem;
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkPowerOutputPortGenerator> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkPowerOutputPortGenerator>((component, data) => component.OnCopySettings(data));

        private Guid powerOutputPortStatusHandle = Guid.Empty;
        private int worldId = -1;
        private string lastStatus;
        private float syncedGeneratorWatts = -1f;
        private AttributeModifier outputRateModifier;
        private float outputBudgetJoules;
        private float poweredVisualHold;
        private string cachedStatusText;

        public StorageNetworkMaterialRequester.RequestMode CurrentSourceMode
        {
            get => (StorageNetworkMaterialRequester.RequestMode)Mathf.Clamp(SourceModeValue, 0, 1);
            set => SourceModeValue = (int)value;
        }

        public float PortJoulesAvailable => Mathf.Clamp(StoredJoules, 0f, Capacity);

        public float PortAvailableCapacityJoules => Mathf.Max(0f, Capacity - PortJoulesAvailable);

        public override float JoulesAvailable
        {
            get
            {
                if (!StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()) || GetOutputWatts() <= 0f)
                {
                    return 0f;
                }

                float available = Mathf.Min(GetOutputBudgetJoules(), GetAvailableOutputJoules());
                if (available > 0f)
                {
                    return available;
                }

                return poweredVisualHold > 0f && CanPotentiallyOutput()
                    ? Mathf.Min(1f, Mathf.Max(1f, GetOutputWatts() * SimTickSeconds))
                    : 0f;
            }
        }

        public override float Capacity => Mathf.Max(1f, storage != null ? storage.Capacity() : 1f);

        public override bool IsEmpty => JoulesAvailable <= 0f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            StoredJoules = PortJoulesAvailable;
            SyncGeneratorOutputModifier();
            RefreshPowerOutputPortStatus();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnCleanUp()
        {
            RemovePowerOutputPortStatus();
            base.OnCleanUp();
        }

        public override void EnergySim200ms(float dt)
        {
            SyncGeneratorOutputModifier();
            poweredVisualHold = Mathf.Max(0f, poweredVisualHold - Mathf.Max(0f, dt));
            outputBudgetJoules = GetOutputWatts() * SimTickSeconds;
            RefreshPowerOutputPortStatus();
            bool canOutput = GetOutputWatts() > 0f &&
                StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()) &&
                !IsOutputLimitSatisfied();
            if (canOutput)
            {
                RefillOutputBuffer();
            }
            else
            {
                outputBudgetJoules = 0f;
                ResetJoules();
            }

            base.EnergySim200ms(dt);
            float joulesAvailableThisTick = JoulesAvailable;
            if (GetOutputWatts() <= 0f)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
            }
            else if (!StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()))
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE);
            }
            else if (IsOutputLimitSatisfied())
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED);
            }
            else if (joulesAvailableThisTick <= 0f)
            {
                lastStatus = GetStoredJoulesAvailable() <= 0f
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_NO_STORED)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED);
            }

            UpdateCachedStatusText();
        }

        public override void ApplyDeltaJoules(float joulesDelta, bool canOverPower = false)
        {
            if (joulesDelta < 0f)
            {
                ConsumeEnergy(-joulesDelta);
                return;
            }

            ResetJoules();
        }

        public override void ConsumeEnergy(float joules)
        {
            ConsumeFromBufferThenNetwork(joules);
        }

        public override bool IsProducingPower()
        {
            return StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()) &&
                base.IsProducingPower() &&
                JoulesAvailable > 0f &&
                GetOutputWatts() > 0f;
        }

        private float GetOutputWatts()
        {
            return Mathf.Max(0f, OutputWatts);
        }

        public float GetOutputWattsSetting()
        {
            return Mathf.Clamp(OutputWatts, MinOutputWatts, MaxOutputWatts);
        }

        public void SetOutputWatts(float watts)
        {
            OutputWatts = Mathf.Clamp(watts, MinOutputWatts, MaxOutputWatts);
            SyncGeneratorOutputModifier();
        }

        public string SliderTitleKey => "STRINGS.UI.STORAGE_NETWORK.POWER_PORT_OUTPUT_RATE";

        public string SliderUnits => global::STRINGS.UI.UNITSUFFIXES.ELECTRICAL.WATT;

        public int SliderDecimalPlaces(int index)
        {
            return 0;
        }

        public float GetSliderMin(int index)
        {
            return MinOutputWatts;
        }

        public float GetSliderMax(int index)
        {
            return MaxOutputWatts;
        }

        public float GetSliderValue(int index)
        {
            return GetOutputWattsSetting();
        }

        public void SetSliderValue(float value, int index)
        {
            SetOutputWatts(value);
        }

        public string GetSliderTooltipKey(int index)
        {
            return "STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_RATE_TOOLTIP";
        }

        public string GetSliderTooltip(int index)
        {
            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_RATE_TOOLTIP),
                FormatPowerRate(GetOutputWattsSetting()));
        }

        public void SetSourceStorage(Storage source)
        {
            KPrefabID prefabId = source != null ? source.GetComponent<KPrefabID>() : null;
            SourceStorageInstanceId = prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
            CurrentSourceMode = StorageNetworkMaterialRequester.RequestMode.SpecificStorage;
        }

        public void UseAutomaticSourceStorage()
        {
            CurrentSourceMode = StorageNetworkMaterialRequester.RequestMode.SearchNetwork;
            SourceStorageInstanceId = KPrefabID.InvalidInstanceID;
        }

        public Storage ResolveSourceStorage()
        {
            if (SourceStorageInstanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage source = info?.Storage;
                KPrefabID prefabId = source != null ? source.GetComponent<KPrefabID>() : null;
                if (info?.Minion == null &&
                    prefabId != null &&
                    prefabId.InstanceID == SourceStorageInstanceId &&
                    source.GetComponent<StorageNetworkPowerStorage>() != null)
                {
                    return source;
                }
            }

            return null;
        }

        public void SetOutputLimitEnabled(bool enabled)
        {
            OutputLimitEnabled = enabled;
            if (enabled && OutputLimitJoules <= 0f)
            {
                OutputLimitJoules = Mathf.Max(DefaultOutputWatts, StorageNetworkPowerService.GetCapacityJoules(GetWorldId()) * 0.1f);
            }
        }

        public void ResetOutputLimitUsed()
        {
            OutputLimitUsedJoules = 0f;
        }

        public bool IsOutputLimitSatisfied()
        {
            return OutputLimitEnabled && Mathf.Max(0f, OutputLimitUsedJoules) >= Mathf.Max(0f, OutputLimitJoules) - 0.01f;
        }

        private float GetAvailableOutputJoules()
        {
            if (IsOutputLimitSatisfied())
            {
                return 0f;
            }

            float available = PortJoulesAvailable + GetStoredJoulesAvailable();
            if (!OutputLimitEnabled)
            {
                return available;
            }

            return Mathf.Min(available, Mathf.Max(0f, OutputLimitJoules - OutputLimitUsedJoules));
        }

        private float GetOutputBudgetJoules()
        {
            if (outputBudgetJoules <= 0f)
            {
                outputBudgetJoules = GetOutputWatts() * SimTickSeconds;
            }

            return Mathf.Max(0f, outputBudgetJoules);
        }

        private bool CanPotentiallyOutput()
        {
            return GetOutputWatts() > 0f &&
                !IsOutputLimitSatisfied() &&
                StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()) &&
                GetAvailableOutputJoules() > 0f;
        }

        private float GetStoredJoulesAvailable()
        {
            if (CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = ResolveSourceStorage();
                StorageNetworkPowerStorage powerStorage = source != null ? source.GetComponent<StorageNetworkPowerStorage>() : null;
                return powerStorage != null ? powerStorage.RawJoulesAvailable : 0f;
            }

            return StorageNetworkPowerService.GetStoredJoules(GetWorldId());
        }

        private float ConsumeFromBufferThenNetwork(float joules)
        {
            if (joules <= 0f ||
                IsOutputLimitSatisfied() ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()))
            {
                return 0f;
            }

            float limitedJoules = OutputLimitEnabled
                ? Mathf.Min(joules, Mathf.Max(0f, OutputLimitJoules - OutputLimitUsedJoules))
                : joules;
            float consumedFromBuffer = ConsumeFromBuffer(limitedJoules);
            float remaining = Mathf.Max(0f, limitedJoules - consumedFromBuffer);
            float consumedFromNetwork = ConsumeFromNetwork(remaining);
            float consumed = consumedFromBuffer + consumedFromNetwork;
            outputBudgetJoules = Mathf.Max(0f, GetOutputBudgetJoules() - consumed);
            if (OutputLimitEnabled)
            {
                OutputLimitUsedJoules += consumed;
            }

            lastStatus = consumed > 0f
                ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_OUTPUT), GameUtil.GetFormattedJoules(consumed, "F1", GameUtil.TimeSlice.None))
                : Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_NO_STORED);
            if (consumed > 0f)
            {
                poweredVisualHold = PoweredVisualHoldSeconds;
            }

            UpdateCachedStatusText();
            return consumed;
        }

        private float ConsumeFromBuffer(float joules)
        {
            float consumed = Mathf.Min(Mathf.Max(0f, joules), PortJoulesAvailable);
            if (consumed > 0f)
            {
                StoredJoules = Mathf.Max(0f, PortJoulesAvailable - consumed);
            }

            return consumed;
        }

        private void RefillOutputBuffer()
        {
            if (GetOutputWatts() <= 0f ||
                PortAvailableCapacityJoules <= 0f ||
                IsOutputLimitSatisfied() ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()))
            {
                return;
            }

            float requested = Mathf.Min(GetOutputWatts() * SimTickSeconds, PortAvailableCapacityJoules);
            if (requested <= 0f)
            {
                return;
            }

            float refilled = ConsumeFromNetwork(requested);
            if (refilled > 0f)
            {
                StoredJoules = Mathf.Min(Capacity, PortJoulesAvailable + refilled);
            }
        }

        private float ConsumeFromNetwork(float joules)
        {
            if (joules <= 0f)
            {
                return 0f;
            }

            float consumed = 0f;
            if (CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = ResolveSourceStorage();
                consumed = StorageNetworkPowerService.ConsumeEnergy(source != null ? source.GetComponent<StorageNetworkPowerStorage>() : null, joules);
            }
            else
            {
                consumed = StorageNetworkPowerService.ConsumeEnergy(GetWorldId(), joules);
            }

            return consumed;
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            StorageNetworkPowerOutputPortGenerator source = sourceObject != null
                ? sourceObject.GetComponent<StorageNetworkPowerOutputPortGenerator>()
                : null;
            if (source == null || source == this)
            {
                return;
            }

            OutputWatts = source.OutputWatts;
            SyncGeneratorOutputModifier();
            SourceModeValue = source.SourceModeValue;
            SourceStorageInstanceId = source.SourceStorageInstanceId;
            OutputLimitEnabled = source.OutputLimitEnabled;
            OutputLimitJoules = source.OutputLimitJoules;
            OutputLimitUsedJoules = 0f;
            lastStatus = string.Empty;
            cachedStatusText = null;
        }

        private void RefreshPowerOutputPortStatus()
        {
            if (powerOutputPortStatusHandle != Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                powerOutputPortStatusHandle = selectable.AddStatusItem(GetPowerOutputPortStatusItem(), this);
            }
        }

        private void RemovePowerOutputPortStatus()
        {
            if (powerOutputPortStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(powerOutputPortStatusHandle);
            }

            powerOutputPortStatusHandle = Guid.Empty;
        }

        private static StatusItem GetPowerOutputPortStatusItem()
        {
            if (powerOutputPortStatusItem != null)
            {
                return powerOutputPortStatusItem;
            }

            powerOutputPortStatusItem = new StatusItem(
                "StorageNetworkPowerOutputPort",
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            powerOutputPortStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkPowerOutputPortGenerator output = data as StorageNetworkPowerOutputPortGenerator;
                return output != null ? output.GetStatusText() : text;
            };
            powerOutputPortStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkPowerOutputPortGenerator output = data as StorageNetworkPowerOutputPortGenerator;
                return output != null ? output.GetStatusText() : tooltip;
            };

            return powerOutputPortStatusItem;
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
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_STATUS_ITEM),
                GetCurrentStatusText())) + "\n" + string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_STATUS_TOOLTIP),
                ColorizeEnabled(GetOutputWattsSetting() > 0f),
                ColorizeNetwork(StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId())),
                ColorizeInfo(GetSourceModeStatusText()),
                ColorizeLimit(GetOutputLimitStatusText()),
                ColorizeAmount(FormatPowerRate(GetOutputWattsSetting())),
                ColorizeAmount(GameUtil.GetFormattedJoules(PortJoulesAvailable, "F1", GameUtil.TimeSlice.None)),
                ColorizeAmount(GameUtil.GetFormattedJoules(Capacity, "F1", GameUtil.TimeSlice.None)),
                ColorizeStatus(GetCurrentStatusText()));
        }

        private string GetSourceModeStatusText()
        {
            if (CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = ResolveSourceStorage();
                return source != null
                    ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        private string GetOutputLimitStatusText()
        {
            if (!OutputLimitEnabled)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
            }

            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_LIMIT),
                GameUtil.GetFormattedJoules(Mathf.Max(0f, OutputLimitUsedJoules), "F1", GameUtil.TimeSlice.None),
                GameUtil.GetFormattedJoules(Mathf.Max(0f, OutputLimitJoules), "F1", GameUtil.TimeSlice.None));
        }

        private string GetCurrentStatusText()
        {
            if (GetOutputWattsSetting() <= 0f)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
            }

            if (!StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()))
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE);
            }

            if (IsOutputLimitSatisfied())
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED);
            }

            return string.IsNullOrEmpty(lastStatus)
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED)
                : lastStatus;
        }

        private static string FormatPowerRate(float watts)
        {
            return GameUtil.GetFormattedWattage(watts, GameUtil.WattageFormatterUnit.Automatic, true);
        }

        private void SyncGeneratorOutputModifier()
        {
            float watts = GetOutputWattsSetting();
            if (Mathf.Approximately(syncedGeneratorWatts, watts))
            {
                return;
            }

            AttributeInstance outputAttribute = gameObject.GetAttributes()?.Get(Db.Get().Attributes.GeneratorOutput.Id);
            if (outputAttribute == null)
            {
                return;
            }

            float modifierValue = (Mathf.Clamp01(watts / MaxOutputWatts) - 1f) * 100f;
            if (outputRateModifier == null)
            {
                outputRateModifier = new AttributeModifier(
                    Db.Get().Attributes.GeneratorOutput.Id,
                    modifierValue,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_PORT_OUTPUT_RATE),
                    false,
                    false,
                    false);
                outputAttribute.Add(outputRateModifier);
            }
            else
            {
                outputRateModifier.SetValue(modifierValue);
            }

            syncedGeneratorWatts = watts;
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

        private static string ColorizeLimit(string text)
        {
            return Colorize(text, text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED)) ? "#9aa3ad" : "#f0c96a");
        }

        private static string ColorizeStatus(string text)
        {
            bool warning = text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STATUS_NO_STORED)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED));
            return Colorize(text, warning ? "#d86a6a" : "#55d17a");
        }

        private static string Colorize(string text, string color)
        {
            return string.Format("<color={0}>{1}</color>", color, text);
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
