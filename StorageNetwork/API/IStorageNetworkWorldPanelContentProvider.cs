using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>
    /// 建筑世界文字面板内容提供器。其他模组可以注册实现，覆盖或补充 StorageNetwork 的默认显示。
    /// </summary>
    public interface IStorageNetworkWorldPanelContentProvider
    {
        /// <summary>
        /// 尝试为目标建筑生成面板内容。返回 true 表示本提供器已经处理，后续提供器不会再执行。
        /// </summary>
        bool TryBuild(GameObject target, out StorageNetworkWorldPanelContent content);
    }
}
