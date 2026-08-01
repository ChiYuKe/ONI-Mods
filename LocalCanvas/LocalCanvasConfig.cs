using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LocalCanvas
{
    internal sealed class LocalCanvasConfigData
    {
        public string CanvasFolder { get; set; } = "images/Canvas";
        public string CanvasTallFolder { get; set; } = "images/CanvasTall";
        public string CanvasWideFolder { get; set; } = "images/CanvasWide";
        public float ImageBrightness { get; set; } = 0.8f;
        public float VerticalOffset { get; set; }
        public float DepthOffset { get; set; } = -0.05f;
    }

    internal static class LocalCanvasConfig
    {
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> warnedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static string ModDirectory { get; private set; }
        public static LocalCanvasConfigData Data { get; private set; }

        public static void Load()
        {
            ModDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string configPath = Path.Combine(ModDirectory, "config.json");
            Directory.CreateDirectory(Path.Combine(ModDirectory, "images"));
            Directory.CreateDirectory(Path.Combine(ModDirectory, "images", "Canvas"));
            Directory.CreateDirectory(Path.Combine(ModDirectory, "images", "CanvasTall"));
            Directory.CreateDirectory(Path.Combine(ModDirectory, "images", "CanvasWide"));
            Data = new LocalCanvasConfigData();

            try
            {
                if (File.Exists(configPath))
                {
                    Data = JsonConvert.DeserializeObject<LocalCanvasConfigData>(File.ReadAllText(configPath)) ?? Data;
                }
                else
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(Data, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalCanvas] failed to load config: {ex}");
            }
        }

        public static IEnumerable<string> EnumerateImageFiles(string prefabId)
        {
            string configuredPath = prefabId switch
            {
                "CanvasTall" => Data.CanvasTallFolder,
                "CanvasWide" => Data.CanvasWideFolder,
                _ => Data.CanvasFolder
            };

            string folder = ResolvePath(configuredPath);
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return Array.Empty<string>();
            }

            try
            {
                return Directory.GetFiles(folder)
                    .Where(IsSupportedImage)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalCanvas] failed to enumerate {folder}: {ex}");
                return Array.Empty<string>();
            }
        }

        public static string GetImageFolderPath(string prefabId)
        {
            string configuredPath = prefabId switch
            {
                "CanvasTall" => Data.CanvasTallFolder,
                "CanvasWide" => Data.CanvasWideFolder,
                _ => Data.CanvasFolder
            };

            return ResolvePath(configuredPath);
        }

        public static Sprite LoadSprite(string path)
        {
            path = Path.GetFullPath(path);
            try
            {
                Texture2D texture = LoadTexture(path);
                if (texture == null)
                {
                    return null;
                }

                if (!spriteCache.TryGetValue(path, out Sprite sprite) || sprite == null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    sprite.name = "LocalCanvasSprite_" + Path.GetFileName(path);
                    spriteCache[path] = sprite;
                }

                return sprite;
            }
            catch (Exception ex)
            {
                WarnOnce(path, $"failed to load image {path}: {ex.Message}");
                return null;
            }
        }

        public static Texture2D LoadTexture(string path)
        {
            path = Path.GetFullPath(path);
            try
            {
                if (!textureCache.TryGetValue(path, out Texture2D texture) || texture == null)
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                    if (!texture.LoadImage(bytes, false))
                    {
                        UnityEngine.Object.Destroy(texture);
                        WarnOnce(path, $"failed to decode image: {path}");
                        return null;
                    }

                    ApplyBrightness(texture, Mathf.Clamp(Data?.ImageBrightness ?? 0.8f, 0f, 1f));
                    // Keep source images readable until LocalCanvasKAnimRegistry
                    // has packed them into the per-canvas shared KAnim atlas.
                    texture.Apply(false, false);
                    texture.name = "LocalCanvas_" + Path.GetFileName(path);
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Bilinear;
                    textureCache[path] = texture;
                }

                return texture;
            }
            catch (Exception ex)
            {
                WarnOnce(path, $"failed to load image {path}: {ex.Message}");
                return null;
            }
        }

        private static void ApplyBrightness(Texture2D texture, float brightness)
        {
            if (Mathf.Approximately(brightness, 1f))
            {
                return;
            }

            Color32[] pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                pixel.r = (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.r * brightness), 0, 255);
                pixel.g = (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.g * brightness), 0, 255);
                pixel.b = (byte)Mathf.Clamp(Mathf.RoundToInt(pixel.b * brightness), 0, 255);
                pixels[i] = pixel;
            }

            texture.SetPixels32(pixels);
        }

        private static string ResolvePath(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return null;
            }

            string path = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(ModDirectory, path));
        }

        private static bool IsSupportedImage(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static void WarnOnce(string key, string message)
        {
            key ??= "<empty>";
            if (warnedPaths.Add(key))
            {
                Debug.LogWarning("[LocalCanvas] " + message);
            }
        }
    }
}
