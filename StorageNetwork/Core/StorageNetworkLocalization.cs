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

        public static void Translate(Type root, bool generateTemplate = false)
        {
            Localization.RegisterForTranslation(root);
            LoadStrings();
            LocString.CreateLocStringKeys(root, null);
            if (generateTemplate)
            {
                Localization.GenerateStringsTemplate(root, Path.Combine(GetModPath(), "translations"));
            }
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
                if (!File.Exists(poPath) && localeCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    poPath = Path.Combine(translationsPath, "en.po");
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
