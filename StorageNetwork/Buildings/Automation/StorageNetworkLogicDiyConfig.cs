using StorageNetwork.Components;
using System.Collections.Generic;
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
            buildingDef.LogicOutputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.OutputPort(
                    StorageNetworkLogicDiy.PORT_ID,
                    OutputPortOffset,
                    STRINGS.BUILDINGS.PREFABS.STORAGENETWORKLOGICDIY.LOGIC_PORT,
                    STRINGS.BUILDINGS.PREFABS.STORAGENETWORKLOGICDIY.LOGIC_PORT_ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.STORAGENETWORKLOGICDIY.LOGIC_PORT_INACTIVE,
                    false,
                    false)
            };

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
            go.AddOrGet<CopyBuildingSettings>();
            go.AddOrGet<LogicPorts>();
            go.AddOrGet<UserNameable>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<StorageNetworkLogicDiy>();
            go.GetComponent<KPrefabID>()?.AddTag(GameTags.OverlayBehindConduits);
        }
    }
}
