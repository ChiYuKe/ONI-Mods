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

                public static LocString SUMMARY_BUTTON = "汇总";
                public static LocString SUMMARY_TOOLTIP = "汇总当前分类中所有箱子的物品";
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
