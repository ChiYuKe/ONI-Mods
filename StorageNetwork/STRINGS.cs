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

                public class STORAGENETWORKSMALLSOLIDSERVER
                {
                    public static LocString NAME = "小型固体服务器";
                    public static LocString DESC = "用于接入储存网络的小型固体储存服务器。";
                    public static LocString EFFECT = "储存固体物品，并显示在储存网络窗口中。";
                }

                public class STORAGENETWORKSCENESTORAGEBOX
                {
                    public static LocString NAME = "场景储存箱（已弃用）";
                    public static LocString DESC = "旧版本储存网络使用的兼容储存箱，已弃用。";
                    public static LocString EFFECT = "此建筑仅用于保护旧存档中的材料，可能会在将来的模组更新中被移除。请尽快取出或转移箱子中的材料，并拆除该箱子。";
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

                public class STORAGENETWORKMEDIUMSOLIDSERVER
                {
                    public static LocString NAME = "中型固体服务器";
                    public static LocString DESC = "用于接入储存网络的中型固体储存服务器。";
                    public static LocString EFFECT = "储存更多固体物品，并显示在储存网络窗口中。";
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

                public class STORAGENETWORKLARGESOLIDSERVER
                {
                    public static LocString NAME = "大型固体服务器";
                    public static LocString DESC = "用于接入储存网络的大型固体储存服务器。";
                    public static LocString EFFECT = "储存大量固体物品，并显示在储存网络窗口中。";
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

                public class STORAGENETWORKRELAYMODULE
                {
                    public static LocString NAME = "储存网络中继器";
                    public static LocString DESC = "安装在火箭上的储存网络中继舱。";
                    public static LocString EFFECT = "火箭发射到太空后，允许储存网络跨星球传输物品。";
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

                public class STORAGENETWORKSMALLSTORAGE
                {
                    public static LocString NAME = "小型储存";
                    public static LocString DESC = "解锁小型固体、液体、气体服务器。";
                }

                public class STORAGENETWORKMEDIUMSTORAGE
                {
                    public static LocString NAME = "中级储存";
                    public static LocString DESC = "解锁中型固体、液体、气体服务器。";
                }

                public class STORAGENETWORKLARGESTORAGE
                {
                    public static LocString NAME = "高级储存";
                    public static LocString DESC = "解锁大型固体、液体、气体服务器。";
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
                public static LocString NO_STORAGE_CONTENT = "没有储存内容";
                public static LocString STORAGE_SETTINGS = "设置";
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
                public static LocString TARGET_SELECTION_TITLE = "选择目标箱子";
                public static LocString TARGET_SELECTION_HEADER = "当前场景中的可接收目标";
                public static LocString CANCEL = "取消";
                public static LocString BUILDING_SETTINGS_TITLE = "建筑设置";
                public static LocString STORAGE_DETAILS = "储存：{0} / {1}\n剩余容量：{2}";
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
                public static LocString MINION_MATERIAL_REQUEST_ENABLED = "允许复制人从网络中请求材料";
                public static LocString MINION_MATERIAL_REQUEST_DESC = "开启后，复制人建造时可从储存网络调拨所需材料。";
                public static LocString ALL_MINION_SETTINGS_TITLE = "全部复制人设置";
                public static LocString ALL_MINION_SETTINGS_SUMMARY = "当前允许：{0} / {1}";
                public static LocString ALL_MINION_SETTINGS_DESC = "批量控制当前列表中的复制人是否可以在建造时从储存网络请求材料。";
                public static LocString ALL_MINION_ENABLE = "全部开启";
                public static LocString ALL_MINION_DISABLE = "全部关闭";
                public static LocString MATERIAL_REQUEST_MODE = "请求方式：{0}";
                public static LocString MATERIAL_REQUEST_MODE_SEARCH = "从服务器中寻找材料";
                public static LocString MATERIAL_REQUEST_MODE_SPECIFIC = "指定网络中某个箱子提供材料";
                public static LocString MATERIAL_REQUEST_SOURCE = "指定箱子：{0}";
                public static LocString MATERIAL_REQUEST_SOURCE_NONE = "未指定";
                public static LocString MATERIAL_REQUEST_SELECT_SOURCE = "选择箱子";
                public static LocString MATERIAL_REQUEST_LIMIT_ENABLED = "启用请求限额";
                public static LocString MATERIAL_REQUEST_LIMIT = "限额：{0} / {1}";
                public static LocString MATERIAL_REQUEST_SET_LIMIT = "设置限额";
                public static LocString MATERIAL_REQUEST_STATUS = "请求状态：{0}";
                public static LocString MATERIAL_REQUEST_RESET = "重置已请求";
                public static LocString MATERIAL_REQUEST_STATUS_ITEM = "材料提供由储存网络供给";
                public static LocString MATERIAL_REQUEST_STATUS_TOOLTIP = "这个建筑会从储存网络中请求当前生产需要的材料。\n{0}";
                public static LocString MATERIAL_REQUEST_AUTO_DESC = "自动从已接入网络的箱子中寻找材料，优先使用库存最多的来源。";
                public static LocString ENERGY_GENERATOR_SOURCE_DESC = "发电设施会从已接入网络的服务器中自动寻找可用燃料。";
                public static LocString OUTPUT_STORE_TITLE = "成品入网";
                public static LocString OUTPUT_STORE_ENABLED = "加工完成后自动存入网络";
                public static LocString STORAGE_OUTPUT_STORE_TITLE = "内容物入网";
                public static LocString STORAGE_OUTPUT_STORE_ENABLED = "允许把内容物输入网络";
                public static LocString STORAGE_OUTPUT_STORE_DESC = "开启后，这个储存建筑会把当前内容物自动转移到储存网络中的匹配箱子。";
                public static LocString OUTPUT_STORE_MODE = "存放方式：{0}";
                public static LocString OUTPUT_STORE_MODE_AUTO = "自动寻找匹配箱子";
                public static LocString OUTPUT_STORE_MODE_SPECIFIC = "指定网络中某个箱子存放";
                public static LocString OUTPUT_STORE_AUTO_DESC = "自动寻找已接入网络的匹配箱子，优先堆到已有同类物品的箱子；容量不足时会按剩余空间分批存入。";
                public static LocString OUTPUT_STORE_STATUS = "入网状态：{0}";
                public static LocString OUTPUT_STORE_TARGET = "目标箱子：{0}";
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
                public static LocString CONFIG_SCENE_SCAN_CACHE = "场景扫描缓存秒数";
                public static LocString CONFIG_SCENE_SCAN_CACHE_DESC = "数值越小刷新越快，但遍历储存建筑更频繁。";
                public static LocString CONFIG_DEFAULT_MATERIAL_LIMIT = "材料请求默认限额 kg";
                public static LocString CONFIG_DEFAULT_MATERIAL_LIMIT_DESC = "新接入生产建筑的默认请求限额。";
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
                public static LocString TRACKING_CYCLE_VALUE = "{0} 周期";
                public static LocString TRACKING_MERGED_ACTIVITY = "已合并 {0} 次 · 活动周期 {1}";
                public static LocString TRACKING_SUMMARY = "{0}：{1} 个活动订单 / {2} 条最近记录";
                public static LocString TRACKING_FILTER_CURRENT = "当前";
                public static LocString TRACKING_FILTER_ALL = "全部";
                public static LocString TRACKING_FILTER_RUNNING = "运行中";
                public static LocString TRACKING_FILTER_COMPLETED = "已完成";
                public static LocString TRACKING_FILTER_ABNORMAL = "异常";
                public static LocString TRACKING_ORDER_SOURCE_BATCH = "{0}订单 · 批次 x{1}";
                public static LocString TRACKING_WAITING_MATERIALS = "成品设备 {0} 台等待材料，{1} 台设备补产缺口。";
                public static LocString TRACKING_MACHINES_RUNNING = "{0} 台设备正在处理该订单。";
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
                public static LocString GEYSER_DIRECT_OUTPUT_DESC = "开启后会拦截泉的 ElementEmitter 输出，优先存入网络中的匹配箱子；容量不足的部分仍会排放到世界。";
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
            }
        }
    }
}
