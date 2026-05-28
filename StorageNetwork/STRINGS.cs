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
                public class STORAGENETWORKSCENESTORAGEBOX
                {
                    public static LocString NAME = "场景储存箱";
                    public static LocString DESC = "供场景储存总览面板识别的专用储物箱。";
                    public static LocString EFFECT = "复用原版储物箱动画与基础储存行为，并纳入当前场景储存总览。";
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
                public static LocString EMPTY_DETAILS = "会收集专用场景储存箱，以及手动加入的原版储物箱。";
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

                public static LocString CATEGORY_SCENE_STORAGE = "储存箱";
                public static LocString CATEGORY_VANILLA_STORAGE = "原版储存";
                public static LocString CATEGORY_RECIPE_BUILDING = "生产建筑";
                public static LocString CATEGORY_MOD_STORAGE = "模组建筑";
                public static LocString SOURCE_MOD_NAME = "来源：{0}";

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
                public static LocString MATERIAL_REQUEST_MODE = "请求方式：{0}";
                public static LocString MATERIAL_REQUEST_MODE_SEARCH = "从网络箱子中寻找合适材料";
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
                public static LocString CLOSE = "关闭";
                public static LocString AMOUNT_LABEL = "数量：{0}";
                public static LocString ALL = "全部";
                public static LocString CONFIRM = "确定";
                public static LocString ON = "开";
                public static LocString AMOUNT_INPUT = "输入数量";

                public static LocString ENROLL_STATUS = "已经接入储存网络";
                public static LocString ENROLL_STATUS_TOOLTIP = "这个建筑已经接入储存网络，会显示在储存网络面板中。";
                public static LocString ENROLL_ADD = "加入网络";
                public static LocString ENROLL_REMOVE = "移出网络";
                public static LocString ENROLL_ADD_TOOLTIP = "将这个建筑加入储存网络面板。";
                public static LocString ENROLL_REMOVE_TOOLTIP = "将这个建筑从储存网络面板中移除。";
                public static LocString STARMAP_NAME_HINT = "星图";
            }
        }
    }
}
