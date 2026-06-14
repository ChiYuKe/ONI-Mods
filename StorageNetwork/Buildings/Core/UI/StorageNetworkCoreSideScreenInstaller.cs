namespace StorageNetwork.UI.Installers
{
    internal static class StorageNetworkCoreSideScreenInstaller
    {
        public static void Install(DetailsScreen detailsScreen)
        {
            // Disabled: the core overview is available in the main Storage Network window.
            // Keeping this installer as a no-op avoids injecting a fragile custom DetailsScreen prefab.
        }
    }
}
