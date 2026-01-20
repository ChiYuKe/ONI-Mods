namespace EternalDecay.Content.Config
{
    public class EDGameTags
    {
        // --- 生命状态标签 (Life Status) ---
        /// <summary>标记小人是寿终正寝，用于讣告或特殊遗物掉落判断</summary>
        public static readonly Tag DieOfOldAge = TagManager.Create("EternalDecay.DieOfOldAge");

        /// <summary>防止其他复制人对其产生哀悼压力 Buff</summary>
        public static readonly Tag NoMourning = TagManager.Create("EternalDecay.NoMourning");


        /// <summary>标记该对象已被分配</summary>
        public static readonly Tag IsLegacyAssigned = TagManager.Create("EternalDecay.IsLegacyAssigned");

        // --- 生物特性/环境标签 (Biological Traits) ---
        // 建议使用 "HasTrait" 或 "Status" 前缀来表达这是一个特性

        /// <summary>深渊恐惧：对真空或极端黑暗环境的负面标签</summary>
        public static readonly Tag HasAbyssophobia = TagManager.Create("EternalDecay.HasAbyssophobia");

        /// <summary>冰霜行者：在严寒环境中具有特殊加成的生物</summary>
        public static readonly Tag CoolWanderer = TagManager.Create("EternalDecay.CoolWanderer");

        /// <summary>炽热行者：耐高温生物</summary>
        public static readonly Tag HeatWanderer = TagManager.Create("EternalDecay.HeatWanderer");

        /// <summary>炽热金属持有者：特殊的交互标签</summary>
        public static readonly Tag MetalCarrier_Hot = TagManager.Create("EternalDecay.ScorchingMetalSharer");
    }
}