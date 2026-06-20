using KSerialization;
using System;
using System.Collections.Generic;
using StorageNetwork.API;
using StorageNetwork.Core;
using StorageNetwork.Services;
using StorageNetwork.UI;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkSolidOutputPortEgress : KMonoBehaviour, ISim1000ms, ISidescreenButtonControl
    {
        private const float RetrySeconds = 1f;
        public const float DefaultRequestRateKgPerSecond = 20f;
        public const float MinRequestRateKgPerSecond = 0.1f;
        public const float MaxRequestRateKgPerSecond = 100f;

        [Serialize]
        public bool OutputRequestEnabled = false;

        [Serialize]
        public int SourceModeValue;

        [Serialize]
        public int SourceStorageInstanceId = KPrefabID.InvalidInstanceID;

        [Serialize]
        public string OutputItemTagName;

        [Serialize]
        public bool OutputLimitEnabled;

        [Serialize]
        public float OutputLimitKg;

        [Serialize]
        public float OutputLimitUsedKg;

        [Serialize]
        public float RequestRateKgPerSecond = DefaultRequestRateKgPerSecond;

        [Serialize]
        public bool AllowManualOperation = true;

        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private SolidConduitDispenser dispenser = null;

        private static StatusItem solidOutputPortStatusItem;
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkSolidOutputPortEgress> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkSolidOutputPortEgress>((component, data) => component.OnCopySettings(data));

        private Guid solidOutputPortStatusHandle = Guid.Empty;
        private float retryTimer;
        private string lastStatus;
        private string cachedStatusText;
        private string cachedStatusSignature;

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
            Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_FILTER_DESC);

        public string SidescreenTitle =>
            Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_REQUEST_TITLE);

        protected override void OnSpawn()
        {
            base.OnSpawn();
            gameObject.AddOrGet<StorageNetworkSolidOutputPortManualOperationButton>();
            Destroy(gameObject.GetComponent<RequireOutputs>());
            ConfigureFetchableOutputStorage();
            storage?.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
            InitializeOutputLimitUsage();
            SyncDispenserState();
            RefreshSolidOutputPortStatus();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnCleanUp()
        {
            ClearBufferedOutputMarkers();
            RemoveSolidOutputPortStatus();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            RefreshSolidOutputPortStatus();
            SyncDispenserState();

            if (storage == null)
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

        private void ConfigureFetchableOutputStorage()
        {
            if (storage == null)
            {
                return;
            }

            storage.allowItemRemoval = AllowManualOperation;
            storage.allowUIItemRemoval = false;
            storage.ignoreSourcePriority = AllowManualOperation;
            storage.fetchCategory = AllowManualOperation
                ? Storage.FetchCategory.GeneralStorage
                : Storage.FetchCategory.Building;
            if (storage.items == null)
            {
                return;
            }

            foreach (GameObject item in storage.items)
            {
                Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
                pickupable?.OnStore(storage);
            }
        }

        private void RequestFromNetwork()
        {
            Storage source = CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage ? ResolveSourceStorage() : null;

            if (TrySupplyConstruction(source))
            {
                return;
            }

            if (TrySupplyFarming(source))
            {
                return;
            }

            if (TrySupplyFabricator(source))
            {
                return;
            }

            if (!OutputRequestEnabled)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
                retryTimer = RetrySeconds;
                return;
            }

            if (!IsSolidRailConnected())
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS);
                retryTimer = RetrySeconds;
                return;
            }

            RequestBufferedOutput(source);
        }

        private bool TrySupplyConstruction(Storage source)
        {
            if (!AllowManualOperation)
            {
                return false;
            }

            StorageTransferResult constructionResult = StorageNetworkConstructionSupplyService.SupplyNextConstruction(
                storage,
                source,
                GetSelectedOutputTags());
            if (constructionResult.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = NetworkStorageTransferService.FormatOutputStatus(constructionResult, Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS));
                if (OutputLimitEnabled)
                {
                    OutputLimitUsedKg += constructionResult.MovedKg;
                }

                RefreshFetchableBufferedItems();
                retryTimer = 0f;
                return true;
            }

            return false;
        }

        private bool TrySupplyFarming(Storage source)
        {
            if (!AllowManualOperation)
            {
                return false;
            }

            StorageTransferResult farmingResult = StorageNetworkFarmingSupplyService.SupplyNextPlanting(
                storage,
                source,
                GetSelectedOutputTags());
            if (farmingResult.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = NetworkStorageTransferService.FormatOutputStatus(farmingResult, Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS));
                if (OutputLimitEnabled)
                {
                    OutputLimitUsedKg += farmingResult.MovedKg;
                }

                RefreshFetchableBufferedItems();
                retryTimer = 0f;
                return true;
            }

            return false;
        }

        private bool TrySupplyFabricator(Storage source)
        {
            if (!AllowManualOperation)
            {
                return false;
            }

            StorageTransferResult fabricatorResult = StorageNetworkFabricatorSupplyService.SupplyNextFabricator(
                storage,
                source,
                GetSelectedOutputTags());
            if (fabricatorResult.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = NetworkStorageTransferService.FormatOutputStatus(fabricatorResult, Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS));
                if (OutputLimitEnabled)
                {
                    OutputLimitUsedKg += fabricatorResult.MovedKg;
                }

                RefreshFetchableBufferedItems();
                retryTimer = 0f;
                return true;
            }

            return false;
        }

        private void RequestBufferedOutput(Storage source)
        {
            float requestCapacity = GetRequestCapacityKg();
            if (requestCapacity <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_SATISFIED);
                retryTimer = RetrySeconds;
                return;
            }

            StorageTransferResult result = NetworkStorageTransferService.TransferAnySolidFromNetworkToStorage(
                storage,
                requestCapacity,
                new[] { storage },
                source,
                GetSelectedOutputTags());

            lastStatus = NetworkStorageTransferService.FormatOutputStatus(result, Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS));
            if (OutputLimitEnabled && result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                OutputLimitUsedKg += result.MovedKg;
            }

            SyncDispenserState();
            MarkBufferedOutputItems();
            RefreshFetchableBufferedItems();
            retryTimer = result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? 0f : RetrySeconds;
        }

        private bool IsSolidRailConnected()
        {
            return dispenser != null && dispenser.IsConnected;
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

        public void SetOutputLimitEnabled(bool enabled)
        {
            OutputLimitEnabled = enabled;
            if (enabled && OutputLimitKg <= 0f)
            {
                OutputLimitKg = Mathf.Max(1f, GetPortCapacityKg());
            }

            if (enabled && OutputLimitUsedKg <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                OutputLimitUsedKg = GetPortStoredMassKg();
            }
        }

        public void ResetOutputLimitUsed()
        {
            OutputLimitUsedKg = GetPortStoredMassKg();
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
            return Mathf.Max(MinRequestRateKgPerSecond, Config.Instance.SolidOutputMaxKgPerSecond);
        }

        public void SetAllowManualOperation(bool enabled)
        {
            AllowManualOperation = enabled;
            ConfigureFetchableOutputStorage();
            RefreshFetchableBufferedItems();
            cachedStatusText = null;
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
            StorageNetworkPanel.ShowMaterialOutputFilterPicker(storage, this);
        }

        public int HorizontalGroupID()
        {
            return -1;
        }

        public int ButtonSideScreenSortOrder()
        {
            return 20;
        }

        public float GetRequestCapacityKg()
        {
            float remainingCapacity = GetPortRemainingCapacityKg();
            float requestRate = GetRequestRateKgPerSecond();
            if (!OutputLimitEnabled)
            {
                return Mathf.Min(remainingCapacity, requestRate);
            }

            float remainingLimit = Mathf.Max(0f, Mathf.Max(0f, OutputLimitKg) - Mathf.Max(0f, OutputLimitUsedKg));
            return Mathf.Min(remainingCapacity, remainingLimit, requestRate);
        }

        private void InitializeOutputLimitUsage()
        {
            if (!OutputLimitEnabled || OutputLimitUsedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            OutputLimitUsedKg = GetPortStoredMassKg();
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            StorageNetworkSolidOutputPortEgress source = sourceObject != null ? sourceObject.GetComponent<StorageNetworkSolidOutputPortEgress>() : null;
            if (source == null || source == this)
            {
                return;
            }

            OutputRequestEnabled = source.OutputRequestEnabled;
            SourceModeValue = source.SourceModeValue;
            SourceStorageInstanceId = source.SourceStorageInstanceId;
            OutputItemTagName = source.OutputItemTagName;
            OutputLimitEnabled = source.OutputLimitEnabled;
            OutputLimitKg = source.OutputLimitKg;
            OutputLimitUsedKg = OutputLimitEnabled ? GetPortStoredMassKg() : 0f;
            RequestRateKgPerSecond = source.GetRequestRateKgPerSecond();
            AllowManualOperation = source.AllowManualOperation;
            retryTimer = 0f;
            lastStatus = string.Empty;
            cachedStatusText = null;
            ConfigureFetchableOutputStorage();
            SyncDispenserState();
        }

        private void SyncDispenserState()
        {
            if (dispenser == null)
            {
                return;
            }

            dispenser.alwaysDispense = OutputRequestEnabled && StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId());
        }

        private void RefreshFetchableBufferedItems()
        {
            if (storage == null || storage.items == null)
            {
                return;
            }

            storage.allowItemRemoval = AllowManualOperation;
            storage.ignoreSourcePriority = AllowManualOperation;
            storage.fetchCategory = AllowManualOperation
                ? Storage.FetchCategory.GeneralStorage
                : Storage.FetchCategory.Building;
            foreach (GameObject item in storage.items)
            {
                Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
                if (pickupable != null)
                {
                    pickupable.OnStore(storage);
                }
            }
        }

        private void MarkBufferedOutputItems()
        {
            if (storage?.items == null)
            {
                return;
            }

            foreach (GameObject item in storage.items)
            {
                if (item == null || StorageNetworkConstructionSupplyService.IsConstructionReserved(item))
                {
                    continue;
                }

                item.GetComponent<KPrefabID>()?.AddTag(StorageNetworkTags.SolidOutputPortBufferedItem, true);
            }
        }

        private void ClearBufferedOutputMarkers()
        {
            if (storage?.items == null)
            {
                return;
            }

            foreach (GameObject item in storage.items)
            {
                StorageNetworkConstructionSupplyService.ClearSolidOutputBufferMarker(item);
            }
        }

        public Tag? GetSelectedOutputTag()
        {
            return string.IsNullOrEmpty(OutputItemTagName) ? (Tag?)null : OutputItemTagName.ToTag();
        }

        public void SetOutputTag(Tag? tag)
        {
            OutputItemTagName = tag.HasValue && tag.Value != Tag.Invalid ? tag.Value.Name : null;
        }

        public void SetOutputTagAndRefresh(Tag? tag)
        {
            SetOutputTag(tag);
            ReturnMismatchedBufferedItemsToNetwork(tag);
            retryTimer = 0f;
            RequestFromNetwork();
            SyncDispenserState();
        }

        private HashSet<Tag> GetSelectedOutputTags()
        {
            Tag? selected = GetSelectedOutputTag();
            if (!selected.HasValue || selected.Value == Tag.Invalid)
            {
                return null;
            }

            return new HashSet<Tag> { selected.Value };
        }

        private void ReturnMismatchedBufferedItemsToNetwork(Tag? selected)
        {
            if (storage == null || storage.items == null)
            {
                return;
            }

            List<GameObject> itemsToReturn = new List<GameObject>();
            foreach (GameObject item in storage.items)
            {
                if (!IsNormalBufferedOutputItem(item))
                {
                    continue;
                }

                if (!selected.HasValue ||
                    selected.Value == Tag.Invalid ||
                    !StorageItemUtility.MatchesStorageTag(item, selected.Value))
                {
                    itemsToReturn.Add(item);
                }
            }

            foreach (GameObject item in itemsToReturn)
            {
                if (item == null || storage.items == null || !storage.items.Contains(item))
                {
                    continue;
                }

                StorageNetworkConstructionSupplyService.ClearSolidOutputBufferMarker(item);
                NetworkStorageTransferService.TransferStoredItemToNetwork(
                    storage,
                    item,
                    new[] { storage },
                    null,
                    preferColdStorageForFood: true);
            }
        }

        private static bool IsNormalBufferedOutputItem(GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            if (prefabId == null ||
                !prefabId.HasTag(StorageNetworkTags.SolidOutputPortBufferedItem))
            {
                return false;
            }

            return !prefabId.HasTag(StorageNetworkTags.ReservedForConstruction) &&
                !prefabId.HasTag(StorageNetworkTags.ReservedForFarming) &&
                !prefabId.HasTag(StorageNetworkTags.ReservedForFabricator);
        }

        private bool IsOutputLimitSatisfied()
        {
            return OutputLimitEnabled && Mathf.Max(0f, OutputLimitUsedKg) >= Mathf.Max(0f, OutputLimitKg) - PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private void RefreshSolidOutputPortStatus()
        {
            if (solidOutputPortStatusHandle != Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                solidOutputPortStatusHandle = selectable.AddStatusItem(GetSolidOutputPortStatusItem(), this);
            }
        }

        private void RemoveSolidOutputPortStatus()
        {
            if (solidOutputPortStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(solidOutputPortStatusHandle);
            }

            solidOutputPortStatusHandle = Guid.Empty;
        }

        private static StatusItem GetSolidOutputPortStatusItem()
        {
            if (solidOutputPortStatusItem != null)
            {
                return solidOutputPortStatusItem;
            }

            solidOutputPortStatusItem = new StatusItem(
                "StorageNetworkSolidOutputPort",
                Loc.Get(Loc.UI.STORAGE_NETWORK.SOLID_OUTPUT_PORT_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.SOLID_OUTPUT_PORT_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            solidOutputPortStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkSolidOutputPortEgress egress = data as StorageNetworkSolidOutputPortEgress;
                return egress != null ? egress.GetStatusText() : text;
            };
            solidOutputPortStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkSolidOutputPortEgress egress = data as StorageNetworkSolidOutputPortEgress;
                return egress != null ? egress.GetStatusText() : tooltip;
            };

            return solidOutputPortStatusItem;
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
            string signature = BuildStatusSignature();
            if (cachedStatusText != null && cachedStatusSignature == signature)
            {
                return;
            }

            cachedStatusText = BuildStatusText();
            cachedStatusSignature = signature;
        }

        private string BuildStatusSignature()
        {
            return string.Format(
                "{0}|{1}|{2}|{3}|{4:0.###}|{5:0.###}|{6:0.###}|{7:0.###}|{8}|{9}|{10}|{11:0.###}|{12:0.###}",
                OutputRequestEnabled,
                SourceModeValue,
                SourceStorageInstanceId,
                OutputItemTagName,
                OutputLimitEnabled ? OutputLimitKg : -1f,
                OutputLimitEnabled ? OutputLimitUsedKg : -1f,
                RequestRateKgPerSecond,
                retryTimer,
                AllowManualOperation,
                lastStatus,
                StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()),
                GetPortStoredMassKg(),
                GetPortCapacityKg());
        }

        private string BuildStatusText()
        {
            return ColorizeInfo(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.SOLID_OUTPUT_PORT_STATUS_ITEM),
                GetCurrentStatusText())) + "\n" + string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.SOLID_OUTPUT_PORT_STATUS_TOOLTIP),
                ColorizeEnabled(OutputRequestEnabled),
                ColorizeNetwork(StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId())),
                ColorizeInfo(GetSourceModeStatusText()),
                ColorizeInfo(GetOutputFilterStatusText()),
                ColorizeLimit(GetOutputLimitStatusText()),
                ColorizeAmount(GetRequestRateStatusText()),
                ColorizeAmount(GameUtil.GetFormattedMass(GetPortStoredMassKg())),
                ColorizeAmount(GameUtil.GetFormattedMass(GetPortCapacityKg())),
                ColorizeStatus(GetCurrentStatusText()),
                ColorizeManual(AllowManualOperation));
        }

        private float GetPortStoredMassKg()
        {
            return storage != null ? Mathf.Max(0f, StorageItemUtility.GetStoredMass(storage)) : 0f;
        }

        private float GetPortCapacityKg()
        {
            return storage != null ? Mathf.Max(0f, storage.Capacity()) : 0f;
        }

        private float GetPortRemainingCapacityKg()
        {
            return Mathf.Max(0f, GetPortCapacityKg() - GetPortStoredMassKg());
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
            Tag? selected = GetSelectedOutputTag();
            return selected.HasValue && selected.Value != Tag.Invalid
                ? StorageItemUtility.GetTagDisplayName(selected.Value)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_FILTER_ANY);
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
            return string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE_VALUE), GameUtil.GetFormattedMass(GetRequestRateKgPerSecond()));
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

            return string.IsNullOrEmpty(lastStatus) ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED) : lastStatus;
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
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.CORE_OFFLINE_TITLE)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED));
            return Colorize(text, warning ? "#d86a6a" : "#55d17a");
        }

        private static string ColorizeManual(bool enabled)
        {
            return Colorize(
                enabled
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL_ALLOWED)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL_FORBIDDEN),
                enabled ? "#55d17a" : "#9aa3ad");
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

    }
}
