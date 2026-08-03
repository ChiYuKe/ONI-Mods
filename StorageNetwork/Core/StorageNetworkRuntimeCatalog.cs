using System.Collections.Generic;
using StorageNetwork.API;
using StorageNetwork.Buildings;
using StorageNetwork.Components;

namespace StorageNetwork.Core
{
    /// <summary>
    /// Runtime-only component and role directory. Native Storage remains authoritative;
    /// descriptors only cache topology/capability data that changes on explicit version
    /// boundaries.
    /// </summary>
    internal static class StorageNetworkRuntimeCatalog
    {
        private static readonly Dictionary<Storage, StorageRuntimeDescriptor> Descriptors =
            new Dictionary<Storage, StorageRuntimeDescriptor>();
        private static readonly Dictionary<TreeFilterable, Storage> StoragesByFilter =
            new Dictionary<TreeFilterable, Storage>();
        private static readonly Dictionary<ComplexFabricator, StorageNetworkMaterialRequester> RequestersByFabricator =
            new Dictionary<ComplexFabricator, StorageNetworkMaterialRequester>();
        private static int capabilityGeneration;

        public static void Register(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            if (!Descriptors.TryGetValue(storage, out StorageRuntimeDescriptor descriptor))
            {
                descriptor = new StorageRuntimeDescriptor(storage);
                Descriptors.Add(storage, descriptor);
            }

            RefreshDescriptor(descriptor, force: true);
        }

        private static void RefreshDescriptor(
            StorageRuntimeDescriptor descriptor,
            bool force)
        {
            TreeFilterable previousFilter = descriptor.TreeFilterable;
            descriptor.EnsureCurrent(capabilityGeneration, force);
            if (previousFilter != null && previousFilter != descriptor.TreeFilterable)
            {
                StoragesByFilter.Remove(previousFilter);
            }

            if (descriptor.TreeFilterable != null)
            {
                StoragesByFilter[descriptor.TreeFilterable] = descriptor.Storage;
            }
        }

        public static void Unregister(Storage storage)
        {
            if (!ReferenceEquals(storage, null) &&
                Descriptors.TryGetValue(storage, out StorageRuntimeDescriptor descriptor))
            {
                if (descriptor.TreeFilterable != null)
                {
                    StoragesByFilter.Remove(descriptor.TreeFilterable);
                }

                Descriptors.Remove(storage);
            }
        }

        public static bool TryGet(
            Storage storage,
            out StorageRuntimeDescriptor descriptor)
        {
            descriptor = null;
            if (storage == null || !Descriptors.TryGetValue(storage, out descriptor))
            {
                return false;
            }

            RefreshDescriptor(descriptor, force: false);
            return true;
        }

        public static void InvalidateAllCapabilities()
        {
            unchecked
            {
                capabilityGeneration++;
            }
        }

        public static bool TryGetStorage(
            TreeFilterable filter,
            out Storage storage)
        {
            storage = null;
            return filter != null &&
                   StoragesByFilter.TryGetValue(filter, out storage) &&
                   storage != null;
        }

        public static void RegisterMaterialRequester(
            ComplexFabricator fabricator,
            StorageNetworkMaterialRequester requester)
        {
            if (fabricator != null && requester != null)
            {
                RequestersByFabricator[fabricator] = requester;
            }
        }

        public static void UnregisterMaterialRequester(
            ComplexFabricator fabricator,
            StorageNetworkMaterialRequester requester)
        {
            if (fabricator != null &&
                RequestersByFabricator.TryGetValue(
                    fabricator,
                    out StorageNetworkMaterialRequester current) &&
                ReferenceEquals(current, requester))
            {
                RequestersByFabricator.Remove(fabricator);
            }
        }

        public static bool TryGetMaterialRequester(
            ComplexFabricator fabricator,
            out StorageNetworkMaterialRequester requester)
        {
            requester = null;
            return fabricator != null &&
                   RequestersByFabricator.TryGetValue(fabricator, out requester) &&
                   requester != null;
        }

        public static void ResetRuntimeState()
        {
            Descriptors.Clear();
            StoragesByFilter.Clear();
            RequestersByFabricator.Clear();
            capabilityGeneration = 0;
        }
    }

    internal sealed class StorageRuntimeDescriptor
    {
        private readonly Storage storage;
        private int capabilityVersion = int.MinValue;

        public StorageRuntimeDescriptor(Storage storage)
        {
            this.storage = storage;
        }

        public Storage Storage => storage;
        public int WorldId { get; private set; }
        public int PrefabInstanceId { get; private set; }
        public Tag PrefabTag { get; private set; }
        public StorageNetworkStorageFlags Flags { get; private set; }
        public Operational Operational { get; private set; }
        public TreeFilterable TreeFilterable { get; private set; }
        public StorageNetworkFilterState FilterState { get; private set; }
        public StorageNetworkEnrollment Enrollment { get; private set; }
        public ComplexFabricator Fabricator { get; private set; }
        public EnergyGenerator EnergyGenerator { get; private set; }
        public bool IsMinionStorage { get; private set; }
        public bool IsProductionStorage { get; private set; }
        public bool IsPowerStorage { get; private set; }
        public bool IsParticleStorage { get; private set; }
        public bool IsColdStorage { get; private set; }
        public bool IsSolidStorage { get; private set; }
        public bool IsLiquidStorage { get; private set; }
        public bool IsGasStorage { get; private set; }
        public bool HasRefrigerator { get; private set; }
        public bool HasReservoir { get; private set; }

        public bool IsOperational => Operational == null || Operational.IsOperational;
        public bool IsServerStorage =>
            (Flags & (StorageNetworkStorageFlags.NetworkStorage |
                      StorageNetworkStorageFlags.ServerStorage)) ==
            (StorageNetworkStorageFlags.NetworkStorage |
             StorageNetworkStorageFlags.ServerStorage);
        public bool IsNetworkPort =>
            (Flags & (StorageNetworkStorageFlags.InputPort |
                      StorageNetworkStorageFlags.OutputPort)) != 0;

        public void EnsureCurrent(int currentCapabilityVersion, bool force)
        {
            if ((!force && capabilityVersion == currentCapabilityVersion) || storage == null)
            {
                return;
            }

            capabilityVersion = currentCapabilityVersion;
            WorldId = storage.gameObject != null ? storage.gameObject.GetMyWorldId() : -1;
            KPrefabID prefabId = storage.GetComponent<KPrefabID>();
            PrefabInstanceId = prefabId != null
                ? prefabId.InstanceID
                : KPrefabID.InvalidInstanceID;
            PrefabTag = prefabId != null ? prefabId.PrefabTag : Tag.Invalid;
            Flags = StorageNetworkInterfaceResolver.GetStorageFlags(storage);
            Operational = storage.GetComponent<Operational>();
            TreeFilterable = storage.GetComponent<TreeFilterable>();
            FilterState = storage.GetComponent<StorageNetworkFilterState>();
            Enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            Fabricator = storage.GetComponent<ComplexFabricator>();
            EnergyGenerator = storage.GetComponent<EnergyGenerator>();
            IsMinionStorage = storage.GetComponent<MinionIdentity>() != null;
            IsPowerStorage = storage.GetComponent<StorageNetworkPowerStorage>() != null;
            IsParticleStorage = storage.GetComponent<HighEnergyParticleStorage>() != null ||
                                IsParticleServerPrefab(PrefabTag);
            IsColdStorage = storage.GetComponent<StorageNetworkColdStorageCooling>() != null ||
                            IsColdServerPrefab(PrefabTag);
            IsSolidStorage = IsSolidServerPrefab(PrefabTag);
            IsLiquidStorage = IsLiquidServerPrefab(PrefabTag);
            IsGasStorage = IsGasServerPrefab(PrefabTag);
            HasRefrigerator = storage.GetComponent<Refrigerator>() != null;
            HasReservoir = storage.GetComponent<Reservoir>() != null;
            IsProductionStorage =
                Enrollment != null &&
                    (Enrollment.IsComplexRecipeBuilding() ||
                     Enrollment.IsEnergyGeneratorBuilding()) ||
                StorageNetworkEnergyGeneratorRequester.HasFuelInputs(EnergyGenerator) ||
                Fabricator != null;
        }

        private static bool IsColdServerPrefab(Tag prefabTag)
        {
            return prefabTag == SmallColdStorageServerConfig.ID ||
                   prefabTag == MediumColdStorageServerConfig.ID ||
                   prefabTag == LargeColdStorageServerConfig.ID;
        }

        private static bool IsSolidServerPrefab(Tag prefabTag)
        {
            return prefabTag == SmallSolidServerConfig.ID ||
                   prefabTag == MediumSolidServerConfig.ID ||
                   prefabTag == LargeSolidServerConfig.ID;
        }

        private static bool IsLiquidServerPrefab(Tag prefabTag)
        {
            return prefabTag == SmallLiquidServerConfig.ID ||
                   prefabTag == MediumLiquidServerConfig.ID ||
                   prefabTag == LargeLiquidServerConfig.ID;
        }

        private static bool IsGasServerPrefab(Tag prefabTag)
        {
            return prefabTag == SmallGasServerConfig.ID ||
                   prefabTag == MediumGasServerConfig.ID ||
                   prefabTag == LargeGasServerConfig.ID;
        }

        private static bool IsParticleServerPrefab(Tag prefabTag)
        {
            return prefabTag == SmallParticleServerConfig.ID ||
                   prefabTag == MediumParticleServerConfig.ID ||
                   prefabTag == LargeParticleServerConfig.ID;
        }
    }
}
