using HarmonyLib;
using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class GameVignetteController
    {
        private const float HiddenAlpha = 0f;
        private const float DefaultAlpha = 0.4705882f;

        public static void ApplySavedState()
        {
            SetVignetteAlpha(VisualEnhancerSettings.HideGameVignette ? HiddenAlpha : DefaultAlpha);
        }

        public static void SuppressAlertVignette(Vignette vignette)
        {
            SetVignetteAlpha(HiddenAlpha);
            Traverse.Create(vignette).Field("showingRedAlert").SetValue(false);
            Traverse.Create(vignette).Field("showingYellowAlert").SetValue(false);

            LoopingSounds sounds = vignette.GetComponent<LoopingSounds>();
            if (sounds == null)
            {
                return;
            }

            sounds.StopSound(GlobalAssets.GetSound("RedAlert_LP"));
            sounds.StopSound(GlobalAssets.GetSound("YellowAlert_LP"));
        }

        private static void SetVignetteAlpha(float alpha)
        {
            if (Vignette.Instance == null)
            {
                return;
            }

            Color color = Vignette.Instance.defaultColor;
            color.a = alpha;
            Vignette.Instance.SetColor(color);
        }
    }
}

