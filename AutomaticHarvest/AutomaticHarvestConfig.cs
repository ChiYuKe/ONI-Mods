using System.Collections.Generic;
using TemplateClasses;
using TUNING;
using UnityEngine;

namespace AutomaticHarvest
{
    public class AutomaticHarvestConfig : IBuildingConfig
    {
        public const string ID = "AutomaticHarvestConfig";

        private const string AnimFile = "AutomaticHarvest_kanim";
        private const int Width = 1;
        private const int Height = 1;
        private const int HitPoints = 30;
        private const float ConstructionTime = 120f;
        private const float MeltingPoint = 1600f;
        private const float EnergyConsumptionWatts = 120f;
        private const float SelfHeatKilowatts = 1f;
        private const float StorageCapacityKg = 20000f;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                Width,
                Height,
                AnimFile,
                HitPoints,
                ConstructionTime,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER5,
                MATERIALS.ANY_BUILDABLE,
                MeltingPoint,
                BuildLocationRule.NotInTiles,
                DECOR.NONE,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.Floodable = false;
            buildingDef.Entombable = false;
            buildingDef.Overheatable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.RequiresPowerInput = true;
            buildingDef.AddLogicPowerPort = false;
            buildingDef.EnergyConsumptionWhenActive = EnergyConsumptionWatts;
            buildingDef.SelfHeatKilowattsWhenActive = SelfHeatKilowatts;
            buildingDef.PowerInputOffset = new CellOffset(0, 0);

            buildingDef.LogicOutputPorts = new List<LogicPorts.Port>
            {
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
            AddVisualizer(go);
          
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            go.AddTag(AutomaticHarvestTags.Building);
            go.AddTag(new Tag("StorageNetwork_ModStorage"));
            go.AddTag(new Tag("StorageNetwork_ShowSettingsButton"));
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefabTag);

            GeneratedBuildings.MakeBuildingAlwaysOperational(go);

            AddVisualizer(go);
            go.AddComponent<AutoPlantHarvester>();
            go.AddOrGet<AutomaticHarvestK>();
            go.AddOrGet<Reservoir>();
            go.AddOrGet<AutomaticHarvestLogic>();
            go.AddOrGet<CopyBuildingSettings>();
            go.AddOrGet<Rotatable>();

            Storage storage = go.AddOrGet<Storage>();
            storage.capacityKg = StorageCapacityKg;
            storage.storageFilters = new List<Tag>
            {
                GameTags.Edible,
                GameTags.Seed,
            };

            storage.SetDefaultStoredItemModifiers(new List<Storage.StoredItemModifier>
            {
                Storage.StoredItemModifier.Preserve
            });

            storage.allowItemRemoval = true;
            storage.showCapacityStatusItem = true;
            storage.showCapacityAsMainStatus = true;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<EnergyConsumer>();
            go.AddOrGet<SolidConduitDispenserK>();
            go.AddOrGet<Operational>();
        }

        private static void AddVisualizer(GameObject prefab)
        {
            RangeVisualizer rangeVisualizer = prefab.AddOrGet<RangeVisualizer>();
            rangeVisualizer.OriginOffset = new Vector2I(0, 0);
            rangeVisualizer.RangeMin.x = -8;
            rangeVisualizer.RangeMin.y = -3;
            rangeVisualizer.RangeMax.x = 8;
            rangeVisualizer.RangeMax.y = 3;
            rangeVisualizer.BlockingTileVisible = true;
        }
    }
}
