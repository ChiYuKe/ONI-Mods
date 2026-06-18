using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STRINGS;
using TUNING;
using UnityEngine;
using static PlantElementAbsorber;
using static STRINGS.CODEX.STORY_TRAITS.LONELYMINION.GIFTRESPONSE_POPUP;

namespace TestPlanter
{
    public class PlantConfig : IEntityConfig, IHasDlcRestrictions
    {
        

        public GameObject CreatePrefab()
        {

            string id = "TestPlanter";
            string name = UI.FormatAsLink("测试植物", "TESTPLANTER");
            string desc = "植物描述";
            float mass = 1f;
            KAnimFile anim = Assets.GetAnim("TestPlanter_kanim"); 
            string initialAnim = "idle_full";
            int width = 1;
            int height = 1;
            EffectorValues decor = DECOR.BONUS.TIER1;
            EffectorValues noise = NOISE_POLLUTION.NONE;
            SimHashes element = SimHashes.Creature;
            List<Tag> additionalTags = new List<Tag>() { GameTags.Plant };
            float defaultTemperature = 293f;
            // 创建实体
            GameObject gameObject = EntityTemplates.CreatePlacedEntity(id, name, desc, mass,  anim, initialAnim,
                Grid.SceneLayer.BuildingFront, width, height,  decor, noise,  element,  additionalTags,  defaultTemperature);

            // 设置植物的初始属性
            EntityTemplates.ExtendEntityToBasicPlant(
                 gameObject,                          // 要扩展的GameObject实体，通常是预制体
                 temperature_lethal_low: 268.15f,    // 低温致死阈值（单位：开尔文），低于该温度植物死亡
                 temperature_warning_low: 283.15f,   // 低温警告阈值，低于该温度植物进入警告状态
                 temperature_warning_high: 303.15f,  // 高温警告阈值，高于该温度植物进入警告状态
                 temperature_lethal_high: 398.15f,   // 高温致死阈值，高于该温度植物死亡
                 safe_elements: new SimHashes[] { SimHashes.Oxygen, SimHashes.CarbonDioxide }, // 植物可安全存在的气体元素列表
                 pressure_sensitive: true,            // 是否对气压敏感（真：植物对气压变化有反应）
                 pressure_lethal_low: 0.1f,            // 低气压致死阈值（单位：kg/tile），低于该值植物死亡
                 pressure_warning_low: 0.2f,          // 低气压警告阈值，低于该值植物进入警告状态
                 crop_id: "TestFood",                      // 作物ID，null表示非作物植物；传入有效ID则扩展为作物
                 can_drown: true,                   // 是否可被淹死（真：植物会因为淹没而死亡）
                 can_tinker: true,                  // 是否允许通过“调整”（Tinker）来修改或互动
                 require_solid_tile: true,          // 是否需要固体地面块才能种植
                 require_Backwall_Foundation: false,
                 should_grow_old: true,             // 是否会随着时间变老
                 max_age: 2400f,                    // 最大寿命（单位：秒）
                 min_radiation: 0f,                 // 最小辐射阈值，低于该值影响生长
                 max_radiation: 2200f,              // 最大辐射阈值，高于该值影响生长或死亡
                 baseTraitId: "BasicPlantTrait",   // 关联的基础特质ID，用于植物状态定义
                 baseTraitName: UI.FormatAsLink("TestPlanter", "TESTPLANTER")     // 关联的基础特质名称
            );

            // 设置植物需求
            EntityTemplates.ExtendPlantToIrrigated(gameObject,
            [
                new ConsumeInfo
                {
                    tag = GameTags.Water,
                    massConsumptionRate = 0.033333335f, // 每秒消耗水的质量（单位：kg/s），这里设置为0.033333335kg/s
                    
                }
            ]);

            gameObject.AddOrGet<Crop>(); // 作物组件
            gameObject.AddOrGet<Growing>(); // 植物生长组件
            gameObject.AddOrGet<StandardCropTest>(); // 植物状态机
            gameObject.AddOrGet<DirectlyEdiblePlant_Growth>(); // 提供了对植物成熟度的检测和食用接口
            gameObject.AddOrGet<BlightVulnerable>(); // 植物易受枯萎影响 ,如果没做枯萎动画那就移除掉吧


            KBatchedAnimController animController = gameObject.GetComponent<KBatchedAnimController>();
            if (animController != null)
            {
                animController.animScale = 0.0007f;
            }



            IHasDlcRestrictions hasDlcRestrictions = this as IHasDlcRestrictions;


            // 注册种子
            GameObject seed = EntityTemplates.CreateAndRegisterSeedForPlant(
                gameObject,
                hasDlcRestrictions, // 植物ID
                SeedProducer.ProductionType.Harvest,
                "TestPlanterSeed", // 种子ID
                "测试种子",         // 显示名
                "这是测试植物的种子。", // 描述
                Assets.GetAnim("TestSeed_kanim"), // 动画
                "object",             // 初始动画
                1,                    // 数量
                new List<Tag>() { GameTags.CropSeed }, // 标签
                SingleEntityReceptacle.ReceptacleDirection.Top, // 可种植方向
                default(Tag),        // 果实Tag
                2,                   // 次级ID优先级
                "测试植物的家养描述", // 家养描述
                EntityTemplates.CollisionShape.CIRCLE,
                0.25f,
                0.25f,
                null,
                "",
                false
            );

            //KBatchedAnimController seedanimController = seed.GetComponent<KBatchedAnimController>();
            //if (seedanimController != null)
            //{
            //    seedanimController.animScale = 0.0004f;
            //}




            // 注册预览物体 就是你拿这个种子种植时候显示的白色描边
           GameObject TestPlanter_preview =  EntityTemplates.CreateAndRegisterPreviewForPlant(
                seed,                     // 种子 prefab
                "TestPlanter_preview",     // 预览物体ID
                Assets.GetAnim("TestPlanter_kanim"),
                "place",  // 初始动画
                1, 
                1
            );

            KBatchedAnimController TestPlanter_preview1 = TestPlanter_preview.GetComponent<KBatchedAnimController>();
            if (TestPlanter_preview1 != null)
            {
                TestPlanter_preview1.animScale = 0.0004f;
            }



            // 我这里直接使用的毛刺花的声音
            SoundEventVolumeCache.instance.AddVolume("bristleblossom_kanim", "PrickleFlower_harvest", NOISE_POLLUTION.CREATURES.TIER3);
            SoundEventVolumeCache.instance.AddVolume("bristleblossom_kanim", "PrickleFlower_grow", NOISE_POLLUTION.CREATURES.TIER3);





            return gameObject;
        }

        public void OnPrefabInit(GameObject inst)
        {
            inst.GetComponent<PrimaryElement>().Temperature = 288.15f;// 设置植物的初始温度为15摄氏度

        }

        public void OnSpawn(GameObject inst)
        {
        }





        public string[] GetRequiredDlcIds()
        {
            return DlcManager.EXPANSION1;
        }
        public string[] GetForbiddenDlcIds()
        {
            return null;
        }


        public string[] GetDlcIds()
        {
            return null;
        }
    }
}
