using System.Reflection;
using HarmonyLib;
using StorageNetwork.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkPanelInputPatch
    {
        [HarmonyPatch]
        public static class TMPInputFieldCaretPositionPatch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(TMP_InputField), "AssignPositioningIfNeeded");
            }

            public static bool Prefix(TMP_InputField __instance, RectTransform ___caretRectTrans, TMP_Text ___m_TextComponent)
            {
                try
                {
                    if (StorageNetworkInputPatchSupport.TryStartCaretResize(__instance, ___caretRectTrans, ___m_TextComponent))
                    {
                        return false;
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to align input caret: " + exception);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(TMP_InputField), "OnEnable")]
        public static class TMPInputFieldRectMaskPatch
        {
            public static void Postfix(TMP_Text ___m_TextComponent, Scrollbar ___m_VerticalScrollbar)
            {
                try
                {
                    StorageNetworkInputPatchSupport.ApplyRectMaskFix(___m_TextComponent, ___m_VerticalScrollbar);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to apply TMP rect mask fix: " + exception);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.OnKeyDown))]
        public static class PlayerControllerOnKeyDownPatch
        {
            public static bool Prefix(KButtonEvent e)
            {
                if (StorageNetworkPanel.IsTextInputFocused())
                {
                    e.Consumed = true;
                    return false;
                }

                return !ConsumeStorageNetworkRightClick(e);
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.OnKeyUp))]
        public static class PlayerControllerOnKeyUpPatch
        {
            public static bool Prefix(KButtonEvent e)
            {
                if (StorageNetworkPanel.IsTextInputFocused())
                {
                    e.Consumed = true;
                    return false;
                }

                return !ConsumeStorageNetworkRightClick(e, true);
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), nameof(DetailsScreen.OnKeyUp))]
        public static class DetailsScreenOnKeyUpPatch
        {
            public static bool Prefix(KButtonEvent e)
            {
                if (StorageNetworkPanel.IsTextInputFocused())
                {
                    e.Consumed = true;
                    return false;
                }

                return !ConsumeStorageNetworkRightClick(e, true);
            }
        }

        [HarmonyPatch(typeof(OverlayMenu), nameof(OverlayMenu.OnKeyUp))]
        public static class OverlayMenuOnKeyUpPatch
        {
            public static bool Prefix(KButtonEvent e)
            {
                if (StorageNetworkPanel.IsTextInputFocused())
                {
                    e.Consumed = true;
                    return false;
                }

                return !ConsumeStorageNetworkRightClick(e, true);
            }
        }

        private static bool ConsumeStorageNetworkRightClick(KButtonEvent e, bool keyUp = false)
        {
            if (e == null || !e.IsAction(global::Action.MouseRight) || !StorageNetworkPanel.IsOpen())
            {
                return false;
            }

            return false;
        }
    }
}
