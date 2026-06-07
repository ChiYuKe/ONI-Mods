using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkInputFieldEvents : KScreen
    {
        private TMP_InputField input;
        private bool listenersRegistered;

        public void Configure(TMP_InputField inputField)
        {
            activateOnSpawn = true;
            input = inputField;
            if (KScreenManager.Instance != null && !IsActive())
            {
                Activate();
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            RegisterInputListeners();
        }

        protected override void OnCleanUp()
        {
            UnregisterInputListeners();
            base.OnCleanUp();
        }

        private void RegisterInputListeners()
        {
            if (input == null || listenersRegistered)
            {
                return;
            }

            input.onFocus = (System.Action)System.Delegate.Combine(input.onFocus, new System.Action(OnFocus));
            input.onValueChanged.AddListener(OnValueChanged);
            input.onEndEdit.AddListener(OnEndEdit);
            listenersRegistered = true;
        }

        private void UnregisterInputListeners()
        {
            if (input == null || !listenersRegistered)
            {
                return;
            }

            input.onFocus = (System.Action)System.Delegate.Remove(input.onFocus, new System.Action(OnFocus));
            input.onValueChanged.RemoveListener(OnValueChanged);
            input.onEndEdit.RemoveListener(OnEndEdit);
            listenersRegistered = false;
        }

        public override float GetSortKey()
        {
            return isEditing ? 99f : base.GetSortKey();
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (isEditing)
            {
                e.Consumed = true;
                return;
            }

            base.OnKeyDown(e);
        }

        public override void OnKeyUp(KButtonEvent e)
        {
            if (isEditing)
            {
                e.Consumed = true;
                return;
            }

            base.OnKeyUp(e);
        }

        private void OnFocus()
        {
            isEditing = true;
            input.Select();
            input.ActivateInputField();
            KScreenManager.Instance?.RefreshStack();
        }

        private void OnValueChanged(string text)
        {
            if (input != null && input.textComponent != null)
            {
                RectTransform textRect = input.textComponent.rectTransform;
                textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, LayoutUtility.GetPreferredHeight(textRect));
            }
        }

        private void OnEndEdit(string text)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayedStopEditing());
            }
            else
            {
                StopEditing();
            }
        }

        private IEnumerator DelayedStopEditing()
        {
            yield return new WaitForEndOfFrame();
            StopEditing();
        }

        private void StopEditing()
        {
            if (input != null && input.gameObject.activeInHierarchy)
            {
                input.DeactivateInputField();
            }

            isEditing = false;
            KScreenManager.Instance?.RefreshStack();
        }
    }
}
