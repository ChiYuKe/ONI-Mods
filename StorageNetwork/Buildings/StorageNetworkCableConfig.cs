using StorageNetwork.Components;
using StorageNetwork.UI;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public class StorageNetworkCableConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkCable";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                1,
                1,
                "storage_network_wires_kanim",
                10,
                3f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER_TINY,
                MATERIALS.REFINED_METALS,
                1600f,
                BuildLocationRule.Anywhere,
                TUNING.BUILDINGS.DECOR.PENALTY.TIER0,
                NOISE_POLLUTION.NONE);

            buildingDef.ViewMode = StorageNetworkOverviewMode.ID;
            buildingDef.ObjectLayer = ObjectLayer.LogicWire;
            buildingDef.TileLayer = ObjectLayer.LogicWireTile;
            buildingDef.ReplacementLayer = ObjectLayer.ReplacementLogicWire;
            buildingDef.SceneLayer = Grid.SceneLayer.LogicWires;
            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.Entombable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.isKAnimTile = true;
            buildingDef.isUtility = true;
            buildingDef.DragBuild = true;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.AUTOMATION);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefabTag);
            go.AddOrGet<StorageNetworkCable>();
            KAnimGraphTileVisualizer visualizer = go.AddOrGet<KAnimGraphTileVisualizer>();
            visualizer.connectionSource = KAnimGraphTileVisualizer.ConnectionSource.Logic;
            visualizer.isPhysicalBuilding = false;
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            go.GetComponent<Constructable>().isDiggingRequired = false;
            KAnimGraphTileVisualizer visualizer = go.AddOrGet<KAnimGraphTileVisualizer>();
            visualizer.connectionSource = KAnimGraphTileVisualizer.ConnectionSource.Logic;
            visualizer.isPhysicalBuilding = false;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }
    }
}
