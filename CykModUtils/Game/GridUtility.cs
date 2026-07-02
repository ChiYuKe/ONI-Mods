using System;
using UnityEngine;

namespace CykModUtils.Game
{
    /// <summary>
    /// ONI 网格坐标和格子检测相关辅助方法。
    /// </summary>
    public static class GridUtility
    {
        /// <summary>
        /// 安全地把 cell 转换为 x/y 坐标。
        /// </summary>
        /// <param name="cell">ONI Grid cell。</param>
        /// <param name="x">输出的 X 坐标。</param>
        /// <param name="y">输出的 Y 坐标。</param>
        /// <returns>cell 有效时返回 true。</returns>
        public static bool TryCellToXY(int cell, out int x, out int y)
        {
            x = 0;
            y = 0;
            if (!Grid.IsValidCell(cell))
            {
                return false;
            }

            Grid.CellToXY(cell, out x, out y);
            return true;
        }

        /// <summary>
        /// 使用 RangeVisualizer 的阻挡规则检测两个 cell 之间视线是否被阻挡。
        /// </summary>
        /// <param name="startCell">起点 cell。</param>
        /// <param name="targetCell">目标 cell。</param>
        /// <param name="rangeVisualizer">提供 BlockingCb / BlockingVisibleCb 的范围可视化器。</param>
        /// <returns>视线被阻挡时返回 true；参数无效时返回 false。</returns>
        public static bool IsLineOfSightBlocked(int startCell, int targetCell, RangeVisualizer rangeVisualizer)
        {
            if (rangeVisualizer == null || !TryCellToXY(startCell, out int startX, out int startY) || !TryCellToXY(targetCell, out int targetX, out int targetY))
            {
                return false;
            }

            Func<int, bool> visibleBlocking = rangeVisualizer.BlockingVisibleCb ?? (_ => rangeVisualizer.BlockingTileVisible);
            return Grid.TestLineOfSight(startX, startY, targetX, targetY, rangeVisualizer.BlockingCb, visibleBlocking, true);
        }

        /// <summary>
        /// 获取对象当前所在 cell。
        /// </summary>
        /// <param name="target">目标 GameObject。</param>
        /// <returns>对象为空时返回 Grid.InvalidCell。</returns>
        public static int GetCell(GameObject target)
        {
            return target == null ? Grid.InvalidCell : Grid.PosToCell(target);
        }
    }
}
