using KSerialization;
using System;
using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.Services;
using StorageNetwork.UI;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkLiquidOutputPortEgress : KMonoBehaviour, ISim1000ms, ISidescreenButtonControl
    {
        private const float RetrySeconds = 1f;
        public const float DefaultRequestRateKgPerSecond = 10f;
        public const float MinRequestRateKgPerSecond = 0.1f;
        public const float MaxRequestRateKgPerSecond = 20f;

        [Serialize]
        public bool OutputRequestEnabled;

        [Serialize]
        public int SourceModeValue;

        [Serialize]
        public int SourceStorageInstanceId = KPrefabID.InvalidInstanceID;

        [Serialize]
        public int OutputElementHash;

        [Serialize]
        public bool OutputLimitEnabled;

        [Serialize]
        public float OutputLimitKg;

        [Serialize]
        public float OutputLimitUsedKg;

        [Serialize]
        public float RequestRateKgPerSecond = DefaultRequestRateKgPerSecond;

        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private ConduitDispenser dispenser = null;

        private static StatusItem liquidOutputPortStatusItem;
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkLiquidOutputPortEgress> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkLiquidOutputPortEgress>((component, data) => component.OnCopySettings(data));

        private Guid liquidOutputPortStatusHandle = Guid.Empty;
        private float retryTimer;
        private string lastStatus;
        private string cachedStatusText;

        public StorageNetworkMaterialRequester.RequestMode CurrentSourceMode
        {
            get => (StorageNetworkMaterialRequester.RequestMode)Mathf.Clamp(SourceModeValue, 0, 1);
            set => SourceModeValue = (int)value;
        }

        public string LastStatus => lastStatus;

        public Storage PortStorage => storage;

        public string SidescreenButtonText =>
            Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER) + "：" + GetOutputFilterStatusText();

        public string SidescreenButtonTooltip =>
            Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_DESC);

        public string SidescreenTitle =>
            Loc.Get(Loc.UI.STORAGE_NETWORK.LIQUID_OUTPUT_SIDE_SCREEN_TITLE);

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (storage != null)
            {
                storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
            }

            InitializeOutputLimitUsage();
            SyncDispenserFilter();
            SyncDispenserState();
            RefreshLiquidOutputPortStatus();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnCleanUp()
        {
            RemoveLiquidOutputPortStatus();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            RefreshLiquidOutputPortStatus();
            SyncDispenserState();

            if (storage == null || !OutputRequestEnabled)
            {
                return;
            }

            if (retryTimer > 0f)
            {
                retryTimer -= dt;
                return;
            }

            RequestFromNetwork();
            UpdateCachedStatusText();
        }

        private void RequestFromNetwork()
        {
            if (storage == null || !OutputRequestEnabled)
            {
                return;
            }

            float requestCapacity = GetRequestCapacityKg();
            if (requestCapacity <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_SATISFIED);
                retryTimer = RetrySeconds;
                return;
            }

            Storage source = CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? ResolveSourceStorage()
                : null;
            StorageTransferResult result = NetworkStorageTransferService.TransferAnyLiquidFromNetworkToStorage(
                storage,
                requestCapacity,
                new[] { storage },
                source,
                GetSelectedOutputElement());

            lastStatus = FormatRequestStatus(result);
            if (OutputLimitEnabled && result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                OutputLimitUsedKg += result.MovedKg;
            }

            SyncDispenserState();
            retryTimer = result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? 0f : RetrySeconds;
        }

        public void SetSourceStorage(Storage source)
        {
            SourceStorageInstanceId = GetStorageInstanceId(source);
            CurrentSourceMode = StorageNetworkMaterialRequester.RequestMode.SpecificStorage;
        }

        public void UseAutomaticSourceStorage()
        {
            CurrentSourceMode = StorageNetworkMaterialRequester.RequestMode.SearchNetwork;
            SourceStorageInstanceId = KPrefabID.InvalidInstanceID;
            lastStatus = string.Empty;
            cachedStatusText = null;
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
                if (info?.Minion == null &&
                    StorageNetworkStorageRules.IsNetworkStorageTarget(source, storage) &&
                    GetStorageInstanceId(source) == SourceStorageInstanceId)
                {
                    return source;
                }
            }

            return null;
        }

        public SimHashes? GetSelectedOutputElement()
        {
            return OutputElementHash == 0 ? (SimHashes?)null : (SimHashes)OutputElementHash;
        }

        public void SetOutputElement(SimHashes? elementHash)
        {
            OutputElementHash = elementHash.HasValue ? (int)elementHash.Value : 0;
            SyncDispenserFilter();
        }

        public void SetOutputElementAndRefresh(SimHashes? elementHash)
        {
            SetOutputElement(elementHash);
            ReturnMismatchedBufferedLiquidsToNetwork(elementHash);
            retryTimer = 0f;
            RequestFromNetwork();
            SyncDispenserState();
        }

        public void SetOutputLimitEnabled(bool enabled)
        {
            OutputLimitEnabled = enabled;
            if (enabled && OutputLimitKg <= 0f)
            {
                OutputLimitKg = storage != null ? Mathf.Max(1f, storage.Capacity()) : 1f;
            }

            if (enabled && OutputLimitUsedKg <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                OutputLimitUsedKg = storage != null ? Mathf.Max(0f, storage.MassStored()) : 0f;
            }

            SyncDispenserState();
        }

        public void ResetOutputLimitUsed()
        {
            OutputLimitUsedKg = storage != null ? Mathf.Max(0f, storage.MassStored()) : 0f;
        }

        public float GetRequestRateKgPerSecond()
        {
            return Mathf.Clamp(
                RequestRateKgPerSecond <= 0f ? DefaultRequestRateKgPerSecond : RequestRateKgPerSecond,
                MinRequestRateKgPerSecond,
                GetMaxRequestRateKgPerSecond());
        }

        public void SetRequestRateKgPerSecond(float value)
        {
            RequestRateKgPerSecond = Mathf.Clamp(value, MinRequestRateKgPerSecond, GetMaxRequestRateKgPerSecond());
            retryTimer = 0f;
            cachedStatusText = null;
        }

        public static float GetMaxRequestRateKgPerSecond()
        {
            return Mathf.Max(MinRequestRateKgPerSecond, Config.Instance.LiquidOutputMaxKgPerSecond);
        }

        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
        {
        }

        public bool SidescreenEnabled()
        {
            return storage != null;
        }

        public bool SidescreenButtonInteractable()
        {
            return storage != null;
        }

        public void OnSidescreenButtonPressed()
        {
            StorageNetworkPanel.ShowLiquidOutputFilterPicker(storage, this);
        }

        public int HorizontalGroupID()
        {
            return -1;
        }

        public int ButtonSideScreenSortOrder()
        {
            return 20;
        }

        private void InitializeOutputLimitUsage()
        {
            if (!OutputLimitEnabled || OutputLimitUsedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            OutputLimitUsedKg = storage != null ? Mathf.Max(0f, storage.MassStored()) : 0f;
        }

        public float GetRequestCapacityKg()
        {
            float remainingCapacity = storage != null ? Mathf.Max(0f, storage.RemainingCapacity()) : 0f;
            float requestRate = GetRequestRateKgPerSecond();
            if (!OutputLimitEnabled)
            {
                return Mathf.Min(remainingCapacity, requestRate);
            }

            float limit = Mathf.Max(0f, OutputLimitKg);
            float remainingLimit = Mathf.Max(0f, limit - Mathf.Max(0f, OutputLimitUsedKg));
            return Mathf.Min(remainingCapacity, remainingLimit, requestRate);
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            StorageNetworkLiquidOutputPortEgress source = sourceObject != null
                ? sourceObject.GetComponent<StorageNetworkLiquidOutputPortEgress>()
                : null;
            if (source == null || source == this)
            {
                return;
            }

            OutputRequestEnabled = source.OutputRequestEnabled;
            SourceModeValue = source.SourceModeValue;
            SourceStorageInstanceId = source.SourceStorageInstanceId;
            OutputElementHash = source.OutputElementHash;
            OutputLimitEnabled = source.OutputLimitEnabled;
            OutputLimitKg = source.OutputLimitKg;
            OutputLimitUsedKg = OutputLimitEnabled && storage != null
                ? Mathf.Max(0f, storage.MassStored())
                : 0f;
            RequestRateKgPerSecond = source.GetRequestRateKgPerSecond();
            retryTimer = 0f;
            lastStatus = string.Empty;
            cachedStatusText = null;

            ReturnMismatchedBufferedLiquidsToNetwork(GetSelectedOutputElement());
            SyncDispenserFilter();
            SyncDispenserState();
        }

        private void SyncDispenserState()
        {
            if (dispenser == null)
            {
                return;
            }

            bool shouldOutput = OutputRequestEnabled &&
                StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId());
            if (dispenser.isOn != shouldOutput)
            {
                dispenser.SetOnState(shouldOutput);
            }
        }

        private void SyncDispenserFilter()
        {
            if (dispenser == null)
            {
                return;
            }

            SimHashes? selected = GetSelectedOutputElement();
            dispenser.elementFilter = selected.HasValue ? new[] { selected.Value } : null;
        }

        private void ReturnMismatchedBufferedLiquidsToNetwork(SimHashes? selected)
        {
            if (storage == null || storage.items == null || !selected.HasValue)
            {
                return;
            }

            HashSet<Tag> mismatchedTags = new HashSet<Tag>();
            foreach (GameObject item in storage.items)
            {
                PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                if (primaryElement == null || primaryElement.ElementID == selected.Value)
                {
                    continue;
                }

                Element element = ElementLoader.FindElementByHash(primaryElement.ElementID);
                if (element != null && element.IsLiquid)
                {
                    mismatchedTags.Add(primaryElement.ElementID.CreateTag());
                }
            }

            if (mismatchedTags.Count == 0)
            {
                return;
            }

            NetworkStorageTransferService.TransferStoredItemsToNetwork(
                storage,
                new[] { storage },
                null,
                mismatchedTags);
        }

        private bool IsOutputLimitSatisfied()
        {
            if (!OutputLimitEnabled)
            {
                return false;
            }

            float limit = Mathf.Max(0f, OutputLimitKg);
            return Mathf.Max(0f, OutputLimitUsedKg) >= limit - PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private int GetWorldId()
        {
            int worldId = gameObject.GetMyWorldId();
            if (worldId != byte.MaxValue && worldId >= 0)
            {
                return worldId;
            }

            int cell = Grid.PosToCell(gameObject);
            return Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
        }

        private void RefreshLiquidOutputPortStatus()
        {
            if (liquidOutputPortStatusHandle != Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                liquidOutputPortStatusHandle = selectable.AddStatusItem(GetLiquidOutputPortStatusItem(), this);
            }
        }

        private void RemoveLiquidOutputPortStatus()
        {
            if (liquidOutputPortStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(liquidOutputPortStatusHandle);
            }

            liquidOutputPortStatusHandle = Guid.Empty;
        }

        private static StatusItem GetLiquidOutputPortStatusItem()
        {
            if (liquidOutputPortStatusItem != null)
            {
                return liquidOutputPortStatusItem;
            }

            liquidOutputPortStatusItem = new StatusItem(
                "StorageNetworkLiquidOutputPort",
                Loc.Get(Loc.UI.STORAGE_NETWORK.LIQUID_OUTPUT_PORT_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LIQUID_OUTPUT_PORT_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            liquidOutputPortStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkLiquidOutputPortEgress egress = data as StorageNetworkLiquidOutputPortEgress;
                return egress != null ? egress.GetStatusText() : text;
            };
            liquidOutputPortStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkLiquidOutputPortEgress egress = data as StorageNetworkLiquidOutputPortEgress;
                return egress != null ? egress.GetStatusText() : tooltip;
            };

            return liquidOutputPortStatusItem;
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
                Loc.Get(Loc.UI.STORAGE_NETWORK.LIQUID_OUTPUT_PORT_STATUS_ITEM),
                GetCurrentStatusText())) + "\n" + string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LIQUID_OUTPUT_PORT_STATUS_TOOLTIP),
                ColorizeEnabled(OutputRequestEnabled),
                ColorizeNetwork(StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId())),
                ColorizeInfo(GetSourceModeStatusText()),
                ColorizeInfo(GetOutputFilterStatusText()),
                ColorizeLimit(GetOutputLimitStatusText()),
                ColorizeAmount(GetRequestRateStatusText()),
                ColorizeAmount(GameUtil.GetFormattedMass(storage != null ? storage.MassStored() : 0f)),
                ColorizeAmount(GameUtil.GetFormattedMass(storage != null ? storage.Capacity() : 0f)),
                ColorizeStatus(GetCurrentStatusText()));
        }

        private string GetNetworkText()
        {
            return StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId())
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_ONLINE)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE);
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

        private string GetOutputFilterStatusText()
        {
            SimHashes? selected = GetSelectedOutputElement();
            if (!selected.HasValue)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_ANY);
            }

            Element element = ElementLoader.FindElementByHash(selected.Value);
            return element != null ? element.name : selected.Value.ToString();
        }

        private string GetOutputLimitStatusText()
        {
            if (!OutputLimitEnabled)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
            }

            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT),
                GameUtil.GetFormattedMass(Mathf.Max(0f, OutputLimitUsedKg)),
                GameUtil.GetFormattedMass(Mathf.Max(0f, OutputLimitKg)));
        }

        private string GetRequestRateStatusText()
        {
            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE_VALUE),
                GameUtil.GetFormattedMass(GetRequestRateKgPerSecond()));
        }

        private static string GetOnOffText(bool enabled)
        {
            return enabled
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
        }

        private string GetCurrentStatusText()
        {
            if (!OutputRequestEnabled)
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

        private static string ColorizeEnabled(bool enabled)
        {
            return Colorize(GetOnOffText(enabled), enabled ? "#55d17a" : "#d86a6a");
        }

        private static string ColorizeNetwork(bool online)
        {
            return Colorize(
                online ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_ONLINE) : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE),
                online ? "#55d17a" : "#d86a6a");
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
            bool disabled = text == Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
            return Colorize(text, disabled ? "#9aa3ad" : "#f0c96a");
        }

        private static string ColorizeStatus(string text)
        {
            bool warning = text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.CORE_OFFLINE_TITLE));
            bool limit = text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED));
            return Colorize(text, warning ? "#d86a6a" : limit ? "#f0c96a" : "#55d17a");
        }

        private static string Colorize(string text, string color)
        {
            return string.Format("<color={0}>{1}</color>", color, text);
        }

        private static string FormatRequestStatus(StorageTransferResult result)
        {
            if (result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return string.Format(
                    StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_REQUESTED),
                    GameUtil.GetFormattedMass(result.MovedKg),
                    string.Empty).TrimEnd();
            }

            if (result.NetworkOffline)
            {
                return StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_OFFLINE_TITLE);
            }

            if (!string.IsNullOrEmpty(result.BlockedItem))
            {
                return string.Format(
                    StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_MISSING_SOURCE),
                    result.BlockedItem);
            }

            return StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS);
        }

        private static int GetStorageInstanceId(Storage target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }
    }
}
