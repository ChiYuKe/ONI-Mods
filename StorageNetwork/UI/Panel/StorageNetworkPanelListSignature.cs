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
            Func<GameObject, string> getStoredItemKey,
            Func<StorageInfo, bool> isOfflineNetworkServer)
        {
            string searchSignature = searchText ?? string.Empty;
            return searchSignature + "|" + string.Join("|", storages
                .OrderBy(getStorageTypeKey)
                .ThenBy(storage => storage.GameObject != null ? storage.GameObject.GetInstanceID() : 0)
                .Select(storage =>
                {
                    IEnumerable<GameObject> storedItems = storage.StoredItems ?? Enumerable.Empty<GameObject>();
                    string items = string.Join(",", storedItems
                        .GroupBy(getStoredItemKey)
                        .OrderBy(group => group.Key)
                        .Select(group => group.Key));

                    return string.Format("{0}:{1}:{2}",
                        getStorageTypeKey(storage),
                        storage.GameObject != null ? storage.GameObject.GetInstanceID() : 0,
                        items + ":" + (isOfflineNetworkServer(storage) ? "offline" : "online"));
                }));
        }
    }
}
