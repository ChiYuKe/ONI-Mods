using FoodandFoodBuffTutorialCase.Items;

namespace FoodandFoodBuffTutorialCase
{
    public static class STRINGS
    {
        public static class ITEMS
        {
            public static class INDUSTRIAL_PRODUCTS
            {
                public static class MYFIRSTITEM
                {
                    public static LocString NAME = global::STRINGS.UI.FormatAsLink("我的第一个物品", MyFirstItemConfig.ID);
                    public static LocString DESC = "一个用于测试实体注册的掉落物。";
                }
            }

            public static class FOOD
            {
                public static class MYFIRSTFOOD
                {
                    public static LocString NAME = global::STRINGS.UI.FormatAsLink("我的第一个食物", MyFirstFoodConfig.ID);
                    public static LocString DESC = "一个会给复制人添加示例增益的测试食物。";
                }
            }
        }

        public static class DUPLICANTS
        {
            public static class MODIFIERS
            {
                public static class WELLFEDEXAMPLE
                {
                    public static LocString NAME = "吃得很开心";
                    public static LocString DESCRIPTION = "运动属性暂时提高。";
                    public static LocString TOOLTIP = "运动属性暂时提高。";
                }
            }
        }
    }
}
