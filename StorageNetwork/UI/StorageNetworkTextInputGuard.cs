using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkTextInputGuard : MonoBehaviour, IPointerClickHandler, ISelectHandler
    {
        private KInputTextField input;
        private static KInputTextField focusedInput;

        public static bool IsAnyFocused => focusedInput != null && focusedInput.isFocused;

        public void Configure(KInputTextField inputField)
        {
            input = inputField;
            if (input == null)
            {
                return;
            }

            input.customCaretColor = true;
            input.caretColor = new Color(0.05f, 0.06f, 0.07f, 1f);
            input.caretWidth = 2;
            input.selectionColor = new Color(0.34f, 0.52f, 0.78f, 0.55f);
            input.onSelect.AddListener(_ => SelectAllOnNextFrame());
            input.onEndEdit.AddListener(_ => RefreshFocusedState());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SelectAllOnNextFrame();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SelectAllOnNextFrame();
        }

        private void Update()
        {
            RefreshFocusedState();
        }

        private void OnDisable()
        {
            RefreshFocusedState();
        }

        private void OnDestroy()
        {
            RefreshFocusedState();
        }

        private void SelectAllOnNextFrame()
        {
            if (input == null || !isActiveAndEnabled)
            {
                return;
            }

            StartCoroutine(SelectAllAfterFocus());
        }

        private IEnumerator SelectAllAfterFocus()
        {
            yield return null;
            if (input == null)
            {
                yield break;
            }

            input.ActivateInputField();
            input.selectionAnchorPosition = 0;
            input.selectionFocusPosition = input.text != null ? input.text.Length : 0;
            input.caretPosition = input.selectionFocusPosition;
            RefreshFocusedState();
        }

        private void RefreshFocusedState()
        {
            if (input != null && input.isFocused)
            {
                focusedInput = input;
            }
            else if (focusedInput == input)
            {
                focusedInput = null;
            }
        }
    }
}
