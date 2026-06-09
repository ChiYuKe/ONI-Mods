namespace StorageNetwork.API
{
    public static class StorageNetworkTags
    {
        public const string ModStorageTagName = "StorageNetwork_ModStorage";
        public const string ServerStorageTagName = "StorageNetwork_ServerStorage";
        public const string ShowSettingsButtonTagName = "StorageNetwork_ShowSettingsButton";

        public static readonly Tag ModStorage = new Tag(ModStorageTagName);
        public static readonly Tag ServerStorage = new Tag(ServerStorageTagName);
        public static readonly Tag ShowSettingsButton = new Tag(ShowSettingsButtonTagName);
    }
}
