using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkNumberInputField : KScreen
    {
        private static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
        private static StorageNetworkNumberInputField editingField;
        private KInputTextField inputField;
        private bool listenersRegistered;

        public int decimalPlaces = -1;
        public float currentValue;
        public float minValue;
        public float maxValue;

        public KInputTextField field => inputField;
        public static bool IsAnyEditing => editingField != null && editingField.isEditing && editingField.inputField != null;

        public event System.Action onStartEdit;
        public event System.Action onEndEdit;

        public void Configure(KInputTextField field, float min, float max, bool integer)
        {
            activateOnSpawn = true;
            inputField = field;
            minValue = min;
            maxValue = max;
            decimalPlaces = integer ? 0 : -1;

            if (inputField == null || listenersRegistered)
            {
                return;
            }

            inputField.customCaretColor = true;
            inputField.caretColor = new Color(0.05f, 0.06f, 0.07f, 1f);
            inputField.caretWidth = 2;
            inputField.selectionColor = new Color(0.65882355f, 0.80784315f, 1f, 0.7529412f);
            inputField.onFocusSelectAll = true;
            inputField.keepTextSelectionVisible = true;

            inputField.onFocus = (System.Action)System.Delegate.Combine(inputField.onFocus, new System.Action(OnEditStart));
            inputField.onSelect.AddListener(OnSelect);
            inputField.onEndEdit.AddListener(OnEndEdit);
            listenersRegistered = true;

            if (KScreenManager.Instance != null && !IsActive())
            {
                Activate();
            }
        }

        protected override void OnCleanUp()
        {
            if (editingField == this)
            {
                editingField = null;
            }

            if (inputField != null && listenersRegistered)
            {
                inputField.onFocus = (System.Action)System.Delegate.Remove(inputField.onFocus, new System.Action(OnEditStart));
                inputField.onSelect.RemoveListener(OnSelect);
                inputField.onEndEdit.RemoveListener(OnEndEdit);
            }

            base.OnCleanUp();
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

        private void OnSelect(string _)
        {
            OnEditStart();
        }

        private void OnEditStart()
        {
            if (inputField == null)
            {
                return;
            }

            isEditing = true;
            editingField = this;
            inputField.Select();
            inputField.ActivateInputField();
            KScreenManager.Instance?.RefreshStack();
            SelectAllNextFrame();
            onStartEdit?.Invoke();
        }

        private void OnEndEdit(string input)
        {
            if (gameObject.activeInHierarchy)
            {
                ProcessInput(input);
                if (isEditing)
                {
                    StartCoroutine(DelayedEndEdit());
                }
                else
                {
                    StopEditing();
                }
            }
            else
            {
                StopEditing();
            }
        }

        private IEnumerator DelayedEndEdit()
        {
            if (isEditing)
            {
                yield return WaitForEndOfFrame;
                StopEditing();
            }
        }

        private void StopEditing()
        {
            isEditing = false;
            if (editingField == this)
            {
                editingField = null;
            }

            inputField?.DeactivateInputField();
            onEndEdit?.Invoke();
        }

        public void SetAmount(float newValue)
        {
            newValue = Mathf.Clamp(newValue, minValue, maxValue);
            if (decimalPlaces != -1)
            {
                float scale = Mathf.Pow(10f, decimalPlaces);
                newValue = Mathf.Round(newValue * scale) / scale;
            }

            currentValue = newValue;
            SetDisplayValue(Format(currentValue));
        }

        public void SetDisplayValue(string input)
        {
            if (inputField != null)
            {
                inputField.text = input;
            }
        }

        private void ProcessInput(string input)
        {
            input = string.IsNullOrEmpty(input) ? minValue.ToString(CultureInfo.InvariantCulture) : input;
            if (float.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) ||
                float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                SetAmount(value);
            }
        }

        private void SelectAllNextFrame()
        {
            if (inputField != null && isActiveAndEnabled)
            {
                StartCoroutine(SelectAllAfterFocus());
            }
        }

        private IEnumerator SelectAllAfterFocus()
        {
            yield return null;
            if (inputField == null || !isEditing)
            {
                yield break;
            }

            inputField.Select();
            inputField.ActivateInputField();
            inputField.selectionAnchorPosition = 0;
            inputField.selectionFocusPosition = inputField.text != null ? inputField.text.Length : 0;
            inputField.selectionStringAnchorPosition = 0;
            inputField.selectionStringFocusPosition = inputField.text != null ? inputField.text.Length : 0;
            inputField.caretPosition = inputField.selectionFocusPosition;
            inputField.stringPosition = inputField.selectionStringFocusPosition;
            inputField.ForceLabelUpdate();
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
