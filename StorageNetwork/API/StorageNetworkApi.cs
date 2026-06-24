using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>
    /// 提供给附属模组调用的轻量公共 API。用于在运行时状态变化后通知储存网络刷新场景缓存或安装桥接组件。
    /// </summary>
    public static class StorageNetworkApi
    {
        /// <summary>
        /// 将目标对象登记到储存网络场景注册表中。通常只在动态创建对象或自定义生命周期时需要手动调用。
        /// </summary>
        public static void RegisterSceneMember(GameObject gameObject)
        {
            StorageSceneRegistry.Register(gameObject);
        }

        /// <summary>
        /// 如果目标对象上存在储存网络接口组件，则安装外部 API 桥接组件，用于用户菜单按钮和场景注册。
        /// </summary>
        public static void InstallExternalApiBridge(GameObject gameObject)
        {
            StorageNetworkInterfaceResolver.InstallExternalApiBridgeIfNeeded(gameObject);
        }

        /// <summary>
        /// 从储存网络场景注册表中移除目标对象。通常只在动态对象清理或自定义生命周期时需要手动调用。
        /// </summary>
        public static void UnregisterSceneMember(GameObject gameObject)
        {
            StorageSceneRegistry.Unregister(gameObject);
        }

        /// <summary>
        /// 使储存网络场景缓存失效。附属模组切换接入状态、容量或端口能力后可调用此方法刷新主面板。
        /// </summary>
        public static void InvalidateScene()
        {
            StorageSceneRegistry.Invalidate();
        }

        /// <summary>
        /// 刷新目标对象相关的原版界面。附属模组切换接入状态后可调用此方法，让用户菜单按钮和详情标题按钮立刻更新。
        /// </summary>
        public static void RefreshObjectUi(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            Game.Instance?.userMenu?.Refresh(gameObject);
            DetailsScreen.Instance?.Refresh(gameObject);
        }

        /// <summary>
        /// 将指定仓库中的物品转存到同世界的储存网络。默认会排除源仓库，避免物品转回自己。
        /// </summary>
        public static StorageNetworkTransferResult TransferStoredItemsToNetwork(Storage source, Storage specificTarget = null, bool preferColdStorageForFood = false)
        {
            StorageTransferResult result = NetworkStorageTransferService.TransferStoredItemsToNetwork(
                source,
                source != null ? new[] { source } : null,
                specificTarget,
                null,
                false,
                preferColdStorageForFood);
            return ToApiResult(result);
        }

        /// <summary>
        /// 将指定仓库中的一个物品转存到同世界的储存网络。默认会排除源仓库，避免物品转回自己。
        /// </summary>
        public static StorageNetworkTransferResult TransferStoredItemToNetwork(Storage source, GameObject item, Storage specificTarget = null, bool preferColdStorageForFood = false)
        {
            StorageTransferResult result = NetworkStorageTransferService.TransferStoredItemToNetwork(
                source,
                item,
                source != null ? new[] { source } : null,
                specificTarget,
                preferColdStorageForFood);
            return ToApiResult(result);
        }

        /// <summary>
        /// 按标签从同世界的储存网络拉取物品到目标仓库。可用于附属建筑的材料请求。
        /// </summary>
        public static float TransferFromNetworkToStorage(System.Collections.Generic.IEnumerable<Tag> tags, float amount, Storage destination, Storage specificSource = null)
        {
            return NetworkStorageTransferService.TransferFromNetworkToStorage(
                tags,
                amount,
                destination,
                destination != null ? new[] { destination } : null,
                specificSource);
        }

        private static StorageNetworkTransferResult ToApiResult(StorageTransferResult result)
        {
            if (result == null)
            {
                return StorageNetworkTransferResult.Idle;
            }

            return new StorageNetworkTransferResult(result.MovedKg, result.BlockedItem, result.NetworkOffline);
        }
    }
}
