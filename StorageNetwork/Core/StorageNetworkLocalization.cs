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
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORK.NAME", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORK.NAME));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORK.DESC", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORK.DESC));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKCORE.NAME", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKCORE.NAME));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKCORE.DESC", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKCORE.DESC));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKSMALLSTORAGE.NAME", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKSMALLSTORAGE.NAME));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKSMALLSTORAGE.DESC", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKSMALLSTORAGE.DESC));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKMEDIUMSTORAGE.NAME", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKMEDIUMSTORAGE.NAME));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKMEDIUMSTORAGE.DESC", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKMEDIUMSTORAGE.DESC));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKORDERPRODUCTION.NAME", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKORDERPRODUCTION.NAME));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKORDERPRODUCTION.DESC", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKORDERPRODUCTION.DESC));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKLARGESTORAGE.NAME", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKLARGESTORAGE.NAME));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKLARGESTORAGE.DESC", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKLARGESTORAGE.DESC));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKRELAY.NAME", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKRELAY.NAME));
            Strings.Add("STRINGS.RESEARCH.TECHS.STORAGENETWORKRELAY.DESC", STRINGS.Get(STRINGS.RESEARCH.TECHS.STORAGENETWORKRELAY.DESC));
            Strings.Add("STRINGS.RESEARCH.TREES.TITLE_STORAGENETWORK", STRINGS.Get(STRINGS.RESEARCH.TREES.TITLE_STORAGENETWORK));
            Strings.Add("StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_REQUIRED", STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_REQUIRED));
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
                        return;
                    }
                }

                if (!File.Exists(poPath))
                {
                    Debug.LogWarning("[StorageNetwork] Missing localization file: " + poPath);
                    return;
                }

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
