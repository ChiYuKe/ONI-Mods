using System.Collections.Generic;
using StorageNetwork.Components;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public sealed class StorageNetworkEnergySensorConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkEnergySensor";
        private const string AnimFile = "batterysmart_kanim";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                2,
                2,
                AnimFile,
                30,
                30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER2,
                MATERIALS.REFINED_METALS,
                1600f,
                BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.NONE,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.ObjectLayer = ObjectLayer.Building;
            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.CanMove = false;
            buildingDef.UseStructureTemperature = false;
            buildingDef.ViewMode = OverlayModes.Logic.ID;
            buildingDef.DefaultAnimState = "off";
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
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            go.AddOrGet<CopyBuildingSettings>();
            go.AddOrGet<LogicPorts>();
            go.AddOrGet<StorageNetworkEnergySensor>();
            go.AddOrGet<UserNameable>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }
    }
}
