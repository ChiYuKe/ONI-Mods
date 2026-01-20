using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunningOutOfTime.Content.Config
{
    internal class STRINGS
    {

        public class UI
        {

            public class INHERITANCEINFORMATION
            {
                public static LocString TOPTEXT = "继承详情";
                public static LocString BUTTONTEXT = "关闭窗口";

                public class ATTRIBUTESTITLE
                {
                    public static LocString NAME = "属性继承详情";
                    public static LocString DESC = "以下是该复制人所继承的属性详情";
                }
                public class SKILLSTITLE
                {
                    public static LocString NAME = "技能继承详情";
                    public static LocString DESC = "以下是该复制人所继承的技能详情";
                }
                public class TRAITSTITLE
                {
                    public static LocString NAME = "特质继承详情";
                    public static LocString DESC = "以下是该复制人所继承的特质详情";

                }
            }


            public class INFOSCREEN
            {
                public class TRAITS
                {
                    public static LocString NAME = "特质遗产";

                }
                public class RESUME
                {
                    public static LocString NAME = "技能知识遗产";

                }
                public class ATTRIBUTES
                {
                    public static LocString NAME = "属性知识遗产";

                }

            }
        }

        public class ITEMS 
        {
            public class INDUSTRIAL_PRODUCTS 
            {
                public class MINIONBRAIN 
                {
                    public static LocString NAME = "罐中脑";
                    public static LocString DESC = "复制人最后的火光了";

                }
                public class MINIONBRAINBAD
                {
                    public static LocString NAME = "泄露的罐中脑";
                    public static LocString DESC = "虽然不能用了但还能吃";

                }
            }
        }

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class WATERCOOLER
                {

                    public class OPTION_TOOLTIPS
                    {
                        public static LocString KMODMINIBRAINBAD = "罐中脑脑积液";
                    }
                }
            }
        }
        public class KUI
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
                public class LUMINESCENCEKING
                {
                    public static LocString NAME = "帝皇";
                    public static LocString TOOLTIP = "发光";
                }
                public class ETERNALDECAY_SHUAILAO 
                {
                    public static LocString NAME = "衰老";
                    public static LocString TOOLTIP = "衰老带来的虚弱感，极大地影响了复制人的各项机能";
                }
                public class ETERNALDECAY_BAOZHIQI
                {
                    public static LocString NAME = "稳定期";
                    public static LocString TOOLTIP = "稳定期为 0 时，罐中脑会直接损坏";
                }

                public class HEATWANDERER
                {
                    public static LocString NAME = KUI.FormatAsHotkey("散热者");
                    public static LocString TOOLTIP = $"{KUI.FormatAsBold("继承失败所造成的负面效果:")} \n\n  周期性的使自身降温，周围物体升温 \n 降温自身温度不会低于15 ℃，升温周围物体不会高于50 ℃";
                }
                public class COOLWANDERER
                {
                    public static LocString NAME = KUI.FormatAsHotkey("积热者");
                    public static LocString TOOLTIP = $"{KUI.FormatAsBold("继承失败所造成的负面效果:")} \n\n 周期性的使自身升温，周围物体降温 \n 降温周围物体温度不会低于0 ℃，升温自身不会高于55";
                }
                public class SCORCHINGMETALSHARER
                {
                    public static LocString NAME = KUI.FormatAsHotkey("炽热金属分享者");
                    public static LocString TOOLTIP = $"{KUI.FormatAsBold("食用特殊食物所造成的负面效果:")} \n\n 复制觉得有好东西要分享，至少这些东西比原神。。。烧  \n\n 周期性的向周围抛下200℃高温金属碎片 ";
                    public static LocString DESCRIPTION = "复制人觉得这个食物感觉比. . . .原神. . .烧 ，而感到开心的丢高温金属";
                }


                public class ABYSSO
                {
                    public class ABYSSO0
                    {
                        public static LocString NAME = "深渊恐惧症";
                        public static LocString TOOLTIP = "以打印舱为水平面，越低则深渊恐惧症等级越高，最大为LV5";
                    }
                    public class ABYSSO1
                    {
                        public static LocString NAME = "深渊恐惧症 LV1";
                        public static LocString TOOLTIP = "轻微的深渊恐惧症，感觉还能坚持";
                    }
                    public class ABYSSO2
                    {
                        public static LocString NAME = "深渊恐惧症 LV2";
                        public static LocString TOOLTIP = "中度的深渊恐惧症，偶尔会感到不适";
                    }
                    public class ABYSSO3
                    {
                        public static LocString NAME = "深渊恐惧症 LV3";
                        public static LocString TOOLTIP = "严重的深渊恐惧症，时常会感到不适";
                    }
                    public class ABYSSO4
                    {
                        public static LocString NAME = "深渊恐惧症 LV4";
                        public static LocString TOOLTIP = "极严重的深渊恐惧症，频繁地感到不适";
                    }
                    public class ABYSSO5
                    {
                        public static LocString NAME = "深渊恐惧症 LV5";
                        public static LocString TOOLTIP = "致命的深渊恐惧症，几乎无法正常工作";


                    }
                }
            }

            public class AGEATTRIBUTE
            {
                public static LocString NAME = "年龄: {0} / {1} 周期";
                public static LocString TOOLTIP = "当前年龄为 {0}，上限 {1}";
            }



            public class STATS
            {
                public class AGEATTRIBUTE
                {
                    public static LocString NAME = "年龄";
                    public static LocString TOOLTIP = "人终究难逃一死";
                }
            }



        }

        public class MISC 
        {
            public class NOTIFICATIONS
            {
                public class DEATHROULETTE
                {
                    public static LocString NAME = "进入衰老期"; 
                }
                public class DEBUFFINFO
                {
                    public static LocString TOOLTIP = "当前复制人由于中途停止导致出现免疫排斥反应";
                    public class IMMUNERESPONSE
                    {
                        public static LocString NAME = "免疫排斥反应";
                    }
                }
                
                public class AGEATTRIBUTE
                {
                    public static LocString NAME = "自然衰老";
                    public static LocString TOOLTIP = "{0} 已经活到了生命的尽头。";
                }
            }
            
            public class NEWMINIONNAME
            {
                public static LocString NAME = " - 罐中脑";
            }



            public class TAGS
            {
                public class MINIONBRAIN
                {
                    public static LocString NAME = "复制人大脑";
                  
                }
            }



        }


        public class DEATHS
        {
            public class AGING
            {
                public static LocString NAME = "衰老死亡";
                public static LocString DESCRIPTION = "这幅身躯终究是支撑不住，还是死亡了，但留下来宝贵传承。";
            }
        }
    }
}
