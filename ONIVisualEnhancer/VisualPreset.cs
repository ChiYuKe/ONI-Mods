using UnityEngine;

namespace ONIVisualEnhancer
{
    internal sealed class VisualPreset
    {
        public VisualPresetMode Mode { get; }
        public string Name { get; }
        public Color Tint { get; }
        public Color Vignette { get; }
        public float VignetteStrength { get; }
        public float ScanlineStrength { get; }
        public float GrainStrength { get; }

        public VisualPreset(
            VisualPresetMode mode,
            string name,
            Color tint,
            Color vignette,
            float vignetteStrength,
            float scanlineStrength,
            float grainStrength)
        {
            Mode = mode;
            Name = name;
            Tint = tint;
            Vignette = vignette;
            VignetteStrength = vignetteStrength;
            ScanlineStrength = scanlineStrength;
            GrainStrength = grainStrength;
        }
    }

    internal enum VisualPresetMode
    {
        Off,
        Soft,
        Cinematic,
        CoolSpace,
        WarmColony,
        Noir,
        Toxic,
        RetroFilm,
        DeepSea,
        Magma,
        Frost,
        Dream,
        Terminal
    }
}
