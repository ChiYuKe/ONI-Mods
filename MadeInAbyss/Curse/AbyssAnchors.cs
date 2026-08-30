using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 深渊“地表”锚点：优先使用各世界的打印舱（探窟营地）所在高度作为地表；
    /// 没有打印舱的星球（群星模式下的小行星等）自动扫描其最高的非中子岩
    /// 实心格作为地表。深度（米）= 地表高度 - 目标高度，1 格 = 1 米。
    /// </summary>
    public static class AbyssAnchors
    {
        private class AnchorEntry
        {
            public float surfaceY;
            public float lastRefreshTime;

            /// <summary>锚点是否来自地表扫描（无打印舱的星球）。</summary>
            public bool scanned;
        }

        private static readonly Dictionary<int, AnchorEntry> anchors = new Dictionary<int, AnchorEntry>();

        private const float RefreshIntervalS = 600f;

        /// <summary>
        /// 获取指定世界的地表高度；无法确定（无打印舱且扫描不到地表）时返回 NaN。
        /// </summary>
        public static float GetSurfaceY(int worldId)
        {
            if (anchors.TryGetValue(worldId, out AnchorEntry entry) &&
                Time.time - entry.lastRefreshTime < RefreshIntervalS)
            {
                return entry.surfaceY;
            }

            float surfaceY = FindSurfaceY(worldId, out bool scanned);
            if (float.IsNaN(surfaceY))
            {
                anchors.Remove(worldId);
                return surfaceY;
            }

            anchors[worldId] = new AnchorEntry { surfaceY = surfaceY, lastRefreshTime = Time.time, scanned = scanned };
            return surfaceY;
        }

        /// <summary>该世界的锚点是否来自地表扫描（无打印舱的星球）。</summary>
        public static bool IsScannedAnchor(int worldId)
        {
            return anchors.TryGetValue(worldId, out AnchorEntry entry) && entry.scanned;
        }

        private static float FindSurfaceY(int worldId, out bool scanned)
        {
            scanned = false;
            // 优先使用打印舱。
            List<Telepad> telepads = Components.Telepads.GetWorldItems(worldId);
            if (telepads != null && telepads.Count > 0)
                return telepads[0].transform.GetPosition().y;

            // 无打印舱的星球：从世界顶部向下扫描，最高的非中子岩实心行即为地表。
            scanned = true;
            WorldContainer world = ClusterManager.Instance.GetWorld(worldId);
            if (world == null)
                return float.NaN;

            Vector2I offset = world.WorldOffset;
            Vector2I size = world.WorldSize;
            for (int row = size.y - 1; row >= 0; row--)
            {
                int y = offset.y + row;
                for (int col = 0; col < size.x; col++)
                {
                    int cell = Grid.XYToCell(offset.x + col, y);
                    if (!Grid.IsValidCell(cell) || !Grid.Solid[cell])
                        continue;
                    if (Grid.Element[cell].id == SimHashes.Unobtanium)
                        continue;
                    if (row == size.y - 1)
                        Debug.Log($"[MadeInAbyss] 世界 {worldId} 无打印舱，已扫描地表高度（{y}m）作为深渊锚点");
                    return y;
                }
            }

            Debug.LogWarning($"[MadeInAbyss] 世界 {worldId} 扫描不到任何实心地表，该世界不启用深渊系统");
            return float.NaN;
        }

        /// <summary>计算指定世界的某高度对应的深渊深度（米）；无锚点时返回 NaN。</summary>
        public static float GetDepthM(int worldId, float y)
        {
            float surfaceY = GetSurfaceY(worldId);
            if (float.IsNaN(surfaceY))
                return float.NaN;
            return surfaceY - y;
        }
    }
}
