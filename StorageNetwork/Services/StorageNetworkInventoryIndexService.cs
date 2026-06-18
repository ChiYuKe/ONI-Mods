using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkInventoryIndexService
    {
        private const float InventoryIndexTtlSeconds = 2f;
        private static readonly Dictionary<InventoryIndexKey, InventoryIndexSnapshot> Snapshots = new Dictionary<InventoryIndexKey, InventoryIndexSnapshot>();
        private static readonly List<InventoryIndexKey> ExpiredSnapshotKeys = new List<InventoryIndexKey>();
        private static int cachedRegistryVersion = -1;

        public static float GetAmount(int worldId, bool includeRelatedWorlds, Tag tag, Tag[] forbiddenTags = null)
        {
            if (worldId < 0 || tag == Tag.Invalid)
            {
                return 0f;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds);
            return snapshot != null ? snapshot.GetAmount(tag, forbiddenTags) : 0f;
        }

        public static bool HasAnyAmount(int worldId, bool includeRelatedWorlds, IEnumerable<Tag> tags)
        {
            if (worldId < 0 || tags == null)
            {
                return false;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds);
            if (snapshot == null)
            {
                return false;
            }

            foreach (Tag tag in tags)
            {
                if (tag != Tag.Invalid && snapshot.GetAmount(tag, null) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return true;
                }
            }

            return false;
        }

        public static float GetEdibleCalories(int worldId, bool includeRelatedWorlds, Dictionary<string, float> unitsById = null)
        {
            if (worldId < 0)
            {
                return 0f;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds);
            return snapshot != null ? snapshot.GetEdibleCalories(null, unitsById) : 0f;
        }

        public static float GetEdibleCaloriesForId(int worldId, bool includeRelatedWorlds, string foodId)
        {
            if (worldId < 0 || string.IsNullOrEmpty(foodId))
            {
                return 0f;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds);
            return snapshot != null ? snapshot.GetEdibleCalories(foodId, null) : 0f;
        }

        public static void Invalidate()
        {
            Snapshots.Clear();
            cachedRegistryVersion = StorageSceneRegistry.Version;
        }

        public static void ResetRuntimeState()
        {
            Snapshots.Clear();
            cachedRegistryVersion = -1;
        }

        private static InventoryIndexSnapshot GetSnapshot(int worldId, bool includeRelatedWorlds)
        {
            PruneSnapshots();
            InventoryIndexKey key = new InventoryIndexKey(worldId, includeRelatedWorlds);
            if (Snapshots.TryGetValue(key, out InventoryIndexSnapshot snapshot) &&
                (snapshot.Frame == Time.frameCount || Time.unscaledTime - snapshot.CreatedAt <= InventoryIndexTtlSeconds))
            {
                return snapshot;
            }

            snapshot = BuildSnapshot(worldId, includeRelatedWorlds);
            Snapshots[key] = snapshot;
            StorageNetworkPerformanceCounters.RecordInventoryIndexRebuild();
            return snapshot;
        }

        private static InventoryIndexSnapshot BuildSnapshot(int worldId, bool includeRelatedWorlds)
        {
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return new InventoryIndexSnapshot(
                    new Dictionary<Tag, float>(),
                    new List<InventoryIndexItem>(),
                    Time.frameCount,
                    Time.unscaledTime);
            }

            Dictionary<Tag, float> amountsByTag = new Dictionary<Tag, float>();
            List<InventoryIndexItem> items = new List<InventoryIndexItem>();
            StorageSceneLightweightSnapshot sceneSnapshot = StorageSceneCollector.CollectLightweightForWorld(worldId, includeRelatedWorlds);
            foreach (Storage storage in sceneSnapshot.Storages)
            {
                if (!StorageNetworkStorageRules.IsServerStorage(storage) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(storage) ||
                    storage.items == null)
                {
                    continue;
                }

                foreach (GameObject item in storage.items)
                {
                    InventoryIndexItem indexItem = CreateIndexItem(item);
                    if (indexItem.Amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    items.Add(indexItem);
                    foreach (Tag itemTag in indexItem.Tags)
                    {
                        if (amountsByTag.TryGetValue(itemTag, out float amount))
                        {
                            amountsByTag[itemTag] = amount + indexItem.Amount;
                        }
                        else
                        {
                            amountsByTag[itemTag] = indexItem.Amount;
                        }
                    }
                }
            }

            return new InventoryIndexSnapshot(amountsByTag, items, Time.frameCount, Time.unscaledTime);
        }

        private static InventoryIndexItem CreateIndexItem(GameObject item)
        {
            HashSet<Tag> tags = StorageItemUtility.GetStorageMatchTags(item);
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;
            Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
            Edible edible = item != null ? item.GetComponent<Edible>() : null;
            float amount = pickupable != null ? pickupable.TotalAmount : StorageItemUtility.GetMass(item);
            return new InventoryIndexItem(tags, prefabId, amount, edible);
        }

        private static void PruneSnapshots()
        {
            int registryVersion = StorageSceneRegistry.Version;
            if (cachedRegistryVersion != registryVersion)
            {
                Snapshots.Clear();
                cachedRegistryVersion = registryVersion;
                return;
            }

            float cutoff = Time.unscaledTime - InventoryIndexTtlSeconds;
            ExpiredSnapshotKeys.Clear();
            foreach (KeyValuePair<InventoryIndexKey, InventoryIndexSnapshot> pair in Snapshots)
            {
                if (pair.Value.CreatedAt < cutoff)
                {
                    ExpiredSnapshotKeys.Add(pair.Key);
                }
            }

            foreach (InventoryIndexKey key in ExpiredSnapshotKeys)
            {
                Snapshots.Remove(key);
            }

            ExpiredSnapshotKeys.Clear();
        }

        private readonly struct InventoryIndexKey : IEquatable<InventoryIndexKey>
        {
            private readonly int worldId;
            private readonly bool includeRelatedWorlds;

            public InventoryIndexKey(int worldId, bool includeRelatedWorlds)
            {
                this.worldId = worldId;
                this.includeRelatedWorlds = includeRelatedWorlds;
            }

            public bool Equals(InventoryIndexKey other)
            {
                return worldId == other.worldId && includeRelatedWorlds == other.includeRelatedWorlds;
            }

            public override bool Equals(object obj)
            {
                return obj is InventoryIndexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (worldId * 397) ^ includeRelatedWorlds.GetHashCode();
                }
            }
        }

        private readonly struct ForbiddenAmountKey : IEquatable<ForbiddenAmountKey>
        {
            private readonly Tag tag;
            private readonly string forbiddenSignature;

            public ForbiddenAmountKey(Tag tag, string forbiddenSignature)
            {
                this.tag = tag;
                this.forbiddenSignature = forbiddenSignature ?? string.Empty;
            }

            public bool Equals(ForbiddenAmountKey other)
            {
                return tag == other.tag && forbiddenSignature == other.forbiddenSignature;
            }

            public override bool Equals(object obj)
            {
                return obj is ForbiddenAmountKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (tag.GetHashCode() * 397) ^ forbiddenSignature.GetHashCode();
                }
            }
        }

        private sealed class InventoryIndexSnapshot
        {
            private readonly Dictionary<Tag, float> amountsByTag;
            private readonly List<InventoryIndexItem> items;
            private readonly Dictionary<ForbiddenAmountKey, float> forbiddenAmountCache = new Dictionary<ForbiddenAmountKey, float>();

            public InventoryIndexSnapshot(Dictionary<Tag, float> amountsByTag, List<InventoryIndexItem> items, int frame, float createdAt)
            {
                this.amountsByTag = amountsByTag;
                this.items = items;
                Frame = frame;
                CreatedAt = createdAt;
            }

            public int Frame { get; }

            public float CreatedAt { get; }

            public float GetAmount(Tag tag, Tag[] forbiddenTags)
            {
                if (forbiddenTags == null || forbiddenTags.Length == 0)
                {
                    return amountsByTag.TryGetValue(tag, out float amount) ? amount : 0f;
                }

                ForbiddenAmountKey key = new ForbiddenAmountKey(tag, BuildForbiddenSignature(forbiddenTags));
                if (forbiddenAmountCache.TryGetValue(key, out float cachedAmount))
                {
                    return cachedAmount;
                }

                float amountWithFilter = 0f;
                foreach (InventoryIndexItem item in items)
                {
                    if (item.Matches(tag) && !item.HasAnyForbiddenTag(forbiddenTags))
                    {
                        amountWithFilter += item.Amount;
                    }
                }

                forbiddenAmountCache[key] = amountWithFilter;
                return amountWithFilter;
            }

            public float GetEdibleCalories(string foodId, Dictionary<string, float> unitsById)
            {
                float calories = 0f;
                foreach (InventoryIndexItem item in items)
                {
                    if (item.EdibleCalories <= 0f || string.IsNullOrEmpty(item.FoodId))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(foodId) && item.FoodId != foodId)
                    {
                        continue;
                    }

                    calories += item.EdibleCalories;
                    if (unitsById != null)
                    {
                        if (unitsById.ContainsKey(item.FoodId))
                        {
                            unitsById[item.FoodId] += item.EdibleUnits;
                        }
                        else
                        {
                            unitsById[item.FoodId] = item.EdibleUnits;
                        }
                    }
                }

                return calories;
            }

            private static string BuildForbiddenSignature(IEnumerable<Tag> forbiddenTags)
            {
                return string.Join(
                    "|",
                    forbiddenTags
                        .Where(tag => tag != Tag.Invalid)
                        .Select(tag => tag.Name)
                        .OrderBy(name => name));
            }
        }

        private readonly struct InventoryIndexItem
        {
            private readonly HashSet<Tag> tags;
            private readonly KPrefabID prefabId;

            public InventoryIndexItem(HashSet<Tag> tags, KPrefabID prefabId, float amount, Edible edible)
            {
                this.tags = tags ?? new HashSet<Tag>();
                this.prefabId = prefabId;
                Amount = amount;
                FoodId = edible != null ? edible.FoodID : null;
                EdibleCalories = edible != null ? edible.Calories : 0f;
                EdibleUnits = edible != null ? edible.Units : 0f;
            }

            public IEnumerable<Tag> Tags => tags;

            public float Amount { get; }

            public string FoodId { get; }

            public float EdibleCalories { get; }

            public float EdibleUnits { get; }

            public bool Matches(Tag tag)
            {
                return tags != null && tags.Contains(tag);
            }

            public bool HasAnyForbiddenTag(Tag[] forbiddenTags)
            {
                return prefabId != null && forbiddenTags != null && prefabId.HasAnyTags(forbiddenTags);
            }
        }
    }
}
