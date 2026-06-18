using System.Collections.Generic;
using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class MaterialVisualController
    {
        private const string LiquidShader = "Klei/LiquidSubstance";
        private const string SolidShader = "Klei/Substance";

        private static readonly Dictionary<int, MaterialSnapshot> originals = new Dictionary<int, MaterialSnapshot>();

        public static void ApplySettings()
        {
            if (!VisualEnhancerSettings.MaterialAdjustmentsEnabled)
            {
                RestoreAll();
                return;
            }

            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (!IsSupportedMaterial(material, out bool liquid))
                {
                    continue;
                }

                int id = material.GetInstanceID();
                if (!originals.TryGetValue(id, out MaterialSnapshot snapshot))
                {
                    snapshot = MaterialSnapshot.Capture(material);
                    originals[id] = snapshot;
                }

                ApplyMaterial(material, snapshot, liquid);
            }
        }

        public static void RestoreAll()
        {
            foreach (KeyValuePair<int, MaterialSnapshot> pair in originals)
            {
                Material material = pair.Value.Material;
                if (material != null)
                {
                    pair.Value.Restore();
                }
            }
        }

        public static void ClearRuntimeState()
        {
            RestoreAll();
            originals.Clear();
        }

        private static bool IsSupportedMaterial(Material material, out bool liquid)
        {
            liquid = false;
            if (material == null || material.shader == null)
            {
                return false;
            }

            string shaderName = material.shader.name;
            if (shaderName == LiquidShader)
            {
                liquid = true;
                return true;
            }

            return shaderName == SolidShader;
        }

        private static void ApplyMaterial(Material material, MaterialSnapshot snapshot, bool liquid)
        {
            if (liquid)
            {
                ApplyColor(material, snapshot, "_Colour", VisualEnhancerSettings.LiquidColor);
                ApplyColor(material, snapshot, "_ShineColour", VisualEnhancerSettings.LiquidShine);
                ApplyColor(material, snapshot, "_SpecColor", VisualEnhancerSettings.LiquidShine);
                ApplyFloat(material, snapshot, "_Frequency", VisualEnhancerSettings.LiquidFlow);
                ApplyFloat(material, snapshot, "_Fresnel", VisualEnhancerSettings.LiquidShine);
                ApplyFloat(material, snapshot, "_Glossiness", VisualEnhancerSettings.LiquidShine);
                ApplyFloat(material, snapshot, "_Shininess", VisualEnhancerSettings.LiquidShine);
                ApplyFloat(material, snapshot, "_SpecInt", VisualEnhancerSettings.LiquidShine);
            }
            else
            {
                ApplyColor(material, snapshot, "_Colour", VisualEnhancerSettings.SolidColor);
                ApplyColor(material, snapshot, "_ShineColour", VisualEnhancerSettings.SolidShine);
                ApplyColor(material, snapshot, "_SpecColor", VisualEnhancerSettings.SolidShine);
                ApplyFloat(material, snapshot, "_Fresnel", VisualEnhancerSettings.SolidShine);
                ApplyFloat(material, snapshot, "_Glossiness", VisualEnhancerSettings.SolidShine);
                ApplyFloat(material, snapshot, "_Shininess", VisualEnhancerSettings.SolidShine);
                ApplyFloat(material, snapshot, "_SpecInt", VisualEnhancerSettings.SolidShine);
            }

            ApplyFloat(material, snapshot, "_WorldUVScale", VisualEnhancerSettings.MaterialTextureScale);
        }

        private static void ApplyColor(Material material, MaterialSnapshot snapshot, string property, float multiplier)
        {
            if (!snapshot.TryGetColor(property, out Color original))
            {
                return;
            }

            material.SetColor(property, new Color(
                Mathf.Clamp01(original.r * multiplier),
                Mathf.Clamp01(original.g * multiplier),
                Mathf.Clamp01(original.b * multiplier),
                original.a));
        }

        private static void ApplyFloat(Material material, MaterialSnapshot snapshot, string property, float multiplier)
        {
            if (!snapshot.TryGetFloat(property, out float original))
            {
                return;
            }

            material.SetFloat(property, original * multiplier);
        }

        private sealed class MaterialSnapshot
        {
            private static readonly string[] FloatProperties =
            {
                "_Frequency",
                "_Fresnel",
                "_Glossiness",
                "_Shininess",
                "_SpecInt",
                "_WorldUVScale"
            };

            private static readonly string[] ColorProperties =
            {
                "_Colour",
                "_ShineColour",
                "_SpecColor"
            };

            private readonly Dictionary<string, float> floats = new Dictionary<string, float>();
            private readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

            public Material Material { get; }

            private MaterialSnapshot(Material material)
            {
                Material = material;
            }

            public static MaterialSnapshot Capture(Material material)
            {
                MaterialSnapshot snapshot = new MaterialSnapshot(material);
                for (int i = 0; i < FloatProperties.Length; i++)
                {
                    string property = FloatProperties[i];
                    if (material.HasProperty(property))
                    {
                        snapshot.floats[property] = material.GetFloat(property);
                    }
                }

                for (int i = 0; i < ColorProperties.Length; i++)
                {
                    string property = ColorProperties[i];
                    if (material.HasProperty(property))
                    {
                        snapshot.colors[property] = material.GetColor(property);
                    }
                }

                return snapshot;
            }

            public bool TryGetFloat(string property, out float value)
            {
                return floats.TryGetValue(property, out value);
            }

            public bool TryGetColor(string property, out Color value)
            {
                return colors.TryGetValue(property, out value);
            }

            public void Restore()
            {
                foreach (KeyValuePair<string, float> pair in floats)
                {
                    if (Material.HasProperty(pair.Key))
                    {
                        Material.SetFloat(pair.Key, pair.Value);
                    }
                }

                foreach (KeyValuePair<string, Color> pair in colors)
                {
                    if (Material.HasProperty(pair.Key))
                    {
                        Material.SetColor(pair.Key, pair.Value);
                    }
                }
            }
        }
    }
}
