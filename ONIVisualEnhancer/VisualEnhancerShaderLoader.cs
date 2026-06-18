using System.IO;
using System.Reflection;
using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class VisualEnhancerShaderLoader
    {
        private const string BundleRelativePath = "assets/visual_enhancer_postprocess";
        private const string CustomShaderName = "Hidden/ONIVisualEnhancer/PostProcess";

        private static Shader customShader;
        private static bool customLoaded;

        public static Shader Find(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.Log("[ONIVisualEnhancer] Shader not found: " + shaderName);
            }

            return shader;
        }

        public static Shader GetCustomPostProcessShader()
        {
            if (customLoaded)
            {
                return customShader;
            }

            customLoaded = true;
            customShader = Shader.Find(CustomShaderName);
            if (customShader != null)
            {
                return customShader;
            }

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string modRoot = Path.GetDirectoryName(assemblyPath);
            string bundlePath = Path.Combine(modRoot ?? string.Empty, BundleRelativePath);
            if (!File.Exists(bundlePath))
            {
                return null;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogWarning("[ONIVisualEnhancer] Failed to load post-process asset bundle: " + bundlePath);
                return null;
            }

            customShader = bundle.LoadAsset<Shader>(CustomShaderName);
            if (customShader == null)
            {
                customShader = bundle.LoadAsset<Shader>("ONIVisualEnhancerPostProcess");
            }

            return customShader;
        }
    }
}
