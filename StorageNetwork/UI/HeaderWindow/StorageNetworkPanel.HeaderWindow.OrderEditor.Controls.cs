using System.Collections.Generic;
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
        private void AddAmountEditor(Transform parent, ProductDisplayGroup product)
        {
            AddSmallTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_AMOUNT_LABEL));
            GameObject panel = CreatePlainImage("AmountEditor", parent, new Color(0.78f, 0.78f, 0.72f, 1f));
            LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
            panelLayout.minHeight = 66f;
            panelLayout.preferredHeight = 66f;
            panelLayout.flexibleHeight = 0f;
            VerticalLayoutGroup panelGroup = panel.AddComponent<VerticalLayoutGroup>();
            panelGroup.padding = new RectOffset(6, 6, 5, 5);
            panelGroup.spacing = 4f;
            panelGroup.childAlignment = TextAnchor.MiddleLeft;
            panelGroup.childControlWidth = true;
            panelGroup.childControlHeight = true;
            panelGroup.childForceExpandWidth = true;
            panelGroup.childForceExpandHeight = false;

            GameObject row = new GameObject("AmountValueRow");
            row.transform.SetParent(panel.transform, false);
            row.AddComponent<RectTransform>();
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 26f;
            rowLayout.minHeight = 26f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            orderAmountInput = CreateFixedAmountInput(row.transform, 92f, 24f);
            StorageNetworkNumberInputField orderAmountNumberInput = orderAmountInput.GetComponent<StorageNetworkNumberInputField>();
            orderAmountNumberInput?.Configure(orderAmountInput, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, float.MaxValue, false);
            orderAmountNumberInput?.SetAmount(requestedProductAmount);
            if (orderAmountNumberInput != null)
            {
                orderAmountNumberInput.onEndEdit += () =>
                {
                    requestedProductAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, orderAmountNumberInput.currentValue);
                    orderDetailsSignature = null;
                    RebuildOrderDetails();
                };
            }
            else
            {
                orderAmountInput.text = FormatAmount(requestedProductAmount);
                orderAmountInput.onEndEdit.AddListener(value =>
                {
                    if (TryParseAmount(value, out float parsed))
                    {
                        requestedProductAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, parsed);
                        orderDetailsSignature = null;
                    }

                    RebuildOrderDetails();
                });
            }

            TextMeshProUGUI unit = CreateText("Unit", row.transform, "kg", 15, TextAlignmentOptions.MidlineLeft);
            unit.color = MutedTextColor();
            unit.gameObject.AddComponent<LayoutElement>().preferredWidth = 20f;

            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            AddAmountAdjustButton(row.transform, "icon_TrendArrows_Down_1", () => AdjustRequestedAmount(-orderAmountStep));
            AddAmountAdjustButton(row.transform, "icon_TrendArrows_Up_1", () => AdjustRequestedAmount(orderAmountStep));
            AddAmountStepSelector(panel.transform);
        }

        private void AddKeepRuleEditor(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo route)
        {
            ProductionKeepRule rule = productionOrderService.GetKeepRule(product.ProductTag);
            AddSmallTitle(parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_TITLE));
            GameObject row = CreatePlainImage("KeepRuleRow", parent, new Color(0.78f, 0.78f, 0.72f, 1f));
            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 34f;
            rowElement.minHeight = 34f;
            rowElement.flexibleHeight = 0f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 5, 5);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            string label = rule != null
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_STATUS), GameUtil.GetFormattedMass(rule.TargetAmount))
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_DISABLED);
            TextMeshProUGUI status = CreateText("KeepRuleStatus", row.transform, label, 11, TextAlignmentOptions.MidlineLeft);
            status.color = rule != null ? PositiveColor() : MutedTextColor();
            status.fontStyle = FontStyles.Bold;
            status.textWrappingMode = TextWrappingModes.NoWrap;
            status.overflowMode = TextOverflowModes.Ellipsis;
            status.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            EnsureKeepRuleDraft(product, rule);
            keepRuleAmountInput = CreateFixedAmountInput(row.transform, 74f, 24f);
            StorageNetworkNumberInputField keepRuleNumberInput = keepRuleAmountInput.GetComponent<StorageNetworkNumberInputField>();
            keepRuleNumberInput?.Configure(keepRuleAmountInput, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, float.MaxValue, false);
            keepRuleNumberInput?.SetAmount(keepRuleDraftAmount);
            if (keepRuleNumberInput != null)
            {
                keepRuleNumberInput.onEndEdit += () =>
                {
                    keepRuleDraftAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, keepRuleNumberInput.currentValue);
                    orderDetailsSignature = null;
                };
            }
            else
            {
                keepRuleAmountInput.text = FormatAmount(keepRuleDraftAmount);
                keepRuleAmountInput.onEndEdit.AddListener(value =>
                {
                    if (TryParseAmount(value, out float parsed))
                    {
                        keepRuleDraftAmount = Mathf.Max(PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT, parsed);
                        orderDetailsSignature = null;
                    }
                });
            }

            GameObject enableButton = CreateStyledButton("EnableKeepRule", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_BUTTON), () =>
            {
                float keepAmount = GetKeepRuleAmountFromInput(rule);
                productionOrderService.SetKeepRule(product, route, keepAmount);
                lastOrderStatus = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_ENABLED_STATUS), product.ProductName, GameUtil.GetFormattedMass(keepAmount));
                productionOrderService.Refresh();
                orderDetailsSignature = null;
                RebuildOrderDetails();
            }, KleiBlueStyle());
            LayoutElement enableLayout = enableButton.AddComponent<LayoutElement>();
            enableLayout.preferredWidth = 44f;
            enableLayout.preferredHeight = 24f;

            GameObject clearButton = CreateStyledButton("ClearKeepRule", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE), () =>
            {
                productionOrderService.ClearKeepRule(product.ProductTag);
                lastOrderStatus = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_CLEARED_STATUS), product.ProductName);
                orderDetailsSignature = null;
                RebuildOrderDetails();
            }, rule != null ? KleiPinkStyle() : KleiBlueStyle());
            LayoutElement clearLayout = clearButton.AddComponent<LayoutElement>();
            clearLayout.preferredWidth = 44f;
            clearLayout.preferredHeight = 24f;
            clearButton.GetComponent<KButton>().isInteractable = rule != null;

        }

        private void AddKeepRuleMetrics(Transform parent, ProductDisplayGroup product)
        {
            ProductionKeepRule rule = productionOrderService.GetKeepRule(product.ProductTag);
            float currentStock = productionOrderService.GetNetworkRawAmount(product.ProductTag);
            float automaticPending = productionOrderService.GetActiveOrdersForProduct(product.ProductTag, 100)
                .Where(order => order.IsAutomatic)
                .Sum(order => Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit));
            float targetStock = rule != null ? rule.TargetAmount : 0f;
            float shortage = rule != null ? Mathf.Max(0f, targetStock - currentStock - automaticPending) : 0f;

            GameObject metrics = new GameObject("KeepRuleMetrics");
            metrics.transform.SetParent(parent, false);
            metrics.AddComponent<RectTransform>();
            LayoutElement metricsElement = metrics.AddComponent<LayoutElement>();
            metricsElement.preferredHeight = 48f;
            metricsElement.minHeight = 48f;
            metricsElement.flexibleHeight = 0f;
            HorizontalLayoutGroup metricsLayout = metrics.AddComponent<HorizontalLayoutGroup>();
            metricsLayout.spacing = 5f;
            metricsLayout.childControlWidth = true;
            metricsLayout.childControlHeight = true;
            metricsLayout.childForceExpandWidth = true;
            metricsLayout.childForceExpandHeight = true;

            AddKeepRuleMetric(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_CURRENT_STOCK), currentStock, PositiveColor());
            if (rule != null)
            {
                AddKeepRuleMetric(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_TARGET_STOCK), targetStock, NeutralTextColor());
            }
            else
            {
                AddKeepRuleMetricText(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_TARGET_STOCK), Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_DISABLED), MutedTextColor());
            }
            AddKeepRuleMetric(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_PENDING), automaticPending, WarningColor());
            if (rule != null)
            {
                AddKeepRuleMetric(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_SHORTAGE), shortage, shortage > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? WarningColor() : PositiveColor());
            }
            else
            {
                AddKeepRuleMetricText(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_SHORTAGE), Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_KEEP_DISABLED), MutedTextColor());
            }
        }

        private void AddKeepRuleMetric(Transform parent, string format, float amount, Color valueColor)
        {
            AddKeepRuleMetricText(parent, format, GameUtil.GetFormattedMass(amount), valueColor);
        }

        private void AddKeepRuleMetricText(Transform parent, string format, string value, Color valueColor)
        {
            GameObject card = CreatePlainImage("KeepRuleMetric", parent, new Color(0.72f, 0.73f, 0.68f, 1f));
            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 48f;
            cardLayout.minHeight = 48f;
            cardLayout.flexibleHeight = 0f;
            VerticalLayoutGroup cardContentLayout = card.AddComponent<VerticalLayoutGroup>();
            cardContentLayout.padding = new RectOffset(3, 3, 4, 4);
            cardContentLayout.childAlignment = TextAnchor.MiddleCenter;
            cardContentLayout.childControlWidth = true;
            cardContentLayout.childControlHeight = true;
            cardContentLayout.childForceExpandWidth = true;
            cardContentLayout.childForceExpandHeight = false;
            TextMeshProUGUI text = CreateText("Value", card.transform, string.Format(format, value), 10, TextAlignmentOptions.Center);
            text.color = valueColor;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.preferredHeight = 38f;
            textLayout.minHeight = 38f;
            textLayout.flexibleHeight = 0f;
        }

        private void AddRouteEditor(Transform parent, ProductDisplayGroup product)
        {
            AddSmallTitle(parent, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_SECTION), product.Routes.Select(route => route.FabricatorName).Distinct().Count()));

            foreach (IGrouping<string, RecipeDisplayInfo> group in product.Routes.GroupBy(route => route.FabricatorName).Take(5))
            {
                int routeIndex = product.Routes.FindIndex(route => route.FabricatorName == group.Key);
                bool selected = selectedRouteIndex >= 0 &&
                                selectedRouteIndex < product.Routes.Count &&
                                product.Routes[selectedRouteIndex].FabricatorName == group.Key;

                GameObject button = CreateStyledButton(
                    "RouteDeviceButton",
                    parent,
                    string.Empty,
                    () =>
                    {
                        selectedRouteIndex = routeIndex;
                        lastOrderStatus = null;
                        orderDetailsSignature = null;
                        RebuildOrderDetails();
                    },
                    selected ? KleiPinkStyle() : KleiBlueStyle());

                LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
                buttonLayout.preferredHeight = 58f;
                buttonLayout.minHeight = 58f;
                buttonLayout.flexibleHeight = 0f;

                HorizontalLayoutGroup layout = button.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(10, 12, 6, 6);
                layout.spacing = 10f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                RecipeDisplayInfo firstRoute = group.FirstOrDefault();
                AddIcon(button.transform, GetFabricatorIcon(firstRoute), 38f);

                int fabricatorCount = group.SelectMany(route => route.Fabricators ?? new List<ComplexFabricator>()).Distinct().Count();
                TextMeshProUGUI label = CreateText("DeviceName", button.transform, BuildRouteDeviceLabel(group.Key, fabricatorCount, group.Count()), 12, TextAlignmentOptions.MidlineLeft);
                label.color = new Color(0.94f, 0.96f, 0.98f, 1f);
                label.fontStyle = FontStyles.Bold;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                TextMeshProUGUI arrow = CreateText("Arrow", button.transform, ">", 18, TextAlignmentOptions.Center);
                arrow.color = new Color(0.94f, 0.96f, 0.98f, 1f);
                arrow.fontStyle = FontStyles.Bold;
                arrow.gameObject.AddComponent<LayoutElement>().preferredWidth = 22f;
            }
        }

        private static Sprite GetFabricatorIcon(RecipeDisplayInfo route)
        {
            ComplexFabricator fabricator = route.Fabricators?.FirstOrDefault(item => item != null);
            KPrefabID prefabId = fabricator != null ? fabricator.GetComponent<KPrefabID>() : null;
            if (prefabId != null)
            {
                var uiSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                if (uiSprite.first != null)
                {
                    return uiSprite.first;
                }
            }

            return route.Icon;
        }

        private static string BuildRouteDeviceLabel(string fabricatorName, int fabricatorCount, int recipeCount)
        {
            return recipeCount > 1
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_DEVICE_MULTI), fabricatorCount, recipeCount)
                : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_ROUTE_DEVICE_SINGLE), fabricatorCount);
        }

        private void AddRecipeEditor(Transform parent, ProductDisplayGroup product, RecipeDisplayInfo selectedRoute)
        {
            List<RecipeDisplayInfo> alternatives = product.Routes
                .Where(route => route.FabricatorName == selectedRoute.FabricatorName)
                .ToList();
            AddSmallTitle(parent, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_RECIPE_SECTION), alternatives.Count));
            if (alternatives.Count <= 1)
            {
                AddPlanLine(parent, ProductionOrderFormatting.FormatRecipeElements(selectedRoute.Recipe.ingredients), 12, FontStyles.Bold, new Color(0.18f, 0.21f, 0.21f, 1f), 24f);
                return;
            }

            foreach (RecipeDisplayInfo route in alternatives.Take(4))
            {
                int routeIndex = product.Routes.IndexOf(route);
                string label = ProductionOrderFormatting.FormatRecipeElements(route.Recipe.ingredients);
                AddChoiceButton(parent, routeIndex == selectedRouteIndex ? "> " + label : label, routeIndex, routeIndex == selectedRouteIndex, 15f);
            }
        }
    }
}
