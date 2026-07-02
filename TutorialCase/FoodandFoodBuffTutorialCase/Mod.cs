using System;
using System.IO;
using System.Reflection;
using FoodandFoodBuffTutorialCase.Effects;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace FoodandFoodBuffTutorialCase
{
    public sealed class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[FoodandFoodBuffTutorialCase] Loaded");
        }

        [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
        private static class DbInitializePatch
        {
            private static void Postfix(Db __instance)
            {
                ModEffects.RegisterAll(__instance);
                Debug.Log("[FoodandFoodBuffTutorialCase] Db initialized");
            }
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
        private static class LocalizationInitializePatch
        {
            private static void Postfix()
            {
                Localize(typeof(STRINGS));
            }
        }

        public static void Localize(Type root, bool generateTemplate = false)
        {
            Localization.RegisterForTranslation(root);
            LocString.CreateLocStringKeys(root, null);

            string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string translationsDirectory = Path.Combine(modDirectory, "translations");
            Localization.Locale locale = Localization.GetLocale();
            string localeCode = locale != null ? locale.Code : "en";
            string poFile = Path.Combine(translationsDirectory, localeCode + ".po");

            if (File.Exists(poFile))
            {
                Localization.OverloadStrings(Localization.LoadStringsFile(poFile, false));
            }

            if (generateTemplate)
            {
                Localization.GenerateStringsTemplate(root, translationsDirectory);
            }
        }
    }
}
