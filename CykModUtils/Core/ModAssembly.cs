using System;
using System.IO;
using System.Reflection;

namespace CykModUtils.Core
{
    /// <summary>
    /// 提供和 Mod 程序集位置相关的常用信息。
    /// </summary>
    public static class ModAssembly
    {
        /// <summary>
        /// 当前工具库自身的程序集。注意它不是调用方 Mod 的程序集。
        /// </summary>
        public static Assembly UtilityAssembly => typeof(ModAssembly).Assembly;

        /// <summary>
        /// 获取指定程序集所在目录。
        /// </summary>
        /// <param name="assembly">目标程序集；为 null 时使用调用方程序集。</param>
        /// <returns>程序集 DLL 所在目录；无法解析时返回空字符串。</returns>
        public static string GetDirectory(Assembly assembly)
        {
            assembly = assembly ?? Assembly.GetCallingAssembly();
            string location = assembly.Location;
            return string.IsNullOrEmpty(location) ? string.Empty : Path.GetDirectoryName(location);
        }

        /// <summary>
        /// 通过锚点类型获取其所属程序集所在目录。
        /// </summary>
        /// <param name="anchorType">Mod 内任意类型，通常传入 typeof(ModEntry) 或 typeof(STRINGS)。</param>
        /// <returns>锚点类型所属程序集的 DLL 所在目录。</returns>
        public static string GetDirectory(Type anchorType)
        {
            return GetDirectory(anchorType != null ? anchorType.Assembly : Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// 获取指定程序集目录下的 assets 文件夹路径。
        /// </summary>
        /// <param name="assembly">目标程序集。</param>
        /// <returns>assets 文件夹路径。</returns>
        public static string GetAssetsDirectory(Assembly assembly)
        {
            return Path.Combine(GetDirectory(assembly), "assets");
        }

        /// <summary>
        /// 通过锚点类型获取其所属 Mod 的 assets 文件夹路径。
        /// </summary>
        /// <param name="anchorType">Mod 内任意类型。</param>
        /// <returns>assets 文件夹路径。</returns>
        public static string GetAssetsDirectory(Type anchorType)
        {
            return Path.Combine(GetDirectory(anchorType), "assets");
        }

        /// <summary>
        /// 获取程序集名称。
        /// </summary>
        /// <param name="assembly">目标程序集；为 null 时使用调用方程序集。</param>
        /// <returns>程序集名称。</returns>
        public static string GetAssemblyName(Assembly assembly)
        {
            return (assembly ?? Assembly.GetCallingAssembly()).GetName().Name;
        }

        /// <summary>
        /// 获取程序集版本号字符串。
        /// </summary>
        /// <param name="assembly">目标程序集；为 null 时使用调用方程序集。</param>
        /// <returns>程序集版本号。</returns>
        public static string GetAssemblyVersion(Assembly assembly)
        {
            return (assembly ?? Assembly.GetCallingAssembly()).GetName().Version.ToString();
        }
    }
}
