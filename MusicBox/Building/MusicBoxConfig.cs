using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace MusicBox.Building
{
    public sealed class MusicBoxConfig : IBuildingConfig
    {
        public const string ID = "MusicBox";
        private const string Anim = "MusicBox_logic_kanim";
        public static readonly HashedString PORT_ID = "MusicBoxInput";
        private static readonly CellOffset InputPortOffset = CellOffset.none;

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
                BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.PENALTY.TIER0,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.ObjectLayer = ObjectLayer.Building;
            buildingDef.ThermalConductivity = 0.05f;
            buildingDef.Overheatable = false;
            buildingDef.Floodable = false;
            buildingDef.Entombable = false;
            buildingDef.ViewMode = OverlayModes.Logic.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.DragBuild = true;

            buildingDef.LogicInputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.RibbonInputPort(
                    PORT_ID,
                    InputPortOffset,
                    STRINGS.BUILDINGS.PREFABS.MUSICBOX.LOGIC_PORT,
                    STRINGS.BUILDINGS.PREFABS.MUSICBOX.LOGIC_PORT_ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.MUSICBOX.LOGIC_PORT_INACTIVE,
                    false,
                    false)
            };

            GeneratedBuildings.RegisterWithOverlay(OverlayModes.Logic.HighlightItemIDs, ID);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            go.AddOrGet<LogicPorts>();
            go.AddOrGet<CopyBuildingSettings>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<MusicBoxComponent>();
            go.GetComponent<KPrefabID>()?.AddTag(GameTags.OverlayBehindConduits);
        }
    }
}
