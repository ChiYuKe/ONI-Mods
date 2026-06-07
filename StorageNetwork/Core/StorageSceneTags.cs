using StorageNetwork.API;

namespace StorageNetwork.Core
{
    public static class StorageSceneTags
    {
        public const string ModStorageTagName = StorageNetworkTags.ModStorageTagName;
        public const string ShowSettingsButtonTagName = StorageNetworkTags.ShowSettingsButtonTagName;
        public const string SceneStorageBoxTagName = "StorageNetwork_SceneStorageBox";

        public static readonly Tag ModStorage = StorageNetworkTags.ModStorage;
        public static readonly Tag ShowSettingsButton = StorageNetworkTags.ShowSettingsButton;
        public static readonly Tag SceneStorageBox = new Tag(SceneStorageBoxTagName);
    }
}
