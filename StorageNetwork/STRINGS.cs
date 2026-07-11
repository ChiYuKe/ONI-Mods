using STRINGS;

namespace StorageNetwork
{
    internal class STRINGS
    {
        public static string Get(LocString value)
        {
            return value.ToString();
        }

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class STORAGENETWORKCORE
                {
                    public static LocString NAME = "储存网络核心";
                    public static LocString DESC = "维持储存网络运行的核心节点。";
                    public static LocString EFFECT = "提供大容量通用储存，并作为储存网络的主要基础设施。";
                }

                public class STORAGENETWORKORDERPRODUCTIONCENTER
                {
                    public static LocString NAME = "订单生产中心";
                    public static LocString DESC = "用于刻录制造台配方并承接订单生产的网络建筑。";
                    public static LocString EFFECT = "点击“刻录”后选择一个带配方的生产建筑，将它的配方刻录到订单生产中心；源建筑会被销毁，同一配方只能刻录一次。";
                }

                public class STORAGENETWORKSMALLSOLIDSERVER
                {
                    public static LocString NAME = "小型固体服务器";
                    public static LocString DESC = "用于接入 <link=\"STORAGENETWORK\">储存网络</link> 的小型固体储存服务器。固体物品需要通过 <link=\"STORAGENETWORKSOLIDINPUTPORT\">材料入网端口</link> 存入网络。";
                    public static LocString EFFECT = "储存固体物品，并显示在 <link=\"STORAGENETWORK\">储存网络</link> 窗口中。复制人可以像普通储物箱一样从这里取出物品；关闭“允许人力操作”后将禁止复制人手动取物。";
                }

                public class STORAGENETWORKSOLIDINPUTPORT
                {
                    public static LocString NAME = "材料入网端口";
                    public static LocString DESC = "将固体运输轨道连接到储存网络的输入端口。";
                    public static LocString EFFECT = "用于接收运输轨道上的固体材料，并作为后续入网逻辑的连接建筑。";
                }

                public class STORAGENETWORKSOLIDOUTPUTPORT
                {
                    public static LocString NAME = "材料出网端口";
                    public static LocString DESC = "将储存网络连接到固体运输轨道的输出端口。";
                    public static LocString EFFECT = "用于向运输轨道输出固体材料，并作为后续出网逻辑的连接建筑。";
                }

                public class STORAGENETWORKLIQUIDINPUTPORT
                {
                    public static LocString NAME = "液体入网端口";
                    public static LocString DESC = "将液体管道连接到储存网络的输入端口。";
                    public static LocString EFFECT = "用于接收管道中的液体，并作为后续入网逻辑的连接建筑。";
                }

                public class STORAGENETWORKLIQUIDOUTPUTPORT
                {
                    public static LocString NAME = "液体出网端口";
                    public static LocString DESC = "将储存网络连接到液体管道的输出端口。";
                    public static LocString EFFECT = "用于向管道输出液体，并作为后续出网逻辑的连接建筑。";
                }

                public class STORAGENETWORKGASINPUTPORT
                {
                    public static LocString NAME = "气体入网端口";
                    public static LocString DESC = "将气体管道连接到储存网络的输入端口。";
                    public static LocString EFFECT = "用于接收管道中的气体，并作为后续入网逻辑的连接建筑。";
                }

                public class STORAGENETWORKGASOUTPUTPORT
                {
                    public static LocString NAME = "气体出网端口";
                    public static LocString DESC = "将储存网络连接到气体管道的输出端口。";
                    public static LocString EFFECT = "用于向管道输出气体，并作为后续出网逻辑的连接建筑。";
                }

                public class STORAGENETWORKPOWERINPUTPORT
                {
                    public static LocString NAME = "电力入网端口";
                    public static LocString DESC = "将电路连接到储存网络的电力输入端口。";
                    public static LocString EFFECT = "用于接入电力网络，并作为后续电力入网逻辑的连接建筑。";
                }

                public class STORAGENETWORKPOWEROUTPUTPORT
                {
                    public static LocString NAME = "电力出网端口";
                    public static LocString DESC = "将储存网络连接到电路的电力输出端口。";
                    public static LocString EFFECT = "用于向电路输出电力，并作为后续电力出网逻辑的连接建筑。";
                }

                public class STORAGENETWORKENERGYSENSOR
                {
                    public static LocString NAME = "储存网络电量传感器";
                    public static LocString DESC = "测量储存网络中所有可访问电池服务器的总电量。";
                    public static LocString EFFECT = "根据储存网络的总充电百分比发送自动化信号。";
                    public static LocString LOGIC_PORT = "储存网络电量";
                    public static LocString LOGIC_PORT_ACTIVE = "需要充电时发送绿色信号";
                    public static LocString LOGIC_PORT_INACTIVE = "不需要充电时发送红色信号";
                }

                public class STORAGENETWORKPARTICLEINPUTPORT
                {
                    public static LocString NAME = "粒子入网端口";
                    public static LocString DESC = "将高能粒子接入储存网络的输入端口。";
                    public static LocString EFFECT = "捕获经过端口的高能粒子，并将粒子存入同星球的粒子服务器。";
                }

                public class STORAGENETWORKPARTICLEOUTPUTPORT
                {
                    public static LocString NAME = "粒子出网端口";
                    public static LocString DESC = "将储存网络中的高能粒子发射出去的输出端口。";
                    public static LocString EFFECT = "从同星球的粒子服务器中取出高能粒子，并向右发射。";
                }

                public class STORAGENETWORKLOGICDIY
                {
                    public static LocString NAME = "储存网络信号输出器";
                    public static LocString DESC = "用于向自动化线路输出储存网络信号的小型自动化建筑。";
                    public static LocString EFFECT = "提供一个自动化信号输出端。";
                    public static LocString LOGIC_PORT = "储存网络信号输出";
                    public static LocString LOGIC_PORT_ACTIVE = "输出绿色信号。";
                    public static LocString LOGIC_PORT_INACTIVE = "输出红色信号。";
                }

                public class STORAGENETWORKSMALLLIQUIDSERVER
                {
                    public static LocString NAME = "小型液体服务器";
                    public static LocString DESC = "用于接入储存网络的小型液体储存服务器。";
                    public static LocString EFFECT = "储存液体物品，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKSMALLGASSERVER
                {
                    public static LocString NAME = "小型气体服务器";
                    public static LocString DESC = "用于接入储存网络的小型气体储存服务器。";
                    public static LocString EFFECT = "储存气体物品，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKSMALLPARTICLESERVER
                {
                    public static LocString NAME = "小型粒子服务器";
                    public static LocString DESC = "用于接入储存网络的小型高能粒子储存服务器。";
                    public static LocString EFFECT = "储存高能粒子，并供粒子出网端口发射。";
                }

                public class STORAGENETWORKSMALLBATTERYSERVER
                {
                    public static LocString NAME = "小型电池服务器";
                    public static LocString DESC = "用于接入储存网络的小型电力储存服务器。";
                    public static LocString EFFECT = "储存电能，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKSMALLCOLDSTORAGESERVER
                {
                    public static LocString NAME = "小型冷库服务器";
                    public static LocString DESC = "用于接入储存网络的小型冷藏储存服务器。";
                    public static LocString EFFECT = "储存食物和食材，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKMEDIUMSOLIDSERVER
                {
                    public static LocString NAME = "中型固体服务器";
                    public static LocString DESC = "用于接入 <link=\"STORAGENETWORK\">储存网络</link> 的中型固体储存服务器。固体物品需要通过 <link=\"STORAGENETWORKSOLIDINPUTPORT\">材料入网端口</link> 存入网络。";
                    public static LocString EFFECT = "储存更多固体物品，并显示在 <link=\"STORAGENETWORK\">储存网络</link> 窗口中。复制人可以像普通储物箱一样从这里取出物品；关闭“允许人力操作”后将禁止复制人手动取物。";
                }

                public class STORAGENETWORKMEDIUMLIQUIDSERVER
                {
                    public static LocString NAME = "中型液体服务器";
                    public static LocString DESC = "用于接入储存网络的中型液体储存服务器。";
                    public static LocString EFFECT = "储存更多液体物品，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKMEDIUMGASSERVER
                {
                    public static LocString NAME = "中型气体服务器";
                    public static LocString DESC = "用于接入储存网络的中型气体储存服务器。";
                    public static LocString EFFECT = "储存更多气体物品，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKMEDIUMPARTICLESERVER
                {
                    public static LocString NAME = "中型粒子服务器";
                    public static LocString DESC = "用于接入储存网络的中型高能粒子储存服务器。";
                    public static LocString EFFECT = "储存更多高能粒子，并供粒子出网端口发射。";
                }

                public class STORAGENETWORKMEDIUMBATTERYSERVER
                {
                    public static LocString NAME = "中型电池服务器";
                    public static LocString DESC = "用于接入储存网络的中型电力储存服务器。";
                    public static LocString EFFECT = "储存更多电能，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKMEDIUMCOLDSTORAGESERVER
                {
                    public static LocString NAME = "中型冷库服务器";
                    public static LocString DESC = "用于接入储存网络的中型冷藏储存服务器。";
                    public static LocString EFFECT = "储存更多食物和食材，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKLARGESOLIDSERVER
                {
                    public static LocString NAME = "大型固体服务器";
                    public static LocString DESC = "用于接入 <link=\"STORAGENETWORK\">储存网络</link> 的大型固体储存服务器。固体物品需要通过 <link=\"STORAGENETWORKSOLIDINPUTPORT\">材料入网端口</link> 存入网络。";
                    public static LocString EFFECT = "储存大量固体物品，并显示在 <link=\"STORAGENETWORK\">储存网络</link> 窗口中。复制人可以像普通储物箱一样从这里取出物品；关闭“允许人力操作”后将禁止复制人手动取物。";
                }

                public class STORAGENETWORKLARGELIQUIDSERVER
                {
                    public static LocString NAME = "大型液体服务器";
                    public static LocString DESC = "用于接入储存网络的大型液体储存服务器。";
                    public static LocString EFFECT = "储存大量液体物品，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKLARGEGASSERVER
                {
                    public static LocString NAME = "大型气体服务器";
                    public static LocString DESC = "用于接入储存网络的大型气体储存服务器。";
                    public static LocString EFFECT = "储存大量气体物品，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKLARGEPARTICLESERVER
                {
                    public static LocString NAME = "大型粒子服务器";
                    public static LocString DESC = "用于接入储存网络的大型高能粒子储存服务器。";
                    public static LocString EFFECT = "储存大量高能粒子，并供粒子出网端口发射。";
                }

                public class STORAGENETWORKLARGEBATTERYSERVER
                {
                    public static LocString NAME = "大型电池服务器";
                    public static LocString DESC = "用于接入储存网络的大型电力储存服务器。";
                    public static LocString EFFECT = "储存大量电能，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKLARGECOLDSTORAGESERVER
                {
                    public static LocString NAME = "大型冷库服务器";
                    public static LocString DESC = "用于接入储存网络的大型冷藏储存服务器。";
                    public static LocString EFFECT = "储存大量食物和食材，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKRELAYMODULE
                {
                    public static LocString NAME = "储存网络中继器";
                    public static LocString DESC = "安装在火箭上的储存网络中继舱。";
                    public static LocString EFFECT = "火箭发射到太空后，允许储存网络跨星球传输物品。";
                }
            }
        }

        public class ITEMS
        {
            public class INDUSTRIAL_PRODUCTS
            {
                public class STORAGE_NETWORK_ENGRAVING_DISK
                {
                    public static LocString NAME = "刻录盘";
                    public static LocString DESC = "用于保存 <link=\"STORAGENETWORKORDERPRODUCTIONCENTER\">订单生产中心</link> 刻录出的配方数据，并为订单生产中心提供 1 个生产核心。";
                    public static LocString RECIPEDESC = "制造一张空白刻录盘，可放入 <link=\"STORAGENETWORKORDERPRODUCTIONCENTER\">订单生产中心</link>，保存刻录出的生产配方，并提供 1 个生产核心。\n\n生产方：<link=\"CRAFTINGTABLE\">工作台</link>、<link=\"SUPERMATERIALREFINERY\">分子熔炉</link>。";
                }
            }
        }

        public class RESEARCH
        {
            public class TREES
            {
                public static LocString TITLE_STORAGENETWORK = "储存网络";
            }

            public class TECHS
            {
                public class STORAGENETWORK
                {
                    public static LocString NAME = "储存网络";
                    public static LocString DESC = "解锁储存网络核心、服务器和跨星球中继设备。";
                }

                public class STORAGENETWORKCORE
                {
                    public static LocString NAME = "储存核心";
                    public static LocString DESC = "解锁储存网络核心。";
                }

                public class STORAGENETWORKPORTS
                {
                    public static LocString NAME = "网络端口";
                    public static LocString DESC = "解锁材料、液体、气体和电力的储存网络入网与出网端口。";
                }

                public class STORAGENETWORKSMALLSTORAGE
                {
                    public static LocString NAME = "小型储存";
                    public static LocString DESC = "解锁小型固体、液体、气体、电池和冷库服务器。";
                }

                public class STORAGENETWORKMEDIUMSTORAGE
                {
                    public static LocString NAME = "中级储存";
                    public static LocString DESC = "解锁中型固体、液体、气体、电池和冷库服务器。";
                }

                public class STORAGENETWORKSIGNAL
                {
                    public static LocString NAME = "网络信号";
                    public static LocString DESC = "解锁可向自动化线路输出储存网络信号的信号输出器和电量传感器。";
                }

                public class STORAGENETWORKORDERPRODUCTION
                {
                    public static LocString NAME = "订单生产中心";
                    public static LocString DESC = "解锁可刻录制造台配方并承接订单生产的订单生产中心。";
                }

                public class STORAGENETWORKLARGESTORAGE
                {
                    public static LocString NAME = "高级储存";
                    public static LocString DESC = "解锁大型固体、液体、气体、电池和冷库服务器。";
                }

                public class STORAGENETWORKRELAY
                {
                    public static LocString NAME = "储存网络中继器";
                    public static LocString DESC = "解锁可用于跨星球传输的储存网络中继舱。";
                }
            }
        }

        public class UI
        {
            public class STORAGE_NETWORK
            {
                public static LocString OVERVIEW_TOOLTIP = "显示当前场景中的储存箱总览。";
                public static LocString TITLE = "储存网络";
                public static LocString SUMMARY_TITLE = "<b>场景储存总览</b>";
                public static LocString SUMMARY_LINE = "储存建筑：{0}    容量：{1} / {2}";
                public static LocString EMPTY_TITLE = "当前场景没有可收集的储存建筑";
                public static LocString EMPTY_DETAILS = "会收集储存网络核心、服务器，以及手动加入的原版储物箱。";
                public static LocString CORE_OFFLINE_TITLE = "储存网络未启动";
                public static LocString CORE_OFFLINE_DETAILS = "当前场景需要建造并供电至少一个储存网络核心，网络面板和材料调度才会启用。";
                public static LocString CROSS_WORLD_RELAY_OFFLINE = "未发射储存网络中继器，无法与其他星球进行网络互通";
                public static LocString NO_STORAGE_CONTENT = "没有储存内容";
                public static LocString STORAGE_SETTINGS = "设置";
                public static LocString PORT_NETWORK_SETTINGS_BUTTON = "储存网络设置";
                public static LocString PORT_NETWORK_SETTINGS_TOOLTIP = "打开储存网络窗口，并进入该端口的设置界面。";
                public static LocString CATEGORY_COUNT = "<b>{0}</b>\n<size=10>{1} 个</size>";
                public static LocString TRANSFER = "转移";
                public static LocString TRANSFER_TOOLTIP = "把这个物品转移到当前场景中的目标储存箱";
                public static LocString DROP = "丢弃";
                public static LocString DROP_TOOLTIP = "丢弃这个储存建筑中的目标物品";
                public static LocString REMAINING_CAPACITY = "剩余 {0}";
                public static LocString SOURCE_FALLBACK = "源";
                public static LocString TARGET_FALLBACK = "目";
                public static LocString LOCATE_SOURCE_TOOLTIP = "定位当前箱子";
                public static LocString LOCATE_TARGET_TOOLTIP = "定位目标箱子";

                public static LocString CATEGORY_VANILLA_STORAGE = "原版储存";
                public static LocString CATEGORY_RECIPE_BUILDING = "生产建筑";
                public static LocString CATEGORY_ENERGY_GENERATOR = "发电设施";
                public static LocString CATEGORY_MOD_STORAGE = "模组建筑";
                public static LocString CATEGORY_INPUT_PORT = "输入端口";
                public static LocString CATEGORY_OUTPUT_PORT = "输出端口";
                public static LocString CATEGORY_MINION = "复制人";
                public static LocString CATEGORY_GEYSER = "泉";
                public static LocString SOURCE_MOD_NAME = "来源：{0}";
                public static LocString SERVER_OFFLINE = "服务器已掉线";
                public static LocString SERVER_OFFLINE_COUNT = "{0} 台服务器已掉线";
                public static LocString HEALTH_CAPACITY = "容量";
                public static LocString HEALTH_REMAINING = "剩余";
                public static LocString HEALTH_ORDERS = "订单";
                public static LocString HEALTH_WAITING = "待料";
                public static LocString HEALTH_ABNORMAL = "异常";
                public static LocString HEALTH_OFFLINE = "掉线";
                public static LocString HEALTH_POWER_STORED = "储电";
                public static LocString HEALTH_POWER_LEAK = "泄露";
                public static LocString VIRTUAL_POWER_ITEM_NAME = "虚拟电力";
                public static LocString VIRTUAL_POWER_ITEM_DETAILS = "容量：{0}    泄露：{1}";
                public static LocString VIRTUAL_POWER_ITEM_TOOLTIP = "电池服务器中的虚拟电力，不是可搬运的实体物品。";

                public static LocString SUMMARY_BUTTON = "汇总";
                public static LocString SUMMARY_TOOLTIP = "汇总当前分类中所有箱子的物品";
                public static LocString HEADER_WINDOW_BUTTON = "窗";
                public static LocString HEADER_WINDOW_TOOLTIP = "显示当前场景中生产建筑可制作的配方。";
                public static LocString HEADER_WINDOW_TITLE = "可制作配方";
                public static LocString RECIPE_WINDOW_HEADER = "当前可下单成品：{0}";
                public static LocString RECIPE_WINDOW_EMPTY = "当前场景中没有可制作配方。\n建造或接入生产建筑后会显示在这里。";
                public static LocString RECIPE_WINDOW_DETAILS = "<b>原料：</b>{0}\n<b>产物：</b>{1}";
                public static LocString RECIPE_PRODUCT_ROUTES = "{0} 条路线";
                public static LocString RECIPE_PRODUCT_STOCK = "网络库存：{0}    单次产出：{1}";
                public static LocString RECIPE_ORDER = "下单";
                public static LocString RECIPE_ORDER_TITLE = "生产订单";
                public static LocString RECIPE_ORDER_RESULT = "单次产出：{0}";
                public static LocString RECIPE_PREVIEW = "预览";
                public static LocString RECIPE_PLAN_TITLE = "执行方案";
                public static LocString RECIPE_PLAN_SUMMARY = "订单总量：{0}    生产批次：{1}";
                public static LocString ORDER_TIME_SUMMARY = "当前周期：{0}    预计完成：{1}";
                public static LocString ORDER_TIME_UNKNOWN = "当前周期：{0}    预计完成：未知（队列无限）";
                public static LocString ORDER_MATERIAL_TITLE = "资源校验";
                public static LocString ORDER_MATERIAL_ROW = "{0}    需要 {1}    库存 {2}    缺口 {3}";
                public static LocString ORDER_MATERIAL_PRODUCED = "缺口将由产线补齐：{0}";
                public static LocString ORDER_PRODUCT_LIST_TITLE = "成品目录";
                public static LocString ORDER_PRODUCT_META = "库存 {0}    {1} 方案";
                public static LocString ORDER_AMOUNT_LABEL = "订单数量";
                public static LocString ORDER_ROUTE_TITLE = "生产设备";
                public static LocString ORDER_CONFIRM = "提交订单";
                public static LocString ORDER_RECIPE_TITLE = "配方方案";
                public static LocString ORDER_ASSIGNMENT_TITLE = "任务分配";
                public static LocString ORDER_CHAIN_TITLE = "产线链路";
                public static LocString ORDER_STATUS_READY = "库存充足";
                public static LocString ORDER_STATUS_PRODUCE = "补产";
                public static LocString ORDER_STATUS_BLOCKED = "缺料";
                public static LocString ORDER_ABNORMAL_NOTIFICATION = "储存网络订单异常";
                public static LocString ORDER_ABNORMAL_NOTIFICATION_TOOLTIP = "以下生产订单已被储存网络自动取消，请检查材料来源、生产建筑或队列状态。\n";
                public static LocString ORDER_ABNORMAL_NOTIFICATION_DETAIL = "• #{0} {1}：目标 {2}，已完成 {3}\n  {4}";
                public static LocString ORDER_ABNORMAL_DEFAULT_REASON = "订单长时间没有进度变动。";
                public static LocString ORDER_CANCEL_MISSING = "订单取消失败：找不到目标订单。";
                public static LocString ORDER_CANCEL_ALREADY_DONE = "订单 #{0} 已经结束，无需取消。";
                public static LocString ORDER_CANCEL_REASON_MANUAL = "用户手动取消。";
                public static LocString ORDER_CANCEL_SUCCESS = "订单追踪：已手动取消订单 #{0}，并释放剩余排队批次。";
                public static LocString ORDER_CLEAR_ABNORMAL_SUCCESS = "订单追踪：已清理 {0} 条异常订单。";
                public static LocString ORDER_CLEAR_COMPLETED_SUCCESS = "订单追踪：已清理 {0} 条已完成订单。";
                public static LocString ORDER_RETRY_MISSING = "订单重试失败：找不到目标订单。";
                public static LocString ORDER_RETRY_INVALID = "订单重试失败：找不到原配方或生产路线。";
                public static LocString ORDER_RETRY_SUCCESS = "订单追踪：已按订单 #{0} 的原参数重新提交。{1}";
                public static LocString ORDER_ABNORMAL_TIMEOUT_REASON = "{0:0.##} 周期内无进度变动，已自动取消建筑排产。最后变动周期 {1}";
                public static LocString ORDER_DISPATCH_TITLE = "调度策略";
                public static LocString ORDER_DISPATCH_SUMMARY = "优先调拨网络库存；缺口由已接入生产建筑补产；提交后自动开启材料请求。";
                public static LocString ENROLLABLE_BUTTON_TOOLTIP = "显示当前场景中所有可接入储存网络的建筑。";
                public static LocString ENROLLABLE_TITLE = "可接入建筑";
                public static LocString ENROLLABLE_HEADER = "当前场景中可接入储存网络的建筑";
                public static LocString ENROLLABLE_EMPTY = "当前场景中没有可接入的建筑。";
                public static LocString ENROLLABLE_CONNECTED = "已接入";
                public static LocString ENROLLABLE_NOT_CONNECTED = "未接入";
                public static LocString ENROLLABLE_CATEGORY_COUNT = "{0} 个";
                public static LocString ENROLLABLE_CATEGORY_OTHER = "其他建筑";
                public static LocString ENROLLABLE_CATEGORY_GEYSER = "泉";
                public static LocString ENROLLABLE_GEYSER_OUTPUT = "{0}  {1}";
                public static LocString ENROLLABLE_WORLD_FILTER = "星球";
                public static LocString ENROLLABLE_WORLD_ALL = "全部星球";
                public static LocString SUMMARY_EMPTY = "当前分类没有储存内容";
                public static LocString SUMMARY_TITLE_LINE = "<b>{0}</b>\n箱子：{1}    总量：{2}";
                public static LocString TREND_NO_DATA = "--/周期";
                public static LocString TREND_ZERO = "0/周期";
                public static LocString TREND_PER_CYCLE = "{0}{1}/周期";

                public static LocString DROP_AMOUNT_TITLE = "丢弃数量";
                public static LocString DROP_AVAILABLE = "当前箱子可丢弃：{0}";
                public static LocString TRANSFER_ITEM_TITLE = "转移物品";
                public static LocString NO_TRANSFER_TARGET = "当前场景中没有可接收物品的目标箱子。";
                public static LocString TARGET_NOT_ENOUGH_CAPACITY = "目标箱子没有足够容量。";
                public static LocString TARGET_CAPACITY_DETAILS = "目标容量：{0} / {1}\n最大可转移：{2}";
                public static LocString TRANSFER_AMOUNT_TITLE = "转移数量";
                public static LocString TARGET_PREFIX = "目标：{0}";
                public static LocString CHANGE_TARGET = "更换目标";
                public static LocString TARGET_SELECTION_TITLE = "选择目标箱子";
                public static LocString TARGET_SELECTION_HEADER = "当前场景中的可接收目标";
                public static LocString CANCEL = "取消";
                public static LocString BUILDING_SETTINGS_TITLE = "建筑设置";
                public static LocString STORAGE_DETAILS = "储存：{0} / {1}\n剩余容量：{2}";
                public static LocString POWER_PORT_STORAGE_DETAILS = "储电缓存：{0} / {1}\n剩余容量：{2}";
                public static LocString POWER_PORT_BATTERY_TITLE = "端口电池";
                public static LocString POWER_PORT_BATTERY_NAME = "本地电量";
                public static LocString COLD_STORAGE_COOLING_TITLE = "冷库降温";
                public static LocString COLD_STORAGE_COOLING_SIDE_SCREEN_TITLE = "冷库降温";
                public static LocString COLD_STORAGE_COOLING_CURRENT = "目标温度";
                public static LocString COLD_STORAGE_COOLING_AMOUNT = "目标温度";
                public static LocString COLD_STORAGE_COOLING_DEFAULT = "默认值";
                public static LocString COLD_STORAGE_COOLING_VALUE = "目标温度：{0}";
                public static LocString COLD_STORAGE_COOLING_DESC = "调整冷库服务器把内容物冷却到的目标温度；储存网络窗口与详情侧屏使用同一数值。";
                public static LocString COLD_STORAGE_COOLING_POWER = "制冷功耗: {0} W";
                public static LocString COLD_STORAGE_COOLING_POWER_TOOLTIP = "主动降温时消耗 {0} W；保温节能时消耗 {1} W。";
                public static LocString POWER_STORAGE_JOULES_STATUS = "可用电力：{JoulesAvailable}/{JoulesCapacity}（{JoulesPercent}）";
                public static LocString POWER_STORAGE_JOULES_TOOLTIP = "当前电池服务器储存的电力。";
                public static LocString POWER_STORAGE_HEAT_STATUS = "产热：{Heat}";
                public static LocString POWER_STORAGE_HEAT_TOOLTIP = "电池服务器运行时产生的热量。";
                public static LocString COLD_STORAGE_COOLING_STATUS = "正在制冷";
                public static LocString COLD_STORAGE_COOLING_STATUS_TOOLTIP = "冷库服务器正在将内容物冷却到目标温度。";
                public static LocString COLD_STORAGE_STEADY_STATUS = "节能模式: {Power}";
                public static LocString COLD_STORAGE_STEADY_STATUS_TOOLTIP = "内容物已经达到目标温度，冷库服务器正在以节能模式维持温度。";
                public static LocString COLD_STORAGE_HEAT_STATUS = "产热: {Heat}/秒";
                public static LocString COLD_STORAGE_HEAT_STATUS_TOOLTIP = "冷库服务器降温时产生的热量。目标温度越低，产热越高。";
                public static LocString LOGIC_DIY_SIDE_SCREEN_TITLE = "信号输出设置";
                public static LocString LOGIC_DIY_EDITOR_TITLE = "逻辑编辑器";
                public static LocString LOGIC_DIY_OUTPUT_MODE = "输出通道";
                public static LocString LOGIC_DIY_CURRENT_MODE = "当前模式：{0}";
                public static LocString LOGIC_DIY_OPEN_SETTINGS = "打开本地逻辑编辑器";
                public static LocString LOGIC_DIY_OPEN_SETTINGS_TOOLTIP = "在浏览器中打开本地节点编辑器，保存后同步到这个信号输出器。";
                public static LocString LOGIC_DIY_CONFIG_PANEL_TITLE = "信号输出器设置";
                public static LocString LOGIC_DIY_SELECT_MODE = "选择";
                public static LocString LOGIC_DIY_SELECTED_MODE = "已选择";
                public static LocString LOGIC_DIY_SINGLE_CHANNEL = "单通道输出";
                public static LocString LOGIC_DIY_SINGLE_CHANNEL_TOOLTIP = "输出 1 位自动化信号，只使用红/绿状态。";
                public static LocString LOGIC_DIY_SINGLE_CHANNEL_DESC = "当前模式：单通道。输出值会限制为 0 或 1。";
                public static LocString LOGIC_DIY_FOUR_CHANNEL = "4 通道输出";
                public static LocString LOGIC_DIY_FOUR_CHANNEL_TOOLTIP = "输出 4 位自动化信号，可用于信号带网络。";
                public static LocString LOGIC_DIY_FOUR_CHANNEL_DESC = "当前模式：4 通道。输出值可使用 0 到 15 的四位信号。";
                public static LocString LOGIC_DIY_CURRENT_VALUE_SINGLE = "固定输出：{0}";
                public static LocString LOGIC_DIY_CURRENT_VALUE_FOUR = "固定输出：Value {0} / Binary {1}";
                public static LocString LOGIC_DIY_OUTPUT_ON = "Port0：ON";
                public static LocString LOGIC_DIY_OUTPUT_ON_DESC = "输出绿色信号。";
                public static LocString LOGIC_DIY_OUTPUT_ON_TOOLTIP = "固定输出 ON。";
                public static LocString LOGIC_DIY_OUTPUT_OFF = "Port0：OFF";
                public static LocString LOGIC_DIY_OUTPUT_OFF_DESC = "输出红色信号。";
                public static LocString LOGIC_DIY_OUTPUT_OFF_TOOLTIP = "固定输出 OFF。";
                public static LocString LOGIC_DIY_VALUE_TITLE = "Value：{0}";
                public static LocString LOGIC_DIY_VALUE_BINARY = "Binary：{0}";
                public static LocString LOGIC_DIY_VALUE_TOOLTIP = "固定输出 Value {0}，Binary {1}。";
                public static LocString LOGIC_DIY_SOURCE_FIXED = "固定输出";
                public static LocString LOGIC_DIY_SOURCE_FIXED_DESC = "手动选择固定的自动化输出值。";
                public static LocString LOGIC_DIY_SOURCE_FIXED_TOOLTIP = "不读取网络库存，直接输出指定值。";
                public static LocString LOGIC_DIY_SOURCE_CONDITION = "材料条件输出";
                public static LocString LOGIC_DIY_SOURCE_CONDITION_DESC = "根据储存网络中的材料数量判断并输出信号。";
                public static LocString LOGIC_DIY_SOURCE_CONDITION_TOOLTIP = "选择一种网络材料，设置大于等于或小于阈值后输出。";
                public static LocString LOGIC_DIY_MATERIAL = "判断材料";
                public static LocString LOGIC_DIY_MATERIAL_VALUE = "{0}  当前 {1}";
                public static LocString LOGIC_DIY_MATERIAL_CURRENT = "当前 {0}";
                public static LocString LOGIC_DIY_MATERIAL_TOOLTIP = "选择储存网络服务器中已有的材料。";
                public static LocString LOGIC_DIY_MATERIAL_NONE = "未选择材料";
                public static LocString LOGIC_DIY_MATERIAL_NONE_DESC = "当前网络没有可选择的材料，或材料尚未进入服务器。";
                public static LocString LOGIC_DIY_MATERIAL_PICKER_TITLE = "选择判断材料";
                public static LocString LOGIC_DIY_MATERIAL_PICKER_COUNT = "可选材料：{0} 个";
                public static LocString LOGIC_DIY_MATERIAL_PICKER_HINT = "列表来自当前同星球储存网络服务器中的内容物；跨星球中继在线时会包含可达星球。";
                public static LocString LOGIC_DIY_COMPARE_GE = "条件：大于等于";
                public static LocString LOGIC_DIY_COMPARE_GE_DESC = "材料数量 >= 阈值时输出。";
                public static LocString LOGIC_DIY_COMPARE_GE_TOOLTIP = "当选中材料数量大于等于阈值时，指定通道输出 ON。";
                public static LocString LOGIC_DIY_COMPARE_LT = "条件：小于";
                public static LocString LOGIC_DIY_COMPARE_LT_DESC = "材料数量 < 阈值时输出。";
                public static LocString LOGIC_DIY_COMPARE_LT_TOOLTIP = "当选中材料数量小于阈值时，指定通道输出 ON。";
                public static LocString LOGIC_DIY_THRESHOLD = "变量值";
                public static LocString LOGIC_DIY_THRESHOLD_TOOLTIP = "选择用于节点图比较的数值。";
                public static LocString LOGIC_DIY_THRESHOLD_PICKER_TITLE = "选择变量值";
                public static LocString LOGIC_DIY_THRESHOLD_CURRENT = "当前变量值：{0}";
                public static LocString LOGIC_DIY_THRESHOLD_PICKER_HINT = "第一版先提供常用档位；后续会接入储存网络已有的数字输入弹窗。";
                public static LocString LOGIC_DIY_THRESHOLD_OPTION_DESC = "设为材料判断阈值。";
                public static LocString LOGIC_DIY_OUTPUT_CHANNEL = "Port{0}";
                public static LocString LOGIC_DIY_OUTPUT_CHANNEL_DESC = "条件成立时输出数值 {0}。";
                public static LocString LOGIC_DIY_OUTPUT_CHANNEL_TOOLTIP = "4 通道模式下只点亮这个 bit，条件不成立时输出 0。";
                public static LocString LOGIC_DIY_OUTPUT_CHANNEL_SINGLE = "Port0";
                public static LocString LOGIC_DIY_OUTPUT_CHANNEL_SINGLE_DESC = "单通道模式下条件成立输出 ON，不成立输出 OFF。";
                public static LocString LOGIC_DIY_OUTPUT_CHANNEL_SINGLE_TOOLTIP = "单通道只有 Port0。";
                public static LocString LOGIC_DIY_CURRENT_CONDITION = "{0} 当前 {1}，条件 {2} {3}，输出 {4}";
                public static LocString LOGIC_DIY_SETTING_SOURCE = "输出方式";
                public static LocString LOGIC_DIY_SETTING_SOURCE_TOOLTIP = "选择固定输出或根据网络材料条件输出。";
                public static LocString LOGIC_DIY_SETTING_CHANNEL_MODE = "通道模式";
                public static LocString LOGIC_DIY_SETTING_CHANNEL_MODE_TOOLTIP = "选择单通道或 4 通道输出。";
                public static LocString LOGIC_DIY_SETTING_FIXED_VALUE = "固定输出值";
                public static LocString LOGIC_DIY_SETTING_MATERIAL = "判断材料";
                public static LocString LOGIC_DIY_SETTING_COMPARE = "判断条件";
                public static LocString LOGIC_DIY_SETTING_COMPARE_TOOLTIP = "选择材料数量与阈值的比较方式。";
                public static LocString LOGIC_DIY_SETTING_THRESHOLD = "变量值";
                public static LocString LOGIC_DIY_SETTING_OUTPUT_PORT = "输出端口";
                public static LocString LOGIC_DIY_MODULE_LIBRARY = "模块";
                public static LocString LOGIC_DIY_CONNECTION_GRAPH = "条件连接";
                public static LocString LOGIC_DIY_BLUEPRINT_HINT = "从左侧拖入模块到画布；点击节点端点可连接模块。";
                public static LocString LOGIC_DIY_SOURCE_MODULES = "输出";
                public static LocString LOGIC_DIY_MATH_MODULES = "数学";
                public static LocString LOGIC_DIY_COMPARE_MODULES = "比较";
                public static LocString LOGIC_DIY_BOOL_MODULES = "布尔";
                public static LocString LOGIC_DIY_FUTURE_MODULES = "扩展模块";
                public static LocString LOGIC_DIY_LUA_MODULE = "Lua 模块";
                public static LocString LOGIC_DIY_LUA_MODULE_DESC = "后续用于脚本控制输出。";
                public static LocString LOGIC_DIY_TIMER_MODULE = "定时模块";
                public static LocString LOGIC_DIY_TIMER_MODULE_DESC = "后续用于动画、循环和波形输出。";
                public static LocString LOGIC_DIY_NODE_MATERIAL = "材料数据源";
                public static LocString LOGIC_DIY_NODE_COMPARE = "变量模块";
                public static LocString LOGIC_DIY_NODE_FIXED = "固定输出模块";
                public static LocString LOGIC_DIY_NODE_OUTPUT = "输出模块";
                public static LocString LOGIC_DIY_NODE_ADD = "加法";
                public static LocString LOGIC_DIY_NODE_ADD_DESC = "两个数值相加。";
                public static LocString LOGIC_DIY_NODE_SUBTRACT = "减法";
                public static LocString LOGIC_DIY_NODE_SUBTRACT_DESC = "左输入减右输入。";
                public static LocString LOGIC_DIY_NODE_MULTIPLY = "乘法";
                public static LocString LOGIC_DIY_NODE_MULTIPLY_DESC = "两个数值相乘。";
                public static LocString LOGIC_DIY_NODE_DIVIDE = "除法";
                public static LocString LOGIC_DIY_NODE_DIVIDE_DESC = "左输入除以右输入。";
                public static LocString LOGIC_DIY_NODE_GREATER = "大于";
                public static LocString LOGIC_DIY_NODE_GREATER_DESC = "A > B 时为真。";
                public static LocString LOGIC_DIY_NODE_EQUAL = "等于";
                public static LocString LOGIC_DIY_NODE_EQUAL_DESC = "A = B 时为真。";
                public static LocString LOGIC_DIY_NODE_LESS = "小于";
                public static LocString LOGIC_DIY_NODE_LESS_DESC = "A < B 时为真。";
                public static LocString LOGIC_DIY_NODE_BOOL_TRUE = "TRUE";
                public static LocString LOGIC_DIY_NODE_BOOL_TRUE_DESC = "布尔真常量。";
                public static LocString LOGIC_DIY_NODE_BOOL_FALSE = "FALSE";
                public static LocString LOGIC_DIY_NODE_BOOL_FALSE_DESC = "布尔假常量。";
                public static LocString LOGIC_DIY_NODE_BOOL_AND = "与";
                public static LocString LOGIC_DIY_NODE_BOOL_AND_DESC = "两个输入都为真。";
                public static LocString LOGIC_DIY_NODE_BOOL_OR = "或";
                public static LocString LOGIC_DIY_NODE_BOOL_OR_DESC = "任意输入为真。";
                public static LocString LOGIC_DIY_NODE_BOOL_NOT = "非";
                public static LocString LOGIC_DIY_NODE_BOOL_NOT_DESC = "反转一个布尔输入。";
                public static LocString LOGIC_DIY_CONNECT_TO_COMPARE = "变量";
                public static LocString LOGIC_DIY_CONNECT_TO_OUTPUT = "输出";
                public static LocString PRODUCTION_STATUS_TITLE = "运行状态";
                public static LocString PRODUCTION_CONTENT_TITLE = "内容物";
                public static LocString PRODUCTION_STATUS_IDLE = "当前状态：待机";
                public static LocString PRODUCTION_STATUS_CRAFTING = "当前状态：正在制作";
                public static LocString PRODUCTION_STATUS_WAITING_WORKER = "当前状态：等待复制人";
                public static LocString PRODUCTION_CURRENT_RECIPE = "当前配方：{0}";
                public static LocString PRODUCTION_PROGRESS = "制作进度：{0}%";
                public static LocString PRODUCTION_NO_RECIPE = "当前没有正在制作的配方";
                public static LocString MATERIAL_REQUEST_TITLE = "材料请求";
                public static LocString MATERIAL_REQUEST_ENABLED = "向网络请求材料";
                public static LocString PORT_OUTPUT_AMOUNT = "输出量";
                public static LocString PORT_OUTPUT_AMOUNT_VALUE = "{0}/次";
                public static LocString PORT_OUTPUT_AMOUNT_TOOLTIP = "输出端口会尽量按 {0} 形成管道或轨道输出包。";
                public static LocString MATERIAL_PORT_SETTINGS_TITLE = "端口设置";
                public static LocString MATERIAL_PORT_INPUT_STATUS = "材料输入端口";
                public static LocString MATERIAL_PORT_OUTPUT_STATUS = "材料输出端口";
                public static LocString LIQUID_PORT_INPUT_STATUS = "液体输入端口";
                public static LocString LIQUID_PORT_OUTPUT_STATUS = "液体输出端口";
                public static LocString GAS_PORT_INPUT_STATUS = "气体输入端口";
                public static LocString GAS_PORT_OUTPUT_STATUS = "气体输出端口";
                public static LocString POWER_PORT_INPUT_STATUS = "电力输入端口";
                public static LocString POWER_PORT_OUTPUT_STATUS = "电力输出端口";
                public static LocString PARTICLE_PORT_INPUT_STATUS = "粒子输入端口";
                public static LocString PARTICLE_PORT_OUTPUT_STATUS = "粒子输出端口";
                public static LocString MATERIAL_PORT_DIRECTION = "端口方向";
                public static LocString MATERIAL_PORT_DIRECTION_INPUT = "输入网络";
                public static LocString MATERIAL_PORT_DIRECTION_OUTPUT = "输出网络";
                public static LocString MATERIAL_PORT_MANUAL_OPERATION_ALLOWED = "允许人力操作";
                public static LocString PORT_STATUS_ITEM_NAME = "储存网络端口";
                public static LocString PORT_STATUS_ITEM_TOOLTIP = "{0}";
                public static LocString PORT_STATUS_ONLINE = "网络：在线";
                public static LocString PORT_STATUS_OFFLINE = "网络：离线（当前星球没有在线核心）";
                public static LocString PORT_STATUS_SHORT_ONLINE = "在线";
                public static LocString PORT_STATUS_SHORT_OFFLINE = "离线";
                public static LocString PORT_STATUS_DIRECTION = "方向：{0}";
                public static LocString PORT_STATUS_CACHE = "端口缓存：{0} / {1}";
                public static LocString PORT_STATUS_REMAINING = "剩余容量：{0}";
                public static LocString PORT_STATUS_FILTERS = "筛选：{0}";
                public static LocString PORT_STATUS_FILTERS_EMPTY = "未选择物品";
                public static LocString PORT_STATUS_FILTERS_ANY = "不限制";
                public static LocString PORT_STATUS_MANUAL = "人力操作：{0}";
                public static LocString PORT_STATUS_MANUAL_ALLOWED = "允许";
                public static LocString PORT_STATUS_MANUAL_FORBIDDEN = "禁止";
                public static LocString PORT_STATUS_INPUT_ENABLED = "内容物入网：{0}";
                public static LocString PORT_STATUS_OUTPUT_ENABLED = "请求输出：{0}";
                public static LocString PORT_STATUS_INPUT_SUMMARY = "内容物入网：{0}  {1}";
                public static LocString PORT_STATUS_OUTPUT_SUMMARY = "请求输出：{0}  {1}";
                public static LocString PORT_STATUS_POLICY = "存放策略：{0}";
                public static LocString PORT_STATUS_SOURCE_POLICY = "来源策略：{0}";
                public static LocString PORT_STATUS_REQUEST = "网络调度：{0}";
                public static LocString PORT_STATUS_BUFFER = "复制人取货缓冲：{0}";
                public static LocString PORT_STATUS_OUTPUT_AMOUNT = "输出包目标：{0}";
                public static LocString INPUT_PORT_STORE_TITLE = "内容物入网";
                public static LocString INPUT_PORT_STORE_ENABLED = "向网络输入";
                public static LocString INPUT_PORT_STORE_DESC = "开启后，端口会把自身缓存中的内容物自动存入储存网络中的匹配服务器。";
                public static LocString INPUT_PORT_STORE_STATUS = "入网状态：{0}";
                public static LocString OUTPUT_PORT_REQUEST_TITLE = "内容物出网";
                public static LocString OUTPUT_PORT_REQUEST_ENABLED = "向管道输出";
                public static LocString OUTPUT_PORT_REQUEST_DESC = "开启后，端口会从储存网络中的匹配服务器请求液体，并输出到连接的液体管道。";
                public static LocString OUTPUT_PORT_REQUEST_STATUS = "出网状态：{0}";
                public static LocString OUTPUT_PORT_SOURCE_POLICY = "来源策略";
                public static LocString OUTPUT_PORT_FILTER = "输出筛选";
                public static LocString OUTPUT_PORT_FILTER_ANY = "任意液体";
                public static LocString OUTPUT_PORT_FILTER_SELECT = "选择输出液体";
                public static LocString OUTPUT_PORT_FILTER_DESC = "选择当前网络服务器中已有的液体；端口只会向管道输出所选液体。";
                public static LocString OUTPUT_PORT_FILTER_EMPTY = "当前网络服务器中没有可输出液体";
                public static LocString MATERIAL_OUTPUT_PORT_REQUEST_TITLE = "材料出网";
                public static LocString MATERIAL_OUTPUT_PORT_REQUEST_ENABLED = "向轨道输出";
                public static LocString MATERIAL_OUTPUT_PORT_REQUEST_DESC = "开启后，端口会从储存网络中的匹配服务器请求材料，并输出到连接的运输轨道。";
                public static LocString MATERIAL_OUTPUT_PORT_REQUEST_STATUS = "出网状态：{0}";
                public static LocString MATERIAL_OUTPUT_PORT_FILTER_ANY = "任意材料";
                public static LocString MATERIAL_OUTPUT_PORT_FILTER_SELECT = "选择输出材料";
                public static LocString MATERIAL_OUTPUT_PORT_FILTER_DESC = "选择当前网络服务器中已有的材料；端口只会向运输轨道输出所选材料。";
                public static LocString MATERIAL_OUTPUT_PORT_FILTER_EMPTY = "当前网络服务器中没有可输出材料";
                public static LocString MATERIAL_OUTPUT_PORT_MANUAL_ALLOWED_TOOLTIP = "允许复制人从材料出网端口缓存中取货。";
                public static LocString MATERIAL_OUTPUT_PORT_MANUAL_FORBIDDEN_TOOLTIP = "禁止复制人从材料出网端口缓存中取货。";
                public static LocString OUTPUT_PORT_LIMIT_ENABLED = "启用输出限额";
                public static LocString OUTPUT_PORT_LIMIT = "输出限额：{0} / {1}";
                public static LocString OUTPUT_PORT_SET_LIMIT = "设置限额";
                public static LocString OUTPUT_PORT_REQUEST_RATE = "请求速率";
                public static LocString OUTPUT_PORT_OUTPUT_RATE = "输出速率";
                public static LocString OUTPUT_PORT_REQUEST_RATE_VALUE = "{0}/秒";
                public static LocString OUTPUT_PORT_SET_REQUEST_RATE = "设置请求速率";
                public static LocString POWER_INPUT_PORT_STORE_TITLE = "电力入网";
                public static LocString POWER_INPUT_PORT_STORE_ENABLED = "向网络输入";
                public static LocString POWER_INPUT_PORT_STORE_DESC = "开启后，端口会把连接电路中的外部电力存入储存网络中的电池服务器。";
                public static LocString POWER_INPUT_PORT_STORE_AUTO_DESC = "自动存入已接入网络且仍有容量的电池服务器。";
                public static LocString POWER_OUTPUT_PORT_REQUEST_TITLE = "电力出网";
                public static LocString POWER_OUTPUT_PORT_REQUEST_ENABLED = "向电路输出";
                public static LocString POWER_OUTPUT_PORT_REQUEST_DESC = "开启后，端口会从储存网络中的电池服务器取电，并输出到连接的电路。";
                public static LocString POWER_OUTPUT_PORT_SOURCE_AUTO_DESC = "自动从已接入网络的电池服务器中取电。";
                public static LocString POWER_OUTPUT_PORT_LIMIT = "输出限额：{0} / {1}";
                public static LocString POWER_OUTPUT_PORT_SET_LIMIT = "设置输出限额";
                public static LocString POWER_OUTPUT_PORT_LIMIT_LABEL = "电力限额";
                public static LocString POWER_PORT_RATE = "电力速率";
                public static LocString POWER_PORT_INPUT_RATE = "输入速率";
                public static LocString POWER_PORT_OUTPUT_RATE = "输出速率";
                public static LocString POWER_PORT_RATE_VALUE = "{0:0.#} W";
                public static LocString POWER_PORT_SET_RATE = "设置电力速率";
                public static LocString POWER_INPUT_PORT_RATE_TOOLTIP = "设置电力入网端口从外部电路存入网络的最大速率：{0}";
                public static LocString POWER_OUTPUT_PORT_RATE_TOOLTIP = "设置电力出网端口向外部电路输出的最大速率：{0}";
                public static LocString POWER_INPUT_PORT_STATUS_ITEM = "电力入网端口：{0}";
                public static LocString POWER_INPUT_PORT_STATUS_TOOLTIP = "向网络输入：{0}\n网络：{1}\n存放策略：{2}\n输入速率：{3}\n端口缓存：{4} / {5}\n入网状态：{6}";
                public static LocString POWER_OUTPUT_PORT_STATUS_ITEM = "电力出网端口：{0}";
                public static LocString POWER_OUTPUT_PORT_STATUS_TOOLTIP = "向电路输出：{0}\n网络：{1}\n来源策略：{2}\n输出限额：{3}\n输出速率：{4}\n端口缓存：{5} / {6}\n出网状态：{7}";
                public static LocString PARTICLE_PORT_STORAGE_TITLE = "网络粒子储存";
                public static LocString PARTICLE_PORT_ITEM_NAME = "高能粒子";
                public static LocString PARTICLE_PORT_AMOUNT_VALUE = "{0} 粒子";
                public static LocString PARTICLE_PORT_ITEM_TOOLTIP = "粒子服务器中的高能粒子，不是可搬运的实体物品。";
                public static LocString PARTICLE_INPUT_PORT_STORE_TITLE = "粒子入网";
                public static LocString PARTICLE_INPUT_PORT_STORE_ENABLED = "向网络输入";
                public static LocString PARTICLE_INPUT_PORT_MODE = "接收模式";
                public static LocString PARTICLE_PORT_CAPTURE_MODE = "捕获经过端口的高能粒子";
                public static LocString PARTICLE_INPUT_PORT_STORE_DESC = "端口会捕获经过的高能粒子，并存入同星球已接入网络的粒子服务器。";
                public static LocString PARTICLE_OUTPUT_PORT_REQUEST_TITLE = "粒子出网";
                public static LocString PARTICLE_OUTPUT_PORT_REQUEST_ENABLED = "向管线发射";
                public static LocString PARTICLE_OUTPUT_PORT_THRESHOLD = "发射阈值";
                public static LocString PARTICLE_OUTPUT_PORT_SET_THRESHOLD = "设置发射阈值";
                public static LocString PARTICLE_OUTPUT_PORT_DIRECTION = "发射方向";
                public static LocString PARTICLE_OUTPUT_PORT_SELECT_DIRECTION = "选择发射方向";
                public static LocString PARTICLE_OUTPUT_PORT_DIRECTION_DESC = "选择粒子出网端口发射高能粒子的方向。";
                public static LocString PARTICLE_OUTPUT_PORT_LIMIT = "输出限额：{0} / {1}";
                public static LocString PARTICLE_OUTPUT_PORT_SET_LIMIT = "设置粒子限额";
                public static LocString PARTICLE_OUTPUT_PORT_LIMIT_LABEL = "粒子限额";
                public static LocString PARTICLE_OUTPUT_PORT_ENABLE_BUTTON = "开启粒子发射";
                public static LocString PARTICLE_OUTPUT_PORT_DISABLE_BUTTON = "关闭粒子发射";
                public static LocString PARTICLE_OUTPUT_PORT_ENABLE_TOOLTIP = "允许粒子出网端口从网络中取出高能粒子并发射。";
                public static LocString PARTICLE_OUTPUT_PORT_DISABLE_TOOLTIP = "停止粒子出网端口发射高能粒子。";
                public static LocString PARTICLE_OUTPUT_PORT_SOURCE_AUTO_DESC = "自动从已接入网络的粒子服务器中取出高能粒子。";
                public static LocString PARTICLE_OUTPUT_PORT_REQUEST_DESC = "端口会从同星球的粒子服务器取出高能粒子，并按当前方向发射。方向、阈值和限额可在这里或建筑侧屏调整。";
                public static LocString PARTICLE_DIRECTION_UP = "上";
                public static LocString PARTICLE_DIRECTION_DOWN = "下";
                public static LocString PARTICLE_DIRECTION_LEFT = "左";
                public static LocString PARTICLE_DIRECTION_RIGHT = "右";
                public static LocString PARTICLE_DIRECTION_UP_LEFT = "左上";
                public static LocString PARTICLE_DIRECTION_UP_RIGHT = "右上";
                public static LocString PARTICLE_DIRECTION_DOWN_LEFT = "左下";
                public static LocString PARTICLE_DIRECTION_DOWN_RIGHT = "右下";
                public static LocString POWER_STATUS_WAITING_EXTERNAL = "等待外部供电";
                public static LocString POWER_STATUS_NO_CAPACITY = "电池服务器已满";
                public static LocString POWER_STATUS_NO_STORED = "网络电力不足";
                public static LocString POWER_STATUS_STORED = "当前输入 {0}";
                public static LocString POWER_STATUS_OUTPUT = "已输出 {0}";
                public static LocString POWER_STATUS_BUFFER_FULL = "端口电池已满";
                public static LocString POWER_STATUS_CHARGED = "当前充入 {0}";
                public static LocString POWER_STATUS_REFILLED = "已从网络取电 {0}";
                public static LocString ENERGY_SENSOR_SIDE_SCREEN_TITLE = "储存网络电量范围";
                public static LocString ENERGY_SENSOR_HIGH_THRESHOLD = "停止充电";
                public static LocString ENERGY_SENSOR_LOW_THRESHOLD = "开始充电";
                public static LocString ENERGY_SENSOR_HIGH_THRESHOLD_TOOLTIP = "当储存网络电量达到此百分比时发送红色信号。";
                public static LocString ENERGY_SENSOR_LOW_THRESHOLD_TOOLTIP = "当储存网络电量降至此百分比时发送绿色信号。";
                public static LocString ENERGY_SENSOR_STATUS_ITEM = "网络电量：{StoredJoules}/{CapacityJoules}（{Percent}）\n自动化：{Signal}";
                public static LocString ENERGY_SENSOR_STATUS_TOOLTIP = "所有当前可访问电池服务器中储存的总电量。";
                public static LocString ENERGY_SENSOR_NETWORK_OFFLINE = "网络离线";
                public static LocString ENERGY_SENSOR_NO_CAPACITY = "没有可访问的电池服务器";
                public static LocString ENERGY_SENSOR_SIGNAL_GREEN = "需要充电";
                public static LocString ENERGY_SENSOR_SIGNAL_RED = "停止充电";
                public static LocString SOLID_INPUT_PORT_STATUS_ITEM = "固体入网端口：{0}";
                public static LocString SOLID_INPUT_PORT_STATUS_TOOLTIP = "向网络输入：{0}\n网络：{1}\n存放策略：{2}\n端口缓存：{3} / {4}\n入网状态：{5}";
                public static LocString SOLID_OUTPUT_PORT_STATUS_ITEM = "固体出网端口：{0}";
                public static LocString SOLID_OUTPUT_PORT_STATUS_TOOLTIP = "向轨道输出：{0}\n网络：{1}\n来源策略：{2}\n输出筛选：{3}\n输出限额：{4}\n请求速率：{5}\n端口缓存：{6} / {7}\n出网状态：{8}";
                public static LocString LIQUID_INPUT_PORT_STATUS_ITEM = "液体入网端口：{0}";
                public static LocString LIQUID_INPUT_PORT_STATUS_TOOLTIP = "向网络输入：{0}\n网络：{1}\n存放策略：{2}\n端口缓存：{3} / {4}\n入网状态：{5}";
                public static LocString LIQUID_OUTPUT_PORT_STATUS_ITEM = "液体出网端口：{0}";
                public static LocString LIQUID_OUTPUT_PORT_STATUS_TOOLTIP = "向管道输出：{0}\n网络：{1}\n来源策略：{2}\n输出筛选：{3}\n输出限额：{4}\n请求速率：{5}\n端口缓存：{6} / {7}\n出网状态：{8}";
                public static LocString GAS_INPUT_PORT_STORE_TITLE = "气体入网";
                public static LocString GAS_INPUT_PORT_STORE_ENABLED = "向网络输入";
                public static LocString GAS_INPUT_PORT_STORE_DESC = "开启后，端口会把气体管道输入的气体存入储存网络中的匹配服务器。";
                public static LocString GAS_INPUT_PORT_STORE_STATUS = "入网状态：{0}";
                public static LocString GAS_OUTPUT_PORT_REQUEST_TITLE = "气体出网";
                public static LocString GAS_OUTPUT_PORT_REQUEST_ENABLED = "向管道输出";
                public static LocString GAS_OUTPUT_PORT_REQUEST_DESC = "开启后，端口会从储存网络中的匹配服务器请求气体，并输出到连接的气体管道。";
                public static LocString GAS_OUTPUT_PORT_REQUEST_STATUS = "出网状态：{0}";
                public static LocString GAS_OUTPUT_PORT_FILTER_ANY = "任意气体";
                public static LocString GAS_OUTPUT_PORT_FILTER_SELECT = "选择输出气体";
                public static LocString GAS_OUTPUT_PORT_FILTER_DESC = "选择当前网络服务器中已有的气体；端口只会向管道输出所选气体。";
                public static LocString GAS_OUTPUT_PORT_FILTER_EMPTY = "当前网络服务器中没有可输出气体";
                public static LocString GAS_INPUT_PORT_STATUS_ITEM = "气体入网端口：{0}";
                public static LocString GAS_INPUT_PORT_STATUS_TOOLTIP = "向网络输入：{0}\n网络：{1}\n存放策略：{2}\n端口缓存：{3} / {4}\n入网状态：{5}";
                public static LocString GAS_OUTPUT_PORT_STATUS_ITEM = "气体出网端口：{0}";
                public static LocString GAS_OUTPUT_PORT_STATUS_TOOLTIP = "向管道输出：{0}\n网络：{1}\n来源策略：{2}\n输出筛选：{3}\n输出限额：{4}\n请求速率：{5}\n端口缓存：{6} / {7}\n出网状态：{8}";
                public static LocString GAS_OUTPUT_SIDE_SCREEN_TITLE = "气体输出筛选";
                public static LocString MATERIAL_REQUEST_MODE = "请求方式：{0}";
                public static LocString MATERIAL_REQUEST_MODE_SEARCH = "从服务器中寻找材料";
                public static LocString MATERIAL_REQUEST_MODE_SPECIFIC = "指定网络中某个服务器提供材料";
                public static LocString MATERIAL_REQUEST_SOURCE = "指定服务器：{0}";
                public static LocString MATERIAL_REQUEST_SOURCE_NONE = "未指定";
                public static LocString MATERIAL_REQUEST_SELECT_SOURCE = "选择服务器";
                public static LocString MATERIAL_REQUEST_LIMIT_ENABLED = "启用请求限额";
                public static LocString MATERIAL_REQUEST_LIMIT = "限额：{0} / {1}";
                public static LocString MATERIAL_REQUEST_SET_LIMIT = "设置限额";
                public static LocString MATERIAL_REQUEST_STATUS = "请求状态：{0}";
                public static LocString MATERIAL_REQUEST_RESET = "重置已请求";
                public static LocString MATERIAL_REQUEST_STATUS_ITEM = "材料提供由储存网络供给";
                public static LocString MATERIAL_REQUEST_STATUS_TOOLTIP = "这个建筑会从储存网络中请求当前生产需要的材料。\n{0}";
                public static LocString MATERIAL_REQUEST_AUTO_DESC = "自动从已接入网络的服务器中寻找材料，优先使用库存最多的来源。";
                public static LocString ENERGY_GENERATOR_SOURCE_DESC = "发电设施会从已接入网络的服务器中自动寻找可用燃料。";
                public static LocString OUTPUT_STORE_TITLE = "成品入网";
                public static LocString OUTPUT_STORE_ENABLED = "加工完成后自动存入网络";
                public static LocString STORAGE_OUTPUT_STORE_TITLE = "内容物入网";
                public static LocString STORAGE_OUTPUT_STORE_ENABLED = "允许把内容物输入网络";
                public static LocString STORAGE_OUTPUT_STORE_DESC = "开启后，这个储存建筑会把当前内容物自动转移到储存网络中的匹配服务器。";
                public static LocString OUTPUT_STORE_MODE = "存放方式：{0}";
                public static LocString OUTPUT_STORE_MODE_AUTO = "自动寻找匹配服务器";
                public static LocString OUTPUT_STORE_MODE_SPECIFIC = "指定网络中某个服务器存放";
                public static LocString OUTPUT_STORE_AUTO_DESC = "自动寻找已接入网络的匹配服务器，优先堆到已有同类物品的服务器；容量不足时会按剩余空间分批存入。";
                public static LocString OUTPUT_STORE_STATUS = "入网状态：{0}";
                public static LocString OUTPUT_STORE_TARGET = "目标服务器：{0}";
                public static LocString OUTPUT_STORE_TARGET_DETAILS = "当前 {0} / {1}    剩余 {2}";
                public static LocString PICKER_OPTION_COUNT = "可选网络服务器：{0} 个";
                public static LocString PICKER_POLICY_HINT = "自动策略会在已接入网络的服务器中寻找合适目标；指定服务器后，只从该服务器取料或向该服务器存放。";
                public static LocString CLOSE = "关闭";
                public static LocString AMOUNT_TITLE = "数量";
                public static LocString AMOUNT_LABEL = "数量：{0}";
                public static LocString ALL = "全部";
                public static LocString CONFIRM = "确定";
                public static LocString ON = "开";
                public static LocString AMOUNT_INPUT = "输入数量";
                public static LocString OFF = "关";
                public static LocString RESET_DEFAULTS = "默认值";
                public static LocString INFO = "信息";
                public static LocString RESTART = "重启";
                public static LocString CONTINUE = "继续";
                public static LocString OPTIONS_BUTTON = "选项";
                public static LocString ENROLLABLE_SHORT = "可接入";
                public static LocString RECIPE_SHORT = "配方";
                public static LocString CONFIG_TITLE = "StorageNetwork 选项";
                public static LocString CONFIG_TOOLTIP = "调整 StorageNetwork 模组数值";
                public static LocString CONFIG_HINT = "保存后会写入 StorageNetworkConfig.json。建筑容量等部分数值需要重进存档或重建建筑才会完全体现。";
                public static LocString CONFIG_RESTART_REQUIRED = "要使这些选项生效，必须重新启动游戏。";
                public static LocString CONFIG_TOGGLE_ON = "开启";
                public static LocString CONFIG_TOGGLE_OFF = "关闭";
                public static LocString CONFIG_RESTART_DIALOG_TITLE = "信息";
                public static LocString CONFIG_RESTART_CONTINUE = "继续";
                public static LocString CONFIG_DEFAULT_MATERIAL_LIMIT = "材料请求默认限额 kg";
                public static LocString CONFIG_DEFAULT_MATERIAL_LIMIT_DESC = "新接入生产建筑的默认请求限额。";
                public static LocString CONFIG_GEYSER_WORLD_OUTPUT_FALLBACK = "允许泉在网络不可用时排放到世界";
                public static LocString CONFIG_GEYSER_WORLD_OUTPUT_FALLBACK_DESC = "开启后，已接入网络并启用直接入网的泉会在网络无法储存产物时排放到世界；关闭后会暂停输出并等待网络恢复。";
                public static LocString CONFIG_REQUEST_SUCCESS_COOLDOWN = "请求成功冷却秒数";
                public static LocString CONFIG_REQUEST_SUCCESS_COOLDOWN_DESC = "材料已满足或达到限额后的检查间隔。";
                public static LocString CONFIG_REQUEST_RETRY_COOLDOWN = "请求失败重试秒数";
                public static LocString CONFIG_REQUEST_RETRY_COOLDOWN_DESC = "缺料或没有可请求配方后的重试间隔。";
                public static LocString CONFIG_INFINITE_QUEUE_BATCHES = "无限队列请求批次数";
                public static LocString CONFIG_INFINITE_QUEUE_BATCHES_DESC = "生产队列为无限时，一次按多少批材料请求。";
                public static LocString CONFIG_MAX_REQUEST_BATCHES = "最大请求批次数";
                public static LocString CONFIG_MAX_REQUEST_BATCHES_DESC = "单次材料请求最多按多少批计算。";
                public static LocString CONFIG_PLAN_RECURSION_DEPTH = "生产计划递归深度";
                public static LocString CONFIG_PLAN_RECURSION_DEPTH_DESC = "补产链路向下追踪的最大层数。";
                public static LocString CONFIG_ABNORMAL_TIMEOUT = "异常订单超时周期";
                public static LocString CONFIG_ABNORMAL_TIMEOUT_DESC = "订单多长时间无进度后自动取消排产。";
                public static LocString CONFIG_COMPLETED_RETENTION = "完成订单保留周期";
                public static LocString CONFIG_COMPLETED_RETENTION_DESC = "完成/取消/异常订单在列表中保留多久。";
                public static LocString CONFIG_SERVER_CAPACITY_MULTIPLIER = "服务器容量倍率";
                public static LocString CONFIG_SERVER_CAPACITY_MULTIPLIER_DESC = "影响新建储存服务器和电池服务器的容量。已存在建筑通常需要重建或重新加载后才完全刷新。";
                public static LocString CONFIG_PORT_CAPACITY_MULTIPLIER = "端口缓存容量倍率";
                public static LocString CONFIG_PORT_CAPACITY_MULTIPLIER_DESC = "影响新建材料、液体、气体端口的缓存容量。";
                public static LocString CONFIG_POWER_PORT_CAPACITY_MULTIPLIER = "电力端口缓存倍率";
                public static LocString CONFIG_POWER_PORT_CAPACITY_MULTIPLIER_DESC = "影响新建电力入网/出网端口的缓存电量。";
                public static LocString CONFIG_BATTERY_SERVER_LEAK = "电池服务器漏电 J/周期";
                public static LocString CONFIG_BATTERY_SERVER_LEAK_DESC = "所有电池服务器每周期损失的电量。";
                public static LocString CONFIG_COLD_STORAGE_MIN_TEMPERATURE = "冷库最低目标温度 °C";
                public static LocString CONFIG_COLD_STORAGE_MIN_TEMPERATURE_DESC = "冷库温度滑条允许设置的最低目标温度。";
                public static LocString CONFIG_COLD_STORAGE_MAX_TEMPERATURE = "冷库最高目标温度 °C";
                public static LocString CONFIG_COLD_STORAGE_MAX_TEMPERATURE_DESC = "冷库温度滑条允许设置的最高目标温度。";
                public static LocString CONFIG_COLD_STORAGE_ENERGY_SAVER_WATTS = "冷库节能功耗 W";
                public static LocString CONFIG_COLD_STORAGE_ENERGY_SAVER_WATTS_DESC = "内容物达到目标温度后，冷库维持温度时的功耗。";
                public static LocString CONFIG_COLD_STORAGE_MAX_COOLING_WATTS = "冷库最大制冷功耗 W";
                public static LocString CONFIG_COLD_STORAGE_MAX_COOLING_WATTS_DESC = "目标温度越低越接近此功耗。";
                public static LocString CONFIG_COLD_STORAGE_MAX_COOLING_HEAT = "冷库最大产热 kDTU/s";
                public static LocString CONFIG_COLD_STORAGE_MAX_COOLING_HEAT_DESC = "目标温度越低越接近此产热。";
                public static LocString CONFIG_SOLID_OUTPUT_MAX_RATE = "材料出网最大速率 kg/s";
                public static LocString CONFIG_SOLID_OUTPUT_MAX_RATE_DESC = "材料出网端口设置滑条的最大输出速率。";
                public static LocString CONFIG_LIQUID_OUTPUT_MAX_RATE = "液体出网最大速率 kg/s";
                public static LocString CONFIG_LIQUID_OUTPUT_MAX_RATE_DESC = "液体出网端口设置滑条的最大输出速率。";
                public static LocString CONFIG_GAS_OUTPUT_MAX_RATE = "气体出网最大速率 kg/s";
                public static LocString CONFIG_GAS_OUTPUT_MAX_RATE_DESC = "气体出网端口设置滑条的最大输出速率。";
                public static LocString CONFIG_POWER_INPUT_MAX_WATTS = "电力入网最大功率 W";
                public static LocString CONFIG_POWER_INPUT_MAX_WATTS_DESC = "电力入网端口滑条的最大输入功率。";
                public static LocString CONFIG_POWER_OUTPUT_MAX_WATTS = "电力出网最大功率 W";
                public static LocString CONFIG_POWER_OUTPUT_MAX_WATTS_DESC = "电力出网端口滑条的最大输出功率。";

                public static LocString ORDER_CENTER_TITLE = "生产订单中心";
                public static LocString ORDER_CENTER_SUBTITLE = "库存承诺 / 补产链路 / 多设备调度 / 异常追踪";
                public static LocString ORDER_WORKSPACE_TITLE = "订单工作台";
                public static LocString ORDER_TRACKING_TITLE = "订单追踪";
                public static LocString ORDER_EDITOR_TITLE = "下单参数";
                public static LocString ORDER_PREVIEW_TITLE = "执行预览";
                public static LocString ORDER_AMOUNT_KEEP_TITLE = "数量 / 保持";
                public static LocString ORDER_ROUTE_RECIPE_TITLE = "设备 / 配方";
                public static LocString ORDER_VALIDATION_TITLE = "校验";
                public static LocString ORDER_MATERIAL_SCHEDULE_TITLE = "材料调度";
                public static LocString ORDER_EQUIPMENT_RECIPE_TITLE = "设备 / 配方";
                public static LocString ORDER_DRAFT_EDITOR_TITLE = "下单参数";
                public static LocString ORDER_PLAN_PANEL_TITLE = "订单执行预览";
                public static LocString ORDER_PRODUCT_BRIEF = "{0} x{1} | 耗时 {2}周期";
                public static LocString ORDER_PRODUCT_BRIEF_UNKNOWN = "{0} x{1} | 耗时 未知";
                public static LocString ORDER_ROUTE_RECIPE_LINE = "路线 {0}    配方 {1}";
                public static LocString ORDER_METRIC_TARGET = "目标";
                public static LocString ORDER_METRIC_AVAILABLE = "可用";
                public static LocString ORDER_METRIC_BATCHES = "批次";
                public static LocString ORDER_METRIC_STATUS = "状态";
                public static LocString ORDER_KEEP_TITLE = "货物保持";
                public static LocString ORDER_KEEP_STATUS = "保持 {0}";
                public static LocString ORDER_KEEP_DISABLED = "未开启";
                public static LocString ORDER_KEEP_BUTTON = "保持";
                public static LocString ORDER_KEEP_ENABLED_STATUS = "货物保持：{0} 低于 {1} 时自动下单。";
                public static LocString ORDER_KEEP_CLEARED_STATUS = "货物保持：已关闭 {0}。";
                public static LocString ORDER_ROUTE_SECTION = "生产设备 ({0})";
                public static LocString ORDER_ROUTE_DEVICE_MULTI = "{0}台 / {1}个配方";
                public static LocString ORDER_ROUTE_DEVICE_SINGLE = "{0}台";
                public static LocString ORDER_RECIPE_SECTION = "配方方案 ({0})";
                public static LocString ORDER_DRAFT_VALIDATION_TITLE = "草案校验";
                public static LocString ORDER_VALIDATION_READY_BODY = "库存、设备、材料均可执行。";
                public static LocString ORDER_VALIDATION_OUTPUT = "预计可产出：{0}";
                public static LocString ORDER_VALIDATION_ACTIVE_ORDERS = "{0} 活动订单：{1}";
                public static LocString ORDER_DRAFT_MISSING_PRODUCT = "未选择可生产的成品或配方。";
                public static LocString ORDER_DRAFT_AMOUNT_POSITIVE = "订单数量必须大于 0。";
                public static LocString ORDER_DRAFT_NO_EQUIPMENT = "没有可用生产设备，无法提交。";
                public static LocString ORDER_DRAFT_BLOCKED_REQUIREMENTS = "有 {0} 项材料既无库存也无可接入补产路线。";
                public static LocString ORDER_DRAFT_DUPLICATE_MERGE = "检测到活动订单 #{0}，提交将合并数量而不是创建重复订单。";
                public static LocString ORDER_DRAFT_AUTO_PRODUCE = "{0} 项材料缺口会自动补产。";
                public static LocString ORDER_SUBMIT_NO_EQUIPMENT = "订单追踪：提交失败，没有可用生产设备";
                public static LocString ORDER_SUBMIT_MERGED = "订单追踪：已合并到活动订单 #{0}，新增批次 {1}";
                public static LocString ORDER_SUBMIT_CREATED = "订单追踪：已创建活动订单 #{0}，批次 {1}";
                public static LocString ORDER_FOOTER_READY = "草案已通过，可提交调度。";
                public static LocString ORDER_FOOTER_BLOCKED = "草案存在阻塞，请检查材料或设备。";
                public static LocString ORDER_CONFIRM_MERGE = "合并订单";
                public static LocString ORDER_DUPLICATE_FOUND = "检测到活动订单 #{0}，提交将合并数量并追加调度。";
                public static LocString ORDER_ACTIVE_ORDERS_FOUND = "当前成品还有 {0} 个活动订单；本次会创建新的追踪项。";
                public static LocString ORDER_NO_ACTIVE_ORDERS = "当前没有活动订单；提交后创建新的追踪项。";
                public static LocString ORDER_RISK_BLOCKED = "阻塞";
                public static LocString ORDER_RISK_WARNING = "需调度";
                public static LocString ORDER_RISK_READY = "可提交";
                public static LocString ORDER_AUTOMATION_BLOCKED = "存在不可满足材料或设备阻塞，提交前需要处理。";
                public static LocString ORDER_AUTOMATION_PRODUCE = "{0} 项材料会自动补产；提交后接管材料请求和成品回存。";
                public static LocString ORDER_AUTOMATION_READY = "库存可覆盖材料需求；提交后会按设备负载分配批次。";

                public static LocString ORDER_METRIC_CURRENT_CYCLE = "当前周期";
                public static LocString ORDER_METRIC_FINISH = "预计完成";
                public static LocString ORDER_METRIC_EQUIPMENT = "生产设备";
                public static LocString ORDER_METRIC_AUTO_PRODUCE = "自动补产";
                public static LocString ORDER_METRIC_BLOCKED = "阻塞项";
                public static LocString ORDER_UNKNOWN = "未知";
                public static LocString ORDER_ASSIGNMENT_PANEL = "设备排产";
                public static LocString ORDER_MATERIAL_STOCK_PANEL = "材料与库存";
                public static LocString ORDER_NO_EQUIPMENT = "没有可用生产建筑。";
                public static LocString ORDER_NO_MATERIALS = "该配方没有材料输入，提交后直接排产。";
                public static LocString ORDER_RESEARCH_BATCH_SUMMARY = "{0}批  {1}";
                public static LocString ORDER_RESEARCH_MISSING_STOCK = "缺 {0}  库存 {1}";
                public static LocString ORDER_DISPATCH_DIRECT = "直接调拨";
                public static LocString ORDER_DISPATCH_AUTO = "自动补产";
                public static LocString ORDER_DISPATCH_NO_ROUTE = "缺少补产路线";
                public static LocString ORDER_OUTPUT_AMOUNT = "产出 {0}";
                public static LocString ORDER_ASSIGNMENT_META = "按当前设备负载分配";
                public static LocString ORDER_TABLE_MATERIAL = "材料";
                public static LocString ORDER_TABLE_REQUIRED = "需求";
                public static LocString ORDER_TABLE_STOCK_MISSING = "库存 / 缺口";
                public static LocString ORDER_TABLE_ACTION = "处理方式";
                public static LocString ORDER_REQUIREMENT_STOCK = "需求 {0} / 库存 {1}";
                public static LocString ORDER_REQUIREMENT_MISSING = "需求 {0} / 缺口 {1}";
                public static LocString ORDER_ACTION_DIRECT = "处理方式：从网络库存送入成品设备";
                public static LocString ORDER_ACTION_AUTO = "处理方式：自动补产，{0}";
                public static LocString ORDER_ACTION_BLOCKED = "处理方式：缺料，没有可用补产路线";
                public static LocString ORDER_CHILD_ROUTE = "补产路线：{0} x{1}";
                public static LocString ORDER_DEVICE_LINE = "设备：{0}";
                public static LocString ORDER_MISSING_PREFIX = "缺 {0}";
                public static LocString ORDER_PREVIEW_PANEL = "调度预览";
                public static LocString ORDER_NO_CHAIN = "没有可显示的产线。";
                public static LocString ORDER_DISPATCH_TITLE_LINE = "{0}：先排成品，缺料再补产";
                public static LocString ORDER_DISPATCH_DESC = "成品设备会等待材料；缺口材料由其它可用设备补齐。";
                public static LocString ORDER_BATCH_COUNT = "{0} 批";
                public static LocString ORDER_MACHINE_COUNT = "{0} 台";
                public static LocString ORDER_PRODUCT_DISPATCH = "成品排产";
                public static LocString ORDER_NEEDS_PRODUCTION = "需要补产";
                public static LocString ORDER_SEND_FROM_NETWORK = "从网络库存送入成品设备";
                public static LocString ORDER_FLOW_DESC = "每一行表示一种原料：库存足够就直接调拨，不足则显示补产来源。";
                public static LocString ORDER_FLOW_RECIPE = "排产配方";
                public static LocString ORDER_FLOW_MATERIAL = "本批材料需求";
                public static LocString ORDER_FLOW_STATUS = "补产设备 / 状态";
                public static LocString ORDER_BATCH_LABEL = "批次 {0}";
                public static LocString ORDER_EQUIPMENT_LABEL = "设备 {0}";
                public static LocString ORDER_OUTPUT_LABEL = "产出 {0}";
                public static LocString ORDER_STILL_MISSING = "仍缺料";

                public static LocString TRACKING_NO_PRODUCT = "选择成品后显示活动订单。";
                public static LocString TRACKING_ALL_PRODUCTS = "全部成品";
                public static LocString TRACKING_EMPTY = "暂无活动订单。提交后会显示状态、数量、批次和合并记录。";
                public static LocString TRACKING_ACTIVE_TITLE = "活动订单追踪";
                public static LocString TRACKING_CREATED_CYCLE = "创建周期";
                public static LocString TRACKING_ESTIMATED_FINISH_CYCLE = "预计完成";
                public static LocString TRACKING_FINISHED_CYCLE = "完成周期";
                public static LocString TRACKING_CYCLE_VALUE = "{0} 周期";
                public static LocString TRACKING_CYCLE_UNKNOWN = "未知";
                public static LocString TRACKING_MERGED_ACTIVITY = "已合并 {0} 次 · 活动周期 {1}";
                public static LocString TRACKING_SUMMARY = "{0}：{1} 个活动订单 / {2} 条最近记录";
                public static LocString TRACKING_FILTER_CURRENT = "当前";
                public static LocString TRACKING_FILTER_ALL = "全部";
                public static LocString TRACKING_FILTER_RUNNING = "运行中";
                public static LocString TRACKING_FILTER_COMPLETED = "已完成";
                public static LocString TRACKING_FILTER_ABNORMAL = "异常";
                public static LocString TRACKING_ORDER_SOURCE_BATCH = "{0}订单 · 批次 x{1}";
                public static LocString TRACKING_WAITING_MATERIALS = "成品设备 {0} 台等待材料，{1} 台设备补产缺口。";
                public static LocString TRACKING_MACHINES_RUNNING = "{0} 台设备正在以 {1} 核处理该订单。";
                public static LocString TRACKING_STATE_CREATED = "{0}，创建于 {1} 周期。";
                public static LocString TRACKING_SOURCE_KEEP = "货物保持";
                public static LocString TRACKING_SOURCE_MANUAL = "手动";
                public static LocString TRACKING_STATE_SUBMITTED = "已提交";
                public static LocString TRACKING_STATE_WAITING = "待材料";
                public static LocString TRACKING_STATE_PRODUCING = "生产中";
                public static LocString TRACKING_STATE_COMPLETED = "完成";
                public static LocString TRACKING_STATE_ABNORMAL = "异常取消";
                public static LocString TRACKING_STATE_CANCELLED = "已取消";
                public static LocString TRACKING_STATE_TRACKING = "追踪中";
                public static LocString TRACKING_ACTION_CLEAR_ABNORMAL = "清异常";
                public static LocString TRACKING_ACTION_CLEAR_COMPLETED = "清完成";
                public static LocString TRACKING_ACTION_RETRY = "重试";
                public static LocString ORDER_PRODUCTION_CENTER_TITLE = "订单生产中心";
                public static LocString ORDER_CENTER_ENGRAVE_SECTION_TITLE = "刻录";
                public static LocString ORDER_CENTER_ORDER_SECTION_TITLE = "订单";
                public static LocString ORDER_CENTER_OPEN_BUTTON = "订单";
                public static LocString ORDER_CENTER_OPEN_TOOLTIP = "打开此订单生产中心的专属订单面板；只显示并提交刻录到该建筑的配方。";
                public static LocString ORDER_CENTER_ENGRAVE_BUTTON = "刻录";
                public static LocString ORDER_CENTER_ENGRAVE_TOOLTIP = "选择一个或多个带配方的建筑进行刻录";
                public static LocString ORDER_CENTER_ENGRAVE_TOOLNAME = "刻录工具";
                public static LocString ORDER_CENTER_ENGRAVE_ACTION = "刻录";
                public static LocString ORDER_CENTER_ENGRAVE_STARTED = "刻录模式：请选择一个带有配方的生产建筑。";
                public static LocString ORDER_CENTER_ENGRAVE_SUCCESS = "订单生产中心：已刻录 {0} 个新配方。";
                public static LocString ORDER_CENTER_ENGRAVE_DUPLICATE = "订单生产中心：目标建筑的配方都已经刻录过。";
                public static LocString ORDER_CENTER_ENGRAVE_NO_RECIPES = "订单生产中心：请选择一个带有配方的生产建筑。";
                public static LocString ORDER_CENTER_ENGRAVE_NO_DISK = "订单生产中心：请先放入一张空刻录盘。";
                public static LocString ORDER_CENTER_DISK_CONFIG_TITLE = "刻录盘";
                public static LocString ORDER_CENTER_DISK_CONFIG_TOOLTIP = "配置订单生产中心的 3 个刻录盘槽。刻录出的配方会写入空刻录盘；每张刻录盘还会提供 1 个生产核心。";
                public static LocString ORDER_CENTER_DISK_SLOT_EMPTY = "空槽";
                public static LocString ORDER_CENTER_DISK_SLOT_BLANK = "空白刻录盘";
                public static LocString ORDER_CENTER_DISK_SLOT_WRITTEN = "刻录盘：{0} 个配方";
                public static LocString ORDER_CENTER_DISK_WAITING_DELIVERY = "等待运送";
                public static LocString ORDER_CENTER_DISK_WAITING_DELIVERY_TOOLTIP = "已选择刻录盘，等待复制人运送到该槽位。";
                public static LocString ORDER_CENTER_DISK_CANCEL_DELIVERY = "取消";
                public static LocString ORDER_CENTER_DISK_INSERT = "放入";
                public static LocString ORDER_CENTER_DISK_EJECT = "弹出";
                public static LocString ORDER_CENTER_DISK_INSERTED = "已放入刻录盘。";
                public static LocString ORDER_CENTER_DISK_EJECTED = "已弹出刻录盘。";
                public static LocString ORDER_CENTER_DISK_NO_AVAILABLE = "附近没有可用的刻录盘。";
                public static LocString ORDER_CENTER_DISK_RECORDED_TITLE = "已刻录配方";
                public static LocString ORDER_CENTER_DISK_RECIPE_REMOVE = "删除";
                public static LocString ORDER_CENTER_DISK_RECIPE_REMOVED = "已删除刻录配方。";
                public static LocString ORDER_CENTER_DISK_PICKER_TITLE = "选择刻录盘";
                public static LocString ORDER_CENTER_DISK_PICKER_COUNT = "可选刻录盘：{0} 个";
                public static LocString ORDER_CENTER_DISK_PICKER_HINT = "选择一个刻录盘后，复制人会前往订单生产中心完成装盘；只有完成任务后刻录盘才会放入对应槽位。";
                public static LocString ORDER_CENTER_DISK_PICKER_ANY = "任意刻录盘";
                public static LocString ORDER_CENTER_DISK_PICKER_ANY_DESC = "选择当前可用刻录盘列表中的一张盘";
                public static LocString ORDER_CENTER_DISK_PICKER_ASSIGNED = "已安排复制人装盘。";
                public static LocString ORDER_CENTER_DISK_PICKER_ASSIGN_FAILED = "无法安排装盘，目标槽位或刻录盘已不可用。";
                public static LocString ORDER_CENTER_DISK_PICKER_STORAGE_FALLBACK = "储存网络";
                public static LocString ORDER_CENTER_DISK_PICKER_DETAIL_STORAGE = "{0}  ·  {1}";
                public static LocString ORDER_CENTER_DISK_PICKER_DETAIL_DISTANCE = "{0}  ·  距离 {1:0.0}";
                public static LocString ORDER_CENTER_DISK_SUMMARY_MORE = "{0} 等 {1} 个配方";
                public static LocString ORDER_CENTER_DISK_INFO_CORE_TITLE = "核心能力";
                public static LocString ORDER_CENTER_DISK_INFO_CORE_DESC = "提供 1 个生产核心";
                public static LocString ORDER_CENTER_DISK_INFO_RECORDED_TITLE = "已刻录配方";
                public static LocString ORDER_CENTER_DISK_INFO_RECORDED_COUNT = "共 {0} 个配方";
                public static LocString ORDER_CENTER_DISK_INFO_DESCRIPTION = "{0}\n\n<b>{1}</b>\n{2}\n\n<b>{3}</b>\n{4}\n\n{5}";
                public static LocString ORDER_CENTER_DISK_RECIPE_DETAIL = "<b>{0}</b>\n  材料：{1}\n  产出：{2}";
                public static LocString TRACKING_BUILDING_RUNNING = "正常运行";
                public static LocString TRACKING_BUILDING_WAITING_MATERIALS = "等待材料";
                public static LocString TRACKING_BUILDING_NO_POWER = "缺电";
                public static LocString TRACKING_BUILDING_DISABLED = "禁用";
                public static LocString TRACKING_BUILDING_NO_RECIPE = "无配方";
                public static LocString TRACKING_BUILDING_ABNORMAL = "异常暂停";
                public static LocString TRACKING_BUILDING_QUEUED = "排队中";
                public static LocString TRACKING_BUILDING_MISSING = "建筑不存在";
                public static LocString TRACKING_BUILDING_PROGRESS = "进度 {0:P0}";
                public static LocString TRACKING_BUILDING_QUEUE = "{0} x{1}    队列 {2}";
                public static LocString TRACKING_DETAIL_TARGET = "目标 {0}";
                public static LocString TRACKING_DETAIL_SUPPLY = "供给 {0}";
                public static LocString TRACKING_OUTPUT_RESERVED = "产出预留 {0}";
                public static LocString TRACKING_MATERIAL_DISPATCH = "材料调拨 {0}";

                public static LocString PRODUCTION_METRIC_STORAGE = "储存";
                public static LocString PRODUCTION_METRIC_RUNNING = "运行";
                public static LocString PRODUCTION_METRIC_RECIPE = "配方";
                public static LocString PRODUCTION_METRIC_REQUIRED = "需求物";
                public static LocString PRODUCTION_METRIC_NETWORK = "网络";
                public static LocString STATUS_ENABLED = "已开启";
                public static LocString STATUS_DISABLED = "已关闭";
                public static LocString ACTION_CLOSE = "关闭";
                public static LocString SOURCE_POLICY = "来源策略";
                public static LocString OUTPUT_POLICY = "存放策略";
                public static LocString NO_LIMIT = "不限额";
                public static LocString OUTPUT_STORE_MANUAL_DESC = "成品保留在建筑输出栏，不自动转移。";
                public static LocString OUTPUT_STORE_AUTO_STATUS = "自动入网";
                public static LocString OUTPUT_STORE_MANUAL_STATUS = "手动取出";
                public static LocString PRODUCTION_SHORT_IDLE = "待机";
                public static LocString PRODUCTION_SHORT_WAITING_WORKER = "等人";
                public static LocString PRODUCTION_SHORT_CRAFTING = "制作中";
                public static LocString NONE = "无";
                public static LocString REQUEST_ON_SHORT = "请求开";
                public static LocString REQUEST_OFF_SHORT = "请求关";
                public static LocString OUTPUT_ON_SHORT = "入网开";
                public static LocString OUTPUT_OFF_SHORT = "入网关";
                public static LocString NO_COMPONENT = "无组件";
                public static LocString ORDER_USAGE_PREFIX = "订单用途：{0}";
                public static LocString ORDER_USAGE_PRIMARY = "#{0} 执行 {1} x{2}";
                public static LocString ORDER_USAGE_SUPPLY = "#{0} 为 {1} 提供 {2} x{3}";

                public static LocString WORLD_MATERIAL_REQUEST_ON = "材料请求：开启";
                public static LocString WORLD_MATERIAL_REQUEST_OFF = "材料请求：关闭";
                public static LocString WORLD_STATUS = "状态：{0}";
                public static LocString WORLD_WAITING_QUEUE = "等待生产队列";
                public static LocString WORLD_ORDER_EMPTY = "订单：当前建筑没有活动订单";
                public static LocString WORLD_ORDER_LIST = "订单：{0}";
                public static LocString WORLD_PRODUCTION_TITLE = "Storage Network 生产接入";
                public static LocString WORLD_STORAGE_TITLE = "Storage Network 储存接入";
                public static LocString WORLD_NETWORK_CAPACITY = "网络容量：{0} / {1}";
                public static LocString WORLD_BUILDING_STORAGE = "本建筑：{0} / {1}";
                public static LocString WORLD_BUILDING_NO_STORAGE = "本建筑：未检测到 Storage";
                public static LocString WORLD_OUTPUT_STATUS = "输出：{0}";
                public static LocString WORLD_CONNECTED_BUILDINGS = "接入建筑：{0}";
                public static LocString CORE_SIDE_SCREEN_TITLE = "储存网络概览";
                public static LocString CORE_SIDE_SCREEN_STATUS = "星球：{0}  状态：{1}";
                public static LocString CORE_SIDE_SCREEN_ONLINE = "在线";
                public static LocString CORE_SIDE_SCREEN_OFFLINE = "离线";
                public static LocString CORE_SIDE_SCREEN_CAPACITY = "容量：{0} / {1}  剩余：{2}";
                public static LocString CORE_SIDE_SCREEN_BUILDINGS = "服务器：{0}  接入储存：{1}";
                public static LocString CORE_SIDE_SCREEN_RELAY = "跨星球中继：{0}";
                public static LocString CORE_SIDE_SCREEN_RELAY_ONLINE = "已连接";
                public static LocString CORE_SIDE_SCREEN_RELAY_OFFLINE = "未连接";
                public static LocString CORE_SIDE_SCREEN_WORLD_LABEL = "星球";
                public static LocString CORE_SIDE_SCREEN_STATUS_LABEL = "状态";
                public static LocString CORE_SIDE_SCREEN_STORED_LABEL = "容量";
                public static LocString CORE_SIDE_SCREEN_REMAINING_LABEL = "剩余";
                public static LocString CORE_SIDE_SCREEN_SERVERS_LABEL = "服务器";
                public static LocString CORE_SIDE_SCREEN_STORAGES_LABEL = "接入储存";
                public static LocString CORE_SIDE_SCREEN_RELAY_LABEL = "中继";
                public static LocString CORE_SIDE_SCREEN_INTERNAL_BATTERY_LABEL = "备用电池";
                public static LocString CORE_SIDE_SCREEN_POWER_LABEL = "供电";
                public static LocString CORE_SIDE_SCREEN_POWER_EXTERNAL = "外部";
                public static LocString CORE_SIDE_SCREEN_POWER_INTERNAL = "备用";
                public static LocString CORE_INTERNAL_BATTERY_LOW_NOTIFICATION = "储存网络核心备用电池电量低。";
                public static LocString CORE_INTERNAL_BATTERY_STATUS = "可用电力：{Battery}/{Capacity}";
                public static LocString CORE_INTERNAL_BATTERY_STATUS_TOOLTIP = "储存网络核心内置备用电池电量：{Battery}/{Capacity}（{Percent}）。外部电力中断时会消耗这部分电力维持网络在线。";
                public static LocString CORE_BACKUP_POWER_NOTIFICATION = "储存网络核心外部供电中断，已切换到备用电池。";
                public static LocString CORE_BACKUP_POWER_STATUS = "备用电池供电：{Battery}";
                public static LocString CORE_BACKUP_POWER_STATUS_TOOLTIP = "外部电力中断，储存网络核心正在消耗备用电池维持网络在线。当前备用电量：{Battery}";
                public static LocString LIQUID_OUTPUT_SIDE_SCREEN_TITLE = "液体输出筛选";
                public static LocString LIQUID_OUTPUT_SIDE_SCREEN_CURRENT = "当前输出：{0}";
                public static LocString LIQUID_OUTPUT_SIDE_SCREEN_HINT = "快速选择液体出网端口要输出的液体。列表来自当前网络服务器中已有的液体，切换后会替换端口缓存并立即尝试输出。";
                public static LocString MAIN_SEARCH_TOOLTIP = "搜索建筑、分类、储存物或泉输出。";

                public static LocString MATERIAL_STATUS_NO_QUEUE = "没有可请求材料的排队配方";
                public static LocString MATERIAL_STATUS_LIMIT_REACHED = "已达到请求限额";
                public static LocString MATERIAL_STATUS_MISSING_SOURCE = "缺少 {0}，网络中没有可用来源";
                public static LocString MATERIAL_STATUS_REQUESTED = "已请求 {0} {1}";
                public static LocString MATERIAL_STATUS_SATISFIED = "当前配方材料已满足";
                public static LocString MATERIAL_STATUS_WAITING_OUTPUT = "等待下一批成品";
                public static LocString MATERIAL_STATUS_WAITING_CONTENTS = "等待内容物进入储存栏";
                public static LocString MATERIAL_STATUS_WAITING_PRODUCTS = "等待成品进入输出栏";
                public static LocString MATERIAL_STATUS_NO_OUTPUT_STORAGE = "建筑没有可读取的输出栏";
                public static LocString TRANSFER_STATUS_MOVED = "已入网 {0}";
                public static LocString TRANSFER_STATUS_BLOCKED = "无法存入 {0}：没有匹配箱子或容量不足";
                public static LocString TRANSFER_STATUS_RESERVED_TARGETS = "自动存放无可用目标：匹配服务器已被输入端指定。";
                public static LocString INPUT_TARGET_NOT_FOUND = "指定服务器未找到";
                public static LocString INPUT_TARGET_UNREACHABLE = "指定服务器当前不可达";
                public static LocString SERVER_ASSIGNMENTS_TITLE = "端口指定";
                public static LocString INPUT_ASSIGNMENT_TITLE = "输入端指定";
                public static LocString OUTPUT_SOURCE_ASSIGNMENT_TITLE = "输出端指定来源";
                public static LocString INPUT_ASSIGNMENT_CLEAR = "取消";
                public static LocString INPUT_ASSIGNMENT_CLEAR_ALL = "全部取消指定";
                public static LocString INPUT_ASSIGNMENT_NONE = "无";

                public static LocString ENROLL_STATUS = "已经接入储存网络";
                public static LocString ENROLL_STATUS_TOOLTIP = "这个建筑已经接入储存网络，会显示在储存网络面板中。";
                public static LocString ENROLL_ADD = "加入网络";
                public static LocString ENROLL_REMOVE = "移出网络";
                public static LocString ENROLL_ADD_TOOLTIP = "将这个建筑加入储存网络面板。";
                public static LocString ENROLL_REMOVE_TOOLTIP = "将这个建筑从储存网络面板中移除。";
                public static LocString STARMAP_NAME_HINT = "星图";
                public static LocString GEYSER_ANALYZED = "已分析";
                public static LocString GEYSER_OUTPUT = "{0}  平均 {1}";
                public static LocString GEYSER_COUNT = "{0} 个泉";
                public static LocString GEYSER_SETTINGS_TITLE = "泉设置";
                public static LocString GEYSER_NETWORK_ENABLED = "接入储存网络";
                public static LocString GEYSER_DIRECT_OUTPUT_ENABLED = "喷发物直接入网";
                public static LocString GEYSER_DIRECT_OUTPUT_DESC = "开启后会拦截泉的 ElementEmitter 输出并存入网络中的匹配箱子；网络无法储存时是否排放到世界由模组选项决定。";
                public static LocString GEYSER_DIRECT_OUTPUT_ON_SHORT = "直入开";
                public static LocString GEYSER_DIRECT_OUTPUT_OFF_SHORT = "直入关";
                public static LocString GEYSER_ERUPTING = "喷发中";
                public static LocString GEYSER_NOT_ERUPTING = "停止喷发";
                public static LocString GEYSER_OUTPUT_STORE_TITLE = "喷发物入网";
                public static LocString GEYSER_OUTPUT_CONTENT_TITLE = "内容物";
                public static LocString GEYSER_OUTPUT_DIRECT_STATUS = "直接入网";
                public static LocString GEYSER_OUTPUT_WORLD_STATUS = "排放到世界";
                public static LocString GEYSER_METRIC_OUTPUT = "产物";
                public static LocString GEYSER_METRIC_RATE = "平均输出";
                public static LocString GEYSER_METRIC_DIRECT_OUTPUT = "直入";
                public static LocString GEYSER_STATUS_ITEM = "储存网络：{0}";
                public static LocString GEYSER_STATUS_TOOLTIP = "{0}";
                public static LocString GEYSER_STATUS_LINE_NETWORK = "接入：{0}    核心：{1}";
                public static LocString GEYSER_STATUS_LINE_DIRECT = "直入：{0}    状态：{1}";
                public static LocString GEYSER_STATUS_LINE_OUTPUT = "产物：{0}    速率：{1}";
                public static LocString GEYSER_STATUS_LINE_POLICY = "存放策略：{0}";
                public static LocString GEYSER_STATUS_LINE_TARGETS = "可用目标：{0}";
                public static LocString GEYSER_STATUS_LINE_STATUS = "当前去向：{0}";
                public static LocString GEYSER_STATUS_NETWORK_OUTPUT = "存入网络";
                public static LocString GEYSER_STATUS_OVERFLOW_OUTPUT = "服务器已满，溢出到世界";
                public static LocString GEYSER_STATUS_FULL_PAUSED = "服务器已满，输出已暂停";
                public static LocString GEYSER_STATUS_NETWORK_PAUSED = "网络不可用，输出已暂停";
                public static LocString GEYSER_STATUS_WORLD_OUTPUT = "按原版排放";
                public static LocString GEYSER_STATUS_MISSING_EMITTER = "缺少喷发组件";
                public static LocString GEYSER_STATUS_TARGET_FULL = "有匹配服务器，但容量已满";
                public static LocString GEYSER_STATUS_TARGET_SUMMARY = "{0} 个服务器，剩余 {1}";
            }
        }
    }
}
