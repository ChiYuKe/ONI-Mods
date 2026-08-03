using System.Collections.Generic;

namespace StorageNetwork.Services
{
    /// <summary>
    /// Compatibility facade over the shared event-driven content index.
    /// </summary>
    internal static class StorageNetworkInventoryIndexService
    {
        public static float GetAmount(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag[] forbiddenTags = null,
            bool allowStaleContent = false)
        {
            return StorageNetworkContentIndexService.GetAmount(
                worldId,
                includeRelatedWorlds,
                tag,
                forbiddenTags,
                allowStaleContent);
        }

        public static float GetMass(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag[] forbiddenTags = null,
            bool allowStaleContent = false)
        {
            return GetAmount(
                worldId,
                includeRelatedWorlds,
                tag,
                forbiddenTags,
                allowStaleContent);
        }

        public static StorageNetworkInventoryMetrics GetMetrics(
            int worldId,
            bool includeRelatedWorlds,
            bool allowStaleContent = false)
        {
            return StorageNetworkContentIndexService.GetMetrics(
                worldId,
                includeRelatedWorlds,
                allowStaleContent);
        }

        public static bool HasAnyAmount(
            int worldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> tags)
        {
            return StorageNetworkContentIndexService.HasAnyAmount(
                worldId,
                includeRelatedWorlds,
                tags);
        }

        public static int GetCountWithAdditionalTag(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag additionalTag)
        {
            return StorageNetworkContentIndexService.GetCountWithAdditionalTag(
                worldId,
                includeRelatedWorlds,
                tag,
                additionalTag);
        }

        public static bool HasPlantableSeed(
            int worldId,
            bool includeRelatedWorlds,
            Tag seedTag,
            Tag additionalTag)
        {
            return StorageNetworkContentIndexService.HasPlantableSeed(
                worldId,
                includeRelatedWorlds,
                seedTag,
                additionalTag);
        }

        public static float GetEdibleCalories(
            int worldId,
            bool includeRelatedWorlds,
            Dictionary<string, float> unitsById = null)
        {
            return StorageNetworkContentIndexService.GetEdibleCalories(
                worldId,
                includeRelatedWorlds,
                null,
                unitsById);
        }

        public static float GetEdibleCaloriesForId(
            int worldId,
            bool includeRelatedWorlds,
            string foodId)
        {
            return string.IsNullOrEmpty(foodId)
                ? 0f
                : StorageNetworkContentIndexService.GetEdibleCalories(
                    worldId,
                    includeRelatedWorlds,
                    foodId,
                    null);
        }

        public static void FillAmounts(
            int worldId,
            bool includeRelatedWorlds,
            Dictionary<Tag, float> destination,
            bool allowStaleContent = true)
        {
            StorageNetworkContentIndexService.FillAmounts(
                worldId,
                includeRelatedWorlds,
                destination,
                allowStaleContent);
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

        public static void ResetRuntimeState()
        {
            StorageNetworkContentIndexService.ResetRuntimeState();
        }
    }

    internal readonly struct StorageNetworkInventoryMetrics
    {
        public StorageNetworkInventoryMetrics(
            bool networkOnline,
            float totalStoredKg,
            float totalCapacityKg)
        {
            NetworkOnline = networkOnline;
            TotalStoredKg = totalStoredKg;
            TotalCapacityKg = totalCapacityKg;
        }

        public bool NetworkOnline { get; }
        public float TotalStoredKg { get; }
        public float TotalCapacityKg { get; }
    }
}
