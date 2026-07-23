using KSerialization;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkSolidServerManualOperation : KMonoBehaviour, ISim1000ms
    {
        [Serialize]
        private bool initialized;

        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private Automatable automatable = null;

        private FilteredStorage filteredStorage;
        private bool manualFetchEnabled;
        private bool manualFetchInitialized;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (!initialized && automatable != null)
            {
                automatable.SetAutomationOnly(false);
                initialized = true;
            }

            SyncManualOperation();
        }

        protected override void OnCleanUp()
        {
            CleanupFilteredStorage();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            SyncManualOperation();
        }

        private void SyncManualOperation()
        {
            if (storage == null)
            {
                return;
            }

            bool allowManualOperation = automatable == null || !automatable.GetAutomationOnly();
            storage.allowItemRemoval = allowManualOperation;
            storage.allowUIItemRemoval = allowManualOperation;
            storage.fetchCategory = allowManualOperation
                ? Storage.FetchCategory.GeneralStorage
                : Storage.FetchCategory.Building;

            if (!manualFetchInitialized || manualFetchEnabled != allowManualOperation)
            {
                manualFetchInitialized = true;
                manualFetchEnabled = allowManualOperation;
                if (allowManualOperation)
                {
                    EnsureFilteredStorage();
                    filteredStorage.FilterChanged();
                }
                else
                {
                    CleanupFilteredStorage();
                }
            }
        }

        private void EnsureFilteredStorage()
        {
            if (filteredStorage != null)
            {
                return;
            }

            filteredStorage = new FilteredStorage(
                this,
                null,
                null,
                false,
                Db.Get().ChoreTypes.StorageFetch);
        }

        private void CleanupFilteredStorage()
        {
            if (filteredStorage == null)
            {
                return;
            }

            filteredStorage.CleanUp();
            filteredStorage = null;
        }
    }
}
