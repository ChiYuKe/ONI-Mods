using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace NewElementRegistration
{
    [HarmonyPatch(typeof(Localization), "Initialize")]
    public static class LocalizationPatch
    {
        public static void Postfix()
        {
            Type root = typeof(STRINGS);
            Localization.RegisterForTranslation(root);

            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(modPath))
            {
                Localization.GenerateStringsTemplate(root, Path.Combine(modPath, "translations"));
            }

            LocString.CreateLocStringKeys(root, string.Empty);
        }
    }
}
