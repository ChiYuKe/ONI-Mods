using StorageNetwork.Components;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public sealed class StorageNetworkLogicDiyConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkLogicDiy";
        private const string Anim = "storagenetwork_logic_diy_kanim";
        private static readonly CellOffset OutputPortOffset = CellOffset.none;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                1,
                1,
                Anim,
                10,
                3f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER0,
                MATERIALS.REFINED_METALS,
                1600f,
                BuildLocationRule.Anywhere,
                BUILDINGS.DECOR.PENALTY.TIER0,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.ObjectLayer = ObjectLayer.LogicGate;
            buildingDef.SceneLayer = Grid.SceneLayer.LogicGates;
            buildingDef.ThermalConductivity = 0.05f;
            buildingDef.Overheatable = false;
            buildingDef.Floodable = false;
            buildingDef.Entombable = false;
            buildingDef.ViewMode = OverlayModes.Logic.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            buildingDef.DragBuild = true;

            GeneratedBuildings.RegisterWithOverlay(OverlayModes.Logic.HighlightItemIDs, ID);
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.AUTOMATION);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefabTag);
            go.AddOrGet<CodexEntryRedirector>().CodexID = ID;
            go.AddOrGet<StorageNetworkSceneMember>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            StorageNetworkLogicDiy logic = go.AddOrGet<StorageNetworkLogicDiy>();
            logic.OutputPortOffset = OutputPortOffset;
            go.GetComponent<KPrefabID>()?.AddTag(GameTags.OverlayBehindConduits);
        }
    }
}
