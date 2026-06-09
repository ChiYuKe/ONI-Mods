using System.Collections;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace CykUtils
{
    public class ABHelper
    {

        public static GameObject UIPrefab;
        public static GameObject ButtonPrefab;
        public static GameObject TestScreen;



        public static void LoadABUI()
        {
            AssetBundle assetBundle = ABHelper.LoadAB("eternaldecayab", null, true);
            // UIPrefab = assetBundle.LoadAsset<GameObject>("TestUI.prefab");
            ButtonPrefab = assetBundle.LoadAsset<GameObject>("TestButton.prefab");

            TestScreen = assetBundle.LoadAsset<GameObject>("testscreen.prefab");
            

        }








        public static AssetBundle LoadAB(string assetBundleName, string path = null, bool platformSpecific = false)
        {
          
            foreach (var loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (loadedBundle.name == assetBundleName) return loadedBundle;
            }

          
            if (string.IsNullOrEmpty(path))
            {
                
                path = KUtils.AssetsPath;
            }

           
            if (platformSpecific)
            {
                string platformFolder = Application.platform switch
                {
                    RuntimePlatform.WindowsPlayer => "windows",
                    RuntimePlatform.OSXPlayer => "mac",
                    RuntimePlatform.LinuxPlayer => "linux",
                    _ => "" 
                };
                path = Path.Combine(path, platformFolder);
            }

            // 4. 最终路径拼合并执行加载
            string fullPath = Path.Combine(path, assetBundleName);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[ABLoader] 找不到文件: {fullPath}");
                return null;
            }

            return AssetBundle.LoadFromFile(fullPath);
        }

    }
}
