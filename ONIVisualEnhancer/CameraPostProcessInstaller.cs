using UnityEngine;

namespace ONIVisualEnhancer
{
    internal static class CameraPostProcessInstaller
    {
        private static bool targetCameraMissingLogged;

        public static void Install()
        {
            Camera targetCamera = FindFinalCamera();
            if (targetCamera == null)
            {
                if (!targetCameraMissingLogged)
                {
                    Debug.Log("[ONIVisualEnhancer] Final uiCamera is not ready; camera post-process will be installed on the next lifecycle call.");
                    targetCameraMissingLogged = true;
                }

                return;
            }

            targetCameraMissingLogged = false;
            RemoveEffectsFromOtherCameras(targetCamera);

            if (targetCamera.GetComponent<CameraPostProcessEffect>() == null)
            {
                targetCamera.gameObject.AddComponent<CameraPostProcessEffect>();
            }
        }

        private static Camera FindFinalCamera()
        {
            CameraController controller = CameraController.Instance;
            if (controller != null && IsEligible(controller.uiCamera))
            {
                return controller.uiCamera;
            }

            // CameraController is not always initialized when the first lifecycle
            // callback runs. The exact name is safe here; do not fall back to a
            // broad name/depth search because ONI has several auxiliary cameras.
            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && string.Equals(camera.name, "uiCamera", System.StringComparison.OrdinalIgnoreCase) && IsEligible(camera))
                {
                    return camera;
                }
            }

            return null;
        }

        private static bool IsEligible(Camera camera)
        {
            return camera != null && camera.isActiveAndEnabled && camera.targetTexture == null;
        }

        private static void RemoveEffectsFromOtherCameras(Camera targetCamera)
        {
            CameraPostProcessEffect[] effects = Object.FindObjectsOfType<CameraPostProcessEffect>();
            for (int i = 0; i < effects.Length; i++)
            {
                CameraPostProcessEffect effect = effects[i];
                if (effect != null && effect.gameObject != targetCamera.gameObject)
                {
                    Object.Destroy(effect);
                }
            }
        }
    }
}
