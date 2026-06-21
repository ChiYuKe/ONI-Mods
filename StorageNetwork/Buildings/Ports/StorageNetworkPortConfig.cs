using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public enum StorageNetworkPortKind
    {
        SolidInput,
        SolidOutput,
        LiquidInput,
        LiquidOutput,
        GasInput,
        GasOutput,
        PowerInput,
        PowerOutput,
        ParticleInput,
        ParticleOutput
    }

    public abstract class StorageNetworkPortBuildingBase : IBuildingConfig
    {
        private const float PortConstructionTime = 40f;

        protected abstract StorageNetworkPortSpec Spec { get; }

        public override BuildingDef CreateBuildingDef()
        {
            StorageNetworkPortSpec spec = Spec;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                spec.Id,
                spec.Width,
                spec.Height,
                spec.AnimFile,
                1000,
                PortConstructionTime,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                MATERIALS.REFINED_METALS,
                9999f,
                BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.NONE,
                NOISE_POLLUTION.NOISY.TIER2,
                0.2f);

            buildingDef.ObjectLayer = ObjectLayer.Building;
            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.CanMove = false;
            buildingDef.UseStructureTemperature = false;
            buildingDef.ViewMode = spec.ViewMode;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);

            if (spec.ConduitType != ConduitType.None)
            {
                if (spec.Direction == StorageNetworkPortDirection.Input)
                {
                    buildingDef.InputConduitType = spec.ConduitType;
                    buildingDef.UtilityInputOffset = spec.InputOffset;
                }
                else
                {
                    buildingDef.OutputConduitType = spec.ConduitType;
                    buildingDef.UtilityOutputOffset = spec.OutputOffset;
                }
            }

            if (spec.ParticlePort)
            {
                buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(spec.ParticleOffset);
                if (spec.Direction == StorageNetworkPortDirection.Input)
                {
                    buildingDef.UseHighEnergyParticleInputPort = true;
                    buildingDef.HighEnergyParticleInputOffset = spec.ParticleOffset;
                }
                else
                {
                    buildingDef.UseHighEnergyParticleOutputPort = true;
                    buildingDef.HighEnergyParticleOutputOffset = spec.ParticleOffset;
                }
            }

            if (spec.PowerPort)
            {
                buildingDef.RequiresPowerInput = spec.Direction == StorageNetworkPortDirection.Input;
                buildingDef.RequiresPowerOutput = spec.Direction == StorageNetworkPortDirection.Output;
                buildingDef.PowerInputOffset = spec.Direction == StorageNetworkPortDirection.Input ? spec.PowerOffset : new CellOffset(0, 0);
                buildingDef.PowerOutputOffset = spec.Direction == StorageNetworkPortDirection.Output ? spec.PowerOffset : new CellOffset(0, 0);
                buildingDef.ElectricalArrowOffset = spec.PowerOffset;
                buildingDef.EnergyConsumptionWhenActive = 0f;
                buildingDef.GeneratorWattageRating = spec.Direction == StorageNetworkPortDirection.Output
                    ? StorageNetworkPowerOutputPortGenerator.GetMaxOutputWatts()
                    : 0f;
                buildingDef.GeneratorBaseCapacity = spec.Direction == StorageNetworkPortDirection.Output
                    ? StorageNetworkPowerOutputPortGenerator.GetMaxOutputWatts()
                    : 0f;
            }

            RegisterOverlay(spec);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            if (!Spec.ParticlePort)
            {
                GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            }

            go.AddOrGet<CodexEntryRedirector>().CodexID = Spec.Id;
            KPrefabID prefabId = go.GetComponent<KPrefabID>();
            prefabId?.AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            prefabId?.AddTag(StorageSceneTags.ModStorage);
            prefabId?.AddTag(Spec.Direction == StorageNetworkPortDirection.Input
                ? StorageSceneTags.CategoryInputPort
                : StorageSceneTags.CategoryOutputPort);
            AddPortCategoryTags(prefabId, Spec.Kind);

            Storage storage = go.AddOrGet<Storage>();
            storage.capacityKg = Spec.CapacityKg * GetCapacityMultiplier(Spec);
            storage.showInUI = !Spec.PowerPort;
            storage.allowItemRemoval = false;
            storage.showDescriptor = false;
            storage.storageFilters = Spec.Filters ?? new List<Tag>();
            storage.fetchCategory = Storage.FetchCategory.Building;
            storage.showCapacityStatusItem = false;
            storage.showCapacityAsMainStatus = false;

            go.AddOrGet<StorageNetworkSceneMember>();
            ConfigureRuntime(go, storage, Spec);
            go.AddOrGet<UserNameable>();
            go.AddOrGetDef<RocketUsageRestriction.Def>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            if (Spec.Kind == StorageNetworkPortKind.SolidOutput)
            {
                Object.DestroyImmediate(go.GetComponent<RequireOutputs>());
            }

            Prioritizable.AddRef(go);
        }

        private static void RegisterOverlay(StorageNetworkPortSpec spec)
        {
            switch (spec.ConduitType)
            {
                case ConduitType.Solid:
                    GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SolidConveyorIDs, spec.Id);
                    break;
                case ConduitType.Liquid:
                    GeneratedBuildings.RegisterWithOverlay(OverlayScreen.LiquidVentIDs, spec.Id);
                    break;
                case ConduitType.Gas:
                    GeneratedBuildings.RegisterWithOverlay(OverlayScreen.GasVentIDs, spec.Id);
                    break;
                default:
                    if (spec.PowerPort)
                    {
                        GeneratedBuildings.RegisterWithOverlay(OverlayScreen.WireIDs, spec.Id);
                    }
                    else if (spec.ParticlePort)
                    {
                        GeneratedBuildings.RegisterWithOverlay(OverlayScreen.RadiationIDs, spec.Id);
                    }

                    break;
            }
        }

        private static void ConfigureRuntime(GameObject go, Storage storage, StorageNetworkPortSpec spec)
        {
            if (spec.Kind == StorageNetworkPortKind.LiquidInput)
            {
                StorageNetworkLiquidInputPortIngressConduit.Configure(go, storage, spec.CapacityKg * Config.Instance.PortCapacityMultiplier);
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkLiquidInputPortIngress>();
            }
            else if (spec.Kind == StorageNetworkPortKind.LiquidOutput)
            {
                StorageNetworkLiquidOutputPortEgressConduit.Configure(go, storage);
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkLiquidOutputPortEgress>();
            }
            else if (spec.Kind == StorageNetworkPortKind.GasInput)
            {
                StorageNetworkGasInputPortIngressConduit.Configure(go, storage, spec.CapacityKg * Config.Instance.PortCapacityMultiplier);
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkGasInputPortIngress>();
            }
            else if (spec.Kind == StorageNetworkPortKind.GasOutput)
            {
                StorageNetworkGasOutputPortEgressConduit.Configure(go, storage);
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkGasOutputPortEgress>();
            }
            else if (spec.Kind == StorageNetworkPortKind.PowerInput)
            {
                Battery battery = go.AddOrGet<Battery>();
                battery.capacity = spec.CapacityKg * Config.Instance.PowerPortCapacityMultiplier;
                battery.chargeWattage = Config.Instance.PowerInputMaxWatts;
                battery.joulesLostPerSecond = 0f;
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkPowerInputPortConsumer>();
            }
            else if (spec.Kind == StorageNetworkPortKind.PowerOutput)
            {
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkPowerOutputPortGenerator>();
            }
            else if (spec.Kind == StorageNetworkPortKind.ParticleInput)
            {
                go.AddOrGet<LogicOperationalController>();
                HighEnergyParticlePort port = go.AddOrGet<HighEnergyParticlePort>();
                port.particleInputEnabled = true;
                port.particleInputOffset = spec.ParticleOffset;
                port.requireOperational = true;
                HighEnergyParticleStorage particleStorage = go.AddOrGet<HighEnergyParticleStorage>();
                particleStorage.capacity = 0f;
                particleStorage.showInUI = false;
                particleStorage.showCapacityStatusItem = false;
                particleStorage.showCapacityAsMainStatus = false;
                particleStorage.autoStore = false;
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkParticleInputPortIngress>();
            }
            else if (spec.Kind == StorageNetworkPortKind.ParticleOutput)
            {
                go.AddOrGet<LogicOperationalController>();
                HighEnergyParticlePort port = go.AddOrGet<HighEnergyParticlePort>();
                port.particleOutputEnabled = true;
                port.particleOutputOffset = spec.ParticleOffset;
                port.requireOperational = true;
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkParticleOutputPortEgress>();
            }
            else if (spec.Kind == StorageNetworkPortKind.SolidInput)
            {
                SolidConduitConsumer consumer = go.AddOrGet<SolidConduitConsumer>();
                consumer.storage = storage;
                consumer.capacityTag = GameTags.Any;
                consumer.capacityKG = spec.CapacityKg * Config.Instance.PortCapacityMultiplier;
                consumer.alwaysConsume = true;
                go.AddOrGet<Automatable>();
                TreeFilterable filterable = go.AddOrGet<TreeFilterable>();
                filterable.dropIncorrectOnFilterChange = false;
                filterable.autoSelectStoredOnLoad = false;
                filterable.preventAutoAddOnDiscovery = true;
                filterable.copySettingsEnabled = true;
                filterable.tintOnNoFiltersSet = false;
                filterable.uiHeight = TreeFilterable.UISideScreenHeight.Tall;
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkDefaultFilterInitializer>();
                go.AddOrGet<StorageNetworkSolidInputPortIngress>();
            }
            else if (spec.Kind == StorageNetworkPortKind.SolidOutput)
            {
                SolidConduitDispenser dispenser = go.AddOrGet<SolidConduitDispenser>();
                dispenser.storage = storage;
                dispenser.elementFilter = null;
                dispenser.solidOnly = true;
                dispenser.alwaysDispense = true;
                go.AddOrGet<CopyBuildingSettings>();
                go.AddOrGet<StorageNetworkSolidOutputPortEgress>();
            }
        }

        private static void AddPortCategoryTags(KPrefabID prefabId, StorageNetworkPortKind kind)
        {
            if (prefabId == null)
            {
                return;
            }

            switch (kind)
            {
                case StorageNetworkPortKind.SolidInput:
                    prefabId.AddTag(StorageSceneTags.CategorySolidPort);
                    prefabId.AddTag(StorageSceneTags.CategorySolidInputPort);
                    break;
                case StorageNetworkPortKind.SolidOutput:
                    prefabId.AddTag(StorageSceneTags.CategorySolidPort);
                    prefabId.AddTag(StorageSceneTags.CategorySolidOutputPort);
                    break;
                case StorageNetworkPortKind.LiquidInput:
                    prefabId.AddTag(StorageSceneTags.CategoryLiquidPort);
                    prefabId.AddTag(StorageSceneTags.CategoryLiquidInputPort);
                    break;
                case StorageNetworkPortKind.LiquidOutput:
                    prefabId.AddTag(StorageSceneTags.CategoryLiquidPort);
                    prefabId.AddTag(StorageSceneTags.CategoryLiquidOutputPort);
                    break;
                case StorageNetworkPortKind.GasInput:
                    prefabId.AddTag(StorageSceneTags.CategoryGasPort);
                    prefabId.AddTag(StorageSceneTags.CategoryGasInputPort);
                    break;
                case StorageNetworkPortKind.GasOutput:
                    prefabId.AddTag(StorageSceneTags.CategoryGasPort);
                    prefabId.AddTag(StorageSceneTags.CategoryGasOutputPort);
                    break;
                case StorageNetworkPortKind.PowerInput:
                    prefabId.AddTag(StorageSceneTags.CategoryPowerPort);
                    prefabId.AddTag(StorageSceneTags.CategoryPowerInputPort);
                    break;
                case StorageNetworkPortKind.PowerOutput:
                    prefabId.AddTag(StorageSceneTags.CategoryPowerPort);
                    prefabId.AddTag(StorageSceneTags.CategoryPowerOutputPort);
                    break;
                case StorageNetworkPortKind.ParticleInput:
                    prefabId.AddTag(StorageSceneTags.CategoryParticlePort);
                    prefabId.AddTag(StorageSceneTags.CategoryParticleInputPort);
                    break;
                case StorageNetworkPortKind.ParticleOutput:
                    prefabId.AddTag(StorageSceneTags.CategoryParticlePort);
                    prefabId.AddTag(StorageSceneTags.CategoryParticleOutputPort);
                    break;
            }
        }

        private static float GetCapacityMultiplier(StorageNetworkPortSpec spec)
        {
            return spec.PowerPort
                ? Config.Instance.PowerPortCapacityMultiplier
                : Config.Instance.PortCapacityMultiplier;
        }

    }

    public sealed class StorageNetworkSolidInputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkSolidInputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.SolidInput;
    }

    public sealed class StorageNetworkSolidOutputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkSolidOutputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.SolidOutput;
    }

    public sealed class StorageNetworkLiquidInputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkLiquidInputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.LiquidInput;
    }

    public sealed class StorageNetworkLiquidOutputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkLiquidOutputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.LiquidOutput;
    }

    public sealed class StorageNetworkGasInputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkGasInputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.GasInput;
    }

    public sealed class StorageNetworkGasOutputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkGasOutputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.GasOutput;
    }

    public sealed class StorageNetworkPowerInputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkPowerInputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.PowerInput;
    }

    public sealed class StorageNetworkPowerOutputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkPowerOutputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.PowerOutput;
    }

    public sealed class StorageNetworkParticleInputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkParticleInputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.ParticleInput;
    }

    public sealed class StorageNetworkParticleOutputPortConfig : StorageNetworkPortBuildingBase
    {
        public const string ID = "StorageNetworkParticleOutputPort";
        protected override StorageNetworkPortSpec Spec => StorageNetworkPortSpecs.ParticleOutput;
    }

    public enum StorageNetworkPortDirection
    {
        Input,
        Output
    }

    public sealed class StorageNetworkPortSpec
    {
        public string Id { get; set; }
        public StorageNetworkPortKind Kind { get; set; }
        public StorageNetworkPortDirection Direction { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string AnimFile { get; set; }
        public HashedString ViewMode { get; set; }
        public ConduitType ConduitType { get; set; }
        public bool PowerPort { get; set; }
        public bool ParticlePort { get; set; }
        public CellOffset InputOffset { get; set; }
        public CellOffset OutputOffset { get; set; }
        public CellOffset PowerOffset { get; set; }
        public CellOffset ParticleOffset { get; set; }
        public float CapacityKg { get; set; }
        public List<Tag> Filters { get; set; }
    }

    public static class StorageNetworkPortSpecs
    {
        private static readonly CellOffset AccessoryInputOffset = new CellOffset(0, 0);
        private static readonly CellOffset AccessoryOutputOffset = new CellOffset(0, 0);
        private static readonly CellOffset AccessoryPowerOffset = new CellOffset(0, 0);
        private static readonly CellOffset AccessoryParticleOffset = new CellOffset(0, 0);

        private const string SolidInputPortAnimFile = "StorageNetworkSolidInputPort_kanim";
        private const string SolidOutputPortAnimFile = "StorageNetworkSolidOutputPort_kanim";
        private const string LiquidInputPortAnimFile = "StorageNetworkLiquidInputPort_kanim";
        private const string LiquidOutputPortAnimFile = "StorageNetworkLiquidOutputPort_kanim";
        private const string GasInputPortAnimFile = "StorageNetworkGasInputPort_kanim";
        private const string GasOutputPortAnimFile = "StorageNetworkGasOutputPort_kanim";
        private const string PowerInputPortAnimFile = "StorageNetworkPowerInputPort_kanim";
        private const string PowerOutputPortAnimFile = "StorageNetworkPowerOutputPort_kanim";
        private const string ParticleInputPortAnimFile = "StorageNetworkParticleInputPort_kanim";
        private const string ParticleOutputPortAnimFile = "StorageNetworkParticleOutputPort_kanim";
        private const float PowerInputPortCapacityJoules = 10000f;
        private const float PowerOutputPortCapacityJoules = 10000f;

        public static readonly StorageNetworkPortSpec SolidInput = Create(
            StorageNetworkSolidInputPortConfig.ID,
            StorageNetworkPortKind.SolidInput,
            StorageNetworkPortDirection.Input,
            SolidInputPortAnimFile,
            OverlayModes.SolidConveyor.ID,
            ConduitType.Solid,
            20000f);

        public static readonly StorageNetworkPortSpec SolidOutput = Create(
            StorageNetworkSolidOutputPortConfig.ID,
            StorageNetworkPortKind.SolidOutput,
            StorageNetworkPortDirection.Output,
            SolidOutputPortAnimFile,
            OverlayModes.SolidConveyor.ID,
            ConduitType.Solid,
            2000f);

        public static readonly StorageNetworkPortSpec LiquidInput = Create(
            StorageNetworkLiquidInputPortConfig.ID,
            StorageNetworkPortKind.LiquidInput,
            StorageNetworkPortDirection.Input,
            LiquidInputPortAnimFile,
            OverlayModes.LiquidConduits.ID,
            ConduitType.Liquid,
            200f);

        public static readonly StorageNetworkPortSpec LiquidOutput = Create(
            StorageNetworkLiquidOutputPortConfig.ID,
            StorageNetworkPortKind.LiquidOutput,
            StorageNetworkPortDirection.Output,
            LiquidOutputPortAnimFile,
            OverlayModes.LiquidConduits.ID,
            ConduitType.Liquid,
            50f);

        public static readonly StorageNetworkPortSpec GasInput = Create(
            StorageNetworkGasInputPortConfig.ID,
            StorageNetworkPortKind.GasInput,
            StorageNetworkPortDirection.Input,
            GasInputPortAnimFile,
            OverlayModes.GasConduits.ID,
            ConduitType.Gas,
            200f);

        public static readonly StorageNetworkPortSpec GasOutput = Create(
            StorageNetworkGasOutputPortConfig.ID,
            StorageNetworkPortKind.GasOutput,
            StorageNetworkPortDirection.Output,
            GasOutputPortAnimFile,
            OverlayModes.GasConduits.ID,
            ConduitType.Gas,
            5f);

        public static readonly StorageNetworkPortSpec PowerInput = CreatePower(
            StorageNetworkPowerInputPortConfig.ID,
            StorageNetworkPortKind.PowerInput,
            StorageNetworkPortDirection.Input,
            PowerInputPortAnimFile,
            PowerInputPortCapacityJoules);

        public static readonly StorageNetworkPortSpec PowerOutput = CreatePower(
            StorageNetworkPowerOutputPortConfig.ID,
            StorageNetworkPortKind.PowerOutput,
            StorageNetworkPortDirection.Output,
            PowerOutputPortAnimFile,
            PowerOutputPortCapacityJoules);

        public static readonly StorageNetworkPortSpec ParticleInput = CreateParticle(
            StorageNetworkParticleInputPortConfig.ID,
            StorageNetworkPortKind.ParticleInput,
            StorageNetworkPortDirection.Input,
            ParticleInputPortAnimFile);

        public static readonly StorageNetworkPortSpec ParticleOutput = CreateParticle(
            StorageNetworkParticleOutputPortConfig.ID,
            StorageNetworkPortKind.ParticleOutput,
            StorageNetworkPortDirection.Output,
            ParticleOutputPortAnimFile);

        public static IEnumerable<string> AllIds
        {
            get
            {
                yield return StorageNetworkSolidInputPortConfig.ID;
                yield return StorageNetworkSolidOutputPortConfig.ID;
                yield return StorageNetworkLiquidInputPortConfig.ID;
                yield return StorageNetworkLiquidOutputPortConfig.ID;
                yield return StorageNetworkGasInputPortConfig.ID;
                yield return StorageNetworkGasOutputPortConfig.ID;
                yield return StorageNetworkPowerInputPortConfig.ID;
                yield return StorageNetworkPowerOutputPortConfig.ID;
                yield return StorageNetworkParticleInputPortConfig.ID;
                yield return StorageNetworkParticleOutputPortConfig.ID;
            }
        }

        private static StorageNetworkPortSpec Create(
            string id,
            StorageNetworkPortKind kind,
            StorageNetworkPortDirection direction,
            string animFile,
            HashedString viewMode,
            ConduitType conduitType,
            float capacityKg = 10000f)
        {
            return new StorageNetworkPortSpec
            {
                Id = id,
                Kind = kind,
                Direction = direction,
                Width = 1,
                Height = 1,
                AnimFile = animFile,
                ViewMode = viewMode,
                ConduitType = conduitType,
                InputOffset = AccessoryInputOffset,
                OutputOffset = AccessoryOutputOffset,
                PowerOffset = AccessoryPowerOffset,
                ParticleOffset = AccessoryParticleOffset,
                CapacityKg = capacityKg,
                Filters = GetStorageFilters(kind, conduitType)
            };
        }

        private static StorageNetworkPortSpec CreatePower(string id, StorageNetworkPortKind kind, StorageNetworkPortDirection direction, string animFile, float capacityJoules)
        {
            return new StorageNetworkPortSpec
            {
                Id = id,
                Kind = kind,
                Direction = direction,
                Width = 1,
                Height = 1,
                AnimFile = animFile,
                ViewMode = OverlayModes.Power.ID,
                ConduitType = ConduitType.None,
                PowerPort = true,
                InputOffset = AccessoryInputOffset,
                OutputOffset = AccessoryOutputOffset,
                PowerOffset = AccessoryPowerOffset,
                ParticleOffset = AccessoryParticleOffset,
                CapacityKg = capacityJoules,
                Filters = new List<Tag>()
            };
        }

        private static StorageNetworkPortSpec CreateParticle(string id, StorageNetworkPortKind kind, StorageNetworkPortDirection direction, string animFile)
        {
            return new StorageNetworkPortSpec
            {
                Id = id,
                Kind = kind,
                Direction = direction,
                Width = 1,
                Height = 1,
                AnimFile = animFile,
                ViewMode = OverlayModes.Radiation.ID,
                ConduitType = ConduitType.None,
                ParticlePort = true,
                InputOffset = AccessoryInputOffset,
                OutputOffset = AccessoryOutputOffset,
                PowerOffset = AccessoryPowerOffset,
                ParticleOffset = AccessoryParticleOffset,
                CapacityKg = 0f,
                Filters = new List<Tag>()
            };
        }

        private static List<Tag> GetStorageFilters(StorageNetworkPortKind kind, ConduitType conduitType)
        {
            switch (conduitType)
            {
                case ConduitType.Solid:
                    if (kind == StorageNetworkPortKind.SolidInput)
                    {
                        HashSet<Tag> filters = new HashSet<Tag>(STORAGEFILTERS.STORAGE_LOCKERS_STANDARD);
                        foreach (Tag foodFilter in STORAGEFILTERS.FOOD)
                        {
                            filters.Add(foodFilter);
                        }

                        return new List<Tag>(filters);
                    }

                    return new List<Tag>(STORAGEFILTERS.STORAGE_LOCKERS_STANDARD);
                case ConduitType.Liquid:
                    return new List<Tag>(STORAGEFILTERS.LIQUIDS);
                case ConduitType.Gas:
                    return new List<Tag>(STORAGEFILTERS.GASES);
                default:
                    return new List<Tag>();
            }
        }
    }
}
