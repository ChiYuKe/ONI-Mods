using StorageNetwork.Components;
using StorageNetwork.UI;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public class StorageNetworkCableBridgeConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkCableBridge";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                3,
                1,
                "storage_network_bridge_kanim",
                30,
                3f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER_TINY,
                MATERIALS.REFINED_METALS,
                1600f,
                BuildLocationRule.LogicBridge,
                BUILDINGS.DECOR.PENALTY.TIER0,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.ViewMode = StorageNetworkOverviewMode.ID;
            buildingDef.ObjectLayer = ObjectLayer.LogicGate;
            buildingDef.SceneLayer = Grid.SceneLayer.LogicGates;
            buildingDef.Overheatable = false;
            buildingDef.Floodable = false;
            buildingDef.Entombable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            buildingDef.UtilityInputOffset = new CellOffset(0, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(2, 0);
            buildingDef.AlwaysOperational = true;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefabTag);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            AddBridge(go).VisualizeOnly = true;
            go.AddOrGet<BuildingCellVisualizer>();
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            AddBridge(go).VisualizeOnly = true;
            go.AddOrGet<BuildingCellVisualizer>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            AddBridge(go).VisualizeOnly = false;
            go.AddOrGet<BuildingCellVisualizer>();
        }

        private static StorageNetworkCableBridge AddBridge(GameObject go)
        {
            StorageNetworkCableBridge bridge = go.AddOrGet<StorageNetworkCableBridge>();
            return bridge;
        }
    }
}
