using System;
using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetworkTrading.UI
{
    public sealed partial class TradingPanel
    {
        private GameObject tradingDialogueButton;
        private GameObject tradingDialogueRoot;
        private int tradingDialogueSupplierId;
        private Sprite tradingAlertSprite;
        private bool tradingAlertSpriteResolved;

        private GameObject CreateTradingAlertButton(string name, Transform parent, System.Action onClick, float iconInset = 4)
        {
            // Reuse the actual management menu alert artwork, including inactive alerts.
            if (!tradingAlertSpriteResolved)
            {
                tradingAlertSpriteResolved = true;
                ImageToggleState nativeAlert = ManagementMenu.Instance != null ? ManagementMenu.Instance.GetComponentsInChildren<ImageToggleState>(true)
                    .FirstOrDefault(i => i.name.IndexOf("Alert", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        (i.ActiveSprite != null || (i.TargetImage != null && i.TargetImage.sprite != null))) : null;
                tradingAlertSprite = nativeAlert != null ? nativeAlert.ActiveSprite ?? nativeAlert.TargetImage?.sprite : null;
            }
            GameObject result;
            if (tradingAlertSprite != null)
            {
                result = CreateIconOnlyButton(name, parent, tradingAlertSprite, onClick);
                var icon = result.transform.Find("Icon").GetComponent<Image>();
                icon.color = new Color(1f, 0.62f, 0.34f, 1);
                Stretch(icon.rectTransform(), iconInset, iconInset);
            }
            else
            {
                result = CreateGameButton(name, parent, "!", onClick);
                var label = result.GetComponentInChildren<TextMeshProUGUI>();
                // Compact alerts must not inherit normal button padding/ellipsis:
                // a 20px-high label otherwise clips the whole glyph.
                Stretch(label.rectTransform(), 0, 0);
                label.margin = Vector4.zero;
                label.fontSize = 16;
                label.overflowMode = TextOverflowModes.Overflow;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(1f, 0.62f, 0.34f, 1);
            }
            return result;
        }

        private void BuildTradingDialogueButton(Transform footer)
        {
            tradingDialogueButton = CreateTradingAlertButton("SupplierDialogue", footer, ShowSelectedTradingDialogue);
            RectTransform rect = tradingDialogueButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(36, 36);
            rect.anchoredPosition = new Vector2(-10, 0);
            tradingDialogueButton.AddComponent<ToolTip>().toolTip = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_0337DDFE13F8"));
            tradingDialogueButton.SetActive(false);
        }

        private void RefreshTradingDialogue(TradingSupplier selected)
        {
            bool pending = TradingDialogue.Pending(selected, StorageNetworkTradingState.Cycle);
            tradingDialogueButton?.SetActive(pending);
            tradingStatus.rectTransform().offsetMax = new Vector2(pending ? -56 : -10, -3);
            if (tradingDialogueRoot != null && modalRoot == tradingDialogueRoot)
            {
                TradingSupplier speaker = StorageNetworkTradingState.Instance?.Market?.Suppliers.Find(s => s.Id == tradingDialogueSupplierId);
                if (!TradingDialogue.Pending(speaker, StorageNetworkTradingState.Cycle))
                {
                    tradingDialogueRoot.SetActive(false);
                    CloseModal();
                }
            }
        }

        private void ShowSelectedTradingDialogue()
        {
            var suppliers = StorageNetworkTradingState.Instance?.LocalVisitors(GetActiveWorldFilterId());
            if (suppliers == null || tradingSlot < 0 || tradingSlot >= suppliers.Count) return;
            ShowTradingDialogue(suppliers[tradingSlot].Id);
        }

        private void ShowTradingDialogue(int supplierId)
        {
            TradingSupplier supplier = StorageNetworkTradingState.Instance?.Market?.Suppliers.Find(s => s.Id == supplierId);
            if (!TradingDialogue.Pending(supplier, StorageNetworkTradingState.Cycle)) { RefreshTradingWindow(); return; }
            var market = StorageNetworkTradingState.Instance.Market;
            TradingConversations.Upgrade(supplier);
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            tradingDialogueSupplierId = supplierId;
            modalRoot = CreateAmountModalFrame(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_79FFE8D3B8D8") + (supplier.Name ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8A11D7DF26E")), 700, 620, out GameObject body);
            tradingDialogueRoot = modalRoot;
            GameObject identity = AddAmountModalRow(body.transform, 12, 44);
            GameObject portrait = new GameObject("SpeakerPortrait", typeof(RectTransform), typeof(LayoutElement), typeof(StorageNetworkTradingPortrait));
            portrait.transform.SetParent(identity.transform, false);
            LayoutElement portraitLayout = portrait.GetComponent<LayoutElement>();
            portraitLayout.minWidth = portraitLayout.preferredWidth = 44;
            portraitLayout.minHeight = portraitLayout.preferredHeight = 44;
            portrait.GetComponent<StorageNetworkTradingPortrait>().Bind(supplier);
            var name = CreateAmountModalText(identity.transform, supplier.Name + "\n" + StorageNetworkTradingState.SpecialtyName(supplier.Specialty), 15, FontStyles.Bold);
            name.color = StorageNetworkPanelPalette.BodyText;
            name.GetComponent<LayoutElement>().flexibleWidth = 1;
            AddAmountModalButton(identity.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_43286F5234E0"), 84, () => ShowTradingIdentityCard(supplierId, true));
            GameObject chatFrame = CreatePlainImage("ConversationHistory", body.transform, StorageNetworkPanelPalette.RowBackground);
            chatFrame.AddComponent<LayoutElement>().preferredHeight = 332;
            RectTransform viewport = CreateScrollViewport(chatFrame.transform, "Messages", out RectTransform messages, 0, 0, 0, 0, 8);
            viewport.GetComponent<Image>().color = StorageNetworkPanelPalette.RowBackground;
            // Native animation portraits implement IMaskable (stencil Mask),
            // but not IClippable (RectMask2D). Keep rectangular text clipping
            // and add the mask understood by KBatchedAnimCanvasRenderer.
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.sprite = null;
            viewportImage.type = Image.Type.Simple;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Scrollbar scrollbar = CreateScrollbar(chatFrame.transform, 0, 0);
            WireScrollRect(viewport.gameObject, messages, scrollbar, 26);
            var lines = supplier.DialogueLines ?? new System.Collections.Generic.List<TradingDialogueLine>();
            if (lines.Count == 0)
                lines = new System.Collections.Generic.List<TradingDialogueLine> { new TradingDialogueLine {
                    Text = TradingConversations.Text(market, supplier, StorageNetworkTradingState.Instance.Catalog, StorageNetworkTradingState.Cycle) } };
            foreach (TradingDialogueLine line in lines)
            {
                CreateTradingChatBubble(messages, supplier, line);
            }
            string[] choices = TradingConversations.Choices(supplier);
            GameObject optionRow = null;
            for (int i = 0; i < choices.Length; i++)
            {
                int choice = i;
                if (i % 2 == 0) optionRow = AddAmountModalRow(body.transform, 8, 28);
                GameObject button = AddAmountModalButton(optionRow.transform, choices[i], 290, () =>
                {
                    var state = StorageNetworkTradingState.Instance;
                    if (state == null || !state.AdvanceDialogue(supplierId, choice))
                    {
                        if (modalRoot != null) modalRoot.SetActive(false);
                        CloseModal();
                        RefreshTradingWindow();
                        return;
                    }
                    RefreshTradingWindow();
                    TradingSupplier speaker = state.Market.Suppliers.Find(s => s.Id == supplierId);
                    if (TradingDialogue.Pending(speaker, StorageNetworkTradingState.Cycle)) ShowTradingDialogue(supplierId);
                    else
                    {
                        if (modalRoot != null) modalRoot.SetActive(false);
                        CloseModal();
                        RefreshTradingWindow();
                    }
                });
                button.GetComponent<LayoutElement>().flexibleWidth = 1;
            }
            var hint = CreateAmountModalText(body.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_93CFA6834E09"), 11, FontStyles.Normal);
            hint.color = StorageNetworkPanelPalette.MutedText;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(messages);
            viewport.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;
        }

        private void CreateTradingChatBubble(RectTransform messages, TradingSupplier supplier, TradingDialogueLine line, bool literal = false, string speaker = null)
        {
            bool player = line.Player;
            var row = new GameObject(player ? "PlayerMessage" : "SupplierMessage", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(messages, false);
            Vector2 corner = new Vector2(player ? 1 : 0, 1);
            GameObject avatar = CreatePlainImage("Avatar", row.transform,
                player ? new Color(0.43f, 0.20f, 0.34f, 1) : new Color(0.72f, 0.72f, 0.67f, 1));
            RectTransform avatarRect = avatar.GetComponent<RectTransform>();
            avatarRect.anchorMin = avatarRect.anchorMax = avatarRect.pivot = corner;
            avatarRect.sizeDelta = new Vector2(36, 36);
            avatarRect.anchoredPosition = new Vector2(player ? -4 : 4, -20);
            avatar.GetComponent<Image>().raycastTarget = false;
            if (player)
            {
                var initials = CreateText("Player", avatar.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A0C7716669B5"), 17, TextAlignmentOptions.Center);
                initials.color = Color.white;
                initials.raycastTarget = false;
                Stretch(initials.rectTransform(), 0, 0);
            }
            else avatar.AddComponent<StorageNetworkTradingPortrait>().Bind(supplier);

            var name = CreateText("Speaker", row.transform, player ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A0C7716669B5") : speaker ?? supplier.Name ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8A11D7DF26E"), 11,
                player ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft);
            name.color = StorageNetworkPanelPalette.MutedText;
            name.raycastTarget = false;
            RectTransform nameRect = name.rectTransform();
            nameRect.anchorMin = nameRect.anchorMax = nameRect.pivot = corner;
            nameRect.sizeDelta = new Vector2(420, 18);
            nameRect.anchoredPosition = new Vector2(player ? -50 : 50, 0);

            Color bubbleColor = player ? new Color(0.43f, 0.20f, 0.34f, 1) : new Color(0.89f, 0.89f, 0.84f, 1);
            GameObject bubble = CreatePlainImage("ChatBubble", row.transform, bubbleColor);
            bubble.GetComponent<Image>().raycastTarget = false;
            var text = CreateText("Message", bubble.transform, line.Text ?? "", 13, TextAlignmentOptions.TopLeft);
            if (literal) text.richText = false;
            text.color = player ? new Color(0.97f, 0.95f, 0.96f, 1) : StorageNetworkPanelPalette.BodyText;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            float maximumWidth = literal ? Mathf.Clamp(messages.rect.width * 0.72f, 300, 540) : 420;
            float width = Mathf.Clamp(text.GetPreferredValues(text.text, maximumWidth - 24, 0).x + 24, 80, maximumWidth);
            float height = Mathf.Max(38, text.GetPreferredValues(text.text, width - 24, 0).y + 20);
            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.anchorMin = bubbleRect.anchorMax = bubbleRect.pivot = corner;
            bubbleRect.sizeDelta = new Vector2(width, height);
            bubbleRect.anchoredPosition = new Vector2(player ? -50 : 50, -20);
            SetStretch(text.rectTransform(), 12, 12, 10, 10);
            GameObject tail = CreatePlainImage("BubbleTail", bubble.transform, bubbleColor);
            tail.GetComponent<Image>().raycastTarget = false;
            RectTransform tailRect = tail.GetComponent<RectTransform>();
            tailRect.anchorMin = tailRect.anchorMax = new Vector2(player ? 1 : 0, 1);
            tailRect.sizeDelta = new Vector2(8, 8);
            tailRect.anchoredPosition = new Vector2(0, -15);
            tailRect.localRotation = Quaternion.Euler(0, 0, 45);
            tail.transform.SetAsFirstSibling();
            row.GetComponent<LayoutElement>().preferredHeight = height + (literal ? 24 : 28);
        }
    }
}
