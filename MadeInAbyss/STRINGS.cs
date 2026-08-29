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

            public static string FormatAsColor(string text, string color)
            {
                return "<color=" + color + ">" + text + "</color>";
            }
        }

        public class DUPLICANTS
        {
            public class MODIFIERS
            {
                // —— 上升负荷（深渊诅咒），ID 与 AbyssStatics.CurseEffectIds 对应，六层各有专属颜色 ——

                public class ABYSSCURSE1
                {
                    public static LocString NAME = UI.FormatAsColor("上升负荷·眩晕", "#C7C74A");
                    public static LocString TOOLTIP = "第1层「阿比斯之渊」的诅咒：\n\n从浅层返回时出现轻微的眩晕与恶心，注意力难以集中。\n\n压力增加，运动能力下降。";
                }

                public class ABYSSCURSE2
                {
                    public static LocString NAME = UI.FormatAsColor("上升负荷·恶心", "#F2913D");
                    public static LocString TOOLTIP = "第2层「诱惑之森」的诅咒：\n\n强烈头痛伴随四肢麻木，浑身像被重锤敲打过。\n\n压力增加，运动能力进一步下降，体力恢复减缓。";
                }

                public class ABYSSCURSE3
                {
                    public static LocString NAME = UI.FormatAsColor("上升负荷·幻觉", "#B04AF2");
                    public static LocString TOOLTIP = "第3层「大断层」的诅咒：\n\n剧烈的眩晕中交织着幻视与幻听，分不清现实与深渊的低语。\n\n压力大幅增加，学习、机械操作与建造能力下降。";
                }

                public class ABYSSCURSE4
                {
                    public static LocString NAME = UI.FormatAsColor("上升负荷·剧痛", "#F44A4A");
                    public static LocString TOOLTIP = "第4层「巨人之杯」的诅咒：\n\n全身如同被撕裂般剧痛，七窍渗血。\n\n立即受到 20 点伤害，力量与挖掘能力下降。";
                }

                public class ABYSSCURSE5
                {
                    public static LocString NAME = UI.FormatAsColor("上升负荷·感觉剥夺", "#4A9BF2");
                    public static LocString TOOLTIP = "第5层「亡骸之海」的诅咒：\n\n感觉被深渊一点点夺走，身体不再听从使唤，开始自残或停止思考。\n\n立即受到 30 点伤害，压力急剧增加，运动与学习能力严重下降。";
                }

                public class ABYSSCURSE6
                {
                    public static LocString NAME = UI.FormatAsColor("上升负荷·失去人性", "#F2418A");
                    public static LocString TOOLTIP = "第6层「最终地」的诅咒：\n\n深渊的意志碾过了人类的边界——从最终地上升的复制人将立即被重塑为生骸，变成一只随机的小动物。";
                }

                // —— 笛级（探窟家头衔的永久加成），按笛级进阶配色 ——

                public class ABYSSWHISTLEBLUE
                {
                    public static LocString NAME = UI.FormatAsColor("蓝笛探窟家", "#6BD26B");
                    public static LocString TOOLTIP = "已抵达「诱惑之森」的证明。\n\n挖掘 +2。";
                }

                public class ABYSSWHISTLEMOON
                {
                    public static LocString NAME = UI.FormatAsColor("苍笛探窟家", "#4AD2C7");
                    public static LocString TOOLTIP = "已抵达「大断层」的证明。\n\n挖掘 +2，运动 +2。";
                }

                public class ABYSSWHISTLEBLACK
                {
                    public static LocString NAME = UI.FormatAsColor("黑笛探窟家", "#A8A8B8");
                    public static LocString TOOLTIP = "已抵达「巨人之杯」的证明。\n\n挖掘 +3，运动 +3，力量 +2。";
                }

                public class ABYSSWHISTLEWHITE
                {
                    public static LocString NAME = UI.FormatAsColor("白笛探窟家", "#FFD24A");
                    public static LocString TOOLTIP = "已抵达「亡骸之海」的传奇证明，深受殖民地敬仰。\n\n挖掘 +5，运动 +5，力量 +3，压力减少。";
                }
            }

            public class DEATHS
            {
                public class ABYSSCURSE
                {
                    public static LocString NAME = "深渊的诅咒";
                    public static LocString DESCRIPTION = "{Target} 的生命被上升负荷夺走，化作了深渊的一部分。";
                }

                public class ABYSSNAREHATE
                {
                    public static LocString NAME = "化为生骸";
                    public static LocString DESCRIPTION = "{Target} 的血肉被深渊重塑——他们以另一种形态活了下来。";
                }
            }
        }

        public class ASSIGNABLE_SLOTS
        {
            public class AMMOPOUCH
            {
                public static LocString NAME = "弹药包";
            }
        }

        public class ITEMS
        {
            public class AMMO_POUCH_DAMAGED
            {
                public static LocString NAME = "损坏的弹药包";
                public static LocString DESC = "内衬的护符在抵挡诅咒时碎裂了，还在缓缓渗出粉红色的护符凝液。把它和布料一起送到服装纺织机，还能缝补一新。";
            }
        }

        public class ELEMENTS
        {
            public class WISHINGVAPOR
            {
                public static LocString NAME = "祈愿雾气";
                public static LocString DESC = "祈愿培养基沸腾时升起的粉色雾气。吸入无害，但那股香气总让人想起没能实现的愿望。";
            }

            public class TALISMANCONDENSATE
            {
                public static LocString NAME = "祈愿培养基";
                public static LocString DESC = "入口顺滑，矿物质丰富（铁元素含量爆表），带有淡淡的\"爸爸再爱我一次\"的回甘。粘度适中，比水厚重，比悔恨轻盈";
            }
        }

        public class EQUIPMENT
        {
            public class PREFABS
            {
                public class AMMO_POUCH
                {
                    public static LocString NAME = "探窟弹药包";
                    public static LocString DESC = "探窟家的标准装具，鼓鼓囊囊地塞满了备用工具和口粮，内衬的护符能抵挡浅层的诅咒——每抵挡一次便会损坏掉落。\n\n装备后：携带量 +800kg，挖掘 +2。\n可抵挡一次第 1~2 层的上升负荷，抵挡后损坏掉落。";
                    public static LocString RECIPE_DESC = "为复制人缝制一个探窟弹药包。";
                    public static LocString REPAIR_DESC = "将损坏的弹药包缝补一新。";
                }
            }
        }

        public class MISC
        {
            public class STATUSITEM_TOOLTIPS
            {
                public static LocString ABYSS_CURSE_DEAD = "他们不是在打盹——深渊的诅咒带走了他们，让他们化作了深渊的一部分。";
                public static LocString ABYSS_NAREHATE_DEAD = "他们不是在打盹——深渊重塑了他们的血肉，他们正以生骸的形态继续活着。";
            }

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

                public class ABYSS_POUCH_CONSUMED
                {
                    public static LocString NAME = UI.FormatAsBold("弹药包损坏");
                    public static LocString TOOLTIP = "探窟弹药包抵挡了一次上升负荷后损坏，掉落在地。";
                }

                public class ABYSS_NAREHATE
                {
                    public static LocString NAME = UI.FormatAsBold("化为生骸");
                    public static LocString TOOLTIP = "从「最终地」的诅咒中幸存下来的复制人被深渊重塑，变成了一只生骸。";
                }
            }
        }
    }
}
