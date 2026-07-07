using System.Collections.Generic;
using StorageNetwork.Components;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public sealed class StorageNetworkEnergySensorConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkEnergySensor";
        private const string AnimFile = "storagenetwork_small_battery_server_kanim";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                1,
                1,
                AnimFile,
                30,
                30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER2,
                MATERIALS.REFINED_METALS,
                1600f,
                BuildLocationRule.Anywhere,
                BUILDINGS.DECOR.PENALTY.TIER0,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.ObjectLayer = ObjectLayer.LogicGate;
            buildingDef.SceneLayer = Grid.SceneLayer.LogicGates;
            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.Entombable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            buildingDef.CanMove = false;
            buildingDef.UseStructureTemperature = false;
            buildingDef.ViewMode = OverlayModes.Logic.ID;
            buildingDef.DefaultAnimState = "off";
            buildingDef.BaseTimeUntilRepair = -1f;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            buildingDef.DragBuild = true;
            buildingDef.LogicOutputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.OutputPort(
                    StorageNetworkEnergySensor.PORT_ID,
                    new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.STORAGENETWORKENERGYSENSOR.LOGIC_PORT,
                    STRINGS.BUILDINGS.PREFABS.STORAGENETWORKENERGYSENSOR.LOGIC_PORT_ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.STORAGENETWORKENERGYSENSOR.LOGIC_PORT_INACTIVE,
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
            go.AddOrGet<StorageNetworkEnergySensor>();
            go.AddOrGet<UserNameable>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<KPrefabID>()?.AddTag(GameTags.OverlayBehindConduits);
        }
    }
}
