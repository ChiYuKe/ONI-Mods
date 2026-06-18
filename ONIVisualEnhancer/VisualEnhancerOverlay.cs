using UnityEngine;

namespace ONIVisualEnhancer
{
    internal sealed class VisualEnhancerOverlay : MonoBehaviour
    {
        private Texture2D pixel;
        private Texture2D vignetteTexture;
        private Texture2D scanlineTexture;
        private Texture2D grainTexture;
        private VisualPreset preset;
        private float grainOffset;
        private float pulseTime;

        public void ApplyPreset(VisualPreset nextPreset)
        {
            preset = nextPreset;
            enabled = preset.Mode != VisualPresetMode.Off || VisualEnhancerSettings.HideGameVignette;
        }

        private void Awake()
        {
            pixel = CreatePixelTexture(Color.white);
            scanlineTexture = CreateScanlineTexture();
            grainTexture = CreateGrainTexture();
            vignetteTexture = CreateVignetteTexture(256);
            preset = VisualEnhancerSettings.GetCurrentPreset();
            enabled = preset.Mode != VisualPresetMode.Off;
        }

        private void Update()
        {
            grainOffset += Time.unscaledDeltaTime * 0.11f * VisualEnhancerSettings.GrainSpeed;
            if (grainOffset > 1f)
            {
                grainOffset -= 1f;
            }

            pulseTime += Time.unscaledDeltaTime;
        }

        private void OnGUI()
        {
            if (preset == null || preset.Mode == VisualPresetMode.Off || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.depth = int.MaxValue;
            DrawBrightness(screen);
            DrawTint(screen);
            DrawCinematicBars(screen);
            DrawVignette(screen);
            DrawScanlines(screen);
            DrawGrain(screen);
            DrawShadow(screen);
        }

        private void DrawBrightness(Rect screen)
        {
            float brightness = VisualEnhancerSettings.Brightness;
            if (Mathf.Abs(brightness - 1f) <= 0.001f)
            {
                return;
            }

            Color oldColor = GUI.color;
            if (brightness > 1f)
            {
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((brightness - 1f) * 0.25f));
            }
            else
            {
                GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01((1f - brightness) * 0.45f));
            }

            GUI.DrawTexture(screen, pixel);
            GUI.color = oldColor;
        }

        private void DrawTint(Rect screen)
        {
            float alpha = preset.Tint.a * VisualEnhancerSettings.TintIntensity;
            if (alpha <= 0f)
            {
                return;
            }

            Color oldColor = GUI.color;
            Color color = preset.Tint;
            float pulse = 1f + Mathf.Sin(pulseTime * 2.4f) * 0.22f * VisualEnhancerSettings.Pulse;
            color.a = Mathf.Clamp01(alpha * pulse);
            GUI.color = color;
            GUI.DrawTexture(screen, pixel);
            GUI.color = oldColor;
        }

        private void DrawShadow(Rect screen)
        {
            float strength = VisualEnhancerSettings.Shadow;
            if (strength <= 0f)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(strength * 0.38f));
            GUI.DrawTexture(screen, pixel);
            GUI.color = oldColor;
        }

        private void DrawVignette(Rect screen)
        {
            float strength = preset.VignetteStrength * VisualEnhancerSettings.VignetteIntensity;
            if (strength <= 0f)
            {
                return;
            }

            Color oldColor = GUI.color;
            Color color = preset.Vignette;
            color.a = Mathf.Clamp01(strength);
            GUI.color = color;
            GUI.DrawTexture(screen, vignetteTexture, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
        }

        private void DrawCinematicBars(Rect screen)
        {
            if (preset.Mode != VisualPresetMode.Cinematic &&
                preset.Mode != VisualPresetMode.Noir &&
                preset.Mode != VisualPresetMode.RetroFilm &&
                preset.Mode != VisualPresetMode.Terminal)
            {
                return;
            }

            float height = preset.Mode == VisualPresetMode.Noir || preset.Mode == VisualPresetMode.Terminal
                ? Screen.height * 0.055f
                : Screen.height * 0.035f;
            height *= VisualEnhancerSettings.Letterbox;
            if (height <= 0.5f)
            {
                return;
            }

            Color oldColor = GUI.color;
            float alpha = (preset.Mode == VisualPresetMode.Noir || preset.Mode == VisualPresetMode.Terminal ? 0.58f : 0.42f) * VisualEnhancerSettings.Letterbox;
            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, height), pixel);
            GUI.DrawTexture(new Rect(0f, Screen.height - height, Screen.width, height), pixel);
            GUI.color = oldColor;
        }

        private void DrawScanlines(Rect screen)
        {
            float strength = preset.ScanlineStrength * VisualEnhancerSettings.ScanlineIntensity;
            if (strength <= 0f)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(strength));
            float density = Mathf.Lerp(0.5f, 2.5f, VisualEnhancerSettings.ScanlineDensity * 0.5f);
            GUI.DrawTextureWithTexCoords(screen, scanlineTexture, new Rect(0f, 0f, Screen.width / 2f, Screen.height / 4f * density));
            GUI.color = oldColor;
        }

        private void DrawGrain(Rect screen)
        {
            float strength = preset.GrainStrength * VisualEnhancerSettings.GrainIntensity;
            if (strength <= 0f)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(strength));
            float scale = Mathf.Lerp(1.8f, 0.35f, VisualEnhancerSettings.GrainScale * 0.5f);
            GUI.DrawTextureWithTexCoords(screen, grainTexture, new Rect(grainOffset, grainOffset * 0.5f, Screen.width / 96f * scale, Screen.height / 96f * scale));
            GUI.color = oldColor;
        }

        private static Texture2D CreatePixelTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateScanlineTexture()
        {
            Texture2D texture = new Texture2D(2, 4, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    texture.SetPixel(x, y, y == 0 ? Color.white : Color.clear);
                }
            }

            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateGrainTexture()
        {
            Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            uint state = 2166136261u;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    state ^= (uint)(x + y * texture.width);
                    state *= 16777619u;
                    float value = (state & 255u) / 255f;
                    texture.SetPixel(x, y, new Color(value, value, value, 1f));
                }
            }

            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateVignetteTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (size - 1f) - 0.5f) * 2f;
                    float ny = (y / (size - 1f) - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.25f, distance));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return texture;
        }
    }
}
