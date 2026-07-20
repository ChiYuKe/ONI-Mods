using LogicNetwork.Components;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace LogicNetwork.Buildings
{
    public sealed class LogicNetworkEmitterConfig : IBuildingConfig
    {
        public const string ID = "LogicNetworkEmitter";
        private const string Anim = "logicnetwork_emitter_out_kanim";
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
                    LogicNetworkEmitter.PORT_ID,
                    OutputPortOffset,
                    global::LogicNetwork.LogicNetworkStrings.LogicPort,
                    global::LogicNetwork.LogicNetworkStrings.LogicPortActive,
                    global::LogicNetwork.LogicNetworkStrings.LogicPortInactive,
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
            go.AddOrGet<CopyBuildingSettings>();
            go.AddOrGet<LogicPorts>();
            go.AddOrGet<UserNameable>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicNetworkEmitter>();
            go.GetComponent<KPrefabID>()?.AddTag(GameTags.OverlayBehindConduits);
        }
    }
}
