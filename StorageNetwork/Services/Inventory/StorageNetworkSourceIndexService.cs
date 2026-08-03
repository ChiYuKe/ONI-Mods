using System.Collections.Generic;
using StorageNetwork.Core;

namespace StorageNetwork.Services
{
    /// <summary>
    /// Compatibility facade over the shared event-driven content index.
    /// </summary>
    internal static class StorageNetworkSourceIndexService
    {
        public static List<Storage> GetSourceStorages(
            int worldId,
            bool includeReachableWorlds,
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            Storage specificSource = null)
        {
            List<Storage> result = new List<Storage>();
            FillSourceStorages(
                worldId,
                includeReachableWorlds,
                wantedTags,
                excludedStorages,
                specificSource,
                result);
            return result;
        }

        public static void FillSourceStorages(
            int worldId,
            bool includeReachableWorlds,
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            Storage specificSource,
            List<Storage> result,
            bool allowStaleContent = false)
        {
            StorageNetworkPerformanceCounters.RecordNetworkSourceScan();
            using (StorageNetworkFrameProfileTool.BeginWork(
                StorageNetworkPerformanceArea.SourceSelection))
            {
                StorageNetworkContentIndexService.FillSourceStorages(
                    worldId,
                    includeReachableWorlds,
                    wantedTags,
                    excludedStorages,
                    specificSource,
                    result,
                    allowStaleContent);
            }
        }

        public static float GetStorageAmount(
            Storage storage,
            Tag tag,
            bool allowStaleContent = false)
        {
            return StorageNetworkContentIndexService.GetStorageAmount(
                storage,
                tag,
                allowStaleContent);
        }

        public static void ResetRuntimeState()
        {
            // Inventory and source facades share one runtime index. The lifecycle
            // currently resets both facades; resetting twice is intentionally safe.
            StorageNetworkContentIndexService.ResetRuntimeState();
        }

        public static void Invalidate()
        {
            StorageNetworkContentIndexService.InvalidateAll();
        }

        public static void Invalidate(Storage storage)
        {
            StorageNetworkContentIndexService.Invalidate(storage);
        }

        public static void Invalidate(Storage first, Storage second)
        {
            StorageNetworkContentIndexService.Invalidate(first, second);
        }
    }
}
