using UnityEngine;

namespace ONIVisualEnhancer
{
    internal sealed class CameraPostProcessEffect : MonoBehaviour
    {
        private Material customMaterial;
        private Material hueSaturationMaterial;
        private Material bloomMaskMaterial;
        private Material bloomCompositeMaterial;
        private Material blurMaterial;
        private bool shaderMissingLogged;
        private bool bloomFailed;

        public static bool ShaderAvailable { get; private set; }

        private void OnEnable()
        {
            EnsureMaterials();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!VisualEnhancerSettings.CameraPostProcessEnabled || !EnsureMaterials())
            {
                Graphics.Blit(source, destination);
                return;
            }

            RenderTexture current = source;
            RenderTexture hueTarget = null;
            bool releaseCurrent = false;

            if (hueSaturationMaterial != null && ShouldRunHueSaturation())
            {
                ApplyHueSaturation(hueSaturationMaterial);
                hueTarget = RenderTexture.GetTemporary(source.width, source.height, 0);
                Graphics.Blit(current, hueTarget, hueSaturationMaterial);
                current = hueTarget;
                releaseCurrent = true;
            }

            if (customMaterial != null && ShouldRunCustomShader())
            {
                ApplyCustomSettings(customMaterial);
                RenderTexture customTarget = RenderTexture.GetTemporary(source.width, source.height, 0);
                Graphics.Blit(current, customTarget, customMaterial);
                if (releaseCurrent)
                {
                    RenderTexture.ReleaseTemporary(current);
                }

                current = customTarget;
                releaseCurrent = true;
            }

            if (CanRunBloom())
            {
                if (!TryRunBloom(current, destination))
                {
                    Graphics.Blit(current, destination);
                }
            }
            else
            {
                Graphics.Blit(current, destination);
            }

            if (releaseCurrent)
            {
                RenderTexture.ReleaseTemporary(current);
            }
        }

        private bool EnsureMaterials()
        {
            if (IsUsable(hueSaturationMaterial) || IsUsable(customMaterial) || IsUsable(bloomCompositeMaterial))
            {
                return true;
            }

            Shader hueSaturation = VisualEnhancerShaderLoader.Find("Klei/PostFX/HueSaturation");
            if (hueSaturation != null)
            {
                hueSaturationMaterial = CreateMaterial(hueSaturation);
            }

            Shader bloomMask = VisualEnhancerShaderLoader.Find("Klei/PostFX/BloomMask");
            Shader bloomComposite = VisualEnhancerShaderLoader.Find("Klei/PostFX/BloomComposite");
            Shader blur = VisualEnhancerShaderLoader.Find("Klei/PostFX/Blur");
            if (bloomMask != null && bloomComposite != null && blur != null)
            {
                bloomMaskMaterial = CreateMaterial(bloomMask);
                bloomCompositeMaterial = CreateMaterial(bloomComposite);
                blurMaterial = CreateMaterial(blur);
            }

            Shader custom = VisualEnhancerShaderLoader.GetCustomPostProcessShader();
            if (custom != null)
            {
                customMaterial = CreateMaterial(custom);
            }

            ShaderAvailable = IsUsable(hueSaturationMaterial) || IsUsable(bloomCompositeMaterial) || IsUsable(customMaterial);
            if (!ShaderAvailable && !shaderMissingLogged)
            {
                Debug.Log("[ONIVisualEnhancer] No usable camera post-process shaders were found; using overlay fallback.");
                shaderMissingLogged = true;
            }

            return ShaderAvailable;
        }

        private static Material CreateMaterial(Shader shader)
        {
            Material material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
            return material;
        }

        private static bool IsUsable(Material material)
        {
            return material != null && material.shader != null && material.shader.isSupported;
        }

        private static bool ShouldRunHueSaturation()
        {
            return Mathf.Abs(VisualEnhancerSettings.HueShift - 1f) > 0.001f ||
                Mathf.Abs(VisualEnhancerSettings.Saturation - 1f) > 0.001f;
        }

        private static bool ShouldRunCustomShader()
        {
            return Mathf.Abs(VisualEnhancerSettings.Exposure - 1f) > 0.001f ||
                Mathf.Abs(VisualEnhancerSettings.Contrast - 1f) > 0.001f ||
                Mathf.Abs(VisualEnhancerSettings.Temperature - 1f) > 0.001f ||
                VisualEnhancerSettings.ChromaticAberration > 0.001f ||
                VisualEnhancerSettings.LensDistortion > 0.001f;
        }

        private static void ApplyHueSaturation(Material target)
        {
            target.SetFloat("_Hue", (VisualEnhancerSettings.HueShift - 1f) * 0.5f);
            target.SetFloat("_Saturation", VisualEnhancerSettings.Saturation);
        }

        private static void ApplyCustomSettings(Material target)
        {
            target.SetFloat("_Exposure", VisualEnhancerSettings.Exposure);
            target.SetFloat("_Contrast", VisualEnhancerSettings.Contrast);
            target.SetFloat("_Temperature", VisualEnhancerSettings.Temperature);
            target.SetFloat("_ChromaticAberration", VisualEnhancerSettings.ChromaticAberration);
            target.SetFloat("_LensDistortion", VisualEnhancerSettings.LensDistortion);
        }

        private bool CanRunBloom()
        {
            return !bloomFailed &&
                VisualEnhancerSettings.Bloom > 0.001f &&
                IsUsable(bloomMaskMaterial) &&
                IsUsable(bloomCompositeMaterial) &&
                IsUsable(blurMaterial);
        }

        private bool TryRunBloom(RenderTexture source, RenderTexture destination)
        {
            if (source == null || destination == null)
            {
                return false;
            }

            try
            {
                RunBloom(source, destination);
                return true;
            }
            catch (System.Exception exception)
            {
                bloomFailed = true;
                Debug.LogWarning("[ONIVisualEnhancer] Bloom failed and has been disabled for this session: " + exception);
                return false;
            }
        }

        private void RunBloom(RenderTexture source, RenderTexture destination)
        {
            RenderTexture temporary = null;
            RenderTexture blurred = null;

            try
            {
                temporary = RenderTexture.GetTemporary(source.width, source.height, 0);
                Graphics.Blit(source, temporary, bloomMaskMaterial);

                int width = Mathf.Max(source.width / 4, 4);
                int height = Mathf.Max(source.height / 4, 4);
                blurred = RenderTexture.GetTemporary(width, height, 0);
                DownSample4x(temporary, blurred);
                RenderTexture.ReleaseTemporary(temporary);
                temporary = null;

                int iterations = Mathf.Clamp(Mathf.RoundToInt(VisualEnhancerSettings.Bloom * 3f), 1, 6);
                for (int i = 0; i < iterations; i++)
                {
                    RenderTexture next = RenderTexture.GetTemporary(width, height, 0);
                    FourTapCone(blurred, next, i);
                    RenderTexture.ReleaseTemporary(blurred);
                    blurred = next;
                }

                bloomCompositeMaterial.SetTexture("_BloomTex", blurred);
                Graphics.Blit(source, destination, bloomCompositeMaterial);
            }
            finally
            {
                if (temporary != null)
                {
                    RenderTexture.ReleaseTemporary(temporary);
                }

                if (blurred != null)
                {
                    RenderTexture.ReleaseTemporary(blurred);
                }
            }
        }

        private void DownSample4x(RenderTexture source, RenderTexture destination)
        {
            const float offset = 1f;
            Graphics.BlitMultiTap(
                source,
                destination,
                blurMaterial,
                new Vector2(-offset, -offset),
                new Vector2(-offset, offset),
                new Vector2(offset, offset),
                new Vector2(offset, -offset));
        }

        private void FourTapCone(RenderTexture source, RenderTexture destination, int iteration)
        {
            float offset = 0.5f + iteration * Mathf.Lerp(0.2f, 1.0f, VisualEnhancerSettings.Bloom * 0.5f);
            Graphics.BlitMultiTap(
                source,
                destination,
                blurMaterial,
                new Vector2(-offset, -offset),
                new Vector2(-offset, offset),
                new Vector2(offset, offset),
                new Vector2(offset, -offset));
        }

        private void OnDestroy()
        {
            DestroyMaterial(customMaterial);
            DestroyMaterial(hueSaturationMaterial);
            DestroyMaterial(bloomMaskMaterial);
            DestroyMaterial(bloomCompositeMaterial);
            DestroyMaterial(blurMaterial);
        }

        private static void DestroyMaterial(Material material)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
