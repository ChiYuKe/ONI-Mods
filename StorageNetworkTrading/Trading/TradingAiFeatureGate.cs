using System;
using System.IO;

namespace StorageNetwork.Trading
{
    // Like the storage network profiler, opt in with a marker file at mod load.
    // AI credentials and save sidecars never enable this feature implicitly.
    internal static class TradingAiFeatureGate
    {
        internal const string EnableFileName = "TradingAi.enabled";
        internal static bool Enabled { get; private set; }

        internal static void Configure(string modPath)
        {
            string configDirectory = null;
            try
            {
                configDirectory = Path.Combine(Util.RootFolder(), "mods", "StorageNetworkTrading");
            }
            catch
            {
                // A missing user directory must not implicitly enable AI.
            }
            Enabled = IsEnabled(modPath, configDirectory);
        }

        internal static bool IsEnabled(string modPath, string configDirectory)
        {
            return HasEnableFile(configDirectory) || HasEnableFile(modPath);
        }

        private static bool HasEnableFile(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return false;
            try { return File.Exists(Path.Combine(directory, EnableFileName)); }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
        }
    }
}