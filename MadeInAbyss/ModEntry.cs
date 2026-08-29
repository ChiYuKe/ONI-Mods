using HarmonyLib;
using KMod;
using System;
using System.IO;
using System.Reflection;

namespace MadeInAbyss
{
    public static class ModEntry
    {
        public class Loader : UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                base.OnLoad(harmony);
                Debug.Log("[MadeInAbyss] 来自深渊 mod 已加载：深渊自有其法则，上升必然伴随代价。");
            }
        }

        /// <summary>
        /// 注册六层诅咒、笛级与生骸化效果。
        /// </summary>
        [HarmonyPatch(typeof(ModifierSet), "Initialize")]
        public static class ModifierSet_Initialize_Patch
        {
            public static void Postfix(ModifierSet __instance)
            {
                AbyssEffects.RegisterAll(__instance);
            }
        }

        /// <summary>
        /// 切换存档时重载该存档专属的配置文件。
        /// </summary>
        [HarmonyPatch(typeof(SaveLoader), "SetActiveSaveFilePath")]
        public static class SaveLoader_SetActiveSaveFilePath_Patch
        {
            public static void Postfix()
            {
                AbyssConfig.Reload();
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize_Patch
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
                    Debug.LogWarning($"{modName}: Localization file not found at: {poFilePath}");
                    return;
                }

                var localizedStrings = Localization.LoadStringsFile(poFilePath, false);
                Localization.OverloadStrings(localizedStrings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{modName}: Failed to load localization file. Error: {ex.Message}");
            }
        }
    }
}
