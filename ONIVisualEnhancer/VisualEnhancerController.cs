using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class VisualEnhancerController
    {
        private const string RootName = "ONIVisualEnhancerRoot";

        private static VisualEnhancerSettingsWindow settingsWindow;

        public static void EnsureOverlay()
        {
            if (settingsWindow != null)
            {
                return;
            }

            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Object.DontDestroyOnLoad(root);
            }

            EnsureSettingsWindow(root);
            CameraPostProcessInstaller.Install();
            MaterialVisualController.ApplySettings();
            GameVignetteController.ApplySavedState();
        }

        public static void ToggleSettingsWindow()
        {
            EnsureOverlay();
            if (settingsWindow != null)
            {
                settingsWindow.Toggle();
            }
        }

        public static void ApplySettingsChanged()
        {
            EnsureOverlay();
            CameraPostProcessInstaller.Install();
            MaterialVisualController.ApplySettings();
            GameVignetteController.ApplySavedState();
        }

        public static void ResetRuntimeState()
        {
            MaterialVisualController.ClearRuntimeState();
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
