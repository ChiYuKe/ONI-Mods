using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkInputPatchSupport
    {
        private static readonly FieldInfo IgnoreRectMaskCullingField = typeof(TMP_Text).GetField(
            "ignoreRectMaskCulling",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool IsStorageNetworkInput(TMP_InputField input)
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

        public static bool TryStartCaretResize(TMP_InputField input, RectTransform caretTransform, TMP_Text textComponent)
        {
            if (input == null || !IsStorageNetworkInput(input))
            {
                return false;
            }

            if (textComponent == null || caretTransform == null || !textComponent.isActiveAndEnabled)
            {
                return false;
            }

            RectTransform textTransform = textComponent.rectTransform;
            if (!NeedsCaretResize(caretTransform, textTransform))
            {
                return false;
            }

            input.StartCoroutine(ResizeCaret(caretTransform, textTransform));
            return true;
        }

        public static void ApplyRectMaskFix(TMP_Text textComponent, Scrollbar verticalScrollbar)
        {
            if (textComponent != null && IgnoreRectMaskCullingField != null)
            {
                IgnoreRectMaskCullingField.SetValue(textComponent, verticalScrollbar != null);
            }
        }

        private static bool NeedsCaretResize(RectTransform caretTransform, RectTransform textTransform)
        {
            return caretTransform.localPosition != textTransform.localPosition ||
                   caretTransform.localRotation != textTransform.localRotation ||
                   caretTransform.localScale != textTransform.localScale ||
                   caretTransform.anchorMin != textTransform.anchorMin ||
                   caretTransform.anchorMax != textTransform.anchorMax ||
                   caretTransform.anchoredPosition != textTransform.anchoredPosition ||
                   caretTransform.sizeDelta != textTransform.sizeDelta ||
                   caretTransform.pivot != textTransform.pivot;
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
}
