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
            public class TOOLS
            {
                public class FILTERLAYERS
                {
                    public class STORAGENETWORK
                    {
                        public static LocString NAME = "储存网络";
                        public static LocString TOOLTIP = "仅储存网络线缆";
                    }
                }
            }

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
                public static LocString PORT_INPUT_HOVER_FORMAT = "{Name} 的 {Port}";
                public static LocString PORT_OUTPUT_HOVER_FORMAT = "{Name} 的 {Port}";
                public static LocString PORT_CONNECTED = "已连接储存网络";
                public static LocString PORT_DISCONNECTED = "未连接储存网络";
                public static LocString PORT_STORAGE_AVAILABLE = "储存可加入网络";
                public static LocString PORT_STORAGE_UNAVAILABLE = "储存当前不可用";
                public static LocString REQUEST_RECIPE_MATERIALS = "从网络中请求材料";
                public static LocString STORE_RECIPE_PRODUCTS = "生产完成后将成品输入网络";
                public static LocString MISSING_RECIPE_MATERIAL_STATUS = "网络中缺失此类材料";
                public static LocString MISSING_RECIPE_MATERIAL_TOOLTIP = "网络中缺失此类材料，将由复制人补充材料。";
                public static LocString LEGEND_INPUT_PORT = "输入端";
                public static LocString LEGEND_INPUT_PORT_TOOLTIP = "储存网络从这里接入建筑。";
                public static LocString LEGEND_OUTPUT_PORT = "输出端";
                public static LocString LEGEND_OUTPUT_PORT_TOOLTIP = "储存网络从这里继续连接到其他建筑。";
                public static LocString LEGEND_CONNECTED_INPUT = "输入端已连接";
                public static LocString LEGEND_CONNECTED_INPUT_TOOLTIP = "输入端所在格有储存网络线缆。";
                public static LocString LEGEND_CONNECTED_OUTPUT = "输出端已连接";
                public static LocString LEGEND_CONNECTED_OUTPUT_TOOLTIP = "输出端所在格有储存网络线缆。";
                public static LocString LEGEND_DISCONNECTED = "未连接";
                public static LocString LEGEND_DISCONNECTED_TOOLTIP = "端口所在格没有储存网络线缆。";
                public static LocString LEGEND_CABLE = "储存网络线缆";
                public static LocString LEGEND_CABLE_TOOLTIP = "连接储存网络核心与储存建筑。";
                public static LocString LEGEND_CONNECTED_CABLE = "线缆已连接";
                public static LocString LEGEND_CONNECTED_CABLE_TOOLTIP = "这段储存网络线缆已连到储存网络核心。";
                public static LocString LEGEND_DISCONNECTED_CABLE = "线缆未连接";
                public static LocString LEGEND_DISCONNECTED_CABLE_TOOLTIP = "这段储存网络线缆没有连到储存网络核心。";
                public static LocString LEGEND_HUB = "储存网络核心";
                public static LocString LEGEND_HUB_TOOLTIP = "扫描并汇总相连储存建筑。";
                public static LocString LEGEND_STORAGE = "已连接储存";
                public static LocString LEGEND_STORAGE_TOOLTIP = "当前储存网络可共享的储存建筑。";
            }
        }
    }
}
