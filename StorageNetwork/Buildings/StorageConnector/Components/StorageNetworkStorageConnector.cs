using KSerialization;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 储存建筑内容物自动入网组件。用于普通 Storage，把内部物品转移到网络中的匹配箱子。
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkStorageConnector : KMonoBehaviour, ISim1000ms
    {
        private const float EmptyRetrySeconds = 5f;

        [Serialize]
        public bool OutputStoreEnabled;

        [Serialize]
        public int OutputStoreModeValue;

        [Serialize]
        public int OutputStorageInstanceId = KPrefabID.InvalidInstanceID;

        private Storage storage;
        private string lastOutputStatus;
        private float outputRetryTimer;

        public string LastOutputStatus => lastOutputStatus;

        public StorageNetworkMaterialRequester.OutputStoreMode CurrentOutputStoreMode
        {
            get => (StorageNetworkMaterialRequester.OutputStoreMode)Mathf.Clamp(OutputStoreModeValue, 0, 1);
            set => OutputStoreModeValue = (int)value;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureStorage();
            if (StorageNetworkStorageRules.IsServerStorage(storage))
            {
                OutputStoreEnabled = false;
                lastOutputStatus = string.Empty;
                return;
            }

            OutputStoreEnabled = IsOutputStoreEnabled();
            ApplyServerStorageModifiers();
            StorageSceneRegistry.Register(gameObject);
        }

        protected override void OnCleanUp()
        {
            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            EnsureStorage();
            if (storage == null || StorageNetworkStorageRules.IsServerStorage(storage) || !IsOutputStoreEnabled())
            {
                lastOutputStatus = string.Empty;
                outputRetryTimer = 0f;
                return;
            }

            if (outputRetryTimer > 0f)
            {
                outputRetryTimer -= dt;
                return;
            }

            if (storage.items == null || storage.items.Count == 0)
            {
                lastOutputStatus = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS);
                outputRetryTimer = EmptyRetrySeconds;
                return;
            }

            StorageTransferResult result = NetworkStorageTransferService.TransferStoredItemsToNetwork(
                storage,
                new[] { storage },
                GetSpecificOutputTarget());
            lastOutputStatus = NetworkStorageTransferService.FormatOutputStatus(result, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS));
            outputRetryTimer = result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? 0f : EmptyRetrySeconds;
        }

        public bool IsOutputStoreEnabled()
        {
            EnsureStorage();
            if (StorageNetworkStorageRules.IsServerStorage(storage))
            {
                return false;
            }

            return OutputStoreEnabled ||
                   Config.Instance.IsStorageOutputStoreToNetworkEnabled(storage);
        }

        public void SetOutputStoreEnabled(bool enabled)
        {
            EnsureStorage();
            if (StorageNetworkStorageRules.IsServerStorage(storage))
            {
                OutputStoreEnabled = false;
                return;
            }

            OutputStoreEnabled = enabled;
            Config.Instance.SetStorageOutputStoreToNetworkEnabled(storage, enabled);
            Config.Save();
        }

        public Storage ResolveOutputStorage()
        {
            if (OutputStorageInstanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage candidate = info?.Storage;
                if (StorageNetworkStorageRules.IsServerStorage(candidate) &&
                    StorageNetworkStorageRules.IsStorageCompatibleWithFilters(candidate, storage?.storageFilters) &&
                    GetStorageInstanceId(candidate) == OutputStorageInstanceId)
                {
                    return candidate;
                }
            }

            return null;
        }

        public void SetOutputStorage(Storage target)
        {
            OutputStorageInstanceId = GetStorageInstanceId(target);
            CurrentOutputStoreMode = StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage;
        }

        public void UseAutomaticOutputStorage()
        {
            CurrentOutputStoreMode = StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork;
            OutputStorageInstanceId = KPrefabID.InvalidInstanceID;
        }

        private Storage GetSpecificOutputTarget()
        {
            return CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage
                ? ResolveOutputStorage()
                : null;
        }

        private static int GetStorageInstanceId(Storage candidate)
        {
            KPrefabID prefabId = candidate != null ? candidate.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        /// <summary>
        /// 缓存本建筑的 Storage，供入网逻辑和 UI 状态读取复用。
        /// </summary>
        private void EnsureStorage()
        {
            if (storage == null)
            {
                storage = GetComponent<Storage>();
            }
        }

        private void ApplyServerStorageModifiers()
        {
            if (storage == null)
            {
                return;
            }

            KPrefabID prefabId = GetComponent<KPrefabID>();
            string prefabTag = prefabId != null ? prefabId.PrefabTag.Name : string.Empty;
            if (prefabTag != StorageNetworkCoreConfig.ID)
            {
                storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
            }
        }

    }
}
