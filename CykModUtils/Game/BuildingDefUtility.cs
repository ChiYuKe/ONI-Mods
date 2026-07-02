using System;
using System.Collections.Generic;
using UnityEngine;

namespace CykModUtils.Game
{
    /// <summary>
    /// BuildingDef 常用字段和占格逻辑的辅助方法。
    /// </summary>
    public static class BuildingDefUtility
    {
        /// <summary>
        /// 获取建筑在指定朝向下占用的所有 cell。
        /// </summary>
        /// <param name="def">建筑定义。</param>
        /// <param name="baseCell">建筑基准 cell。</param>
        /// <param name="orientation">建筑朝向。</param>
        /// <returns>占用 cell 列表；参数无效时返回空列表。</returns>
        public static List<int> GetOccupiedCells(BuildingDef def, int baseCell, Orientation orientation = Orientation.Neutral)
        {
            var cells = new List<int>();
            if (def == null || !Grid.IsValidCell(baseCell))
            {
                return cells;
            }

            CellOffset[] offsets = def.PlacementOffsets;
            if (offsets == null || offsets.Length == 0)
            {
                def.GenerateOffsets();
                offsets = def.PlacementOffsets;
            }

            foreach (CellOffset offset in offsets)
            {
                CellOffset rotated = Rotatable.GetRotatedCellOffset(offset, orientation);
                if (Grid.IsCellOffsetValid(baseCell, rotated))
                {
                    cells.Add(Grid.OffsetCell(baseCell, rotated));
                }
            }

            return cells;
        }

        /// <summary>
        /// 判断建筑在指定 cell 和朝向下所有占格是否满足自定义条件。
        /// </summary>
        /// <param name="def">建筑定义。</param>
        /// <param name="baseCell">建筑基准 cell。</param>
        /// <param name="predicate">每个 cell 必须满足的条件。</param>
        /// <param name="orientation">建筑朝向。</param>
        /// <returns>所有占格都满足条件时返回 true。</returns>
        public static bool AreAllOccupiedCellsValid(BuildingDef def, int baseCell, Func<int, bool> predicate, Orientation orientation = Orientation.Neutral)
        {
            if (def == null || predicate == null)
            {
                return false;
            }

            foreach (int cell in GetOccupiedCells(def, baseCell, orientation))
            {
                if (!predicate(cell))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断建筑定义是否需要任意输入或输出端口。
        /// </summary>
        /// <param name="def">建筑定义。</param>
        /// <returns>需要电力、管道、运输轨道或高能粒子端口时返回 true。</returns>
        public static bool RequiresAnyPort(BuildingDef def)
        {
            return def != null && (
                def.CheckRequiresPowerInput() ||
                def.CheckRequiresPowerOutput() ||
                def.CheckRequiresGasInput() ||
                def.CheckRequiresGasOutput() ||
                def.CheckRequiresLiquidInput() ||
                def.CheckRequiresLiquidOutput() ||
                def.CheckRequiresSolidInput() ||
                def.CheckRequiresSolidOutput() ||
                def.CheckRequiresHighEnergyParticleInput() ||
                def.CheckRequiresHighEnergyParticleOutput());
        }

        /// <summary>
        /// 获取建筑默认材料列表。
        /// </summary>
        /// <param name="def">建筑定义。</param>
        /// <returns>默认材料 Tag 列表；无可用材料时返回空列表。</returns>
        public static List<Tag> GetDefaultMaterials(BuildingDef def)
        {
            return def == null ? new List<Tag>() : def.DefaultElements();
        }

        /// <summary>
        /// 尝试从场景对象获取 BuildingDef。
        /// </summary>
        /// <param name="target">建筑对象。</param>
        /// <param name="def">找到的建筑定义。</param>
        /// <returns>找到 Building 或 BuildingComplete 定义时返回 true。</returns>
        public static bool TryGetDef(GameObject target, out BuildingDef def)
        {
            def = null;
            if (target == null)
            {
                return false;
            }

            Building building = target.GetComponent<Building>();
            if (building != null)
            {
                def = building.Def;
                return def != null;
            }

            BuildingComplete complete = target.GetComponent<BuildingComplete>();
            if (complete != null)
            {
                def = complete.Def;
                return def != null;
            }

            return false;
        }
    }
}
