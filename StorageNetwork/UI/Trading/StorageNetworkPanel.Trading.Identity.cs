using StorageNetwork.API;
using StorageNetwork.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private GameObject tradingIdentityRoot;

        private void ShowTradingIdentityCard(int supplierId, bool returnToConversation = false)
        {
            TradingSupplier supplier = StorageNetworkTradingState.Instance?.Market?.Suppliers.Find(s => s.Id == supplierId);
            if (supplier == null || supplier.Departure <= StorageNetworkTradingState.Cycle) return;
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            modalRoot = CreateAmountModalFrame(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_B527C37EB500") + (supplier.Name ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_C8A11D7DF26E")), 680, 560, out GameObject body);
            tradingIdentityRoot = modalRoot;
            GameObject heading = AddAmountModalRow(body.transform, 12, 44);
            var avatar = new GameObject("Portrait", typeof(RectTransform), typeof(LayoutElement), typeof(StorageNetworkTradingPortrait));
            avatar.transform.SetParent(heading.transform, false);
            LayoutElement avatarLayout = avatar.GetComponent<LayoutElement>();
            avatarLayout.minWidth = avatarLayout.preferredWidth = 44;
            avatarLayout.minHeight = avatarLayout.preferredHeight = 44;
            avatar.GetComponent<StorageNetworkTradingPortrait>().Bind(supplier);
            var title = CreateAmountModalText(heading.transform, supplier.Name + "\n" + TradingPersonas.Specialty(supplier.Specialty), 13, FontStyles.Bold);
            title.color = StorageNetworkPanelPalette.BodyText;
            LayoutElement titleLayout = title.GetComponent<LayoutElement>();
            titleLayout.minWidth = titleLayout.preferredWidth = 124;
            titleLayout.flexibleWidth = 0;
            BuildTradingAffectionBar(heading.transform, supplier);
            GameObject frame = CreatePlainImage("IdentityDetails", body.transform, StorageNetworkPanelPalette.RowBackground);
            frame.AddComponent<LayoutElement>().preferredHeight = 360;
            RectTransform viewport = CreateScrollViewport(frame.transform, "Details", out RectTransform content, 0, 0, 0, 0, 8);
            Scrollbar scrollbar = CreateScrollbar(frame.transform, 0, 0);
            WireScrollRect(viewport.gameObject, content, scrollbar, 26);
            var details = CreateAmountModalText(content, TradingRelationships.Card(supplier) + "\n\n" + TradingPersonas.PublicCard(supplier), 13, FontStyles.Normal);
            details.alignment = TextAlignmentOptions.TopLeft;
            details.color = StorageNetworkPanelPalette.BodyText;
            details.GetComponent<LayoutElement>().preferredHeight = details.GetPreferredValues(details.text, 590, 0).y + 16;
            GameObject buttons = AddAmountModalRow(body.transform, 8, 28);
            AddFooterSpacer(buttons.transform);
            AddAmountModalButton(buttons.transform, returnToConversation ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_A9AD48A1556B") : TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_37270849D0C9"), 100, () =>
            {
                if (modalRoot != null) modalRoot.SetActive(false);
                CloseModal();
                if (returnToConversation) ShowTradingDialogue(supplierId);
            });
        }

        private void BuildTradingAffectionBar(Transform parent, TradingSupplier supplier)
        {
            GameObject panel = new GameObject("AffectionProgress", typeof(RectTransform), typeof(LayoutElement));
            panel.transform.SetParent(parent, false);
            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            panelLayout.minWidth = 260;
            panelLayout.preferredWidth = 300;
            panelLayout.flexibleWidth = 1;
            panelLayout.preferredHeight = 44;
            decimal favor = TradingRelationships.Favor(supplier);
            Color accent = favor >= 60 ? StorageNetworkPanelPalette.PositiveStatus : new Color(0.68f, 0.36f, 0.53f, 1);
            var level = CreateText("RelationshipLevel", panel.transform, TradingRelationships.Stage(supplier), 12, TextAlignmentOptions.MidlineLeft);
            level.fontStyle = FontStyles.Bold;
            level.color = StorageNetworkPanelPalette.HeadingText;
            RectTransform levelRect = level.rectTransform();
            levelRect.anchorMin = levelRect.anchorMax = levelRect.pivot = new Vector2(0, 1);
            float levelWidth = Mathf.Clamp(level.GetPreferredValues(level.text).x + 6, 36, 124);
            levelRect.sizeDelta = new Vector2(levelWidth, 22);
            levelRect.anchoredPosition = Vector2.zero;
            var percentage = CreateText("AffectionPercent", panel.transform, favor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "%", 12, TextAlignmentOptions.MidlineRight);
            percentage.fontStyle = FontStyles.Bold;
            percentage.color = accent;
            RectTransform percentageRect = percentage.rectTransform();
            percentageRect.anchorMin = percentageRect.anchorMax = percentageRect.pivot = new Vector2(1, 1);
            percentageRect.sizeDelta = new Vector2(52, 22);
            percentageRect.anchoredPosition = Vector2.zero;
            GameObject track = CreatePlainImage("Track", panel.transform, new Color(0.16f, 0.18f, 0.23f, 1));
            SetTopStretch(track.GetComponent<RectTransform>(), levelWidth + 8, 60, 7, 8);
            track.GetComponent<Image>().type = Image.Type.Simple;
            track.GetComponent<Image>().raycastTarget = false;
            GameObject fill = CreatePlainImage("Fill", track.transform, accent);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.type = Image.Type.Simple;
            fillImage.raycastTarget = false;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01((float)(favor / 100m)), 1);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        }
    }
}
