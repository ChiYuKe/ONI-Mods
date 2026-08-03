using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkPanelListSignature
    {
        internal static string BuildStorageListSignature(
            IEnumerable<StorageInfo> storages,
            string searchText,
            Func<StorageInfo, string> getStorageTypeKey,
            Func<GameObject, string> getStoredItemKey)
        {
            string searchSignature = searchText ?? string.Empty;
            return searchSignature + "|" + string.Join("|", storages
                .OrderBy(getStorageTypeKey)
                .ThenBy(storage => storage.GameObject != null ? storage.GameObject.GetInstanceID() : 0)
                .Select(storage => string.Format(
                    "{0}:{1}",
                    getStorageTypeKey(storage),
                    storage.GameObject != null ? storage.GameObject.GetInstanceID() : 0)));
        }
    }
}
