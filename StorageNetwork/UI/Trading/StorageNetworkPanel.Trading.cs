using System;
using System.Globalization;
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
        // Same list background as the main storage panel; other surfaces use its shared palette.
        private static readonly Color TradingBackground = new Color(0.80f, 0.79f, 0.74f, 1f);
        private GameObject tradingRoot;
        private RectTransform tradingContent;
        private ScrollRect tradingScrollRect;
        private StorageNetworkKeyedRowCache tradingRows;
        private readonly List<TradingGood> tradingVisibleGoods = new List<TradingGood>(20);
        private TextMeshProUGUI tradingSummary;
        private TextMeshProUGUI tradingStatus;
        private TextMeshProUGUI tradingPageLabel;
        private TextMeshProUGUI tradingQuoteText;
        private TextMeshProUGUI tradingQuantityLabel;
        private KButton tradingConfirmButton;
        private KButton tradingMaximumButton;
        private GameObject tradingAmountDialogRoot;
        private KSlider tradingAmountSlider;
        private bool tradingAmountUpdating;
        private string tradingDialogFeedback;
        private RectTransform tradingHistoryContent;
        private StorageNetworkKeyedRowCache tradingHistoryRows;
        private TextMeshProUGUI tradingHistoryTitle;
        private TextMeshProUGUI tradingHistoryTotals;
        private int tradingHistoryVersion = -1;
        private TradingGood tradingQuoteGood;
        private int tradingQuoteSupplierId;
        private string tradingQuoteSupplierName;
        private bool tradingQuoteBuy;
        private int tradingQuoteWorldId;
        private TradingQuote tradingQuote;
        private readonly TextMeshProUGUI[] supplierLabels = new TextMeshProUGUI[5];
        private readonly Image[] supplierTimeFills = new Image[5];
        private readonly StorageNetworkTradingPortrait[] supplierPortraits = new StorageNetworkTradingPortrait[5];
        private KInputTextField tradingAmountInput;
        private KInputTextField tradingSearchInput;
        private int tradingSlot;
        private int tradingPage;
        private float tradingRefreshElapsed;
        private int tradingLayoutFrame = -1;
        private bool resetTradingScrollAfterLayout;
        private string tradingRowsSignature;
        private string tradingFilter = string.Empty;
        private int tradingQuickFilter;
        private TextMeshProUGUI tradingQuickFilterLabel;
        private TextMeshProUGUI tradingEventLabel;
        private TextMeshProUGUI tradingDeliveryText;
        private KButton tradingLocateButton;
        private readonly List<Storage> tradingLocateStorages = new List<Storage>();
        private int tradingLocateIndex;
        private string tradingMessage = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DADEC6AE34C8");

        private void ToggleTradingWindow()
        {
            if (tradingRoot == null) BuildTradingWindow();
            bool open = !tradingRoot.activeSelf;
            if (!open) { CloseTradingWindow(); return; }
            tradingRoot.SetActive(true);
            tradingRoot.transform.SetAsLastSibling();
            RefreshTradingWindow();
            RequestTradingLayout(false);
        }

        private void CloseTradingWindow()
        {
            if (tradingAmountInput != null) tradingAmountInput.DeactivateInputField();
            if (tradingAmountDialogRoot != null && modalRoot == tradingAmountDialogRoot) CloseModal();
            if (tradingChartRoot != null && modalRoot == tradingChartRoot) CloseModal();
            if (tradingDialogueRoot != null && modalRoot == tradingDialogueRoot) CloseModal();
            if (tradingIdentityRoot != null && modalRoot == tradingIdentityRoot) CloseModal();
            if (tradingAiSettingsRoot != null && modalRoot == tradingAiSettingsRoot) CloseModal();
            if (tradingGroupRoot != null && modalRoot == tradingGroupRoot) { StorageNetworkTradingState.Instance?.CancelGroupChat(); CloseModal(); }
            tradingSearchInput?.DeactivateInputField();
            tradingLayoutFrame = -1;
            resetTradingScrollAfterLayout = false;
            if (tradingRoot != null) tradingRoot.SetActive(false);
        }

        private void BuildTradingWindow()
        {
            tradingRoot = CreateBox("TradingMarket", transform, StorageNetworkPanelPalette.WindowBackground);
            tradingRoot.SetActive(false);
            ApplyThinBoxSprite(tradingRoot.GetComponent<Image>());
            // Match the main window: web_box, an 8px inset light content plate, then the inner surface.
            GameObject contentPlate = CreateBox("Content", tradingRoot.transform, StorageNetworkPanelPalette.ContentBackground);
            SetStretch(contentPlate.GetComponent<RectTransform>(), 8, 8, 8, 54);
            GameObject background = CreateBox("Background", contentPlate.transform, TradingBackground);
            Stretch(background.GetComponent<RectTransform>(), 8, 8);
            RectTransform rect = tradingRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1;
            rect.sizeDelta = new Vector2(Mathf.Min(1120, Screen.width / scale - 32), Mathf.Min(860, Screen.height / scale - 32));
            rect.anchoredPosition = Vector2.zero;
            GameObject header = CreateBox("Header", tradingRoot.transform, StorageNetworkPanelPalette.HeaderBackground);
            SetTopStretch(header.GetComponent<RectTransform>(), 8, 8, 8, 40);
            header.AddComponent<StorageNetworkWindowDrag>().Configure(rect, "tradingMarket");
            tradingSummary = CreateText("Title", header.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FAD7E42F054D"), 16, TextAlignmentOptions.MidlineLeft);
            tradingSummary.fontStyle = FontStyles.Bold;
            tradingSummary.textWrappingMode = TextWrappingModes.NoWrap;
            tradingSummary.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(tradingSummary.rectTransform(), 12, 0);
            tradingSummary.rectTransform().offsetMax = new Vector2(-160, 0);
            BuildTradingAiSettingsButton(header.transform);
            BuildTradingGroupChatButton(header.transform);
            GameObject close = CreateCloseIconButton("Close", header.transform, CloseTradingWindow);
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.sizeDelta = new Vector2(28, 26);
            closeRect.anchoredPosition = new Vector2(-8, 0);

            GameObject suppliers = CreateBox("Suppliers", contentPlate.transform, TradingBackground);
            SetTopStretch(suppliers.GetComponent<RectTransform>(), 8, 8, 8, 68);
            HorizontalLayoutGroup supplierLayout = suppliers.AddComponent<HorizontalLayoutGroup>();
            supplierLayout.padding = new RectOffset(6, 6, 6, 6);
            supplierLayout.spacing = 6;
            supplierLayout.childControlWidth = supplierLayout.childControlHeight = true;
            supplierLayout.childForceExpandWidth = supplierLayout.childForceExpandHeight = true;
            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                GameObject button = CreateGameButton("Supplier" + i, suppliers.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_13E43251E905"), () =>
                {
                    tradingSlot = slot;
                    tradingPage = 0;
                    tradingQuoteGood = null;
                    tradingRowsSignature = null;
                    RefreshTradingWindow();
                });
                supplierLabels[i] = button.GetComponentInChildren<TextMeshProUGUI>();
                supplierLabels[i].textWrappingMode = TextWrappingModes.NoWrap;
                supplierLabels[i].maxVisibleLines = 2;
                supplierLabels[i].alignment = TextAlignmentOptions.MidlineLeft;
                SetStretch(supplierLabels[i].rectTransform(), 54, 30, 15, 5);
                GameObject identityButton = CreateGameButton("IdentityCard", button.transform, "i", () =>
                {
                    var visitors = StorageNetworkTradingState.Instance?.Market?.Suppliers;
                    if (visitors != null && slot < visitors.Count) ShowTradingIdentityCard(visitors[slot].Id);
                });
                RectTransform identityRect = identityButton.GetComponent<RectTransform>();
                identityRect.anchorMin = identityRect.anchorMax = identityRect.pivot = new Vector2(1, 1);
                identityRect.sizeDelta = new Vector2(22, 22);
                identityRect.anchoredPosition = new Vector2(-5, -5);
                identityButton.AddComponent<ToolTip>().toolTip = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_100256B8A848"));
                GameObject portraitSlot = new GameObject("SupplierPortrait", typeof(RectTransform), typeof(StorageNetworkTradingPortrait));
                portraitSlot.transform.SetParent(button.transform, false);
                RectTransform portraitRect = portraitSlot.GetComponent<RectTransform>();
                portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = new Vector2(0, 0.5f);
                portraitRect.sizeDelta = new Vector2(40, 40);
                portraitRect.anchoredPosition = new Vector2(8, 5);
                supplierPortraits[i] = portraitSlot.GetComponent<StorageNetworkTradingPortrait>();
                GameObject timeTrack = CreatePlainImage("VisitTimeTrack", button.transform, new Color(0.12f, 0.14f, 0.18f, 1));
                Image trackImage = timeTrack.GetComponent<Image>();
                trackImage.type = Image.Type.Simple;
                trackImage.raycastTarget = false;
                RectTransform trackRect = timeTrack.GetComponent<RectTransform>();
                trackRect.anchorMin = Vector2.zero;
                trackRect.anchorMax = new Vector2(1, 0);
                trackRect.offsetMin = new Vector2(8, 6);
                trackRect.offsetMax = new Vector2(-8, 11);
                GameObject fill = CreatePlainImage("RemainingVisitTime", timeTrack.transform, Color.white);
                supplierTimeFills[i] = fill.GetComponent<Image>();
                supplierTimeFills[i].type = Image.Type.Simple;
                supplierTimeFills[i].raycastTarget = false;
                Stretch(fill.GetComponent<RectTransform>(), 0, 0);
                button.AddComponent<ToolTip>();
            }

            GameObject controls = CreateBox("Controls", contentPlate.transform, StorageNetworkPanelPalette.CardBackground);
            SetTopStretch(controls.GetComponent<RectTransform>(), 8, 8, 82, 38);
            HorizontalLayoutGroup controlsLayout = controls.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.padding = new RectOffset(8, 8, 5, 5);
            controlsLayout.spacing = 8;
            controlsLayout.childControlWidth = controlsLayout.childControlHeight = true;
            controlsLayout.childForceExpandWidth = false;
            controlsLayout.childForceExpandHeight = true;
            TradingLabel(controls.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_98C2FE486054"), 80);
            tradingSearchInput = CreateFixedTextInput(controls.transform, "TradingSearch", "", 240, 28, 12);
            tradingSearchInput.onValueChanged.AddListener(value => { tradingFilter = value ?? ""; tradingPage = 0; });
            GameObject previous = CreateGameButton("Previous", controls.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C9B9AE7A6144"), () => { tradingPage = Math.Max(0, tradingPage - 1); RefreshTradingWindow(); });
            previous.AddComponent<LayoutElement>().preferredWidth = 64;
            GameObject next = CreateGameButton("Next", controls.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_8A8542F69648"), () => { tradingPage++; RefreshTradingWindow(); });
            next.AddComponent<LayoutElement>().preferredWidth = 64;
            tradingPageLabel = TradingLabel(controls.transform, "", 90);
            GameObject quickFilter = CreateGameButton("QuickFilter", controls.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D11D7CBA555B"), () =>
            {
                tradingQuickFilter = (tradingQuickFilter + 1) % 3;
                tradingPage = 0;
                tradingRowsSignature = null;
                RefreshTradingWindow();
            });
            quickFilter.AddComponent<LayoutElement>().preferredWidth = 96;
            tradingQuickFilterLabel = quickFilter.GetComponentInChildren<TextMeshProUGUI>();
            quickFilter.AddComponent<ToolTip>().toolTip = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_223963F053C4"));
            tradingEventLabel = TradingLabel(controls.transform, "", 180);
            tradingEventLabel.raycastTarget = true;
            tradingEventLabel.gameObject.AddComponent<ToolTip>().toolTip = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BE55C2A75D1B"));

            GameObject workspace = new GameObject("TradingWorkspace", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            workspace.transform.SetParent(contentPlate.transform, false);
            SetStretch(workspace.GetComponent<RectTransform>(), 8, 8, 70, 124);
            HorizontalLayoutGroup workspaceLayout = workspace.GetComponent<HorizontalLayoutGroup>();
            workspaceLayout.spacing = 8;
            workspaceLayout.childControlWidth = workspaceLayout.childControlHeight = true;
            workspaceLayout.childForceExpandWidth = false;
            workspaceLayout.childForceExpandHeight = true;
            GameObject goodsPane = CreateBox("GoodsPane", workspace.transform, TradingBackground);
            LayoutElement goodsPaneLayout = goodsPane.AddComponent<LayoutElement>();
            goodsPaneLayout.minWidth = 440;
            goodsPaneLayout.flexibleWidth = 1;
            GameObject listHeader = CreateBox("GoodsTitle", goodsPane.transform, StorageNetworkPanelPalette.SettingsHeaderBackground);
            SetTopStretch(listHeader.GetComponent<RectTransform>(), 0, 0, 0, 28);
            TextMeshProUGUI listTitle = CreateText("Title", listHeader.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6678CEA99B81"), 12, TextAlignmentOptions.MidlineLeft);
            SetStretch(listTitle.rectTransform(), 14, 316, 0, 0);
            TextMeshProUGUI priceTitle = CreateText("PriceTitle", listHeader.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E6B3D9BC208C"), 12, TextAlignmentOptions.MidlineLeft);
            SetTradingRightColumn(priceTitle.rectTransform(), 128, 180, 28);
            TextMeshProUGUI actionTitle = CreateText("ActionTitle", listHeader.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_919DDBBD91C0"), 12, TextAlignmentOptions.Center);
            SetTradingRightColumn(actionTitle.rectTransform(), 28, 92, 28);
            RectTransform viewport = CreateScrollViewport(goodsPane.transform, "Goods", out tradingContent, 32, 0, 0, 0, 8);
            viewport.GetComponent<Image>().color = TradingBackground;
            tradingRows = new StorageNetworkKeyedRowCache(tradingContent, 40, 6);
            Scrollbar scrollbar = CreateScrollbar(goodsPane.transform, 32, 0);
            WireScrollRect(viewport.gameObject, tradingContent, scrollbar, 26);
            tradingScrollRect = viewport.GetComponent<ScrollRect>();
            BuildTradingHistoryPane(workspace.transform);
            GameObject footer = CreateBox("Status", contentPlate.transform, StorageNetworkPanelPalette.SettingsHeaderBackground);
            RectTransform footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = Vector2.zero;
            footerRect.anchorMax = new Vector2(1, 0);
            footerRect.offsetMin = new Vector2(8, 8);
            footerRect.offsetMax = new Vector2(-8, 64);
            tradingStatus = CreateText("Message", footer.transform, "", 12, TextAlignmentOptions.MidlineLeft);
            tradingStatus.textWrappingMode = TextWrappingModes.NoWrap;
            tradingStatus.overflowMode = TextOverflowModes.Ellipsis;
            tradingStatus.maxVisibleLines = 2;
            Stretch(tradingStatus.rectTransform(), 10, 3);
            BuildTradingDialogueButton(footer.transform);

            AddTradingWindowBorder(scale);
            tradingRoot.SetActive(false);
        }

        private void AddTradingWindowBorder(float canvasScale)
        {
            // Draw inside the window bounds, after its contents, so neither a sprite's
            // transparent edge nor viewport clipping can hide the outer black line.
            float thickness = 1f / canvasScale;
            AddTradingBorderEdge("BorderLeft", Vector2.zero, new Vector2(0, 1),
                Vector2.zero, new Vector2(thickness, 0));
            AddTradingBorderEdge("BorderRight", new Vector2(1, 0), Vector2.one,
                new Vector2(-thickness, 0), Vector2.zero);
            AddTradingBorderEdge("BorderBottom", Vector2.zero, new Vector2(1, 0),
                Vector2.zero, new Vector2(0, thickness));
            AddTradingBorderEdge("BorderTop", new Vector2(0, 1), Vector2.one,
                new Vector2(0, -thickness), Vector2.zero);
        }

        private void AddTradingBorderEdge(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject edge = CreatePlainImage(name, tradingRoot.transform, Color.black);
            Image image = edge.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            RectTransform rect = edge.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void ShowTradingAmountDialog()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            tradingDialogFeedback = null;
            modalRoot = CreateAmountModalFrame(tradingQuoteBuy ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D8D7A6452DE9") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7D6C856F7E96"), 620, 510, out GameObject body);
            tradingAmountDialogRoot = modalRoot;
            GameObject card = CreatePlainImage("TradeQuote", body.transform, StorageNetworkPanelPalette.RowBackground);
            card.AddComponent<LayoutElement>().preferredHeight = 124;
            card.AddComponent<RectMask2D>();
            tradingQuoteText = CreateText("TradeDetails", card.transform, "", 12, TextAlignmentOptions.MidlineLeft);
            tradingQuoteText.color = StorageNetworkPanelPalette.BodyText;
            tradingQuoteText.textWrappingMode = TextWrappingModes.Normal;
            tradingQuoteText.overflowMode = TextOverflowModes.Ellipsis;
            tradingQuoteText.maxVisibleLines = 6;
            Stretch(tradingQuoteText.rectTransform(), 10, 6);
            GameObject deliveryCard = CreatePlainImage("DeliveryPreview", body.transform, StorageNetworkPanelPalette.CardBackground);
            deliveryCard.AddComponent<LayoutElement>().preferredHeight = 68;
            tradingDeliveryText = CreateText("Delivery", deliveryCard.transform, "", 11, TextAlignmentOptions.MidlineLeft);
            tradingDeliveryText.color = StorageNetworkPanelPalette.BodyText;
            tradingDeliveryText.textWrappingMode = TextWrappingModes.NoWrap;
            tradingDeliveryText.overflowMode = TextOverflowModes.Ellipsis;
            tradingDeliveryText.maxVisibleLines = 3;
            tradingDeliveryText.raycastTarget = true;
            tradingDeliveryText.gameObject.AddComponent<ToolTip>();
            SetStretch(tradingDeliveryText.rectTransform(), 10, 100, 4, 4);
            GameObject locate = CreateGameButton("LocateDelivery", deliveryCard.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2D533BF10CD6"), LocateTradingStorage);
            SetTradingRightColumn(locate.GetComponent<RectTransform>(), 8, 86, 26);
            tradingLocateButton = locate.GetComponent<KButton>();
            tradingLocateIndex = 0;
            tradingQuantityLabel = CreateAmountModalText(body.transform, "", 12, FontStyles.Bold);
            tradingQuantityLabel.color = StorageNetworkPanelPalette.BodyText;
            tradingAmountSlider = CreateAmountSlider(body.transform, 1);
            tradingAmountSlider.wholeNumbers = tradingQuoteGood.ByUnit;
            tradingAmountInput = CreateAmountInputRow(body.transform);
            tradingAmountInput.onValueChanged.AddListener(_ =>
            {
                if (tradingAmountUpdating) return;
                tradingDialogFeedback = null;
                RefreshTradingQuote();
            });
            tradingAmountSlider.onValueChanged.AddListener(value =>
            {
                if (tradingAmountUpdating) return;
                tradingDialogFeedback = null;
                SetTradingDialogAmount(tradingQuoteGood.ByUnit ? Math.Round(value) : Math.Floor(value * 1000d) / 1000d);
            });
            GameObject shortcuts = AddAmountModalRow(body.transform, 8, 28);
            AddFooterSpacer(shortcuts.transform);
            GameObject maximum = AddAmountModalButton(shortcuts.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_5C55A67935AF"), 86, SetMaximumTradingAmount);
            tradingMaximumButton = maximum.GetComponent<KButton>();
            GameObject footer = AddAmountModalRow(body.transform, 8, 30);
            AddFooterSpacer(footer.transform);
            AddAmountModalButton(footer.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2CD0F3BE8738"), 92, CloseModal);
            GameObject confirm = AddAmountModalButton(footer.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_39248F1A42C6"), 100, ConfirmTradingQuote);
            confirm.GetComponent<KImage>().colorStyleSetting = KleiPinkStyle();
            confirm.GetComponent<KImage>().ApplyColorStyleSetting();
            tradingConfirmButton = confirm.GetComponent<KButton>();
            var initial = StorageNetworkTradingState.Instance.GetQuote(tradingQuoteSupplierId, tradingQuoteGood, 1, tradingQuoteBuy, GetActiveWorldFilterId());
            double remembered = StorageNetworkTradingState.Instance.LastAmount(tradingQuoteGood, tradingQuoteBuy, tradingQuoteGood.ByUnit ? 1 : 100);
            SetTradingDialogAmount(Math.Min(initial.MaximumAmount, remembered));
        }

        private void SetTradingDialogAmount(double amount)
        {
            if (tradingAmountDialogRoot == null || modalRoot != tradingAmountDialogRoot) return;
            tradingAmountUpdating = true;
            tradingAmountInput.GetComponent<StorageNetworkNumberInputField>().SetAmount((float)amount);
            tradingAmountUpdating = false;
            RefreshTradingQuote();
        }

        private void BuildTradingHistoryPane(Transform parent)
        {
            GameObject pane = CreateBox("RecentHistory", parent, TradingBackground);
            LayoutElement layout = pane.AddComponent<LayoutElement>();
            layout.preferredWidth = 300;
            layout.minWidth = 260;
            GameObject header = CreateBox("HistoryHeader", pane.transform, StorageNetworkPanelPalette.SettingsHeaderBackground);
            SetTopStretch(header.GetComponent<RectTransform>(), 0, 0, 0, 28);
            tradingHistoryTitle = CreateText("Title", header.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_AE77FA5E5263"), 12, TextAlignmentOptions.MidlineLeft);
            Stretch(tradingHistoryTitle.rectTransform(), 8, 0);
            tradingHistoryTotals = CreateText("Totals", pane.transform, "", 11, TextAlignmentOptions.MidlineLeft);
            tradingHistoryTotals.color = StorageNetworkPanelPalette.BodyText;
            SetTopStretch(tradingHistoryTotals.rectTransform(), 6, 6, 32, 26);
            tradingHistoryTotals.textWrappingMode = TextWrappingModes.NoWrap;
            tradingHistoryTotals.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform viewport = CreateScrollViewport(pane.transform, "History", out tradingHistoryContent, 62, 0, 0, 0, 8);
            viewport.GetComponent<Image>().color = TradingBackground;
            tradingHistoryRows = new StorageNetworkKeyedRowCache(tradingHistoryContent, 0, 0);
            WireScrollRect(viewport.gameObject, tradingHistoryContent, CreateScrollbar(pane.transform, 62, 0), 24);
        }

        private void RefreshTradingHistory(TradingMarket market)
        {
            if (tradingHistoryVersion == market.NextTradeId) return;
            tradingHistoryVersion = market.NextTradeId;
            SetTextIfChanged(tradingHistoryTitle, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DE52F420EB2D", TradingText.Argument((object)(market.Receipts.Count), null, 0)));
            string income = ColorUtility.ToHtmlStringRGB(StorageNetworkPanelPalette.PositiveStatus);
            string expense = ColorUtility.ToHtmlStringRGB(StorageNetworkPanelPalette.DangerStatus);
            SetTextIfChanged(tradingHistoryTotals, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_4242349E33B3", TradingText.Argument((object)(income), null, 0), TradingText.Argument((object)(market.Receipts.Where(r => !r.Buy).Sum(r => r.Total)), "N0", 0), TradingText.Argument((object)(expense), null, 0), TradingText.Argument((object)(market.Receipts.Where(r => r.Buy).Sum(r => r.Total)), "N0", 0)));
            tradingHistoryRows.Begin();
            foreach (TradingReceipt receipt in market.Receipts.AsEnumerable().Reverse())
                tradingHistoryRows.Use("receipt:" + receipt.Id, () => CreateTradingReceiptRow(receipt));
            if (market.Receipts.Count == 0)
                tradingHistoryRows.Use("empty", () =>
                {
                    TextMeshProUGUI empty = TradingLabel(tradingHistoryContent, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FDD458B3264D"), 0);
                    empty.GetComponent<LayoutElement>().preferredHeight = 44;
                    empty.textWrappingMode = TextWrappingModes.Normal;
                    return empty.gameObject;
                });
            tradingHistoryRows.Commit();
            LayoutRebuilder.MarkLayoutForRebuild(tradingHistoryContent);
        }

        private static TextMeshProUGUI TradingLabel(Transform parent, string text, float width)
        {
            TextMeshProUGUI label = CreateText("Text", parent, text, 12, TextAlignmentOptions.MidlineLeft);
            label.color = StorageNetworkPanelPalette.BodyText;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.maskable = true;
            LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            return label;
        }

        private void UpdateTradingWindow(float dt)
        {
            if (tradingRoot == null || !tradingRoot.activeInHierarchy) return;
            RunDeferredTradingLayout();
            tradingRefreshElapsed += dt;
            if (tradingRefreshElapsed < 1) return;
            tradingRefreshElapsed = 0;
            RefreshTradingWindow();
            RefreshTradingPriceChart();
        }

        private void RefreshTradingWindow()
        {
            StorageNetworkTradingState state = StorageNetworkTradingState.Instance;
            if (state == null || state.Market == null) { tradingStatus.text = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_289A3F2079CF")); return; }
            using var displayRefresh = state.BeginDisplayRefresh();
            int worldId = GetActiveWorldFilterId();
            state.Refresh(worldId);
            double cycle = StorageNetworkTradingState.Cycle;
            TradingSupplier selected = state.Market.Suppliers[tradingSlot];
            SetTextIfChanged(tradingQuickFilterLabel, new[] { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D11D7CBA555B"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_29C8539FD571"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D8390476F99E") }[tradingQuickFilter]);
            string marketEvent = TradingMarket.EventName(selected.Specialty, cycle);
            decimal marketEventFactor = TradingMarket.EventFactor(selected.Specialty, cycle);
            SetTextIfChanged(tradingEventLabel, ColoredTradingText(marketEvent, marketEventFactor > 1 ? StorageNetworkPanelPalette.DangerStatus :
                marketEventFactor < 1 ? StorageNetworkPanelPalette.PositiveStatus : StorageNetworkPanelPalette.MutedText));
            SetTextIfChanged(tradingSummary, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FDDC805F60FB", TradingText.Argument((object)(state.Market.Balance), "0.#####", 0), TradingText.Argument((object)(StorageNetworkWorldDisplay.GetWorldName(worldId)), null, 0)));
            for (int i = 0; i < 5; i++)
            {
                TradingSupplier supplier = state.Market.Suppliers[i];
                double remaining = Math.Max(0, supplier.Departure - cycle);
                double duration = Math.Max(0.001, supplier.Departure - supplier.Arrival);
                Image timeFill = supplierTimeFills[i];
                // Each visit starts full and drains relative to its own total duration.
                timeFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)(remaining / duration)), 1);
                timeFill.color = remaining <= 1 ? new Color(0.86f, 0.40f, 0.40f, 1)
                    : remaining <= 2 ? new Color(0.90f, 0.70f, 0.36f, 1) : new Color(0.40f, 0.74f, 0.68f, 1);
                supplierPortraits[i].Bind(supplier);
                SetTextIfChanged(supplierLabels[i], $"<b>{(i == tradingSlot ? "▶ " : "")}{supplier.Name ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8A11D7DF26E")}</b>\n{StorageNetworkTradingState.SpecialtyName(supplier.Specialty)} #{supplier.Id}");
                supplierLabels[i].GetComponentInParent<ToolTip>().toolTip = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_EA3C0C11A4B9", TradingText.Argument((object)(supplier.Name ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8A11D7DF26E")), null, 0), TradingText.Argument((object)(StorageNetworkTradingState.SpecialtyName(supplier.Specialty)), null, 0), TradingText.Argument((object)(supplier.Id), null, 0), TradingText.Argument((object)(supplier.BuyLimit - supplier.Bought), "0.###", 0), TradingText.Argument((object)(supplier.SellLimit - supplier.Sold), "0.###", 0)) + StorageNetworkTradingState.ProfileName(supplier) +
                    TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D34F20700273", TradingText.Argument((object)(duration), "0.#", 0), TradingText.Argument((object)(remaining), "0.00", 0)) +
                    TradingMarket.PreferenceName(supplier) + TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_93192E1DBBFB") +
                    (TradingCharacters.Description(supplier) is string description ? "\n\n" + description : ""));
            }
            SetTextIfChanged(tradingStatus, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F23E40F78F37", TradingText.Argument((object)(tradingMessage), null, 0), TradingText.Argument((object)(selected.BuyLimit - selected.Bought), "0.###", 0), TradingText.Argument((object)(selected.BuyLimit), "0", 0), TradingText.Argument((object)(selected.SellLimit - selected.Sold), "0.###", 0), TradingText.Argument((object)(selected.SellLimit), "0", 0)));
            RefreshTradingDialogue(selected);
            // Only list membership affects structure; changing amounts or prices updates existing views.
            List<TradingGood> catalog = state.Catalog.Where(g => g.Category == selected.Specialty &&
                (string.IsNullOrEmpty(tradingFilter) || g.Name.IndexOf(tradingFilter, StringComparison.CurrentCultureIgnoreCase) >= 0 || g.Id.IndexOf(tradingFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                MatchesTradingQuickFilter(g, selected, state, worldId)).ToList();
            string signature = $"{selected.Id}|{tradingPage}|{tradingFilter}|" + string.Join(",", catalog.Select(g => g.Id));
            if (signature != tradingRowsSignature)
                ReconcileTradingRows(state, selected, catalog, signature);
            foreach (TradingGood good in tradingVisibleGoods)
                if (tradingRows.TryGetMetadata(good.Id, out TradingGoodRowView view))
                    UpdateTradingGoodRow(view, state, selected, cycle, worldId);
            RefreshTradingHistory(state.Market);
            RefreshTradingQuote();
        }

        private bool MatchesTradingQuickFilter(TradingGood good, TradingSupplier supplier, StorageNetworkTradingState state, int worldId)
        {
            bool stock = supplier.Stock.TryGetValue(good.Id, out double value) && value > 0;
            if (tradingQuickFilter == 1) return stock && state.GetQuote(supplier.Id, good, good.ByUnit ? 1 : 0.01, true, worldId).MaximumAmount > 0;
            if (tradingQuickFilter == 0 && stock) return true;
            bool available = state.DisplayAvailable(good, worldId) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
            if (tradingQuickFilter == 2) return available && state.GetQuote(supplier.Id, good, good.ByUnit ? 1 : 0.01, false, worldId).MaximumAmount > 0;
            return stock || available;
        }

        private void ReconcileTradingRows(StorageNetworkTradingState state, TradingSupplier selected, List<TradingGood> goods, string signature)
        {
            int count = goods.Count;
            int pages = Math.Max(1, (count + 19) / 20);
            tradingPage = Math.Min(tradingPage, pages - 1);
            SetTextIfChanged(tradingPageLabel, $"{tradingPage + 1} / {pages}");
            tradingRowsSignature = signature;
            tradingVisibleGoods.Clear();
            tradingVisibleGoods.AddRange(goods.Skip(tradingPage * 20).Take(20));
            tradingRows.Begin();
            foreach (TradingGood good in tradingVisibleGoods)
            {
                TradingGoodRowView created = null;
                tradingRows.Use(good.Id, () => CreateTradingGoodRow(good, out created));
                if (created != null) tradingRows.SetMetadata(good.Id, created);
            }
            if (count == 0)
                tradingRows.Use("empty", () =>
                {
                    TextMeshProUGUI empty = TradingLabel(tradingContent,
                        TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E83D08B85782"), 0);
                    empty.GetComponent<LayoutElement>().preferredHeight = 60;
                    empty.GetComponent<LayoutElement>().flexibleWidth = 1;
                    return empty.gameObject;
                });
            tradingRows.Commit();
            RequestTradingLayout(true);
        }

        private void RequestTradingLayout(bool resetScroll)
        {
            LayoutRebuilder.MarkLayoutForRebuild(tradingContent);
            if (tradingLayoutFrame < 0) tradingLayoutFrame = Time.frameCount + 1;
            resetTradingScrollAfterLayout |= resetScroll;
        }

        private void RunDeferredTradingLayout()
        {
            if (tradingLayoutFrame < 0 || Time.frameCount < tradingLayoutFrame) return;
            tradingLayoutFrame = -1;
            using var scope = Core.StorageNetworkFrameProfileTool.BeginWork(Core.StorageNetworkPerformanceArea.Layout);
            LayoutRebuilder.ForceRebuildLayoutImmediate(tradingContent);
            if (!resetTradingScrollAfterLayout) return;
            resetTradingScrollAfterLayout = false;
            tradingScrollRect.StopMovement();
            tradingScrollRect.verticalNormalizedPosition = 1;
        }

        private GameObject CreateTradingGoodRow(TradingGood good, out TradingGoodRowView view)
        {
            GameObject row = CreateBox("Good_" + good.Id, tradingContent, StorageNetworkPanelPalette.RowBackground);
            row.SetActive(false);
            row.AddComponent<RectMask2D>();
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = rowLayout.preferredHeight = 76;
            rowLayout.flexibleHeight = 0;
            GameObject iconObject = new GameObject("GoodIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(row.transform, false);
            Image icon = iconObject.GetComponent<Image>();
            var uiSprite = Def.GetUISprite(good.Id.ToTag(), "ui", false);
            icon.sprite = uiSprite?.first ?? Assets.GetSprite("unknown");
            icon.color = uiSprite != null ? uiSprite.second : Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.minWidth = iconLayout.preferredWidth = 44;
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(8, 0);
            iconRect.sizeDelta = new Vector2(44, 44);

            var rowView = new TradingGoodRowView { Good = good, Background = row.GetComponent<Image>() };
            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            textColumn.transform.SetParent(row.transform, false);
            SetStretch(textColumn.GetComponent<RectTransform>(), 60, 296, 4, 4);
            LayoutElement columnLayout = textColumn.GetComponent<LayoutElement>();
            columnLayout.minWidth = 120;
            columnLayout.preferredWidth = 200;
            columnLayout.flexibleWidth = 1;
            VerticalLayoutGroup columnGroup = textColumn.GetComponent<VerticalLayoutGroup>();
            columnGroup.childAlignment = TextAnchor.MiddleLeft;
            columnGroup.childControlWidth = columnGroup.childControlHeight = true;
            columnGroup.childForceExpandWidth = true;
            columnGroup.childForceExpandHeight = false;
            GameObject nameRow = AddAmountModalRow(textColumn.transform, 6, 22);
            TextMeshProUGUI name = TradingLabel(nameRow.transform, good.Name, 0);
            name.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            name.fontSize = 13;
            name.fontStyle = FontStyles.Bold;
            name.color = StorageNetworkPanelPalette.HeadingText;
            SetTradingLineHeight(name, 22);
            GameObject trendButton = CreateGameButton("PriceHistory", nameRow.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_501F8AA52560"), () => ShowTradingPriceChart(rowView.SupplierId, rowView.Good));
            LayoutElement trendLayout = trendButton.AddComponent<LayoutElement>();
            trendLayout.minWidth = trendLayout.preferredWidth = 40;
            trendLayout.minHeight = trendLayout.preferredHeight = 22;
            trendButton.AddComponent<ToolTip>().toolTip = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_36D384DF5163"));
            TextMeshProUGUI inventory = TradingLabel(textColumn.transform, "", 0);
            inventory.color = StorageNetworkPanelPalette.MutedText;
            SetTradingLineHeight(inventory, 38);
            inventory.maxVisibleLines = 2;
            inventory.fontSize = 11;
            TextMeshProUGUI prices = TradingLabel(row.transform, "", 180);
            prices.fontSize = 11;
            prices.maxVisibleLines = 2;
            SetTradingRightColumn(prices.rectTransform(), 108, 180, 56);
            rowView.Inventory = inventory;
            rowView.Prices = prices;
            GameObject actions = new GameObject("TradeActions", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            actions.transform.SetParent(row.transform, false);
            SetTradingRightColumn(actions.GetComponent<RectTransform>(), 8, 92, 56);
            actions.GetComponent<LayoutElement>().minWidth = actions.GetComponent<LayoutElement>().preferredWidth = 92;
            VerticalLayoutGroup actionsLayout = actions.GetComponent<VerticalLayoutGroup>();
            actionsLayout.spacing = 4;
            actionsLayout.childAlignment = TextAnchor.MiddleCenter;
            actionsLayout.childControlWidth = actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = false;
            GameObject buyButton = CreateGameButton("Buy", actions.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A1557EC3B077"), () => SelectTradingGood(rowView.SupplierId, rowView.Good, true));
            GameObject sellButton = CreateGameButton("Sell", actions.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FB28BA14091D"), () => SelectTradingGood(rowView.SupplierId, rowView.Good, false));
            foreach (GameObject button in new[] { buyButton, sellButton })
            {
                LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
                buttonLayout.minWidth = buttonLayout.preferredWidth = 92;
                buttonLayout.minHeight = buttonLayout.preferredHeight = 26;
            }
            rowView.Buy = buyButton.GetComponent<KButton>();
            rowView.Sell = sellButton.GetComponent<KButton>();
            rowView.SellTip = sellButton.AddComponent<ToolTip>();
            view = rowView;
            return row;
        }

        private static void SetTradingRightColumn(RectTransform rect, float right, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(-right, 0);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTradingLineHeight(TextMeshProUGUI text, float height)
        {
            text.maxVisibleLines = 1;
            LayoutElement layout = text.gameObject.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = height;
            layout.flexibleHeight = 0;
        }

        private void UpdateTradingGoodRow(TradingGoodRowView view, StorageNetworkTradingState state, TradingSupplier supplier, double cycle, int worldId)
        {
            TradingGood good = view.Good;
            view.SupplierId = supplier.Id;
            view.Background.color = tradingQuoteGood?.Id == good.Id && tradingQuoteSupplierId == supplier.Id
                ? StorageNetworkPanelPalette.CardBackground : StorageNetworkPanelPalette.RowBackground;
            string unit = good.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7C0025557708") : "kg";
            supplier.Stock.TryGetValue(good.Id, out double stock);
            float available = state.DisplayAvailable(good, worldId);
            string own = ColoredTradingText($"{available:#,0.###} {unit}", StorageNetworkPanelPalette.PositiveStatus);
            string vendor = ColoredTradingText($"{stock:#,0.###} {unit}", StorageNetworkPanelPalette.MetricAccent);
            SetTextIfChanged(view.Inventory, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_87AF53A707B2", TradingText.Argument((object)(own), null, 0), TradingText.Argument((object)(vendor), null, 0)));
            SetTextIfChanged(view.Prices, "<line-height=30>" + TradingPriceLine(supplier, good, cycle, true, unit) + "\n" + TradingPriceLine(supplier, good, cycle, false, unit) + "</line-height>");
            view.SellTip.toolTip = TradingText.Render(TradingMarket.Prefers(supplier, good) ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_8854589C732E") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_27704A1FBD0B"));
            view.Buy.isInteractable = supplier.Departure > cycle && stock > 0;
            view.Sell.isInteractable = supplier.Departure > cycle && available > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static string ColoredTradingText(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }

        private static string TradingPriceLine(TradingSupplier supplier, TradingGood good, double cycle, bool buy, string unit)
        {
            decimal price = TradingMarket.Price(supplier, good, cycle, buy);
            decimal previous = price;
            if (supplier.History.TryGetValue(good.Id, out var history))
            {
                var point = history.LastOrDefault(p => p.Cycle == Math.Floor(cycle) - 1);
                if (point != null) previous = buy ? point.Buy : point.Sell;
            }
            decimal change = (price - previous) / previous * 100m;
            Color color = change > 0 ? StorageNetworkPanelPalette.DangerStatus : change < 0 ? StorageNetworkPanelPalette.PositiveStatus : StorageNetworkPanelPalette.MutedText;
            string trend = ColoredTradingText($"{(change > 0 ? "↑" : change < 0 ? "↓" : "—")}{Math.Abs(change):0.0}%", color);
            string quote = ColoredTradingText($"{(buy ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2CD2507D65DE") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D998FC91A172"))} ¥{price:0.00}/{unit}", buy ? StorageNetworkPanelPalette.MetricAccent : StorageNetworkPanelPalette.PositiveStatus);
            return quote + "<pos=100>" + trend;
        }

        private sealed class TradingGoodRowView
        {
            public TradingGood Good;
            public Image Background;
            public int SupplierId;
            public TextMeshProUGUI Inventory;
            public TextMeshProUGUI Prices;
            public KButton Buy;
            public KButton Sell;
            public ToolTip SellTip;
        }

        private GameObject CreateTradingReceiptRow(TradingReceipt receipt)
        {
            GameObject row = CreateBox("Receipt" + receipt.Id, tradingHistoryContent, StorageNetworkPanelPalette.CardBackground);
            row.AddComponent<LayoutElement>().minHeight = 166;
            row.GetComponent<LayoutElement>().preferredHeight = 166;
            row.AddComponent<RectMask2D>();
            Color amountColor = receipt.Buy ? StorageNetworkPanelPalette.DangerStatus : StorageNetworkPanelPalette.PositiveStatus;
            GameObject accent = CreatePlainImage("DirectionAccent", row.transform, amountColor);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = Vector2.zero;
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.offsetMin = new Vector2(0, 6);
            accentRect.offsetMax = new Vector2(3, -6);
            accent.GetComponent<Image>().raycastTarget = false;
            Image icon = new GameObject("GoodIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(row.transform, false);
            var sprite = Def.GetUISprite(receipt.GoodId.ToTag(), "ui", false);
            icon.sprite = sprite?.first ?? Assets.GetSprite("unknown");
            icon.color = sprite != null ? sprite.second : Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform().anchorMin = icon.rectTransform().anchorMax = new Vector2(0, 1);
            icon.rectTransform().pivot = new Vector2(0, 1);
            icon.rectTransform().anchoredPosition = new Vector2(10, -8);
            icon.rectTransform().sizeDelta = new Vector2(36, 36);
            string localizedGoodName = StorageNetworkTradingState.Instance?.Catalog?.FirstOrDefault(g => g.Id == receipt.GoodId)?.Name ?? receipt.GoodName;
            TextMeshProUGUI receiptName = ReceiptLine(row.transform, "Name", ColoredTradingText(receipt.Buy ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A1557EC3B077") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FB28BA14091D"), amountColor) + "  <b>" + localizedGoodName + "</b>", 54, 8, 22, 13, StorageNetworkPanelPalette.HeadingText);
            receiptName.rectTransform().offsetMax = new Vector2(-66, receiptName.rectTransform().offsetMax.y);
            GameObject locateReceipt = CreateGameButton("LocateReceipt", row.transform, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_49726F09C812"), () => LocateTradingReceipt(receipt));
            SetTopStretch(locateReceipt.GetComponent<RectTransform>(), 0, 8, 8, 22);
            RectTransform locateReceiptRect = locateReceipt.GetComponent<RectTransform>();
            locateReceiptRect.anchorMin = locateReceiptRect.anchorMax = locateReceiptRect.pivot = new Vector2(1, 1);
            locateReceiptRect.sizeDelta = new Vector2(50, 22);
            locateReceiptRect.anchoredPosition = new Vector2(-8, -8);
            locateReceipt.GetComponent<KButton>().isInteractable = receipt.StorageIds != null && receipt.StorageIds.Count > 0;
            ReceiptLine(row.transform, "Quantity", TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BECA349C1FC7", TradingText.Argument((object)(receipt.Amount), "#,0.###", 0), TradingText.Argument((object)((receipt.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7C0025557708") : "kg")), null, 0), TradingText.Argument((object)(receipt.UnitPrice), "0.00", 0)), 54, 32, 18, 10, StorageNetworkPanelPalette.MetricAccent);
            TextMeshProUGUI delivery = ReceiptLine(row.transform, "Delivery", receipt.Delivery ?? "", 10, 138, 22, 10, StorageNetworkPanelPalette.MutedText);
            delivery.raycastTarget = true;
            delivery.gameObject.AddComponent<ToolTip>().toolTip = TradingText.Render(receipt.Delivery ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FACCE382ABF9"));
            ReceiptLine(row.transform, "Total", $"<b>{(receipt.Buy ? "−" : "+")}¥{receipt.Total:#,0.#####}</b>", 10, 54, 26, 16, amountColor);
            ReceiptLine(row.transform, "Balance", TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_82A524B0D821", TradingText.Argument((object)(receipt.BalanceAfter), "#,0.#####", 0)), 10, 82, 18, 10, StorageNetworkPanelPalette.NeutralStatus);
            ReceiptLine(row.transform, "Context", TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_EADA23F062D8", TradingText.Argument((object)(receipt.Id), "0000", 0), TradingText.Argument((object)(receipt.Cycle + 1), "0.00", 0), TradingText.Argument((object)(receipt.SupplierName ?? StorageNetworkTradingState.SpecialtyName(receipt.Specialty)), null, 0), TradingText.Argument((object)(receipt.SupplierId), null, 0), TradingText.Argument((object)(StorageNetworkWorldDisplay.GetWorldName(receipt.WorldId)), null, 0)), 10, 102, 34, 10, StorageNetworkPanelPalette.MutedText);
            return row;
        }

        private static TextMeshProUGUI ReceiptLine(Transform parent, string name, string value, float left, float top, float height, int fontSize, Color color)
        {
            TextMeshProUGUI text = CreateText(name, parent, value, fontSize, TextAlignmentOptions.MidlineLeft);
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.maxVisibleLines = name == "Context" ? 2 : 1;
            SetTopStretch(text.rectTransform(), left, 10, top, height);
            return text;
        }

        private void SelectTradingGood(int supplierId, TradingGood good, bool buy)
        {
            tradingQuoteGood = good;
            tradingQuoteSupplierId = supplierId;
            tradingQuoteSupplierName = StorageNetworkTradingState.Instance?.Market?.Suppliers.Find(s => s.Id == supplierId)?.Name;
            tradingQuoteBuy = buy;
            RefreshTradingWindow();
            ShowTradingAmountDialog();
        }

        private void RefreshTradingQuote()
        {
            if (tradingAmountDialogRoot == null || modalRoot != tradingAmountDialogRoot) return;
            tradingQuote = null;
            tradingConfirmButton.isInteractable = tradingMaximumButton.isInteractable = false;
            if (tradingQuoteGood == null)
            {
                SetTextIfChanged(tradingQuoteText, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_31EEBA955D49"));
                return;
            }
            var state = StorageNetworkTradingState.Instance;
            if (state == null) return;
            if (!double.TryParse(tradingAmountInput.text, NumberStyles.Float, CultureInfo.CurrentCulture, out double requested))
                double.TryParse(tradingAmountInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out requested);
            tradingQuoteWorldId = GetActiveWorldFilterId();
            tradingQuote = state.GetQuote(tradingQuoteSupplierId, tradingQuoteGood, requested, tradingQuoteBuy, tradingQuoteWorldId);
            string unit = tradingQuoteGood.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7C0025557708") : "kg";
            SetTextIfChanged(tradingQuantityLabel, TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A5A84D846268", TradingText.Argument((object)(tradingQuote.MaximumAmount), "0.###", 0), TradingText.Argument((object)(unit), null, 0), TradingText.Argument((object)(tradingQuote.Amount), "0.###", 0), TradingText.Argument((object)(unit), null, 0), TradingText.Argument((object)(tradingQuote.MinimumAmount), "0.###", 0), TradingText.Argument((object)(unit), null, 0)));
            SetTextIfChanged(tradingQuoteText,
                TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_30E12A23B389", TradingText.Argument((object)((tradingQuoteBuy ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A1557EC3B077") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_FB28BA14091D"))), null, 0), TradingText.Argument((object)(tradingQuoteGood.Name), null, 0), TradingText.Argument((object)(tradingQuoteSupplierName ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_13E43251E905")), null, 0), TradingText.Argument((object)(tradingQuoteSupplierId), null, 0), TradingText.Argument((object)(tradingQuote.Amount), "0.###", 0), TradingText.Argument((object)(unit), null, 0), TradingText.Argument((object)(tradingQuote.UnitPrice), "0.00", 0), TradingText.Argument((object)(tradingQuote.Total), "0.#####", 0), TradingText.Argument((object)(state.Market.Balance), "0.#####", 0), TradingText.Argument((object)((tradingQuote.CanTrade ? tradingQuote.BalanceAfter : state.Market.Balance)), "0.#####", 0), TradingText.Argument((object)(tradingDialogFeedback ?? tradingQuote.Reason ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_D2A16107A98F")), null, 0)));
            tradingAmountUpdating = true;
            float maximum = (float)tradingQuote.MaximumAmount;
            if (maximum > tradingQuote.MaximumAmount) maximum = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(maximum) - 1);
            tradingAmountSlider.maxValue = Math.Max(0.001f, maximum);
            tradingAmountSlider.interactable = tradingQuote.MaximumAmount > 0;
            tradingAmountSlider.SetValueWithoutNotify((float)tradingQuote.Amount);
            tradingAmountInput.GetComponent<StorageNetworkNumberInputField>().Configure(tradingAmountInput, 0, maximum, tradingQuoteGood.ByUnit);
            tradingAmountUpdating = false;
            tradingConfirmButton.isInteractable = tradingQuote.CanTrade;
            tradingMaximumButton.isInteractable = tradingQuote.MaximumAmount > 0;
            RefreshTradingDeliveryPreview(state);
        }

        private void SetMaximumTradingAmount()
        {
            if (tradingQuote == null || tradingQuote.MaximumAmount <= 0) return;
            tradingDialogFeedback = null;
            SetTradingDialogAmount(tradingQuote.MaximumAmount);
        }

        private void ConfirmTradingQuote()
        {
            TradingQuote quote = tradingQuote;
            var state = StorageNetworkTradingState.Instance;
            if (tradingAmountDialogRoot == null || modalRoot != tradingAmountDialogRoot || quote == null || !quote.CanTrade || state == null) return;
            int before = state.Market.NextTradeId;
            if (tradingQuoteWorldId != GetActiveWorldFilterId()) tradingMessage = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_19DCD9ED2D00");
            else tradingMessage = state.Trade(quote.SupplierId, quote.Good, quote.Amount, quote.Buy, tradingQuoteWorldId, quote.UnitPrice, true, quote.Cycle);
            if (state.Market.NextTradeId != before) CloseModal();
            else tradingDialogFeedback = tradingMessage;
            RefreshTradingWindow();
        }
    }
}
