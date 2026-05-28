using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {
        private void ShowProductionSettingsPanel(Storage storage)
        {
            bool sameStorage = productionSettingsStorage == storage;
            productionSettingsStorage = storage;
            EnsureProductionSettingsPanel();
            productionSettingsRoot.SetActive(true);
            KeepProductionSettingsPanelOnScreen();
            UpdateProductionSettingsPanel(!sameStorage);
        }

        private void CloseProductionSettingsPanel()
        {
            CloseProductionPicker();
            if (productionSettingsRoot != null)
            {
                productionSettingsRoot.SetActive(false);
            }
        }

        private void EnsureProductionSettingsPanel()
        {
            if (productionSettingsRoot != null)
            {
                return;
            }

            productionSettingsRoot = CreateBox("ProductionSettingsPanel", transform, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            productionSettingsRoot.AddComponent<ScrollWheelBlocker>();
            ApplyThinBoxSprite(productionSettingsRoot.GetComponent<Image>());
            RectTransform panelRect = productionSettingsRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 24f);
            panelRect.sizeDelta = new Vector2(760f, 560f);

            GameObject header = CreateBox("Header", productionSettingsRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);
            TextMeshProUGUI title = CreateText("Title", header.transform, string.Empty, 13, TextAlignmentOptions.TopLeft);
            title.name = "ProductionSettingsTitle";
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = 2f;
            Stretch(title.rectTransform(), 10f, 7f);

            GameObject closeButton = CreateGameButton("CloseButton", header.transform, "X", CloseProductionSettingsPanel);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-4f, -4f);
            closeRect.sizeDelta = new Vector2(22f, 20f);

            GameObject viewport = CreateBox("Viewport", productionSettingsRoot.transform, new Color(0.72f, 0.72f, 0.66f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 10f, 10f, 10f, 70f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            productionSettingsContent = content.AddComponent<RectTransform>();
            productionSettingsContent.anchorMin = new Vector2(0f, 1f);
            productionSettingsContent.anchorMax = new Vector2(1f, 1f);
            productionSettingsContent.pivot = new Vector2(0.5f, 1f);
            productionSettingsContent.offsetMin = Vector2.zero;
            productionSettingsContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(productionSettingsRoot.transform);

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = productionSettingsContent;
            ConfigureSmoothVerticalScroll(scrollRect, 26f);
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
            viewport.AddComponent<ScrollWheelBlocker>();

            productionSettingsRoot.SetActive(false);
        }

        private void KeepProductionSettingsPanelOnScreen()
        {
            if (productionSettingsRoot == null)
            {
                return;
            }

            RectTransform panelRect = productionSettingsRoot.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            const float sideSafeArea = 18f;
            const float topSafeArea = 86f;
            const float bottomMargin = 24f;
            const float leftSafeArea = 18f;

            float width = Mathf.Clamp(760f, 620f, Mathf.Max(620f, Screen.width - sideSafeArea - leftSafeArea));
            float height = Mathf.Clamp(560f, 360f, Mathf.Max(360f, Screen.height - topSafeArea - bottomMargin));
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(width, height);
            panelRect.anchoredPosition = new Vector2(0f, bottomMargin);
        }

        private void UpdateProductionSettingsPanel(bool force = false)
        {
            if (productionSettingsRoot == null || !productionSettingsRoot.activeSelf || productionSettingsContent == null)
            {
                return;
            }

            Storage storage = productionSettingsStorage;
            if (storage == null)
            {
                CloseProductionSettingsPanel();
                return;
            }

            ComplexFabricator fabricator = storage.GetComponent<ComplexFabricator>();
            string signature = BuildProductionSettingsSignature(storage, fabricator);
            if (!force && signature == productionSettingsSignature)
            {
                return;
            }

            productionSettingsSignature = signature;
            ClearProductionSettingsContent();
            SetProductionSettingsTitle(storage);
            KeepProductionSettingsPanelOnScreen();
            StorageNetworkMaterialRequester requester = storage.GetComponent<StorageNetworkMaterialRequester>();
            StorageNetworkStorageConnector connector = GetStorageConnector(storage);
            AddProductionOverviewCard(storage, fabricator, requester, connector);
            if (requester != null)
            {
                AddAutomationCards(storage, requester);
            }
            else if (connector != null)
            {
                AddStorageOutputCard(storage, connector);
            }
            AddInventoryCard(storage, fabricator);

            LayoutRebuilder.MarkLayoutForRebuild(productionSettingsContent);
        }

        private void ClearProductionSettingsContent()
        {
            for (int i = productionSettingsContent.childCount - 1; i >= 0; i--)
            {
                Destroy(productionSettingsContent.GetChild(i).gameObject);
            }
        }

        private static string BuildProductionSettingsSignature(Storage storage, ComplexFabricator fabricator)
        {
            StorageNetworkMaterialRequester requester = storage != null ? storage.GetComponent<StorageNetworkMaterialRequester>() : null;
            StorageNetworkStorageConnector connector = storage != null ? storage.GetComponent<StorageNetworkStorageConnector>() : null;
            string itemSignature = string.Join("|", GetProductionStorages(storage, fabricator)
                .SelectMany(itemStorage => itemStorage.items.Where(item => item != null))
                .GroupBy(GetStoredItemKey)
                .OrderBy(group => group.Key)
                .Select(group => string.Format(
                    "{0}:{1:0.###}",
                    group.Key,
                    group.Sum(GetStoredItemMass))));

            return string.Join(
                "~",
                storage != null ? storage.GetInstanceID().ToString() : "null",
                storage != null ? storage.MassStored().ToString("0.###") : "0",
                storage != null ? storage.Capacity().ToString("0.###") : "0",
                fabricator != null && fabricator.CurrentWorkingOrder != null ? fabricator.CurrentWorkingOrder.id : "none",
                fabricator != null ? Mathf.RoundToInt(Mathf.Clamp01(fabricator.OrderProgress) * 100f).ToString() : "0",
                fabricator != null && fabricator.WaitingForWorker ? "worker" : "run",
                requester != null && requester.RequestEnabled ? "req1" : "req0",
                requester != null ? requester.Mode.ToString() : "0",
                requester != null ? requester.SourceStorageInstanceId.ToString() : "0",
                requester != null && requester.LimitEnabled ? "lim1" : "lim0",
                requester != null ? requester.LimitKg.ToString("0.###") : "0",
                requester != null ? requester.RequestedKg.ToString("0.###") : "0",
                requester != null && requester.OutputStoreEnabled ? "out1" : "out0",
                requester != null ? requester.OutputStoreModeValue.ToString() : "0",
                requester != null ? requester.OutputStorageInstanceId.ToString() : "0",
                requester != null ? requester.LastStatus : string.Empty,
                requester != null ? requester.LastOutputStatus : string.Empty,
                connector != null && connector.OutputStoreEnabled ? "conn1" : "conn0",
                connector != null ? connector.LastOutputStatus : string.Empty,
                itemSignature);
        }

        private void SetProductionSettingsTitle(Storage storage)
        {
            TextMeshProUGUI title = productionSettingsRoot.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.name == "ProductionSettingsTitle");
            if (title != null)
            {
                title.text = string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_DETAILS),
                    GameUtil.GetFormattedMass(storage.MassStored()),
                    GameUtil.GetFormattedMass(storage.Capacity()),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, storage.RemainingCapacity())));
            }
        }

        private void AddProductionOverviewCard(Storage storage, ComplexFabricator fabricator, StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector)
        {
            GameObject card = CreateProductionCard("OverviewCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_TITLE), 116f);
            TextMeshProUGUI title = CreateText("BuildingName", card.transform, storage.GetProperName(), 16, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.14f, 0.15f, 0.14f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            GameObject metrics = new GameObject("Metrics");
            metrics.transform.SetParent(card.transform, false);
            metrics.AddComponent<RectTransform>();
            metrics.AddComponent<LayoutElement>().preferredHeight = 54f;
            HorizontalLayoutGroup layout = metrics.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateMetricTile(metrics.transform, "储存", string.Format("{0} / {1}", GameUtil.GetFormattedMass(storage.MassStored()), GameUtil.GetFormattedMass(storage.Capacity())), new Color(0.35f, 0.40f, 0.43f, 1f));
            CreateMetricTile(metrics.transform, "运行", GetProductionStateText(fabricator), GetProductionStateColor(fabricator));
            CreateMetricTile(metrics.transform, "配方", GetCurrentRecipeText(fabricator), new Color(0.39f, 0.42f, 0.45f, 1f));
            CreateMetricTile(metrics.transform, "网络", GetNetworkStateText(requester, connector), requester != null && requester.RequestEnabled || connector != null && connector.OutputStoreEnabled ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.50f, 0.42f, 0.34f, 1f));
        }

        private void AddAutomationCards(Storage storage, StorageNetworkMaterialRequester requester)
        {
            GameObject grid = new GameObject("AutomationGrid");
            grid.transform.SetParent(productionSettingsContent, false);
            grid.AddComponent<RectTransform>();
            bool compact = productionSettingsRoot != null && productionSettingsRoot.GetComponent<RectTransform>().rect.width < 620f;
            LayoutElement gridLayout = grid.AddComponent<LayoutElement>();
            gridLayout.minHeight = compact ? 360f : 184f;
            grid.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (compact)
            {
                VerticalLayoutGroup layout = grid.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }
            else
            {
                HorizontalLayoutGroup layout = grid.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }

            AddMaterialAutomationCard(grid.transform, storage, requester);
            AddOutputAutomationCard(grid.transform, storage, requester);
        }

        private void AddMaterialAutomationCard(Transform parent, Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            GameObject card = CreateProductionCard(parent, "MaterialCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_TITLE), 0f);
            ApplyEqualAutomationCardLayout(card);
            CreateStatusStrip(card.transform, requester.RequestEnabled ? "已开启" : "已关闭", requester.RequestEnabled ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.52f, 0.38f, 0.30f, 1f));
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_ENABLED), requester.RequestEnabled ? "关闭" : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                requester.RequestEnabled = !requester.RequestEnabled;
                UpdateProductionSettingsPanel(true);
            }, requester.RequestEnabled ? KleiPinkStyle() : KleiBlueStyle());
            CreateProductionActionRow(card.transform, "来源策略", GetMaterialRequestModeName(requester), () => ShowMaterialSourcePicker(ownerStorage, requester));
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED), requester.LimitEnabled ? string.Format("{0} / {1}", GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())), GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))) : "不限额", () =>
            {
                if (requester.LimitEnabled)
                {
                    ShowMaterialRequestLimitDialog(requester);
                }
                else
                {
                    requester.LimitEnabled = true;
                    UpdateProductionSettingsPanel(true);
                }
            });
            if (!string.IsNullOrEmpty(requester.LastStatus))
            {
                CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), requester.LastStatus));
            }
        }

        private void AddOutputAutomationCard(Transform parent, Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            GameObject card = CreateProductionCard(parent, "OutputCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TITLE), 0f);
            ApplyEqualAutomationCardLayout(card);
            CreateStatusStrip(card.transform, requester.OutputStoreEnabled ? "自动入网" : "手动取出", requester.OutputStoreEnabled ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.48f, 0.45f, 0.36f, 1f));
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_ENABLED), requester.OutputStoreEnabled ? "关闭" : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                requester.OutputStoreEnabled = !requester.OutputStoreEnabled;
                UpdateProductionSettingsPanel(true);
            }, requester.OutputStoreEnabled ? KleiPinkStyle() : KleiBlueStyle());
            CreateProductionActionRow(card.transform, "存放策略", GetOutputStoreModeName(requester), () => ShowOutputStorePicker(ownerStorage, requester));
            CreateFinePrint(card.transform, requester.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC) : "成品保留在建筑输出栏，不自动转移。");
            if (requester.OutputStoreEnabled && !string.IsNullOrEmpty(requester.LastOutputStatus))
            {
                CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_STATUS), requester.LastOutputStatus));
            }
        }

        private void AddStorageOutputCard(Storage ownerStorage, StorageNetworkStorageConnector connector)
        {
            GameObject card = CreateProductionCard("StorageOutputCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_TITLE), 132f);
            CreateStatusStrip(card.transform, connector.OutputStoreEnabled ? "自动入网" : "手动取出", connector.OutputStoreEnabled ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.48f, 0.45f, 0.36f, 1f));
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_ENABLED), connector.OutputStoreEnabled ? "关闭" : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                connector.OutputStoreEnabled = !connector.OutputStoreEnabled;
                UpdateProductionSettingsPanel(true);
            }, connector.OutputStoreEnabled ? KleiPinkStyle() : KleiBlueStyle());
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_DESC));
            if (connector.OutputStoreEnabled && !string.IsNullOrEmpty(connector.LastOutputStatus))
            {
                CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_STATUS), connector.LastOutputStatus));
            }
        }

        private static void ApplyEqualAutomationCardLayout(GameObject card)
        {
            LayoutElement layout = card.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = card.AddComponent<LayoutElement>();
            }

            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 1f;
            layout.minHeight = 0f;
            layout.preferredHeight = -1f;

            if (card.GetComponent<ContentSizeFitter>() == null)
            {
                card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void AddInventoryCard(Storage storage, ComplexFabricator fabricator)
        {
            List<GameObject> items = GetProductionStorages(storage, fabricator)
                .SelectMany(itemStorage => itemStorage.items.Where(item => item != null))
                .ToList();
            GameObject card = CreateProductionCard("InventoryCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_CONTENT_TITLE), Mathf.Clamp(52f + Mathf.Max(1, items.GroupBy(GetStoredItemKey).Count()) * 26f, 82f, 150f));
            if (items.Count == 0)
            {
                CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT));
                return;
            }

            foreach (IGrouping<string, GameObject> group in items.GroupBy(GetStoredItemKey).OrderBy(group => GetStoredItemName(group.FirstOrDefault())))
            {
                float mass = group.Sum(GetStoredItemMass);
                CreateProductionSettingsItemRow(
                    card.transform,
                    GetStoredItemName(group.FirstOrDefault()),
                    GameUtil.GetFormattedMass(mass),
                    group.FirstOrDefault());
            }
        }

        private GameObject CreateProductionCard(string name, string title, float preferredHeight)
        {
            return CreateProductionCard(productionSettingsContent, name, title, preferredHeight);
        }

        private GameObject CreateProductionCard(Transform parent, string name, string title, float preferredHeight)
        {
            GameObject card = CreatePlainImage(name, parent, new Color(0.82f, 0.81f, 0.75f, 1f));
            LayoutElement layoutElement = card.AddComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                layoutElement.preferredHeight = preferredHeight;
            }

            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI heading = CreateText("CardTitle", card.transform, title, 12, TextAlignmentOptions.MidlineLeft);
            heading.color = new Color(0.18f, 0.19f, 0.18f, 1f);
            heading.fontStyle = FontStyles.Bold;
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            return card;
        }

        private void CreateMetricTile(Transform parent, string label, string value, Color accent)
        {
            GameObject tile = CreatePlainImage("MetricTile", parent, new Color(0.72f, 0.72f, 0.66f, 1f));
            tile.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup layout = tile.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 4, 4);
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI labelText = CreateText("Label", tile.transform, label, 9, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.30f, 0.32f, 0.31f, 1f);
            labelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            TextMeshProUGUI valueText = CreateText("Value", tile.transform, value, 11, TextAlignmentOptions.MidlineLeft);
            valueText.color = accent;
            valueText.fontStyle = FontStyles.Bold;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        }

        private void CreateStatusStrip(Transform parent, string text, Color color)
        {
            GameObject strip = CreatePlainImage("StatusStrip", parent, color);
            strip.AddComponent<LayoutElement>().preferredHeight = 24f;
            TextMeshProUGUI label = CreateText("Status", strip.transform, text, 11, TextAlignmentOptions.Center);
            label.color = new Color(0.96f, 0.96f, 0.90f, 1f);
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform(), 4f, 0f);
        }

        private void CreateProductionActionRow(Transform parent, string label, string value, System.Action onClick, ColorStyleSetting buttonStyle = null)
        {
            GameObject row = CreatePlainImage("ActionRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 5, 3, 3);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 10, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject button = CreateStyledButton("Action", row.transform, value, onClick, buttonStyle ?? KleiBlueStyle());
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.minWidth = 126f;
            buttonLayout.flexibleWidth = 0f;
            buttonLayout.preferredHeight = 24f;
        }

        private void CreateFinePrint(Transform parent, string text)
        {
            TextMeshProUGUI label = CreateText("FinePrint", parent, text, 10, TextAlignmentOptions.TopLeft);
            label.color = new Color(0.34f, 0.35f, 0.33f, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 18f;
            layout.preferredHeight = -1f;
            label.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static string GetProductionStateText(ComplexFabricator fabricator)
        {
            if (fabricator == null || fabricator.CurrentWorkingOrder == null)
            {
                return "待机";
            }

            return fabricator.WaitingForWorker ? "等人" : "制作中";
        }

        private static Color GetProductionStateColor(ComplexFabricator fabricator)
        {
            if (fabricator == null || fabricator.CurrentWorkingOrder == null)
            {
                return new Color(0.38f, 0.42f, 0.36f, 1f);
            }

            return fabricator.WaitingForWorker ? new Color(0.64f, 0.42f, 0.24f, 1f) : new Color(0.26f, 0.52f, 0.34f, 1f);
        }

        private static string GetCurrentRecipeText(ComplexFabricator fabricator)
        {
            return fabricator != null && fabricator.CurrentWorkingOrder != null
                ? GetRecipeDisplayName(fabricator.CurrentWorkingOrder)
                : "无";
        }

        private static string GetNetworkStateText(StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector)
        {
            if (requester != null)
            {
                return requester.RequestEnabled ? "请求开" : "请求关";
            }

            if (connector != null)
            {
                return connector.OutputStoreEnabled ? "入网开" : "入网关";
            }

            return "无组件";
        }

        private void AddProductionSettingsInfo(Storage storage, ComplexFabricator fabricator)
        {
            AddProductionSettingsText(storage.GetProperName(), 16, FontStyles.Bold, 34f);
            AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_TITLE), 12, FontStyles.Bold, 24f);
            AddProductionSettingsStatus(fabricator);
        }

        private void AddMaterialRequestSettings(Storage storage)
        {
            StorageNetworkMaterialRequester requester = storage.GetComponent<StorageNetworkMaterialRequester>();
            if (requester == null)
            {
                return;
            }

            AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_TITLE), 12, FontStyles.Bold, 24f);
            CreateProductionToggleRow(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_ENABLED),
                requester.RequestEnabled,
                value =>
                {
                    requester.RequestEnabled = value;
                    UpdateProductionSettingsPanel();
                });

            CreateProductionOptionFoldout(
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE), GetMaterialRequestModeName(requester)),
                row => ShowMaterialSourcePicker(storage, requester));

            CreateProductionToggleRow(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED),
                requester.LimitEnabled,
                value =>
                {
                    requester.LimitEnabled = value;
                    UpdateProductionSettingsPanel();
                });

            if (requester.LimitEnabled)
            {
                CreateProductionButtonRow(
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SET_LIMIT),
                    () => ShowMaterialRequestLimitDialog(requester));
                CreateProductionButtonRow(
                    string.Empty,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_RESET),
                    () =>
                    {
                        requester.ResetRequestedAmount();
                        UpdateProductionSettingsPanel();
                    });
            }

            if (!string.IsNullOrEmpty(requester.LastStatus))
            {
                AddProductionSettingsText(
                    ColorizeMaterialRequestStatus(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), requester.LastStatus)),
                    11,
                    FontStyles.Normal,
                    22f);
            }

            AddOutputStoreSettings(storage, requester);
            AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_CONTENT_TITLE), 12, FontStyles.Bold, 24f);
        }

        private void AddOutputStoreSettings(Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TITLE), 12, FontStyles.Bold, 24f);
            CreateProductionToggleRow(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_ENABLED),
                requester.OutputStoreEnabled,
                value =>
                {
                    requester.OutputStoreEnabled = value;
                    UpdateProductionSettingsPanel();
                });

            if (!requester.OutputStoreEnabled)
            {
                return;
            }

            CreateProductionOptionFoldout(
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE), GetOutputStoreModeName(requester)),
                row => ShowOutputStorePicker(ownerStorage, requester));
        }

        private void AddProductionSettingsStatus(ComplexFabricator fabricator)
        {
            if (fabricator == null)
            {
                AddProductionSettingsText(ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_NO_RECIPE), "#6b6b63"), 12, FontStyles.Normal, 24f);
                return;
            }

            ComplexRecipe currentRecipe = fabricator.CurrentWorkingOrder;
            if (currentRecipe == null)
            {
                AddProductionSettingsText(ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_IDLE), "#5f665d"), 12, FontStyles.Normal, 22f);
                AddProductionSettingsText(ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_NO_RECIPE), "#6b6b63"), 12, FontStyles.Normal, 22f);
                return;
            }

            string statusText = fabricator.WaitingForWorker
                ? ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_WAITING_WORKER), "#b5753c")
                : ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_CRAFTING), "#3f7f4a");
            AddProductionSettingsText(statusText, 12, FontStyles.Normal, 22f);
            AddProductionSettingsText(
                ColorText(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_CURRENT_RECIPE), GetRecipeDisplayName(currentRecipe)), "#38485d"),
                12,
                FontStyles.Normal,
                22f);
            AddProductionSettingsText(
                ColorText(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_PROGRESS), Mathf.RoundToInt(Mathf.Clamp01(fabricator.OrderProgress) * 100f)), "#5a5f66"),
                12,
                FontStyles.Normal,
                22f);
        }

        private void AddProductionSettingsItems(Storage storage, ComplexFabricator fabricator)
        {
            List<GameObject> items = GetProductionStorages(storage, fabricator)
                .SelectMany(itemStorage => itemStorage.items.Where(item => item != null))
                .ToList();
            if (items.Count == 0)
            {
                AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT), 12, FontStyles.Normal, 26f);
                return;
            }

            foreach (IGrouping<string, GameObject> group in items.GroupBy(GetStoredItemKey).OrderBy(group => GetStoredItemName(group.FirstOrDefault())))
            {
                float mass = group.Sum(GetStoredItemMass);
                CreateProductionSettingsItemRow(
                    GetStoredItemName(group.FirstOrDefault()),
                    GameUtil.GetFormattedMass(mass),
                    group.FirstOrDefault());
            }
        }

        private void AddProductionSettingsText(string text, int size, FontStyles style, float height)
        {
            TextMeshProUGUI label = CreateText("ProductionSettingsText", productionSettingsContent, text, size, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            label.fontStyle = style;
            label.richText = true;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static string ColorizeMaterialRequestStatus(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (text.Contains("缺少") || text.Contains("没有可用") || text.Contains("Missing") || text.Contains("no source"))
            {
                return ColorText(text, "#a64c3c");
            }

            if (text.Contains("限额") || text.Contains("limit"))
            {
                return ColorText(text, "#b5753c");
            }

            if (text.Contains("已请求") || text.Contains("已满足") || text.Contains("requested") || text.Contains("satisfied"))
            {
                return ColorText(text, "#3f7f4a");
            }

            return ColorText(text, "#5a5f66");
        }

        private static string ColorText(string text, string color)
        {
            return string.Format("<color={0}>{1}</color>", color, text);
        }

        private void CreateProductionToggleRow(string label, bool value, System.Action<bool> onChanged)
        {
            CreateProductionButtonRow(
                label,
                value ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON) : string.Empty,
                () => onChanged?.Invoke(!value),
                value ? KleiPinkStyle() : KleiBlueStyle());
        }

        private void CreateProductionButtonRow(string label, string buttonText, System.Action onClick, ColorStyleSetting buttonStyle = null)
        {
            GameObject row = CreatePlainImage("ProductionSettingRow", productionSettingsContent, new Color(0.86f, 0.85f, 0.80f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 28f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 11, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject button = CreateStyledButton("Button", row.transform, buttonText, onClick, buttonStyle ?? KleiBlueStyle());
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 168f;
            buttonLayout.preferredHeight = 22f;
        }

        private void CreateProductionOptionFoldout(string label, System.Action<GameObject> onClick)
        {
            GameObject row = CreateStyledButton("ProductionOptionDropdown", productionSettingsContent, string.Empty, null, KleiBlueStyle());
            KButton button = row.GetComponent<KButton>();
            if (button != null)
            {
                button.onClick += () => onClick?.Invoke(row);
            }
            row.AddComponent<LayoutElement>().preferredHeight = 30f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            CreateFoldoutIcon(row.transform, false);
            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 11, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void ShowMaterialSourcePicker(Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_AUTO_DESC),
                requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork,
                () =>
                {
                    requester.UseAutomaticMaterialSource();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                })
            };

            foreach (Storage target in GetNetworkStorageTargets(ownerStorage))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), captured.GetProperName()),
                    FormatStorageOptionDetails(captured),
                    requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage && requester.ResolveSourceStorage() == captured,
                    () =>
                    {
                        requester.SetSourceStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE), options);
        }

        private void ShowOutputStorePicker(Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC),
                requester.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork,
                () =>
                {
                    requester.UseAutomaticOutputStorage();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                })
            };

            foreach (Storage target in GetNetworkStorageTargets(ownerStorage))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), captured.GetProperName()),
                    FormatStorageOptionDetails(captured),
                    requester.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage && requester.ResolveOutputStorage() == captured,
                    () =>
                    {
                        requester.SetOutputStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC), options);
        }

        private void ShowProductionPicker(string title, List<ProductionPickerOption> options)
        {
            CloseProductionPicker();
            if (productionSettingsRoot == null || options == null || options.Count == 0)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("ProductionPicker", productionSettingsRoot.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 14f, 14f, 76f, 74f);

            GameObject header = CreatePlainImage("PickerHeader", productionPickerRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            RectTransform headerRect = header.GetComponent<RectTransform>();
            SetTopStretch(headerRect, 8f, 8f, 8f, 34f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("PickerTitle", header.transform, title, 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateGameButton("PickerClose", header.transform, "X", CloseProductionPicker);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject viewport = CreatePlainImage("PickerViewport", productionPickerRoot.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("PickerContent");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            ConfigureSmoothVerticalScroll(scrollRect, 24f);

            foreach (ProductionPickerOption option in options)
            {
                CreateStorageOptionRow(content.transform, option.Title, option.Details, option.Selected, option.OnClick);
            }
        }

        private void CloseProductionPicker()
        {
            if (productionPickerRoot != null)
            {
                Destroy(productionPickerRoot);
                productionPickerRoot = null;
            }
        }

        private void CreateStorageOptionRow(Transform parent, string title, string details, bool selected, System.Action onClick)
        {
            GameObject row = CreatePlainImage("StorageOptionRow", parent, selected ? new Color(0.56f, 0.31f, 0.45f, 1f) : new Color(0.76f, 0.76f, 0.70f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 42f;
            KButton button = row.AddComponent<KButton>();
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 3, 3);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI titleText = CreateText("Title", row.transform, title, 11, TextAlignmentOptions.MidlineLeft);
            titleText.color = selected ? new Color(0.98f, 0.96f, 0.90f, 1f) : new Color(0.16f, 0.17f, 0.16f, 1f);
            titleText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI detailText = CreateText("Details", row.transform, details, 9, TextAlignmentOptions.MidlineLeft);
            detailText.color = selected ? new Color(0.88f, 0.84f, 0.78f, 1f) : new Color(0.34f, 0.35f, 0.33f, 1f);
            detailText.textWrappingMode = TextWrappingModes.NoWrap;
            detailText.overflowMode = TextOverflowModes.Ellipsis;
            detailText.gameObject.AddComponent<LayoutElement>().preferredWidth = 170f;
        }

        private static string GetMaterialRequestModeName(StorageNetworkMaterialRequester requester)
        {
            if (requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = requester.ResolveSourceStorage();
                return source != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        private static string GetOutputStoreModeName(StorageNetworkMaterialRequester requester)
        {
            if (requester.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = requester.ResolveOutputStorage();
                return target != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private static List<Storage> GetNetworkStorageTargets(Storage ownerStorage)
        {
            return StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage);
        }

        private static StorageNetworkStorageConnector GetStorageConnector(Storage storage)
        {
            if (storage == null || !StorageNetworkStorageRules.HasSettingsButtonTag(storage))
            {
                return null;
            }

            return storage.GetComponent<StorageNetworkStorageConnector>() ?? storage.gameObject.AddOrGet<StorageNetworkStorageConnector>();
        }

        private static string FormatStorageOptionDetails(Storage storage)
        {
            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET_DETAILS),
                GameUtil.GetFormattedMass(storage.MassStored()),
                GameUtil.GetFormattedMass(storage.Capacity()),
                GameUtil.GetFormattedMass(Mathf.Max(0f, storage.RemainingCapacity())));
        }

        private void ShowMaterialRequestSourceSelection(Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            List<Storage> targets = StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage);

            if (targets.Count == 0)
            {
                ShowMessageDialog(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_TRANSFER_TARGET));
                return;
            }

            ShowTargetSelectionDialog(ownerStorage, targets, requester.ResolveSourceStorage(), target =>
            {
                requester.SetSourceStorage(target);
                CloseModal();
                UpdateProductionSettingsPanel();
            });
        }

        private void ShowMaterialRequestLimitDialog(StorageNetworkMaterialRequester requester)
        {
            ShowAmountDialog(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SET_LIMIT),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED),
                string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))),
                Mathf.Max(1f, requester.LimitKg <= 0f ? Config.Instance.DefaultMaterialRequestLimitKg : requester.LimitKg * 10f),
                amount =>
                {
                    requester.LimitKg = amount;
                    UpdateProductionSettingsPanel();
                });
        }

        private void CreateProductionSettingsItemRow(string itemName, string formattedMass, GameObject representative)
        {
            CreateProductionSettingsItemRow(productionSettingsContent, itemName, formattedMass, representative);
        }

        private void CreateProductionSettingsItemRow(Transform parent, string itemName, string formattedMass, GameObject representative)
        {
            GameObject row = CreatePlainImage("ProductionSettingsItemRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 24f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 1, 1);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>();
            iconObject.AddComponent<LayoutElement>().preferredWidth = 20f;
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            SetStoredItemIcon(icon, representative);

            TextMeshProUGUI name = CreateText("Name", row.transform, itemName, 11, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI mass = CreateText("Mass", row.transform, formattedMass, 11, TextAlignmentOptions.MidlineRight);
            mass.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            mass.textWrappingMode = TextWrappingModes.NoWrap;
            mass.gameObject.AddComponent<LayoutElement>().preferredWidth = 92f;
        }

        private static IEnumerable<Storage> GetProductionStorages(Storage storage, ComplexFabricator fabricator)
        {
            HashSet<Storage> storages = new HashSet<Storage>();
            AddProductionStorage(storages, storage);
            if (fabricator != null)
            {
                AddProductionStorage(storages, fabricator.inStorage);
                AddProductionStorage(storages, fabricator.buildStorage);
                AddProductionStorage(storages, fabricator.outStorage);
            }

            return storages;
        }

        private static void AddProductionStorage(HashSet<Storage> storages, Storage storage)
        {
            if (storage != null)
            {
                storages.Add(storage);
            }
        }

        private static string GetRecipeDisplayName(ComplexRecipe recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            return recipe.GetUIName(false);
        }

        private sealed class ProductionPickerOption
        {
            public ProductionPickerOption(string title, string details, bool selected, System.Action onClick)
            {
                Title = title;
                Details = details;
                Selected = selected;
                OnClick = onClick;
            }

            public string Title { get; }

            public string Details { get; }

            public bool Selected { get; }

            public System.Action OnClick { get; }
        }
    }
}
