using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;


namespace AutomaticHarvest
{
    public class AutomaticHarvestConfig : IBuildingConfig
    {
       
        public override BuildingDef CreateBuildingDef()
        {
            string text = "AutomaticHarvestConfig";
            int num = 1;
            int num2 = 1;
            string text2 = "AutomaticHarvest_kanim";//"thermalblock_kanim";testanim_kanim
            int num3 = 30;
            float num4 = 120f;
            float[] tier = BUILDINGS.CONSTRUCTION_MASS_KG.TIER5;
            string[] any_BUILDABLE = MATERIALS.ANY_BUILDABLE;
            float num5 = 1600f;
            BuildLocationRule buildLocationRule = BuildLocationRule.NotInTiles;
            EffectorValues none = NOISE_POLLUTION.NONE;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(text, num, num2, text2, num3, num4, tier, any_BUILDABLE, num5, buildLocationRule, DECOR.NONE, none, 0.2f);
            buildingDef.Floodable = false;
            buildingDef.Entombable = false;
            buildingDef.Overheatable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.RequiresPowerInput = true;
            buildingDef.AddLogicPowerPort = false;
            buildingDef.EnergyConsumptionWhenActive = 120f;     
            buildingDef.SelfHeatKilowattsWhenActive = 1f;
            buildingDef.PowerInputOffset = new CellOffset(0, 0);

            buildingDef.LogicOutputPorts = new List<LogicPorts.Port> {
                LogicPorts.Port.OutputPort(
                    AutomaticHarvestLogic.PORT_ID,
                    new CellOffset(0, 0),
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.LOGIC_PORT,
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.LOGIC_PORT_ACTIVE,
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.LOGIC_PORT_INACTIVE,
                    false,
                    false)
            };


            buildingDef.OutputConduitType = ConduitType.Solid;
            buildingDef.UtilityOutputOffset = new CellOffset(0, 0);
            buildingDef.DefaultAnimState = "off";
            buildingDef.ObjectLayer = ObjectLayer.Building;
            buildingDef.SceneLayer = Grid.SceneLayer.SceneMAX;
            return buildingDef;
        }


        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
           AddVisualizer(go, true);
        }


        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddTag("AutomaticHarvest");
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);

            GeneratedBuildings.MakeBuildingAlwaysOperational(go);

            AddVisualizer(go, false);
            go.AddComponent<AutoPlantHarvester>();
            go.AddOrGet<AutomaticHarvestK>();
            go.AddOrGet<Reservoir>();
            go.AddOrGet<AutomaticHarvestLogic>();
            go.AddOrGet<Rotatable>();
            



            //Operational operational = go.AddOrGet<Operational>();
            //operational.SetFlag(new Operational.Flag("output_conduit", Operational.Flag.Type.Functional), true);

            Storage storage = go.AddOrGet<Storage>();
            storage.capacityKg = 20000f;

            //设置 storageFilters 允许存储收获物和种子
            storage.storageFilters = new List<Tag>
            {
                GameTags.Edible,      // 食物
                GameTags.Seed,        // 种子
            };

            // 用 SetDefaultStoredItemModifiers 方法来设置保鲜功能
            storage.SetDefaultStoredItemModifiers(new List<Storage.StoredItemModifier>
            {
                Storage.StoredItemModifier.Preserve
            });

            // 阻止复制人从该存储中拿走物品
            storage.allowItemRemoval = true;

            // 启用在建筑上方世界空间（World Space）显示的容量状态图标（Status Item）
            storage.showCapacityStatusItem = true;
            // 告诉游戏将容量状态作为该建筑的主要状态条来渲染，
            storage.showCapacityAsMainStatus = true;

        }


        public override void DoPostConfigureComplete(GameObject go)// 建造配置
        {
           
            go.AddOrGet<EnergyConsumer>();
            go.AddOrGet<SolidConduitDispenserK>();
            go.AddOrGet<Operational>();




           //  go.AddOrGet<Automatable>();
            // go.AddOrGet<TreeFilterable>();

            // go.AddOrGet<LogicOperationalController>();

        }


  



        private static void AddVisualizer(GameObject prefab, bool movable)
        {
            RangeVisualizer rangeVisualizer = prefab.AddOrGet<RangeVisualizer>();
            rangeVisualizer.OriginOffset = new Vector2I(0, 0);
            rangeVisualizer.RangeMin.x = -8;
            rangeVisualizer.RangeMin.y = -3;
            rangeVisualizer.RangeMax.x = 8;
            rangeVisualizer.RangeMax.y = 3;
            rangeVisualizer.BlockingTileVisible = true;
        }

        public const string ID = "AutomaticHarvestConfig";

    }
}