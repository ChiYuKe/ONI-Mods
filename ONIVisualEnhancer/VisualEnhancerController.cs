using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class VisualEnhancerController
    {
        private const string RootName = "ONIVisualEnhancerOverlay";

        private static VisualEnhancerOverlay overlay;
        private static VisualEnhancerSettingsWindow settingsWindow;

        public static void EnsureOverlay()
        {
            if (overlay != null)
            {
                overlay.ApplyPreset(VisualEnhancerSettings.GetCurrentPreset());
                EnsureSettingsWindow(overlay.gameObject);
                return;
            }

            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Object.DontDestroyOnLoad(root);
            }

            overlay = root.GetComponent<VisualEnhancerOverlay>();
            if (overlay == null)
            {
                overlay = root.AddComponent<VisualEnhancerOverlay>();
            }

            overlay.ApplyPreset(VisualEnhancerSettings.GetCurrentPreset());
            EnsureSettingsWindow(root);
            CameraPostProcessInstaller.Install();
            MaterialVisualController.ApplySettings();
            GameVignetteController.ApplySavedState();
            VisualEnhancerToggleButton.SetState(VisualEnhancerSettings.GetCurrentPreset());
        }

        public static void ToggleSettingsWindow()
        {
            EnsureOverlay();
            if (settingsWindow != null)
            {
                settingsWindow.Toggle();
            }
        }

        public static void CyclePreset(int direction)
        {
            VisualPreset preset = VisualEnhancerSettings.CyclePreset(direction);
            ApplySettingsChanged();
        }

        public static void ApplySettingsChanged()
        {
            VisualPreset preset = VisualEnhancerSettings.GetCurrentPreset();
            EnsureOverlay();
            overlay.ApplyPreset(preset);
            CameraPostProcessInstaller.Install();
            MaterialVisualController.ApplySettings();
            VisualEnhancerToggleButton.SetState(preset);
            GameVignetteController.ApplySavedState();
        }

        public static void ResetRuntimeState()
        {
            MaterialVisualController.ClearRuntimeState();
            overlay = null;
            settingsWindow = null;
        }

        private static void EnsureSettingsWindow(GameObject root)
        {
            settingsWindow = root.GetComponent<VisualEnhancerSettingsWindow>();
            if (settingsWindow == null)
            {
                settingsWindow = root.AddComponent<VisualEnhancerSettingsWindow>();
            }
        }
    }
}
