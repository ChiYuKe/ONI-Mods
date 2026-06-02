using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModConfig
{
    internal sealed class ModConfigInputFieldEvents : KScreen
    {
        private TMP_InputField input;

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
            if (input == null)
            {
                return;
            }

            input.onFocus = (System.Action)System.Delegate.Combine(input.onFocus, new System.Action(OnFocus));
            input.onValueChanged.AddListener(OnValueChanged);
            input.onEndEdit.AddListener(OnEndEdit);
        }

        protected override void OnCleanUp()
        {
            if (input != null)
            {
                input.onFocus = (System.Action)System.Delegate.Remove(input.onFocus, new System.Action(OnFocus));
                input.onValueChanged.RemoveListener(OnValueChanged);
                input.onEndEdit.RemoveListener(OnEndEdit);
            }

            base.OnCleanUp();
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
