namespace StorageNetwork.API
{
    /// <summary>
    /// 储存网络转运结果。附属模组可用它判断本次请求是否移动了物品，以及失败时显示原因。
    /// </summary>
    public sealed class StorageNetworkTransferResult
    {
        /// <summary>
        /// 空闲结果，表示没有物品需要移动。
        /// </summary>
        public static readonly StorageNetworkTransferResult Idle = new StorageNetworkTransferResult(0f, null, false);

        /// <summary>
        /// 网络离线结果，表示当前世界没有可用的储存网络核心。
        /// </summary>
        public static readonly StorageNetworkTransferResult Offline = new StorageNetworkTransferResult(0f, null, true);

        /// <summary>
        /// 创建一个转运结果。
        /// </summary>
        public StorageNetworkTransferResult(float movedKg, string blockedItem, bool networkOffline = false)
        {
            MovedKg = movedKg;
            BlockedItem = blockedItem;
            NetworkOffline = networkOffline;
        }

        /// <summary>
        /// 本次成功移动的质量，单位为千克。
        /// </summary>
        public float MovedKg { get; }

        /// <summary>
        /// 阻塞转运的物品名称。为空表示没有明确阻塞物品。
        /// </summary>
        public string BlockedItem { get; }

        /// <summary>
        /// 是否因为储存网络离线而无法移动。
        /// </summary>
        public bool NetworkOffline { get; }

        /// <summary>
        /// 是否实际移动了物品。
        /// </summary>
        public bool HasMoved => MovedKg > 0f;

        /// <summary>
        /// 创建一个被指定物品阻塞的结果。
        /// </summary>
        public static StorageNetworkTransferResult Blocked(string itemName)
        {
            return new StorageNetworkTransferResult(0f, itemName);
        }
    }
}
