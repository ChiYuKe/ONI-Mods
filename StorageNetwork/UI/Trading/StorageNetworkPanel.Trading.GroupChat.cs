using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private GameObject tradingGroupRoot;
        private RectTransform tradingGroupWindow;
        private bool tradingGroupFloating;
        private Vector2 tradingGroupFullSize;
        private TextMeshProUGUI tradingGroupFloatLabel;
        private RectTransform tradingGroupMessages;
        private ScrollRect tradingGroupScroll;
        private Transform tradingGroupMembers;
        private KInputTextField tradingGroupInput;
        private KButton tradingGroupSend;
        private TextMeshProUGUI tradingGroupStatus;
        private readonly Dictionary<TradingGroupMessage, GameObject> tradingGroupRows = new Dictionary<TradingGroupMessage, GameObject>();
        private int tradingGroupVersion = -1;
        private string tradingGroupRoster = "";
        private float tradingGroupRefresh;
        private static string ChatText(string key) => TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING." + key);

        private void BuildTradingGroupChatButton(Transform header)
        {
            GameObject button = CreateGameButton("GroupChat", header, ChatText("GROUP_BUTTON"), ShowTradingGroupChat);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(70, 26);
            rect.anchoredPosition = new Vector2(-80, 0);
            button.AddComponent<ToolTip>().toolTip = TradingText.Render(ChatText("GROUP_TITLE"));
        }

        private void ShowTradingGroupChat()
        {
            if (tradingGroupRoot != null && tradingGroupFloating)
            {
                tradingGroupRoot.transform.SetAsLastSibling();
                return;
            }
            if (!TradingAiSettingsStore.Local.Load(out var settings) || !settings.CanChat())
            {
                ShowTradingAiSettings();
                return;
            }
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1;
            float height = Mathf.Min(900, Screen.height / scale - 32);
            modalRoot = CreateAmountModalFrame(ChatText("GROUP_TITLE"), Mathf.Min(820, Screen.width / scale - 32), height, out GameObject body);
            tradingGroupRoot = modalRoot;
            tradingGroupFloating = false;
            tradingGroupWindow = tradingGroupRoot.transform.Find("AmountDialog").GetComponent<RectTransform>();
            tradingGroupFullSize = tradingGroupWindow.sizeDelta;
            var header = tradingGroupWindow.Find("Header");
            header.gameObject.AddComponent<StorageNetworkWindowDrag>().Configure(tradingGroupWindow, "supplierChat");
            header.Find("CloseButton").gameObject.SetActive(false);
            var close = CreateCloseIconButton("GroupClose", header, CloseTradingGroupChat).GetComponent<RectTransform>();
            close.anchorMin = close.anchorMax = close.pivot = new Vector2(1, 0.5f);
            close.sizeDelta = new Vector2(24, 22);
            close.anchoredPosition = new Vector2(-5, 0);
            SetStretch(header.Find("Title").GetComponent<RectTransform>(), 12, 124, 0, 0);
            var floatButton = CreateGameButton("FloatingMode", header, ChatText("GROUP_FLOAT"), ToggleTradingGroupFloat);
            var floatRect = floatButton.GetComponent<RectTransform>();
            floatRect.anchorMin = floatRect.anchorMax = floatRect.pivot = new Vector2(1, 0.5f);
            floatRect.sizeDelta = new Vector2(76, 26);
            floatRect.anchoredPosition = new Vector2(-36, 0);
            tradingGroupFloatLabel = floatButton.GetComponentInChildren<TextMeshProUGUI>();
            var bodyLayout = body.GetComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 6;
            bodyLayout.padding = new RectOffset(10, 10, 10, 10);
            tradingGroupRows.Clear(); tradingGroupVersion = -1; tradingGroupRoster = "";
            tradingGroupMembers = AddAmountModalRow(body.transform, 6, 62).transform;
            var hint = CreateAmountModalText(body.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.GROUP_COMPACT_HINT", settings.ReplyProbability.ToString()), 11, FontStyles.Normal);
            hint.color = StorageNetworkPanelPalette.MutedText;
            hint.GetComponent<LayoutElement>().preferredHeight = 22;
            hint.gameObject.AddComponent<ToolTip>().toolTip = TradingText.Render(ChatText("GROUP_HINT"));
            var frame = CreatePlainImage("GroupMessages", body.transform, StorageNetworkPanelPalette.RowBackground);
            var messageLayout = frame.AddComponent<LayoutElement>();
            messageLayout.minHeight = 120;
            messageLayout.preferredHeight = 0;
            messageLayout.flexibleHeight = 1;
            RectTransform viewport = CreateScrollViewport(frame.transform, "Messages", out tradingGroupMessages, 0, 0, 0, 0, 8);
            viewport.GetComponent<Image>().sprite = null;
            viewport.GetComponent<Image>().type = Image.Type.Simple;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Scrollbar scrollbar = CreateScrollbar(frame.transform, 0, 0);
            WireScrollRect(viewport.gameObject, tradingGroupMessages, scrollbar, 34);
            tradingGroupScroll = viewport.GetComponent<ScrollRect>();
            var inputRow = AddAmountModalRow(body.transform, 8, 44);
            tradingGroupInput = CreateFixedTextInput(inputRow.transform, "GroupInput", "", 280, 40, 13);
            var inputSlot = tradingGroupInput.transform.parent.GetComponent<LayoutElement>();
            inputSlot.minWidth = 120;
            inputSlot.preferredWidth = 280;
            inputSlot.flexibleWidth = 1;
            SetStretch(tradingGroupInput.GetComponent<RectTransform>(), 0, 0, 2, 2);
            tradingGroupInput.characterLimit = 2000;
            tradingGroupInput.textComponent.richText = false;
            tradingGroupInput.onSubmit.AddListener(_ => SendTradingGroupMessage());
            tradingGroupSend = AddAmountModalButton(inputRow.transform, ChatText("GROUP_SEND"), 94, SendTradingGroupMessage).GetComponent<KButton>();
            var sendLayout = tradingGroupSend.GetComponent<LayoutElement>();
            sendLayout.minHeight = sendLayout.preferredHeight = 40;
            tradingGroupStatus = CreateAmountModalText(body.transform, ChatText("GROUP_READY"), 12, FontStyles.Normal);
            tradingGroupStatus.color = StorageNetworkPanelPalette.MutedText;
            tradingGroupStatus.GetComponent<LayoutElement>().preferredHeight = 32;
            tradingGroupStatus.fontSize = 11;
            tradingGroupStatus.maxVisibleLines = 2;
            tradingGroupStatus.gameObject.AddComponent<ToolTip>();
            Canvas.ForceUpdateCanvases();
            RefreshTradingGroupChat();
        }

        private void SendTradingGroupMessage()
        {
            if (tradingGroupRoot == null || (!tradingGroupFloating && modalRoot != tradingGroupRoot) || !tradingGroupRoot.activeInHierarchy) return;
            var state = StorageNetworkTradingState.Instance;
            if (state == null) return;
            string failure = state.SendGroupChat(tradingGroupInput.text, GetActiveWorldFilterId());
            if (failure != null) { SetTextIfChanged(tradingGroupStatus, failure); return; }
            tradingGroupInput.SetTextWithoutNotify("");
            RefreshTradingGroupChat();
        }

        internal void UpdateTradingGroupChat(float dt)
        {
            if (tradingGroupRoot == null) return;
            if ((!tradingGroupFloating && modalRoot != tradingGroupRoot) || !tradingGroupRoot.activeInHierarchy)
            {
                StorageNetworkTradingState.Instance?.CancelGroupChat();
                tradingGroupRoot = null;
                tradingGroupRows.Clear();
                return;
            }
            tradingGroupRefresh += dt;
            if (tradingGroupRefresh < 0.25f) return;
            tradingGroupRefresh = 0;
            RefreshTradingGroupChat();
        }

        private void ToggleTradingGroupFloat()
        {
            tradingGroupFloating = !tradingGroupFloating;
            var overlay = tradingGroupRoot.GetComponent<Image>();
            overlay.enabled = !tradingGroupFloating;
            overlay.raycastTarget = !tradingGroupFloating;
            if (tradingGroupFloating)
            {
                if (modalRoot == tradingGroupRoot) modalRoot = null;
                Canvas canvas = GetComponentInParent<Canvas>();
                tradingGroupRoot.transform.SetParent(canvas.transform, false);
                Stretch(tradingGroupRoot.GetComponent<RectTransform>(), 0, 0);
                tradingGroupWindow.sizeDelta = new Vector2(Mathf.Min(660, tradingGroupFullSize.x), Mathf.Min(620, tradingGroupFullSize.y));
                var host = tradingGroupRoot.GetComponent<StorageNetworkFloatingChatHost>() ?? tradingGroupRoot.AddComponent<StorageNetworkFloatingChatHost>();
                host.Owner = this;
                host.enabled = true;
            }
            else
            {
                CloseModal();
                gameObject.SetActive(true);
                tradingGroupRoot.transform.SetParent(transform, false);
                Stretch(tradingGroupRoot.GetComponent<RectTransform>(), 0, 0);
                modalRoot = tradingGroupRoot;
                tradingGroupRoot.GetComponent<StorageNetworkFloatingChatHost>().enabled = false;
                tradingGroupWindow.sizeDelta = tradingGroupFullSize;
            }
            SetTextIfChanged(tradingGroupFloatLabel, ChatText(tradingGroupFloating ? "GROUP_EXPAND" : "GROUP_FLOAT"));
            StorageNetworkWindowDrag.ClampToScreen(tradingGroupWindow);
            Canvas.ForceUpdateCanvases();
            foreach (var row in tradingGroupRows.Values) { row.SetActive(false); Util.KDestroyGameObject(row); }
            tradingGroupRows.Clear();
            tradingGroupVersion = -1;
            RefreshTradingGroupChat();
        }

        private void CloseTradingGroupChat()
        {
            StorageNetworkTradingState.Instance?.CancelGroupChat();
            if (modalRoot == tradingGroupRoot) modalRoot = null;
            if (tradingGroupRoot != null) { tradingGroupRoot.SetActive(false); Destroy(tradingGroupRoot); }
            tradingGroupRoot = null;
            tradingGroupFloating = false;
            tradingGroupRows.Clear();
        }

        private void RefreshTradingGroupChat()
        {
            var state = StorageNetworkTradingState.Instance;
            if (state?.Market == null || tradingGroupRoot == null) return;
            var market = state.Market;
            tradingGroupSend.isInteractable = !state.GroupChatBusy;
            SetTextIfChanged(tradingGroupStatus, state.GroupChatStatus ?? ChatText("GROUP_READY"));
            tradingGroupStatus.GetComponent<ToolTip>().toolTip = TradingText.Render(state.GroupChatStatus ?? ChatText("GROUP_READY"));
            string roster = string.Join(",", market.Suppliers.Select(s => s.Id));
            if (tradingGroupRoster != roster)
            {
                tradingGroupRoster = roster;
                foreach (var existing in tradingGroupRows)
                    if (!existing.Key.Player) SetTextIfChanged(existing.Value.transform.Find("Speaker").GetComponent<TextMeshProUGUI>(), GroupSpeaker(existing.Key, market));
                foreach (Transform child in tradingGroupMembers) { child.gameObject.SetActive(false); Util.KDestroyGameObject(child.gameObject); }
                foreach (var supplier in market.Suppliers)
                {
                    int id = supplier.Id;
                    string mention = "@" + StorageNetworkTradingState.DisplaySupplierName(supplier) + "#" + id + " ";
                    var button = AddAmountModalButton(tradingGroupMembers, StorageNetworkTradingState.DisplaySupplierName(supplier) + "\n#" + id + " · " + StorageNetworkTradingState.SpecialtyName(supplier.Specialty), 100, () =>
                    {
                        tradingGroupInput.text += mention;
                        tradingGroupInput.ActivateInputField();
                        tradingGroupInput.caretPosition = tradingGroupInput.text.Length;
                    });
                    button.GetComponent<LayoutElement>().flexibleWidth = 1;
                    button.GetComponent<LayoutElement>().minHeight = button.GetComponent<LayoutElement>().preferredHeight = 62;
                    var label = button.GetComponentInChildren<TextMeshProUGUI>();
                    label.fontSize = 11;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.maxVisibleLines = 2;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    SetStretch(label.rectTransform(), 50, 6, 6, 6);
                    var portrait = new GameObject("MemberPortrait", typeof(RectTransform), typeof(StorageNetworkTradingPortrait));
                    portrait.transform.SetParent(button.transform, false);
                    var portraitRect = portrait.GetComponent<RectTransform>();
                    portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = new Vector2(0, 0.5f);
                    portraitRect.sizeDelta = new Vector2(36, 36);
                    portraitRect.anchoredPosition = new Vector2(7, 0);
                    portrait.GetComponent<StorageNetworkTradingPortrait>().Bind(supplier);
                    button.AddComponent<ToolTip>().toolTip = TradingText.Render(StorageNetworkTradingState.DisplaySupplierName(supplier) + " #" + id + " · " + StorageNetworkTradingState.SpecialtyName(supplier.Specialty));
                }
            }
            market.GroupChat = market.GroupChat ?? new TradingGroupChat();
            if (tradingGroupVersion == market.GroupChat.Version) return;
            tradingGroupVersion = market.GroupChat.Version;
            bool atBottom = tradingGroupRows.Count == 0 || tradingGroupScroll.verticalNormalizedPosition < 0.05f;
            var visible = market.GroupChat.Messages.Skip(Math.Max(0, market.GroupChat.Messages.Count - 64)).ToList();
            foreach (var old in tradingGroupRows.Keys.Where(m => !visible.Contains(m)).ToList())
            {
                tradingGroupRows[old].SetActive(false);
                Util.KDestroyGameObject(tradingGroupRows[old]); tradingGroupRows.Remove(old);
            }
            foreach (var message in visible)
            {
                if (tradingGroupRows.ContainsKey(message)) continue;
                var supplier = market.Suppliers.Find(s => s.Id == message.SupplierId) ?? new TradingSupplier { Id = message.SupplierId, Name = message.Name, Appearance = message.Appearance };
                CreateTradingChatBubble(tradingGroupMessages, supplier, new TradingDialogueLine { Player = message.Player, Text = message.Text }, true, GroupSpeaker(message, market));
                var row = tradingGroupMessages.GetChild(tradingGroupMessages.childCount - 1).gameObject;
                tradingGroupRows[message] = row;
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tradingGroupMessages);
            if (atBottom) tradingGroupScroll.verticalNormalizedPosition = 0;
        }

        private static string GroupSpeaker(TradingGroupMessage message, TradingMarket market)
        {
            var current = market.Suppliers.Find(s => s.Id == message.SupplierId && s.Departure > StorageNetworkTradingState.Cycle);
            string name = current == null ? message.Name : StorageNetworkTradingState.DisplaySupplierName(current);
            string specialty = current?.Specialty ?? message.Specialty;
            return name + " #" + message.SupplierId + (string.IsNullOrEmpty(specialty) ? "" : " · " + StorageNetworkTradingState.SpecialtyName(specialty)) +
                (current == null ? " · " + ChatText("GROUP_DEPARTED") : "");
        }
    }
}
