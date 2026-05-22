using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace StorageNetwork.Patches
{
    [HarmonyPatch(typeof(Localization), "Initialize")]
    public static class LocalizationPatch
    {
        public static void Postfix()
        {
            Localize(typeof(StorageNetwork.STRINGS));
        }

        private static void Localize(Type root)
        {
            ModUtil.RegisterForTranslation(root);

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string translationsPath = Path.Combine(Path.GetDirectoryName(executingAssembly.Location), "translations");
            string languageCode = Localization.GetLocale()?.Code ?? "en";
            string poFilePath = Path.Combine(translationsPath, languageCode + ".po");

            if (File.Exists(poFilePath))
            {
                Localization.OverloadStrings(Localization.LoadStringsFile(poFilePath, false));
            }

            LocString.CreateLocStringKeys(root, string.Empty);
        }
    }
}
