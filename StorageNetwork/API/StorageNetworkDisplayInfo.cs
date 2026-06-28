using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>
    /// 描述仓库在储存网络主面板中的显示覆盖项。未设置的字段会继续使用主模组默认显示。
    /// </summary>
    public sealed class StorageNetworkDisplayInfo
    {
        /// <summary>类型分组稳定键。相同键会合并到同一个折叠组。</summary>
        public string TypeKey { get; set; }

        /// <summary>类型分组显示名称。</summary>
        public string TypeName { get; set; }

        /// <summary>单个仓库行显示名称。</summary>
        public string RowName { get; set; }

        /// <summary>类型分组图标。</summary>
        public Sprite TypeIcon { get; set; }

        /// <summary>类型分组图标颜色。</summary>
        public Color? TypeIconTint { get; set; }
    }
}
