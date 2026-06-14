using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkCategorySummarySignature
    {
        public static string Build(string categoryKey, List<Storage> storages, List<StorageNetworkCategorySummaryItemTotal> totals)
        {
            string storageSignature = string.Join(",", storages
                .OrderBy(storage => storage != null ? storage.GetInstanceID() : 0)
                .Select(storage => string.Format("{0}:{1:0.###}",
                    storage != null ? storage.GetInstanceID() : 0,
                    storage != null ? storage.MassStored() : 0f)));

            string totalSignature = string.Join(",", totals
                .OrderBy(total => total.Key)
                .Select(total => string.Format("{0}:{1:0.###}", total.Key, total.MassKg)));

            return string.Format("{0}|{1}|{2}", categoryKey ?? string.Empty, storageSignature, totalSignature);
        }
    }
}
