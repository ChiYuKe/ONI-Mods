using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class VisualEnhancerSettings
    {
        private const string HideGameVignetteKey = "ONIVisualEnhancer.HideGameVignette";
        private const string BrightnessKey = "ONIVisualEnhancer.Brightness";
        private const string ShadowKey = "ONIVisualEnhancer.Shadow";
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
        private const string ParameterModelVersionKey = "ONIVisualEnhancer.ParameterModelVersion";
        private const int ParameterModelVersion = 2;

        public static bool HideGameVignette { get; private set; } = true;
        public static float Brightness { get; private set; } = 0f;
        public static float Shadow { get; private set; } = 0f;
        public static bool CameraPostProcessEnabled { get; private set; } = true;
        public static float Exposure { get; private set; } = 0f;
        public static float Contrast { get; private set; } = 0f;
        public static float Saturation { get; private set; } = 0f;
        public static float Temperature { get; private set; } = 0f;
        public static float HueShift { get; private set; } = 0f;
        public static float ChromaticAberration { get; private set; } = 0f;
        public static float LensDistortion { get; private set; } = 0f;
        public static float Bloom { get; private set; } = 0f;
        public static bool MaterialAdjustmentsEnabled { get; private set; } = true;
        public static float LiquidColor { get; private set; } = 0f;
        public static float LiquidShine { get; private set; } = 0f;
        public static float LiquidFlow { get; private set; } = 0f;
        public static float SolidColor { get; private set; } = 0f;
        public static float SolidShine { get; private set; } = 0f;
        public static float MaterialTextureScale { get; private set; } = 0f;

        public static void Load()
        {
            HideGameVignette = PlayerPrefs.GetInt(HideGameVignetteKey, 1) == 1;
            Brightness = PlayerPrefs.GetFloat(BrightnessKey, 0f);
            Shadow = PlayerPrefs.GetFloat(ShadowKey, 0f);
            CameraPostProcessEnabled = PlayerPrefs.GetInt(CameraPostProcessKey, 1) == 1;
            Exposure = PlayerPrefs.GetFloat(ExposureKey, 0f);
            Contrast = PlayerPrefs.GetFloat(ContrastKey, 0f);
            Saturation = PlayerPrefs.GetFloat(SaturationKey, 0f);
            Temperature = PlayerPrefs.GetFloat(TemperatureKey, 0f);
            HueShift = PlayerPrefs.GetFloat(HueShiftKey, 0f);
            ChromaticAberration = PlayerPrefs.GetFloat(ChromaticAberrationKey, 0f);
            LensDistortion = PlayerPrefs.GetFloat(LensDistortionKey, 0f);
            Bloom = PlayerPrefs.GetFloat(BloomKey, 0f);
            MaterialAdjustmentsEnabled = PlayerPrefs.GetInt(MaterialAdjustmentsKey, 1) == 1;
            LiquidColor = PlayerPrefs.GetFloat(LiquidColorKey, 0f);
            LiquidShine = PlayerPrefs.GetFloat(LiquidShineKey, 0f);
            LiquidFlow = PlayerPrefs.GetFloat(LiquidFlowKey, 0f);
            SolidColor = PlayerPrefs.GetFloat(SolidColorKey, 0f);
            SolidShine = PlayerPrefs.GetFloat(SolidShineKey, 0f);
            MaterialTextureScale = PlayerPrefs.GetFloat(MaterialTextureScaleKey, 0f);

            MigrateLegacyParameterValues();
            NormalizeIntensities();
            Save();
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(HideGameVignetteKey, HideGameVignette ? 1 : 0);
            PlayerPrefs.SetFloat(BrightnessKey, Brightness);
            PlayerPrefs.SetFloat(ShadowKey, Shadow);
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
            PlayerPrefs.SetInt(ParameterModelVersionKey, ParameterModelVersion);
            PlayerPrefs.Save();
        }

        public static void SetHideGameVignette(bool hidden)
        {
            HideGameVignette = hidden;
            Save();
        }

        public static void SetBrightness(float value)
        {
            Brightness = Clamp(value, -1f, 1f);
            Save();
        }

        public static void SetShadow(float value)
        {
            Shadow = Clamp(value, 0f, 1f);
            Save();
        }

        public static void SetCameraPostProcessEnabled(bool enabled)
        {
            CameraPostProcessEnabled = enabled;
            Save();
        }

        public static void SetExposure(float value) { Exposure = Clamp(value, -1f, 1f); Save(); }
        public static void SetContrast(float value) { Contrast = Clamp(value, -1f, 1f); Save(); }
        public static void SetSaturation(float value) { Saturation = Clamp(value, -1f, 1f); Save(); }
        public static void SetTemperature(float value) { Temperature = Clamp(value, -1f, 1f); Save(); }
        public static void SetHueShift(float value) { HueShift = Clamp(value, -1f, 1f); Save(); }
        public static void SetChromaticAberration(float value) { ChromaticAberration = Clamp(value, 0f, 1f); Save(); }
        public static void SetLensDistortion(float value) { LensDistortion = Clamp(value, -1f, 1f); Save(); }
        public static void SetBloom(float value) { Bloom = Clamp(value, 0f, 1f); Save(); }
        public static void SetMaterialAdjustmentsEnabled(bool enabled) { MaterialAdjustmentsEnabled = enabled; Save(); }
        public static void SetLiquidColor(float value) { LiquidColor = Clamp(value, -1f, 1f); Save(); }
        public static void SetLiquidShine(float value) { LiquidShine = Clamp(value, -1f, 1f); Save(); }
        public static void SetLiquidFlow(float value) { LiquidFlow = Clamp(value, -1f, 1f); Save(); }
        public static void SetSolidColor(float value) { SolidColor = Clamp(value, -1f, 1f); Save(); }
        public static void SetSolidShine(float value) { SolidShine = Clamp(value, -1f, 1f); Save(); }
        public static void SetMaterialTextureScale(float value) { MaterialTextureScale = Clamp(value, -1f, 1f); Save(); }

        private static void NormalizeIntensities()
        {
            Brightness = Clamp(Brightness, -1f, 1f);
            Shadow = Clamp(Shadow, 0f, 1f);
            Exposure = Clamp(Exposure, -1f, 1f);
            Contrast = Clamp(Contrast, -1f, 1f);
            Saturation = Clamp(Saturation, -1f, 1f);
            Temperature = Clamp(Temperature, -1f, 1f);
            HueShift = Clamp(HueShift, -1f, 1f);
            ChromaticAberration = Clamp(ChromaticAberration, 0f, 1f);
            LensDistortion = Clamp(LensDistortion, -1f, 1f);
            Bloom = Clamp(Bloom, 0f, 1f);
            LiquidColor = Clamp(LiquidColor, -1f, 1f);
            LiquidShine = Clamp(LiquidShine, -1f, 1f);
            LiquidFlow = Clamp(LiquidFlow, -1f, 1f);
            SolidColor = Clamp(SolidColor, -1f, 1f);
            SolidShine = Clamp(SolidShine, -1f, 1f);
            MaterialTextureScale = Clamp(MaterialTextureScale, -1f, 1f);
        }

        private static void MigrateLegacyParameterValues()
        {
            if (PlayerPrefs.GetInt(ParameterModelVersionKey, 0) >= ParameterModelVersion)
            {
                return;
            }

            bool hasLegacyValues = PlayerPrefs.HasKey(BrightnessKey)
                || PlayerPrefs.HasKey(ExposureKey)
                || PlayerPrefs.HasKey(ContrastKey)
                || PlayerPrefs.HasKey(SaturationKey)
                || PlayerPrefs.HasKey(TemperatureKey)
                || PlayerPrefs.HasKey(HueShiftKey)
                || PlayerPrefs.HasKey(LiquidColorKey)
                || PlayerPrefs.HasKey(LiquidShineKey)
                || PlayerPrefs.HasKey(LiquidFlowKey)
                || PlayerPrefs.HasKey(SolidColorKey)
                || PlayerPrefs.HasKey(SolidShineKey)
                || PlayerPrefs.HasKey(MaterialTextureScaleKey);

            if (!hasLegacyValues)
            {
                return;
            }

            // Version 1 stored multiplicative values where 1.0 meant neutral.
            Brightness = LegacyCenteredValue(BrightnessKey, Brightness);
            Exposure = LegacyCenteredValue(ExposureKey, Exposure);
            Contrast = LegacyCenteredValue(ContrastKey, Contrast);
            Saturation = LegacyCenteredValue(SaturationKey, Saturation);
            Temperature = LegacyCenteredValue(TemperatureKey, Temperature);
            HueShift = LegacyCenteredValue(HueShiftKey, HueShift);
            LiquidColor = LegacyCenteredValue(LiquidColorKey, LiquidColor);
            LiquidShine = LegacyCenteredValue(LiquidShineKey, LiquidShine);
            LiquidFlow = LegacyCenteredValue(LiquidFlowKey, LiquidFlow);
            SolidColor = LegacyCenteredValue(SolidColorKey, SolidColor);
            SolidShine = LegacyCenteredValue(SolidShineKey, SolidShine);
            MaterialTextureScale = LegacyCenteredValue(MaterialTextureScaleKey, MaterialTextureScale);
        }

        private static float LegacyCenteredValue(string key, float value)
        {
            return PlayerPrefs.HasKey(key) ? value - 1f : value;
        }

        private static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }
    }
}
