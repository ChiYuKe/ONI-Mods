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
        }
    }
}
