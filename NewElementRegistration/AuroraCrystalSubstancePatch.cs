using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NewElementRegistration
{
    [HarmonyPatch(typeof(Assets), "SubstanceListHookup")]
    public static class AuroraCrystalSubstancePatch
    {
        public static void Postfix()
        {
            Element element = ElementLoader.FindElementByHash(ModElements.AuroraCrystal);
            if (element == null || element.substance == null)
            {
                Debug.LogError("[NewElementRegistration] AuroraCrystal was not loaded from elements/custom_elements.yaml");
                return;
            }

            Substance source = Assets.instance.substanceTable.GetSubstance(SimHashes.Granite);
            if (source == null || source.material == null)
            {
                Debug.LogError("[NewElementRegistration] Granite substance/material was not available");
                return;
            }

            Material material = new Material(source.material)
            {
                name = "matAuroraCrystal"
            };

            Texture2D texture = LoadTexture("AuroraCrystal.png");
            if (texture != null)
            {
                material.mainTexture = texture;
            }

            Color32 color = new Color32(102, 220, 255, 255);
            KAnimFile anim = Assets.GetAnim("Test_element_kanim");
            KAnimFile[] anims = null;
            if (anim != null)
            {
                anims = new[] { anim };
            }
            else
            {
                Debug.LogWarning("[NewElementRegistration] Test_element_kanim was not available; falling back to Granite anim");
                anim = source.anim;
                anims = Traverse.Create(source).Field("anims").GetValue<KAnimFile[]>();
                if (anim == null && anims != null && anims.Length > 0)
                {
                    anim = anims[0];
                }
            }

            if (anim == null)
            {
                Debug.LogError("[NewElementRegistration] Granite anim was not available");
                return;
            }

            if (anims == null || anims.Length == 0 || anims[0] == null)
            {
                anims = new[] { anim };
            }

            element.substance.anim = anim;
            element.substance.material = material;
            element.substance.colour = color;
            element.substance.uiColour = color;
            element.substance.conduitColour = color;

            Traverse.Create(element.substance)
                .Field("anims")
                .SetValue(anims);
        }

        private static Texture2D LoadTexture(string fileName)
        {
            try
            {
                string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(modPath, "assets", "textures", fileName);
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[NewElementRegistration] Texture not found: " + path);
                    return null;
                }

                byte[] bytes = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Debug.LogWarning("[NewElementRegistration] Texture decode failed: " + path);
                    return null;
                }

                texture.name = Path.GetFileNameWithoutExtension(fileName);
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogError("[NewElementRegistration] Failed to load texture " + fileName + ": " + ex);
                return null;
            }
        }
    }
}
