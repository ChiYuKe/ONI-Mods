using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public abstract class StorageNetworkStorageBuildingBase : IBuildingConfig
    {
        protected abstract StorageNetworkStorageBuildingSpec Spec { get; }
        protected virtual bool ProvidesStorage => true;
        protected virtual bool OnePerWorld => false;
        protected virtual bool AllowManualRemoval => false;
        protected virtual Storage.FetchCategory FetchCategory => Storage.FetchCategory.Building;
        protected virtual bool SupportsFilterUi => true;
        protected virtual bool SupportsStorageConnector => false;
        protected virtual bool ShowStorageSettingsButton => true;
        protected virtual bool UsesRefrigeratedStorage => false;
        protected virtual bool StoresPower => false;
        protected virtual bool StoresParticles => false;
        protected virtual Tag? StorageStateCategoryTag => null;

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

            buildingDef.Floodable = StoresPower;
            buildingDef.AudioCategory = "Metal";
            buildingDef.Overheatable = false;
            buildingDef.RequiresPowerInput = spec.PowerWatts > 0f && !StoresPower;
            buildingDef.EnergyConsumptionWhenActive = StoresPower ? 0f : spec.PowerWatts;
            buildingDef.SelfHeatKilowattsWhenActive = spec.SelfHeatKilowatts;
            buildingDef.ExhaustKilowattsWhenActive = UsesRefrigeratedStorage ? 0f : buildingDef.ExhaustKilowattsWhenActive;
            buildingDef.RequiresPowerOutput = false;
            buildingDef.UseWhitePowerOutputConnectorColour = false;

            buildingDef.OnePerWorld = OnePerWorld;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);
            if (UsesRefrigeratedStorage)
            {
                buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.FRIDGE);
            }
            if (StoresPower)
            {
                buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.POWER);
            }

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            StorageNetworkStorageBuildingSpec spec = Spec;
            go.AddOrGet<CodexEntryRedirector>().CodexID = spec.Id;
            go.AddOrGet<StorageNetworkSceneMember>();
            SoundEventVolumeCache.instance.AddVolume(spec.AnimFile, "StorageLocker_Hit_metallic_low", NOISE_POLLUTION.NOISY.TIER1);
            Prioritizable.AddRef(go);

            KPrefabID prefabId = go.GetComponent<KPrefabID>();
            if (ProvidesStorage)
            {
                prefabId?.AddTag(StorageSceneTags.ModStorage);
                prefabId?.AddTag(StorageSceneTags.ServerStorage);
                prefabId?.AddTag(StorageSceneTags.CategoryModStorage);
                Tag? stateCategoryTag = StorageStateCategoryTag;
                if (stateCategoryTag.HasValue)
                {
                    prefabId?.AddTag(stateCategoryTag.Value);
                }

                if (ShowStorageSettingsButton)
                {
                    prefabId?.AddTag(StorageSceneTags.ShowSettingsButton);
                }

                Storage storage = go.AddOrGet<Storage>();
                storage.capacityKg = spec.CapacityKg * Config.Instance.ServerCapacityMultiplier;
                storage.showInUI = !StoresPower && !StoresParticles;
                storage.allowItemRemoval = AllowManualRemoval;
                storage.showDescriptor = !StoresPower && !StoresParticles;
                storage.storageFilters = spec.Filters;
                storage.storageFullMargin = STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
                storage.fetchCategory = FetchCategory;
                storage.showCapacityStatusItem = !StoresPower && !StoresParticles;
                storage.showCapacityAsMainStatus = !StoresPower && !StoresParticles && !UsesRefrigeratedStorage;
                storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);

                if (UsesRefrigeratedStorage)
                {
                    prefabId?.AddTag(RoomConstraints.ConstraintTags.KitchenRefrigerator);
                    ConfigureRefrigeratedStorage(go);
                }

                if (StoresPower)
                {
                    prefabId?.AddTag(RoomConstraints.ConstraintTags.PowerBuilding);
                    ConfigurePowerStorage(go, spec);
                }

                if (StoresParticles)
                {
                    ConfigureParticleStorage(go, spec);
                }

                if (SupportsStorageConnector)
                {
                    go.AddOrGet<StorageNetworkStorageConnector>();
                }

                go.AddOrGet<StorageNetworkServerStatus>();

                if (SupportsFilterUi)
                {
                    StorageNetworkFilterConfigurator.Configure(go.AddOrGet<TreeFilterable>());
                    go.AddOrGet<StorageNetworkDefaultFilterInitializer>();
                    CopyBuildingSettings copySettings = go.AddOrGet<CopyBuildingSettings>();
                    copySettings.copyGroupTag = StorageStateCategoryTag ?? prefabTag;
                    go.AddOrGet<StorageNetworkServerCopySettings>();
                }
            }
            else
            {
                prefabId?.AddTag(GameTags.UniquePerWorld);
                go.AddOrGet<StorageNetworkCore>();
            }

            go.AddOrGet<UserNameable>();
            go.AddOrGetDef<RocketUsageRestriction.Def>();
            if (spec.SelfHeatKilowatts > 0f && !UsesRefrigeratedStorage)
            {
                go.AddOrGet<StorageNetworkServerHeat>();
            }

            if (spec.PowerWatts > 0f && !StoresPower)
            {
                go.AddOrGet<Operational>();
                go.AddOrGet<EnergyConsumer>();
            }
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            if (ProvidesStorage)
            {
                go.AddOrGetDef<StorageController.Def>();
            }

            if (Spec.PowerWatts > 0f && !StoresPower)
            {
                go.AddOrGetDef<PoweredController.Def>();
            }

            if (UsesRefrigeratedStorage)
            {
                go.AddOrGetDef<StorageNetworkColdStorageController.Def>();
            }

        }

        private static void ConfigureRefrigeratedStorage(GameObject go)
        {
            Storage storage = go.AddOrGet<Storage>();
            storage.SetDefaultStoredItemModifiers(StorageNetworkColdStorageCooling.StoredItemModifiers);
            go.AddOrGet<StorageNetworkColdStorageCooling>();
        }

        private static void ConfigurePowerStorage(GameObject go, StorageNetworkStorageBuildingSpec spec)
        {
            StorageNetworkPowerStorage powerStorage = go.AddOrGet<StorageNetworkPowerStorage>();
            powerStorage.capacityJoules = spec.CapacityKg * Config.Instance.ServerCapacityMultiplier;
            powerStorage.joulesLostPerSecond = spec.PowerStorageJoulesLostPerSecond;
            go.AddOrGet<StorageNetworkPowerOverlayBattery>();
        }

        private static void ConfigureParticleStorage(GameObject go, StorageNetworkStorageBuildingSpec spec)
        {
            HighEnergyParticleStorage particleStorage = go.AddOrGet<HighEnergyParticleStorage>();
            particleStorage.capacity = spec.CapacityKg * Config.Instance.ServerCapacityMultiplier;
            particleStorage.showInUI = true;
            particleStorage.showCapacityStatusItem = true;
            particleStorage.showCapacityAsMainStatus = true;
            particleStorage.autoStore = false;
            go.AddOrGet<StorageNetworkParticleServer>();
        }
    }

    public sealed class StorageNetworkCoreConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkCore";
        protected override bool ProvidesStorage => false;
        protected override bool OnePerWorld => true;
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.Core;
    }

    public sealed class SmallSolidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallSolidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallSolid;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategorySolidPort;
    }

    public sealed class SmallLiquidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallLiquidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallLiquid;
        protected override bool SupportsFilterUi => false;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryLiquidPort;
    }

    public sealed class SmallGasServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallGasServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallGas;
        protected override bool SupportsFilterUi => false;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryGasPort;
    }

    public sealed class SmallParticleServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallParticleServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallParticle;
        protected override bool SupportsFilterUi => false;
        protected override bool StoresParticles => true;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryParticlePort;
    }

    public sealed class SmallBatteryServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallBatteryServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallBattery;
        protected override bool SupportsFilterUi => false;
        protected override bool StoresPower => true;
    }

    public sealed class SmallColdStorageServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSmallColdStorageServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.SmallColdStorage;
        protected override bool UsesRefrigeratedStorage => true;
        protected override bool AllowManualRemoval => true;
        protected override Storage.FetchCategory FetchCategory => Storage.FetchCategory.GeneralStorage;
    }

    public sealed class MediumSolidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumSolidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumSolid;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategorySolidPort;
    }

    public sealed class MediumLiquidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumLiquidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumLiquid;
        protected override bool SupportsFilterUi => false;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryLiquidPort;
    }

    public sealed class MediumGasServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumGasServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumGas;
        protected override bool SupportsFilterUi => false;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryGasPort;
    }

    public sealed class MediumParticleServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumParticleServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumParticle;
        protected override bool SupportsFilterUi => false;
        protected override bool StoresParticles => true;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryParticlePort;
    }

    public sealed class MediumBatteryServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumBatteryServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumBattery;
        protected override bool SupportsFilterUi => false;
        protected override bool StoresPower => true;
    }

    public sealed class MediumColdStorageServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkMediumColdStorageServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.MediumColdStorage;
        protected override bool UsesRefrigeratedStorage => true;
        protected override bool AllowManualRemoval => true;
        protected override Storage.FetchCategory FetchCategory => Storage.FetchCategory.GeneralStorage;
    }

    public sealed class LargeSolidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeSolidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeSolid;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategorySolidPort;
    }

    public sealed class LargeLiquidServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeLiquidServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeLiquid;
        protected override bool SupportsFilterUi => false;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryLiquidPort;
    }

    public sealed class LargeGasServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeGasServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeGas;
        protected override bool SupportsFilterUi => false;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryGasPort;
    }

    public sealed class LargeParticleServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeParticleServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeParticle;
        protected override bool SupportsFilterUi => false;
        protected override bool StoresParticles => true;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategoryParticlePort;
    }

    public sealed class LargeBatteryServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeBatteryServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeBattery;
        protected override bool SupportsFilterUi => false;
        protected override bool StoresPower => true;
    }

    public sealed class LargeColdStorageServerConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkLargeColdStorageServer";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LargeColdStorage;
        protected override bool UsesRefrigeratedStorage => true;
        protected override bool AllowManualRemoval => true;
        protected override Storage.FetchCategory FetchCategory => Storage.FetchCategory.GeneralStorage;
    }

    [System.Obsolete("Compatibility prefab for old saves only. It may be removed in a future StorageNetwork update.")]
    public sealed class SceneStorageBoxConfig : StorageNetworkStorageBuildingBase
    {
        public const string ID = "StorageNetworkSceneStorageBox";
        protected override StorageNetworkStorageBuildingSpec Spec => StorageNetworkStorageBuildingSpecs.LegacySceneStorageBox;
        protected override bool AllowManualRemoval => true;
        protected override Storage.FetchCategory FetchCategory => Storage.FetchCategory.GeneralStorage;
        protected override bool SupportsStorageConnector => true;
        protected override Tag? StorageStateCategoryTag => StorageSceneTags.CategorySolidPort;
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            base.ConfigureBuildingTemplate(go, prefabTag);
            go.GetComponent<KPrefabID>()?.AddTag(StorageSceneTags.SceneStorageBox);
            go.AddOrGet<SceneStorageBoxMarker>();
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.StorageLocker;
            go.AddOrGet<StorageLocker>();
        }
    }

    public sealed class StorageNetworkRelayModuleConfig : IBuildingConfig
    {
        public const string ID = "StorageNetworkRelayModule";
        private const string Anim = "storagenetwork_relay_module_kanim";

        public override string[] GetRequiredDlcIds()
        {
            return DlcManager.EXPANSION1;
        }

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                5,
                3,
                Anim,
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
            SoundEventVolumeCache.instance.AddVolume(Anim, "RocketScannerModule_radar", NOISE_POLLUTION.NOISY.TIER2);
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
        public float PowerStorageJoulesLostPerSecond { get; set; }
        public List<Tag> Filters { get; set; }
    }

    public static class StorageNetworkStorageBuildingSpecs
    {
        private const string CoreAnim = "storagenetwork_core_kanim";
        private const string SmallSolidServerAnim = "storagenetwork_small_solid_server_kanim";
        private const string SmallLiquidServerAnim = "storagenetwork_small_liquid_server_kanim";
        private const string SmallGasServerAnim = "storagenetwork_small_gas_server_kanim";
        private const string SmallParticleServerAnim = SmallGasServerAnim;
        private const string SmallBatteryServerAnim = "storagenetwork_small_battery_server_kanim";
        private const string SmallColdStorageServerAnim = "storagenetwork_small_cold_storage_server_kanim";
        private const string MediumSolidServerAnim = "storagenetwork_medium_solid_server_kanim";
        private const string MediumLiquidServerAnim = "storagenetwork_medium_liquid_server_kanim";
        private const string MediumGasServerAnim = "storagenetwork_medium_gas_server_kanim";
        private const string MediumParticleServerAnim = MediumGasServerAnim;
        private const string MediumBatteryServerAnim = "storagenetwork_medium_battery_server_kanim";
        private const string MediumColdStorageServerAnim = "storagenetwork_medium_cold_storage_server_kanim";
        private const string LargeSolidServerAnim = "storagenetwork_large_solid_server_kanim";
        private const string LargeLiquidServerAnim = "storagenetwork_large_liquid_server_kanim";
        private const string LargeGasServerAnim = "storagenetwork_large_gas_server_kanim";
        private const string LargeParticleServerAnim = LargeGasServerAnim;
        private const string LargeBatteryServerAnim = "storagenetwork_large_battery_server_kanim";
        private const string LargeColdStorageServerAnim = "storagenetwork_large_cold_storage_server_kanim";
        private const string LegacySceneStorageBoxId = "StorageNetworkSceneStorageBox";
        private const string LegacySceneStorageBoxAnim = "storagelocker_kanim";
        private const float MeltingPoint = 1600f;
        private const float ServerSelfHeatKilowatts = 0.1f;
        private const float BatteryServerJoulesLostPerSecond = 100f;
        private const float SmallParticleServerCapacity = 500f;
        private const float MediumParticleServerCapacity = 2000f;
        private const float LargeParticleServerCapacity = 10000f;
        private const float SmallBatteryServerSelfHeatKilowatts = ServerSelfHeatKilowatts;
        private const float MediumBatteryServerSelfHeatKilowatts = ServerSelfHeatKilowatts;
        private const float LargeBatteryServerSelfHeatKilowatts = ServerSelfHeatKilowatts;

        public static readonly StorageNetworkStorageBuildingSpec Core = Create(
            StorageNetworkCoreConfig.ID,
            3,
            4,
            CoreAnim,
            500000f,
            240f,
            ServerSelfHeatKilowatts,
            STORAGEFILTERS.STORAGE_LOCKERS_STANDARD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static readonly StorageNetworkStorageBuildingSpec SmallSolid = CreateServer(
            SmallSolidServerConfig.ID,
            1,
            3,
            SmallSolidServerAnim,
            25000f,
            60f,
            STORAGEFILTERS.STORAGE_LOCKERS_STANDARD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3);

        public static readonly StorageNetworkStorageBuildingSpec SmallLiquid = CreateServer(
            SmallLiquidServerConfig.ID,
            1,
            3,
            SmallLiquidServerAnim,
            25000f,
            60f,
            STORAGEFILTERS.LIQUIDS,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3);

        public static readonly StorageNetworkStorageBuildingSpec SmallGas = CreateServer(
            SmallGasServerConfig.ID,
            1,
            3,
            SmallGasServerAnim,
            25000f,
            60f,
            STORAGEFILTERS.GASES,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3);

        public static readonly StorageNetworkStorageBuildingSpec SmallParticle = CreateServer(
            SmallParticleServerConfig.ID,
            1,
            3,
            SmallParticleServerAnim,
            SmallParticleServerCapacity,
            60f,
            new List<Tag> { GameTags.HighEnergyParticle },
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3);

        public static readonly StorageNetworkStorageBuildingSpec SmallBattery = CreateServer(
            SmallBatteryServerConfig.ID,
            1,
            3,
            SmallBatteryServerAnim,
            25000f,
            60f,
            STORAGEFILTERS.POWER_BANKS,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
            BatteryServerJoulesLostPerSecond,
            SmallBatteryServerSelfHeatKilowatts);

        public static readonly StorageNetworkStorageBuildingSpec SmallColdStorage = CreateServer(
            SmallColdStorageServerConfig.ID,
            1,
            3,
            SmallColdStorageServerAnim,
            25000f,
            60f,
            STORAGEFILTERS.FOOD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
            selfHeatKilowatts: 0f);

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

        public static readonly StorageNetworkStorageBuildingSpec MediumParticle = CreateServer(
            MediumParticleServerConfig.ID,
            2,
            2,
            MediumParticleServerAnim,
            MediumParticleServerCapacity,
            120f,
            new List<Tag> { GameTags.HighEnergyParticle },
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER4);

        public static readonly StorageNetworkStorageBuildingSpec MediumBattery = CreateServer(
            MediumBatteryServerConfig.ID,
            2,
            2,
            MediumBatteryServerAnim,
            100000f,
            120f,
            STORAGEFILTERS.POWER_BANKS,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
            BatteryServerJoulesLostPerSecond,
            MediumBatteryServerSelfHeatKilowatts);

        public static readonly StorageNetworkStorageBuildingSpec MediumColdStorage = CreateServer(
            MediumColdStorageServerConfig.ID,
            2,
            2,
            MediumColdStorageServerAnim,
            100000f,
            120f,
            STORAGEFILTERS.FOOD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
            selfHeatKilowatts: 0f);

        public static readonly StorageNetworkStorageBuildingSpec LargeSolid = CreateServer(
            LargeSolidServerConfig.ID,
            2,
            4,
            LargeSolidServerAnim,
            2500000f,
            240f,
            STORAGEFILTERS.STORAGE_LOCKERS_STANDARD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static readonly StorageNetworkStorageBuildingSpec LargeLiquid = CreateServer(
            LargeLiquidServerConfig.ID,
            2,
            4,
            LargeLiquidServerAnim,
            2500000f,
            240f,
            STORAGEFILTERS.LIQUIDS,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static readonly StorageNetworkStorageBuildingSpec LargeGas = CreateServer(
            LargeGasServerConfig.ID,
            2,
            4,
            LargeGasServerAnim,
            2500000f,
            240f,
            STORAGEFILTERS.GASES,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static readonly StorageNetworkStorageBuildingSpec LargeParticle = CreateServer(
            LargeParticleServerConfig.ID,
            2,
            4,
            LargeParticleServerAnim,
            LargeParticleServerCapacity,
            240f,
            new List<Tag> { GameTags.HighEnergyParticle },
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5);

        public static readonly StorageNetworkStorageBuildingSpec LargeBattery = CreateServer(
            LargeBatteryServerConfig.ID,
            2,
            4,
            LargeBatteryServerAnim,
            2500000f,
            240f,
            STORAGEFILTERS.POWER_BANKS,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5,
            BatteryServerJoulesLostPerSecond,
            LargeBatteryServerSelfHeatKilowatts);

        public static readonly StorageNetworkStorageBuildingSpec LargeColdStorage = CreateServer(
            LargeColdStorageServerConfig.ID,
            2,
            4,
            LargeColdStorageServerAnim,
            2500000f,
            240f,
            STORAGEFILTERS.FOOD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER5,
            selfHeatKilowatts: 0f);

        public static readonly StorageNetworkStorageBuildingSpec LegacySceneStorageBox = Create(
            LegacySceneStorageBoxId,
            1,
            2,
            LegacySceneStorageBoxAnim,
            500000f,
            0f,
            0f,
            STORAGEFILTERS.STORAGE_LOCKERS_STANDARD,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
            MATERIALS.RAW_MINERALS_OR_METALS,
            10f);

        public static IEnumerable<string> AllIds
        {
            get
            {
                yield return StorageNetworkCoreConfig.ID;
                yield return StorageNetworkOrderProductionCenterConfig.ID;
                yield return SmallSolidServerConfig.ID;
                yield return SmallLiquidServerConfig.ID;
                yield return SmallGasServerConfig.ID;
                yield return SmallParticleServerConfig.ID;
                yield return SmallBatteryServerConfig.ID;
                yield return SmallColdStorageServerConfig.ID;
                yield return MediumSolidServerConfig.ID;
                yield return MediumLiquidServerConfig.ID;
                yield return MediumGasServerConfig.ID;
                yield return MediumParticleServerConfig.ID;
                yield return MediumBatteryServerConfig.ID;
                yield return MediumColdStorageServerConfig.ID;
                yield return LargeSolidServerConfig.ID;
                yield return LargeLiquidServerConfig.ID;
                yield return LargeGasServerConfig.ID;
                yield return LargeParticleServerConfig.ID;
                yield return LargeBatteryServerConfig.ID;
                yield return LargeColdStorageServerConfig.ID;
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
            float[] constructionMass,
            float powerStorageJoulesLostPerSecond = 0f,
            float selfHeatKilowatts = ServerSelfHeatKilowatts)
        {
            return Create(id, width, height, animFile, capacityKg, powerWatts, selfHeatKilowatts, filters, constructionMass, powerStorageJoulesLostPerSecond);
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
            float[] constructionMass,
            float powerStorageJoulesLostPerSecond = 0f)
        {
            return Create(id, width, height, animFile, capacityKg, powerWatts, selfHeatKilowatts, filters, constructionMass, MATERIALS.REFINED_METALS, 30f, powerStorageJoulesLostPerSecond);
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
            float[] constructionMass,
            string[] constructionMaterials,
            float constructionTime,
            float powerStorageJoulesLostPerSecond = 0f)
        {
            return new StorageNetworkStorageBuildingSpec
            {
                Id = id,
                Width = width,
                Height = height,
                AnimFile = animFile,
                ConstructionTime = constructionTime,
                ConstructionMass = constructionMass,
                ConstructionMaterials = constructionMaterials,
                MeltingPoint = MeltingPoint,
                CapacityKg = capacityKg,
                PowerWatts = powerWatts,
                SelfHeatKilowatts = selfHeatKilowatts,
                PowerStorageJoulesLostPerSecond = powerStorageJoulesLostPerSecond,
                Filters = filters
            };
        }
    }
}
