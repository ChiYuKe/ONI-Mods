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
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.activeColor = StorageNetworkPanelPalette.BlueButtonPressed;
            style.inactiveColor = StorageNetworkPanelPalette.BlueButtonNormal;
            style.hoverColor = StorageNetworkPanelPalette.BlueButtonHover;
            style.disabledColor = new Color(0.4156863f, 0.4117647f, 0.4f);
            style.disabledActiveColor = new Color(0.625f, 0.6158088f, 0.5882353f);
            style.disabledhoverColor = new Color(0.5f, 0.4898898f, 0.4595588f);
            return style;
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
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.activeColor = OniPinkActive();
            style.inactiveColor = OniPinkInactive();
            style.hoverColor = OniPinkHover();
            style.disabledColor = new Color(0.4156863f, 0.4117647f, 0.4f);
            style.disabledActiveColor = Color.clear;
            style.disabledhoverColor = new Color(0.5f, 0.5f, 0.5f);
            return style;
        }

        private static ColorStyleSetting CreateColorStyle(Color normal, Color hover, Color pressed)
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = normal;
            style.hoverColor = hover;
            style.activeColor = pressed;
            style.disabledColor = Darken(normal, 0.08f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
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
