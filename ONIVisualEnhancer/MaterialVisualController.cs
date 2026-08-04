using System.Collections.Generic;
using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class MaterialVisualController
    {
        private const string LiquidShader = "Klei/LiquidSubstance";
        private const string SolidShader = "Klei/Substance";

        private static readonly Dictionary<int, MaterialSnapshot> originals = new Dictionary<int, MaterialSnapshot>();
        private static float lastApplyTime = float.MinValue;

        public static void ApplySettings()
        {
            float now = Time.unscaledTime;
            if (now - lastApplyTime < 0.15f)
            {
                return;
            }

            lastApplyTime = now;

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

            switch (material.shader.name)
            {
                case LiquidShader:
                    liquid = true;
                    return true;
                case SolidShader:
                    return true;
                default:
                    return false;
            }
        }

        private static void ApplyMaterial(Material material, MaterialSnapshot snapshot, bool liquid)
        {
            if (liquid)
            {
                ApplyLiquid(material, snapshot);
            }
            else
            {
                ApplySolid(material, snapshot);
            }
        }

        private static void ApplyLiquid(Material material, MaterialSnapshot snapshot)
        {
            ApplyColor(material, snapshot, "_Colour", MaterialMultiplier(VisualEnhancerSettings.LiquidColor));
            ApplyColor(material, snapshot, "_ShineColour", MaterialMultiplier(VisualEnhancerSettings.LiquidShine));
            ApplyColor(material, snapshot, "_SpecColor", MaterialMultiplier(VisualEnhancerSettings.LiquidShine));
            ApplyFloat(material, snapshot, "_Frequency", MaterialMultiplier(VisualEnhancerSettings.LiquidFlow));
            ApplyFloat(material, snapshot, "_Fresnel", MaterialMultiplier(VisualEnhancerSettings.LiquidShine));
            ApplyFloat(material, snapshot, "_Glossiness", MaterialMultiplier(VisualEnhancerSettings.LiquidShine));
            ApplyFloat(material, snapshot, "_Shininess", MaterialMultiplier(VisualEnhancerSettings.LiquidShine));
            ApplyFloat(material, snapshot, "_SpecInt", MaterialMultiplier(VisualEnhancerSettings.LiquidShine));
            ApplyFloat(material, snapshot, "_WorldUVScale", MaterialMultiplier(VisualEnhancerSettings.MaterialTextureScale));
        }

        private static void ApplySolid(Material material, MaterialSnapshot snapshot)
        {
            ApplyColor(material, snapshot, "_ColourTint", MaterialMultiplier(VisualEnhancerSettings.SolidColor));
            ApplyColor(material, snapshot, "_ShineColour", MaterialMultiplier(VisualEnhancerSettings.SolidShine));
            ApplyColor(material, snapshot, "_SpecColor", MaterialMultiplier(VisualEnhancerSettings.SolidShine));
            ApplyFloat(material, snapshot, "_Fresnel", MaterialMultiplier(VisualEnhancerSettings.SolidShine));
            ApplyFloat(material, snapshot, "_Glossiness", MaterialMultiplier(VisualEnhancerSettings.SolidShine));
            ApplyFloat(material, snapshot, "_Shininess", MaterialMultiplier(VisualEnhancerSettings.SolidShine));
            ApplyFloat(material, snapshot, "_SpecInt", MaterialMultiplier(VisualEnhancerSettings.SolidShine));
            ApplyFloat(material, snapshot, "_WorldUVScale", MaterialMultiplier(VisualEnhancerSettings.MaterialTextureScale));
        }

        private static float MaterialMultiplier(float adjustment)
        {
            return 1f + adjustment * 0.15f;
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
                "_ColourTint",
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
