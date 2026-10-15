using HarmonyLib;
using UnityEngine;

namespace StorageNetworkTrading.UI
{
    public sealed partial class TradingPanel
    {
        internal static bool TryConsumeCameraZoom(KButtonEvent e)
        {
            if (e == null || e.Consumed ||
                (!e.IsAction(global::Action.ZoomIn) && !e.IsAction(global::Action.ZoomOut)) ||
                !IsPointerOverTradingPanel()) return false;
            e.Consumed = true;
            return true;
        }

        private static bool IsPointerOverTradingPanel()
        {
            if (!IsOpen()) return false;
            // Modal overlays also cover the space outside their dialog window.
            if (instance.modalRoot != null && instance.modalRoot.activeInHierarchy) return true;
            if (instance.tradingRoot == null || !instance.tradingRoot.activeInHierarchy) return false;
            var rect = instance.tradingRoot.GetComponent<RectTransform>();
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, KInputManager.GetMousePos(), camera);
        }
    }

    // Unity UI scroll events and game camera actions use separate dispatchers.
    // Guard the camera as well so interception does not depend on screen ordering
    // or the prerequisite's separate ScrollWheelBlocker hover state.
    [HarmonyPatch(typeof(CameraController), nameof(CameraController.OnKeyDown))]
    internal static class TradingCameraScrollPatch
    {
        private static bool Prefix(KButtonEvent e) => !TradingPanel.TryConsumeCameraZoom(e);
    }
}
