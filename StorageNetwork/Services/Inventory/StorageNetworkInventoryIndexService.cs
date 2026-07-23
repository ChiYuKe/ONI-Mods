using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkInventoryIndexService
    {
        private const float InventoryIndexTtlSeconds = 5f;
        private const float HotContentRefreshSeconds = 0.25f;
        private static readonly Dictionary<InventoryIndexKey, InventoryIndexSnapshot> Snapshots = new Dictionary<InventoryIndexKey, InventoryIndexSnapshot>();
        private static readonly List<InventoryIndexKey> ExpiredSnapshotKeys = new List<InventoryIndexKey>();
        private static int cachedRegistryVersion = -1;
        private static int contentVersion;

        public static float GetAmount(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag[] forbiddenTags = null,
            bool allowStaleContent = false)
        {
            if (worldId < 0 || tag == Tag.Invalid)
            {
                return 0f;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds, allowStaleContent);
            return snapshot != null ? snapshot.GetAmount(tag, forbiddenTags) : 0f;
        }

        public static float GetMass(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag[] forbiddenTags = null,
            bool allowStaleContent = false)
        {
            if (worldId < 0 || tag == Tag.Invalid)
            {
                return 0f;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds, allowStaleContent);
            return snapshot != null ? snapshot.GetMass(tag, forbiddenTags) : 0f;
        }

        public static StorageNetworkInventoryMetrics GetMetrics(
            int worldId,
            bool includeRelatedWorlds,
            bool allowStaleContent = false)
        {
            if (worldId < 0)
            {
                return default;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds, allowStaleContent);
            return snapshot != null
                ? new StorageNetworkInventoryMetrics(
                    snapshot.NetworkOnline,
                    snapshot.TotalStoredKg,
                    snapshot.TotalCapacityKg)
                : default;
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

        public static int GetCountWithAdditionalTag(int worldId, bool includeRelatedWorlds, Tag tag, Tag additionalTag)
        {
            if (worldId < 0 || tag == Tag.Invalid)
            {
                return 0;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds);
            return snapshot != null ? snapshot.GetCountWithAdditionalTag(tag, additionalTag) : 0;
        }

        public static bool HasPlantableSeed(int worldId, bool includeRelatedWorlds, Tag seedTag, Tag additionalTag)
        {
            if (worldId < 0 || seedTag == Tag.Invalid)
            {
                return false;
            }

            InventoryIndexSnapshot snapshot = GetSnapshot(worldId, includeRelatedWorlds);
            return snapshot != null && snapshot.HasPlantableSeed(seedTag, additionalTag);
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
            unchecked
            {
                contentVersion++;
            }
        }

        public static void ResetRuntimeState()
        {
            Snapshots.Clear();
            cachedRegistryVersion = -1;
            contentVersion = 0;
        }

        private static InventoryIndexSnapshot GetSnapshot(
            int worldId,
            bool includeRelatedWorlds,
            bool allowStaleContent = false)
        {
            PruneSnapshots();
            InventoryIndexKey key = new InventoryIndexKey(worldId, includeRelatedWorlds);
            if (Snapshots.TryGetValue(key, out InventoryIndexSnapshot snapshot) &&
                (snapshot.ContentVersion == contentVersion &&
                 (snapshot.Frame == Time.frameCount || Time.unscaledTime - snapshot.CreatedAt <= InventoryIndexTtlSeconds) ||
                 allowStaleContent && Time.unscaledTime - snapshot.CreatedAt <= HotContentRefreshSeconds))
            {
                return snapshot;
            }

            snapshot = BuildSnapshot(worldId, includeRelatedWorlds, snapshot);
            snapshot.ContentVersion = contentVersion;
            Snapshots[key] = snapshot;
            StorageNetworkPerformanceCounters.RecordInventoryIndexRebuild();
            return snapshot;
        }

        private static InventoryIndexSnapshot BuildSnapshot(
            int worldId,
            bool includeRelatedWorlds,
            InventoryIndexSnapshot snapshot)
        {
            snapshot = snapshot ?? new InventoryIndexSnapshot();
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                snapshot.Reset(false, 0f, 0f);
                return snapshot;
            }

            snapshot.Reset(true, 0f, 0f);
            float totalStoredKg = 0f;
            float totalCapacityKg = 0f;
            StorageSceneLightweightSnapshot sceneSnapshot = StorageSceneCollector.CollectLightweightForWorld(worldId, includeRelatedWorlds);
            foreach (Storage storage in sceneSnapshot.Storages)
            {
                if (!StorageNetworkStorageRules.IsServerStorage(storage) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(storage) ||
                    storage.items == null)
                {
                    continue;
                }

                if (StorageNetworkStorageRules.CountsTowardNetworkCapacity(storage))
                {
                    totalStoredKg += storage.MassStored();
                    totalCapacityKg += storage.Capacity();
                }

                foreach (GameObject item in storage.items)
                {
                    InventoryIndexItem indexItem = CreateIndexItem(item);
                    if (indexItem.Amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    snapshot.AddItem(indexItem);
                    foreach (Tag itemTag in indexItem.Tags)
                    {
                        snapshot.AddAmount(itemTag, indexItem.Amount);
                    }
                }
            }

            snapshot.SetTotals(totalStoredKg, totalCapacityKg);
            return snapshot;
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
                unchecked
                {
                    contentVersion++;
                }
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
            private readonly Dictionary<Tag, float> amountsByTag = new Dictionary<Tag, float>();
            private readonly List<InventoryIndexItem> items = new List<InventoryIndexItem>();
            private readonly Dictionary<ForbiddenAmountKey, float> forbiddenAmountCache = new Dictionary<ForbiddenAmountKey, float>();

            public int ContentVersion { get; set; }

            public bool NetworkOnline { get; private set; }

            public float TotalStoredKg { get; private set; }

            public float TotalCapacityKg { get; private set; }

            public int Frame { get; private set; }

            public float CreatedAt { get; private set; }

            public void Reset(bool networkOnline, float totalStoredKg, float totalCapacityKg)
            {
                NetworkOnline = networkOnline;
                TotalStoredKg = totalStoredKg;
                TotalCapacityKg = totalCapacityKg;
                Frame = Time.frameCount;
                CreatedAt = Time.unscaledTime;
                amountsByTag.Clear();
                items.Clear();
                forbiddenAmountCache.Clear();
            }

            public void AddItem(InventoryIndexItem item)
            {
                items.Add(item);
            }

            public void AddAmount(Tag tag, float amount)
            {
                if (tag == Tag.Invalid)
                {
                    return;
                }

                if (amountsByTag.TryGetValue(tag, out float existing))
                {
                    amountsByTag[tag] = existing + amount;
                }
                else
                {
                    amountsByTag[tag] = amount;
                }
            }

            public void SetTotals(float totalStoredKg, float totalCapacityKg)
            {
                TotalStoredKg = totalStoredKg;
                TotalCapacityKg = totalCapacityKg;
            }

            public float GetMass(Tag tag, Tag[] forbiddenTags)
            {
                return GetAmount(tag, forbiddenTags);
            }

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

            public int GetCountWithAdditionalTag(Tag tag, Tag additionalTag)
            {
                int count = 0;
                foreach (InventoryIndexItem item in items)
                {
                    if (!item.Matches(tag) ||
                        (additionalTag.IsValid && !item.HasTag(additionalTag)))
                    {
                        continue;
                    }

                    count += Mathf.CeilToInt(item.Amount);
                }

                return count;
            }

            public bool HasPlantableSeed(Tag seedTag, Tag additionalTag)
            {
                foreach (InventoryIndexItem item in items)
                {
                    if (item.IsPlantableSeed &&
                        item.Matches(seedTag) &&
                        (!additionalTag.IsValid || item.HasTag(additionalTag)))
                    {
                        return true;
                    }
                }

                return false;
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
                IsPlantableSeed = prefabId != null && prefabId.GetComponent<PlantableSeed>() != null;
                FoodId = edible != null ? edible.FoodID : null;
                EdibleCalories = edible != null ? edible.Calories : 0f;
                EdibleUnits = edible != null ? edible.Units : 0f;
            }

            public IEnumerable<Tag> Tags => tags;

            public float Amount { get; }

            public bool IsPlantableSeed { get; }

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

            public bool HasTag(Tag tag)
            {
                return tag != Tag.Invalid && prefabId != null && prefabId.HasTag(tag);
            }
        }
    }

    internal readonly struct StorageNetworkInventoryMetrics
    {
        public StorageNetworkInventoryMetrics(bool networkOnline, float totalStoredKg, float totalCapacityKg)
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
