namespace StorageNetwork.API
{
    /// <summary>
    /// 描述详情标题栏储存网络设置按钮的运行时状态。
    /// </summary>
    public sealed class StorageNetworkSettingsButtonState
    {
        /// <summary>
        /// 是否显示设置按钮。
        /// </summary>
        public bool IsVisible { get; }

        /// <summary>
        /// 是否允许玩家点击设置按钮。
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// 鼠标悬停时显示的提示文本。为空时使用主模组默认提示。
        /// </summary>
        public string Tooltip { get; }

        public StorageNetworkSettingsButtonState(bool isVisible, bool isEnabled, string tooltip = null)
        {
            IsVisible = isVisible;
            IsEnabled = isEnabled;
            Tooltip = tooltip;
        }
    }
}
