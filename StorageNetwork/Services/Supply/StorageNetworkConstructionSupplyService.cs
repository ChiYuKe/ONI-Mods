using System.Collections.Generic;
using StorageNetwork.API;
using UnityEngine;

namespace StorageNetwork.Services
{
    /// <summary>
    /// One-release compatibility boundary for construction/output-buffer tags
    /// written by older StorageNetwork builds.
    ///
    /// The former construction supplier had no callers but registered every
    /// Constructable and could reconcile the whole network.  Only the tag
    /// cleanup helpers remain.  Global Harmony patches must enter through the
    /// registered solid-output set or a Pickupable's cached KPrefabID so their
    /// ordinary path performs no component lookup or scene/network scan.
    /// </summary>
    internal static class StorageNetworkConstructionSupplyService
    {
        private const string LogPrefix = "[StorageNetwork][LegacyConstruction]";
        private static readonly HashSet<Storage> SolidOutputPortStorages = new HashSet<Storage>();

        private static bool nativeStorageCleanupCompleted;
        private static long legacyReservedSelectionHits;
        private static long legacyTransferTagClears;
        private static long legacyFetchTagClears;
        private static int cleanupRuns;
        private static int cleanupStoragesScanned;
        private static int cleanupItemsScanned;
        private static int cleanupReservationTagsCleared;
        private static int cleanupBufferTagsCleared;

        internal static StorageNetworkLegacyConstructionCounters RuntimeCounters =>
            new StorageNetworkLegacyConstructionCounters(
                legacyReservedSelectionHits,
                legacyTransferTagClears,
                legacyFetchTagClears,
                cleanupRuns,
                cleanupStoragesScanned,
                cleanupItemsScanned,
                cleanupReservationTagsCleared,
                cleanupBufferTagsCleared);

        public static void RegisterSolidOutputPort(Storage storage)
        {
            if (storage != null)
            {
                bool added = SolidOutputPortStorages.Add(storage);
                if (added && nativeStorageCleanupCompleted)
                {
                    // Covers a world/rocket interior activated after the one-time
                    // scene scan. This is local to the newly spawned port.
                    CleanupTagsInStorage(storage, countAsNativeScan: false);
                }
            }
        }

        public static void UnregisterSolidOutputPort(Storage storage)
        {
            if (storage != null)
            {
                SolidOutputPortStorages.Remove(storage);
            }
        }

        /// <summary>
        /// O(1) role guard for global patches.  Unity's destroyed-object check
        /// is deliberately omitted here; lifecycle registration owns pruning.
        /// </summary>
        public static bool IsRegisteredSolidOutputPort(Storage storage)
        {
            return storage != null && SolidOutputPortStorages.Contains(storage);
        }

        public static void Reset()
        {
            if (nativeStorageCleanupCompleted)
            {
                Debug.Log(
                    LogPrefix + " runtime counters: reserved-selection-hits=" +
                    legacyReservedSelectionHits +
                    ", transfer-tag-clears=" + legacyTransferTagClears +
                    ", fetch-tag-clears=" + legacyFetchTagClears + ".");
            }

            SolidOutputPortStorages.Clear();
            nativeStorageCleanupCompleted = false;
            legacyReservedSelectionHits = 0L;
            legacyTransferTagClears = 0L;
            legacyFetchTagClears = 0L;
            cleanupRuns = 0;
            cleanupStoragesScanned = 0;
            cleanupItemsScanned = 0;
            cleanupReservationTagsCleared = 0;
            cleanupBufferTagsCleared = 0;
        }

        /// <summary>
        /// Runs once after the save loader has materialized the scene.  This
        /// intentionally enumerates native Storage components and Storage.items
        /// instead of any StorageNetwork snapshot/index, so stale tags cannot
        /// be hidden by enrollment/filter state and no save schema is changed.
        /// </summary>
        public static void CleanupLegacyTagsFromNativeStorages()
        {
            if (nativeStorageCleanupCompleted)
            {
                return;
            }

            nativeStorageCleanupCompleted = true;
            cleanupRuns++;

            Storage[] storages = Object.FindObjectsByType<Storage>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            cleanupStoragesScanned = storages != null ? storages.Length : 0;
            if (storages != null)
            {
                foreach (Storage storage in storages)
                {
                    if (storage == null)
                    {
                        continue;
                    }

                    KPrefabID storagePrefabId = storage.GetComponent<KPrefabID>();
                    if (storagePrefabId != null &&
                        storagePrefabId.HasTag(StorageNetworkTags.CategorySolidOutputPort))
                    {
                        SolidOutputPortStorages.Add(storage);
                    }

                    if (storage.items == null)
                    {
                        continue;
                    }

                    CleanupTagsInStorage(storage, countAsNativeScan: true);
                }
            }

            Debug.Log(
                LogPrefix + " native-storage cleanup: storages=" + cleanupStoragesScanned +
                ", items=" + cleanupItemsScanned +
                ", reservation-tags-cleared=" + cleanupReservationTagsCleared +
                ", buffer-tags-cleared=" + cleanupBufferTagsCleared + ".");
        }

        public static void ClearSolidOutputBufferMarker(GameObject item)
        {
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            RemoveTagIfPresent(prefabId, StorageNetworkTags.SolidOutputPortBufferedItem);
        }

        public static bool IsConstructionReserved(GameObject item)
        {
            // The obsolete supplier is gone and the post-load migration removes
            // every persisted reservation. This keeps the solid-output request
            // loop allocation/component-lookup free after migration.
            if (nativeStorageCleanupCompleted)
            {
                return false;
            }

            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            return HasTag(prefabId, StorageNetworkTags.ReservedForConstruction);
        }

        /// <summary>
        /// Cached-component overload used from the global dispenser patch.
        /// </summary>
        public static bool IsConstructionReserved(Pickupable pickupable)
        {
            return pickupable != null &&
                   HasTag(pickupable.KPrefabID, StorageNetworkTags.ReservedForConstruction);
        }

        public static void RecordLegacyReservedSelection()
        {
            legacyReservedSelectionHits++;
        }

        /// <summary>
        /// Called only after the O(1) solid-output storage guard succeeds.
        /// Component lookup therefore never appears on the global miss path.
        /// </summary>
        public static void ClearLegacyTagsForSolidOutputTransfer(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (RemoveTagIfPresent(prefabId, StorageNetworkTags.ReservedForConstruction))
            {
                legacyTransferTagClears++;
            }

            if (RemoveTagIfPresent(prefabId, StorageNetworkTags.SolidOutputPortBufferedItem))
            {
                legacyTransferTagClears++;
            }
        }

        /// <summary>
        /// FetchChore supplies Pickupable directly, including its MyCmpReq
        /// KPrefabID.  The overwhelmingly common miss therefore needs only a
        /// null check and a tag lookup.
        /// </summary>
        public static void ClearBufferMarkerForFetch(Pickupable pickupable)
        {
            KPrefabID prefabId = pickupable != null ? pickupable.KPrefabID : null;
            if (RemoveTagIfPresent(prefabId, StorageNetworkTags.SolidOutputPortBufferedItem))
            {
                legacyFetchTagClears++;
            }
        }

        private static bool HasTag(KPrefabID prefabId, Tag tag)
        {
            return prefabId != null && prefabId.HasTag(tag);
        }

        private static void CleanupTagsInStorage(Storage storage, bool countAsNativeScan)
        {
            if (storage?.items == null)
            {
                return;
            }

            bool restoreLiveOutputMarker = SolidOutputPortStorages.Contains(storage);
            foreach (GameObject item in storage.items)
            {
                if (item == null)
                {
                    continue;
                }

                if (countAsNativeScan)
                {
                    cleanupItemsScanned++;
                }

                KPrefabID prefabId = item.GetComponent<KPrefabID>();
                if (RemoveTagIfPresent(prefabId, StorageNetworkTags.ReservedForConstruction))
                {
                    cleanupReservationTagsCleared++;
                }

                if (RemoveTagIfPresent(prefabId, StorageNetworkTags.SolidOutputPortBufferedItem))
                {
                    cleanupBufferTagsCleared++;
                }

                // The persisted marker is migration data, but items currently
                // buffered by a live output port still need a freshly-derived
                // marker to prevent an output->input rail loop.
                if (restoreLiveOutputMarker && prefabId != null)
                {
                    prefabId.AddTag(StorageNetworkTags.SolidOutputPortBufferedItem, true);
                }
            }
        }

        private static bool RemoveTagIfPresent(KPrefabID prefabId, Tag tag)
        {
            if (!HasTag(prefabId, tag))
            {
                return false;
            }

            prefabId.RemoveTag(tag);
            return true;
        }
    }

    internal readonly struct StorageNetworkLegacyConstructionCounters
    {
        public StorageNetworkLegacyConstructionCounters(
            long reservedSelectionHits,
            long transferTagClears,
            long fetchTagClears,
            int cleanupRuns,
            int cleanupStoragesScanned,
            int cleanupItemsScanned,
            int cleanupReservationTagsCleared,
            int cleanupBufferTagsCleared)
        {
            ReservedSelectionHits = reservedSelectionHits;
            TransferTagClears = transferTagClears;
            FetchTagClears = fetchTagClears;
            CleanupRuns = cleanupRuns;
            CleanupStoragesScanned = cleanupStoragesScanned;
            CleanupItemsScanned = cleanupItemsScanned;
            CleanupReservationTagsCleared = cleanupReservationTagsCleared;
            CleanupBufferTagsCleared = cleanupBufferTagsCleared;
        }

        public long ReservedSelectionHits { get; }
        public long TransferTagClears { get; }
        public long FetchTagClears { get; }
        public int CleanupRuns { get; }
        public int CleanupStoragesScanned { get; }
        public int CleanupItemsScanned { get; }
        public int CleanupReservationTagsCleared { get; }
        public int CleanupBufferTagsCleared { get; }
    }
}
