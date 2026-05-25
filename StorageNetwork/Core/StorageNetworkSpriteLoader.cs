using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkSpriteLoader
    {
        private static readonly string[] SearchFolders =
        {
            Path.Combine("Assets", "Sprite"),
            Path.Combine("assets", "sprite"),
            "",
            "assets",
            "images",
            "sprites"
        };
        private static readonly Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
        private static string modPath;

        public static void SetModPath(string path)
        {
            modPath = path;
            LoadedSprites.Clear();
        }

        public static Sprite GetSprite(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return null;
            }

            Sprite sprite = Assets.GetSprite(spriteName);
            if (sprite != null)
            {
                return sprite;
            }

            if (LoadedSprites.TryGetValue(spriteName, out sprite))
            {
                return sprite;
            }

            string filePath = FindSpriteFile(spriteName);
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            sprite = LoadSpriteFromFile(spriteName, filePath);
            if (sprite == null)
            {
                return null;
            }

            LoadedSprites[spriteName] = sprite;
            RegisterSprite(spriteName, sprite);
            Debug.Log("[StorageNetwork] Registered sprite: " + spriteName + " from " + filePath);
            return sprite;
        }

        private static string FindSpriteFile(string spriteName)
        {
            if (string.IsNullOrEmpty(modPath))
            {
                return null;
            }

            foreach (string folder in SearchFolders)
            {
                string filePath = string.IsNullOrEmpty(folder)
                    ? Path.Combine(modPath, spriteName + ".png")
                    : Path.Combine(modPath, folder, spriteName + ".png");

                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return null;
        }

        private static Sprite LoadSpriteFromFile(string spriteName, string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = spriteName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                Debug.LogWarning("[StorageNetwork] Failed to decode sprite: " + filePath);
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0U,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }

        private static void RegisterSprite(string spriteName, Sprite sprite)
        {
            if (Assets.Sprites == null)
            {
                Assets.Sprites = new Dictionary<HashedString, Sprite>();
            }

            Assets.Sprites[new HashedString(spriteName)] = sprite;
        }
    }
}
