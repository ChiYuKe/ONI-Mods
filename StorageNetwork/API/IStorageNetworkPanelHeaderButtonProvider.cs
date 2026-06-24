using System.Collections.Generic;

namespace StorageNetwork.API
{
    /// <summary>
    /// 主面板顶部按钮提供器。附属模组实现并注册后，可在储存网络主面板标题栏的订单中心按钮右侧添加按钮。
    /// </summary>
    public interface IStorageNetworkPanelHeaderButtonProvider
    {
        /// <summary>
        /// 返回当前要显示在主面板顶部的按钮描述。主模组会按按钮的 Order 和 Id 排序。
        /// </summary>
        IEnumerable<StorageNetworkPanelHeaderButton> GetHeaderButtons();
    }
}
