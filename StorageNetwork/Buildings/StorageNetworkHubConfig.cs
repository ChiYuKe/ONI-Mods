using System.Collections.Generic;
using StorageNetwork.Components;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public class StorageNetworkHubConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkHub";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                2,
                2,
                "storagelocker_kanim",
                30,
                30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER2,
                MATERIALS.REFINED_METALS,
                1600f,
                BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.PENALTY.TIER1,
                NOISE_POLLUTION.NONE);

            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.Entombable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.RequiresPowerInput = false;
            buildingDef.ViewMode = OverlayModes.Logic.ID;
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            go.AddOrGet<StorageNetworkHub>();
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<Operational>();
        }
    }
}
