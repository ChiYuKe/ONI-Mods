using System;
using System.Collections.Generic;
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
        private GameObject tradingChartRoot;
        private TradingSupplier tradingChartSupplier;
        private TradingGood tradingChartGood;
        private StorageNetworkPriceChart tradingChart;
        private TextMeshProUGUI tradingChartSummary, tradingChartReadout, tradingChartNote;
        private readonly TextMeshProUGUI[] tradingChartPrices = new TextMeshProUGUI[5];
        private readonly TextMeshProUGUI[] tradingChartCycles = new TextMeshProUGUI[5];
        private List<TradingPricePoint> tradingChartPoints;
        private int tradingChartCycle = -1;
        private int tradingChartVersion = -1;
        private int tradingChartMode;
        private TextMeshProUGUI tradingChartRange;
        private readonly KImage[] tradingChartModeImages = new KImage[3];

        private void ShowTradingPriceChart(int supplierId, TradingGood good)
        {
            TradingSupplier supplier = StorageNetworkTradingState.Instance?.Market?.Suppliers.Find(s => s.Id == supplierId);
            if (supplier == null) return;
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            modalRoot = CreateAmountModalFrame(good.Name + TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_3C7094460B0C"), 800, 600, out GameObject body);
            tradingChartRoot = modalRoot;
            tradingChartSupplier = supplier;
            tradingChartGood = good;
            tradingChartCycle = -1;
            tradingChartVersion = -1;
            tradingChartMode = 0;
            GameObject modes = AddAmountModalRow(body.transform, 8, 28);
            string[] modeNames = { TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_23009C76ED42"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_743F8627FEA2"), TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_144D3C14112D") };
            for (int i = 0; i < modeNames.Length; i++)
            {
                int mode = i;
                GameObject button = AddAmountModalButton(modes.transform, modeNames[i], 120, () =>
                {
                    tradingChartMode = mode;
                    tradingChartVersion = -1;
                    RefreshTradingPriceChart();
                });
                tradingChartModeImages[i] = button.GetComponent<KImage>();
            }
            tradingChartSummary = CreateAmountModalText(body.transform, "", 13, FontStyles.Bold);
            tradingChartSummary.color = StorageNetworkPanelPalette.BodyText;
            tradingChartSummary.GetComponent<LayoutElement>().preferredHeight = 30;
            tradingChartReadout = CreateAmountModalText(body.transform, "", 12, FontStyles.Normal);
            tradingChartReadout.color = StorageNetworkPanelPalette.BodyText;
            tradingChartReadout.GetComponent<LayoutElement>().preferredHeight = 24;
            tradingChartRange = CreateAmountModalText(body.transform, "", 11, FontStyles.Normal);
            tradingChartRange.color = StorageNetworkPanelPalette.MutedText;
            tradingChartRange.GetComponent<LayoutElement>().preferredHeight = 24;

            GameObject frame = CreatePlainImage("PriceChartFrame", body.transform, StorageNetworkPanelPalette.RowBackground);
            frame.AddComponent<LayoutElement>().preferredHeight = 280;
            // Space for axis labels around the plot; labels never overlap the lines.
            GameObject plot = new GameObject("PriceLines", typeof(RectTransform), typeof(CanvasRenderer), typeof(StorageNetworkPriceChart));
            plot.transform.SetParent(frame.transform, false);
            SetStretch(plot.GetComponent<RectTransform>(), 72, 22, 38, 20);
            tradingChart = plot.GetComponent<StorageNetworkPriceChart>();
            tradingChart.BuyColor = StorageNetworkPanelPalette.DangerStatus;
            tradingChart.SellColor = StorageNetworkPanelPalette.PositiveStatus;
            tradingChart.OnSelection = UpdateTradingChartReadout;
            tradingChart.raycastTarget = true;
            for (int i = 0; i < 5; i++)
            {
                TextMeshProUGUI price = CreateText("PriceTick" + i, frame.transform, "", 11, TextAlignmentOptions.MidlineRight);
                price.color = StorageNetworkPanelPalette.MutedText;
                price.raycastTarget = false;
                RectTransform priceRect = price.rectTransform();
                float fraction = i / 4f;
                priceRect.anchorMin = priceRect.anchorMax = new Vector2(0, fraction);
                priceRect.pivot = new Vector2(0, 0.5f);
                priceRect.sizeDelta = new Vector2(62, 18);
                priceRect.anchoredPosition = new Vector2(4, 38 - 58 * fraction);
                tradingChartPrices[i] = price;
                TextMeshProUGUI cycle = CreateText("CycleTick" + i, frame.transform, "", 11, TextAlignmentOptions.Center);
                cycle.color = StorageNetworkPanelPalette.MutedText;
                cycle.raycastTarget = false;
                RectTransform cycleRect = cycle.rectTransform();
                cycleRect.anchorMin = cycleRect.anchorMax = new Vector2(fraction, 0);
                cycleRect.pivot = new Vector2(0.5f, 0);
                cycleRect.sizeDelta = new Vector2(80, 24);
                cycleRect.anchoredPosition = new Vector2(72 - 94 * fraction, 8);
                tradingChartCycles[i] = cycle;
            }
            tradingChartNote = CreateAmountModalText(body.transform, "", 11, FontStyles.Normal);
            tradingChartNote.color = StorageNetworkPanelPalette.MutedText;
            tradingChartNote.GetComponent<LayoutElement>().preferredHeight = 42;
            RefreshTradingPriceChart();
        }

        private void RefreshTradingPriceChart()
        {
            if (tradingChartRoot == null || modalRoot != tradingChartRoot) return;
            double now = StorageNetworkTradingState.Cycle;
            TradingMarket market = StorageNetworkTradingState.Instance?.Market;
            if (market == null) return;
            int cycle = (int)Math.Floor(now);
            if (cycle == tradingChartCycle && tradingChartVersion == market.QuoteVersion) return;
            tradingChartCycle = cycle;
            tradingChartVersion = market.QuoteVersion;
            tradingChartPoints = tradingChartMode == 0 ? TradingMarket.PriceHistory(tradingChartSupplier, tradingChartGood, now)
                : market.ReferenceHistory(tradingChartGood.Id, tradingChartMode == 1 ? 30 : 60, now);
            for (int i = 0; i < 3; i++)
            {
                KImage image = tradingChartModeImages[i];
                image.colorStyleSetting = i == tradingChartMode ? KleiPinkStyle() : KleiBlueStyle();
                // Replacing the setting does not refresh the image's existing color.
                // Apply it using the current selector to preserve hover/press feedback.
                image.ApplyColorStyleSetting();
            }
            if (tradingChartPoints.Count == 0) return;
            decimal low = tradingChartPoints.Min(p => p.Sell);
            decimal high = tradingChartPoints.Max(p => p.Buy);
            decimal margin = Math.Max(0.01m, (high - low) * 0.15m);
            decimal minimum = Math.Max(0m, low - margin);
            decimal maximum = high + margin;
            tradingChart.SingleSeries = tradingChartMode > 0;
            tradingChart.BuyColor = tradingChartMode > 0 ? StorageNetworkPanelPalette.MetricAccent : StorageNetworkPanelPalette.DangerStatus;
            tradingChart.Configure(tradingChartPoints, minimum, maximum);
            string unit = tradingChartGood.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7C0025557708") : "kg";
            TradingPricePoint latest = tradingChartPoints[tradingChartPoints.Count - 1];
            tradingChartSummary.text = TradingText.Render(tradingChartMode > 0
                ? ColoredTradingText(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_6534D3B0220D", TradingText.Argument((object)(latest.Buy), "0.00", 0), TradingText.Argument((object)(unit), null, 0)), StorageNetworkPanelPalette.MetricAccent)
                : ColoredTradingText(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_CF4B950F4F9B", TradingText.Argument((object)(latest.Buy), "0.00", 0), TradingText.Argument((object)(unit), null, 0)), StorageNetworkPanelPalette.DangerStatus) +
                "     " + ColoredTradingText(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F5FBBB974756", TradingText.Argument((object)(latest.Sell), "0.00", 0), TradingText.Argument((object)(unit), null, 0)), StorageNetworkPanelPalette.PositiveStatus) +
                $"     {tradingChartSupplier.Name ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_13E43251E905")} #{tradingChartSupplier.Id}");
            tradingChartRange.text = TradingText.Render(tradingChartMode > 0
                ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_34D9B9B9DBC6", TradingText.Argument((object)(low), "0.00", 0), TradingText.Argument((object)(high), "0.00", 0), TradingText.Argument((object)(tradingChartPoints.Count), null, 0), TradingText.Argument((object)(TradingMarket.EventName(tradingChartGood.Category, now)), null, 0))
                : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8ADD72E1E6A", TradingText.Argument((object)(tradingChartPoints.Min(p => p.Buy)), "0.00", 0), TradingText.Argument((object)(high), "0.00", 0), TradingText.Argument((object)(low), "0.00", 0), TradingText.Argument((object)(tradingChartPoints.Max(p => p.Sell)), "0.00", 0), TradingText.Argument((object)(TradingMarket.PreferenceName(tradingChartSupplier)), null, 0)));
            for (int i = 0; i < 5; i++)
            {
                tradingChartPrices[i].text = TradingText.Render("¥" + (minimum + (maximum - minimum) * i / 4).ToString(maximum - minimum < 0.1m ? "0.000" : "0.00"));
                int index = Mathf.RoundToInt((tradingChartPoints.Count - 1) * i / 4f);
                bool unique = i == 0 || index != Mathf.RoundToInt((tradingChartPoints.Count - 1) * (i - 1) / 4f);
                float fraction = tradingChartPoints.Count == 1 ? 0.5f : (tradingChartPoints[index].Cycle - tradingChartPoints[0].Cycle) /
                    (float)(latest.Cycle - tradingChartPoints[0].Cycle);
                RectTransform tickRect = tradingChartCycles[i].rectTransform();
                tickRect.anchorMin = tickRect.anchorMax = new Vector2(fraction, 0);
                tickRect.anchoredPosition = new Vector2(72 - 94 * fraction, 8);
                tradingChartCycles[i].text = TradingText.Render(tradingChartPoints.Count == 1
                    ? (i == 2 ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_8D44187A755D", TradingText.Argument((object)(latest.Cycle + 1), null, 0)) : "")
                    : (unique ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_8D44187A755D", TradingText.Argument((object)(tradingChartPoints[index].Cycle + 1), null, 0)) : ""));
            }
            tradingChartNote.text = TradingText.Render(tradingChartPoints.Count == 1
                ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_DC250D800099")
                : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.QUOTE_CURVES_NOTE"));
            if (now >= tradingChartSupplier.Departure) tradingChartNote.text = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_27CED08A3A4D"));
            if (tradingChartMode > 0) tradingChartNote.text = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2E1EE39B58EB"));
            UpdateTradingChartReadout(tradingChartPoints.Count - 1);
        }

        private void UpdateTradingChartReadout(int index)
        {
            if (tradingChartRoot == null || modalRoot != tradingChartRoot || tradingChartPoints == null || index < 0 || index >= tradingChartPoints.Count) return;
            TradingPricePoint point = tradingChartPoints[index];
            string unit = tradingChartGood.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7C0025557708") : "kg";
            string change = "";
            if (index > 0)
            {
                decimal percent = (point.Buy - tradingChartPoints[index - 1].Buy) / tradingChartPoints[index - 1].Buy * 100m;
                change = (point.Cycle - tradingChartPoints[index - 1].Cycle == 1 ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_29EEAC9D4748") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_65F262CA52A0")) + ColoredTradingText($"{(percent > 0 ? "↑" : percent < 0 ? "↓" : "—")}{Math.Abs(percent):0.0}%",
                    percent > 0 ? StorageNetworkPanelPalette.DangerStatus : percent < 0 ? StorageNetworkPanelPalette.PositiveStatus : StorageNetworkPanelPalette.MutedText);
            }
            tradingChartReadout.text = TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_F878D6B67A3E", TradingText.Argument((object)(point.Cycle + 1), null, 0)) + (tradingChartMode > 0 ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_E04D1BD27B2D", TradingText.Argument((object)(point.Buy), "0.00", 0), TradingText.Argument((object)(unit), null, 0))
                : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_72690153CA27", TradingText.Argument((object)(point.Buy), "0.00", 0), TradingText.Argument((object)(unit), null, 0), TradingText.Argument((object)(point.Sell), "0.00", 0), TradingText.Argument((object)(unit), null, 0))) + change);
        }
    }
}
