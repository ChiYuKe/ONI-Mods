using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>
    /// 储存网络主面板公开调色板。附属模组可以直接使用这些颜色，让自定义面板与主模组保持一致。
    /// </summary>
    public static class StorageNetworkPanelPalette
    {
        /// <summary>
        /// 主窗口背景色。
        /// </summary>
        public static Color WindowBackground => new Color(0.78f, 0.79f, 0.80f, 0.98f);

        /// <summary>
        /// 主窗口内容区背景色。
        /// </summary>
        public static Color ContentBackground => new Color(0.88f, 0.89f, 0.91f, 0.98f);

        /// <summary>
        /// 主窗口标题栏背景色。
        /// </summary>
        public static Color HeaderBackground => new Color(0.43f, 0.20f, 0.34f, 1f);

        /// <summary>
        /// 设置窗口标题栏背景色。
        /// </summary>
        public static Color SettingsHeaderBackground => new Color(0.36f, 0.42f, 0.47f, 1f);

        /// <summary>
        /// 设置卡片背景色。
        /// </summary>
        public static Color CardBackground => new Color(0.82f, 0.81f, 0.75f, 1f);

        /// <summary>
        /// 指标块背景色。
        /// </summary>
        public static Color MetricBackground => new Color(0.72f, 0.72f, 0.66f, 1f);

        /// <summary>
        /// 操作行和只读行背景色。
        /// </summary>
        public static Color RowBackground => new Color(0.76f, 0.76f, 0.70f, 1f);

        /// <summary>
        /// 主要标题文字色。
        /// </summary>
        public static Color HeadingText => new Color(0.18f, 0.19f, 0.18f, 1f);

        /// <summary>
        /// 普通正文文字色。
        /// </summary>
        public static Color BodyText => new Color(0.20f, 0.21f, 0.20f, 1f);

        /// <summary>
        /// 次要说明文字色。
        /// </summary>
        public static Color MutedText => new Color(0.34f, 0.35f, 0.33f, 1f);

        /// <summary>
        /// 指标标签文字色。
        /// </summary>
        public static Color MetricLabelText => new Color(0.30f, 0.32f, 0.31f, 1f);

        /// <summary>
        /// 指标默认强调色。
        /// </summary>
        public static Color MetricAccent => new Color(0.35f, 0.40f, 0.43f, 1f);

        /// <summary>
        /// 状态横幅文字色。
        /// </summary>
        public static Color StatusText => new Color(0.96f, 0.96f, 0.90f, 1f);

        /// <summary>
        /// 正常、启用、成功状态色。
        /// </summary>
        public static Color PositiveStatus => new Color(0.28f, 0.48f, 0.34f, 1f);

        /// <summary>
        /// 警告状态色。
        /// </summary>
        public static Color WarningStatus => new Color(0.50f, 0.42f, 0.34f, 1f);

        /// <summary>
        /// 手动、空闲、未启用状态色。
        /// </summary>
        public static Color NeutralStatus => new Color(0.48f, 0.45f, 0.36f, 1f);

        /// <summary>
        /// 错误或危险状态色。
        /// </summary>
        public static Color DangerStatus => new Color(0.68f, 0.18f, 0.14f, 1f);

        /// <summary>
        /// 蓝色按钮普通状态色。
        /// </summary>
        public static Color BlueButtonNormal => new Color(0.17f, 0.19f, 0.25f, 1f);

        /// <summary>
        /// 蓝色按钮悬停状态色。
        /// </summary>
        public static Color BlueButtonHover => new Color(0.25f, 0.28f, 0.35f, 1f);

        /// <summary>
        /// 蓝色按钮按下状态色。
        /// </summary>
        public static Color BlueButtonPressed => new Color(0.11f, 0.12f, 0.16f, 1f);

        /// <summary>
        /// 粉色按钮普通状态色，主模组用于表示已开启的开关按钮。
        /// </summary>
        public static Color PinkButtonNormal => new Color(0.5294118f, 0.2724914f, 0.4009516f, 1f);

        /// <summary>
        /// 粉色按钮悬停状态色，主模组用于表示已开启的开关按钮。
        /// </summary>
        public static Color PinkButtonHover => new Color(0.6176471f, 0.3315311f, 0.4745891f, 1f);

        /// <summary>
        /// 粉色按钮按下状态色，主模组用于表示已开启的开关按钮。
        /// </summary>
        public static Color PinkButtonPressed => new Color(0.7941176f, 0.4496107f, 0.6242238f, 1f);

        /// <summary>
        /// 根据启用状态返回主模组使用的状态色。
        /// </summary>
        public static Color GetEnabledStatusColor(bool enabled)
        {
            return enabled ? PositiveStatus : new Color(0.52f, 0.38f, 0.30f, 1f);
        }

        /// <summary>
        /// 根据网络自动化状态返回主模组使用的状态色。
        /// </summary>
        public static Color GetNetworkAutomationColor(bool enabled)
        {
            return enabled ? PositiveStatus : WarningStatus;
        }

        /// <summary>
        /// 根据自动入网状态返回主模组使用的状态色。
        /// </summary>
        public static Color GetOutputStoreColor(bool enabled)
        {
            return enabled ? PositiveStatus : NeutralStatus;
        }
    }
}
