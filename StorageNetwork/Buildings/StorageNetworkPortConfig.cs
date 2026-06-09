using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public abstract class StorageNetworkPortBuildingBase : IBuildingConfig
    {
        private const float PowerPortBatteryCapacityKJ = 10000f;
        private const float PowerPortBatteryJoulesLostPerSecond = 1.6666666f;

        protected abstract StorageNetworkPortSpec Spec { get; }

        public override BuildingDef CreateBuildingDef()
        {
            StorageNetworkPortSpec spec = Spec;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                spec.Id,
                spec.Width,
                spec.Height,
                spec.AnimFile,
                30,
                30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
                MATERIALS.REFINED_METALS,
                1600f,
                BuildLocationRule.Anywhere,
                BUILDINGS.DECOR.PENALTY.TIER1,
                NOISE_POLLUTION.NONE,
                0.2f);

            buildingDef.Floodable = false;
            buildingDef.Overheatable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.ViewMode = spec.ViewMode;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            buildingDef.AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);

            if (spec.ConduitType != ConduitType.None)
            {
                if (spec.Direction == StorageNetworkPortDirection.Input)
                {
                    buildingDef.InputConduitType = spec.ConduitType;
                    buildingDef.UtilityInputOffset = spec.UtilityOffset;
                }
                else
                {
                    buildingDef.OutputConduitType = spec.ConduitType;
                    buildingDef.UtilityOutputOffset = spec.UtilityOffset;
                }
            }

            if (spec.PowerPort)
            {
                buildingDef.RequiresPowerInput = spec.Direction == StorageNetworkPortDirection.Input;
                buildingDef.RequiresPowerOutput = spec.Direction == StorageNetworkPortDirection.Output;
                buildingDef.PowerInputOffset = spec.Direction == StorageNetworkPortDirection.Input ? spec.UtilityOffset : new CellOffset(0, 0);
                buildingDef.PowerOutputOffset = spec.Direction == StorageNetworkPortDirection.Output ? spec.UtilityOffset : new CellOffset(0, 0);
                buildingDef.ElectricalArrowOffset = spec.UtilityOffset;
            }

            RegisterOverlay(spec);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            KPrefabID prefabId = go.GetComponent<KPrefabID>();
            prefabId?.AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            prefabId?.AddTag(StorageSceneTags.ModStorage);

            Storage storage = go.AddOrGet<Storage>();
            storage.capacityKg = Spec.CapacityKg;
            storage.showInUI = !Spec.PowerPort;
            storage.allowItemRemoval = false;
            storage.showDescriptor = false;
            storage.storageFilters = Spec.Filters ?? new List<Tag>();
            storage.fetchCategory = Storage.FetchCategory.Building;
            storage.showCapacityStatusItem = false;
            storage.showCapacityAsMainStatus = false;

            go.AddOrGet<StorageNetworkPort>().Configure(Spec.Kind);
            go.AddOrGet<UserNameable>();
            if (Spec.Kind == StorageNetworkPortKind.SolidInput)
            {
                Automatable automatable = go.AddOrGet<Automatable>();
                automatable.SetAutomationOnly(false);
                go.AddOrGet<StorageNetworkPortManualFetch>();
            }

            if (!Spec.PowerPort && Spec.Direction == StorageNetworkPortDirection.Input)
            {
                go.AddOrGet<StorageNetworkStorageConnector>();
            }
            else if (!Spec.PowerPort && Spec.Direction == StorageNetworkPortDirection.Output)
            {
                go.AddOrGet<StorageNetworkPortRequester>();
                ConfigureOutputDispenser(go, storage, Spec);
            }

            if (ShouldShowFilterUI(Spec))
            {
                go.AddOrGet<TreeFilterable>();
                if (ShouldInitializeDefaultFilters(Spec))
                {
                    go.AddOrGet<StorageNetworkDefaultFilterInitializer>();
                }
            }

            go.AddOrGetDef<RocketUsageRestriction.Def>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            Prioritizable.AddRef(go);
            if (Spec.PowerPort)
            {
                Battery battery = go.AddOrGet<Battery>();
                battery.capacity = PowerPortBatteryCapacityKJ;
                battery.joulesLostPerSecond = PowerPortBatteryJoulesLostPerSecond;
                battery.powerSortOrder = 1000;
                go.AddOrGetDef<PoweredActiveController.Def>();
            }
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

                    break;
            }
        }

        private static bool ShouldShowFilterUI(StorageNetworkPortSpec spec)
        {
            return !spec.PowerPort &&
                   spec.Kind != StorageNetworkPortKind.LiquidInput &&
                   spec.Kind != StorageNetworkPortKind.GasInput;
        }

        private static bool ShouldInitializeDefaultFilters(StorageNetworkPortSpec spec)
        {
            return spec.Kind != StorageNetworkPortKind.LiquidOutput &&
                   spec.Kind != StorageNetworkPortKind.GasOutput;
        }

        private static void ConfigureOutputDispenser(GameObject go, Storage storage, StorageNetworkPortSpec spec)
        {
            switch (spec.ConduitType)
            {
                case ConduitType.Solid:
                    SolidConduitDispenser solidDispenser = go.AddOrGet<SolidConduitDispenser>();
                    solidDispenser.storage = storage;
                    solidDispenser.elementFilter = null;
                    solidDispenser.alwaysDispense = true;
                    solidDispenser.solidOnly = true;
                    break;
                case ConduitType.Liquid:
                case ConduitType.Gas:
                    ConduitDispenser conduitDispenser = go.AddOrGet<ConduitDispenser>();
                    conduitDispenser.conduitType = spec.ConduitType;
                    conduitDispenser.elementFilter = null;
                    conduitDispenser.storage = storage;
                    conduitDispenser.alwaysDispense = true;
                    go.AddOrGet<RequireOutputs>().ignoreFullPipe = true;
                    break;
            }
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
        public CellOffset UtilityOffset { get; set; }
        public float CapacityKg { get; set; }
        public List<Tag> Filters { get; set; }
    }

    public static class StorageNetworkPortSpecs
    {
        private static readonly CellOffset PortUtilityOffset = new CellOffset(0, 0);

        private const string SolidInputPortAnimFile = "StorageNetworkSolidInputPort_kanim";
        private const string SolidOutputPortAnimFile = "StorageNetworkSolidOutputPort_kanim";
        private const string LiquidInputPortAnimFile = "StorageNetworkLiquidInputPort_kanim";
        private const string LiquidOutputPortAnimFile = "StorageNetworkLiquidOutputPort_kanim";
        private const string GasInputPortAnimFile = "StorageNetworkGasInputPort_kanim";
        private const string GasOutputPortAnimFile = "StorageNetworkGasOutputPort_kanim";
        private const string PowerInputPortAnimFile = "StorageNetworkPowerInputPort_kanim";
        private const string PowerOutputPortAnimFile = "StorageNetworkPowerOutputPort_kanim";

        public static readonly StorageNetworkPortSpec SolidInput = Create(
            StorageNetworkSolidInputPortConfig.ID,
            StorageNetworkPortKind.SolidInput,
            StorageNetworkPortDirection.Input,
            SolidInputPortAnimFile,
            OverlayModes.SolidConveyor.ID,
            ConduitType.Solid);

        public static readonly StorageNetworkPortSpec SolidOutput = Create(
            StorageNetworkSolidOutputPortConfig.ID,
            StorageNetworkPortKind.SolidOutput,
            StorageNetworkPortDirection.Output,
            SolidOutputPortAnimFile,
            OverlayModes.SolidConveyor.ID,
            ConduitType.Solid);

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
            PowerInputPortAnimFile);

        public static readonly StorageNetworkPortSpec PowerOutput = CreatePower(
            StorageNetworkPowerOutputPortConfig.ID,
            StorageNetworkPortKind.PowerOutput,
            StorageNetworkPortDirection.Output,
            PowerOutputPortAnimFile);

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
                UtilityOffset = PortUtilityOffset,
                CapacityKg = capacityKg,
                Filters = GetStorageFilters(conduitType)
            };
        }

        private static StorageNetworkPortSpec CreatePower(string id, StorageNetworkPortKind kind, StorageNetworkPortDirection direction, string animFile)
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
                UtilityOffset = PortUtilityOffset,
                CapacityKg = 10000f,
                Filters = new List<Tag>()
            };
        }

        private static List<Tag> GetStorageFilters(ConduitType conduitType)
        {
            switch (conduitType)
            {
                case ConduitType.Solid:
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
