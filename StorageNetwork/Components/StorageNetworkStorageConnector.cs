using KSerialization;
using StorageNetwork.Services;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 储存建筑内容物自动入网组件。用于普通 Storage，把内部物品转移到网络中的匹配箱子。
    /// </summary>
    public sealed class StorageNetworkStorageConnector : KMonoBehaviour, ISim1000ms
    {
        [Serialize]
        public bool OutputStoreEnabled;

        private Storage storage;
        private string lastOutputStatus;

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
                return;
            }

            StorageTransferResult result = NetworkStorageTransferService.TransferStoredItemsToNetwork(
                storage,
                new[] { storage });
            lastOutputStatus = NetworkStorageTransferService.FormatOutputStatus(result, "等待内容物进入储存栏");
        }

        private void EnsureStorage()
        {
            if (storage == null)
            {
                storage = GetComponent<Storage>();
            }
        }

    }
}
