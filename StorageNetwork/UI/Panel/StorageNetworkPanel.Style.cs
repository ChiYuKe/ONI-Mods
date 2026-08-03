using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private static readonly Dictionary<ColorStyleKey, ColorStyleSetting> colorStyleCache =
            new Dictionary<ColorStyleKey, ColorStyleSetting>();
        private static ColorStyleSetting kleiBlueStyle;
        private static ColorStyleSetting kleiPinkStyle;

        private static void ApplyThinButtonSprite(KImage image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSpriteByName("web_button");
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.fillCenter = true;
        }

        private static void ApplyThinBoxSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSpriteByName("web_box");
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.fillCenter = true;
        }

        private static Sprite GetSpriteByName(string spriteName)
        {
            if (spriteName == "storage_network_overlay")
            {
                Sprite modSprite = StorageNetworkSprites.GetOverviewIcon();
                if (modSprite != null)
                {
                    return modSprite;
                }
            }

            Sprite sprite = Assets.GetSprite(spriteName);
            if (sprite != null)
            {
                return sprite;
            }

            sprite = StorageNetworkSpriteLoader.GetSprite(spriteName);
            if (sprite != null)
            {
                return sprite;
            }

            if (spriteCache == null)
            {
                spriteCache = new Dictionary<string, Sprite>();
                foreach (Sprite resourceSprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    string name = resourceSprite != null ? resourceSprite.name : null;
                    if (!string.IsNullOrEmpty(name) && !spriteCache.ContainsKey(name))
                    {
                        spriteCache.Add(name, resourceSprite);
                    }
                }
            }

            return spriteCache.TryGetValue(spriteName, out sprite) ? sprite : null;
        }

        private static ColorStyleSetting KleiBlueStyle()
        {
            if (kleiBlueStyle == null)
            {
                kleiBlueStyle = ScriptableObject.CreateInstance<ColorStyleSetting>();
                kleiBlueStyle.activeColor = StorageNetworkPanelPalette.BlueButtonPressed;
                kleiBlueStyle.inactiveColor = StorageNetworkPanelPalette.BlueButtonNormal;
                kleiBlueStyle.hoverColor = StorageNetworkPanelPalette.BlueButtonHover;
                kleiBlueStyle.disabledColor = new Color(0.4156863f, 0.4117647f, 0.4f);
                kleiBlueStyle.disabledActiveColor = new Color(0.625f, 0.6158088f, 0.5882353f);
                kleiBlueStyle.disabledhoverColor = new Color(0.5f, 0.4898898f, 0.4595588f);
            }

            return kleiBlueStyle;
        }

        private static Color OniPinkInactive()
        {
            return StorageNetworkPanelPalette.PinkButtonNormal;
        }

        private static Color OniPinkHover()
        {
            return StorageNetworkPanelPalette.PinkButtonHover;
        }

        private static Color OniPinkActive()
        {
            return StorageNetworkPanelPalette.PinkButtonPressed;
        }

        private static ColorStyleSetting KleiPinkStyle()
        {
            if (kleiPinkStyle == null)
            {
                kleiPinkStyle = ScriptableObject.CreateInstance<ColorStyleSetting>();
                kleiPinkStyle.activeColor = OniPinkActive();
                kleiPinkStyle.inactiveColor = OniPinkInactive();
                kleiPinkStyle.hoverColor = OniPinkHover();
                kleiPinkStyle.disabledColor = new Color(0.4156863f, 0.4117647f, 0.4f);
                kleiPinkStyle.disabledActiveColor = Color.clear;
                kleiPinkStyle.disabledhoverColor = new Color(0.5f, 0.5f, 0.5f);
            }

            return kleiPinkStyle;
        }

        private static ColorStyleSetting CreateColorStyle(Color normal, Color hover, Color pressed)
        {
            ColorStyleKey key = new ColorStyleKey(normal, hover, pressed);
            if (colorStyleCache.TryGetValue(key, out ColorStyleSetting cached) && cached != null)
            {
                return cached;
            }

            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = normal;
            style.hoverColor = hover;
            style.activeColor = pressed;
            style.disabledColor = Darken(normal, 0.08f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            colorStyleCache[key] = style;
            return style;
        }

        private static void ClearColorStyleCache()
        {
            spriteCache?.Clear();
            spriteCache = null;
            foreach (ColorStyleSetting style in colorStyleCache.Values)
            {
                if (style != null)
                {
                    Destroy(style);
                }
            }

            colorStyleCache.Clear();
            productNormalStyle = null;
            productSelectedStyle = null;
            if (kleiBlueStyle != null)
            {
                Destroy(kleiBlueStyle);
                kleiBlueStyle = null;
            }
            if (kleiPinkStyle != null)
            {
                Destroy(kleiPinkStyle);
                kleiPinkStyle = null;
            }
        }

        private readonly struct ColorStyleKey : System.IEquatable<ColorStyleKey>
        {
            private readonly Color32 normal;
            private readonly Color32 hover;
            private readonly Color32 pressed;

            public ColorStyleKey(Color normal, Color hover, Color pressed)
            {
                this.normal = normal;
                this.hover = hover;
                this.pressed = pressed;
            }

            public bool Equals(ColorStyleKey other)
            {
                return normal.Equals(other.normal) &&
                       hover.Equals(other.hover) &&
                       pressed.Equals(other.pressed);
            }

            public override bool Equals(object obj)
            {
                return obj is ColorStyleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = normal.GetHashCode();
                    hashCode = (hashCode * 397) ^ hover.GetHashCode();
                    hashCode = (hashCode * 397) ^ pressed.GetHashCode();
                    return hashCode;
                }
            }
        }

        private static Color Lighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a);
        }

        private static Color Darken(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r - amount),
                Mathf.Clamp01(color.g - amount),
                Mathf.Clamp01(color.b - amount),
                color.a);
        }
    }
}
