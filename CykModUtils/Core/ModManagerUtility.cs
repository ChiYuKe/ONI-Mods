using System;
using System.Collections.Generic;

namespace CykModUtils.Core
{
    /// <summary>
    /// 读取 ONI 当前已加载 Mod 状态的辅助方法。
    /// </summary>
    public static class ModManagerUtility
    {
        /// <summary>
        /// 判断指定 staticID 的 Mod 是否已经加载并处于启用状态。
        /// </summary>
        /// <param name="staticId">Klei Mod 的 staticID。</param>
        /// <param name="logAllMods">为 true 时会把当前 Mod 列表和启用状态写入日志，便于调试依赖检测。</param>
        /// <returns>目标 Mod 已加载且启用时返回 true。</returns>
        public static bool IsModLoaded(string staticId, bool logAllMods = false)
        {
            if (string.IsNullOrWhiteSpace(staticId) ||
                !TryGetMods(out List<KMod.Mod> mods))
            {
                return false;
            }

            KMod.Mod found = null;
            foreach (KMod.Mod mod in mods)
            {
                if (mod == null)
                {
                    continue;
                }

                bool active = mod.IsActive();
                if (logAllMods)
                {
                    Log.Info("Mod ID: " + mod.staticID + ", active: " + active);
                }

                if (found == null &&
                    active &&
                    string.Equals(mod.staticID, staticId, StringComparison.Ordinal))
                {
                    found = mod;
                }
            }

            return found != null;
        }

        /// <summary>
        /// 查找指定 staticID 的 Mod。
        /// </summary>
        /// <param name="staticId">Klei Mod 的 staticID。</param>
        /// <param name="mod">找到的 Mod。</param>
        /// <param name="activeOnly">是否只接受当前启用的 Mod。</param>
        public static bool TryGetMod(string staticId, out KMod.Mod mod, bool activeOnly = true)
        {
            mod = null;
            if (string.IsNullOrWhiteSpace(staticId) || !TryGetMods(out List<KMod.Mod> mods))
            {
                return false;
            }

            foreach (KMod.Mod candidate in mods)
            {
                if (candidate != null &&
                    string.Equals(candidate.staticID, staticId, StringComparison.Ordinal) &&
                    (!activeOnly || candidate.IsActive()))
                {
                    mod = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取当前 Mod 列表的快照。
        /// </summary>
        public static IReadOnlyList<KMod.Mod> GetMods(bool activeOnly = false)
        {
            var result = new List<KMod.Mod>();
            if (!TryGetMods(out List<KMod.Mod> mods))
            {
                return result;
            }

            foreach (KMod.Mod mod in mods)
            {
                if (mod != null && (!activeOnly || mod.IsActive()))
                {
                    result.Add(mod);
                }
            }

            return result;
        }

        private static bool TryGetMods(out List<KMod.Mod> mods)
        {
            mods = Global.Instance?.modManager?.mods;
            return mods != null;
        }
    }
}
