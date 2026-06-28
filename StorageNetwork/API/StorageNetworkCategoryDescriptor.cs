namespace StorageNetwork.API
{
    /// <summary>
    /// 描述储存网络主面板左侧分类页签。附属模组可通过分类提供器返回此对象来自定义分类名称和排序。
    /// </summary>
    public sealed class StorageNetworkCategoryDescriptor
    {
        public StorageNetworkCategoryDescriptor(string key, string name, int order = 0)
        {
            Key = key ?? string.Empty;
            Name = name ?? string.Empty;
            Order = order;
        }

        /// <summary>分类稳定键。相同键会合并到同一个页签。</summary>
        public string Key { get; }

        /// <summary>分类显示名称。</summary>
        public string Name { get; }

        /// <summary>分类排序值，数值越小越靠前。</summary>
        public int Order { get; }
    }
}
