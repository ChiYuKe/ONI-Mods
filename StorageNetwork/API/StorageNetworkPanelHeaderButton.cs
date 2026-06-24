namespace StorageNetwork.API
{
    /// <summary>
    /// 储存网络主面板顶部扩展按钮的描述对象。附属模组通过按钮提供器返回此对象，由主模组负责创建实际 UI。
    /// </summary>
    public sealed class StorageNetworkPanelHeaderButton
    {
        /// <summary>
        /// 创建一个主面板顶部按钮描述。
        /// </summary>
        /// <param name="id">按钮稳定 ID，用于排序和 UI 对象命名。</param>
        /// <param name="label">按钮上显示的文字。</param>
        /// <param name="onClick">点击按钮时执行的回调。</param>
        /// <param name="tooltip">鼠标悬停提示文本。</param>
        /// <param name="iconName">优先使用的图标名称，会从主模组/游戏 sprite 中查找。</param>
        /// <param name="fallbackIconText">找不到图标时显示的备用短文本。</param>
        /// <param name="width">按钮宽度。</param>
        /// <param name="order">排序值，数值越小越靠左。</param>
        public StorageNetworkPanelHeaderButton(
            string id,
            string label,
            System.Action onClick,
            string tooltip = null,
            string iconName = null,
            string fallbackIconText = null,
            float width = 72f,
            int order = 100)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            OnClick = onClick;
            Tooltip = tooltip ?? string.Empty;
            IconName = iconName ?? string.Empty;
            FallbackIconText = fallbackIconText ?? string.Empty;
            Width = width <= 0f ? 72f : width;
            Order = order;
        }

        /// <summary>
        /// 创建一个主面板顶部按钮描述，并在点击时接收主面板上下文。适合附属模组打开自定义窗口或 AssetBundle prefab。
        /// </summary>
        /// <param name="id">按钮稳定 ID，用于排序和 UI 对象命名。</param>
        /// <param name="label">按钮上显示的文字。</param>
        /// <param name="onClick">点击按钮时执行的回调，会收到主面板上下文。</param>
        /// <param name="tooltip">鼠标悬停提示文本。</param>
        /// <param name="iconName">优先使用的图标名称，会从主模组/游戏 sprite 中查找。</param>
        /// <param name="fallbackIconText">找不到图标时显示的备用短文本。</param>
        /// <param name="width">按钮宽度。</param>
        /// <param name="order">排序值，数值越小越靠左。</param>
        public StorageNetworkPanelHeaderButton(
            string id,
            string label,
            System.Action<StorageNetworkPanelHeaderButtonContext> onClick,
            string tooltip = null,
            string iconName = null,
            string fallbackIconText = null,
            float width = 72f,
            int order = 100)
            : this(id, label, (System.Action)null, tooltip, iconName, fallbackIconText, width, order)
        {
            OnClickWithContext = onClick;
        }

        /// <summary>按钮稳定 ID。</summary>
        public string Id { get; }

        /// <summary>按钮显示文字。</summary>
        public string Label { get; }

        /// <summary>按钮点击回调。</summary>
        public System.Action OnClick { get; }

        /// <summary>按钮点击回调，会收到主面板上下文。</summary>
        public System.Action<StorageNetworkPanelHeaderButtonContext> OnClickWithContext { get; }

        /// <summary>按钮悬停提示。</summary>
        public string Tooltip { get; }

        /// <summary>按钮图标名称。</summary>
        public string IconName { get; }

        /// <summary>找不到图标时显示的备用短文本。</summary>
        public string FallbackIconText { get; }

        /// <summary>按钮宽度。</summary>
        public float Width { get; }

        /// <summary>排序值，数值越小越靠左。</summary>
        public int Order { get; }
    }
}
