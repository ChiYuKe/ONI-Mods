using System;
using System.IO;
using System.Reflection;

namespace StorageNetwork.Core
{
    public static class StorageNetworkLocalization
    {
        private static string modPath;

        public static void SetModPath(string path)
        {
            modPath = path;
        }

        /// <summary>
        /// 注册并加载本地化。源代码中的 LocString 默认为中文，缺少中文 po 时不再写警告日志。
        /// </summary>
        public static void Translate(Type root, bool generateTemplate = false)
        {
            Localization.RegisterForTranslation(root);
            RegisterBuildMenuStrings();
            LoadStrings();
            LocString.CreateLocStringKeys(root, null);
            if (generateTemplate)
            {
                Localization.GenerateStringsTemplate(root, Path.Combine(GetModPath(), "translations"));
            }
        }

        private static void RegisterBuildMenuStrings()
        {
            Strings.Add("STRINGS.UI.NEWBUILDCATEGORIES.STORAGENETWORK.BUILDMENUTITLE", "储存网络");
        }

        private static void LoadStrings()
        {
            try
            {
                Localization.Locale locale = Localization.GetLocale();
                string localeCode = locale != null ? locale.Code : "en";
                if (localeCode.IsNullOrWhiteSpace())
                {
                    return;
                }

                string translationsPath = Path.Combine(GetModPath(), "translations");
                string poPath = Path.Combine(translationsPath, localeCode + ".po");
                if (!File.Exists(poPath))
                {
                    if (localeCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                    {
                        poPath = Path.Combine(translationsPath, "en.po");
                    }
                    else
                    {
                        Debug.Log("[StorageNetwork] Localization file not found for locale " + localeCode + "; using built-in strings.");
                        return;
                    }
                }

                if (!File.Exists(poPath))
                {
                    Debug.LogWarning("[StorageNetwork] Missing localization file: " + poPath);
                    return;
                }

                Debug.Log("[StorageNetwork] Loading localization file: " + poPath);
                Localization.OverloadStrings(Localization.LoadStringsFile(poPath, false));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[StorageNetwork] Failed to load localization: " + ex.Message);
            }
        }

        private static string GetModPath()
        {
            return !string.IsNullOrEmpty(modPath)
                ? modPath
                : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
