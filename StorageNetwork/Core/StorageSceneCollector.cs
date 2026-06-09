using System.Collections.Generic;
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

        public static StorageSceneSnapshot CollectForWorld(int worldId, bool includeReachableWorlds = true)
        {
            StorageSceneRegistry.EnsureSceneSeeded();
            bool crossPlanetRelayOnline = includeReachableWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline();
            bool networkOnline = StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
            if (!networkOnline)
            {
                return new StorageSceneSnapshot(new List<StorageInfo>(), 0f, 0f, false);
            }

            List<StorageInfo> collected = new List<StorageInfo>();
            BuildSnapshotContents(collected, worldId, crossPlanetRelayOnline);
            return CreateSnapshot(collected, true);
        }

        private static void BuildSnapshotContents(List<StorageInfo> collected, int worldId, bool crossPlanetRelayOnline)
        {
            foreach (Storage storage in StorageSceneRegistry.GetStorages())
            {
                if (!crossPlanetRelayOnline && worldId >= 0 && storage.GetMyWorldId() != worldId)
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
                if (!crossPlanetRelayOnline && worldId >= 0 && geyser.GetMyWorldId() != worldId)
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
        }

        public static void ResetRuntimeState()
        {
            InvalidateCache();
            cachedNetworkOnline = false;
        }
    }
}
