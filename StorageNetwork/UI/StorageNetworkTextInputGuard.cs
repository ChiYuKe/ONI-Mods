using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkTextInputGuard : KScreen, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        private static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
        private KInputTextField input;
        private Image background;
        private Outline focusOutline;
        private Image selectionHighlight;
        private static KInputTextField focusedInput;
        private static StorageNetworkTextInputGuard focusedGuard;
        private bool editing;
        private Coroutine endEditRoutine;

        public static bool IsAnyFocused => focusedGuard != null && focusedGuard.editing && focusedGuard.input != null;

        public override float GetSortKey()
        {
            return editing ? 99f : base.GetSortKey();
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (editing)
            {
                e.Consumed = true;
                return;
            }

            base.OnKeyDown(e);
        }

        public override void OnKeyUp(KButtonEvent e)
        {
            if (editing)
            {
                e.Consumed = true;
                return;
            }

            base.OnKeyUp(e);
        }

        public void Configure(KInputTextField inputField, Image backgroundImage = null, Outline outline = null)
        {
            activateOnSpawn = true;
            input = inputField;
            background = backgroundImage;
            focusOutline = outline;
            if (input == null)
            {
                return;
            }

            input.customCaretColor = true;
            input.caretColor = new Color(0.05f, 0.06f, 0.07f, 1f);
            input.caretWidth = 2;
            input.selectionColor = new Color(0.66f, 0.82f, 1f, 0.82f);
            input.onFocusSelectAll = true;
            input.keepTextSelectionVisible = true;
            input.onFocus = (global::System.Action)Delegate.Combine(input.onFocus, new global::System.Action(BeginEdit));
            input.onSelect.AddListener(_ => BeginEdit());
            input.onEndEdit.AddListener(_ => EndEdit());
            input.onValueChanged.AddListener(_ =>
            {
                SetSelectionHighlight(false);
            });
            EnsureSelectionHighlight();
            ApplyFocusVisual(false);

            if (KScreenManager.Instance != null && !IsActive())
            {
                Activate();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            BeginEdit();
        }

        public void OnSelect(BaseEventData eventData)
        {
            BeginEdit();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            EndEdit();
        }

        private void Update()
        {
            RefreshFocusedState();
        }

        protected override void OnDisable()
        {
            StopEditing();
            RefreshFocusedState();
            base.OnDisable();
        }

        protected override void OnCleanUp()
        {
            StopEditing();
            RefreshFocusedState();
            base.OnCleanUp();
        }

        private void BeginEdit()
        {
            if (input == null || !isActiveAndEnabled)
            {
                return;
            }

            if (endEditRoutine != null)
            {
                StopCoroutine(endEditRoutine);
                endEditRoutine = null;
            }

            if (editing)
            {
                focusedInput = input;
                focusedGuard = this;
                input.ActivateInputField();
                ApplyFocusVisual(true);
                return;
            }

            editing = true;
            focusedInput = input;
            focusedGuard = this;
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject != input.gameObject)
            {
                eventSystem.SetSelectedGameObject(input.gameObject);
            }

            input.Select();
            input.ActivateInputField();
            KScreenManager.Instance?.RefreshStack();
            SelectAllOnNextFrame();
            ApplyFocusVisual(true);
        }

        private void EndEdit()
        {
            if (!editing)
            {
                RefreshFocusedState();
                return;
            }

            if (endEditRoutine == null && isActiveAndEnabled)
            {
                endEditRoutine = StartCoroutine(DelayedEndEdit());
            }
            else if (!isActiveAndEnabled)
            {
                StopEditing();
            }
        }

        private IEnumerator DelayedEndEdit()
        {
            yield return WaitForEndOfFrame;
            endEditRoutine = null;
            StopEditing();
        }

        private void StopEditing()
        {
            if (endEditRoutine != null)
            {
                StopCoroutine(endEditRoutine);
                endEditRoutine = null;
            }

            editing = false;
            if (input != null)
            {
                input.DeactivateInputField();
            }

            if (focusedInput == input)
            {
                focusedInput = null;
            }

            if (focusedGuard == this)
            {
                focusedGuard = null;
            }

            KScreenManager.Instance?.RefreshStack();
            ApplyFocusVisual(false);
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
            if (input == null || !editing)
            {
                yield break;
            }

            input.Select();
            input.ActivateInputField();
            Canvas.ForceUpdateCanvases();
            input.selectionAnchorPosition = 0;
            input.selectionFocusPosition = input.text != null ? input.text.Length : 0;
            input.selectionStringAnchorPosition = 0;
            input.selectionStringFocusPosition = input.text != null ? input.text.Length : 0;
            input.caretPosition = input.selectionFocusPosition;
            input.stringPosition = input.selectionStringFocusPosition;
            input.ForceLabelUpdate();
            UpdateSelectionHighlightGeometry();
            SetSelectionHighlight(true);
            RefreshFocusedState();
        }

        private void RefreshFocusedState()
        {
            bool focused = input != null && (input.isFocused || editing);
            if (focused)
            {
                focusedInput = input;
                focusedGuard = this;
            }
            else if (focusedInput == input)
            {
                focusedInput = null;
                if (focusedGuard == this)
                {
                    focusedGuard = null;
                }
            }

            ApplyFocusVisual(focused);
        }

        private void ApplyFocusVisual(bool focused)
        {
            if (background != null)
            {
                background.color = focused
                    ? new Color(0.95f, 0.96f, 0.88f, 1f)
                    : Color.white;
            }

            if (focusOutline != null)
            {
                focusOutline.enabled = focused;
            }

            if (!focused)
            {
                SetSelectionHighlight(false);
            }
        }

        private void EnsureSelectionHighlight()
        {
            if (selectionHighlight != null || input == null || input.textViewport == null)
            {
                return;
            }

            GameObject highlightObject = new GameObject("StorageNetworkSelectionHighlight");
            highlightObject.transform.SetParent(input.textViewport, false);
            highlightObject.transform.SetAsFirstSibling();

            RectTransform highlightRect = highlightObject.AddComponent<RectTransform>();
            highlightRect.anchorMin = new Vector2(1f, 0f);
            highlightRect.anchorMax = new Vector2(1f, 1f);
            highlightRect.pivot = new Vector2(1f, 0.5f);
            highlightRect.offsetMin = new Vector2(-1f, 2f);
            highlightRect.offsetMax = new Vector2(0f, -2f);

            selectionHighlight = highlightObject.AddComponent<Image>();
            selectionHighlight.color = new Color(0.66f, 0.82f, 1f, 0.82f);
            selectionHighlight.raycastTarget = false;
            selectionHighlight.enabled = false;
        }

        private void UpdateSelectionHighlightGeometry()
        {
            EnsureSelectionHighlight();
            if (selectionHighlight == null || input == null || input.textComponent == null)
            {
                return;
            }

            RectTransform highlightRect = selectionHighlight.rectTransform;
            float width = input.textComponent.GetPreferredValues(input.text ?? string.Empty).x + 8f;
            float maxWidth = input.textViewport != null ? Mathf.Max(0f, input.textViewport.rect.width) : width;
            width = Mathf.Clamp(width, 8f, maxWidth);

            TextAlignmentOptions alignment = input.textComponent.alignment;
            if ((alignment & TextAlignmentOptions.Right) == TextAlignmentOptions.Right)
            {
                highlightRect.anchorMin = new Vector2(1f, 0f);
                highlightRect.anchorMax = new Vector2(1f, 1f);
                highlightRect.pivot = new Vector2(1f, 0.5f);
                highlightRect.offsetMin = new Vector2(-width, 2f);
                highlightRect.offsetMax = new Vector2(0f, -2f);
            }
            else
            {
                highlightRect.anchorMin = Vector2.zero;
                highlightRect.anchorMax = new Vector2(0f, 1f);
                highlightRect.pivot = new Vector2(0f, 0.5f);
                highlightRect.offsetMin = new Vector2(0f, 2f);
                highlightRect.offsetMax = new Vector2(width, -2f);
            }
        }

        private void SetSelectionHighlight(bool visible)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.enabled = visible;
            }
        }

    }
}
