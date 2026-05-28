using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageSceneCollector
    {
        private static StorageSceneSnapshot cachedSnapshot;
        private static float cachedAtUnscaledTime = -1f;
        private static int cachedFrame = -1;

        /// <summary>
        /// 扫描当前场景中的储存网络成员，并返回带缓存的快照。UI 高频刷新时优先使用这个入口。
        /// </summary>
        public static StorageSceneSnapshot Collect(bool force = false)
        {
            if (!force && cachedSnapshot != null && (cachedFrame == Time.frameCount || Time.unscaledTime - cachedAtUnscaledTime <= Config.Instance.SceneScanCacheSeconds))
            {
                return cachedSnapshot;
            }

            Storage[] storages = Object.FindObjectsByType<Storage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<StorageInfo> collected = storages
                .Where(StorageNetworkMembership.IsCollectableStorage)
                .Select(storage => new StorageInfo(storage))
                .OrderBy(info => info.Name)
                .ToList();

            float totalStoredKg = collected.Sum(info => info.StoredKg);
            float totalCapacityKg = collected.Sum(info => info.CapacityKg);
            cachedSnapshot = new StorageSceneSnapshot(collected, totalStoredKg, totalCapacityKg);
            cachedAtUnscaledTime = Time.unscaledTime;
            cachedFrame = Time.frameCount;
            return cachedSnapshot;
        }
    }
}
