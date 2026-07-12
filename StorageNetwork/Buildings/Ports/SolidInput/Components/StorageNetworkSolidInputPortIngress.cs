using KSerialization;
using System;
using System.Runtime.Serialization;
using StorageNetwork.API;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkSolidInputPortIngress : KMonoBehaviour, ISim1000ms
    {
        private const float EmptyRetrySeconds = 2f;

        [Serialize]
        public bool InputStoreEnabled = true;

        [Serialize]
        public bool AllowManualOperation;

        [Serialize]
        public bool ManualOperationMigratedToAutomatable;

        [Serialize]
        public int InputStoreModeValue;

        [Serialize]
        public int InputStorageInstanceId = KPrefabID.InvalidInstanceID;

        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private Automatable automatable = null;

        private static StatusItem solidInputPortStatusItem;
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkSolidInputPortIngress> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkSolidInputPortIngress>((component, data) => component.OnCopySettings(data));

        private Guid solidInputPortStatusHandle = Guid.Empty;
        private FilteredStorage filteredStorage;
        private float retryTimer;
        private string lastStatus;
        private string cachedStatusText;
        private bool shouldMigrateManualOperation;
        private bool wasInputEnabled;

        public StorageNetworkMaterialRequester.OutputStoreMode CurrentInputStoreMode
        {
            get => (StorageNetworkMaterialRequester.OutputStoreMode)Mathf.Clamp(InputStoreModeValue, 0, 1);
            set => InputStoreModeValue = (int)value;
        }

        public string LastStatus => lastStatus;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            filteredStorage = new FilteredStorage(
                this,
                new[] { StorageNetworkTags.SolidOutputPortBufferedItem, StorageNetworkTags.ReservedForConstruction, StorageNetworkTags.ReservedForFarming, StorageNetworkTags.ReservedForFabricator },
                null,
                false,
                Db.Get().ChoreTypes.StorageFetch);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            storage?.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
            if (shouldMigrateManualOperation)
            {
                MigrateManualOperationToAutomatable();
            }
            else
            {
                ManualOperationMigratedToAutomatable = true;
                SyncManualOperation();
            }

            wasInputEnabled = InputStoreEnabled;
            if (InputStoreEnabled)
            {
                EnsureFilteredStorage();
                filteredStorage?.FilterChanged();
            }
            else
            {
                CleanupFilteredStorage();
            }

            RefreshSolidInputPortStatus();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        [OnDeserialized]
        private void OnDeserialized()
        {
            shouldMigrateManualOperation = !ManualOperationMigratedToAutomatable;
        }

        protected override void OnCleanUp()
        {
            CleanupFilteredStorage();
            RemoveSolidInputPortStatus();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            Operational operational = GetComponent<Operational>();
            if (operational != null && !operational.IsOperational)
            {
                return;
            }

            SyncManualOperation();
            RefreshSolidInputPortStatus();
            if (storage == null)
            {
                return;
            }

            // 检测开关状态变化
            if (wasInputEnabled && !InputStoreEnabled)
            {
                // 用户关闭了输入：将缓存物品退回网络，撤销 FilteredStorage 阻止新送货
                wasInputEnabled = false;
                if (storage.items != null && storage.items.Count > 0)
                {
                    NetworkStorageTransferService.TransferStoredItemsToNetwork(
                        storage,
                        new[] { storage },
                        null,
                        null,
                        true,
                        true);
                    UpdateCachedStatusText();
                }

                CleanupFilteredStorage();

                return;
            }

            if (!wasInputEnabled && InputStoreEnabled)
            {
                // 用户重新开启了输入：重建 FilteredStorage 允许送货
                wasInputEnabled = true;
                EnsureFilteredStorage();
                filteredStorage?.FilterChanged();
                retryTimer = 0f;
                // 继续执行正常传输逻辑
            }

            if (!InputStoreEnabled)
            {
                return;
            }

            if (retryTimer > 0f)
            {
                retryTimer -= dt;
                return;
            }

            if (storage.items == null || storage.items.Count == 0)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS);
                retryTimer = EmptyRetrySeconds;
                UpdateCachedStatusText();
                return;
            }

            StorageTransferResult result = NetworkStorageTransferService.TransferStoredItemsToNetwork(
                storage,
                new[] { storage },
                CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage ? ResolveInputStorage() : null,
                null,
                true,
                true);

            if (CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage &&
                result.MovedKg <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                !string.IsNullOrEmpty(result.BlockedItem))
            {
                DropItemsRejectedBySpecificTarget();
            }

            lastStatus = FormatInputStoreStatus(result);
            retryTimer = result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? 0f : EmptyRetrySeconds;
            UpdateCachedStatusText();
        }

        private void DropItemsRejectedBySpecificTarget()
        {
            Storage target = ResolveInputStorage();
            if (storage?.items == null)
            {
                return;
            }

            var excluded = StorageTargetSelector.BuildExclusionSet(new[] { storage });
            var items = new System.Collections.Generic.List<GameObject>(storage.items);
            int sourceWorldId = StorageTargetSelector.GetObjectWorldId(gameObject);
            foreach (GameObject item in items)
            {
                if (item != null &&
                    (target == null || StorageTargetSelector.FindOutputTarget(
                        item,
                        StorageItemUtility.GetStorageMatchTags(item),
                        excluded,
                        target,
                        null,
                        sourceWorldId,
                        storage) == null))
                {
                    storage.Drop(item, true);
                }
            }
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
            lastStatus = string.Empty;
            cachedStatusText = null;
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
                if (info?.Minion == null &&
                    StorageNetworkStorageRules.IsNetworkStorageTarget(target, storage) &&
                    GetStorageInstanceId(target) == InputStorageInstanceId)
                {
                    return target;
                }
            }

            return null;
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            StorageNetworkSolidInputPortIngress source = sourceObject != null ? sourceObject.GetComponent<StorageNetworkSolidInputPortIngress>() : null;
            if (source == null || source == this)
            {
                return;
            }

            InputStoreEnabled = source.InputStoreEnabled;
            AllowManualOperation = source.AllowManualOperation;
            InputStoreModeValue = source.InputStoreModeValue;
            InputStorageInstanceId = source.InputStorageInstanceId;
            StorageNetworkFilterCopyHelper.CopyFilters(gameObject, sourceObject);
            if (automatable != null)
            {
                automatable.SetAutomationOnly(source.automatable != null
                    ? source.automatable.GetAutomationOnly()
                    : !source.AllowManualOperation);
                ManualOperationMigratedToAutomatable = true;
            }

            retryTimer = 0f;
            lastStatus = string.Empty;
            cachedStatusText = null;
            SyncManualOperation();
        }

        private void EnsureFilteredStorage()
        {
            if (filteredStorage != null)
            {
                return;
            }

            filteredStorage = new FilteredStorage(
                this,
                new[] { StorageNetworkTags.SolidOutputPortBufferedItem, StorageNetworkTags.ReservedForConstruction, StorageNetworkTags.ReservedForFarming, StorageNetworkTags.ReservedForFabricator },
                null,
                false,
                Db.Get().ChoreTypes.StorageFetch);
        }

        private void CleanupFilteredStorage()
        {
            if (filteredStorage == null)
            {
                return;
            }

            filteredStorage.CleanUp();
            filteredStorage = null;
        }

        private void SyncManualOperation()
        {
            if (storage == null)
            {
                return;
            }

            AllowManualOperation = automatable == null || !automatable.GetAutomationOnly();
            storage.allowItemRemoval = false;
            storage.allowUIItemRemoval = false;
            storage.fetchCategory = Storage.FetchCategory.Building;
        }

        private void MigrateManualOperationToAutomatable()
        {
            if (automatable == null)
            {
                return;
            }

            automatable.SetAutomationOnly(!AllowManualOperation);
            ManualOperationMigratedToAutomatable = true;
            shouldMigrateManualOperation = false;
            SyncManualOperation();
        }

        private void RefreshSolidInputPortStatus()
        {
            if (solidInputPortStatusHandle != Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                solidInputPortStatusHandle = selectable.AddStatusItem(GetSolidInputPortStatusItem(), this);
            }
        }

        private void RemoveSolidInputPortStatus()
        {
            if (solidInputPortStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(solidInputPortStatusHandle);
            }

            solidInputPortStatusHandle = Guid.Empty;
        }

        private static StatusItem GetSolidInputPortStatusItem()
        {
            if (solidInputPortStatusItem != null)
            {
                return solidInputPortStatusItem;
            }

            solidInputPortStatusItem = new StatusItem(
                "StorageNetworkSolidInputPort",
                Loc.Get(Loc.UI.STORAGE_NETWORK.SOLID_INPUT_PORT_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.SOLID_INPUT_PORT_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            solidInputPortStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkSolidInputPortIngress ingress = data as StorageNetworkSolidInputPortIngress;
                return ingress != null ? ingress.GetStatusText() : text;
            };
            solidInputPortStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkSolidInputPortIngress ingress = data as StorageNetworkSolidInputPortIngress;
                return ingress != null ? ingress.GetStatusText() : tooltip;
            };

            return solidInputPortStatusItem;
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
                Loc.Get(Loc.UI.STORAGE_NETWORK.SOLID_INPUT_PORT_STATUS_ITEM),
                GetCurrentStatusText())) + "\n" + string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.SOLID_INPUT_PORT_STATUS_TOOLTIP),
                ColorizeEnabled(InputStoreEnabled),
                ColorizeNetwork(StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId())),
                ColorizeInfo(GetInputStoreModeStatusText()),
                ColorizeAmount(GameUtil.GetFormattedMass(storage != null ? storage.MassStored() : 0f)),
                ColorizeAmount(GameUtil.GetFormattedMass(storage != null ? storage.Capacity() : 0f)),
                ColorizeStatus(GetCurrentStatusText()));
        }

        private string GetInputStoreModeStatusText()
        {
            if (CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = ResolveInputStorage();
                if (target == null)
                {
                    return Loc.Get(Loc.UI.STORAGE_NETWORK.INPUT_TARGET_NOT_FOUND);
                }

                return StorageTargetSelector.IsStorageReachableFromWorld(target, GetWorldId())
                    ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.INPUT_TARGET_UNREACHABLE);
            }

            return Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private string GetCurrentStatusText()
        {
            if (!InputStoreEnabled)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED);
            }

            if (!StorageSceneRegistry.HasOnlineCoreInWorld(GetWorldId()))
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE);
            }

            return string.IsNullOrEmpty(lastStatus) ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED) : lastStatus;
        }

        private string FormatInputStoreStatus(StorageTransferResult result)
        {
            if (CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork &&
                result.MovedKg <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                !result.NetworkOffline &&
                !string.IsNullOrEmpty(result.BlockedItem) &&
                HasReservedAutoTargetCandidate())
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.TRANSFER_STATUS_RESERVED_TARGETS);
            }

            return NetworkStorageTransferService.FormatOutputStatus(result, Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS));
        }

        private bool HasReservedAutoTargetCandidate()
        {
            if (storage == null || storage.items == null)
            {
                return false;
            }

            int sourceWorldId = StorageTargetSelector.GetObjectWorldId(storage.gameObject);
            foreach (GameObject item in storage.items)
            {
                if (item != null &&
                    StorageNetworkInputTargetReservationService.HasReservedAutoOutputCandidate(item, storage, sourceWorldId))
                {
                    return true;
                }
            }

            return false;
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

        private static string ColorizeStatus(string text)
        {
            bool warning = text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.TRANSFER_STATUS_RESERVED_TARGETS)) ||
                text.Contains(Loc.Get(Loc.UI.STORAGE_NETWORK.CORE_OFFLINE_TITLE));
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
    }
}
