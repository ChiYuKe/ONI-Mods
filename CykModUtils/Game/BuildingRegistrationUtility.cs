using System;
using System.Collections.Generic;
using CykModUtils.Core;
using TUNING;

namespace CykModUtils.Game
{
    /// <summary>
    /// 注册建筑到建造菜单和科技树的常用操作。
    /// </summary>
    public static class BuildingRegistrationUtility
    {
        /// <summary>
        /// 将建筑加入建造菜单。相同建筑已经存在时不会重复加入。
        /// </summary>
        /// <returns>成功加入或原本已经存在时返回 true。</returns>
        public static bool AddToPlanScreen(
            HashedString category,
            string buildingId,
            string subcategoryId = "uncategorized",
            string relativeBuildingId = null,
            ModUtil.BuildingOrdering ordering = ModUtil.BuildingOrdering.After,
            ModLogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(buildingId))
            {
                logger?.Warning("Cannot add a building with an empty ID to the plan screen.");
                return false;
            }

            int categoryIndex = BUILDINGS.PLANORDER.FindIndex(
                info => info.category == category);
            if (categoryIndex < 0)
            {
                logger?.Warning(
                    "Cannot add building '" + buildingId +
                    "': plan category '" + category + "' does not exist.");
                return false;
            }

            List<KeyValuePair<string, string>> entries =
                BUILDINGS.PLANORDER[categoryIndex].buildingAndSubcategoryData;
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Key, buildingId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            ModUtil.AddBuildingToPlanScreen(
                category,
                buildingId,
                string.IsNullOrWhiteSpace(subcategoryId)
                    ? "uncategorized"
                    : subcategoryId,
                relativeBuildingId,
                ordering);
            return true;
        }

        /// <summary>
        /// 将建筑或其他科技物品加入指定科技。重复调用不会产生重复项。
        /// </summary>
        public static bool AddToTech(
            string techId,
            string itemId,
            ModLogger logger = null)
        {
            if (!TryGetTechAndItem(techId, itemId, logger, out Tech tech))
            {
                return false;
            }

            if (!tech.unlockedItemIDs.Contains(itemId))
            {
                tech.AddUnlockedItemIDs(itemId);
            }

            return true;
        }

        /// <summary>
        /// 从指定科技中移除建筑或其他科技物品。目标不存在时也视为成功。
        /// </summary>
        public static bool RemoveFromTech(
            string techId,
            string itemId,
            ModLogger logger = null)
        {
            if (!TryGetTechAndItem(techId, itemId, logger, out Tech tech))
            {
                return false;
            }

            tech.unlockedItemIDs.Remove(itemId);
            return true;
        }

        /// <summary>
        /// 一次完成建造菜单和科技树注册。
        /// </summary>
        public static bool AddToPlanAndTech(
            HashedString category,
            string techId,
            string buildingId,
            string subcategoryId = "uncategorized",
            string relativeBuildingId = null,
            ModUtil.BuildingOrdering ordering = ModUtil.BuildingOrdering.After,
            ModLogger logger = null)
        {
            bool planAdded = AddToPlanScreen(
                category,
                buildingId,
                subcategoryId,
                relativeBuildingId,
                ordering,
                logger);
            bool techAdded = AddToTech(techId, buildingId, logger);
            return planAdded && techAdded;
        }

        private static bool TryGetTechAndItem(
            string techId,
            string itemId,
            ModLogger logger,
            out Tech tech)
        {
            tech = null;
            if (string.IsNullOrWhiteSpace(techId) ||
                string.IsNullOrWhiteSpace(itemId))
            {
                logger?.Warning("Tech ID and item ID cannot be empty.");
                return false;
            }

            Db database = Db.Get();
            tech = database?.Techs?.TryGet(techId);
            if (tech != null)
            {
                return true;
            }

            logger?.Warning("Tech '" + techId + "' does not exist.");
            return false;
        }
    }
}
