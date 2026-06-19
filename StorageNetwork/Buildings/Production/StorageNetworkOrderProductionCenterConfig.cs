using StorageNetwork.Components;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public sealed class StorageNetworkOrderProductionCenterConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkOrderProductionCenter";
        private const string Anim = "StorageNetwork_OrderProductionCenter_kanim";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                3,
                3,
                Anim,
                100,
                60f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                MATERIALS.REFINED_METALS,
                1600f,
                BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.PENALTY.TIER1,
                NOISE_POLLUTION.NOISY.TIER1,
                0.2f);

            buildingDef.AudioCategory = "Metal";
            buildingDef.Overheatable = false;
            buildingDef.RequiresPowerInput = false;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.AUTOMATION);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            go.AddOrGet<CodexEntryRedirector>().CodexID = ID;
            go.AddOrGet<StorageNetworkSceneMember>();
            StorageNetworkEnrollment enrollment = go.AddOrGet<StorageNetworkEnrollment>();
            enrollment.IncludedInSceneNetwork = true;
            ComplexFabricator fabricator = go.AddOrGet<StorageNetworkOrderProductionCenterFabricator>();
            fabricator.duplicantOperated = false;
            fabricator.showProgressBar = false;
            fabricator.sideScreenStyle = ComplexFabricatorSideScreen.StyleSetting.ListQueueHybrid;
            BuildingTemplates.CreateComplexFabricatorStorage(go, fabricator);
            go.AddOrGet<StorageNetworkMaterialRequester>();
            Storage diskInstallStorage = go.AddComponent<Storage>();
            diskInstallStorage.capacityKg = 3f;
            diskInstallStorage.showInUI = false;
            diskInstallStorage.allowItemRemoval = false;
            diskInstallStorage.allowUIItemRemoval = false;
            diskInstallStorage.fetchCategory = Storage.FetchCategory.Building;
            diskInstallStorage.storageFilters = new List<Tag> { StorageNetworkEngravingDiskConfig.ID };
            diskInstallStorage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            go.AddOrGet<StorageNetworkOrderProductionCenter>();
            go.AddOrGet<StorageNetworkOrderProductionCenterController>();
            go.AddOrGet<StorageNetworkOrderProductionCenterDiskInstallWorkable>();
            go.AddOrGet<CopyBuildingSettings>();
            go.AddOrGet<UserNameable>();
            Prioritizable.AddRef(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            SymbolOverrideControllerUtil.AddToPrefab(go);
            go.AddOrGet<ComplexFabricatorSM>();
        }
    }
}
