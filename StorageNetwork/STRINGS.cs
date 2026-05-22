using STRINGS;

namespace StorageNetwork
{
    internal class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class STORAGENETWORKCABLE
                {
                    public static LocString NAME = "储存网络线缆";
                    public static LocString DESC = "连接储存网络建筑。";
                    public static LocString EFFECT = "用独立网络连接储存建筑，不传输自动化信号。";
                }

                public class STORAGENETWORKHUB
                {
                    public static LocString NAME = "储存网络核心";
                    public static LocString DESC = "显示并管理连接到储存网络线缆的储存建筑。";
                    public static LocString EFFECT = "扫描相连的储存网络线缆，并汇总所有连接储存的容量状态。";
                }
            }
        }

        public class UI
        {
            public class STORAGE_NETWORK
            {
                public static LocString NETWORK_READY = "储存网络已连接";
                public static LocString NETWORK_OFFLINE = "储存网络未连接";
                public static LocString SIDE_SCREEN_TITLE = "储存网络";
                public static LocString NO_STORAGES = "未连接储存建筑";
                public static LocString SUMMARY = "总计：{0} / {1}";
                public static LocString OVERVIEW_BUTTON = "概览";
                public static LocString OVERVIEW_TOOLTIP = "高亮储存网络线缆、核心和已连接储存建筑。";
                public static LocString VIEW_NETWORK_BUTTON = "查看储存网络";
                public static LocString VIEW_NETWORK_TOOLTIP = "打开储存网络面板，查看当前核心连接的所有储存建筑和容量状态。";
                public static LocString LEGEND_CABLE = "储存网络线缆";
                public static LocString LEGEND_CABLE_TOOLTIP = "连接储存网络核心与储存建筑。";
                public static LocString LEGEND_HUB = "储存网络核心";
                public static LocString LEGEND_HUB_TOOLTIP = "扫描并汇总相连储存建筑。";
                public static LocString LEGEND_STORAGE = "已连接储存";
                public static LocString LEGEND_STORAGE_TOOLTIP = "当前储存网络可共享的储存建筑。";
            }
        }
    }
}
