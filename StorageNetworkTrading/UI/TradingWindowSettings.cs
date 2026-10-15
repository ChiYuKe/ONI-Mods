using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace StorageNetworkTrading.UI
{
    internal sealed class StorageNetworkWindowLayout
    {
        public float X, Y, Width, Height;
    }

    internal sealed class TradingWindowSettings
    {
        public Dictionary<string, StorageNetworkWindowLayout> WindowLayouts = new Dictionary<string, StorageNetworkWindowLayout>();
        private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StorageNetworkTrading", "windows.json");
        internal static readonly TradingWindowSettings Instance = Load();
        private static TradingWindowSettings Load()
        {
            try
            {
                return File.Exists(FilePath) ? JsonConvert.DeserializeObject<TradingWindowSettings>(File.ReadAllText(FilePath)) ?? new TradingWindowSettings() : new TradingWindowSettings();
            }
            catch
            {
                return new TradingWindowSettings();
            }
        }

        internal static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(Instance));
            }
            catch
            {
                UnityEngine.Debug.LogWarning("[StorageNetworkTrading] Cannot save window layout.");
            }
        }
    }
}
