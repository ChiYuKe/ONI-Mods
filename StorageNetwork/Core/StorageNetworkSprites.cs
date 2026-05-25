using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkSprites
    {
        private const string OverviewIconName = "storage_network_overlay";

        public static void SetModPath(string path)
        {
            StorageNetworkSpriteLoader.SetModPath(path);
        }

        public static Sprite GetOverviewIcon()
        {
            return StorageNetworkSpriteLoader.GetSprite(OverviewIconName);
        }
    }
}
