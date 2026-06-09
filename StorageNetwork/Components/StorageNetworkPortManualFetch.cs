namespace StorageNetwork.Components
{
    /// <summary>
    /// Enables duplicants to deliver filtered materials to solid input ports using the vanilla storage fetch system.
    /// </summary>
    public sealed class StorageNetworkPortManualFetch : KMonoBehaviour
    {
        private FilteredStorage filteredStorage;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            filteredStorage = new FilteredStorage(this, null, null, false, Db.Get().ChoreTypes.StorageFetch);
            filteredStorage.SetHasMeter(false);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            filteredStorage?.FilterChanged();
        }

        protected override void OnCleanUp()
        {
            filteredStorage?.CleanUp();
            base.OnCleanUp();
        }
    }
}
