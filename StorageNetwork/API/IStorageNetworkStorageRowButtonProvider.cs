using System.Collections.Generic;

namespace StorageNetwork.API
{
    /// <summary>
    /// 允许附属组件向储存网络主面板的仓库行右侧添加按钮。
    /// </summary>
    public interface IStorageNetworkStorageRowButtonProvider
    {
        /// <summary>
        /// 返回当前仓库行要显示的扩展按钮。返回空集合或 null 时不添加按钮。
        /// </summary>
        /// <param name="storage">正在显示的仓库。</param>
        IEnumerable<StorageNetworkStorageRowButton> GetStorageNetworkStorageRowButtons(Storage storage);
    }
}
