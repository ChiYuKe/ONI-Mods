using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CykUtils
{
    public static class ModResources
    {
        private const string DEFAULT_BUNDLE_NAME = "mybundle";

        /// <summary>
        /// 从缓存或加载的 AssetBundle 获取 Texture2D
        /// </summary>
        public static Texture2D LoadTexture(string name, string bundleName = DEFAULT_BUNDLE_NAME)
        {
            var bundle = AssetBundleManager.GetBundle(bundleName);
            if (bundle == null) return null;

            Texture2D tex = bundle.LoadAsset<Texture2D>(name);
            if (tex == null)
                Debug.LogError($"Texture2D 资源不存在: {name} (Bundle: {bundleName})");

            return tex;
        }

        /// <summary>
        /// 从缓存或加载的 AssetBundle 获取 Sprite
        /// </summary>
        public static Sprite LoadSprite(string name, string bundleName = DEFAULT_BUNDLE_NAME)
        {
            var tex = LoadTexture(name, bundleName);
            if (tex == null) return null;

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
        }
    }




    public static class AssetBundleManager
    {
        // 缓存已经加载的 AssetBundle
        private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

        /// <summary>
        /// 获取 AssetBundle，如果已经加载过就直接返回缓存
        /// </summary>
        /// <param name="bundleName">AssetBundle 文件名</param>
        /// <param name="bundlePath">可选路径，不传就用默认 Mod 目录</param>
        /// <returns>AssetBundle 对象</returns>
        public static AssetBundle GetBundle(string bundleName = "mybundle", string bundlePath = null)
        {
            if (loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                return bundle;
            }

            // 如果没传路径，默认放在 Mod Assets 目录
            if (string.IsNullOrEmpty(bundlePath))
            {
                bundlePath = Path.Combine(KUtils.AssetsPath, bundleName);
            }

            if (!File.Exists(bundlePath))
            {
                Debug.LogError($"AssetBundle 文件不存在: {bundlePath}");
                return null;
            }

            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogError($"AssetBundle 加载失败: {bundlePath}");
                return null;
            }

            loadedBundles[bundleName] = bundle;
            return bundle;
        }

        /// <summary>
        /// 卸载所有已加载的 AssetBundle
        /// </summary>
        /// <param name="unloadAssets">是否同时卸载 AssetBundle 内资源</param>
        public static void UnloadAllBundles(bool unloadAssets = false)
        {
            foreach (var kv in loadedBundles)
            {
                kv.Value.Unload(unloadAssets);
            }
            loadedBundles.Clear();
        }
    }
}
