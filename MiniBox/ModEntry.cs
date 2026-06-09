using HarmonyLib;
using KMod;
using PeterHan.PLib.Options;
using System;
using System.IO;
using System.Reflection;
using static MiniBox.ModOptions;

namespace MiniBox
{
    public static class ModEntry
    {
        public class Loader : UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                base.OnLoad(harmony);
                new POptions().RegisterOptions(this, typeof(Settings));
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        private class LocalizationPatch
        {
            public static void Postfix()
            {
                Localize(typeof(STRINGS));
            }
        }

        public static void Localize(Type root)
        {
            ModUtil.RegisterForTranslation(root);

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string modName = executingAssembly.GetName().Name;
            string translationsPath = Path.Combine(Path.GetDirectoryName(executingAssembly.Location), "translations");
            string languageCode = Localization.GetLocale()?.Code ?? "en";
            string poFilePath = Path.Combine(translationsPath, languageCode + ".po");

            LoadLocalizationFile(modName, poFilePath);
            LocString.CreateLocStringKeys(root, "");
        }

        private static void LoadLocalizationFile(string modName, string poFilePath)
        {
            try
            {
                if (!File.Exists(poFilePath))
                {
                    Debug.LogWarning($"{modName}: 未在: {poFilePath} 找到本地化文件");
                    return;
                }

                var localizedStrings = Localization.LoadStringsFile(poFilePath, false);
                Localization.OverloadStrings(localizedStrings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{modName}: 无法加载本地化文件. Error: {ex.Message}");
            }
        }
    }
}




