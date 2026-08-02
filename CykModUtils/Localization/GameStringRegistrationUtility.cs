using System;

namespace CykModUtils.Localization
{
    /// <summary>
    /// 注册建筑、植物、食物和效果等常用游戏字符串。
    /// </summary>
    public static class GameStringRegistrationUtility
    {
        /// <summary>注册任意字符串键。</summary>
        public static void Register(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("String key cannot be empty.", nameof(key));
            }

            Strings.Add(key, value ?? string.Empty);
        }

        /// <summary>注册建筑的 NAME、DESC 和 EFFECT。</summary>
        public static void RegisterBuilding(
            string buildingId,
            string name,
            string description,
            string effect)
        {
            string root = "STRINGS.BUILDINGS.PREFABS." + NormalizeId(buildingId) + ".";
            Register(root + "NAME", name);
            Register(root + "DESC", description);
            Register(root + "EFFECT", effect);
        }

        /// <summary>注册植物的 NAME、DESC 和 DOMESTICATEDDESC。</summary>
        public static void RegisterPlant(
            string plantId,
            string name,
            string description,
            string domesticatedDescription,
            bool formatNameAsLink = true)
        {
            string root = "STRINGS.CREATURES.SPECIES." + NormalizeId(plantId) + ".";
            Register(root + "NAME", FormatName(name, plantId, formatNameAsLink));
            Register(root + "DESC", description);
            Register(root + "DOMESTICATEDDESC", domesticatedDescription);
        }

        /// <summary>注册植物种子的 NAME 和 DESC。</summary>
        public static void RegisterSeed(
            string seedId,
            string name,
            string description,
            bool formatNameAsLink = true)
        {
            string root = "STRINGS.CREATURES.SPECIES.SEEDS." + NormalizeId(seedId) + ".";
            Register(root + "NAME", FormatName(name, seedId, formatNameAsLink));
            Register(root + "DESC", description);
        }

        /// <summary>注册食物的 NAME、DESC，以及可选的 RECIPEDESC。</summary>
        public static void RegisterFood(
            string foodId,
            string name,
            string description,
            string recipeDescription = null,
            bool formatNameAsLink = true)
        {
            string root = "STRINGS.ITEMS.FOOD." + NormalizeId(foodId) + ".";
            Register(root + "NAME", FormatName(name, foodId, formatNameAsLink));
            Register(root + "DESC", description);
            if (recipeDescription != null)
            {
                Register(root + "RECIPEDESC", recipeDescription);
            }
        }

        /// <summary>
        /// 注册效果的 NAME、DESCRIPTION 和 TOOLTIP。
        /// DESCRIPTION 与 TOOLTIP 同时写入，可兼容仓库中两种读取方式。
        /// </summary>
        public static void RegisterEffect(
            string effectId,
            string name,
            string description,
            bool formatNameAsLink = true)
        {
            string root = "STRINGS.DUPLICANTS.MODIFIERS." + NormalizeId(effectId) + ".";
            Register(root + "NAME", FormatName(name, effectId, formatNameAsLink));
            Register(root + "DESCRIPTION", description);
            Register(root + "TOOLTIP", description);
        }

        /// <summary>注册死亡原因的 NAME 和 DESCRIPTION。</summary>
        public static void RegisterDeath(
            string deathId,
            string name,
            string description,
            bool formatNameAsLink = true)
        {
            string root = "STRINGS.DUPLICANTS.DEATHS." + NormalizeId(deathId) + ".";
            Register(root + "NAME", FormatName(name, deathId, formatNameAsLink));
            Register(root + "DESCRIPTION", description);
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Game content ID cannot be empty.", nameof(id));
            }

            return id.Trim().ToUpperInvariant();
        }

        private static string FormatName(string name, string linkId, bool formatAsLink)
        {
            string value = name ?? string.Empty;
            return formatAsLink
                ? global::STRINGS.UI.FormatAsLink(value, linkId)
                : value;
        }
    }
}
