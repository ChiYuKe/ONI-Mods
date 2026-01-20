using Klei.AI;
using CykUtils; // 确保指向你 Builder 所在的命名空间

namespace RunningOutOfTime.Content.Core
{
    public static class ModEffects
    {
        
        public const string STABLE_PERIOD = "stable_period";
        public const string ROOT_SHUAILAO = "root_shuailao";




        /// <summary>
        /// 统一注册所有 Buff 的入口
        /// </summary>
        public static void RegisterAll(ModifierSet root)
        {

        // --- 稳定期 ---
        EffectBuilder.Create(STABLE_PERIOD, 1000f, false)
            .SetTexts("稳定期", "新生的复制人对世界充满渴望。")
            .Register(root);

        EffectBuilder.Create(ROOT_SHUAILAO, 0f, false)
            .SetTexts("衰老", "老了。")
            .Modifier(Db.Get().Attributes.Athletics.Id, -6f)   // 运动
            .Modifier(Db.Get().Attributes.Strength.Id, -5f)    // 力量
            .Modifier(Db.Get().Attributes.Digging.Id, -5f)     // 挖掘
            .Modifier(Db.Get().Attributes.Immunity.Id, -2f)    // 免疫系统
            .Register(root);




        //// --- 壮年期 (例如：全属性加成) ---
        //EffectBuilder.Create(PRIME_ID, -1f, false)
        //    .SetTexts("巅峰状态", "复制人的身体和经验达到了完美平衡。")
        //    .Modifier(Db.Get().Attributes.Athletics.Id, 2f)
        //    .Modifier(Db.Get().Attributes.Strength.Id, 2f)
        //    .Register(root);

        //// --- 老年期 (例如：动作迟缓但更有经验) ---
        //EffectBuilder.Create(ELDER_ID, -1f, true)
        //    .SetTexts("岁月痕迹", "长期的劳动在身上留下了印记。")
        //    .Modifier(Db.Get().Attributes.Athletics.Id, -5f)
        //    .Modifier(Db.Get().Attributes.Strength.Id, -2f)
        //    .Modifier(Db.Get().Attributes.Learning.Id, 3f)
        //    .Emote(Db.Get().Emotes.Minion.Cold, 30f) // 偶尔叹气
        //    .Register(root);
    }
    }
}