using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TestMod
{
    public static class UiPrefabAssetBundleLoader
    {
        private static readonly string[] SearchFolders =
        {
            Path.Combine("Assets", "AssetBundles"),
            Path.Combine("assets", "assetbundles"),
            "AssetBundles",
            "assetbundles",
            "ab",
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
                bundle?.Unload(false);
            }

            LoadedBundles.Clear();
        }

        public static void Preload(IEnumerable<string> bundleNames)
        {
            if (bundleNames == null)
            {
                return;
            }

            foreach (string bundleName in bundleNames)
            {
                TryLoadBundle(bundleName);
            }
        }

        public static GameObject LoadPrefab(IEnumerable<string> bundleNames, IEnumerable<string> prefabNames, out string loadedBundleName, out string loadedPrefabName)
        {
            loadedBundleName = null;
            loadedPrefabName = null;

            if (bundleNames == null || prefabNames == null)
            {
                return null;
            }

            foreach (string bundleName in bundleNames)
            {
                AssetBundle bundle = TryLoadBundle(bundleName);
                if (bundle == null)
                {
                    continue;
                }

                foreach (string prefabName in prefabNames)
                {
                    if (string.IsNullOrWhiteSpace(prefabName))
                    {
                        continue;
                    }

                    string cacheKey = bundleName + ":" + prefabName;
                    if (LoadedPrefabs.TryGetValue(cacheKey, out GameObject cachedPrefab))
                    {
                        loadedBundleName = bundleName;
                        loadedPrefabName = prefabName;
                        return cachedPrefab;
                    }

                    GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
                    if (prefab == null)
                    {
                        continue;
                    }

                    LoadedPrefabs[cacheKey] = prefab;
                    loadedBundleName = bundleName;
                    loadedPrefabName = prefabName;
                    return prefab;
                }
            }

            return null;
        }

        private static AssetBundle TryLoadBundle(string bundleName)
        {
            if (string.IsNullOrWhiteSpace(bundleName))
            {
                return null;
            }

            if (LoadedBundles.TryGetValue(bundleName, out AssetBundle bundle))
            {
                return bundle;
            }

            string bundlePath = FindBundlePath(bundleName);
            if (string.IsNullOrEmpty(bundlePath))
            {
                return null;
            }

            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                return null;
            }

            LoadedBundles[bundleName] = bundle;
            return bundle;
        }

        private static string FindBundlePath(string bundleName)
        {
            if (string.IsNullOrWhiteSpace(modPath))
            {
                return null;
            }

            foreach (string folder in SearchFolders)
            {
                foreach (string candidateFolder in GetCandidateFolders(folder))
                {
                    string bundlePath = string.IsNullOrEmpty(candidateFolder)
                        ? Path.Combine(modPath, bundleName)
                        : Path.Combine(modPath, candidateFolder, bundleName);

                    if (File.Exists(bundlePath))
                    {
                        return bundlePath;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<string> GetCandidateFolders(string baseFolder)
        {
            string platformFolder = GetPlatformFolderName();
            if (!string.IsNullOrEmpty(platformFolder))
            {
                if (string.IsNullOrEmpty(baseFolder))
                {
                    yield return platformFolder;
                }
                else
                {
                    yield return Path.Combine(baseFolder, platformFolder);
                }
            }

            yield return baseFolder;
        }

        private static string GetPlatformFolderName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "mac";
                default:
                    return null;
            }
        }
    }
}
