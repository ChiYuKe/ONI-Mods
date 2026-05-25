using TUNING;
using UnityEngine;
using StorageNetwork.Components;
using StorageNetwork.Core;

namespace StorageNetwork.Buildings
{
    public class SceneStorageBoxConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkSceneStorageBox";
        private const string AnimFile = "storagelocker_kanim";
        private const float CapacityKg = 500000f;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                1,
                2,
                AnimFile,
                30,
                10f,
                TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                MATERIALS.RAW_MINERALS_OR_METALS,
                1600f,
                BuildLocationRule.OnFloor,
                TUNING.BUILDINGS.DECOR.PENALTY.TIER1,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.Floodable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.Overheatable = false;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            SoundEventVolumeCache.instance.AddVolume(AnimFile, "StorageLocker_Hit_metallic_low", NOISE_POLLUTION.NOISY.TIER1);
            Prioritizable.AddRef(go);

            KPrefabID prefabId = go.GetComponent<KPrefabID>();
            prefabId?.AddTag(StorageSceneTags.SceneStorageBox);

            Storage storage = go.AddOrGet<Storage>();
            storage.capacityKg = CapacityKg;
            storage.showInUI = true;
            storage.allowItemRemoval = true;
            storage.showDescriptor = true;
            storage.storageFilters = STORAGEFILTERS.STORAGE_LOCKERS_STANDARD;
            storage.storageFullMargin = STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
            storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
            storage.showCapacityStatusItem = true;
            storage.showCapacityAsMainStatus = true;

            go.AddOrGet<SceneStorageBoxMarker>();
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.StorageLocker;
            go.AddOrGet<StorageLocker>();
            go.AddOrGet<UserNameable>();
            go.AddOrGetDef<RocketUsageRestriction.Def>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGetDef<StorageController.Def>();
        }
    }
}
