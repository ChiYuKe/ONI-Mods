using System.Collections.Generic;
using Klei.AI;

namespace KModTool
{
    public class KModEffectConfigurator
    {
        public KModEffectConfigurator(string effectID, float duration, bool isNegative)
        {
            EffectID = effectID;
            EffectDuration = duration;
            IsNegativeEffect = isNegative;
            EffectName = Strings.Get("STRINGS.DUPLICANTS.MODIFIERS." + effectID.ToUpper() + ".NAME");
            EffectDescription = Strings.Get("STRINGS.DUPLICANTS.MODIFIERS." + effectID.ToUpper() + ".DESCRIPTION");
            ShowFloatingText = true;
            DisplayInUI = true;
            IconPath = "";
        }

        public KModEffectConfigurator SetEffectName(string name)
        {
            EffectName = name;
            return this;
        }

        public KModEffectConfigurator SetEffectDescription(string description)
        {
            EffectDescription = description;
            return this;
        }

        public KModEffectConfigurator AddAttributeModifier(string attributeID, float value, bool isMultiplier = false, bool uiOnly = false, bool readOnly = true)
        {
            AttributeModifiers = AttributeModifiers ?? new List<AttributeModifier>();
            AttributeModifiers.Add(new AttributeModifier(attributeID, value, EffectName, isMultiplier, uiOnly, readOnly));
            return this;
        }

        public KModEffectConfigurator SetAnimation(string animationName, float cooldown)
        {
            AnimationName = animationName;
            AnimationCooldown = cooldown;
            return this;
        }

        public KModEffectConfigurator AddAnimationPrecondition(Reactable.ReactablePrecondition condition)
        {
            AnimationPreconditions = AnimationPreconditions ?? new List<Reactable.ReactablePrecondition>();
            AnimationPreconditions.Add(condition);
            return this;
        }

        public KModEffectConfigurator HideFloatingText()
        {
            ShowFloatingText = false;
            return this;
        }

        public KModEffectConfigurator HideInUI()
        {
            DisplayInUI = false;
            return this;
        }

        public void ApplyTo(ModifierSet modifierSet)
        {
            Effect effect = new Effect(
                EffectID,
                EffectName,
                EffectDescription,
                EffectDuration,
                DisplayInUI,
                ShowFloatingText,
                IsNegativeEffect,
                AnimationName,
                AnimationCooldown,
                null,
                IconPath);

            if (AttributeModifiers != null)
            {
                effect.SelfModifiers = AttributeModifiers;
            }

            if (AnimationPreconditions != null)
            {
                effect.emotePreconditions = AnimationPreconditions;
            }

            modifierSet.effects.Add(effect);
        }

        private readonly string EffectID;
        private string EffectName;
        private string EffectDescription;
        private readonly float EffectDuration;
        private bool ShowFloatingText;
        private bool DisplayInUI;
        private readonly bool IsNegativeEffect;
        private List<AttributeModifier> AttributeModifiers;
        private string AnimationName;
        private float AnimationCooldown;
        private string IconPath;
        private List<Reactable.ReactablePrecondition> AnimationPreconditions;
    }
}
