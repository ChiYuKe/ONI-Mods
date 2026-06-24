namespace StorageNetwork.API
{
    /// <summary>
    /// 可由附属模组组件实现的储存网络设置面板提供器。
    /// 实现后，点击储存网络设置按钮时可以由附属模组构建自己的设置内容。
    /// </summary>
    public interface IStorageNetworkSettingsPanelProvider
    {
        /// <summary>
        /// 返回设置面板内容签名。签名变化时主模组会重建面板内容；为空则每次打开时重建。
        /// </summary>
        string GetStorageNetworkSettingsPanelSignature(Storage storage);

        /// <summary>
        /// 构建设置面板内容。可通过 builder 添加卡片、说明、只读行和按钮。
        /// </summary>
        void BuildStorageNetworkSettingsPanel(Storage storage, StorageNetworkSettingsPanelBuilder builder);
    }
}
