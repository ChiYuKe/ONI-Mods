using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkWorldUtility
    {
        public static int GetObjectWorldId(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return -1;
            }

            int worldId = gameObject.GetMyWorldId();
            if (worldId != byte.MaxValue && worldId >= 0)
            {
                return worldId;
            }

            int cell = Grid.PosToCell(gameObject);
            return Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
        }
    }
}
