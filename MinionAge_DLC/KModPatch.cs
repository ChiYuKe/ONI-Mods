using HarmonyLib;
using KMod;
using MinionAge;
using Newtonsoft.Json;
using PeterHan.PLib.Options;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;


namespace MinionAge_DLC
{
    public static class Main
    {
        public class Patch : UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                base.OnLoad(harmony);
                //harmony.PatchAll();
                ModAssets.LoadAll();// 提前注册到游戏当中
                
                Debug.Log("Mod - MinionAge_Dlc - 已加载并初始化。");
                //  PUtil.InitLibrary(true);
               
            }

            public static bool IsModLoaded(string modID)
            {
                // 打印所有 Mod 的信息
                foreach (Mod mod in Global.Instance.modManager.mods)
                {
                    Console.WriteLine($"Mod ID: {mod.staticID}, Active: {mod.IsActive()}");
                }

                // 检查指定 Mod 是否加载
                foreach (Mod mod in Global.Instance.modManager.mods)
                {
                    if (mod.staticID == modID && mod.IsActive())
                    {
                        return true;
                    }
                }
                return false;
            }

        }


        [HarmonyPatch(typeof(RefrigeratorController.Def), MethodType.Constructor)]
        public static class RefrigeratorControllerDef_Patch
        {
            static void Postfix(RefrigeratorController.Def __instance)
            {
                __instance.simulatedInternalTemperature = 174.15f; // 修改默认值
                Debug.Log("[Mod] 修改 RefrigeratorController.Def.activeCoolingStartBuffer = " + __instance.activeCoolingStartBuffer);
            }
        }



        [HarmonyPatch(typeof(Localization))]
        [HarmonyPatch("Initialize")]
        private class Translate
        {
            public static void Postfix()
            {
                Localize(typeof(STRINGS));
            }
        }

        public static void RegisterForTranslation(Type locstring_tree_root)
        {
            Localization.RegisterForTranslation(locstring_tree_root);
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            // Localization.GenerateStringsTemplate(locstring_tree_root, Path.Combine(Manager.GetDirectory(), "strings_templates"));
            Localization.GenerateStringsTemplate(locstring_tree_root, Path.Combine(Path.GetDirectoryName(executingAssembly.Location), "translations"));

        }


        public static void Localize(Type root)
        {
            // ModUtil.RegisterForTranslation(root);
            RegisterForTranslation(root);
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string modName = executingAssembly.GetName().Name;
            string translationsPath = Path.Combine(Path.GetDirectoryName(executingAssembly.Location), "translations");
            Localization.Locale locale = Localization.GetLocale();
            string languageCode = locale != null ? locale.Code : "en"; // 默认使用英语
            string poFilePath = Path.Combine(translationsPath, languageCode + ".po");
            LoadLocalizationFile(modName, poFilePath);
            // GenerateStringsTemplate(root, translationsPath);
            LocString.CreateLocStringKeys(root, "");
        }

        private static void LoadLocalizationFile(string modName, string poFilePath)
        {
            try
            {
                if (File.Exists(poFilePath))
                {

                    var localizedStrings = Localization.LoadStringsFile(poFilePath, false);
                    Localization.OverloadStrings(localizedStrings);
                }
                else { Debug.LogWarning($"{modName}: 未在: {poFilePath} 找到本地化文件"); }
            }
            catch (Exception ex) { Debug.LogError($"{modName}: 无法加载本地化文件. Error: {ex.Message}"); }
        }

        private static void GenerateStringsTemplate(Type root, string translationsPath)
        {
            try
            {
                Localization.GenerateStringsTemplate(root, translationsPath);
            }
            catch (Exception ex) { Debug.LogError($"无法生成字符串模板. Error: {ex.Message}"); }
        }
    }


}
