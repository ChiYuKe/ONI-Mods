using UnityEngine;

namespace ONIVisualEnhancer
{
    internal sealed class CameraPostProcessEffect : MonoBehaviour
    {
        private const int PassComposite = 0;
        private const int PassBloomBright = 1;
        private const int PassBloomBlurH = 2;
        private const int PassBloomBlurV = 3;
        private const int PassBloomAdd = 4;

        private Material postProcessMaterial;
        private bool shaderMissingLogged;
        private bool bloomFailed;
        private bool postProcessFailed;

        public static bool ShaderAvailable { get; private set; }

        public static bool ShaderTakesOver { get; private set; }

        private void OnEnable()
        {
            EnsureMaterials();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            if (!VisualEnhancerSettings.CameraPostProcessEnabled || !EnsureMaterials())
            {
                Graphics.Blit(source, destination);
                return;
            }

            try
            {
                RunPostProcess(source, destination);
            }
            catch (System.Exception exception)
            {
                postProcessFailed = true;
                Debug.LogWarning("[ONIVisualEnhancer] Post-process failed and has been disabled for this session: " + exception);
                Graphics.Blit(source, destination);
            }
        }

        private void RunPostProcess(RenderTexture source, RenderTexture destination)
        {
            if (postProcessFailed)
            {
                Graphics.Blit(source, destination);
                return;
            }

            ApplyParameters();

            RenderTexture composite = RenderTexture.GetTemporary(source.width, source.height, 0);
            Graphics.Blit(source, composite, postProcessMaterial, PassComposite);

            if (CanRunBloom())
            {
                TryRunBloom(composite, destination);
            }
            else
            {
                Graphics.Blit(composite, destination);
            }

            RenderTexture.ReleaseTemporary(composite);
        }

        private bool EnsureMaterials()
        {
            if (IsUsable(postProcessMaterial))
            {
                ShaderTakesOver = true;
                return true;
            }

            Shader custom = VisualEnhancerShaderLoader.GetCustomPostProcessShader();
            if (custom != null)
            {
                postProcessMaterial = CreateMaterial(custom);
            }

            ShaderAvailable = IsUsable(postProcessMaterial);
            ShaderTakesOver = ShaderAvailable;
            if (!ShaderAvailable && !shaderMissingLogged)
            {
                Debug.Log("[ONIVisualEnhancer] No custom post-process shader found; falling back to GUI overlay.");
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

        private void ApplyParameters()
        {
            Material m = postProcessMaterial;

            m.SetFloat("_Exposure", VisualEnhancerSettings.Exposure);
            m.SetFloat("_Contrast", VisualEnhancerSettings.Contrast);
            m.SetFloat("_Saturation", VisualEnhancerSettings.Saturation);
            m.SetFloat("_Temperature", VisualEnhancerSettings.Temperature);
            m.SetFloat("_HueShift", VisualEnhancerSettings.HueShift);
            m.SetFloat("_ChromaticAberration", VisualEnhancerSettings.ChromaticAberration);
            m.SetFloat("_LensDistortion", VisualEnhancerSettings.LensDistortion);
            m.SetFloat("_Brightness", VisualEnhancerSettings.Brightness);
            m.SetFloat("_Shadow", VisualEnhancerSettings.Shadow);

            m.SetFloat("_BloomThreshold", 0.85f);
            m.SetFloat("_BloomIntensity", VisualEnhancerSettings.Bloom * 0.25f);
        }

        private bool CanRunBloom()
        {
            return !bloomFailed &&
                VisualEnhancerSettings.Bloom > 0.001f &&
                postProcessMaterial != null &&
                postProcessMaterial.HasProperty("_BloomTex");
        }

        private void TryRunBloom(RenderTexture source, RenderTexture destination)
        {
            try
            {
                RunBloom(source, destination);
            }
            catch (System.Exception exception)
            {
                bloomFailed = true;
                Debug.LogWarning("[ONIVisualEnhancer] Bloom failed and has been disabled for this session: " + exception);
                Graphics.Blit(source, destination);
            }
        }

        private void RunBloom(RenderTexture source, RenderTexture destination)
        {
            int width = Mathf.Max(source.width / 4, 4);
            int height = Mathf.Max(source.height / 4, 4);

            RenderTexture bright = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(source, bright, postProcessMaterial, PassBloomBright);

            RenderTexture blurred = RenderTexture.GetTemporary(width, height, 0);
            RenderTexture scratch = RenderTexture.GetTemporary(width, height, 0);

            int iterations = Mathf.Clamp(Mathf.RoundToInt(VisualEnhancerSettings.Bloom * 3f), 1, 6);
            for (int i = 0; i < iterations; i++)
            {
                Graphics.Blit(i == 0 ? bright : scratch, blurred, postProcessMaterial, PassBloomBlurH);
                Graphics.Blit(blurred, scratch, postProcessMaterial, PassBloomBlurV);
            }

            RenderTexture final = RenderTexture.GetTemporary(source.width, source.height, 0);
            postProcessMaterial.SetTexture("_BloomTex", scratch);
            Graphics.Blit(source, final, postProcessMaterial, PassBloomAdd);
            Graphics.Blit(final, destination);

            RenderTexture.ReleaseTemporary(bright);
            RenderTexture.ReleaseTemporary(blurred);
            RenderTexture.ReleaseTemporary(scratch);
            RenderTexture.ReleaseTemporary(final);
        }

        private void OnDestroy()
        {
            if (postProcessMaterial != null)
            {
                Destroy(postProcessMaterial);
            }
        }
    }
}
