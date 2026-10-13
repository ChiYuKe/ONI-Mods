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
        private bool tradingQuoteBuy;
        private int tradingQuoteWorldId;
        private TradingQuote tradingQuote;
        private readonly TextMeshProUGUI[] supplierLabels = new TextMeshProUGUI[5];
        private KInputTextField tradingAmountInput;
        private KInputTextField tradingSearchInput;
        private int tradingSlot;
        private int tradingPage;
        private float tradingRefreshElapsed;
        private int tradingLayoutFrame = -1;
        private bool resetTradingScrollAfterLayout;
        private string tradingRowsSignature;
        private string tradingFilter = string.Empty;
        private string tradingMessage = "选择供应商和商品，检查成交数量与总额后确认交易。货币、货物及成交记录随存档保存。";

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
            tradingRoot.AddComponent<RectMask2D>();
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
            tradingSummary = CreateText("Title", header.transform, "网络交易", 16, TextAlignmentOptions.MidlineLeft);
            tradingSummary.fontStyle = FontStyles.Bold;
            tradingSummary.textWrappingMode = TextWrappingModes.NoWrap;
            tradingSummary.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(tradingSummary.rectTransform(), 12, 0);
            tradingSummary.rectTransform().offsetMax = new Vector2(-44, 0);
            GameObject close = CreateCloseIconButton("Close", header.transform, CloseTradingWindow);
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.sizeDelta = new Vector2(28, 26);
            closeRect.anchoredPosition = new Vector2(-8, 0);

            GameObject suppliers = CreateBox("Suppliers", contentPlate.transform, TradingBackground);
            SetTopStretch(suppliers.GetComponent<RectTransform>(), 8, 8, 8, 76);
            HorizontalLayoutGroup supplierLayout = suppliers.AddComponent<HorizontalLayoutGroup>();
            supplierLayout.padding = new RectOffset(6, 6, 6, 6);
            supplierLayout.spacing = 6;
            supplierLayout.childControlWidth = supplierLayout.childControlHeight = true;
            supplierLayout.childForceExpandWidth = supplierLayout.childForceExpandHeight = true;
            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                GameObject button = CreateGameButton("Supplier" + i, suppliers.transform, "供货商", () =>
                {
                    tradingSlot = slot;
                    tradingPage = 0;
                    tradingQuoteGood = null;
                    tradingRowsSignature = null;
                    RefreshTradingWindow();
                });
                supplierLabels[i] = button.GetComponentInChildren<TextMeshProUGUI>();
                supplierLabels[i].textWrappingMode = TextWrappingModes.Normal;
                supplierLabels[i].maxVisibleLines = 3;
            }

            GameObject controls = CreateBox("Controls", contentPlate.transform, StorageNetworkPanelPalette.CardBackground);
            SetTopStretch(controls.GetComponent<RectTransform>(), 8, 8, 90, 38);
            HorizontalLayoutGroup controlsLayout = controls.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.padding = new RectOffset(8, 8, 5, 5);
            controlsLayout.spacing = 8;
            controlsLayout.childControlWidth = controlsLayout.childControlHeight = true;
            controlsLayout.childForceExpandWidth = false;
            controlsLayout.childForceExpandHeight = true;
            TradingLabel(controls.transform, "搜索商品", 80);
            tradingSearchInput = CreateFixedTextInput(controls.transform, "TradingSearch", "", 240, 28, 12);
            tradingSearchInput.onValueChanged.AddListener(value => { tradingFilter = value ?? ""; tradingPage = 0; });
            GameObject previous = CreateGameButton("Previous", controls.transform, "上一页", () => { tradingPage = Math.Max(0, tradingPage - 1); RefreshTradingWindow(); });
            previous.AddComponent<LayoutElement>().preferredWidth = 64;
            GameObject next = CreateGameButton("Next", controls.transform, "下一页", () => { tradingPage++; RefreshTradingWindow(); });
            next.AddComponent<LayoutElement>().preferredWidth = 64;
            tradingPageLabel = TradingLabel(controls.transform, "", 90);

            GameObject workspace = new GameObject("TradingWorkspace", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            workspace.transform.SetParent(contentPlate.transform, false);
            SetStretch(workspace.GetComponent<RectTransform>(), 8, 8, 70, 132);
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
            TextMeshProUGUI listTitle = CreateText("Title", listHeader.transform, "货物交易 · 买卖报价 / 涨跌较上周期", 12, TextAlignmentOptions.MidlineLeft);
            Stretch(listTitle.rectTransform(), 8, 0);
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

            tradingRoot.SetActive(false);
        }

        private void ShowTradingAmountDialog()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            tradingDialogFeedback = null;
            modalRoot = CreateAmountModalFrame(tradingQuoteBuy ? "买入数量" : "卖出数量", 560, 400, out GameObject body);
            tradingAmountDialogRoot = modalRoot;
            GameObject card = CreatePlainImage("TradeQuote", body.transform, StorageNetworkPanelPalette.RowBackground);
            card.AddComponent<LayoutElement>().preferredHeight = 104;
            card.AddComponent<RectMask2D>();
            tradingQuoteText = CreateText("TradeDetails", card.transform, "", 12, TextAlignmentOptions.MidlineLeft);
            tradingQuoteText.color = StorageNetworkPanelPalette.BodyText;
            tradingQuoteText.textWrappingMode = TextWrappingModes.NoWrap;
            tradingQuoteText.overflowMode = TextOverflowModes.Ellipsis;
            tradingQuoteText.maxVisibleLines = 4;
            Stretch(tradingQuoteText.rectTransform(), 10, 6);
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
            GameObject maximum = AddAmountModalButton(shortcuts.transform, "全部", 86, SetMaximumTradingAmount);
            tradingMaximumButton = maximum.GetComponent<KButton>();
            GameObject footer = AddAmountModalRow(body.transform, 8, 30);
            AddFooterSpacer(footer.transform);
            AddAmountModalButton(footer.transform, "取消", 92, CloseModal);
            GameObject confirm = AddAmountModalButton(footer.transform, "确认成交", 100, ConfirmTradingQuote);
            confirm.GetComponent<KImage>().colorStyleSetting = KleiPinkStyle();
            tradingConfirmButton = confirm.GetComponent<KButton>();
            var initial = StorageNetworkTradingState.Instance.GetQuote(tradingQuoteSupplierId, tradingQuoteGood, 1, tradingQuoteBuy, GetActiveWorldFilterId());
            SetTradingDialogAmount(Math.Min(initial.MaximumAmount, tradingQuoteGood.ByUnit ? 1 : 100));
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
            tradingHistoryTitle = CreateText("Title", header.transform, "最近成交", 12, TextAlignmentOptions.MidlineLeft);
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
            SetTextIfChanged(tradingHistoryTitle, $"最近成交 · {market.Receipts.Count} 笔");
            string income = ColorUtility.ToHtmlStringRGB(StorageNetworkPanelPalette.PositiveStatus);
            string expense = ColorUtility.ToHtmlStringRGB(StorageNetworkPanelPalette.DangerStatus);
            SetTextIfChanged(tradingHistoryTotals, $"<color=#{income}>收入 ¥{market.Receipts.Where(r => !r.Buy).Sum(r => r.Total):N0}</color>  <color=#{expense}>支出 ¥{market.Receipts.Where(r => r.Buy).Sum(r => r.Total):N0}</color>");
            tradingHistoryRows.Begin();
            foreach (TradingReceipt receipt in market.Receipts.AsEnumerable().Reverse())
                tradingHistoryRows.Use("receipt:" + receipt.Id, () => CreateTradingReceiptRow(receipt));
            if (market.Receipts.Count == 0)
                tradingHistoryRows.Use("empty", () =>
                {
                    TextMeshProUGUI empty = TradingLabel(tradingHistoryContent, "完成交易后，记录会出现在这里。", 0);
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
        }

        private void RefreshTradingWindow()
        {
            StorageNetworkTradingState state = StorageNetworkTradingState.Instance;
            if (state == null || state.Market == null) { tradingStatus.text = "交易市场尚未初始化。"; return; }
            state.Refresh();
            int worldId = GetActiveWorldFilterId();
            double cycle = StorageNetworkTradingState.Cycle;
            TradingSupplier selected = state.Market.Suppliers[tradingSlot];
            SetTextIfChanged(tradingSummary, $"网络交易    账户余额 ¥{state.Market.Balance:0.#####}    {StorageNetworkWorldDisplay.GetWorldName(worldId)}");
            for (int i = 0; i < 5; i++)
            {
                TradingSupplier supplier = state.Market.Suppliers[i];
                string profile = StorageNetworkTradingState.ProfileName(supplier).Split('·')[0].Trim();
                SetTextIfChanged(supplierLabels[i], $"{(i == tradingSlot ? "▶ " : "")}{StorageNetworkTradingState.SpecialtyName(supplier.Specialty)} #{supplier.Id}\n{profile} · 剩余 {supplier.Departure - cycle:0.0} 周期\n收购 ¥{supplier.BuyLimit - supplier.Bought:0} · 供货 ¥{supplier.SellLimit - supplier.Sold:0}");
            }
            SetTextIfChanged(tradingStatus, $"{tradingMessage}\n供货商剩余买入额度 {selected.BuyLimit - selected.Bought:0.###} / {selected.BuyLimit:0}；剩余卖出额度 {selected.SellLimit - selected.Sold:0.###} / {selected.SellLimit:0}");
            // Only list membership affects structure; changing amounts or prices updates existing views.
            List<TradingGood> catalog = state.Catalog.Where(g => g.Category == selected.Specialty &&
                (string.IsNullOrEmpty(tradingFilter) || g.Name.IndexOf(tradingFilter, StringComparison.CurrentCultureIgnoreCase) >= 0 || g.Id.IndexOf(tradingFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                ((selected.Stock.TryGetValue(g.Id, out double stock) && stock > 0) || StorageNetworkTradingState.Available(g, worldId) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)).ToList();
            string signature = $"{selected.Id}|{tradingPage}|{tradingFilter}|" + string.Join(",", catalog.Select(g => g.Id));
            if (signature != tradingRowsSignature)
                ReconcileTradingRows(state, selected, catalog, signature);
            foreach (TradingGood good in tradingVisibleGoods)
                if (tradingRows.TryGetMetadata(good.Id, out TradingGoodRowView view))
                    UpdateTradingGoodRow(view, state, selected, cycle, worldId);
            RefreshTradingHistory(state.Market);
            RefreshTradingQuote();
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
                        "暂无符合条件的现货或可售材料。可清空搜索或选择其他供应商。", 0);
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
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
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

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            textColumn.transform.SetParent(row.transform, false);
            LayoutElement columnLayout = textColumn.GetComponent<LayoutElement>();
            columnLayout.minWidth = 120;
            columnLayout.preferredWidth = 200;
            columnLayout.flexibleWidth = 1;
            VerticalLayoutGroup columnGroup = textColumn.GetComponent<VerticalLayoutGroup>();
            columnGroup.childAlignment = TextAnchor.MiddleLeft;
            columnGroup.childControlWidth = columnGroup.childControlHeight = true;
            columnGroup.childForceExpandWidth = true;
            columnGroup.childForceExpandHeight = false;
            TextMeshProUGUI name = TradingLabel(textColumn.transform, good.Name, 0);
            name.fontSize = 13;
            name.fontStyle = FontStyles.Bold;
            name.color = StorageNetworkPanelPalette.HeadingText;
            SetTradingLineHeight(name, 22);
            TextMeshProUGUI inventory = TradingLabel(textColumn.transform, "", 0);
            inventory.color = StorageNetworkPanelPalette.MutedText;
            SetTradingLineHeight(inventory, 34);
            inventory.maxVisibleLines = 2;
            inventory.fontSize = 11;
            TextMeshProUGUI prices = TradingLabel(row.transform, "", 150);
            prices.fontSize = 11;
            prices.maxVisibleLines = 2;
            prices.gameObject.GetComponent<LayoutElement>().minHeight = 42;
            prices.gameObject.GetComponent<LayoutElement>().preferredHeight = 42;
            var rowView = new TradingGoodRowView { Good = good, Background = row.GetComponent<Image>(), Inventory = inventory, Prices = prices };
            GameObject actions = new GameObject("TradeActions", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            actions.transform.SetParent(row.transform, false);
            actions.GetComponent<LayoutElement>().minWidth = actions.GetComponent<LayoutElement>().preferredWidth = 64;
            VerticalLayoutGroup actionsLayout = actions.GetComponent<VerticalLayoutGroup>();
            actionsLayout.spacing = 4;
            actionsLayout.childAlignment = TextAnchor.MiddleCenter;
            actionsLayout.childControlWidth = actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = false;
            GameObject buyButton = CreateGameButton("Buy", actions.transform, "买入", () => SelectTradingGood(rowView.SupplierId, rowView.Good, true));
            GameObject sellButton = CreateGameButton("Sell", actions.transform, "卖出", () => SelectTradingGood(rowView.SupplierId, rowView.Good, false));
            foreach (GameObject button in new[] { buyButton, sellButton })
            {
                LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
                buttonLayout.minWidth = buttonLayout.preferredWidth = 64;
                buttonLayout.minHeight = buttonLayout.preferredHeight = 26;
            }
            rowView.Buy = buyButton.GetComponent<KButton>();
            rowView.Sell = sellButton.GetComponent<KButton>();
            view = rowView;
            return row;
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
            string unit = good.ByUnit ? "件" : "kg";
            supplier.Stock.TryGetValue(good.Id, out double stock);
            float available = StorageNetworkTradingState.Available(good, worldId);
            string own = ColoredTradingText($"{available:#,0.###} {unit}", StorageNetworkPanelPalette.PositiveStatus);
            string vendor = ColoredTradingText($"{stock:#,0.###} {unit}", StorageNetworkPanelPalette.MetricAccent);
            SetTextIfChanged(view.Inventory, $"我的网络 {own}\n供货商现货 {vendor}");
            SetTextIfChanged(view.Prices, TradingPriceLine(supplier, good, cycle, true, unit) + "\n" + TradingPriceLine(supplier, good, cycle, false, unit));
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
            decimal previous = TradingMarket.Price(supplier, good, Math.Max(0, Math.Floor(cycle) - 1), buy);
            decimal change = (price - previous) / previous * 100m;
            Color color = change > 0 ? StorageNetworkPanelPalette.DangerStatus : change < 0 ? StorageNetworkPanelPalette.PositiveStatus : StorageNetworkPanelPalette.MutedText;
            string trend = ColoredTradingText($"{(change > 0 ? "↑" : change < 0 ? "↓" : "—")}{Math.Abs(change):0.0}%", color);
            string quote = ColoredTradingText($"{(buy ? "买" : "卖")} ¥{price:0.00}/{unit}", buy ? StorageNetworkPanelPalette.MetricAccent : StorageNetworkPanelPalette.PositiveStatus);
            return quote + "  " + trend;
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
        }

        private GameObject CreateTradingReceiptRow(TradingReceipt receipt)
        {
            GameObject row = CreateBox("Receipt" + receipt.Id, tradingHistoryContent, StorageNetworkPanelPalette.CardBackground);
            row.AddComponent<LayoutElement>().minHeight = 132;
            row.GetComponent<LayoutElement>().preferredHeight = 132;
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
            ReceiptLine(row.transform, "Name", ColoredTradingText(receipt.Buy ? "买入" : "卖出", amountColor) + "  <b>" + receipt.GoodName + "</b>", 54, 8, 20, 13, StorageNetworkPanelPalette.HeadingText);
            ReceiptLine(row.transform, "Quantity", $"{receipt.Amount:#,0.###} {(receipt.ByUnit ? "件" : "kg")} · 单价 ¥{receipt.UnitPrice:0.00}", 54, 30, 18, 10, StorageNetworkPanelPalette.MetricAccent);
            ReceiptLine(row.transform, "Total", $"<b>{(receipt.Buy ? "−" : "+")}¥{receipt.Total:#,0.#####}</b>", 10, 52, 24, 16, amountColor);
            ReceiptLine(row.transform, "Balance", $"成交后余额 ¥{receipt.BalanceAfter:#,0.#####}", 10, 78, 16, 10, StorageNetworkPanelPalette.NeutralStatus);
            ReceiptLine(row.transform, "Context", $"#{receipt.Id:0000} · 第 {receipt.Cycle + 1:0.00} 周期\n{StorageNetworkTradingState.SpecialtyName(receipt.Specialty)} #{receipt.SupplierId} · {StorageNetworkWorldDisplay.GetWorldName(receipt.WorldId)}", 10, 98, 28, 10, StorageNetworkPanelPalette.MutedText);
            return row;
        }

        private static void ReceiptLine(Transform parent, string name, string value, float left, float top, float height, int fontSize, Color color)
        {
            TextMeshProUGUI text = CreateText(name, parent, value, fontSize, TextAlignmentOptions.MidlineLeft);
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.maxVisibleLines = name == "Context" ? 2 : 1;
            SetTopStretch(text.rectTransform(), left, 10, top, height);
        }

        private void SelectTradingGood(int supplierId, TradingGood good, bool buy)
        {
            tradingQuoteGood = good;
            tradingQuoteSupplierId = supplierId;
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
                SetTextIfChanged(tradingQuoteText, "选择货物的买入或卖出按钮，查看数量和总额后确认成交。");
                return;
            }
            var state = StorageNetworkTradingState.Instance;
            if (state == null) return;
            if (!double.TryParse(tradingAmountInput.text, NumberStyles.Float, CultureInfo.CurrentCulture, out double requested))
                double.TryParse(tradingAmountInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out requested);
            tradingQuoteWorldId = GetActiveWorldFilterId();
            tradingQuote = state.GetQuote(tradingQuoteSupplierId, tradingQuoteGood, requested, tradingQuoteBuy, tradingQuoteWorldId);
            string unit = tradingQuoteGood.ByUnit ? "件" : "kg";
            SetTextIfChanged(tradingQuantityLabel, $"最大可成交 {tradingQuote.MaximumAmount:0.###} {unit}    当前选择 {tradingQuote.Amount:0.###} {unit}");
            SetTextIfChanged(tradingQuoteText,
                $"<b>{(tradingQuoteBuy ? "买入" : "卖出")} {tradingQuoteGood.Name}</b>    供货商 #{tradingQuoteSupplierId}\n数量 {tradingQuote.Amount:0.###} {unit} × ¥{tradingQuote.UnitPrice:0.00}    <b>总额 ¥{tradingQuote.Total:0.#####}</b>\n账户余额 ¥{state.Market.Balance:0.#####} → ¥{(tradingQuote.CanTrade ? tradingQuote.BalanceAfter : state.Market.Balance):0.#####}\n{tradingDialogFeedback ?? tradingQuote.Reason ?? "拖动滑块或输入数量，检查总额后确认成交。"}");
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
            if (tradingQuoteWorldId != GetActiveWorldFilterId()) tradingMessage = "当前星球已变化，请重新检查交易报价。";
            else tradingMessage = state.Trade(quote.SupplierId, quote.Good, quote.Amount, quote.Buy, tradingQuoteWorldId, quote.UnitPrice, true);
            if (state.Market.NextTradeId != before) CloseModal();
            else tradingDialogFeedback = tradingMessage;
            RefreshTradingWindow();
        }
    }
}
