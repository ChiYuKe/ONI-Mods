using System;
using StorageNetwork.API;
using StorageNetwork.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetworkTrading.UI
{
    public sealed partial class TradingPanel
    {
        private readonly GameObject[] supplierTipButtons = new GameObject[5];
        private void BuildMerchantTipButton(Transform parent, int slot)
        {
            var button = CreateTradingAlertButton("MerchantIntelligence", parent, () => ShowMerchantTip(slot), 1);
            supplierTipButtons[slot] = button;
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(22, 22);
            rect.anchoredPosition = new Vector2(-31, -5);
            button.AddComponent<ToolTip>().toolTip = TradingText.Render(WeeklyLabel("TIP_HINT"));
            button.SetActive(false);
        }

        private void ShowMerchantTip(int slot)
        {
            var state = StorageNetworkTradingState.Instance;
            state?.Refresh();
            if (state?.Market == null || slot >= state.Market.Suppliers.Count) return;
            var supplier = state.Market.Suppliers[slot];
            var tip = state.Market.PendingMerchantTip(supplier.Id, StorageNetworkTradingState.Cycle);
            if (tip == null) { RefreshTradingWindow(); return; }
            state.ReadMerchantTip(tip);
            supplierTipButtons[slot].SetActive(false);
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            modalRoot = CreateAmountModalFrame(WeeklyLabel("TIP_TITLE", supplier.Name), 660, 530, out GameObject body);
            var heading = AddAmountModalRow(body.transform, 12, 48);
            var avatar = new GameObject("SourcePortrait", typeof(RectTransform), typeof(LayoutElement), typeof(StorageNetworkTradingPortrait));
            avatar.transform.SetParent(heading.transform, false);
            var avatarLayout = avatar.GetComponent<LayoutElement>();
            avatarLayout.minWidth = avatarLayout.preferredWidth = 44;
            avatarLayout.preferredHeight = 44;
            avatar.GetComponent<StorageNetworkTradingPortrait>().Bind(supplier);
            var name = CreateAmountModalText(heading.transform, supplier.Name + "\n" + TradingPersonas.Specialty(supplier.Specialty), 13, FontStyles.Bold);
            name.color = StorageNetworkPanelPalette.BodyText;
            var frame = CreatePlainImage("MerchantMessage", body.transform, StorageNetworkPanelPalette.RowBackground);
            var frameLayout = frame.AddComponent<LayoutElement>();
            frameLayout.preferredHeight = 330;
            frameLayout.flexibleHeight = 1;
            var viewport = CreateScrollViewport(frame.transform, "Message", out RectTransform content, 8, 12, 4, 8, 12);
            WireScrollRect(viewport.gameObject, content, CreateScrollbar(frame.transform, 8, 8), 26);
            foreach (var article in tip.Articles)
            {
                var title = CreateAmountModalText(content, article.Title, 14, FontStyles.Bold);
                title.color = StorageNetworkPanelPalette.HeaderBackground;
                title.GetComponent<LayoutElement>().preferredHeight = 28;
                var text = CreateAmountModalText(content, article.Body, 13, FontStyles.Normal);
                text.color = StorageNetworkPanelPalette.BodyText;
                text.alignment = TextAlignmentOptions.TopLeft;
                text.GetComponent<LayoutElement>().preferredHeight = Math.Max(48, text.GetPreferredValues(text.text, 540, 0).y + 16);
            }
            var note = CreateAmountModalText(body.transform, WeeklyLabel("TIP_NOTE", TradingText.Argument(tip.Cycle + 1, "0.0")), 11, FontStyles.Normal);
            note.color = StorageNetworkPanelPalette.MutedText;
            note.GetComponent<LayoutElement>().preferredHeight = 36;
        }
    }
}
