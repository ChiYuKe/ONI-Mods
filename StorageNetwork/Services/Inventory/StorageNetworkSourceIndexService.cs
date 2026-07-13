using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkSourceIndexService
    {
        private const float SourceIndexTtlSeconds = 2f;
        private static readonly Dictionary<SourceIndexKey, SourceIndexSnapshot> Snapshots = new Dictionary<SourceIndexKey, SourceIndexSnapshot>();
        private static readonly List<SourceIndexKey> ExpiredSnapshotKeys = new List<SourceIndexKey>();
        private static int cachedRegistryVersion = -1;

        public static List<Storage> GetSourceStorages(
            int worldId,
            bool includeReachableWorlds,
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            Storage specificSource = null)
        {
            StorageNetworkPerformanceCounters.RecordNetworkSourceScan();
            HashSet<Tag> tags = BuildTagSet(wantedTags);
            if (worldId < 0 || tags.Count == 0)
            {
                return new List<Storage>();
            }

            if (specificSource != null)
            {
                return IsUsableSource(specificSource, tags, excludedStorages, worldId)
                    ? new List<Storage> { specificSource }
                    : new List<Storage>();
            }

            SourceIndexSnapshot snapshot = GetSnapshot(worldId, includeReachableWorlds);
            return snapshot != null
                ? snapshot.GetSources(tags, excludedStorages)
                : new List<Storage>();
        }

        public static void ResetRuntimeState()
        {
            Snapshots.Clear();
            cachedRegistryVersion = -1;
        }

        private static SourceIndexSnapshot GetSnapshot(int worldId, bool includeReachableWorlds)
        {
            PruneSnapshots();
            SourceIndexKey key = new SourceIndexKey(worldId, includeReachableWorlds);
            if (Snapshots.TryGetValue(key, out SourceIndexSnapshot snapshot) &&
                (snapshot.Frame == Time.frameCount || Time.unscaledTime - snapshot.CreatedAt <= SourceIndexTtlSeconds))
            {
                return snapshot;
            }

            snapshot = BuildSnapshot(worldId, includeReachableWorlds);
            Snapshots[key] = snapshot;
            return snapshot;
        }

        private static SourceIndexSnapshot BuildSnapshot(int worldId, bool includeReachableWorlds)
        {
            Dictionary<Tag, List<StorageAmount>> sourcesByTag = new Dictionary<Tag, List<StorageAmount>>();
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

                    if (!sourcesByTag.TryGetValue(pair.Key, out List<StorageAmount> sources))
                    {
                        sources = new List<StorageAmount>();
                        sourcesByTag[pair.Key] = sources;
                    }

                    sources.Add(new StorageAmount(storage, pair.Value));
                }
            }

            foreach (List<StorageAmount> sources in sourcesByTag.Values)
            {
                sources.Sort((left, right) => right.Amount.CompareTo(left.Amount));
            }

            return new SourceIndexSnapshot(sourcesByTag, Time.frameCount, Time.unscaledTime);
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
            private readonly Dictionary<Tag, List<StorageAmount>> sourcesByTag;

            public SourceIndexSnapshot(Dictionary<Tag, List<StorageAmount>> sourcesByTag, int frame, float createdAt)
            {
                this.sourcesByTag = sourcesByTag;
                Frame = frame;
                CreatedAt = createdAt;
            }

            public int Frame { get; }

            public float CreatedAt { get; }

            public List<Storage> GetSources(IEnumerable<Tag> tags, HashSet<Storage> excludedStorages)
            {
                Dictionary<Storage, float> bestAmounts = new Dictionary<Storage, float>();
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

                List<KeyValuePair<Storage, float>> ordered = bestAmounts.ToList();
                ordered.Sort((left, right) => right.Value.CompareTo(left.Value));
                return ordered.Select(pair => pair.Key).ToList();
            }
        }
    }
}
