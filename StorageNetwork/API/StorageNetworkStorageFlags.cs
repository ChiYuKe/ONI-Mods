using System;

namespace StorageNetwork.API
{
    /// <summary>
    /// 描述一个 Storage 如何参与储存网络。附属模组可以通过 IStorageNetworkStorageFlagsProvider 返回这些标志，
    /// 替代直接给 prefab 添加 StorageNetwork 标签。
    /// </summary>
    [Flags]
    public enum StorageNetworkStorageFlags
    {
        /// <summary>不参与储存网络。</summary>
        None = 0,
        /// <summary>作为普通网络储存成员参与主面板收集。</summary>
        NetworkStorage = 1 << 0,
        /// <summary>作为储存网络服务器参与容量和目标计算。</summary>
        ServerStorage = 1 << 1,
        /// <summary>在主面板和详情标题栏显示设置按钮。</summary>
        ShowSettingsButton = 1 << 2,
        /// <summary>在主面板中归入“附属储物”分类。</summary>
        CategoryModStorage = 1 << 3,
        /// <summary>声明为输入端口。</summary>
        InputPort = 1 << 4,
        /// <summary>声明为输出端口。</summary>
        OutputPort = 1 << 5,
        /// <summary>声明为固体端口。</summary>
        SolidPort = 1 << 6,
        /// <summary>声明为液体端口。</summary>
        LiquidPort = 1 << 7,
        /// <summary>声明为气体端口。</summary>
        GasPort = 1 << 8,
        /// <summary>声明为电力端口。</summary>
        PowerPort = 1 << 9,
        /// <summary>声明为高能粒子端口。</summary>
        ParticlePort = 1 << 10,
        /// <summary>声明为固体输入端口。</summary>
        SolidInputPort = 1 << 11,
        /// <summary>声明为固体输出端口。</summary>
        SolidOutputPort = 1 << 12,
        /// <summary>声明为液体输入端口。</summary>
        LiquidInputPort = 1 << 13,
        /// <summary>声明为液体输出端口。</summary>
        LiquidOutputPort = 1 << 14,
        /// <summary>声明为气体输入端口。</summary>
        GasInputPort = 1 << 15,
        /// <summary>声明为气体输出端口。</summary>
        GasOutputPort = 1 << 16,
        /// <summary>声明为电力输入端口。</summary>
        PowerInputPort = 1 << 17,
        /// <summary>声明为电力输出端口。</summary>
        PowerOutputPort = 1 << 18,
        /// <summary>声明为高能粒子输入端口。</summary>
        ParticleInputPort = 1 << 19,
        /// <summary>声明为高能粒子输出端口。</summary>
        ParticleOutputPort = 1 << 20
    }
}
