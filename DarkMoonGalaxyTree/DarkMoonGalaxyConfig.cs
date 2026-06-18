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

namespace DarkMoonGalaxy
{
    public class DarkMoonGalaxyConfig : IEntityConfig
    {


        public GameObject CreatePrefab()
        {

            string id = DarkMoonGalaxyConfig.Id;
            string name = UI.FormatAsLink(STRINGS.SPECIES.DARKMOONGALAXY.NAME, DarkMoonGalaxyConfig.Id);
            string desc = STRINGS.SPECIES.DARKMOONGALAXY.DESC;
            float mass = 1f;
            KAnimFile anim = Assets.GetAnim("DarkMoonGalaxy_kanim");
            string initialAnim = "idle_full";
            int width = 1;
            int height = 2;
            EffectorValues decor = DECOR.BONUS.TIER1;
            EffectorValues noise = NOISE_POLLUTION.NONE;
            SimHashes element = SimHashes.Creature;
            List<Tag> additionalTags = new List<Tag>() { GameTags.Plant };
            float defaultTemperature = 293f;

            GameObject gameObject = EntityTemplates.CreatePlacedEntity(id, name, desc, mass, anim, initialAnim,
                Grid.SceneLayer.BuildingFront, width, height, decor, noise, element, additionalTags, defaultTemperature);

            // 设置植物的初始属性
            EntityTemplates.ExtendEntityToBasicPlant(
                 gameObject,                         
                 temperature_lethal_low: 268.15f,    //用的开尔文温度，植物致死低温
                 temperature_warning_low: 283.15f,  
                 temperature_warning_high: 303.15f, 
                 temperature_lethal_high: 398.15f,  
                 safe_elements: new SimHashes[] { SimHashes.Oxygen, SimHashes.CarbonDioxide }, // 植物可安全存在的气体元素列表
                 pressure_sensitive: true,            // 是否对气压敏感（true：植物对气压变化有反应）
                 pressure_lethal_low: 0.1f,           
                 pressure_warning_low: 0.2f,         
                 crop_id: DarkMoonGalaxyFruitConfig.Id,      // 农作物食物ID，null表示非作物植物；传入有效ID则扩展为作物
                 can_drown: true,                  
                 can_tinker: true,                  // 是否允许通过“调整”（Tinker）来修改或互动
                 require_solid_tile: true,         
                 require_Backwall_Foundation: false,
                 should_grow_old: true,             
                 max_age: 2400f,                    
                 min_radiation: 0f,                 
                 max_radiation: 2200f,            
                 baseTraitId: "BasicPlantTrait",   
                 baseTraitName: UI.FormatAsLink(DarkMoonGalaxyConfig.Id, DarkMoonGalaxyConfig.Id.ToUpperInvariant())     // 关联的基础特质名称
            );

            // 植物需求
            EntityTemplates.ExtendPlantToIrrigated(gameObject,
            [
                new ConsumeInfo
                {
                    tag = GameTags.CarbonDioxide,
                    massConsumptionRate = 0.033333335f, 
                    
                }
            ]);

            // 挥发组件
            //Sublimates sublimates = gameObject.AddOrGet<Sublimates>(); 
            //sublimates.spawnFXHash = SpawnFXHashes.OxygenEmissionBubbles;
            //sublimates.info = new Sublimates.Info(0.03f, 0f, 0.8f, 0f, SimHashes.Oxygen, byte.MaxValue, 0);


            gameObject.AddOrGet<Crop>(); // 作物组件
            gameObject.AddOrGet<Growing>(); // 植物生长组件
            gameObject.AddOrGet<DarkMoonGalaxy>(); // 植物状态机，这里用的自己写的
            gameObject.AddOrGet<DirectlyEdiblePlant_Growth>(); // 提供了对植物成熟度的检测和食用接口
            gameObject.AddOrGet<BlightVulnerable>(); // 植物易受枯萎影响 ,如果没做枯萎动画那就移除掉吧


            //KBatchedAnimController animController = gameObject.GetComponent<KBatchedAnimController>();
            //if (animController != null)
            //{
            //    animController.animScale = 0.0007f;
            //}



            IHasDlcRestrictions hasDlcRestrictions = this as IHasDlcRestrictions;


            // 注册种子
            GameObject seed = EntityTemplates.CreateAndRegisterSeedForPlant(
                gameObject,
                hasDlcRestrictions, // 植物ID
                SeedProducer.ProductionType.Harvest,
                SeedId, 
                STRINGS.SPECIES.DARKMOONGALAXYSEED.NAME,
                STRINGS.SPECIES.DARKMOONGALAXYSEED.DESC, 
                Assets.GetAnim("DarkMoonGalaxySeed_kanim"), 
                "object",             // 初始动画
                1,                    // 数量
                new List<Tag>() { GameTags.CropSeed }, 
                SingleEntityReceptacle.ReceptacleDirection.Top, // 可种植方向
                default(Tag),        // 果实Tag
                2,                   // 次级ID优先级
                STRINGS.SPECIES.DARKMOONGALAXYSEED.DOMESTICATEDDESC, 
                EntityTemplates.CollisionShape.CIRCLE,
                0.25f,
                0.25f,
                null,
                "",
                false
            );
            // 注册预览物体 就是你拿这个种子种植时候显示的白色描边
            GameObject TestPlanterplace = EntityTemplates.CreateAndRegisterPreviewForPlant(
                 seed,                     // 种子 prefab
                 "DarkMoonGalaxy_place",     // 预览物体ID
                 Assets.GetAnim("DarkMoonGalaxy_kanim"),
                 "place",  // 初始动画
                 1,
                 2
             );



            //KBatchedAnimController seedanimController = seed.GetComponent<KBatchedAnimController>();
            //if (seedanimController != null)
            //{
            //    seedanimController.animScale = 0.0010f;
            //}

            //KBatchedAnimController TestPlanter_preview1 = TestPlanter_preview.GetComponent<KBatchedAnimController>();
            //if (TestPlanter_preview1 != null)
            //{
            //    TestPlanter_preview1.animScale = 0.0004f;
            //}



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



        public const string Id = "DarkMoonGalaxy";
        public const string SeedId = "DarkMoonGalaxySeed";



        public string[] GetDlcIds()
        {
            return null;
        }
    }
}
