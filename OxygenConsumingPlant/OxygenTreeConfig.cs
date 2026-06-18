using KModTool;
using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace OxygenConsumingPlant
{

    public class OxygenTreeConfig : KMonoBehaviour,IEntityConfig
    {

        public GameObject CreatePrefab()
        {
            string text = "KModOxygenTree";
            string text2 = STRINGS.CREATURES.SPECIES.KMODOXYGENTREE.NAME;
            string text3 = STRINGS.CREATURES.SPECIES.KMODOXYGENTREE.DESC;
            float num = 1f;
            EffectorValues tier = DECOR.BONUS.TIER1;
            GameObject gameObject = EntityTemplates.CreatePlacedEntity(text, text2, text3, num, Assets.GetAnim("KModOxygenTree_kanim"), "idle_empty", Grid.SceneLayer.BuildingFront, 1, 2, tier, default(EffectorValues), SimHashes.Creature, null, 293f);
            EntityTemplates.ExtendEntityToBasicPlant(gameObject, 179.15f, 189.15f, 373.15f, 383.15f, new SimHashes[]
            {
                SimHashes.Oxygen,
                SimHashes.ContaminatedOxygen,
                SimHashes.CarbonDioxide
            }, true, 0f, 0.15f, OxygenTreeFruit_GConfig.ID, true, true, true, false, true, 2400f, 0f, 1000f, "KModOxygenTreeOriginal", STRINGS.CREATURES.SPECIES.KMODOXYGENTREE.NAME);
            EntityTemplates.ExtendPlantToIrrigated(gameObject, new PlantElementAbsorber.ConsumeInfo[]
            {
                new PlantElementAbsorber.ConsumeInfo
                {
                    tag = GameTags.Water,
                    massConsumptionRate = 0.033333335f
                }
            });
            gameObject.AddOrGet<OxygenConsumingPlant>();
            gameObject.AddOrGet<KPrefabID>().AddTag(new Tag("KModOxygenPlantTag"), false);
            GameObject gameObject2 = EntityTemplates.CreateAndRegisterSeedForPlant(gameObject, SeedProducer.ProductionType.Harvest, "KModOxygenTreeSeed", STRINGS.CREATURES.SPECIES.SEEDS.KMODOXYGENTREESEED.NAME, STRINGS.CREATURES.SPECIES.SEEDS.KMODOXYGENTREESEED.DESC, Assets.GetAnim("KModOxygenTreeSeed_kanim"), "object", 0, new List<Tag> { GameTags.CropSeed }, SingleEntityReceptacle.ReceptacleDirection.Top, default(Tag), 2, STRINGS.CREATURES.SPECIES.KMODOXYGENTREE.DOMESTICATEDDESC, EntityTemplates.CollisionShape.CIRCLE, 0.25f, 0.25f, null, "", false, null);
            EntityTemplates.CreateAndRegisterPreviewForPlant(gameObject2, "PrickleFlower_preview", Assets.GetAnim("KModOxygenTree_kanim"), "place", 1, 2);
            SoundEventVolumeCache.instance.AddVolume("bristleblossom_kanim", "PrickleFlower_harvest", NOISE_POLLUTION.CREATURES.TIER3);
            SoundEventVolumeCache.instance.AddVolume("bristleblossom_kanim", "PrickleFlower_grow", NOISE_POLLUTION.CREATURES.TIER3);
            return gameObject;
        }

        public string[] GetDlcIds()
        {
            return DlcManager.AVAILABLE_ALL_VERSIONS;
        }

        public void OnPrefabInit(GameObject inst)
        {
            inst.GetComponent<PrimaryElement>().Temperature = 288.15f;
            Console.WriteLine("植物已经初始化");
        }

        public void OnSpawn(GameObject inst)
        {

            RangeVisualizer rangeVisualizer = inst.AddOrGet<RangeVisualizer>();
            rangeVisualizer.OriginOffset = new Vector2I(0, 0);
            rangeVisualizer.RangeMin.x = -4;
            rangeVisualizer.RangeMin.y = -4;
            rangeVisualizer.RangeMax.x = 4;
            rangeVisualizer.RangeMax.y = 4;
            rangeVisualizer.BlockingTileVisible = true;


            Ownable ownable = inst.AddOrGet<Ownable>();
            ownable.slotID = Db.Get().AssignableSlots.Toilet.Id;
            ownable.canBePublic = true;


            float num;
            float num2;
            KModTransformUtils.TryGetPosition(inst, out num, out num2);
            Debug.Log(string.Format("Target Object X: {0}, Y: {1}", num, num2));


            List<GameObject> allMinionGameObjects = KModMinionUtils.GetAllMinionGameObjects();

            foreach (GameObject gameObject in allMinionGameObjects)
            {
                //KModMinionUtils.PrintMinionIdentityInfo(gameObject);
                //KModMinionUtils.PrintMinionResumeInfo(gameObject);
                //KModMinionUtils.PrintHealthInfo(gameObject);
                //KModMinionUtils.PrintNavigatorInfo(gameObject);
                //KModMinionUtils.PrintChoreDriverInfo(gameObject);
                //KModMinionUtils.PrintAllComponents(gameObject);

                
            }


           
        }


        public const string ID = "KModOxygenTree";

        public const string SEED_ID = "KModOxygenTreeSeed";

        public const float WATER_RATE = 0.033333335f;
    }
}
