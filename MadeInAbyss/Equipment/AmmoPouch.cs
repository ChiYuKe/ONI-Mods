using HarmonyLib;
using Klei.AI;
using STRINGS;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 探窟弹药包：新增“弹药包”装备槽（卡槽）与对应装备物。
    /// 装备后提升携带量与挖掘能力，方便复制人深入深渊带粮带工具。
    /// 纹理复用原版背心（shirt_decor01_kanim）。
    /// </summary>
    public static class AmmoPouch
    {
        public const string SlotId = "AmmoPouch";
        public const string ItemId = "Ammo_Pouch";
        public const string FabricatorId = "ClothingFabricator";

        public static ComplexRecipe recipe;

        /// <summary>
        /// 在可分配槽位资源集中注册“弹药包”装备槽：
        /// MinionAssignablesProxy.ConfigureAssignableSlots 会遍历该资源集，
        /// 为每个复制人自动创建对应的装备槽实例。
        /// </summary>
        [HarmonyPatch(typeof(Database.AssignableSlots), MethodType.Constructor)]
        public static class AssignableSlots_Constructor_Patch
        {
            public static void Postfix(Database.AssignableSlots __instance)
            {
                if (__instance.TryGet(SlotId) != null)
                    return;
                __instance.Add(new EquipmentSlot(SlotId, Strings.Get("STRINGS.ASSIGNABLE_SLOTS.AMMOPOUCH.NAME")));
            }
        }

        /// <summary>
        /// 在服装纺织机上注册弹药包配方。
        /// </summary>
        [HarmonyPatch(typeof(Db), "Initialize")]
        public static class Db_Initialize_Patch
        {
            public static void Postfix()
            {
                if (recipe != null)
                    return;

                ComplexRecipe.RecipeElement[] inputs = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(GameTags.Fabrics, 2f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature, "", false, false),
                    new ComplexRecipe.RecipeElement(new Tag[] { SimHashes.Steel.CreateTag() }, 25f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature, "", false, false),
                };
                ComplexRecipe.RecipeElement[] outputs = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(ItemId.ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature, false),
                };

                recipe = new ComplexRecipe(ComplexRecipeManager.MakeRecipeID(FabricatorId, inputs, outputs), inputs, outputs)
                {
                    time = 40f,
                    description = Strings.Get("STRINGS.EQUIPMENT.PREFABS.AMMO_POUCH.RECIPE_DESC"),
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                    fabricators = new List<Tag> { FabricatorId },
                    sortOrder = 2,
                };
            }
        }
    }

    /// <summary>
    /// 弹药包装备物定义；IEquipmentConfig 会被游戏自动扫描注册。
    /// </summary>
    public class AmmoPouchConfig : IEquipmentConfig
    {
        public EquipmentDef CreateEquipmentDef()
        {
            string sourceName = Strings.Get("STRINGS.EQUIPMENT.PREFABS.AMMO_POUCH.NAME");
            List<AttributeModifier> modifiers = new List<AttributeModifier>
            {
                // 携带量 +800kg，挖掘 +2
                new AttributeModifier(Db.Get().Attributes.CarryAmount.Id, 800f, sourceName, false, false, true),
                new AttributeModifier(Db.Get().Attributes.Digging.Id, 2f, sourceName, false, false, true),
            };

            // 纹理复用原版花哨背心（胸甲式挂带，看起来就像探窟装具）。
            EquipmentDef def = EquipmentTemplates.CreateEquipmentDef(
                AmmoPouch.ItemId,
                AmmoPouch.SlotId,
                SimHashes.Creature,
                5f,
                "shirt_decor01_kanim",
                global::TUNING.EQUIPMENT.VESTS.SNAPON0,
                "body_shirt_decor01_kanim",
                4,
                modifiers,
                global::TUNING.EQUIPMENT.VESTS.SNAPON1,
                true,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.75f,
                0.4f,
                new Tag[] { GameTags.Clothes, GameTags.PedestalDisplayable },
                null);

            def.RecipeDescription = Strings.Get("STRINGS.EQUIPMENT.PREFABS.AMMO_POUCH.RECIPE_DESC");

            // 抗诅咒：装备期间免疫浅层（第 1~2 层）的上升负荷，卸下后失效。
            ResourceSet<Effect> effects = Db.Get().effects;
            foreach (string curseId in new[] { "AbyssCurse1", "AbyssCurse2" })
            {
                Effect curse = effects.TryGet(curseId);
                if (curse != null)
                    def.EffectImmunites.Add(curse);
            }
            return def;
        }

        public void DoPostConfigure(GameObject go)
        {
            Equippable equippable = go.GetComponent<Equippable>();
            if (equippable != null)
                equippable.SetQuality(global::QualityLevel.Good);

            KBatchedAnimController anim;
            if (go.TryGetComponent(out anim))
                anim.sceneLayer = Grid.SceneLayer.BuildingBack;
        }
    }
}
