namespace MadeInAbyss
{
    public class STRINGS
    {
        public class UI
        {
            public static string FormatAsHotkey(string text)
            {
                return "<b><color=#F44A4A>" + text + "</b></color>";
            }

            public static string FormatAsBold(string text)
            {
                return "<b>" + text + "</b>";
            }
        }

        public class DUPLICANTS
        {
            public class MODIFIERS
            {
                // —— 上升负荷（深渊诅咒），ID 与 AbyssStatics.CurseEffectIds 对应 ——

                public class ABYSSCURSE1
                {
                    public static LocString NAME = UI.FormatAsHotkey("上升负荷·眩晕");
                    public static LocString TOOLTIP = "第1层「浅层」的诅咒：\n\n从浅层返回时出现轻微的眩晕与恶心，注意力难以集中。\n\n压力增加，运动能力下降。";
                }

                public class ABYSSCURSE2
                {
                    public static LocString NAME = UI.FormatAsHotkey("上升负荷·恶心");
                    public static LocString TOOLTIP = "第2层「深层」的诅咒：\n\n强烈头痛伴随四肢麻木，浑身像被重锤敲打过。\n\n压力增加，运动能力进一步下降，体力恢复减缓。";
                }

                public class ABYSSCURSE3
                {
                    public static LocString NAME = UI.FormatAsHotkey("上升负荷·幻觉");
                    public static LocString TOOLTIP = "第3层「大断层」的诅咒：\n\n剧烈的眩晕中交织着幻视与幻听，分不清现实与深渊的低语。\n\n压力大幅增加，学习、机械操作与建造能力下降。";
                }

                public class ABYSSCURSE4
                {
                    public static LocString NAME = UI.FormatAsHotkey("上升负荷·剧痛");
                    public static LocString TOOLTIP = "第4层「巨人之杯」的诅咒：\n\n全身如同被撕裂般剧痛，七窍渗血。\n\n立即受到 20 点伤害，力量与挖掘能力下降。";
                }

                public class ABYSSCURSE5
                {
                    public static LocString NAME = UI.FormatAsHotkey("上升负荷·感觉剥夺");
                    public static LocString TOOLTIP = "第5层「亡骸之海」的诅咒：\n\n感觉被深渊一点点夺走，身体不再听从使唤，开始自残或停止思考。\n\n立即受到 30 点伤害，压力急剧增加，运动与学习能力严重下降。";
                }

                public class ABYSSCURSE6
                {
                    public static LocString NAME = UI.FormatAsHotkey("上升负荷·失去人性");
                    public static LocString TOOLTIP = "第6层「最终地」的诅咒：\n\n深渊的意志碾过了人类的边界——若能活下来，他们将不再是原来的自己。\n\n立即受到 50 点伤害，压力狂涨。幸存者将化为生骸。";
                }

                // —— 笛级（探窟家头衔的永久加成） ——

                public class ABYSSWHISTLEBLUE
                {
                    public static LocString NAME = UI.FormatAsBold("蓝笛探窟家");
                    public static LocString TOOLTIP = "已抵达「深层」的证明。\n\n挖掘 +2。";
                }

                public class ABYSSWHISTLEMOON
                {
                    public static LocString NAME = UI.FormatAsBold("苍笛探窟家");
                    public static LocString TOOLTIP = "已抵达「大断层」的证明。\n\n挖掘 +2，运动 +2。";
                }

                public class ABYSSWHISTLEBLACK
                {
                    public static LocString NAME = UI.FormatAsBold("黑笛探窟家");
                    public static LocString TOOLTIP = "已抵达「巨人之杯」的证明。\n\n挖掘 +3，运动 +3，力量 +2。";
                }

                public class ABYSSWHISTLEWHITE
                {
                    public static LocString NAME = UI.FormatAsBold("白笛探窟家");
                    public static LocString TOOLTIP = "已抵达「亡骸之海」的传奇证明，深受殖民地敬仰。\n\n挖掘 +5，运动 +5，力量 +3，压力减少。";
                }

                // —— 生骸化 ——

                public class ABYSSNAREHATE
                {
                    public static LocString NAME = UI.FormatAsBold("生骸");
                    public static LocString TOOLTIP = "深渊的诅咒已在其身上留下永恒的印记——但他们不再被深渊排斥。\n\n免疫一切上升负荷。\n运动 +5，挖掘 +5，力量 +3，压力减少；学习 -5。";
                }
            }
        }

        public class MISC
        {
            public class NOTIFICATIONS
            {
                public class ABYSS_CURSE
                {
                    public static LocString NAME = "上升负荷！";
                    public static LocString TOOLTIP = "从深渊上升的复制人正承受着与他们抵达深度相应的诅咒。";
                }

                public class ABYSS_CURSE_FINAL
                {
                    public static LocString NAME = UI.FormatAsHotkey("深渊凝视着你");
                    public static LocString TOOLTIP = "复制人从「最终地」开始上升——这可能是他们最后一次作为人类返回。";
                }

                public class ABYSS_WHISTLE_PROMOTE
                {
                    public static LocString NAME = UI.FormatAsBold("笛级晋升");
                    public static LocString TOOLTIP = "复制人的探窟深度刷新纪录，晋升为更高阶的探窟家。";
                }

                public class ABYSS_RELIC_FOUND
                {
                    public static LocString NAME = UI.FormatAsBold("深渊遗物！");
                    public static LocString TOOLTIP = "在深渊中挖掘出了远古的遗物。";
                }

                public class ABYSS_NAREHATE
                {
                    public static LocString NAME = UI.FormatAsBold("化为生骸");
                    public static LocString TOOLTIP = "从「最终地」的诅咒中幸存下来的复制人化为了生骸——深渊不再排斥他们。";
                }
            }
        }
    }
}
