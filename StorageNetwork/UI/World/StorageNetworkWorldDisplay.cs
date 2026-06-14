using static StorageNetwork.STRINGS;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkWorldDisplay
    {
        public static string GetWorldName(int worldId)
        {
            WorldContainer world = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(worldId) : null;
            if (world != null)
            {
                string name = world.GetProperName();
                if (!string.IsNullOrEmpty(name))
                {
                    return StorageNetworkTextFormatting.StripKleiLinkFormatting(name);
                }
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STARMAP_NAME_HINT);
        }

        public static bool IsWorldDiscovered(int worldId)
        {
            if (worldId < 0)
            {
                return false;
            }

            WorldContainer world = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(worldId) : null;
            return world != null && world.IsDiscovered;
        }

        public static Sprite GetWorldSprite(int worldId)
        {
            WorldContainer world = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(worldId) : null;
            ClusterGridEntity clusterEntity = world != null ? world.GetComponent<ClusterGridEntity>() : null;
            Sprite sprite = clusterEntity != null ? clusterEntity.GetUISprite() : null;
            return sprite != null ? sprite : Assets.GetSprite("unknown_far");
        }

        public static Sprite GetObjectWorldSprite(GameObject gameObject)
        {
            return GetWorldSprite(StorageNetworkWorldUtility.GetObjectWorldId(gameObject));
        }

        public static string GetObjectWorldName(GameObject gameObject)
        {
            return GetWorldName(StorageNetworkWorldUtility.GetObjectWorldId(gameObject));
        }
    }
}
