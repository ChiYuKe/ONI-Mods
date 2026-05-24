using System.IO;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkSprites
    {
        public const string OverviewIconName = "StorageNetwork_overview_icon";

        private const string FallbackOverviewIconName = "crew_state_encourage";
        private const string OverviewIconPath = "Assets/storage_network_overlay.png";
        private static string pendingModPath;
        private static bool registered;

        public static void SetModPath(string modPath)
        {
            pendingModPath = modPath;
        }

        public static void RegisterPending()
        {
            Register(pendingModPath);
        }

        public static void Register(string modPath)
        {
            if (registered || string.IsNullOrEmpty(modPath) || Assets.Sprites == null)
            {
                return;
            }

            if (Assets.Sprites.ContainsKey(OverviewIconName))
            {
                registered = true;
                return;
            }

            Sprite sprite = LoadSprite(Path.Combine(modPath, OverviewIconPath), OverviewIconName);
            if (sprite != null)
            {
                Assets.Sprites[OverviewIconName] = sprite;
                registered = true;
            }
        }

        public static string GetOverviewIconName()
        {
            return registered && Assets.Sprites != null && Assets.Sprites.ContainsKey(OverviewIconName)
                ? OverviewIconName
                : FallbackOverviewIconName;
        }

        public static Sprite GetOverviewIcon()
        {
            if (Assets.Sprites == null)
            {
                return null;
            }

            Sprite sprite;
            if (Assets.Sprites.TryGetValue(OverviewIconName, out sprite) && sprite != null)
            {
                return sprite;
            }

            return Assets.GetSprite(FallbackOverviewIconName);
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                return null;
            }

            texture.name = spriteName;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }
    }
}
