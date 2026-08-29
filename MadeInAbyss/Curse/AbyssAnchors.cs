using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 深渊“地表”锚点：以各世界的打印舱（探窟营地）所在高度作为地表。
    /// 深度（米）= 地表高度 - 目标高度，1 格 = 1 米。
    /// </summary>
    public static class AbyssAnchors
    {
        private class AnchorEntry
        {
            public float surfaceY;
            public float lastRefreshTime;
        }

        private static readonly Dictionary<int, AnchorEntry> anchors = new Dictionary<int, AnchorEntry>();

        private const float RefreshIntervalS = 600f;

        /// <summary>
        /// 获取指定世界的地表高度；该世界没有打印舱时返回 NaN。
        /// </summary>
        public static float GetSurfaceY(int worldId)
        {
            if (anchors.TryGetValue(worldId, out AnchorEntry entry) &&
                Time.time - entry.lastRefreshTime < RefreshIntervalS)
            {
                return entry.surfaceY;
            }

            List<Telepad> telepads = Components.Telepads.GetWorldItems(worldId);
            if (telepads == null || telepads.Count == 0)
            {
                anchors.Remove(worldId);
                return float.NaN;
            }

            float surfaceY = telepads[0].transform.GetPosition().y;
            anchors[worldId] = new AnchorEntry { surfaceY = surfaceY, lastRefreshTime = Time.time };
            return surfaceY;
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
