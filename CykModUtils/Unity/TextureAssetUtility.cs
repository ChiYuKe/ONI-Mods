using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CykModUtils.Unity
{
    /// <summary>
    /// 从本地图片创建 Texture2D/Sprite，并可注册到 ONI Assets。
    /// </summary>
    public static class TextureAssetUtility
    {
        /// <summary>
        /// 支持的常见图片扩展名。
        /// </summary>
        public static bool IsSupportedImage(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 从 PNG/JPG 文件加载 Texture2D。
        /// </summary>
        public static bool TryLoadTexture(
            string path,
            out Texture2D texture,
            bool markNonReadable = true,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            texture = null;
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path) ||
                !IsSupportedImage(path))
            {
                return false;
            }

            Texture2D created = null;
            try
            {
                created = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    filterMode = filterMode,
                    wrapMode = wrapMode
                };

                if (!created.LoadImage(File.ReadAllBytes(path), markNonReadable))
                {
                    UnityEngine.Object.Destroy(created);
                    return false;
                }

                texture = created;
                return true;
            }
            catch
            {
                if (created != null)
                {
                    UnityEngine.Object.Destroy(created);
                }

                return false;
            }
        }

        /// <summary>
        /// 使用整张纹理创建 Sprite。
        /// </summary>
        public static bool TryCreateSprite(
            Texture2D texture,
            out Sprite sprite,
            string spriteName = null,
            float pixelsPerUnit = 100f,
            Vector2? pivot = null,
            SpriteMeshType meshType = SpriteMeshType.FullRect)
        {
            sprite = null;
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return false;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                pivot ?? new Vector2(0.5f, 0.5f),
                Mathf.Max(0.01f, pixelsPerUnit),
                0U,
                meshType);
            if (sprite == null)
            {
                return false;
            }

            sprite.name = string.IsNullOrWhiteSpace(spriteName)
                ? texture.name
                : spriteName;
            return true;
        }

        /// <summary>
        /// 从图片文件加载 Sprite。Sprite 销毁时调用方也应负责销毁其 texture。
        /// </summary>
        public static bool TryLoadSprite(
            string path,
            out Sprite sprite,
            string spriteName = null,
            float pixelsPerUnit = 100f,
            bool markTextureNonReadable = true)
        {
            sprite = null;
            if (!TryLoadTexture(path, out Texture2D texture, markTextureNonReadable))
            {
                return false;
            }

            if (TryCreateSprite(texture, out sprite, spriteName, pixelsPerUnit))
            {
                return true;
            }

            UnityEngine.Object.Destroy(texture);
            return false;
        }

        /// <summary>
        /// 把 Sprite 注册到 Assets.Sprites。
        /// </summary>
        public static bool RegisterSprite(
            string spriteName,
            Sprite sprite,
            bool overwrite = true)
        {
            if (string.IsNullOrWhiteSpace(spriteName) || sprite == null)
            {
                return false;
            }

            if (Assets.Sprites == null)
            {
                Assets.Sprites = new Dictionary<HashedString, Sprite>();
            }

            HashedString key = new HashedString(spriteName);
            if (!overwrite && Assets.Sprites.ContainsKey(key))
            {
                return false;
            }

            sprite.name = spriteName;
            Assets.Sprites[key] = sprite;
            return true;
        }

        /// <summary>
        /// 加载图片并注册为 ONI Sprite。
        /// </summary>
        public static bool TryLoadAndRegisterSprite(
            string path,
            string spriteName,
            out Sprite sprite,
            float pixelsPerUnit = 100f,
            bool overwrite = true,
            bool markTextureNonReadable = true)
        {
            if (!TryLoadSprite(
                path,
                out sprite,
                spriteName,
                pixelsPerUnit,
                markTextureNonReadable))
            {
                return false;
            }

            if (RegisterSprite(spriteName, sprite, overwrite))
            {
                return true;
            }

            Texture2D texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            UnityEngine.Object.Destroy(texture);
            sprite = null;
            return false;
        }
    }
}
