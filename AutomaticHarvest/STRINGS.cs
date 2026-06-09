using STRINGS;

namespace AutomaticHarvest
{
    internal class STRINGS
    {
        public class BUILDINGS
        {
            public class AUTOMATICHARVESTCONFIG
            {
                public static LocString NAME = "自动收获机";
                public static LocString DESC = "能够收获范围内的植物作物";
                public static LocString EFFECT = "复制人的好帮手";

                public static LocString LOGIC_PORT = "补充参数";
                public static LocString LOGIC_PORT_ACTIVE = "当自动收获机储存达到<b>高阈值</b>时，输出" + UI.FormatAsAutomationState("绿色信号", UI.AutomationState.Active) + "，直到储存再次降低 <b>低阈值</b> 以下";
                public static LocString LOGIC_PORT_INACTIVE = "当自动收获机储存低于<b>低阈值</b>时，输出" + UI.FormatAsAutomationState("红色信号", UI.AutomationState.Standby) + "，直到储存达到 <b>高阈值</b> 为止";

                public static LocString ACTIVATE_TOOLTIP = "当自动收获机储存达到 <b>{0}%</b> 时，发送" + UI.FormatAsAutomationState("绿色信号", UI.AutomationState.Active) + "，直至其容量达到 <b>{1}%（低阈值）</b>";
                public static LocString DEACTIVATE_TOOLTIP = "当自动收获机储存低于 <b>{0}%</b> 时，发送" + UI.FormatAsAutomationState("红色信号", UI.AutomationState.Standby) + "，直至其容量低于 <b>{1}%（高阈值）</b>";
                public static LocString SIDESCREEN_TITLE = "逻辑激活参数";
                public static LocString SIDESCREEN_ACTIVATE = "高阈值：";
                public static LocString SIDESCREEN_DEACTIVATE = "低阈值：";

                public class BUTTON
                {
                    public static LocString USERMENU_CLEAR = "清空内容";
                    public static LocString USERMENU_CLEAR_TOOLTIP = "点击清空，移除储存中的所有物品";
                    public static LocString USERMENU_DEACTIVATE = "禁用轨道输送";
                    public static LocString USERMENU_DEACTIVATE_TOOLTIP = "点击禁用，停止往轨道上运输物品";
                    public static LocString USERMENU_ACTIVATE = "启用轨道输送";
                    public static LocString USERMENU_ACTIVATE_TOOLTIP = "点击启用，允许往轨道上运输物品";
                }
            }
        }
    }
}
