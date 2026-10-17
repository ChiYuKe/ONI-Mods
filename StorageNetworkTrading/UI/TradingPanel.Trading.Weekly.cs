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
        private TextMeshProUGUI tradingWeeklyButtonLabel;
        private static string WeeklyLabel(string key, params string[] args) =>
            TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.WEEKLY_" + key, args);

        private void RefreshWeeklyButton()
        {
            bool unread = StorageNetworkTradingState.Instance?.Market?.WeeklyReports?.Any(r => !r.Read) == true;
            SetTextIfChanged(tradingWeeklyButtonLabel, WeeklyLabel(unread ? "BUTTON_NEW" : "BUTTON"));
        }

        private void ShowTradingWeeklyReport(int index = -1)
        {
            var state = StorageNetworkTradingState.Instance;
            state?.Refresh();
            var reports = state?.Market?.WeeklyReports;
            if (reports == null || reports.Count == 0) return;
            if (index < 0) index = reports.Count - 1;
            index = Math.Min(index, reports.Count - 1);
            var report = reports[index];
            state.ReadWeeklyReport(report);
            RefreshWeeklyButton();
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            modalRoot = CreateAmountModalFrame(WeeklyLabel("TITLE", TradingText.Argument(report.Number)), 740, 620, out GameObject body);
            var navigation = AddAmountModalRow(body.transform, 8, 28);
            int selected = index;
            var older = AddAmountModalButton(navigation.transform, WeeklyLabel("OLDER"), 92, () => ShowTradingWeeklyReport(selected - 1));
            older.GetComponent<KButton>().isInteractable = index > 0;
            var newer = AddAmountModalButton(navigation.transform, WeeklyLabel("NEWER"), 92, () => ShowTradingWeeklyReport(selected + 1));
            newer.GetComponent<KButton>().isInteractable = index < reports.Count - 1;
            AddFooterSpacer(navigation.transform);
            var date = CreateAmountModalText(navigation.transform, WeeklyLabel("DATE", TradingText.Argument(report.Cycle + 1, "0.0")), 12, FontStyles.Normal);
            date.color = StorageNetworkPanelPalette.MutedText;
            date.GetComponent<LayoutElement>().preferredWidth = 210;
            GameObject frame = CreatePlainImage("WeeklyArticles", body.transform, StorageNetworkPanelPalette.RowBackground);
            var frameLayout = frame.AddComponent<LayoutElement>();
            frameLayout.preferredHeight = 430;
            frameLayout.flexibleHeight = 1;
            RectTransform viewport = CreateScrollViewport(frame.transform, "Articles", out RectTransform content, 8, 12, 4, 8, 12);
            WireScrollRect(viewport.gameObject, content, CreateScrollbar(frame.transform, 8, 8), 26);
            foreach (var article in report.Articles)
            {
                var heading = CreateAmountModalText(content, article.Title, 15, FontStyles.Bold);
                heading.color = StorageNetworkPanelPalette.HeaderBackground;
                heading.GetComponent<LayoutElement>().preferredHeight = 30;
                var text = CreateAmountModalText(content, FormatTradingNewsBody(article.Body), 13, FontStyles.Normal);
                text.color = StorageNetworkPanelPalette.BodyText;
                text.alignment = TextAlignmentOptions.TopLeft;
                text.GetComponent<LayoutElement>().preferredHeight = Math.Max(48, text.GetPreferredValues(text.text, 620, 0).y + 20);
            }
            var note = CreateAmountModalText(body.transform, WeeklyLabel("NOTE"), 11, FontStyles.Normal);
            note.color = StorageNetworkPanelPalette.MutedText;
            note.GetComponent<LayoutElement>().preferredHeight = 40;
        }
    }
}
