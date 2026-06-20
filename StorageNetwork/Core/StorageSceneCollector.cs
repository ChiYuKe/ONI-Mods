using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageSceneCollector
    {
        private static StorageSceneSnapshot cachedSnapshot;
        private static float cachedAtUnscaledTime = -1f;
        private static int cachedFrame = -1;
        private static int cachedRegistryVersion = -1;
        private static bool cachedNetworkOnline;
        private static readonly Dictionary<WorldSnapshotKey, WorldSnapshotCacheEntry> WorldSnapshots = new Dictionary<WorldSnapshotKey, WorldSnapshotCacheEntry>();
        private static readonly Dictionary<WorldSnapshotKey, LightweightSnapshotCacheEntry> LightweightSnapshots = new Dictionary<WorldSnapshotKey, LightweightSnapshotCacheEntry>();
        private static readonly List<WorldSnapshotKey> ExpiredWorldSnapshotKeys = new List<WorldSnapshotKey>();
        private static readonly List<WorldSnapshotKey> ExpiredLightweightSnapshotKeys = new List<WorldSnapshotKey>();
        private static int cachedWorldRegistryVersion = -1;

        /// <summary>
        /// 扫描当前场景中的储存网络成员，并返回带缓存的快照。UI 高频刷新时优先使用这个入口。
        /// </summary>
        public static StorageSceneSnapshot Collect(bool force = false)
        {
            StorageSceneRegistry.EnsureSceneSeeded();
            int registryVersion = StorageSceneRegistry.Version;
            bool networkOnline = StorageSceneRegistry.HasOnlineCoreInActiveWorld(out bool crossPlanetRelayOnline);
            if (!force &&
                cachedSnapshot != null &&
                cachedRegistryVersion == registryVersion &&
                cachedNetworkOnline == networkOnline &&
                (cachedFrame == Time.frameCount || Time.unscaledTime - cachedAtUnscaledTime <= Config.Instance.SceneScanCacheSeconds))
            {
                return cachedSnapshot;
            }

            if (!networkOnline)
            {
                cachedSnapshot = new StorageSceneSnapshot(new List<StorageInfo>(), 0f, 0f, false);
                cachedAtUnscaledTime = Time.unscaledTime;
                cachedFrame = Time.frameCount;
                cachedRegistryVersion = registryVersion;
                cachedNetworkOnline = false;
                return cachedSnapshot;
            }

            List<StorageInfo> collected = new List<StorageInfo>();
            int activeWorldId = ClusterManager.Instance != null ? ClusterManager.Instance.activeWorldId : -1;
            BuildSnapshotContents(collected, activeWorldId, crossPlanetRelayOnline);

            StorageSceneSnapshot snapshot = CreateSnapshot(collected, true);
            cachedSnapshot = snapshot;
            cachedAtUnscaledTime = Time.unscaledTime;
            cachedFrame = Time.frameCount;
            cachedRegistryVersion = registryVersion;
            cachedNetworkOnline = true;
            return cachedSnapshot;
        }

        public static StorageSceneSnapshot CollectForWorld(int worldId, bool includeReachableWorlds = true, bool force = false)
        {
            StorageSceneRegistry.EnsureSceneSeeded();
            PruneWorldCaches();
            WorldSnapshotKey key = new WorldSnapshotKey(worldId, includeReachableWorlds);
            if (!force &&
                WorldSnapshots.TryGetValue(key, out WorldSnapshotCacheEntry cached) &&
                (cached.Frame == Time.frameCount || Time.unscaledTime - cached.CreatedAt <= GetWorldSnapshotCacheSeconds()))
            {
                return cached.Snapshot;
            }

            bool networkOnline = StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
            if (!networkOnline)
            {
                StorageSceneSnapshot offline = new StorageSceneSnapshot(new List<StorageInfo>(), 0f, 0f, false);
                WorldSnapshots[key] = new WorldSnapshotCacheEntry(offline, Time.frameCount, Time.unscaledTime);
                return offline;
            }

            bool crossPlanetRelayOnline = includeReachableWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline();
            List<StorageInfo> collected = new List<StorageInfo>();
            BuildSnapshotContents(collected, worldId, crossPlanetRelayOnline);
            StorageSceneSnapshot snapshot = CreateSnapshot(collected, true);
            WorldSnapshots[key] = new WorldSnapshotCacheEntry(snapshot, Time.frameCount, Time.unscaledTime);
            StorageNetworkPerformanceCounters.RecordCollectForWorldRebuild();
            return snapshot;
        }

        public static StorageSceneLightweightSnapshot CollectLightweightForWorld(int worldId, bool includeReachableWorlds = true)
        {
            StorageSceneRegistry.EnsureSceneSeeded();
            PruneWorldCaches();
            WorldSnapshotKey key = new WorldSnapshotKey(worldId, includeReachableWorlds);
            if (LightweightSnapshots.TryGetValue(key, out LightweightSnapshotCacheEntry cached) &&
                (cached.Frame == Time.frameCount || Time.unscaledTime - cached.CreatedAt <= GetWorldSnapshotCacheSeconds()))
            {
                return cached.Snapshot;
            }

            bool networkOnline = StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
            if (!networkOnline)
            {
                LightweightSnapshots[key] = new LightweightSnapshotCacheEntry(StorageSceneLightweightSnapshot.Empty, Time.frameCount, Time.unscaledTime);
                return StorageSceneLightweightSnapshot.Empty;
            }

            bool crossPlanetRelayOnline = includeReachableWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline();
            List<Storage> storages = new List<Storage>();
            foreach (Storage storage in StorageSceneRegistry.GetStorages())
            {
                if (!StorageSceneRegistry.IsLive(storage))
                {
                    continue;
                }

                if (!crossPlanetRelayOnline && worldId >= 0 && storage.gameObject.GetMyWorldId() != worldId)
                {
                    continue;
                }

                if (StorageNetworkMembership.IsCollectableStorage(storage))
                {
                    storages.Add(storage);
                }
            }

            StorageSceneLightweightSnapshot snapshot = new StorageSceneLightweightSnapshot(storages, true);
            LightweightSnapshots[key] = new LightweightSnapshotCacheEntry(snapshot, Time.frameCount, Time.unscaledTime);
            StorageNetworkPerformanceCounters.RecordLightweightSceneRebuild();
            return snapshot;
        }

        private static void BuildSnapshotContents(List<StorageInfo> collected, int worldId, bool crossPlanetRelayOnline)
        {
            foreach (Storage storage in StorageSceneRegistry.GetStorages())
            {
                if (!StorageSceneRegistry.IsLive(storage))
                {
                    continue;
                }

                if (!crossPlanetRelayOnline && worldId >= 0 && storage.gameObject.GetMyWorldId() != worldId)
                {
                    continue;
                }

                if (StorageNetworkMembership.IsCollectableStorage(storage))
                {
                    collected.Add(new StorageInfo(storage));
                }
            }

            foreach (Geyser geyser in StorageSceneRegistry.GetGeysers())
            {
                if (!StorageSceneRegistry.IsLive(geyser))
                {
                    continue;
                }

                if (!crossPlanetRelayOnline && worldId >= 0 && geyser.gameObject.GetMyWorldId() != worldId)
                {
                    continue;
                }

                StorageNetwork.Components.StorageNetworkEnrollment enrollment =
                    geyser != null ? geyser.GetComponent<StorageNetwork.Components.StorageNetworkEnrollment>() : null;
                if (enrollment != null && enrollment.IncludedInSceneNetwork && enrollment.IsAnalyzedGeyser())
                {
                    collected.Add(new StorageInfo(enrollment.GetComponent<Geyser>()));
                }
            }

            collected.Sort((left, right) => string.Compare(left?.Name, right?.Name, System.StringComparison.CurrentCulture));
        }

        private static StorageSceneSnapshot CreateSnapshot(List<StorageInfo> collected, bool networkOnline)
        {
            float totalStoredKg = 0f;
            float totalCapacityKg = 0f;
            foreach (StorageInfo info in collected)
            {
                if (info == null)
                {
                    continue;
                }

                if (StorageNetworkStorageRules.CountsTowardNetworkCapacity(info.Storage))
                {
                    totalStoredKg += info.StoredKg;
                    totalCapacityKg += info.CapacityKg;
                }
            }

            return new StorageSceneSnapshot(collected, totalStoredKg, totalCapacityKg, networkOnline);
        }

        public static void InvalidateCache()
        {
            cachedSnapshot = null;
            cachedAtUnscaledTime = -1f;
            cachedFrame = -1;
            cachedRegistryVersion = -1;
            WorldSnapshots.Clear();
            LightweightSnapshots.Clear();
            cachedWorldRegistryVersion = -1;
        }

        public static void ResetRuntimeState()
        {
            InvalidateCache();
            cachedNetworkOnline = false;
        }

        private static void PruneWorldCaches()
        {
            int registryVersion = StorageSceneRegistry.Version;
            if (cachedWorldRegistryVersion != registryVersion)
            {
                WorldSnapshots.Clear();
                LightweightSnapshots.Clear();
                cachedWorldRegistryVersion = registryVersion;
                return;
            }

            float cutoff = Time.unscaledTime - GetWorldSnapshotCacheSeconds();
            ExpiredWorldSnapshotKeys.Clear();
            foreach (KeyValuePair<WorldSnapshotKey, WorldSnapshotCacheEntry> pair in WorldSnapshots)
            {
                if (pair.Value.CreatedAt < cutoff)
                {
                    ExpiredWorldSnapshotKeys.Add(pair.Key);
                }
            }

            foreach (WorldSnapshotKey key in ExpiredWorldSnapshotKeys)
            {
                WorldSnapshots.Remove(key);
            }

            ExpiredWorldSnapshotKeys.Clear();
            ExpiredLightweightSnapshotKeys.Clear();
            foreach (KeyValuePair<WorldSnapshotKey, LightweightSnapshotCacheEntry> pair in LightweightSnapshots)
            {
                if (pair.Value.CreatedAt < cutoff)
                {
                    ExpiredLightweightSnapshotKeys.Add(pair.Key);
                }
            }

            foreach (WorldSnapshotKey key in ExpiredLightweightSnapshotKeys)
            {
                LightweightSnapshots.Remove(key);
            }

            ExpiredLightweightSnapshotKeys.Clear();
        }

        private readonly struct WorldSnapshotKey : System.IEquatable<WorldSnapshotKey>
        {
            private readonly int worldId;
            private readonly bool includeReachableWorlds;

            public WorldSnapshotKey(int worldId, bool includeReachableWorlds)
            {
                this.worldId = worldId;
                this.includeReachableWorlds = includeReachableWorlds;
            }

            public bool Equals(WorldSnapshotKey other)
            {
                return worldId == other.worldId && includeReachableWorlds == other.includeReachableWorlds;
            }

            public override bool Equals(object obj)
            {
                return obj is WorldSnapshotKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (worldId * 397) ^ includeReachableWorlds.GetHashCode();
                }
            }
        }

        private static float GetWorldSnapshotCacheSeconds()
        {
            return Mathf.Clamp(Config.Instance.SceneScanCacheSeconds, 0.25f, 0.5f);
        }

        private readonly struct WorldSnapshotCacheEntry
        {
            public WorldSnapshotCacheEntry(StorageSceneSnapshot snapshot, int frame, float createdAt)
            {
                Snapshot = snapshot;
                Frame = frame;
                CreatedAt = createdAt;
            }

            public StorageSceneSnapshot Snapshot { get; }

            public int Frame { get; }

            public float CreatedAt { get; }
        }

        private readonly struct LightweightSnapshotCacheEntry
        {
            public LightweightSnapshotCacheEntry(StorageSceneLightweightSnapshot snapshot, int frame, float createdAt)
            {
                Snapshot = snapshot;
                Frame = frame;
                CreatedAt = createdAt;
            }

            public StorageSceneLightweightSnapshot Snapshot { get; }

            public int Frame { get; }

            public float CreatedAt { get; }
        }
    }
}
