using System;
using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkSourceIndexService
    {
        private const float SourceIndexTtlSeconds = 2f;
        private const float HotContentRefreshSeconds = 0.25f;
        private static readonly Dictionary<SourceIndexKey, SourceIndexSnapshot> Snapshots = new Dictionary<SourceIndexKey, SourceIndexSnapshot>();
        private static readonly List<SourceIndexKey> ExpiredSnapshotKeys = new List<SourceIndexKey>();
        private static int cachedRegistryVersion = -1;
        private static int contentVersion;

        public static List<Storage> GetSourceStorages(
            int worldId,
            bool includeReachableWorlds,
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            Storage specificSource = null)
        {
            HashSet<Tag> tags = BuildTagSet(wantedTags);
            List<Storage> result = new List<Storage>();
            FillSourceStorages(
                worldId,
                includeReachableWorlds,
                tags,
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
            if (result == null)
            {
                return;
            }

            result.Clear();
            bool hasWantedTag = false;
            if (wantedTags != null)
            {
                foreach (Tag tag in wantedTags)
                {
                    if (tag != Tag.Invalid)
                    {
                        hasWantedTag = true;
                        break;
                    }
                }
            }

            if (worldId < 0 || !hasWantedTag)
            {
                return;
            }

            if (specificSource != null)
            {
                if (IsUsableSource(specificSource, wantedTags, excludedStorages, worldId))
                {
                    result.Add(specificSource);
                }

                return;
            }

            SourceIndexSnapshot snapshot = GetSnapshot(worldId, includeReachableWorlds, allowStaleContent);
            snapshot?.FillSources(wantedTags, excludedStorages, result);
        }

        public static void ResetRuntimeState()
        {
            Snapshots.Clear();
            cachedRegistryVersion = -1;
            contentVersion = 0;
        }

        public static void Invalidate()
        {
            unchecked
            {
                contentVersion++;
            }
        }

        private static SourceIndexSnapshot GetSnapshot(
            int worldId,
            bool includeReachableWorlds,
            bool allowStaleContent = false)
        {
            PruneSnapshots();
            SourceIndexKey key = new SourceIndexKey(worldId, includeReachableWorlds);
            if (Snapshots.TryGetValue(key, out SourceIndexSnapshot snapshot) &&
                (snapshot.ContentVersion == contentVersion &&
                 (snapshot.Frame == Time.frameCount || Time.unscaledTime - snapshot.CreatedAt <= SourceIndexTtlSeconds) ||
                 allowStaleContent && Time.unscaledTime - snapshot.CreatedAt <= HotContentRefreshSeconds))
            {
                return snapshot;
            }

            snapshot = BuildSnapshot(worldId, includeReachableWorlds, snapshot);
            snapshot.ContentVersion = contentVersion;
            Snapshots[key] = snapshot;
            return snapshot;
        }

        private static SourceIndexSnapshot BuildSnapshot(
            int worldId,
            bool includeReachableWorlds,
            SourceIndexSnapshot snapshot)
        {
            snapshot = snapshot ?? new SourceIndexSnapshot();
            snapshot.Reset();
            StorageSceneLightweightSnapshot sceneSnapshot = StorageSceneCollector.CollectLightweightForWorld(worldId, includeReachableWorlds);
            foreach (Storage storage in sceneSnapshot.Storages)
            {
                if (!StorageNetworkStorageRules.IsServerStorage(storage) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(storage) ||
                    StorageNetworkStorageRules.IsMinionStorage(storage) ||
                    StorageNetworkStorageRules.IsProductionStorage(storage) ||
                    storage.items == null)
                {
                    continue;
                }

                Dictionary<Tag, float> amounts = BuildStorageAmounts(storage);
                foreach (KeyValuePair<Tag, float> pair in amounts)
                {
                    if (pair.Key == Tag.Invalid || pair.Value <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    snapshot.AddSource(pair.Key, storage, pair.Value);
                }
            }

            snapshot.Complete();
            return snapshot;
        }

        private static Dictionary<Tag, float> BuildStorageAmounts(Storage storage)
        {
            Dictionary<Tag, float> amounts = new Dictionary<Tag, float>();
            foreach (GameObject item in storage.items)
            {
                if (item == null)
                {
                    continue;
                }

                Pickupable pickupable = item.GetComponent<Pickupable>();
                float amount = pickupable != null ? pickupable.TotalAmount : StorageItemUtility.GetMass(item);
                if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                foreach (Tag tag in StorageItemUtility.GetStorageMatchTags(item))
                {
                    AddAmount(amounts, tag, amount);
                }

                PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                Element element = primaryElement != null
                    ? ElementLoader.FindElementByHash(primaryElement.ElementID)
                    : null;
                if (element != null)
                {
                    AddAmount(
                        amounts,
                        element.IsLiquid ? GameTags.Liquid : element.IsGas ? GameTags.Gas : GameTags.Solid,
                        amount);
                }
            }

            return amounts;
        }

        private static void AddAmount(Dictionary<Tag, float> amounts, Tag tag, float amount)
        {
            if (tag == Tag.Invalid)
            {
                return;
            }

            if (amounts.TryGetValue(tag, out float existing))
            {
                amounts[tag] = existing + amount;
            }
            else
            {
                amounts[tag] = amount;
            }
        }

        private static bool IsUsableSource(Storage source, IEnumerable<Tag> tags, HashSet<Storage> excludedStorages, int destinationWorldId)
        {
            return StorageSceneRegistry.IsLive(source) &&
                   IsStorageReachableFromWorld(source, destinationWorldId) &&
                   (excludedStorages == null || !excludedStorages.Contains(source)) &&
                   StorageNetworkStorageRules.IsServerStorage(source) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(source) &&
                   !StorageNetworkStorageRules.IsMinionStorage(source) &&
                   !StorageNetworkStorageRules.IsProductionStorage(source) &&
                   GetAmountAvailableByAnyTag(source, tags) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static bool IsStorageReachableFromWorld(Storage storage, int worldId)
        {
            if (!StorageSceneRegistry.IsLive(storage))
            {
                return false;
            }

            if (worldId < 0 || StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                return true;
            }

            return StorageTargetSelector.GetObjectWorldId(storage.gameObject) == worldId;
        }

        private static float GetAmountAvailableByAnyTag(Storage storage, IEnumerable<Tag> tags)
        {
            float amount = 0f;
            foreach (Tag tag in tags)
            {
                amount = Mathf.Max(amount, GetAmountAvailable(storage, tag));
            }

            return amount;
        }

        private static float GetAmountAvailable(Storage storage, Tag tag)
        {
            if (storage?.items == null || tag == Tag.Invalid)
            {
                return 0f;
            }

            if (tag != GameTags.Liquid && tag != GameTags.Gas && tag != GameTags.Solid)
            {
                return storage.GetAmountAvailable(tag);
            }

            float amount = 0f;
            foreach (GameObject item in storage.items)
            {
                PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                Element element = primaryElement != null
                    ? ElementLoader.FindElementByHash(primaryElement.ElementID)
                    : null;
                if (element == null ||
                    tag == GameTags.Liquid && !element.IsLiquid ||
                    tag == GameTags.Gas && !element.IsGas ||
                    tag == GameTags.Solid && (element.IsLiquid || element.IsGas))
                {
                    continue;
                }

                Pickupable pickupable = item.GetComponent<Pickupable>();
                amount += pickupable != null ? pickupable.TotalAmount : StorageItemUtility.GetMass(item);
            }

            return amount;
        }

        private static HashSet<Tag> BuildTagSet(IEnumerable<Tag> tags)
        {
            HashSet<Tag> result = new HashSet<Tag>();
            if (tags == null)
            {
                return result;
            }

            foreach (Tag tag in tags)
            {
                if (tag != Tag.Invalid)
                {
                    result.Add(tag);
                }
            }

            return result;
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

            float cutoff = Time.unscaledTime - SourceIndexTtlSeconds;
            ExpiredSnapshotKeys.Clear();
            foreach (KeyValuePair<SourceIndexKey, SourceIndexSnapshot> pair in Snapshots)
            {
                if (pair.Value.CreatedAt < cutoff)
                {
                    ExpiredSnapshotKeys.Add(pair.Key);
                }
            }

            foreach (SourceIndexKey key in ExpiredSnapshotKeys)
            {
                Snapshots.Remove(key);
            }

            ExpiredSnapshotKeys.Clear();
        }

        private readonly struct SourceIndexKey : IEquatable<SourceIndexKey>
        {
            private readonly int worldId;
            private readonly bool includeReachableWorlds;

            public SourceIndexKey(int worldId, bool includeReachableWorlds)
            {
                this.worldId = worldId;
                this.includeReachableWorlds = includeReachableWorlds;
            }

            public bool Equals(SourceIndexKey other)
            {
                return worldId == other.worldId && includeReachableWorlds == other.includeReachableWorlds;
            }

            public override bool Equals(object obj)
            {
                return obj is SourceIndexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (worldId * 397) ^ includeReachableWorlds.GetHashCode();
                }
            }
        }

        private readonly struct StorageAmount
        {
            public StorageAmount(Storage storage, float amount)
            {
                Storage = storage;
                Amount = amount;
            }

            public Storage Storage { get; }

            public float Amount { get; }
        }

        private sealed class SourceIndexSnapshot
        {
            private readonly Dictionary<Tag, List<StorageAmount>> sourcesByTag = new Dictionary<Tag, List<StorageAmount>>();
            private readonly Dictionary<Storage, float> bestAmounts = new Dictionary<Storage, float>();
            private readonly List<KeyValuePair<Storage, float>> orderedSources = new List<KeyValuePair<Storage, float>>();

            public int ContentVersion { get; set; }

            public int Frame { get; private set; }

            public float CreatedAt { get; private set; }

            public void Reset()
            {
                foreach (List<StorageAmount> sources in sourcesByTag.Values)
                {
                    sources.Clear();
                }

                bestAmounts.Clear();
                orderedSources.Clear();
                Frame = Time.frameCount;
                CreatedAt = Time.unscaledTime;
            }

            public void AddSource(Tag tag, Storage storage, float amount)
            {
                if (!sourcesByTag.TryGetValue(tag, out List<StorageAmount> sources))
                {
                    sources = new List<StorageAmount>();
                    sourcesByTag[tag] = sources;
                }

                sources.Add(new StorageAmount(storage, amount));
            }

            public void Complete()
            {
                foreach (List<StorageAmount> sources in sourcesByTag.Values)
                {
                    sources.Sort((left, right) => right.Amount.CompareTo(left.Amount));
                }
            }

            public void FillSources(IEnumerable<Tag> tags, HashSet<Storage> excludedStorages, List<Storage> result)
            {
                if (tags == null || result == null)
                {
                    return;
                }

                bestAmounts.Clear();
                foreach (Tag tag in tags)
                {
                    if (!sourcesByTag.TryGetValue(tag, out List<StorageAmount> sources))
                    {
                        continue;
                    }

                    foreach (StorageAmount source in sources)
                    {
                        Storage storage = source.Storage;
                        if (!StorageSceneRegistry.IsLive(storage) ||
                            excludedStorages != null && excludedStorages.Contains(storage))
                        {
                            continue;
                        }

                        if (bestAmounts.TryGetValue(storage, out float amount))
                        {
                            bestAmounts[storage] = Mathf.Max(amount, source.Amount);
                        }
                        else
                        {
                            bestAmounts[storage] = source.Amount;
                        }
                    }
                }

                orderedSources.Clear();
                foreach (KeyValuePair<Storage, float> pair in bestAmounts)
                {
                    orderedSources.Add(pair);
                }

                orderedSources.Sort((left, right) => right.Value.CompareTo(left.Value));
                foreach (KeyValuePair<Storage, float> pair in orderedSources)
                {
                    result.Add(pair.Key);
                }
            }
        }
    }
}
