namespace StorageNetwork.API
{
    /// <summary>
    /// 可由附属模组建筑组件实现的储存网络能力提供器。实现后无需给 prefab 添加 StorageNetwork 标签，也能声明自身在网络中的角色。
    /// 该组件需要和目标 Storage 位于同一个 GameObject 上。
    /// </summary>
    public interface IStorageNetworkStorageFlagsProvider
    {
        /// <summary>
        /// 返回指定 Storage 在储存网络中的能力标志。未启用时可返回 StorageNetworkStorageFlags.None。
        /// </summary>
        StorageNetworkStorageFlags GetStorageNetworkStorageFlags(Storage storage);
    }
}
