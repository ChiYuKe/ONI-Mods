using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkAssetBundles
    {
        public const string StorageNetworkUiBundleName = "storagenetwork_ui";
        public const string StorageNetworkBindSelectionScreenPrefabName = "StorageNetworkBindSelectionScreen";
        public const string EngravingOrderPanelPrefabName = "EngravingOrderPanel";

        private static readonly string[] BundleSearchFolders =
        {
            PlatformPath("Assets", "AssetBundles"),
            PlatformPath("assets", "assetbundles"),
            PlatformPath("AssetBundles"),
            PlatformPath("assetbundles"),
            Path.Combine("Assets", "AssetBundles"),
            Path.Combine("assets", "assetbundles"),
            "AssetBundles",
            "assetbundles",
            ""
        };

        private static readonly Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>();
        private static readonly Dictionary<string, GameObject> LoadedPrefabs = new Dictionary<string, GameObject>();
        private static string modPath;

        public static void SetModPath(string path)
        {
            modPath = path;
            LoadedPrefabs.Clear();
            foreach (AssetBundle bundle in LoadedBundles.Values)
            {
                if (bundle != null)
                {
                    bundle.Unload(false);
                }
            }

            LoadedBundles.Clear();
        }

        public static GameObject GetStorageNetworkBindSelectionScreenPrefab()
        {
            return LoadPrefab(StorageNetworkUiBundleName, StorageNetworkBindSelectionScreenPrefabName);
        }

        public static GameObject GetEngravingOrderPanelPrefab()
        {
            return LoadPrefab(StorageNetworkUiBundleName, EngravingOrderPanelPrefabName);
        }

        public static GameObject LoadPrefab(string bundleName, string prefabName)
        {
            if (string.IsNullOrWhiteSpace(bundleName) || string.IsNullOrWhiteSpace(prefabName))
            {
                return null;
            }

            string cacheKey = bundleName + ":" + prefabName;
            if (LoadedPrefabs.TryGetValue(cacheKey, out GameObject prefab))
            {
                return prefab;
            }

            AssetBundle bundle = LoadBundle(bundleName);
            if (bundle == null)
            {
                return null;
            }

            prefab = bundle.LoadAsset<GameObject>(prefabName);
            if (prefab == null)
            {
                Debug.LogWarning("[StorageNetwork] Failed to load prefab '" + prefabName + "' from bundle '" + bundleName + "'.");
                Debug.LogWarning("[StorageNetwork][AB] Available assets in '" + bundleName + "': " + string.Join(", ", bundle.GetAllAssetNames()));
                return null;
            }

            LoadedPrefabs[cacheKey] = prefab;
            return prefab;
        }

        private static AssetBundle LoadBundle(string bundleName)
        {
            if (LoadedBundles.TryGetValue(bundleName, out AssetBundle bundle))
            {
                return bundle;
            }

            string bundlePath = FindBundleFile(bundleName);
            if (string.IsNullOrEmpty(bundlePath))
            {
                Debug.LogWarning("[StorageNetwork] AssetBundle not found: " + bundleName);
                return null;
            }

            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogWarning("[StorageNetwork] Failed to load AssetBundle: " + bundlePath);
                return null;
            }

            LoadedBundles[bundleName] = bundle;
            return bundle;
        }

        private static string FindBundleFile(string bundleName)
        {
            if (string.IsNullOrEmpty(modPath))
            {
                return null;
            }

            foreach (string folder in BundleSearchFolders)
            {
                string filePath = string.IsNullOrEmpty(folder)
                    ? Path.Combine(modPath, bundleName)
                    : Path.Combine(modPath, folder, bundleName);

                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return null;
        }

        private static string GetPlatformFolder()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    return "windows";
                case RuntimePlatform.LinuxPlayer:
                    return "linux";
                case RuntimePlatform.OSXPlayer:
                    return "mac";
                default:
                    return "";
            }
        }

        private static string PlatformPath(params string[] parts)
        {
            string platformFolder = GetPlatformFolder();
            if (string.IsNullOrEmpty(platformFolder))
            {
                return Path.Combine(parts);
            }

            string[] combined = new string[parts.Length + 1];
            for (int i = 0; i < parts.Length; i++)
            {
                combined[i] = parts[i];
            }

            combined[parts.Length] = platformFolder;
            return Path.Combine(combined);
        }
    }
}
