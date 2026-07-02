using KModTool;
using System.Collections.Generic;

namespace FoodandFoodBuffTutorialCase.Effects
{
    public static class ModEffects
    {
        public const string WELL_FED_EXAMPLE = "WellFedExample";

        public static void RegisterAll(ModifierSet modifierSet)
        {
            new KModEffectConfigurator(WELL_FED_EXAMPLE, 600f, false)
                .SetEffectName(STRINGS.DUPLICANTS.MODIFIERS.WELLFEDEXAMPLE.NAME)
                .SetEffectDescription(STRINGS.DUPLICANTS.MODIFIERS.WELLFEDEXAMPLE.DESCRIPTION)
                .AddAttributeModifier(Db.Get().Attributes.Athletics.Id, 2f)
                .ApplyTo(modifierSet);

            // 原版写法如下。这里注释掉，是为了展示 KModEffectConfigurator 背后做了什么。
            //
            // Effect effect = new Effect(
            //     WELL_FED_EXAMPLE,
            //     STRINGS.DUPLICANTS.MODIFIERS.WELLFEDEXAMPLE.NAME,
            //     STRINGS.DUPLICANTS.MODIFIERS.WELLFEDEXAMPLE.DESCRIPTION,
            //     600f,
            //     true,
            //     true,
            //     false);
            //
            // effect.SelfModifiers = new List<AttributeModifier>
            // {
            //     new AttributeModifier(
            //         Db.Get().Attributes.Athletics.Id,
            //         2f,
            //         STRINGS.DUPLICANTS.MODIFIERS.WELLFEDEXAMPLE.NAME)
            // };
            //
            // modifierSet.effects.Add(effect);
        }
    }
}
