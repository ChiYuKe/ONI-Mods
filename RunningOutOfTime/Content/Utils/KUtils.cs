using System.IO;
using System.Linq;
using System.Reflection;


namespace CykUtils
{
    public class KUtils
    {
        public static string ModPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }


        public static string AssetsPath
        {
            get
            {
                return Path.Combine(KUtils.ModPath, "assets");
            }
        }


        public static string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public static string AssemblyName
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Name;
            }
        }




        /// <summary>
        /// 检查指定的 Mod 是否已加载并处于激活状态。
        /// </summary>
        /// <param name="modID">要检查的 Mod 的唯一 ID。</param>
        /// <param name="printMod">
        /// 如果为 <c>true</c>，会打印当前所有 Mod 的 ID 及激活状态，便于调试。
        /// </param>
        /// <returns>
        /// 如果指定 Mod 已加载且激活，则返回 <c>true</c>；否则返回 <c>false</c>。
        /// </returns>
        public static bool IsModLoaded(string modID, bool printMod = false)
        {
            var mods = Global.Instance.modManager.mods;

            if (printMod)
            {
                foreach (var mod in mods)
                {
                    LogUtil.Log($"当前Mod ID: {mod.staticID}, 是否已激活: {mod.IsActive()}");
                }
            }

            return mods.Any(mod => mod.staticID == modID && mod.IsActive());
        }






    }

}
