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
        private void RebuildOrderTracking(ProductDisplayGroup product)
        {
            if (orderTrackingContent == null)
            {
                return;
            }

            IEnumerable<ProductionOrderRecord> sourceRecords = orderTrackingFilterMode == TrackingFilterMode.Current && product != null
                ? productionOrderService.GetRecentOrdersForProduct(product.ProductTag, MaxDisplayedTrackingRecords)
                : productionOrderService.GetRecentOrders(MaxDisplayedTrackingRecords);
            List<ProductionOrderRecord> records = sourceRecords
                .Where(record => StorageNetworkOrderTrackingRules.MatchesFilter(record, orderTrackingFilterMode, orderTrackingSearchText))
                .ToList();
            string signature = StorageNetworkOrderTrackingRules.BuildListSignature(product, records, orderTrackingSearchText, orderTrackingFilterMode);
            if (signature == orderTrackingSignature)
            {
                return;
            }

            orderTrackingSignature = signature;
            EnsureOrderTrackingRows();
            orderTrackingRows.Begin();
            int activeCount = records.Count(StorageNetworkOrderTrackingRules.IsActive);
            UpdateTrackingHeaderRow(GetTrackingScopeTitle(product), activeCount, records.Count);
            if (records.Count == 0)
            {
                UpdateTrackingInfoRow("empty", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_EMPTY), 58f);
                orderTrackingRows.Commit();
                ForceOrderLayout(orderTrackingContent);
                return;
            }

            foreach (ProductionOrderRecord record in records)
            {
                UpdateTrackingCard(orderTrackingContent, record);
            }

            orderTrackingRows.Commit();
            ForceOrderLayout(orderTrackingContent);
        }

        private string GetTrackingScopeTitle(ProductDisplayGroup product)
        {
            if (orderTrackingFilterMode == TrackingFilterMode.Current && product != null)
            {
                return product.ProductName;
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ALL_PRODUCTS);
        }

        private void EnsureOrderTrackingRows()
        {
            if (orderTrackingRows == null || orderTrackingRowsContent != orderTrackingContent)
            {
                orderTrackingRows = new StorageNetworkKeyedRowCache(orderTrackingContent);
                orderTrackingRowsContent = orderTrackingContent;
            }
        }

        private void AddCompactOrderTrackingSection(ProductDisplayGroup product)
        {
            GameObject section = CreateSubPanel(orderDetailsContent, "CompactOrderTracking", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ACTIVE_TITLE), 0f, 0f, 1f);
            section.GetComponent<LayoutElement>().preferredHeight = product == null ? 96f : 230f;
            GameObject contentObject = new GameObject("CompactOrderTrackingContent");
            contentObject.transform.SetParent(section.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            contentObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            AddVerticalLayout(contentObject, 6f, 0, 0, 0, 0);

            orderTrackingContent = content;
            orderTrackingRows = null;
            orderTrackingRowsContent = null;
            orderTrackingSignature = null;
            RebuildOrderTracking(product);
        }

        private void AddExecutionTrackingSection(Transform parent, ProductDisplayGroup product)
        {
            GameObject section = CreateSubPanel(parent, "ExecutionOrderTracking", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_TRACKING_TITLE), 0f, 0f, 1f);
            section.GetComponent<LayoutElement>().preferredHeight = product == null ? 110f : 230f;
            GameObject contentObject = new GameObject("ExecutionOrderTrackingContent");
            contentObject.transform.SetParent(section.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            contentObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            AddVerticalLayout(contentObject, 6f, 0, 0, 0, 0);

            RectTransform previousTrackingContent = orderTrackingContent;
            orderTrackingContent = content;
            orderTrackingRows = null;
            orderTrackingRowsContent = null;
            orderTrackingSignature = null;
            RebuildOrderTracking(product);
            orderTrackingContent = previousTrackingContent;
            orderTrackingRows = null;
            orderTrackingRowsContent = null;
            orderTrackingSignature = null;
        }

        private void AddTrackingCard(Transform parent, ProductionOrderRecord record)
        {
            bool abnormal = record.State == ProductionOrderState.Abnormal;
            bool active = StorageNetworkOrderTrackingRules.IsActive(record);
            Color stateColor = GetOrderStateColor(record.State);
            GameObject card = CreateRoundedOrderImage("TrackingCard", parent, GetTrackingCardColor(record), "UISprite", "Background", "InputField");
            LayoutElement cardElement = card.AddComponent<LayoutElement>();
            cardElement.preferredWidth = TrackingContentWidth - 54f;
            cardElement.preferredHeight = 156f;
            cardElement.flexibleWidth = 0f;
            KButton cardButton = card.AddComponent<KButton>();
            cardButton.bgImage = card.GetComponent<KImage>();
            cardButton.additionalKImages = new KImage[0];
            cardButton.soundPlayer = new ButtonSoundPlayer();
            cardButton.onClick += () => ShowOrderTrackingDetail(record);

            HorizontalLayoutGroup cardLayout = card.AddComponent<HorizontalLayoutGroup>();
            cardLayout.padding = new RectOffset(10, 10, 10, 10);
            cardLayout.spacing = 8f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;

            GameObject main = new GameObject("TrackingMain");
            main.transform.SetParent(card.transform, false);
            main.AddComponent<RectTransform>();
            main.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(main, 6f, 0, 0, 0, 0);

            GameObject top = new GameObject("TrackingTop");
            top.transform.SetParent(main.transform, false);
            top.AddComponent<RectTransform>();
            top.AddComponent<LayoutElement>().preferredHeight = 64f;
            HorizontalLayoutGroup topLayout = top.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.childAlignment = TextAnchor.UpperLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;

            GameObject iconSlot = new GameObject("TrackingIconSlot");
            iconSlot.transform.SetParent(top.transform, false);
            iconSlot.AddComponent<RectTransform>();
            LayoutElement iconSlotLayout = iconSlot.AddComponent<LayoutElement>();
            iconSlotLayout.preferredWidth = 64f;
            iconSlotLayout.preferredHeight = 64f;
            AddIcon(iconSlot.transform, GetTagIcon(record.ProductTag), 50f);

            GameObject titleColumn = new GameObject("TrackingTitleColumn");
            titleColumn.transform.SetParent(top.transform, false);
            titleColumn.AddComponent<RectTransform>();
            titleColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalContainer(titleColumn, 4f, 0, 0, 0, 0);

            TextMeshProUGUI title = CreateOrderText("TrackingTitle", titleColumn.transform, string.Format("#{0} {1}", record.DisplayId, record.ProductName), 14, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.13f, 0.15f, 0.14f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            AddPlanLine(titleColumn.transform, StorageNetworkOrderTrackingRules.GetSummaryLine(record), 9, FontStyles.Normal, MutedTextColor(), 18f);

            GameObject detailArea = CreatePlainImage("TrackingDetailArea", main.transform, new Color(0.78f, 0.78f, 0.71f, 0.42f));
            detailArea.AddComponent<LayoutElement>().preferredHeight = 54f;
            AddVerticalContainer(detailArea, 4f, 6, 6, 4, 4);
            AddTrackingProgressRow(detailArea.transform, record, stateColor);
            AddWrappedPlanLine(detailArea.transform, StorageNetworkOrderTrackingRules.GetDetailLine(record), 10, abnormal ? FontStyles.Bold : FontStyles.Normal, abnormal ? DangerColor() : NeutralTextColor(), 17f, 2, 24);

            AddTrackingSeparator(card.transform, 1f);

            GameObject side = new GameObject("TrackingSide");
            side.transform.SetParent(card.transform, false);
            side.AddComponent<RectTransform>();
            side.AddComponent<LayoutElement>().preferredWidth = 96f;
            AddVerticalContainer(side, 7f, 0, 0, 0, 0);
            VerticalLayoutGroup sideLayout = side.GetComponent<VerticalLayoutGroup>();
            if (sideLayout != null)
            {
                sideLayout.childForceExpandWidth = false;
                sideLayout.childAlignment = TextAnchor.UpperRight;
            }

            AddTrackingStateBadge(side.transform, StorageNetworkOrderTrackingRules.GetOrderStateLabel(record.State), stateColor, 52f, 94f);
            AddTrackingDottedLine(side.transform);
            AddPlanLine(side.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CYCLE_VALUE), ProductionOrderFormatting.FormatCycle(record.CreatedCycle)), 10, FontStyles.Bold, NeutralTextColor(), 20f);
            AddPlanLine(side.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_CREATED_CYCLE), 8, FontStyles.Normal, MutedTextColor(), 15f);

            if (record.MergeCount > 0)
            {
                AddPlanLine(main.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MERGED_ACTIVITY), record.MergeCount, ProductionOrderFormatting.FormatCycle(record.LastActivityCycle)), 8, FontStyles.Italic, MutedTextColor(), 15f);
                cardElement.preferredHeight += 18f;
            }

            if (active)
            {
                GameObject cancelButton = CreateTransparentIconButton("CancelOrderButton", card.transform, GetCancelActionSprite(), () => CancelTrackedOrder(record.Key));
                LayoutElement cancelLayout = cancelButton.AddComponent<LayoutElement>();
                cancelLayout.preferredWidth = 24f;
                cancelLayout.preferredHeight = 24f;
            }
        }

        private void UpdateTrackingHeaderRow(string productName, int activeCount, int recordCount)
        {
            GameObject row = orderTrackingRows.Use("header", () =>
            {
                TextMeshProUGUI created = CreateOrderText("TrackingHeader", orderTrackingContent, string.Empty, 11, TextAlignmentOptions.MidlineLeft);
                created.color = new Color(0.14f, 0.16f, 0.15f, 1f);
                created.fontStyle = FontStyles.Bold;
                created.richText = true;
                created.textWrappingMode = TextWrappingModes.Normal;
                created.overflowMode = TextOverflowModes.Ellipsis;
                created.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
                return created.gameObject;
            });

            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_SUMMARY), productName, activeCount, recordCount);
            }
        }

        private void UpdateTrackingInfoRow(string key, string text, float height)
        {
            GameObject row = orderTrackingRows.Use("info:" + key, () =>
            {
                TextMeshProUGUI created = CreateOrderText("TrackingInfo", orderTrackingContent, string.Empty, 10, TextAlignmentOptions.MidlineLeft);
                created.color = MutedTextColor();
                created.fontStyle = FontStyles.Italic;
                created.richText = true;
                created.textWrappingMode = TextWrappingModes.Normal;
                created.overflowMode = TextOverflowModes.Ellipsis;
                created.gameObject.AddComponent<LayoutElement>();
                return created.gameObject;
            });

            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text;
            }

            LayoutElement layout = row.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = height;
            }
        }

        private void UpdateTrackingCard(Transform parent, ProductionOrderRecord record)
        {
            string key = "order:" + (record.Key ?? record.DisplayId.ToString());
            string signature = StorageNetworkOrderTrackingRules.BuildCardSignature(record);
            bool recreate = orderTrackingRows.TryGetMetadata(key, out string oldSignature) && oldSignature != signature;
            orderTrackingRows.Use(key, () =>
            {
                AddTrackingCard(parent, record);
                return parent.GetChild(parent.childCount - 1).gameObject;
            }, recreate);
            orderTrackingRows.SetMetadata(key, signature);
        }

        private void CancelTrackedOrder(string orderKey)
        {
            lastOrderStatus = productionOrderService.CancelOrder(orderKey, StorageNetworkCycleTime.GetCurrent());
            productionOrderService.Refresh();
            RebuildOrderDetails();
        }

    }
}
