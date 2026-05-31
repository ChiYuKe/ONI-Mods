using KSerialization;
using StorageNetwork.Services;
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

        private Storage storage;
        private string lastOutputStatus;
        private float outputRetryTimer;

        public string LastOutputStatus => lastOutputStatus;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureStorage();
        }

        public void Sim1000ms(float dt)
        {
            EnsureStorage();
            if (!OutputStoreEnabled || storage == null)
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
                new[] { storage });
            lastOutputStatus = NetworkStorageTransferService.FormatOutputStatus(result, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS));
            outputRetryTimer = result.MovedKg > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? 0f : EmptyRetrySeconds;
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

    }
}
