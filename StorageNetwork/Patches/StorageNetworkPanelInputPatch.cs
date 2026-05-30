using System.Collections;
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
            internal static readonly FieldInfo IgnoreRectMaskCullingField = typeof(TMP_Text).GetField("ignoreRectMaskCulling", BindingFlags.Instance | BindingFlags.NonPublic);

            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(TMP_InputField), "AssignPositioningIfNeeded");
            }

            public static bool Prefix(TMP_InputField __instance, RectTransform ___caretRectTrans, TMP_Text ___m_TextComponent)
            {
                if (__instance == null || !IsStorageNetworkInput(__instance))
                {
                    return true;
                }

                try
                {
                    if (___m_TextComponent == null || ___caretRectTrans == null || !___m_TextComponent.isActiveAndEnabled)
                    {
                        return true;
                    }

                    RectTransform textTransform = ___m_TextComponent.rectTransform;
                    if (___caretRectTrans.localPosition != textTransform.localPosition ||
                        ___caretRectTrans.localRotation != textTransform.localRotation ||
                        ___caretRectTrans.localScale != textTransform.localScale ||
                        ___caretRectTrans.anchorMin != textTransform.anchorMin ||
                        ___caretRectTrans.anchorMax != textTransform.anchorMax ||
                        ___caretRectTrans.anchoredPosition != textTransform.anchoredPosition ||
                        ___caretRectTrans.sizeDelta != textTransform.sizeDelta ||
                        ___caretRectTrans.pivot != textTransform.pivot)
                    {
                        __instance.StartCoroutine(ResizeCaret(___caretRectTrans, textTransform));
                        return false;
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to align input caret: " + exception);
                }

                return true;
            }

            private static IEnumerator ResizeCaret(RectTransform caretTransform, RectTransform textTransform)
            {
                yield return null;
                if (caretTransform == null || textTransform == null)
                {
                    yield break;
                }

                caretTransform.localPosition = textTransform.localPosition;
                caretTransform.localRotation = textTransform.localRotation;
                caretTransform.localScale = textTransform.localScale;
                caretTransform.anchorMin = textTransform.anchorMin;
                caretTransform.anchorMax = textTransform.anchorMax;
                caretTransform.anchoredPosition = textTransform.anchoredPosition;
                caretTransform.sizeDelta = textTransform.sizeDelta;
                caretTransform.pivot = textTransform.pivot;
            }
        }

        [HarmonyPatch(typeof(TMP_InputField), "OnEnable")]
        public static class TMPInputFieldRectMaskPatch
        {
            public static void Postfix(TMP_Text ___m_TextComponent, Scrollbar ___m_VerticalScrollbar)
            {
                try
                {
                    FieldInfo field = TMPInputFieldCaretPositionPatch.IgnoreRectMaskCullingField;
                    if (___m_TextComponent != null && field != null)
                    {
                        field.SetValue(___m_TextComponent, ___m_VerticalScrollbar != null);
                    }
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

        private static bool IsStorageNetworkInput(TMP_InputField input)
        {
            return input != null &&
                   (input.GetComponent<StorageNetworkNumberInputField>() != null ||
                    input.GetComponentInParent<StorageNetworkNumberInputField>() != null ||
                    input.GetComponentInChildren<StorageNetworkNumberInputField>(true) != null ||
                    input.GetComponent<StorageNetworkTextInputGuard>() != null ||
                    input.GetComponentInParent<StorageNetworkTextInputGuard>() != null ||
                    input.GetComponentInChildren<StorageNetworkTextInputGuard>(true) != null ||
                    input.GetComponent<StorageNetworkInputFieldMarker>() != null ||
                    input.GetComponentInParent<StorageNetworkInputFieldMarker>() != null ||
                    input.GetComponentInChildren<StorageNetworkInputFieldMarker>(true) != null);
        }
    }
}
