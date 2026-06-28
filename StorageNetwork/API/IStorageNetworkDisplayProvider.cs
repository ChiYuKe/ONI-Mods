namespace StorageNetwork.API
{
    /// <summary>
    /// 允许附属组件覆盖储存网络主面板里的类型名、行名和图标。
    /// </summary>
    public interface IStorageNetworkDisplayProvider
    {
        /// <summary>
        /// 返回显示覆盖信息；返回 null 或留空字段时使用主模组默认显示。
        /// </summary>
        /// <param name="storage">正在显示的仓库。</param>
        StorageNetworkDisplayInfo GetStorageNetworkDisplayInfo(Storage storage);
    }
}
