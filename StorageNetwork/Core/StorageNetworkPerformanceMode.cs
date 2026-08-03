using System.IO;
using UnityEngine;

namespace StorageNetwork.Core
{
    /// <summary>
    /// Developer-only rollout switches. They are marker based so no gameplay
    /// configuration or save data is introduced.
    /// </summary>
    internal static class StorageNetworkPerformanceMode
    {
        private const string LegacyMarker = "StorageNetworkPerformanceLegacy.enabled";
        private const string ShadowMarker = "StorageNetworkPerformanceShadow.enabled";
        private const string ShadowFullMarker = "StorageNetworkPerformanceShadowFull.enabled";
        private static string modPath;

        public static bool LegacyFullScanEnabled { get; private set; }
        public static bool ShadowValidationEnabled { get; private set; }
        public static bool ShadowValidationFullEnabled { get; private set; }

        public static void SetModPath(string path)
        {
            modPath = path;
            Reload();
        }

        public static void Reload()
        {
            LegacyFullScanEnabled = IsMarkerEnabled(LegacyMarker);
            ShadowValidationEnabled = IsMarkerEnabled(ShadowMarker);
            ShadowValidationFullEnabled = IsMarkerEnabled(ShadowFullMarker);
            ShadowValidationEnabled |= ShadowValidationFullEnabled;
            if (LegacyFullScanEnabled)
            {
                Debug.LogWarning(
                    "[StorageNetwork][Performance] legacy full-refresh fallback enabled.");
            }

            if (ShadowValidationEnabled)
            {
                Debug.Log(
                    ShadowValidationFullEnabled
                        ? "[StorageNetwork][Performance] full shadow validation enabled."
                        : "[StorageNetwork][Performance] sampled shadow validation enabled.");
            }
        }

        private static bool IsMarkerEnabled(string fileName)
        {
            string configDirectory = GetConfigDirectory();
            return !string.IsNullOrEmpty(configDirectory) &&
                       File.Exists(Path.Combine(configDirectory, fileName)) ||
                   !string.IsNullOrEmpty(modPath) &&
                       File.Exists(Path.Combine(modPath, fileName));
        }

        private static string GetConfigDirectory()
        {
            try
            {
                return Path.Combine(Util.RootFolder(), "mods", "StorageNetwork");
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
