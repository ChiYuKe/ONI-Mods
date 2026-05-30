using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkFilterBypass
    {
        public static bool ShouldBypassUserFilter(Storage storage)
        {
            return StorageNetworkMembership.IsCollectableStorage(storage) &&
                   !StorageNetworkStorageRules.IsProductionStorage(storage);
        }

        public static void Apply(Storage storage)
        {
            if (!ShouldBypassUserFilter(storage))
            {
                return;
            }

            TreeFilterable filterable = storage.GetComponent<TreeFilterable>();
            if (filterable != null)
            {
                filterable.dropIncorrectOnFilterChange = false;
            }
        }
    }
}
