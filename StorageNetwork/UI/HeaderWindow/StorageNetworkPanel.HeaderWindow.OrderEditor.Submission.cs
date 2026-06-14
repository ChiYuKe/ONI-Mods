using System.Linq;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void AddValidationPanel(Transform parent, ProductDisplayGroup product, ProductionOrderDraft draft)
        {
            AddSmallTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_DRAFT_VALIDATION_TITLE));
            GameObject card = CreatePlainImage("ValidationCard", parent, new Color(0.94f, 0.92f, 0.84f, 1f));
            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 118f;
            cardLayout.minHeight = 118f;
            cardLayout.flexibleHeight = 0f;
            AddVerticalLayout(card, 0f, 0, 0, 0, 0);

            Color statusColor = draft.CanSubmit
                ? new Color(0.39f, 0.53f, 0.37f, 1f)
                : new Color(GetRiskColor(draft.RiskLevel).r, GetRiskColor(draft.RiskLevel).g, GetRiskColor(draft.RiskLevel).b, 0.88f);
            GameObject header = CreatePlainImage("ValidationHeader", card.transform, statusColor);
            header.AddComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(8, 8, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            AddStatusIcon(header.transform, draft.CanSubmit);

            TextMeshProUGUI title = CreateText("ValidationTitle", header.transform, draft.CanSubmit ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RISK_READY) : StorageNetworkOrderEditorText.GetRiskLabel(draft.RiskLevel), 12, TextAlignmentOptions.MidlineLeft);
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject body = new GameObject("ValidationBody");
            body.transform.SetParent(card.transform, false);
            body.AddComponent<RectTransform>();
            body.AddComponent<LayoutElement>().preferredHeight = 88f;
            AddVerticalLayout(body, 4f, 10, 10, 8, 8);

            string message = draft.ValidationMessages.Count > 0
                ? string.Join("\n", draft.ValidationMessages.Take(2))
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_VALIDATION_READY_BODY);
            AddWrappedPlanLine(body.transform, message, 10, FontStyles.Bold, draft.CanSubmit ? new Color(0.25f, 0.42f, 0.27f, 1f) : GetRiskColor(draft.RiskLevel), 34f, 2, 40);
            AddPlanLine(body.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_VALIDATION_OUTPUT), GameUtil.GetFormattedMass(draft.RequestedAmount)), 10, FontStyles.Bold, new Color(0.18f, 0.20f, 0.18f, 1f), 20f);
            AddPlanLine(body.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_VALIDATION_ACTIVE_ORDERS), product.ProductName, productionOrderService.GetActiveOrdersForProduct(product.ProductTag, 99).Count), 8, FontStyles.Italic, MutedTextColor(), 18f);
        }

        private static void AddStatusIcon(Transform parent, bool positive)
        {
            GameObject iconObject = new GameObject("StatusIcon");
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(20f, 20f);
            LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 20f;
            iconLayout.preferredHeight = 20f;

            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = GetSpriteByName(positive ? "crew_state_encourage" : "crew_state_unhappy");
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private void AddOrderFooter(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route, ProductionOrderDraft draft)
        {
            GameObject footer = CreateSection(parent, "OrderFooter", 72f, new Color(0.64f, 0.65f, 0.59f, 1f));
            HorizontalLayoutGroup layout = footer.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 9, 9);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            string statusText = !string.IsNullOrEmpty(lastOrderStatus)
                ? lastOrderStatus
                : draft.CanSubmit ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_FOOTER_READY) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_FOOTER_BLOCKED);
            TextMeshProUGUI status = CreateText("FooterStatus", footer.transform, statusText, 10, TextAlignmentOptions.MidlineLeft);
            status.color = draft.CanSubmit ? PositiveColor() : GetRiskColor(draft.RiskLevel);
            status.fontStyle = FontStyles.Bold;
            status.textWrappingMode = TextWrappingModes.Normal;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.maxVisibleLines = 3;
            LayoutElement statusLayout = status.gameObject.AddComponent<LayoutElement>();
            statusLayout.flexibleWidth = 1f;
            statusLayout.preferredHeight = 54f;

            GameObject button = CreateGameButton("ConfirmOrder", footer.transform, draft.DuplicatePolicy == ProductionOrderDuplicatePolicy.MergeIntoExisting ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CONFIRM_MERGE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CONFIRM), () =>
            {
                ProductionOrderSubmitResult result = productionOrderService.SubmitOrder(product, route, requestedProductAmount, StorageNetworkCycleTime.GetCurrent());
                lastOrderStatus = result.Message;
                productionOrderService.Refresh();
                orderDetailsSignature = null;
                orderTrackingSignature = null;
                RebuildOrderDetails();
            });
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.minWidth = 150f;
            buttonLayout.preferredHeight = 42f;
            buttonLayout.minHeight = 42f;
            buttonLayout.flexibleWidth = 0f;
            buttonLayout.flexibleHeight = 0f;
            button.GetComponent<KButton>().isInteractable = draft.CanSubmit;
        }

        private string BuildOrderTrackingStatus(ProductDisplayGroup product, ProductionOrderDraft draft)
        {
            int activeCount = productionOrderService.GetActiveOrdersForProduct(product.ProductTag, 99).Count;
            return StorageNetworkOrderEditorText.BuildTrackingStatus(lastOrderStatus, draft, activeCount);
        }

        private static Color GetRiskColor(ProductionOrderRiskLevel risk)
        {
            switch (risk)
            {
                case ProductionOrderRiskLevel.Blocked:
                    return DangerColor();
                case ProductionOrderRiskLevel.Warning:
                    return WarningColor();
                default:
                    return PositiveColor();
            }
        }
    }
}
