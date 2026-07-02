using System;
using System.IO;
using System.Reflection;
using CykModUtils.Core;

namespace CykModUtils.Localization
{
    /// <summary>
    /// 封装 ONI 本地化注册、LocString key 创建和 .po 文件加载流程。
    /// </summary>
    public static class LocalizationUtility
    {
        /// <summary>
        /// 注册指定 STRINGS 类型并从该类型所属 Mod 目录加载当前语言文件。
        /// </summary>
        /// <param name="stringsRoot">包含 LocString 字段的根类型，通常是 STRINGS。</param>
        /// <param name="generateTemplate">是否生成翻译模板文件。</param>
        public static void RegisterAndLoad(Type stringsRoot, bool generateTemplate = false)
        {
            if (stringsRoot == null)
            {
                Log.Warning("Cannot register localization for a null type.");
                return;
            }

            RegisterAndLoad(stringsRoot, ModAssembly.GetDirectory(stringsRoot), generateTemplate);
        }

        /// <summary>
        /// 注册指定 STRINGS 类型并加载当前语言文件。
        /// </summary>
        /// <typeparam name="TStrings">包含 LocString 字段的根类型，通常是 STRINGS。</typeparam>
        /// <param name="generateTemplate">是否生成翻译模板文件。</param>
        public static void RegisterAndLoad<TStrings>(bool generateTemplate = false)
        {
            RegisterAndLoad(typeof(TStrings), generateTemplate);
        }

        /// <summary>
        /// 注册指定 STRINGS 类型，并从指定 Mod 目录的 translations 文件夹加载当前语言文件。
        /// </summary>
        /// <param name="stringsRoot">包含 LocString 字段的根类型。</param>
        /// <param name="modDirectory">Mod 输出目录；方法会读取其下的 translations/&lt;locale&gt;.po。</param>
        /// <param name="generateTemplate">是否生成翻译模板文件。</param>
        public static void RegisterAndLoad(Type stringsRoot, string modDirectory, bool generateTemplate = false)
        {
            if (stringsRoot == null)
            {
                Log.Warning("Cannot register localization for a null type.");
                return;
            }

            global::Localization.RegisterForTranslation(stringsRoot);
            LocString.CreateLocStringKeys(stringsRoot, null);

            string translationsDirectory = Path.Combine(modDirectory ?? string.Empty, "translations");
            LoadLocaleFile(translationsDirectory);

            if (generateTemplate)
            {
                global::Localization.GenerateStringsTemplate(stringsRoot, translationsDirectory);
            }
        }

        /// <summary>
        /// 从指定程序集所在目录的 translations 文件夹加载当前语言文件。
        /// </summary>
        /// <param name="assembly">目标 Mod 程序集。</param>
        public static void LoadLocaleFileForAssembly(Assembly assembly)
        {
            LoadLocaleFile(Path.Combine(ModAssembly.GetDirectory(assembly), "translations"));
        }

        /// <summary>
        /// 从 translations 目录加载当前游戏语言对应的 .po 文件。
        /// </summary>
        /// <param name="translationsDirectory">translations 文件夹路径。</param>
        /// <returns>找到并加载语言文件时返回 true。</returns>
        public static bool LoadLocaleFile(string translationsDirectory)
        {
            global::Localization.Locale locale = global::Localization.GetLocale();
            string code = locale != null ? locale.Code : "en";
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(translationsDirectory))
            {
                return false;
            }

            string file = Path.Combine(translationsDirectory, code + ".po");
            if (!File.Exists(file))
            {
                return false;
            }

            global::Localization.OverloadStrings(global::Localization.LoadStringsFile(file, false));
            Log.Info("Loaded translation file: " + file);
            return true;
        }
    }
}
