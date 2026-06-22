using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace WASDMinionControl
{
    [HarmonyPatch(typeof(CameraController), "OnKeyDown")]
    internal static class CameraControllerOnKeyDownPatch
    {
        private static bool Prefix(CameraController __instance, KButtonEvent e)
        {
            ManualControlInput.HandleKeyDown(e);

            if (!WASDMinionInputGuards.IsControlActive())
            {
                return true;
            }

            if (!WASDMinionInputGuards.TryConsumeCameraPan(e))
            {
                return true;
            }

            WASDMinionInputGuards.ClearCameraPanState(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(CameraController), "OnKeyUp")]
    internal static class CameraControllerOnKeyUpPatch
    {
        private static bool Prefix(CameraController __instance, KButtonEvent e)
        {
            ManualControlInput.HandleKeyUp(e);

            if (!WASDMinionInputGuards.IsControlActive())
            {
                return true;
            }

            if (!WASDMinionInputGuards.TryConsumeCameraPan(e))
            {
                return true;
            }

            WASDMinionInputGuards.ClearCameraPanState(__instance);
            return false;
        }
    }

    internal static class WASDMinionInputGuards
    {
        private static readonly AccessTools.FieldRef<CameraController, bool> PanLeft =
            AccessTools.FieldRefAccess<CameraController, bool>("panLeft");

        private static readonly AccessTools.FieldRef<CameraController, bool> PanRight =
            AccessTools.FieldRefAccess<CameraController, bool>("panRight");

        private static readonly AccessTools.FieldRef<CameraController, bool> PanUp =
            AccessTools.FieldRefAccess<CameraController, bool>("panUp");

        private static readonly AccessTools.FieldRef<CameraController, bool> PanDown =
            AccessTools.FieldRefAccess<CameraController, bool>("panDown");

        internal static bool IsControlActive()
        {
            return !IsInputFieldFocused() && WASDMinionController.GetFollowCamDuplicant() != null;
        }

        internal static bool TryConsumeCameraPan(KButtonEvent e)
        {
            return e.TryConsume(global::Action.PanLeft) ||
                   e.TryConsume(global::Action.PanRight) ||
                   e.TryConsume(global::Action.PanUp) ||
                   e.TryConsume(global::Action.PanDown);
        }

        internal static void ClearCameraPanState(CameraController camera)
        {
            if (camera == null)
            {
                return;
            }

            PanLeft(camera) = false;
            PanRight(camera) = false;
            PanUp(camera) = false;
            PanDown(camera) = false;
        }

        internal static bool IsInputFieldFocused()
        {
            global::UnityEngine.EventSystems.EventSystem current = global::UnityEngine.EventSystems.EventSystem.current;
            if (current?.currentSelectedGameObject == null)
            {
                return false;
            }

            GameObject selected = current.currentSelectedGameObject;
            return selected.GetComponent<KInputTextField>() != null ||
                   selected.GetComponent<InputField>() != null;
        }
    }
}
