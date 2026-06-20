namespace StorageNetwork.Services
{
    internal static class StorageNetworkFilterConfigurator
    {
        public static void Configure(TreeFilterable filterable)
        {
            if (filterable == null)
            {
                return;
            }

            filterable.autoSelectStoredOnLoad = false;
            filterable.preventAutoAddOnDiscovery = true;
            filterable.dropIncorrectOnFilterChange = false;
            filterable.filterByStorageCategoriesOnSpawn = false;
            filterable.copySettingsEnabled = false;
            StorageNetwork.Components.StorageNetworkFilterState.Ensure(filterable);
            StorageNetworkFilterSelectionNormalizer.NormalizeExistingSelection(filterable);
        }
    }
}
