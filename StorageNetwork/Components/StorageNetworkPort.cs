using KSerialization;
using StorageNetwork.Buildings;

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
    }
}
