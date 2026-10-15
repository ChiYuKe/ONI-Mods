using System;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.Trading
{
    internal sealed partial class TradingAiSettingsStore
    {
        internal const string SaveExtension = ".StorageNetworkTradingAi.json";

        internal static string GetStorePath(string savePath) =>
            string.IsNullOrEmpty(savePath) ? null : savePath + SaveExtension;

        internal static TradingAiSettingsStore Local
        {
            get
            {
                string path = GetStorePath(SaveLoader.GetActiveSaveFilePath());
                string previous = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Klei", "OxygenNotIncluded", "mods", "config", "StorageNetworkTrading", "trading-ai.json");
                if (!File.Exists(previous))
                    previous = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "StorageNetwork", "trading-ai.json");
                return new TradingAiSettingsStore(path, path == null ? null : previous);
            }
        }
    }

    // Capture before SaveLoader changes the active file, then write beside the
    // actual successful result. This also covers autosaves without changing it.
    [HarmonyPatch(typeof(SaveLoader), "Save", new[] { typeof(string), typeof(bool), typeof(bool) })]
    internal static class TradingAiSettingsSavePatch
    {
        private static void Prefix(out TradingAiSettings __state)
        {
            __state = null;
            if (StorageNetworkTradingState.Instance == null) return;
            if (TradingAiSettingsStore.Local.Load(out var settings) && settings.ValidationKey() == null) __state = settings;
        }

        private static void Postfix(string filename, string __result, TradingAiSettings __state)
        {
            if (__state == null || string.IsNullOrEmpty(__result) || !File.Exists(__result) ||
                !string.Equals(Path.GetFullPath(filename), Path.GetFullPath(__result), StringComparison.OrdinalIgnoreCase)) return;
            try { new TradingAiSettingsStore(TradingAiSettingsStore.GetStorePath(__result)).Save(__state); }
            catch (Exception) { Debug.LogWarning("[StorageNetworkTrading] Cannot save AI configuration beside the save file."); }
        }
    }
}
