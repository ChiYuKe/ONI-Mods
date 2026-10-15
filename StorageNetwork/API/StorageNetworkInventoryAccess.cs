using System;
using System.Collections.Generic;
using UnityEngine;
using StorageNetwork.Services;
using StorageNetwork.Core;

namespace StorageNetwork.API
{
    /// <summary>短期使用的物品标签视图；取得后立即用于接收仓库查询，不应跨帧缓存。</summary>
    public readonly struct StorageNetworkItemTags
    {
        internal readonly StorageItemUtility.StorageMatchTags Inner;
        internal StorageNetworkItemTags(StorageItemUtility.StorageMatchTags inner)
        {
            Inner = inner;
        }
    }

    /// <summary>附属模组使用的库存、生产预留量及接收路由接口。实际交付后须使受影响仓库的索引失效。</summary>
    public static class StorageNetworkInventoryAccess
    {
        /// <summary>判断仓库是否为本模组储存服务器。</summary>
        public static bool IsServerStorage(Storage storage) => StorageNetworkStorageRules.IsServerStorage(storage);
        /// <summary>判断仓库当前是否接入可用网络。</summary>
        public static bool IsConnectedNetworkStorage(Storage storage) => StorageNetworkStorageRules.IsConnectedNetworkStorage(storage);
        public static bool IsSolidStorageServer(Storage storage) => StorageNetworkStorageRules.IsSolidStorageServer(storage);
        public static bool IsColdStorageServer(Storage storage) => StorageNetworkStorageRules.IsColdStorageServer(storage);
        public static bool MatchesElementState(Storage storage, Element element) => StorageNetworkStorageRules.MatchesElementState(storage, element);

        /// <summary>选择可接收该物品的仓库；自动检查网络、过滤器、容量与世界可达性。</summary>
        public static Storage FindItemReceiver(GameObject item, StorageNetworkItemTags matchTags,
            HashSet<Storage> excludedStorages = null, Storage specificTarget = null,
            int sourceWorldId = -1, Storage sourceStorage = null) =>
            StorageTargetSelector.FindOutputTarget(item, matchTags.Inner, excludedStorages, specificTarget,
                null, sourceWorldId, sourceStorage);

        /// <summary>选择食品接收仓库，优先遵循前置的冷库接收规则。</summary>
        public static Storage FindFoodReceiver(GameObject item, StorageNetworkItemTags matchTags,
            HashSet<Storage> excludedStorages = null, Storage specificTarget = null,
            int sourceWorldId = -1, Storage sourceStorage = null) =>
            StorageTargetSelector.FindFoodOutputTarget(item, matchTags.Inner, excludedStorages, specificTarget,
                null, sourceWorldId, sourceStorage);

        /// <summary>按前置的元素路由规则获取可接收仓库，无需调用方构建内部场景快照。</summary>
        public static List<Storage> FindElementReceivers(SimHashes elementHash,
            HashSet<Storage> excludedStorages = null, Storage specificTarget = null, int sourceWorldId = -1) =>
            StorageTargetSelector.FindElementOutputTargets(elementHash, excludedStorages, specificTarget, null, sourceWorldId);

        /// <summary>忽略容量检查是否存在合适接收仓库，供界面区分满仓与规则不匹配。</summary>
        public static bool HasElementReceiverIgnoringCapacity(SimHashes elementHash,
            HashSet<Storage> excludedStorages = null, Storage specificTarget = null, int sourceWorldId = -1) =>
            StorageTargetSelector.HasElementOutputCandidateIgnoringCapacity(elementHash, excludedStorages, specificTarget, null, sourceWorldId);

        public static string GetWorldName(int worldId) => global::StorageNetwork.UI.StorageNetworkWorldDisplay.GetWorldName(worldId);
        public static IReadOnlyCollection<Storage> GetStorages() => global::StorageNetwork.Core.StorageSceneRegistry.GetStorages();
        public static bool HasOnlineCoreInWorld(int worldId) => global::StorageNetwork.Core.StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
        public static void Invalidate() => global::StorageNetwork.Core.StorageSceneRegistry.Invalidate();
        public static float GetReservedAmount(Tag tag, IReadOnlyList<Storage> sources) => global::StorageNetwork.ProductionOrders.ProductionOrderService.GetReservedInventoryAmount(tag, sources);
        public static Storage FindOutputTarget(GameObject item, HashSet<Tag> matchTags, HashSet<Storage> excludedStorages, Storage specificTarget, StorageSceneSnapshot snapshot = null, int sourceWorldId = -1, Storage sourceStorage = null) => global::StorageNetwork.Services.StorageTargetSelector.FindOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot, sourceWorldId, sourceStorage);
        public static Storage FindOutputTarget(GameObject item, StorageNetworkItemTags matchTags, HashSet<Storage> excludedStorages, Storage specificTarget, StorageSceneSnapshot snapshot = null, int sourceWorldId = -1, Storage sourceStorage = null) => global::StorageNetwork.Services.StorageTargetSelector.FindOutputTarget(item, matchTags.Inner, excludedStorages, specificTarget, snapshot, sourceWorldId, sourceStorage);
        public static Storage FindFoodOutputTarget(GameObject item, HashSet<Tag> matchTags, HashSet<Storage> excludedStorages, Storage specificTarget, StorageSceneSnapshot snapshot = null, int sourceWorldId = -1, Storage sourceStorage = null) => global::StorageNetwork.Services.StorageTargetSelector.FindFoodOutputTarget(item, matchTags, excludedStorages, specificTarget, snapshot, sourceWorldId, sourceStorage);
        public static Storage FindFoodOutputTarget(GameObject item, StorageNetworkItemTags matchTags, HashSet<Storage> excludedStorages, Storage specificTarget, StorageSceneSnapshot snapshot = null, int sourceWorldId = -1, Storage sourceStorage = null) => global::StorageNetwork.Services.StorageTargetSelector.FindFoodOutputTarget(item, matchTags.Inner, excludedStorages, specificTarget, snapshot, sourceWorldId, sourceStorage);
        public static List<Storage> FindElementOutputTargets(SimHashes elementHash, HashSet<Storage> excludedStorages = null, Storage specificTarget = null, StorageSceneSnapshot snapshot = null, int sourceWorldId = -1) => global::StorageNetwork.Services.StorageTargetSelector.FindElementOutputTargets(elementHash, excludedStorages, specificTarget, snapshot, sourceWorldId);
        public static bool HasElementOutputCandidateIgnoringCapacity(SimHashes elementHash, HashSet<Storage> excludedStorages = null, Storage specificTarget = null, StorageSceneSnapshot snapshot = null, int sourceWorldId = -1) => global::StorageNetwork.Services.StorageTargetSelector.HasElementOutputCandidateIgnoringCapacity(elementHash, excludedStorages, specificTarget, snapshot, sourceWorldId);
        public static bool IsStorageReachableFromWorld(Storage storage, int worldId) => global::StorageNetwork.Services.StorageTargetSelector.IsStorageReachableFromWorld(storage, worldId);
        public static string GetStoredItemKey(GameObject item) => global::StorageNetwork.Services.StorageItemUtility.GetStoredItemKey(item);
        public static StorageNetworkItemTags GetStorageMatchTagsNonAlloc(GameObject item) => new StorageNetworkItemTags(global::StorageNetwork.Services.StorageItemUtility.GetStorageMatchTagsNonAlloc(item));
        public static int GetStorageInstanceId(Storage storage) => global::StorageNetwork.Services.StorageItemUtility.GetStorageInstanceId(storage);
        public static void Invalidate(Storage storage) => global::StorageNetwork.Services.StorageNetworkContentIndexService.Invalidate(storage);
        public static void Invalidate(Storage first, Storage second) => global::StorageNetwork.Services.StorageNetworkContentIndexService.Invalidate(first, second);
        public static void FillSourceStorages(int worldId, bool includeRelatedWorlds, IEnumerable<Tag> wantedTags, HashSet<Storage> excludedStorages, Storage specificSource, List<Storage> result, bool allowStaleContent) => global::StorageNetwork.Services.StorageNetworkContentIndexService.FillSourceStorages(worldId, includeRelatedWorlds, wantedTags, excludedStorages, specificSource, result, allowStaleContent);
    }
}
