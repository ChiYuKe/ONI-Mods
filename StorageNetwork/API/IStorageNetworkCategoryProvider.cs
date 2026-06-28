namespace StorageNetwork.API
{
    /// <summary>
    /// 允许附属组件决定自己的仓库在储存网络主面板中归属到哪个分类页签。
    /// </summary>
    public interface IStorageNetworkCategoryProvider
    {
        /// <summary>
        /// 返回分类描述；返回 null 时使用主模组默认分类规则。
        /// </summary>
        /// <param name="storage">正在分类的仓库。</param>
        StorageNetworkCategoryDescriptor GetStorageNetworkCategory(Storage storage);
    }
}
