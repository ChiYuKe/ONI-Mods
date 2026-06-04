using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public abstract class StorageNetworkStorageBuildingBase : IBuildingConfig
    {
        protected abstract StorageNetworkStorageBuildingSpec Spec { get; }
        protected virtual bool ProvidesStorage => true;

        public override BuildingDef CreateBuildingDef()
        {
            StorageNetworkStorageBuildingSpec spec = Spec;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                spec.Id,
                spec.Width,
                spec.Height,
                spec.AnimFile,
                30,
                spec.ConstructionTime,
                spec.ConstructionMass,
                spec.ConstructionMaterials,
                spec.MeltingPoint,
                BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.PENALTY.TIER1,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.Floodable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.Overheatable = false;
            buildingDef.RequiresPowerInput = spec.PowerWatts > 0f;
            buildingDef.EnergyConsumptionWhenActive = spec.PowerWatts;
            buildingDef.SelfHeatKilowattsWhenActive = spec.SelfHeatKilowatts;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            StorageNetworkStorageBuildingSpec spec = Spec;
            SoundEventVolumeCache.instance.AddVolume(spec.AnimFile, "StorageLocker_Hit_metallic_low", NOISE_POLLUTION.NOISY.TIER1);
            Prioritizable.AddRef(go);

            KPrefabID prefabId = go.GetComponent<KPrefabID>();
            if (ProvidesStorage)
            {
                prefabId?.AddTag(StorageSceneTags.ModStorage);
                prefabId?.AddTag(StorageSceneTags.ShowSettingsButton);

                Storage storage = go.AddOrGet<Storage>();
                storage.capacityKg = spec.CapacityKg;
                storage.showInUI = true;
                storage.allowItemRemoval = true;
                storage.showDescriptor = true;
                storage.storageFilters = spec.Filters;
                storage.storageFullMargin = STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
                storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
                storage.showCapacityStatusItem = true;
                storage.showCapacityAsMainStatus = true;
                storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);

                go.AddOrGet<StorageNetworkStorageConnector>();
                go.AddOrGet<StorageLocker>();
                go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.StorageLocker;
            }
            else
            {
                go.AddOrGet<StorageNetworkCore>();
            }

            go.AddOrGet<UserNameable>();
            go.AddOrGetDef<RocketUsageRestriction.Def>();
            if (spec.PowerWatts > 0f)
            {
                go.AddOrGet<EnergyConsumer>();
            }
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            if (ProvidesStorage)
            {
                go.AddOrGetDef<StorageController.Def>();
            }

            if (Spec.PowerWatts > 0f)
            {
                go.AddOrGetDef<PoweredController.Def>();
            }
        }
    }

    public sealed class StorageNetworkCoreConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkCore";
        protected override bool ProvidesStorage => false;
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.Core;
    }

    public sealed class SmallSolidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallSolidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallSolid;
    }

    public sealed class SmallLiquidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallLiquidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallLiquid;
    }

    public sealed class SmallGasServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallGasServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallGas;
    }

    public sealed class MediumSolidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumSolidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumSolid;
    }

    public sealed class MediumLiquidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumLiquidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumLiquid;
    }

    public sealed class MediumGasServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumGasServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumGas;
    }

    public sealed class LargeSolidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeSolidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeSolid;
    }

    public sealed class LargeLiquidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeLiquidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeLiquid;
    }

    public sealed class LargeGasServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeGasServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeGas;
    }

    public sealed class StorageNetworkRelayModuleConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkRelayModule";

        public override string[] GetRequiredDlcIds()
        {
            return DlcManager.EXPANSION1;
        }

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                3,
                3,
                "rocket_scanner_module_kanim",
                1000,
                120f,
                BUILDINGS.ROCKETRY_MASS_KG.HOLLOW_TIER2,
                MATERIALS.REFINED_METALS,
                9999f,
                BuildLocationRule.Anywhere,
                BUILDINGS.DECOR.NONE,
                NOISE_POLLUTION.NOISY.TIER2,
                0.2f);

            BuildingTemplates.CreateRocketBuildingDef(buildingDef);
            buildingDef.SceneLayer = Grid.SceneLayer.Building;
            buildingDef.ForegroundLayer = Grid.SceneLayer.Front;
            buildingDef.OverheatTemperature = 2273.15f;
            buildingDef.Floodable = false;
            buildingDef.AttachmentSlotTag = GameTags.Rocket;
            buildingDef.ObjectLayer = ObjectLayer.Building;
            buildingDef.RequiresPowerInput = false;
            buildingDef.attachablePosition = new CellOffset(0, 0);
            buildingDef.CanMove = true;
            buildingDef.Cancellable = false;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.ROCKET);
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefabTag);
            go.AddOrGet<LoopingSounds>();
            go.AddOrGet<StorageNetworkRelayModule>();
            go.GetComponent<KPrefabID>()?.AddTag(GameTags.LaunchButtonRocketModule);
            go.GetComponent<KPrefabID>()?.AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            go.AddOrGet<BuildingAttachPoint>().points = new[]
            {
                new BuildingAttachPoint.HardPoint(new CellOffset(0, 3), GameTags.Rocket, null)
            };
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            Prioritizable.AddRef(go);
            go.AddOrGet<LaunchableRocketCluster>();
            go.AddOrGet<StorageNetworkRelayCommandConditions>();
            go.AddOrGet<RocketProcessConditionDisplayTarget>();
            go.AddOrGet<RocketLaunchConditionVisualizer>();
            BuildingTemplates.ExtendBuildingToRocketModuleCluster(go, null, ROCKETRY.BURDEN.MINOR_PLUS, 0f, 0f);
        }
    }

    public sealed class StorageNetworkStorageBuildingSpec
    {
        public string Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string AnimFile { get; set; }
        public float ConstructionTime { get; set; }
        public float[] ConstructionMass { get; set; }
        public string[] ConstructionMaterials { get; set; }
        public float MeltingPoint { get; set; }
        public float CapacityKg { get; set; }
        public float PowerWatts { get; set; }
        public float SelfHeatKilowatts { get; set; }
        public List<Tag> Filters { get; set; }
    }

    public static class StorageNetworkStorageBuildingSpecs
    {
        private const string CoreAnim = "storagenetwork_core_kanim";
        private const string SmallSolidServerAnim = "storagenetwork_small_solid_server_kanim";
        private const string SmallLiquidServerAnim = "storagenetwork_small_liquid_server_kanim";
        private const string SmallGasServerAnim = "storagenetwork_small_gas_server_kanim";
        private const string MediumSolidServerAnim = "storagenetwork_medium_solid_server_kanim";
        private const string MediumLiquidServerAnim = "storagenetwork_medium_liquid_server_kanim";
        private const string MediumGasServerAnim = "storagenetwork_medium_gas_server_kanim";
        private const string LargeSolidServerAnim = "storagenetwork_large_solid_server_kanim";
        private const string LargeLiquidServerAnim = "storagenetwork_large_liquid_server_kanim";
        private const string LargeGasServerAnim = "storagenetwork_large_gas_server_kanim";
        private const float MeltingPoint = 1600f;

        public static readonly StorageNetworkStorageBuildingSpec Core = Create(
            StorageNetworkCoreConfig.ID,
            3,
            4,
            CoreAnim,
            500000f,
            240f,
            2f,
            STORAGEFILTERS.STORAGE_LOCKERS_STANDARD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static readonly StorageNetworkStorageBuildingSpec SmallSolid = CreateServer(
            SmallSolidServerConfig.ID,
            1,
            2,
            SmallSolidServerAnim,
            25000f,
            60f,
            STORAGEFILTERS.STORAGE_LOCKERS_STANDARD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3);

        public static readonly StorageNetworkStorageBuildingSpec SmallLiquid = CreateServer(
            SmallLiquidServerConfig.ID,
            1,
            2,
            SmallLiquidServerAnim,
            25000f,
            60f,
            STORAGEFILTERS.LIQUIDS,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3);

        public static readonly StorageNetworkStorageBuildingSpec SmallGas = CreateServer(
            SmallGasServerConfig.ID,
            1,
            2,
            SmallGasServerAnim,
            25000f,
            60f,
            STORAGEFILTERS.GASES,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3);

        public static readonly StorageNetworkStorageBuildingSpec MediumSolid = CreateServer(
            MediumSolidServerConfig.ID,
            2,
            2,
            MediumSolidServerAnim,
            100000f,
            120f,
            STORAGEFILTERS.STORAGE_LOCKERS_STANDARD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER4);

        public static readonly StorageNetworkStorageBuildingSpec MediumLiquid = CreateServer(
            MediumLiquidServerConfig.ID,
            2,
            2,
            MediumLiquidServerAnim,
            100000f,
            120f,
            STORAGEFILTERS.LIQUIDS,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER4);

        public static readonly StorageNetworkStorageBuildingSpec MediumGas = CreateServer(
            MediumGasServerConfig.ID,
            2,
            2,
            MediumGasServerAnim,
            100000f,
            120f,
            STORAGEFILTERS.GASES,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER4);

        public static readonly StorageNetworkStorageBuildingSpec LargeSolid = CreateServer(
            LargeSolidServerConfig.ID,
            2,
            3,
            LargeSolidServerAnim,
            250000f,
            240f,
            STORAGEFILTERS.STORAGE_LOCKERS_STANDARD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static readonly StorageNetworkStorageBuildingSpec LargeLiquid = CreateServer(
            LargeLiquidServerConfig.ID,
            2,
            3,
            LargeLiquidServerAnim,
            250000f,
            240f,
            STORAGEFILTERS.LIQUIDS,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static readonly StorageNetworkStorageBuildingSpec LargeGas = CreateServer(
            LargeGasServerConfig.ID,
            2,
            4,
            LargeGasServerAnim,
            250000f,
            240f,
            STORAGEFILTERS.GASES,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static IEnumerable<string> AllIds
        {
            get
            {
                yield return StorageNetworkCoreConfig.ID;
                yield return SmallSolidServerConfig.ID;
                yield return SmallLiquidServerConfig.ID;
                yield return SmallGasServerConfig.ID;
                yield return MediumSolidServerConfig.ID;
                yield return MediumLiquidServerConfig.ID;
                yield return MediumGasServerConfig.ID;
                yield return LargeSolidServerConfig.ID;
                yield return LargeLiquidServerConfig.ID;
                yield return LargeGasServerConfig.ID;
            }
        }

        public static IEnumerable<string> RocketModuleIds
        {
            get
            {
                yield return StorageNetworkRelayModuleConfig.ID;
            }
        }

        public static IEnumerable<string> UnlockIds
        {
            get
            {
                foreach (string buildingId in AllIds)
                {
                    yield return buildingId;
                }

                foreach (string buildingId in RocketModuleIds)
                {
                    yield return buildingId;
                }
            }
        }

        private static StorageNetworkStorageBuildingSpec CreateServer(
            string id,
            int width,
            int height,
            string animFile,
            float capacityKg,
            float powerWatts,
            List<Tag> filters,
            float[] constructionMass)
        {
            return Create(id, width, height, animFile, capacityKg, powerWatts, 1f, filters, constructionMass);
        }

        private static StorageNetworkStorageBuildingSpec Create(
            string id,
            int width,
            int height,
            string animFile,
            float capacityKg,
            float powerWatts,
            float selfHeatKilowatts,
            List<Tag> filters,
            float[] constructionMass)
        {
            return new StorageNetworkStorageBuildingSpec
            {
                Id = id,
                Width = width,
                Height = height,
                AnimFile = animFile,
                ConstructionTime = 30f,
                ConstructionMass = constructionMass,
                ConstructionMaterials = MATERIALS.REFINED_METALS,
                MeltingPoint = MeltingPoint,
                CapacityKg = capacityKg,
                PowerWatts = powerWatts,
                SelfHeatKilowatts = selfHeatKilowatts,
                Filters = filters
            };
        }
    }
}
