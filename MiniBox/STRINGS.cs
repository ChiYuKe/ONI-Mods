namespace MiniBox
{
    internal class STRINGS
    {
        public class CONFIGURATIONITEM
        {
            public class TRANSPORTROUTECONFIG
            {
                public class TRANSPORTTRACK
                {
                    public static LocString TITLE = "运输轨道单包质量上限（kg）";
                    public static LocString TOOLTIP = "设置运输轨道上每个包裹的最大质量。原版为 20 kg，数值越大，单个包裹可承载的材料越多。";
                    public static LocString CATEGORY = "容量与运输";
                }
            }

            public class MISCCONFIG
            {
                public static LocString CATEGORY = "通用功能";

                public class DIGGINGDROPRATE
                {
                    public static LocString TITLE = "挖掘资源产出倍率";
                    public static LocString TOOLTIP = "调整挖掘完成时生成的资源质量。原版为 0.5；数值越大，挖掘获得的资源越多。";
                }

                public class MINIONSLEEP
                {
                    public static LocString TITLE = "复制人无需睡眠";
                    public static LocString TOOLTIP = "开启后，普通复制人的耐力不会自然下降，因此不会因疲劳主动睡觉。";
                }

                public class WATERING
                {
                    public static LocString TITLE = "清理液体时忽略质量限制";
                    public static LocString TOOLTIP = "开启后，拖把不受液体质量上限限制，可以清理任意质量的液体。";
                }

                public class SUPERSPACESUIT
                {
                    public static LocString TITLE = "强化太空服属性";
                    public static LocString TOOLTIP = "提高太空服的挖掘、保温、耐高温和移动属性，并显著降低环境温度影响。";
                }

                public class NOFOOD
                {
                    public static LocString TITLE = "复制人无需进食";
                    public static LocString TOOLTIP = "开启后，普通复制人不会消耗卡路里，也不会因饥饿影响工作。";
                }

                public class NOBLADDER
                {
                    public static LocString TITLE = "复制人无需如厕";
                    public static LocString TOOLTIP = "开启后，普通复制人的膀胱不会增长，不需要使用厕所或厕所替代物。";
                }

                public class NOSTRESS
                {
                    public static LocString TITLE = "复制人压力不会上升";
                    public static LocString TOOLTIP = "开启后，复制人不会获得正向压力变化；已有压力仍可通过休息和娱乐降低。";
                }

                public class CARRYCAPACITY
                {
                    public static LocString TITLE = "复制人搬运能力倍率（x）";
                    public static LocString TOOLTIP = "调整普通复制人的最大搬运重量。原版为 200 kg；1.0 表示原版，2.0 表示两倍。";
                }
            }

            public class BUILDDINGS
            {
                public static LocString CATEGORY = "建筑属性";
                public static LocString CAPACITY_TOOLTIP = "设置该建筑的储存上限，单位为 kg。";
                public static LocString TOGGLE_TOOLTIP = "开启后，该建筑会启用标题所描述的行为；关闭后恢复原版行为。";
                public static LocString POWERCONSUMPTION_TOOLTIP = "设置建筑运行时的功耗，单位为 W。";

                public class GASRESERVOIR
                {
                    public static LocString CAPACITY = "储气库容量（kg）";
                    public static LocString OVERHEATABLE = "允许储气库过热";
                    public static LocString FOUNDATION = "储气库需要地基";
                }

                public class LIQUIDRESERVOIR
                {
                    public static LocString CAPACITY = "储液库容量（kg）";
                    public static LocString OVERHEATABLE = "允许储液库过热";
                    public static LocString FOUNDATION = "储液库需要地基";
                }

                public class MINERALDEOXIDIZER
                {
                    public static LocString TITLE = "氧气扩散器产氧量（kg/s）";
                    public static LocString OXYGENOUTPUT_TOOLTIP = "设置氧气扩散器每秒产出的氧气质量，单位为 kg/s。";
                    public static LocString ENERGYCONSUMPTIONWHENACTIVE = "氧气扩散器运行功耗（W）";
                    public static LocString FLOODABLE = "允许氧气扩散器被淹没";
                    public static LocString OVERHEATABLE = "允许氧气扩散器过热";
                    public static LocString HEATGENERATION = "氧气扩散器产生自热";
                    public static LocString OUTPUTTEMPERATURE = "产出氧气温度（°C）";
                    public static LocString OUTPUTTEMPERATURE_TOOLTIP = "设置氧气扩散器产出氧气的温度，单位为 °C。";
                }

                public class POWERTRANSFORMERSMALL
                {
                    public static LocString HEATGENERATION = "小型变压器产生自热";
                }

                public class POWERTRANSFORMER
                {
                    public static LocString HEATGENERATION = "大型变压器产生自热";
                }

                public class REFRIGERATOR
                {
                    public static LocString ENERGYCONSUMPTIONWHENACTIVE = "冰箱运行功耗（W）";
                    public static LocString FLOODABLE = "允许冰箱被淹没";
                    public static LocString OVERHEATABLE = "允许冰箱过热";
                    public static LocString CAPACITY = "冰箱容量（kg）";
                }

                public class SOLIDCONDUITOUTBOX
                {
                    public static LocString CAPACITY = "运输存放器容量（kg）";
                }

                public class SOLIDCONDUITINBOX
                {
                    public static LocString CAPACITY = "运输装载器容量（kg）";
                }

                public class STORAGELOCKER
                {
                    public static LocString CAPACITY = "储物箱容量（kg）";
                }
            }

            public class POWERCONFIG
            {
                public class WIRING
                {
                    public static LocString CATEGORY = "电线容量";
                    public static LocString TOOLTIP = "设置对应电线的最大输电功率，单位以选项标题为准。";
                    public static LocString WIRE500W = "500 W 线路最大功率（W）";
                    public static LocString WIRES = "普通电线最大功率（kW）";
                    public static LocString CONDUCTORS = "导电线最大功率（kW）";
                    public static LocString RUBBERWIRES = "橡胶电线最大功率（kW）";
                    public static LocString HIGHLOADWIRES = "高负荷电线最大功率（kW）";
                    public static LocString HIGHLOADCONDUCTORS = "高负荷导电线最大功率（kW）";
                }
            }

            public class PLANTCONFIG
            {
                public class PLANT
                {
                    public static LocString CATEGORY = "作物调整";
                    public static LocString ENABLECROPPATCH = "启用作物调整";
                    public static LocString ENABLECROPPATCH_TOOLTIP = "开启后，下面的植物生长周期和每次收获数量设置才会生效。";
                    public static LocString CROPDURATION = "生长周期（秒）";
                    public static LocString CROPDURATION_TOOLTIP = "植物完成一次生长所需的时间，单位为秒；数值越小，成熟越快。";
                    public static LocString NUMPRODUCED = "每次收获数量";
                    public static LocString NUMPRODUCED_TOOLTIP = "植物每次成熟时产出的数量。";

                    public static LocString BASICPLANTFOOD = "米虱木";
                    public static LocString PRICKLEFRUIT = "毛刺花";
                    public static LocString SWAMPFRUIT = "沼浆笼";
                    public static LocString MUSHROOM = "夜幕菇";
                    public static LocString COLDWHEATSEED = "冰霜小麦";
                    public static LocString SPICENUT = "火椒藤";
                    public static LocString BASICFABRIC = "顶针芦苇";
                    public static LocString SWAMPLILYFLOWER = "芳香百合";
                    public static LocString PLANTFIBER = "植物纤维（释气草产出）";
                    public static LocString WOODLOG = "乔木木材";
                    public static LocString SUGARWATER = "花蜜";
                    public static LocString SPACETREEBRANCH = "糖心树枝杈";
                    public static LocString HARDSKINBERRY = "刺壳果";
                    public static LocString CARROT = "羽叶果薯";
                    public static LocString OXYROCK = "气囊芦荟";
                    public static LocString VINEFRUIT = "漫花果";
                    public static LocString LETTUCE = "海生菜";
                    public static LocString KELP = "海梳蕨叶";
                    public static LocString BEANPLANTSEED = "小吃芽";
                    public static LocString OXYFERNSEED = "氧蕨种子";
                    public static LocString PLANTMEAT = "土星动物捕草";
                    public static LocString WORMSBASICFRUIT = "贫瘠虫果";
                    public static LocString WORMSUPERFRUIT = "虫果";
                    public static LocString DEWDRIP = "露珠";
                    public static LocString FERNFOOD = "巨蕨谷粒";
                    public static LocString SALT = "沙盐藤";
                    public static LocString WATER = "仙水掌";
                    public static LocString AMBER = "露饵花";
                    public static LocString GARDENFOODPLANTFOOD = "汗甜玉米";
                    public static LocString BUTTERFLY = "拟蛾";
                }
            }
        }
    }
}
