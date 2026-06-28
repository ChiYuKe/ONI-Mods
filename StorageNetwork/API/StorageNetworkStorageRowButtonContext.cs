using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>
    /// 仓库行扩展按钮点击时传给附属模组的上下文。
    /// </summary>
    public sealed class StorageNetworkStorageRowButtonContext
    {
        internal StorageNetworkStorageRowButtonContext(Storage storage, GameObject panelRoot, GameObject row, GameObject button)
        {
            Storage = storage;
            PanelRoot = panelRoot;
            Row = row;
            Button = button;
        }

        /// <summary>按钮所在的仓库。</summary>
        public Storage Storage { get; }

        /// <summary>储存网络主面板根对象。</summary>
        public GameObject PanelRoot { get; }

        /// <summary>当前仓库行对象。</summary>
        public GameObject Row { get; }

        /// <summary>当前按钮对象。</summary>
        public GameObject Button { get; }
    }
}
