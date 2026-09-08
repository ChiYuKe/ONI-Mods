using Klei.AI;
using STRINGS;
using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 注册所有自定义效果：六个上升负荷诅咒、四个笛级永久加成、生骸化。
    /// </summary>
    public static class AbyssEffects
    {
        public static void RegisterAll(ModifierSet modifiers)
        {
            Database.Attributes attributes = Db.Get().Attributes;
            Database.Amounts amounts = Db.Get().Amounts;
            float duration = AbyssConfig.Instance.CurseDurationS;

            // —— 上升负荷 · 六层诅咒 ——

            Add(modifiers, "AbyssCurse1", duration, isBad: true, mods => mods
                .Add(amounts.Stress.deltaAttribute.Id, 5f / 600f)   // 压力 +5%/周期
                .Add(attributes.Athletics.Id, -2f));                // 运动 -2

            Add(modifiers, "AbyssCurse2", duration, isBad: true, mods => mods
                .Add(amounts.Stress.deltaAttribute.Id, 10f / 600f)  // 压力 +10%/周期
                .Add(attributes.Athletics.Id, -4f)                  // 运动 -4
                .Add(amounts.Stamina.deltaAttribute.Id, -2f / 600f)); // 体力 -2%/周期

            Add(modifiers, "AbyssCurse3", duration, isBad: true, mods => mods
                .Add(amounts.Stress.deltaAttribute.Id, 15f / 600f)  // 压力 +15%/周期
                .Add(attributes.Learning.Id, -5f)                   // 学习 -5
                .Add(attributes.Machinery.Id, -5f)                  // 机械操作 -5
                .Add(attributes.Construction.Id, -3f));             // 建造 -3

            // 第 4~6 层的扣血为施放诅咒时的一次性伤害（数值见 AbyssStatics.CurseInstantDamageHp）。

            Add(modifiers, "AbyssCurse4", duration, isBad: true, mods => mods
                .Add(attributes.Strength.Id, -5f)                   // 力量 -5
                .Add(attributes.Digging.Id, -5f));                  // 挖掘 -5

            Add(modifiers, "AbyssCurse5", duration, isBad: true, mods => mods
                .Add(amounts.Stress.deltaAttribute.Id, 25f / 600f)  // 压力 +25%/周期
                .Add(attributes.Athletics.Id, -8f)                  // 运动 -8
                .Add(attributes.Learning.Id, -6f));                 // 学习 -6

            Add(modifiers, "AbyssCurse6", duration, isBad: true, mods => mods
                .Add(amounts.Stress.deltaAttribute.Id, 40f / 600f)); // 压力 +40%/周期

            // —— 笛级永久加成（赤笛为荣誉头衔，无效果） ——

            Add(modifiers, "AbyssWhistleBlue", 0f, isBad: false, mods => mods
                .Add(attributes.Digging.Id, 2f));                   // 挖掘 +2

            Add(modifiers, "AbyssWhistleMoon", 0f, isBad: false, mods => mods
                .Add(attributes.Digging.Id, 2f)                     // 挖掘 +2
                .Add(attributes.Athletics.Id, 2f));                 // 运动 +2

            Add(modifiers, "AbyssWhistleBlack", 0f, isBad: false, mods => mods
                .Add(attributes.Digging.Id, 3f)                     // 挖掘 +3
                .Add(attributes.Athletics.Id, 3f)                   // 运动 +3
                .Add(attributes.Strength.Id, 2f));                  // 力量 +2

            Add(modifiers, "AbyssWhistleWhite", 0f, isBad: false, mods => mods
                .Add(attributes.Digging.Id, 5f)                     // 挖掘 +5
                .Add(attributes.Athletics.Id, 5f)                   // 运动 +5
                .Add(attributes.Strength.Id, 3f)                    // 力量 +3
                .Add(amounts.Stress.deltaAttribute.Id, -5f / 600f)); // 压力 -5%/周期

            // —— 深渊祝福：6→5 上升被弹药包挡下的诅咒化作馈赠 ——

            // 时长 6 周期；存在期间不会重复获得（见 ApplyBlessing）。
            float blessingDuration = BlessingDurationCycles * 600f;

            // 固定部分：生命恢复 + 呼吸 + 减压（BreathDelta 为复制人呼吸恢复属性，原版肺病同款）。
            Add(modifiers, "AbyssBlessing", blessingDuration, isBad: false, mods => mods
                .Add(amounts.HitPoints.deltaAttribute.Id, 30f / 600f)    // 生命恢复 +30/周期
                .Add(amounts.Breath.deltaAttribute.Id, 0.1f)             // 呼吸恢复 +100 克/秒
                .Add(amounts.Stress.deltaAttribute.Id, -45f / 600f));    // 压力 -45%/周期

            // 随机部分：六条馈赠，授予时随机取两条。
            Add(modifiers, "AbyssGiftDig", blessingDuration, isBad: false, mods => mods
                .Add(attributes.Digging.Id, 3f));                       // 挖掘 +3
            Add(modifiers, "AbyssGiftAthletics", blessingDuration, isBad: false, mods => mods
                .Add(attributes.Athletics.Id, 3f));                     // 运动 +3
            Add(modifiers, "AbyssGiftStrength", blessingDuration, isBad: false, mods => mods
                .Add(attributes.Strength.Id, 3f));                      // 力量 +3
            Add(modifiers, "AbyssGiftLearning", blessingDuration, isBad: false, mods => mods
                .Add(attributes.Learning.Id, 5f));                      // 学习 +5
            Add(modifiers, "AbyssGiftCalm", blessingDuration, isBad: false, mods => mods
                .Add(amounts.Stress.deltaAttribute.Id, -10f / 600f));   // 压力 -10%/周期
            Add(modifiers, "AbyssGiftStamina", blessingDuration, isBad: false, mods => mods
                .Add(amounts.Stamina.deltaAttribute.Id, 5f / 600f));    // 体力 +5%/周期

            // —— 生骸化：永久效果——深渊重塑的躯体老化极慢，繁殖力旺盛。
            Add(modifiers, AbyssStatics.NarehateEffectId, 0f, isBad: false, mods => mods
                .Add(amounts.Age.deltaAttribute.Id, -0.9f / 600f)      // 年龄增长 -0.9/周期
                .Add(amounts.Fertility.deltaAttribute.Id, 30f / 600f)); // 繁殖度 +30%/周期
        }

        /// <summary>深渊祝福持续时间（周期）。</summary>
        public const float BlessingDurationCycles = 6f;

        /// <summary>
        /// 6→5（最终地）上升被弹药包挡下时授予：
        /// 固定的「深渊祝福」（生命恢复 + 氧气）+ 两条互不重复的随机「深渊馈赠」，
        /// 持续 6 周期；祝福存在期间不会重复获得。
        /// </summary>
        public static void ApplyBlessing(Effects effects)
        {
            if (effects == null)
                return;
            if (effects.Get("AbyssBlessing") != null)
                return; // 祝福仍在身上，不重复授予
            effects.Add("AbyssBlessing", true);

            List<string> gifts = new List<string>
            {
                "AbyssGiftDig", "AbyssGiftAthletics", "AbyssGiftStrength",
                "AbyssGiftLearning", "AbyssGiftCalm", "AbyssGiftStamina",
            };
            for (int i = 0; i < 2 && gifts.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, gifts.Count);
                effects.Add(gifts[idx], true);
                gifts.RemoveAt(idx);
            }
        }

        private class ModifierList
        {
            private readonly string effectName;
            internal readonly List<AttributeModifier> list = new List<AttributeModifier>();

            public ModifierList(string effectName)
            {
                this.effectName = effectName;
            }

            public ModifierList Add(string attributeId, float value)
            {
                list.Add(new AttributeModifier(attributeId, value, effectName, false, false, true));
                return this;
            }
        }

        private static void Add(
            ModifierSet modifiers,
            string id,
            float duration,
            bool isBad,
            System.Func<ModifierList, ModifierList> build,
            string[] immunityEffects = null)
        {
            if (modifiers.effects.Exists(id))
                return;

            string name = Strings.Get($"STRINGS.DUPLICANTS.MODIFIERS.{id.ToUpper()}.NAME");
            string tooltip = Strings.Get($"STRINGS.DUPLICANTS.MODIFIERS.{id.ToUpper()}.TOOLTIP");

            Effect effect;
            if (immunityEffects != null)
            {
                effect = new Effect(
                    id, name, tooltip, duration,
                    immunityEffects,
                    show_in_ui: true, trigger_floating_text: true, is_bad: isBad,
                    emote: null, max_initial_delay: 0f, stompGroup: null, showStatusInWorld: false);
            }
            else
            {
                effect = new Effect(
                    id, name, tooltip, duration,
                    show_in_ui: true, trigger_floating_text: true, is_bad: isBad,
                    emoteAnim: null, emote_cooldown: -1f, stompGroup: null, custom_icon: null);
            }

            effect.SelfModifiers = build(new ModifierList(name)).list;
            modifiers.effects.Add(effect);
        }
    }
}
