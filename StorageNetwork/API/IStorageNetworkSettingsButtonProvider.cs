namespace StorageNetwork.API
{
    /// <summary>
    /// 可由附属模组组件实现的储存网络设置按钮状态提供器。
    /// 该接口用于控制详情标题栏和主面板行上的储存网络设置按钮是否显示、是否可点击，以及提示文本。
    /// </summary>
    public interface IStorageNetworkSettingsButtonProvider
    {
        /// <summary>
        /// 返回指定 Storage 的设置按钮状态。返回 null 表示使用主模组默认状态。
        /// </summary>
        StorageNetworkSettingsButtonState GetStorageNetworkSettingsButtonState(Storage storage);
    }
}
