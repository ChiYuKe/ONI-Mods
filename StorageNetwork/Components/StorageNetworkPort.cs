using KSerialization;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using StorageNetwork.Services;

namespace StorageNetwork.Components
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
        PowerOutput
    }

    public sealed class StorageNetworkPort : KMonoBehaviour
    {
        [Serialize]
        private StorageNetworkPortKind kind;

        public StorageNetworkPortKind Kind => kind;

        public bool IsSolidMaterialPort => Kind == StorageNetworkPortKind.SolidInput ||
                                           Kind == StorageNetworkPortKind.SolidOutput;

        public bool IsInput => Kind == StorageNetworkPortKind.SolidInput ||
                               Kind == StorageNetworkPortKind.LiquidInput ||
                               Kind == StorageNetworkPortKind.GasInput ||
                               Kind == StorageNetworkPortKind.PowerInput;

        public void Configure(StorageNetworkPortKind kind)
        {
            this.kind = kind;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            kind = InferKindFromPrefabId(kind);
            EnsurePortAutomationComponents();
            StorageSceneRegistry.Register(gameObject);
        }

        protected override void OnCleanUp()
        {
            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        private StorageNetworkPortKind InferKindFromPrefabId(StorageNetworkPortKind fallback)
        {
            KPrefabID prefabId = GetComponent<KPrefabID>();
            string prefabTag = prefabId != null ? prefabId.PrefabTag.Name : string.Empty;
            switch (prefabTag)
            {
                case StorageNetworkSolidInputPortConfig.ID:
                    return StorageNetworkPortKind.SolidInput;
                case StorageNetworkSolidOutputPortConfig.ID:
                    return StorageNetworkPortKind.SolidOutput;
                case StorageNetworkLiquidInputPortConfig.ID:
                    return StorageNetworkPortKind.LiquidInput;
                case StorageNetworkLiquidOutputPortConfig.ID:
                    return StorageNetworkPortKind.LiquidOutput;
                case StorageNetworkGasInputPortConfig.ID:
                    return StorageNetworkPortKind.GasInput;
                case StorageNetworkGasOutputPortConfig.ID:
                    return StorageNetworkPortKind.GasOutput;
                case StorageNetworkPowerInputPortConfig.ID:
                    return StorageNetworkPortKind.PowerInput;
                case StorageNetworkPowerOutputPortConfig.ID:
                    return StorageNetworkPortKind.PowerOutput;
                default:
                    return fallback;
            }
        }

        private void EnsurePortAutomationComponents()
        {
            gameObject.AddOrGet<StorageNetworkPortStatusSilencer>();
            gameObject.AddOrGet<StorageNetworkPortStatusReporter>();
            ConfigurePortFilter();
            ConfigureManualOperation();
            ConfigurePortStorage();

            if (Kind == StorageNetworkPortKind.SolidInput)
            {
                gameObject.AddOrGet<StorageNetworkPortManualFetch>();
            }

            if (IsInput && Kind != StorageNetworkPortKind.PowerInput)
            {
                gameObject.AddOrGet<StorageNetworkStorageConnector>();
            }
            else if (!IsInput && Kind != StorageNetworkPortKind.PowerOutput)
            {
                gameObject.AddOrGet<StorageNetworkPortRequester>();
            }
        }

        private void ConfigurePortFilter()
        {
            TreeFilterable filterable = GetComponent<TreeFilterable>();
            if (filterable == null)
            {
                return;
            }

            StorageNetworkFilterConfigurator.Configure(filterable);
        }

        private void ConfigurePortStorage()
        {
            Storage storage = StorageNetworkPortPickupBufferStorage.FindMainStorage(gameObject);
            if (storage == null)
            {
                return;
            }

            storage.allowItemRemoval = false;
            storage.fetchCategory = Storage.FetchCategory.Building;
            StorageNetworkPortPickupState.SyncStoredItems(storage, false);

            StorageNetworkPortPickupBufferStorage pickupBuffer = GetComponent<StorageNetworkPortPickupBufferStorage>();
            if (Kind == StorageNetworkPortKind.SolidOutput)
            {
                pickupBuffer = gameObject.AddOrGet<StorageNetworkPortPickupBufferStorage>();
            }

            pickupBuffer?.OnManualOperationChanged(IsManualDuplicantOperationAllowed());
            StorageNetworkFetchBridgeCache.ClearPortLookups();
            StorageSceneRegistry.RefreshSolidOutputPickupBuffer(gameObject);
        }

        private void ConfigureManualOperation()
        {
            if (!SupportsManualDuplicantOperation)
            {
                return;
            }

            gameObject.AddOrGet<Automatable>();
        }

        public bool SupportsManualDuplicantOperation => Kind == StorageNetworkPortKind.SolidInput ||
                                                        Kind == StorageNetworkPortKind.SolidOutput;

        public bool IsManualDuplicantOperationAllowed()
        {
            if (!SupportsManualDuplicantOperation)
            {
                return false;
            }

            Automatable automatable = GetComponent<Automatable>();
            return automatable == null || !automatable.GetAutomationOnly();
        }

        public void SetManualDuplicantOperationAllowed(bool allowed)
        {
            if (!SupportsManualDuplicantOperation)
            {
                return;
            }

            Automatable automatable = gameObject.AddOrGet<Automatable>();
            automatable.SetAutomationOnly(!allowed);
            ConfigurePortStorage();
        }
    }
}
