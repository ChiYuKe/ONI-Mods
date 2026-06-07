using StorageNetwork.Components;
using StorageNetwork.Core;

namespace StorageNetwork.Gameplay
{
    internal static class StorageNetworkStorageConnectorResolver
    {
        public static StorageNetworkStorageConnector GetOrCreateForSettingsStorage(Storage storage)
        {
            if (storage == null || !StorageNetworkStorageRules.HasSettingsButtonTag(storage))
            {
                return null;
            }

            return storage.GetComponent<StorageNetworkStorageConnector>() ??
                   storage.gameObject.AddOrGet<StorageNetworkStorageConnector>();
        }
    }
}
