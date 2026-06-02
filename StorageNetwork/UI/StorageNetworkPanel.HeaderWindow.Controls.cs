using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private void AddChoiceButton(Transform parent, string label, int routeIndex, bool selected, float height)
        {
            int lineCount = EstimateTextLineCount(label, 3, 18);
            float preferredHeight = Mathf.Max(height, 15f * lineCount);
            GameObject button = CreateStyledButton("ChoiceButton", parent, label, () =>
            {
                selectedRouteIndex = routeIndex;
                lastOrderStatus = null;
                orderDetailsSignature = null;
                RebuildOrderDetails();
            }, selected ? KleiPinkStyle() : KleiBlueStyle());
            TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
            {
                buttonLabel.alignment = TextAlignmentOptions.Center;
                buttonLabel.textWrappingMode = TextWrappingModes.Normal;
                buttonLabel.overflowMode = TextOverflowModes.Ellipsis;
                buttonLabel.maxVisibleLines = 3;
                Stretch(buttonLabel.rectTransform(), 6f, 1f);
            }

            button.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        }

        private void AddQuickAmountButton(Transform parent, float amount, string label)
        {
            bool selected = Mathf.Approximately(orderAmountStep, amount);
            GameObject button = CreateStyledButton("QuickAmount", parent, label, () =>
            {
                orderAmountStep = amount;
                SetRequestedAmount(amount);
            }, selected ? KleiPinkStyle() : KleiBlueStyle());
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = 56f;
            layout.minWidth = 46f;
            layout.preferredHeight = 22f;
            TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.fontSize = 14;
            }
        }

        private void AddAmountStepSelector(Transform parent)
        {
            GameObject group = CreatePlainImage("AmountStepSelector", parent, new Color(0.70f, 0.70f, 0.64f, 1f));
            group.transform.SetParent(parent, false);
            LayoutElement groupLayout = group.AddComponent<LayoutElement>();
            groupLayout.preferredHeight = 30f;
            groupLayout.minHeight = 30f;
            groupLayout.flexibleWidth = 1f;
            groupLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 4, 4);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddQuickAmountButton(group.transform, 100f, "100kg");
            AddQuickAmountButton(group.transform, 500f, "500kg");
            AddQuickAmountButton(group.transform, 1000f, "1t");
        }

        private void AddAmountAdjustButton(Transform parent, string iconName, System.Action onClick)
        {
            GameObject button = CreateGameButton("AmountAdjust", parent, string.Empty, onClick);
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = 26f;
            layout.minWidth = 26f;
            layout.preferredHeight = 26f;

            Image icon = new GameObject("Icon").AddComponent<Image>();
            icon.transform.SetParent(button.transform, false);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = GetSpriteByName(iconName);
            icon.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(30f, 30f);
        }

        private KInputTextField CreateFixedAmountInput(Transform parent, float width, float height)
        {
            GameObject slot = new GameObject("AmountInputSlot");
            slot.transform.SetParent(parent, false);
            RectTransform slotRect = slot.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(width, height);
            LayoutElement slotLayout = slot.AddComponent<LayoutElement>();
            slotLayout.minWidth = width;
            slotLayout.preferredWidth = width;
            slotLayout.minHeight = height;
            slotLayout.preferredHeight = height;
            slotLayout.flexibleWidth = 0f;
            slotLayout.flexibleHeight = 0f;

            KInputTextField input = StorageNetworkInputBuilder.CreateKNumberInput(
                slot.transform,
                "AmountInput",
                string.Empty,
                width,
                height,
                13,
                TextAlignmentOptions.Center,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                "web_box",
                Color.white,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                Vector2.one,
                false);
            input.gameObject.AddComponent<StorageNetworkNumberInputField>().Configure(input, 0f, float.MaxValue, false);
            RectTransform inputRect = input.gameObject.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = Vector2.zero;
            inputRect.sizeDelta = new Vector2(width, height);
            EnsureInputFieldDragReferences(input, width, height);
            return input;
        }

        private KInputTextField CreateFixedTextInput(Transform parent, string name, string value, float width, float height, int fontSize)
        {
            GameObject slot = new GameObject(name + "Slot");
            slot.transform.SetParent(parent, false);
            RectTransform slotRect = slot.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(width, height);
            LayoutElement slotLayout = slot.AddComponent<LayoutElement>();
            slotLayout.minWidth = width;
            slotLayout.preferredWidth = width;
            slotLayout.minHeight = height;
            slotLayout.preferredHeight = height;
            slotLayout.flexibleWidth = 1f;
            slotLayout.flexibleHeight = 0f;

            KInputTextField input = StorageNetworkInputBuilder.CreateKNumberInput(
                slot.transform,
                name,
                value ?? string.Empty,
                width,
                height,
                fontSize,
                TextAlignmentOptions.MidlineLeft,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                "web_box",
                Color.white,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                Vector2.one,
                false);

            input.characterLimit = 64;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.inputType = TMP_InputField.InputType.Standard;
            input.keyboardType = TouchScreenKeyboardType.Default;
            input.lineType = TMP_InputField.LineType.SingleLine;
            if (input.textComponent != null)
            {
                input.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
                input.textComponent.overflowMode = TextOverflowModes.Ellipsis;
            }

            RectTransform inputRect = input.gameObject.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = Vector2.zero;
            inputRect.sizeDelta = new Vector2(width, height);
            EnsureInputFieldDragReferences(input, width, height);
            input.gameObject.AddComponent<StorageNetworkTextInputGuard>().Configure(input, input.gameObject.GetComponent<Image>());
            return input;
        }

        /// <summary>
        /// TMP_InputField 在拖拽选择文本时会调用 MouseDragOutsideRect，内部强依赖 textViewport。
        /// 有些运行时动态创建的 KInputTextField 没有把 textViewport 正确挂上，拖拽时会在
        /// RectTransformUtility.ScreenPointToLocalPointInRectangle 里空引用。这里强制补齐引用。
        /// </summary>
        private static void EnsureInputFieldDragReferences(TMP_InputField input, float width, float height)
        {
            if (input == null)
            {
                return;
            }

            RectTransform inputRect = input.GetComponent<RectTransform>();
            if (inputRect == null)
            {
                inputRect = input.gameObject.AddComponent<RectTransform>();
            }

            if (input.textComponent == null)
            {
                input.textComponent = input.GetComponentInChildren<TMP_Text>(true);
            }

            if (input.textViewport == null)
            {
                RectTransform viewport = null;
                if (input.textComponent != null)
                {
                    viewport = input.textComponent.rectTransform.parent as RectTransform;
                }

                input.textViewport = viewport != null ? viewport : inputRect;
            }

            input.textViewport.anchorMin = Vector2.zero;
            input.textViewport.anchorMax = Vector2.one;
            input.textViewport.pivot = new Vector2(0.5f, 0.5f);
            input.textViewport.offsetMin = Vector2.one;
            input.textViewport.offsetMax = -Vector2.one;

            if (input.textComponent != null)
            {
                RectTransform textRect = input.textComponent.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.offsetMin = new Vector2(4f, 0f);
                textRect.offsetMax = new Vector2(-4f, 0f);
                input.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
                input.textComponent.overflowMode = TextOverflowModes.Overflow;
                input.textComponent.maxVisibleLines = 1;
                input.textComponent.raycastTarget = true;
            }
        }

        private void AdjustRequestedAmount(float delta)
        {
            SetRequestedAmount(requestedProductAmount + delta);
        }

        private void SetRequestedAmount(float amount)
        {
            requestedProductAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, amount);
            StorageNetworkNumberInputField numberInput = orderAmountInput != null
                ? orderAmountInput.GetComponent<StorageNetworkNumberInputField>()
                : null;
            if (numberInput != null)
            {
                numberInput.SetAmount(requestedProductAmount);
            }
            else if (orderAmountInput != null)
            {
                orderAmountInput.text = FormatAmount(requestedProductAmount);
            }

            lastOrderStatus = null;
            orderDetailsSignature = null;
            RebuildOrderDetails();
        }

        private void EnsureKeepRuleDraft(ProductDisplayGroup product, ProductionKeepRule rule)
        {
            string productKey = product != null ? product.ProductKey : string.Empty;
            if (keepRuleDraftProductKey == productKey)
            {
                return;
            }

            keepRuleDraftProductKey = productKey;
            keepRuleDraftAmount = rule != null
                ? Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, rule.TargetAmount)
                : Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, requestedProductAmount);
        }

        private bool IsOrderInputFocused()
        {
            return (orderAmountInput != null && orderAmountInput.isFocused) ||
                   (keepRuleAmountInput != null && keepRuleAmountInput.isFocused);
        }

        private void DeactivateOrderInputs()
        {
            SafeDeactivateInput(orderAmountInput);
            SafeDeactivateInput(keepRuleAmountInput);
            orderAmountInput = null;
            keepRuleAmountInput = null;
        }

        private static void SafeDeactivateInput(TMP_InputField input)
        {
            if (input == null)
            {
                return;
            }

            try
            {
                input.DeactivateInputField();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[StorageNetwork] Failed to deactivate input field before rebuilding UI: " + exception);
            }
        }

        private float GetKeepRuleAmountFromInput(ProductionKeepRule currentRule)
        {
            if (keepRuleAmountInput != null && TryParseAmount(keepRuleAmountInput.text, out float parsed))
            {
                return Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, parsed);
            }

            if (currentRule != null)
            {
                return Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, currentRule.TargetAmount);
            }

            return Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, requestedProductAmount);
        }

        private void AddTableHeader(Transform parent, string left, string middle, string right)
        {
            AddTableRow(parent, left, middle, right, new Color(0.18f, 0.19f, 0.18f, 1f), true);
        }

        private void AddTableRow(Transform parent, string left, string middle, string right, Color color, bool header = false)
        {
            GameObject row = CreatePlainImage(header ? "TableHeader" : "TableRow", parent, header ? new Color(0.68f, 0.69f, 0.64f, 1f) : new Color(0.78f, 0.78f, 0.72f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = header ? 24f : 26f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 3, 3);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddTableCell(row.transform, left, 9, header ? FontStyles.Bold : FontStyles.Normal, color, 0f, true);
            AddTableCell(row.transform, middle, 9, header ? FontStyles.Bold : FontStyles.Normal, color, 82f, false);
            AddTableCell(row.transform, right, 9, header ? FontStyles.Bold : FontStyles.Normal, color, 128f, false);
        }

        private void AddTableCell(Transform parent, string text, int size, FontStyles style, Color color, float width, bool flexible)
        {
            TextMeshProUGUI cell = CreateText("Cell", parent, text, size, TextAlignmentOptions.MidlineLeft);
            cell.color = color;
            cell.fontStyle = style;
            cell.textWrappingMode = TextWrappingModes.NoWrap;
            cell.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement layout = cell.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                layout.preferredWidth = width;
            }

            if (flexible)
            {
                layout.flexibleWidth = 1f;
            }
        }

    }
}
