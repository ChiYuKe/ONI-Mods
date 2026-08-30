using HarmonyLib;
using KSerialization;
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

        public static ComplexRecipe repairRecipe;

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
        /// 配置页的“装备与设备”面板（OwnablesSidescreen）槽位列表是硬编码的，
        /// 这里把弹药包槽位追加进“套装”分类，否则注册了也不会显示。
        /// </summary>
        [HarmonyPatch(typeof(OwnablesSidescreen), "DefineCategories")]
        public static class OwnablesSidescreen_DefineCategories_Patch
        {
            public static void Postfix(OwnablesSidescreen __instance)
            {
                var categories = Traverse.Create(__instance)
                    .Field<OwnablesSidescreen.Category[]>("categories").Value;
                if (categories == null || categories.Length == 0)
                    return;

                EquipmentSlot pouchSlot = Db.Get().AssignableSlots.TryGet(SlotId) as EquipmentSlot;
                if (pouchSlot == null)
                    return;

                OwnablesSidescreenCategoryRow.Data suitData = categories[0].data;
                foreach (AssignableSlot slot in suitData.slots)
                {
                    if (slot != null && slot.Id == SlotId)
                        return; // 已追加过
                }

                // “套装”分类的槽位回调全部为 Always，重建时保持一致。
                List<OwnablesSidescreenCategoryRow.AssignableSlotData> slotsData =
                    new List<OwnablesSidescreenCategoryRow.AssignableSlotData>();
                foreach (AssignableSlot slot in suitData.slots)
                {
                    if (slot != null)
                        slotsData.Add(new OwnablesSidescreenCategoryRow.AssignableSlotData(slot, _ => true));
                }
                slotsData.Add(new OwnablesSidescreenCategoryRow.AssignableSlotData(pouchSlot, _ => true));

                categories[0] = new OwnablesSidescreen.Category(
                    categories[0].getAssignablesFn,
                    new OwnablesSidescreenCategoryRow.Data(suitData.name, slotsData.ToArray()));

                Traverse.Create(__instance)
                    .Field<OwnablesSidescreen.Category[]>("categories").Value = categories;
            }
        }

        /// <summary>
        /// 记录装备事件：验证弹药包的免疫（EffectImmunites）是否成功挂载。
        /// </summary>
        [HarmonyPatch(typeof(Equippable), "OnEquip")]
        public static class Equippable_OnEquip_Patch
        {
            public static void Postfix(Equippable __instance, AssignableSlotInstance slot)
            {
                EquipmentDef def = __instance.def;
                if (def == null)
                    return;
                int immunityCount = def.EffectImmunites != null ? def.EffectImmunites.Count : 0;
                string slotName = (slot != null && slot.slot != null) ? slot.slot.Name : "?";
                Debug.Log($"[MadeInAbyss] 装备事件：{def.Id} → 卡槽「{slotName}」，免疫数={immunityCount}");
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

                // 修补配方：损坏的弹药包 + 1 布料 → 探窟弹药包
                ComplexRecipe.RecipeElement[] repairInputs = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(new Tag[] { AmmoPouchDamagedConfig.ID.ToTag() }, 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature, "", false, false),
                    new ComplexRecipe.RecipeElement(GameTags.Fabrics, 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature, "", false, false),
                };
                ComplexRecipe.RecipeElement[] repairOutputs = new ComplexRecipe.RecipeElement[]
                {
                    new ComplexRecipe.RecipeElement(ItemId.ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature, false),
                };

                repairRecipe = new ComplexRecipe(ComplexRecipeManager.MakeRecipeID(FabricatorId, repairInputs, repairOutputs), repairInputs, repairOutputs)
                {
                    time = 20f,
                    description = Strings.Get("STRINGS.EQUIPMENT.PREFABS.AMMO_POUCH.REPAIR_DESC"),
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                    fabricators = new List<Tag> { FabricatorId },
                    sortOrder = 3,
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

            // Pickupable 的 [MyCmpAdd] 会在唤醒时补挂 Clearable 并刷一条错误日志，
            // 预先挂好避免报错（也使掉落在地时可被清扫指派）。
            go.AddOrGet<Clearable>();
            go.AddOrGet<Prioritizable>();

            KBatchedAnimController anim;
            if (go.TryGetComponent(out anim))
                anim.sceneLayer = Grid.SceneLayer.BuildingBack;

            // 模板本体不应留在场景中（否则开局会在世界原点/出生点看到实体），
            // 纺织机产出与生成流程会显式激活实例。
            go.SetActive(false);

            Debug.Log("[MadeInAbyss] 探窟弹药包装备已注册");
        }
    }

    /// <summary>
    /// 损坏的弹药包：抵挡一次诅咒后掉落的残骸，不可装备，
    /// 可与布料一起在服装纺织机上缝补修复。
    /// </summary>
    public class AmmoPouchDamagedConfig : IEntityConfig
    {
        public const string ID = "Ammo_Pouch_Damaged";

        public GameObject CreatePrefab()
        {
            GameObject go = EntityTemplates.CreateLooseEntity(
                ID,
                Strings.Get("STRINGS.ITEMS.AMMO_POUCH_DAMAGED.NAME"),
                Strings.Get("STRINGS.ITEMS.AMMO_POUCH_DAMAGED.DESC"),
                5f,
                false,
                Assets.GetAnim("shirt_decor01_kanim"),
                "object",
                Grid.SceneLayer.Ore,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.75f,
                0.4f,
                true,
                0,
                SimHashes.Creature,
                // IndustrialIngredient：进入沙盒生成列表的「工业产品」分类，也可作为储存过滤项。
                new List<Tag> { GameTags.IndustrialIngredient });
            go.AddOrGet<DamagedPouchLeaker>();
            go.AddOrGet<Clearable>();
            go.AddOrGet<Prioritizable>();
            return go;
        }

        public void OnPrefabInit(GameObject inst)
        {
            // 注意：不要在 OnPrefabInit 里 SetActive(false)！
            // prefabInitFn 会在实例激活（KInstantiate(...).SetActive(true)）后触发，
            // 这里关掉会把生成出来的损坏弹药包残骸再次隐藏，导致"挡下诅咒后没有掉落"。
            // 模板本身来自 inactive 的 baseEntityTemplate 链，天然是隐藏的，无需在此停用。
            Debug.Log("[MadeInAbyss] 损坏的弹药包实体已注册");
        }

        public void OnSpawn(GameObject spawned)
        {
        }
    }

    /// <summary>
    /// 损坏的弹药包会缓缓渗出「祈愿培养基」：每 5 秒 1kg，
    /// 单包总渗漏量 10~15kg（生成时随机），存入容器后暂停渗漏。
    /// </summary>
    public class DamagedPouchLeaker : KMonoBehaviour, ISim1000ms
    {
        /// <summary>已渗漏的总质量（kg）。</summary>
        [Serialize]
        private float leakedMass;

        /// <summary>渗漏上限（kg），生成时随机 10~15。</summary>
        [Serialize]
        private float leakCapacity;

        private float cycleTimer;

        private const float LeakIntervalS = 5f;
        private const float LeakTemperatureK = 298f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (leakCapacity <= 0f)
                leakCapacity = UnityEngine.Random.Range(10f, 15f);
        }

        public void Sim1000ms(float dt)
        {
            if (leakedMass >= leakCapacity)
                return;
            if (gameObject.HasTag(GameTags.Stored))
                return;

            cycleTimer += dt;
            if (cycleTimer < LeakIntervalS)
                return;
            cycleTimer -= LeakIntervalS;

            int cell = Grid.PosToCell(this);
            if (!Grid.IsValidCell(cell))
                return;

            Element element = ElementLoader.FindElementByHash(AbyssElements.TalismanCondensate);
            if (element == null || element.substance == null)
                return;

            float mass = Mathf.Min(1f, leakCapacity - leakedMass);
            SimMessages.AddRemoveSubstance(
                cell,
                AbyssElements.TalismanCondensate,
                CellEventLogger.Instance.ElementConsumerSimUpdate,
                mass,
                LeakTemperatureK,
                byte.MaxValue,
                0,
                true,
                -1);
            leakedMass += mass;
        }
    }
}
