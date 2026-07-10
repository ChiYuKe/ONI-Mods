using Database;
using HarmonyLib;
using KMod;
using KModTool;
using MusicBox.Building;
using System;
using System.IO;
using System.Reflection;

namespace MusicBox
{
    public static class ModEntry
    {
        public class Loader : UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                base.OnLoad(harmony);
                ModAssets.LoadAll();
            }
        }

        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        private static class RegisterBuildingPatch
        {
            public static void Prefix()
            {
                ModUtil.AddBuildingToPlanScreen("Automation", MusicBoxConfig.ID);
                KModStringUtils.Add_New_BuildStrings(
                    MusicBoxConfig.ID,
                    STRINGS.BUILDINGS.PREFABS.MUSICBOX.NAME,
                    STRINGS.BUILDINGS.PREFABS.MUSICBOX.DESC,
                    STRINGS.BUILDINGS.PREFABS.MUSICBOX.EFFECT);
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        private static class RegisterTechPatch
        {
            public static void Postfix()
            {
                Db.Get().Techs.Get("GenericSensors").unlockedItemIDs.Add(MusicBoxConfig.ID);
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
