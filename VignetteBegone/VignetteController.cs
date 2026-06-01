using HarmonyLib;
using UnityEngine;

namespace VignetteBegone
{
    public static class VignetteController
    {
        private const string SavedStateKey = "VignetteHidden";
        private const float HiddenAlpha = 0f;
        private const float DefaultAlpha = 0.4705882f;

        public static bool IsHidden { get; private set; }

        public static void LoadSavedState()
        {
            IsHidden = PlayerPrefs.GetInt(SavedStateKey, 1) == 1;
        }

        public static void Toggle()
        {
            if (Vignette.Instance == null)
            {
                Debug.LogWarning("[VignetteBegone] 没有找到 Vignette 实例");
                return;
            }

            SetHidden(!IsHidden);
            SaveState();
        }

        public static void ApplySavedState()
        {
            if (IsHidden && Vignette.Instance != null)
            {
                SetVignetteAlpha(HiddenAlpha);
            }

            VignetteToggleButton.SetState(IsHidden);
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

        private static void SetHidden(bool hidden)
        {
            IsHidden = hidden;

            if (hidden)
            {
                SetVignetteAlpha(HiddenAlpha);
            }
            else
            {
                SetVignetteAlpha(DefaultAlpha);
            }

            VignetteToggleButton.SetState(hidden);
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

        private static void SaveState()
        {
            PlayerPrefs.SetInt(SavedStateKey, IsHidden ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
