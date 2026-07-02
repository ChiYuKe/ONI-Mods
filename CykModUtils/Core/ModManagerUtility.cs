using System.Linq;

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
            if (string.IsNullOrWhiteSpace(staticId) || Global.Instance == null || Global.Instance.modManager == null)
            {
                return false;
            }

            var mods = Global.Instance.modManager.mods;
            if (mods == null)
            {
                return false;
            }

            if (logAllMods)
            {
                foreach (var mod in mods.Where(mod => mod != null))
                {
                    Log.Info("Mod ID: " + mod.staticID + ", active: " + mod.IsActive());
                }
            }

            return mods.Any(mod => mod != null && mod.staticID == staticId && mod.IsActive());
        }
    }
}
