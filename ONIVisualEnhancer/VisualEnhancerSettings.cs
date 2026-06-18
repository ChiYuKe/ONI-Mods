using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class VisualEnhancerSettings
    {
        private const string PresetKey = "ONIVisualEnhancer.Preset";
        private const string HideGameVignetteKey = "ONIVisualEnhancer.HideGameVignette";
        private const string TintIntensityKey = "ONIVisualEnhancer.TintIntensity";
        private const string VignetteIntensityKey = "ONIVisualEnhancer.VignetteIntensity";
        private const string ScanlineIntensityKey = "ONIVisualEnhancer.ScanlineIntensity";
        private const string GrainIntensityKey = "ONIVisualEnhancer.GrainIntensity";
        private const string BrightnessKey = "ONIVisualEnhancer.Brightness";
        private const string ShadowKey = "ONIVisualEnhancer.Shadow";
        private const string LetterboxKey = "ONIVisualEnhancer.Letterbox";
        private const string ScanlineDensityKey = "ONIVisualEnhancer.ScanlineDensity";
        private const string GrainScaleKey = "ONIVisualEnhancer.GrainScale";
        private const string GrainSpeedKey = "ONIVisualEnhancer.GrainSpeed";
        private const string PulseKey = "ONIVisualEnhancer.Pulse";
        private const string CameraPostProcessKey = "ONIVisualEnhancer.CameraPostProcess";
        private const string ExposureKey = "ONIVisualEnhancer.Exposure";
        private const string ContrastKey = "ONIVisualEnhancer.Contrast";
        private const string SaturationKey = "ONIVisualEnhancer.Saturation";
        private const string TemperatureKey = "ONIVisualEnhancer.Temperature";
        private const string HueShiftKey = "ONIVisualEnhancer.HueShift";
        private const string ChromaticAberrationKey = "ONIVisualEnhancer.ChromaticAberration";
        private const string LensDistortionKey = "ONIVisualEnhancer.LensDistortion";
        private const string BloomKey = "ONIVisualEnhancer.Bloom";
        private const string MaterialAdjustmentsKey = "ONIVisualEnhancer.MaterialAdjustments";
        private const string LiquidColorKey = "ONIVisualEnhancer.LiquidColor";
        private const string LiquidShineKey = "ONIVisualEnhancer.LiquidShine";
        private const string LiquidFlowKey = "ONIVisualEnhancer.LiquidFlow";
        private const string SolidColorKey = "ONIVisualEnhancer.SolidColor";
        private const string SolidShineKey = "ONIVisualEnhancer.SolidShine";
        private const string MaterialTextureScaleKey = "ONIVisualEnhancer.MaterialTextureScale";

        public static VisualPresetMode CurrentMode { get; private set; } = VisualPresetMode.Soft;
        public static bool HideGameVignette { get; private set; } = true;
        public static float TintIntensity { get; private set; } = 1f;
        public static float VignetteIntensity { get; private set; } = 1f;
        public static float ScanlineIntensity { get; private set; } = 1f;
        public static float GrainIntensity { get; private set; } = 1f;
        public static float Brightness { get; private set; } = 1f;
        public static float Shadow { get; private set; } = 0f;
        public static float Letterbox { get; private set; } = 1f;
        public static float ScanlineDensity { get; private set; } = 1f;
        public static float GrainScale { get; private set; } = 1f;
        public static float GrainSpeed { get; private set; } = 1f;
        public static float Pulse { get; private set; } = 0f;
        public static bool CameraPostProcessEnabled { get; private set; } = true;
        public static float Exposure { get; private set; } = 1f;
        public static float Contrast { get; private set; } = 1f;
        public static float Saturation { get; private set; } = 1f;
        public static float Temperature { get; private set; } = 1f;
        public static float HueShift { get; private set; } = 1f;
        public static float ChromaticAberration { get; private set; } = 0f;
        public static float LensDistortion { get; private set; } = 0f;
        public static float Bloom { get; private set; } = 0f;
        public static bool MaterialAdjustmentsEnabled { get; private set; } = true;
        public static float LiquidColor { get; private set; } = 1f;
        public static float LiquidShine { get; private set; } = 1f;
        public static float LiquidFlow { get; private set; } = 1f;
        public static float SolidColor { get; private set; } = 1f;
        public static float SolidShine { get; private set; } = 1f;
        public static float MaterialTextureScale { get; private set; } = 1f;

        public static void Load()
        {
            CurrentMode = (VisualPresetMode)Mathf.Clamp(
                PlayerPrefs.GetInt(PresetKey, (int)VisualPresetMode.Soft),
                (int)VisualPresetMode.Off,
                (int)VisualPresetMode.Terminal);
            HideGameVignette = PlayerPrefs.GetInt(HideGameVignetteKey, 1) == 1;
            TintIntensity = PlayerPrefs.GetFloat(TintIntensityKey, 1f);
            VignetteIntensity = PlayerPrefs.GetFloat(VignetteIntensityKey, 1f);
            ScanlineIntensity = PlayerPrefs.GetFloat(ScanlineIntensityKey, 1f);
            GrainIntensity = PlayerPrefs.GetFloat(GrainIntensityKey, 1f);
            Brightness = PlayerPrefs.GetFloat(BrightnessKey, 1f);
            Shadow = PlayerPrefs.GetFloat(ShadowKey, 0f);
            Letterbox = PlayerPrefs.GetFloat(LetterboxKey, 1f);
            ScanlineDensity = PlayerPrefs.GetFloat(ScanlineDensityKey, 1f);
            GrainScale = PlayerPrefs.GetFloat(GrainScaleKey, 1f);
            GrainSpeed = PlayerPrefs.GetFloat(GrainSpeedKey, 1f);
            Pulse = PlayerPrefs.GetFloat(PulseKey, 0f);
            CameraPostProcessEnabled = PlayerPrefs.GetInt(CameraPostProcessKey, 1) == 1;
            Exposure = PlayerPrefs.GetFloat(ExposureKey, 1f);
            Contrast = PlayerPrefs.GetFloat(ContrastKey, 1f);
            Saturation = PlayerPrefs.GetFloat(SaturationKey, 1f);
            Temperature = PlayerPrefs.GetFloat(TemperatureKey, 1f);
            HueShift = PlayerPrefs.GetFloat(HueShiftKey, 1f);
            ChromaticAberration = PlayerPrefs.GetFloat(ChromaticAberrationKey, 0f);
            LensDistortion = PlayerPrefs.GetFloat(LensDistortionKey, 0f);
            Bloom = PlayerPrefs.GetFloat(BloomKey, 0f);
            MaterialAdjustmentsEnabled = PlayerPrefs.GetInt(MaterialAdjustmentsKey, 1) == 1;
            LiquidColor = PlayerPrefs.GetFloat(LiquidColorKey, 1f);
            LiquidShine = PlayerPrefs.GetFloat(LiquidShineKey, 1f);
            LiquidFlow = PlayerPrefs.GetFloat(LiquidFlowKey, 1f);
            SolidColor = PlayerPrefs.GetFloat(SolidColorKey, 1f);
            SolidShine = PlayerPrefs.GetFloat(SolidShineKey, 1f);
            MaterialTextureScale = PlayerPrefs.GetFloat(MaterialTextureScaleKey, 1f);
            NormalizeIntensities();
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(PresetKey, (int)CurrentMode);
            PlayerPrefs.SetInt(HideGameVignetteKey, HideGameVignette ? 1 : 0);
            PlayerPrefs.SetFloat(TintIntensityKey, TintIntensity);
            PlayerPrefs.SetFloat(VignetteIntensityKey, VignetteIntensity);
            PlayerPrefs.SetFloat(ScanlineIntensityKey, ScanlineIntensity);
            PlayerPrefs.SetFloat(GrainIntensityKey, GrainIntensity);
            PlayerPrefs.SetFloat(BrightnessKey, Brightness);
            PlayerPrefs.SetFloat(ShadowKey, Shadow);
            PlayerPrefs.SetFloat(LetterboxKey, Letterbox);
            PlayerPrefs.SetFloat(ScanlineDensityKey, ScanlineDensity);
            PlayerPrefs.SetFloat(GrainScaleKey, GrainScale);
            PlayerPrefs.SetFloat(GrainSpeedKey, GrainSpeed);
            PlayerPrefs.SetFloat(PulseKey, Pulse);
            PlayerPrefs.SetInt(CameraPostProcessKey, CameraPostProcessEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(ExposureKey, Exposure);
            PlayerPrefs.SetFloat(ContrastKey, Contrast);
            PlayerPrefs.SetFloat(SaturationKey, Saturation);
            PlayerPrefs.SetFloat(TemperatureKey, Temperature);
            PlayerPrefs.SetFloat(HueShiftKey, HueShift);
            PlayerPrefs.SetFloat(ChromaticAberrationKey, ChromaticAberration);
            PlayerPrefs.SetFloat(LensDistortionKey, LensDistortion);
            PlayerPrefs.SetFloat(BloomKey, Bloom);
            PlayerPrefs.SetInt(MaterialAdjustmentsKey, MaterialAdjustmentsEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(LiquidColorKey, LiquidColor);
            PlayerPrefs.SetFloat(LiquidShineKey, LiquidShine);
            PlayerPrefs.SetFloat(LiquidFlowKey, LiquidFlow);
            PlayerPrefs.SetFloat(SolidColorKey, SolidColor);
            PlayerPrefs.SetFloat(SolidShineKey, SolidShine);
            PlayerPrefs.SetFloat(MaterialTextureScaleKey, MaterialTextureScale);
            PlayerPrefs.Save();
        }

        public static void SetPreset(VisualPresetMode mode)
        {
            CurrentMode = (VisualPresetMode)Mathf.Clamp((int)mode, (int)VisualPresetMode.Off, (int)VisualPresetMode.Terminal);
            Save();
        }

        public static void SetHideGameVignette(bool hidden)
        {
            HideGameVignette = hidden;
            Save();
        }

        public static void SetTintIntensity(float value)
        {
            TintIntensity = ClampIntensity(value);
            Save();
        }

        public static void SetVignetteIntensity(float value)
        {
            VignetteIntensity = ClampIntensity(value);
            Save();
        }

        public static void SetScanlineIntensity(float value)
        {
            ScanlineIntensity = ClampIntensity(value);
            Save();
        }

        public static void SetGrainIntensity(float value)
        {
            GrainIntensity = ClampIntensity(value);
            Save();
        }

        public static void SetBrightness(float value)
        {
            Brightness = ClampIntensity(value);
            Save();
        }

        public static void SetShadow(float value)
        {
            Shadow = ClampIntensity(value);
            Save();
        }

        public static void SetLetterbox(float value)
        {
            Letterbox = ClampIntensity(value);
            Save();
        }

        public static void SetScanlineDensity(float value)
        {
            ScanlineDensity = ClampIntensity(value);
            Save();
        }

        public static void SetGrainScale(float value)
        {
            GrainScale = ClampIntensity(value);
            Save();
        }

        public static void SetGrainSpeed(float value)
        {
            GrainSpeed = ClampIntensity(value);
            Save();
        }

        public static void SetPulse(float value)
        {
            Pulse = ClampIntensity(value);
            Save();
        }

        public static void SetCameraPostProcessEnabled(bool enabled)
        {
            CameraPostProcessEnabled = enabled;
            Save();
        }

        public static void SetExposure(float value) { Exposure = ClampIntensity(value); Save(); }
        public static void SetContrast(float value) { Contrast = ClampIntensity(value); Save(); }
        public static void SetSaturation(float value) { Saturation = ClampIntensity(value); Save(); }
        public static void SetTemperature(float value) { Temperature = ClampIntensity(value); Save(); }
        public static void SetHueShift(float value) { HueShift = ClampIntensity(value); Save(); }
        public static void SetChromaticAberration(float value) { ChromaticAberration = ClampIntensity(value); Save(); }
        public static void SetLensDistortion(float value) { LensDistortion = ClampIntensity(value); Save(); }
        public static void SetBloom(float value) { Bloom = ClampIntensity(value); Save(); }
        public static void SetMaterialAdjustmentsEnabled(bool enabled) { MaterialAdjustmentsEnabled = enabled; Save(); }
        public static void SetLiquidColor(float value) { LiquidColor = ClampIntensity(value); Save(); }
        public static void SetLiquidShine(float value) { LiquidShine = ClampIntensity(value); Save(); }
        public static void SetLiquidFlow(float value) { LiquidFlow = ClampIntensity(value); Save(); }
        public static void SetSolidColor(float value) { SolidColor = ClampIntensity(value); Save(); }
        public static void SetSolidShine(float value) { SolidShine = ClampIntensity(value); Save(); }
        public static void SetMaterialTextureScale(float value) { MaterialTextureScale = ClampIntensity(value); Save(); }

        public static VisualPreset CyclePreset(int direction)
        {
            int next = (int)CurrentMode + direction;
            if (next > (int)VisualPresetMode.Terminal)
            {
                next = (int)VisualPresetMode.Off;
            }
            else if (next < (int)VisualPresetMode.Off)
            {
                next = (int)VisualPresetMode.Terminal;
            }

            CurrentMode = (VisualPresetMode)next;
            Save();
            return GetCurrentPreset();
        }

        public static VisualPreset GetCurrentPreset()
        {
            switch (CurrentMode)
            {
                case VisualPresetMode.Off:
                    return new VisualPreset(CurrentMode, "Off", Clear(), Clear(), 0f, 0f, 0f);
                case VisualPresetMode.Cinematic:
                    return new VisualPreset(CurrentMode, "Cinematic", new Color(0.18f, 0.105f, 0.035f, 0.34f), new Color(0f, 0f, 0f, 1f), 0.64f, 0.085f, 0.055f);
                case VisualPresetMode.CoolSpace:
                    return new VisualPreset(CurrentMode, "Cool Space", new Color(0.015f, 0.10f, 0.28f, 0.38f), new Color(0f, 0.01f, 0.08f, 1f), 0.52f, 0.07f, 0.045f);
                case VisualPresetMode.WarmColony:
                    return new VisualPreset(CurrentMode, "Warm Colony", new Color(0.34f, 0.145f, 0.025f, 0.34f), new Color(0.14f, 0.04f, 0f, 1f), 0.46f, 0.04f, 0.035f);
                case VisualPresetMode.Noir:
                    return new VisualPreset(CurrentMode, "Noir", new Color(0.015f, 0.015f, 0.02f, 0.48f), new Color(0f, 0f, 0f, 1f), 0.74f, 0.13f, 0.075f);
                case VisualPresetMode.Toxic:
                    return new VisualPreset(CurrentMode, "Toxic", new Color(0.045f, 0.22f, 0.055f, 0.36f), new Color(0f, 0.05f, 0.01f, 1f), 0.52f, 0.08f, 0.05f);
                case VisualPresetMode.RetroFilm:
                    return new VisualPreset(CurrentMode, "Retro Film", new Color(0.26f, 0.16f, 0.055f, 0.32f), new Color(0.08f, 0.035f, 0f, 1f), 0.5f, 0.12f, 0.11f);
                case VisualPresetMode.DeepSea:
                    return new VisualPreset(CurrentMode, "Deep Sea", new Color(0f, 0.12f, 0.20f, 0.42f), new Color(0f, 0.025f, 0.08f, 1f), 0.62f, 0.045f, 0.035f);
                case VisualPresetMode.Magma:
                    return new VisualPreset(CurrentMode, "Magma", new Color(0.42f, 0.095f, 0.005f, 0.38f), new Color(0.18f, 0.02f, 0f, 1f), 0.54f, 0.055f, 0.04f);
                case VisualPresetMode.Frost:
                    return new VisualPreset(CurrentMode, "Frost", new Color(0.12f, 0.22f, 0.34f, 0.36f), new Color(0f, 0.04f, 0.10f, 1f), 0.44f, 0.03f, 0.028f);
                case VisualPresetMode.Dream:
                    return new VisualPreset(CurrentMode, "Dream", new Color(0.20f, 0.08f, 0.26f, 0.30f), new Color(0.08f, 0.015f, 0.10f, 1f), 0.36f, 0.018f, 0.018f);
                case VisualPresetMode.Terminal:
                    return new VisualPreset(CurrentMode, "Terminal", new Color(0f, 0.30f, 0.055f, 0.34f), new Color(0f, 0.08f, 0.01f, 1f), 0.58f, 0.16f, 0.04f);
                default:
                    return new VisualPreset(VisualPresetMode.Soft, "Soft", new Color(0.045f, 0.06f, 0.085f, 0.20f), new Color(0f, 0f, 0f, 1f), 0.34f, 0.04f, 0.028f);
            }
        }

        private static Color Clear()
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        private static void NormalizeIntensities()
        {
            TintIntensity = ClampIntensity(TintIntensity);
            VignetteIntensity = ClampIntensity(VignetteIntensity);
            ScanlineIntensity = ClampIntensity(ScanlineIntensity);
            GrainIntensity = ClampIntensity(GrainIntensity);
            Brightness = ClampIntensity(Brightness);
            Shadow = ClampIntensity(Shadow);
            Letterbox = ClampIntensity(Letterbox);
            ScanlineDensity = ClampIntensity(ScanlineDensity);
            GrainScale = ClampIntensity(GrainScale);
            GrainSpeed = ClampIntensity(GrainSpeed);
            Pulse = ClampIntensity(Pulse);
            Exposure = ClampIntensity(Exposure);
            Contrast = ClampIntensity(Contrast);
            Saturation = ClampIntensity(Saturation);
            Temperature = ClampIntensity(Temperature);
            HueShift = ClampIntensity(HueShift);
            ChromaticAberration = ClampIntensity(ChromaticAberration);
            LensDistortion = ClampIntensity(LensDistortion);
            Bloom = ClampIntensity(Bloom);
            LiquidColor = ClampIntensity(LiquidColor);
            LiquidShine = ClampIntensity(LiquidShine);
            LiquidFlow = ClampIntensity(LiquidFlow);
            SolidColor = ClampIntensity(SolidColor);
            SolidShine = ClampIntensity(SolidShine);
            MaterialTextureScale = ClampIntensity(MaterialTextureScale);
        }

        private static float ClampIntensity(float value)
        {
            return Mathf.Clamp(value, 0f, 2f);
        }
    }
}
