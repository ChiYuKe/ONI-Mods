using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class CameraPostProcessInstaller
    {
        public static void Install()
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            foreach (Camera camera in cameras)
            {
                if (camera == null || !ShouldPatch(camera))
                {
                    continue;
                }

                if (camera.GetComponent<CameraPostProcessEffect>() == null)
                {
                    camera.gameObject.AddComponent<CameraPostProcessEffect>();
                }
            }
        }

        private static bool ShouldPatch(Camera camera)
        {
            if (!camera.isActiveAndEnabled)
            {
                return false;
            }

            string name = camera.name ?? string.Empty;
            if (name.Contains("UI") || name.Contains("Overlay"))
            {
                return false;
            }

            return camera.targetTexture == null;
        }
    }
}
