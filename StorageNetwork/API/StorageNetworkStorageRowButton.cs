namespace StorageNetwork.API
{
    /// <summary>
    /// 描述储存网络主面板仓库行右侧的扩展按钮。
    /// </summary>
    public sealed class StorageNetworkStorageRowButton
    {
        public StorageNetworkStorageRowButton(
            string id,
            string label,
            System.Action<StorageNetworkStorageRowButtonContext> onClick,
            string tooltip = null,
            string iconName = null,
            string fallbackIconText = null,
            float width = 50f,
            int order = 100,
            bool isEnabled = true)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            OnClick = onClick;
            Tooltip = tooltip ?? string.Empty;
            IconName = iconName ?? string.Empty;
            FallbackIconText = fallbackIconText ?? string.Empty;
            Width = width <= 0f ? 50f : width;
            Order = order;
            IsEnabled = isEnabled;
        }

        /// <summary>按钮稳定 ID，用于排序和 UI 对象命名。</summary>
        public string Id { get; }

        /// <summary>按钮显示文字。</summary>
        public string Label { get; }

        /// <summary>按钮点击回调。</summary>
        public System.Action<StorageNetworkStorageRowButtonContext> OnClick { get; }

        /// <summary>鼠标悬停提示。</summary>
        public string Tooltip { get; }

        /// <summary>优先使用的图标名称，会从主模组/游戏 sprite 中查找。</summary>
        public string IconName { get; }

        /// <summary>找不到图标时显示的备用短文本。</summary>
        public string FallbackIconText { get; }

        /// <summary>按钮宽度。</summary>
        public float Width { get; }

        /// <summary>排序值，数值越小越靠左。</summary>
        public int Order { get; }

        /// <summary>是否允许玩家点击。</summary>
        public bool IsEnabled { get; }
    }
}
