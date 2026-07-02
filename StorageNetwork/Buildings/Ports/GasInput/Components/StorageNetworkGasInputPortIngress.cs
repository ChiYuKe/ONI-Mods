using KSerialization;
using System;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkGasInputPortIngress : KMonoBehaviour, ISim1000ms
    {
        private const float EmptyRetrySeconds = 2f;

        [Serialize]
        public bool InputStoreEnabled = true;

        [Serialize]
        public int InputStoreModeValue;

        [Serialize]
        public int InputStorageInstanceId = KPrefabID.InvalidInstanceID;

        [MyCmpGet]
        private Storage storage = null;

        private static StatusItem gasInputPortStatusItem;
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkGasInputPortIngress> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkGasInputPortIngress>((component, data) => component.OnCopySettings(data));

        private Guid gasInputPortStatusHandle = Guid.Empty;
        private float retryTimer;
        private string lastStatus;
        private string cachedStatusText;

        public StorageNetworkMaterialRequester.OutputStoreMode CurrentInputStoreMode
        {
            get => (StorageNetworkMaterialRequester.OutputStoreMode)Mathf.Clamp(InputStoreModeValue, 0, 1);
            set => InputStoreModeValue = (int)value;
        }

        public string LastStatus => lastStatus;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            storage?.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
            RefreshGasInputPortStatus();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnCleanUp()
        {
            RemoveGasInputPortStatus();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            RefreshGasInputPortStatus();
            if (storage == null || !InputStoreEnabled)
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
                specificTarget: CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage
                    ? ResolveInputStorage()
                    : null);

            lastStatus = FormatInputStoreStatus(result);
            retryTimer = result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? 0f : EmptyRetrySeconds;
            UpdateCachedStatusText();
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
            StorageNetworkGasInputPortIngress source = sourceObject != null
                ? sourceObject.GetComponent<StorageNetworkGasInputPortIngress>()
                : null;
            if (source == null || source == this)
            {
                return;
            }

            InputStoreEnabled = source.InputStoreEnabled;
            InputStoreModeValue = source.InputStoreModeValue;
            InputStorageInstanceId = source.InputStorageInstanceId;
            StorageNetworkFilterCopyHelper.CopyFilters(gameObject, sourceObject);
            retryTimer = 0f;
            lastStatus = string.Empty;
            cachedStatusText = null;
        }

        private void RefreshGasInputPortStatus()
        {
            if (gasInputPortStatusHandle != Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                gasInputPortStatusHandle = selectable.AddStatusItem(GetGasInputPortStatusItem(), this);
            }
        }

        private void RemoveGasInputPortStatus()
        {
            if (gasInputPortStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(gasInputPortStatusHandle);
            }

            gasInputPortStatusHandle = Guid.Empty;
        }

        private static StatusItem GetGasInputPortStatusItem()
        {
            if (gasInputPortStatusItem != null)
            {
                return gasInputPortStatusItem;
            }

            gasInputPortStatusItem = new StatusItem(
                "StorageNetworkGasInputPort",
                Loc.Get(Loc.UI.STORAGE_NETWORK.GAS_INPUT_PORT_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.GAS_INPUT_PORT_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            gasInputPortStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkGasInputPortIngress ingress = data as StorageNetworkGasInputPortIngress;
                return ingress != null ? ingress.GetStatusText() : text;
            };
            gasInputPortStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkGasInputPortIngress ingress = data as StorageNetworkGasInputPortIngress;
                return ingress != null ? ingress.GetStatusText() : tooltip;
            };

            return gasInputPortStatusItem;
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
                Loc.Get(Loc.UI.STORAGE_NETWORK.GAS_INPUT_PORT_STATUS_ITEM),
                GetCurrentStatusText())) + "\n" + string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GAS_INPUT_PORT_STATUS_TOOLTIP),
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

            return string.IsNullOrEmpty(lastStatus)
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED)
                : lastStatus;
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

            return NetworkStorageTransferService.FormatOutputStatus(
                result,
                Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS));
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
