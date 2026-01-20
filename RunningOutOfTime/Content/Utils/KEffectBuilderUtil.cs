using System;
using System.Collections.Generic;
using Klei.AI;

namespace CykUtils
{
    /// <summary>
    /// 针对《缺氧》Effect 系统的链式配置工具
    /// </summary>
    public class EffectBuilder
    {
        private readonly string id;
        private readonly float duration;
        private readonly bool isBad;

        private string name;
        private string description;
        private bool showFloatingText = true;
        private bool displayInUI = true;
        private string iconPath = string.Empty;
        private Klei.AI.Emote emoteAnim = null;
        private float emoteCooldown = 0f;

        private readonly List<AttributeModifier> modifiers = new List<AttributeModifier>();
        private readonly List<Reactable.ReactablePrecondition> preconditions = new List<Reactable.ReactablePrecondition>();


        private EffectBuilder(string id, float duration, bool isNegative)
        {
            this.id = id;
            this.duration = duration;
            this.isBad = isNegative;
        }

        public static EffectBuilder Create(string id, float durationDays, bool isbad = false)
        {
          
            return new EffectBuilder(id, durationDays, isbad);
        }

        public EffectBuilder SetTexts(string name, string description = "")
        {
            this.name = name;
            this.description = description;
            return this;
        }

        /// <summary>
        /// 添加属性修改器
        /// </summary>
        /// <param name="attributeId">属性ID (如 Db.Get().Attributes.Learning.Id)</param>
        /// <param name="value">数值</param>
        /// <param name="isMultiplier">是否为百分比倍数</param>
        public EffectBuilder Modifier(string attributeId, float value, bool isMultiplier = false)
        {
            modifiers.Add(new AttributeModifier(attributeId, value, name, isMultiplier));
            return this;
        }

        public EffectBuilder Emote(Klei.AI.Emote animName, float cooldown = 60f)
        {
            this.emoteAnim = animName;
            this.emoteCooldown = cooldown;
            return this;
        }

        public EffectBuilder HideFromUI()
        {
            this.displayInUI = false;
            this.showFloatingText = false;
            return this;
        }

        // 核心构建逻辑
        public Effect Build()
        {

            var finalName = name ?? Strings.Get($"STRINGS.DUPLICANTS.MODIFIERS.{id.ToUpper()}.NAME");
            var finalDesc = description ?? Strings.Get($"STRINGS.DUPLICANTS.MODIFIERS.{id.ToUpper()}.TOOLTIP");

            var effect = new Effect(
                id: id,
                name: finalName,
                description: finalDesc,
                duration: duration,
                show_in_ui: displayInUI,
                trigger_floating_text: showFloatingText,
                is_bad: isBad,
                emote: emoteAnim,
                emote_cooldown: emoteCooldown
            );

            effect.SelfModifiers = modifiers;
            if (preconditions.Count > 0) effect.emotePreconditions = preconditions;

            return effect;
        }

        /// <summary>
        /// 一键注册到游戏资源库
        /// </summary>
        public void Register(ModifierSet root)
        {
            if (root == null) return;
            root.effects.Add(Build());
        }

















    }
}