using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private void ShowProductionSettingsPanel(Storage storage)
        {
            bool sameStorage = productionSettingsStorage == storage;
            productionSettingsStorage = storage;
            productionSettingsMinion = null;
            geyserSettingsGeyser = null;
            geyserSettingsSignature = null;
            EnsureProductionSettingsPanel();
            productionSettingsRoot.SetActive(true);
            KeepProductionSettingsPanelOnScreen();
            UpdateProductionSettingsPanel(!sameStorage);
        }

        private void ShowMinionSettingsPanel(MinionIdentity minion, Storage storage)
        {
            if (minion == null || storage == null)
            {
                return;
            }

            bool sameMinion = productionSettingsMinion == minion;
            productionSettingsMinion = minion;
            productionSettingsStorage = storage;
            geyserSettingsGeyser = null;
            geyserSettingsSignature = null;
            EnsureProductionSettingsPanel();
            productionSettingsRoot.SetActive(true);
            KeepProductionSettingsPanelOnScreen();
            UpdateProductionSettingsPanel(!sameMinion);
        }

        private void CloseProductionSettingsPanel()
        {
            CloseProductionPicker();
            if (productionSettingsRoot != null)
            {
                productionSettingsRoot.SetActive(false);
            }

            productionSettingsMinion = null;
            geyserSettingsGeyser = null;
            geyserSettingsSignature = null;
        }

        private void EnsureProductionSettingsPanel()
        {
            if (productionSettingsRoot != null)
            {
                return;
            }

            SettingsWindowParts window = CreateDraggableSettingsWindow(
                "ProductionSettingsPanel",
                "ProductionSettingsTitle",
                CloseProductionSettingsPanel,
                withScrollbar: true,
                layoutKey: "productionSettings");
            productionSettingsRoot = window.Root;
            productionSettingsContent = window.Content;
        }

        private void KeepProductionSettingsPanelOnScreen()
        {
            productionSettingsPositionInitialized = KeepDraggableSettingsWindowOnScreen(productionSettingsRoot, productionSettingsPositionInitialized, "productionSettings");
        }

        private void UpdateProductionSettingsPanel(bool force = false)
        {
            if (productionSettingsRoot == null || !productionSettingsRoot.activeSelf || productionSettingsContent == null)
            {
                return;
            }

            Storage storage = productionSettingsStorage;
            if (geyserSettingsGeyser != null)
            {
                return;
            }

            if (storage == null)
            {
                CloseProductionSettingsPanel();
                return;
            }

            if (productionSettingsMinion != null)
            {
                string minionSignature = StorageNetworkProductionSettingsSignatureBuilder.BuildMinion(productionSettingsMinion, storage);
                if (!force && minionSignature == productionSettingsSignature)
                {
                    SetMinionSettingsTitle(productionSettingsMinion, storage);
                    return;
                }

                productionSettingsSignature = minionSignature;
                ClearProductionSettingsContent();
                SetMinionSettingsTitle(productionSettingsMinion, storage);
                KeepProductionSettingsPanelOnScreen();
                AddMinionMaterialRequestCard(productionSettingsMinion, storage);
                AddInventoryCard(storage, null);
                LayoutRebuilder.MarkLayoutForRebuild(productionSettingsContent);
                return;
            }

            ComplexFabricator fabricator = storage.GetComponent<ComplexFabricator>();
            string signature = StorageNetworkProductionSettingsSignatureBuilder.BuildProduction(storage, fabricator);
            if (!force && signature == productionSettingsSignature)
            {
                UpdateProductionSettingsLive(storage, fabricator);
                return;
            }

            productionSettingsSignature = signature;
            ClearProductionSettingsContent();
            SetProductionSettingsTitle(storage);
            KeepProductionSettingsPanelOnScreen();
            StorageNetworkMaterialRequester requester = storage.GetComponent<StorageNetworkMaterialRequester>();
            StorageNetworkStorageConnector connector = StorageNetworkStorageConnectorResolver.GetOrCreateForSettingsStorage(storage);
            StorageNetworkEnergyGeneratorRequester energyRequester = storage.GetComponent<StorageNetworkEnergyGeneratorRequester>();
            AddProductionOverviewCard(storage, fabricator, requester, connector, energyRequester);
            if (requester != null)
            {
                AddAutomationCards(storage, requester);
            }
            else if (connector != null)
            {
                AddStorageOutputCard(storage, connector);
            }
            else if (energyRequester != null)
            {
                AddEnergyGeneratorMaterialCard(storage, energyRequester);
            }
            AddInventoryCard(storage, fabricator);
            UpdateProductionSettingsLive(storage, fabricator);

            LayoutRebuilder.MarkLayoutForRebuild(productionSettingsContent);
        }

        private void ClearProductionSettingsContent()
        {
            for (int i = productionSettingsContent.childCount - 1; i >= 0; i--)
            {
                Destroy(productionSettingsContent.GetChild(i).gameObject);
            }

            productionOverviewView = null;
            productionInventoryView = null;
            productionAutomationView = null;
        }

        private void UpdateProductionSettingsLive(Storage storage, ComplexFabricator fabricator)
        {
            if (storage == null)
            {
                return;
            }

            SetProductionSettingsTitle(storage);
            StorageNetworkMaterialRequester requester = storage.GetComponent<StorageNetworkMaterialRequester>();
            StorageNetworkStorageConnector connector = StorageNetworkStorageConnectorResolver.GetOrCreateForSettingsStorage(storage);
            StorageNetworkEnergyGeneratorRequester energyRequester = storage.GetComponent<StorageNetworkEnergyGeneratorRequester>();
            UpdateProductionOverviewCard(storage, fabricator, requester, connector, energyRequester);
            UpdateProductionAutomationCards(requester, connector, energyRequester);
            UpdateProductionInventoryCard(storage, fabricator);
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

        private void SetMinionSettingsTitle(MinionIdentity minion, Storage storage)
        {
            TextMeshProUGUI title = productionSettingsRoot.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.name == "ProductionSettingsTitle");
            if (title != null)
            {
                title.text = string.Format(
                    "{0}\n{1}",
                    minion != null ? minion.GetProperName() : storage.GetProperName(),
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_DETAILS),
                        GameUtil.GetFormattedMass(storage.MassStored()),
                        GameUtil.GetFormattedMass(storage.Capacity()),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, storage.RemainingCapacity()))));
            }
        }

        private void AddMinionMaterialRequestCard(MinionIdentity minion, Storage storage)
        {
            GameObject card = CreateProductionCard("MinionMaterialRequestCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_TITLE), 0f);
            MakeProductionCardAutoHeight(card, 126f);
            bool enabled = Config.Instance.IsMinionAllowedRequestMaterialsFromNetwork(minion);
            CreateEnabledStatusStrip(card.transform, enabled);
            CreateToggleActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MINION_MATERIAL_REQUEST_ENABLED),
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    Config.Instance.SetMinionAllowedRequestMaterialsFromNetwork(minion, !enabled);
                    Config.Save();
                    UpdateProductionSettingsPanel(true);
                },
                enabled);
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MINION_MATERIAL_REQUEST_DESC));
        }

        private void AddProductionOverviewCard(Storage storage, ComplexFabricator fabricator, StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            GameObject card = CreateProductionCard("OverviewCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_TITLE), 132f);
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
            layout.childForceExpandHeight = false;

            productionOverviewView = new ProductionOverviewCardView
            {
                BuildingName = title,
                StorageValue = CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_STORAGE), string.Empty, new Color(0.35f, 0.40f, 0.43f, 1f)),
                StateValue = CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_RUNNING), string.Empty, GetProductionStateColor(fabricator)),
                RecipeValue = CreateMetricTile(metrics.transform, Get(GetProductionInputMetricLabel(fabricator, energyRequester)), string.Empty, new Color(0.39f, 0.42f, 0.45f, 1f)),
                NetworkValue = CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_NETWORK), string.Empty, IsNetworkAutomationEnabled(storage, requester, connector, energyRequester) ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.50f, 0.42f, 0.34f, 1f))
            };
        }

        private void UpdateProductionOverviewCard(Storage storage, ComplexFabricator fabricator, StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            if (productionOverviewView == null)
            {
                return;
            }

            SetTextIfChanged(productionOverviewView.BuildingName, storage.GetProperName());
            SetTextIfChanged(productionOverviewView.StorageValue, string.Format("{0} / {1}", GameUtil.GetFormattedMass(storage.MassStored()), GameUtil.GetFormattedMass(storage.Capacity())));
            SetTextIfChanged(productionOverviewView.StateValue, GetProductionStateText(fabricator));
            productionOverviewView.StateValue.color = GetProductionStateColor(fabricator);
            SetTextIfChanged(productionOverviewView.RecipeValue, energyRequester != null && fabricator == null ? GetEnergyGeneratorFuelText(storage.GetComponent<EnergyGenerator>()) : GetCurrentRecipeText(fabricator));
            SetTextIfChanged(productionOverviewView.NetworkValue, GetNetworkStateText(storage, requester, connector, energyRequester));
            productionOverviewView.NetworkValue.color = IsNetworkAutomationEnabled(storage, requester, connector, energyRequester)
                ? new Color(0.28f, 0.48f, 0.34f, 1f)
                : new Color(0.50f, 0.42f, 0.34f, 1f);
        }

        private void AddAutomationCards(Storage storage, StorageNetworkMaterialRequester requester)
        {
            GameObject grid = new GameObject("AutomationGrid");
            grid.transform.SetParent(productionSettingsContent, false);
            grid.AddComponent<RectTransform>();
            bool compact = productionSettingsRoot != null && productionSettingsRoot.GetComponent<RectTransform>().rect.width < 620f;
            LayoutElement gridLayout = grid.AddComponent<LayoutElement>();
            float gridHeight = Mathf.Max(GetMaterialAutomationCardHeight(requester), GetOutputAutomationCardHeight(requester));
            gridLayout.minHeight = compact ? gridHeight * 2f + 8f : gridHeight;
            gridLayout.preferredHeight = -1f;

            if (compact)
            {
                VerticalLayoutGroup layout = grid.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
            else
            {
                HorizontalLayoutGroup layout = grid.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            AddMaterialAutomationCard(grid.transform, storage, requester);
            AddOutputAutomationCard(grid.transform, storage, requester);
        }

        private void AddMaterialAutomationCard(Transform parent, Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            GameObject card = CreateProductionCard(parent, "MaterialCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_TITLE), 0f);
            ApplyAutomationCardLayout(card, GetMaterialAutomationCardHeight(requester));
            CreateEnabledStatusStrip(card.transform, requester.RequestEnabled);
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_ENABLED), requester.RequestEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                requester.RequestEnabled = !requester.RequestEnabled;
                UpdateProductionSettingsPanel(true);
            }, requester.RequestEnabled);
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SOURCE_POLICY), StorageNetworkProductionSettingsText.GetMaterialRequestModeName(requester), () => ShowMaterialSourcePicker(ownerStorage, requester));
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED), requester.LimitEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                SetMaterialRequestLimitEnabled(requester, !requester.LimitEnabled);
                UpdateProductionSettingsPanel(true);
            }, requester.LimitEnabled);
            if (requester.LimitEnabled)
            {
                CreateProductionActionRow(
                    card.transform,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SET_LIMIT),
                    () => ShowMaterialRequestLimitDialog(requester));
            }
            if (!string.IsNullOrEmpty(requester.LastStatus))
            {
                productionAutomationView ??= new ProductionAutomationCardsView();
                productionAutomationView.MaterialStatus = CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), requester.LastStatus));
                SetFinePrintPreferredHeight(productionAutomationView.MaterialStatus, 24f);
            }
        }

        private void AddOutputAutomationCard(Transform parent, Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            GameObject card = CreateProductionCard(parent, "OutputCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TITLE), 0f);
            ApplyAutomationCardLayout(card, GetOutputAutomationCardHeight(requester));
            CreateStatusStrip(card.transform, requester.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_STATUS) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_STATUS), requester.OutputStoreEnabled ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.48f, 0.45f, 0.36f, 1f));
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_ENABLED), requester.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                requester.OutputStoreEnabled = !requester.OutputStoreEnabled;
                UpdateProductionSettingsPanel(true);
            }, requester.OutputStoreEnabled);
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_POLICY), StorageNetworkProductionSettingsText.GetOutputStoreModeName(requester), () => ShowOutputStorePicker(ownerStorage, requester));
            productionAutomationView ??= new ProductionAutomationCardsView();
            productionAutomationView.OutputDescription = CreateFinePrint(card.transform, requester.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_DESC));
            SetFinePrintPreferredHeight(productionAutomationView.OutputDescription, requester.OutputStoreEnabled ? 42f : 22f);
            if (requester.OutputStoreEnabled && !string.IsNullOrEmpty(requester.LastOutputStatus))
            {
                productionAutomationView.OutputStatus = CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_STATUS), requester.LastOutputStatus));
                SetFinePrintPreferredHeight(productionAutomationView.OutputStatus, 22f);
            }
        }

        private void AddStorageOutputCard(Storage ownerStorage, StorageNetworkStorageConnector connector)
        {
            GameObject card = CreateProductionCard("StorageOutputCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(connector.LastOutputStatus) ? 132f : 156f);
            CreateStatusStrip(card.transform, connector.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_STATUS) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_STATUS), connector.OutputStoreEnabled ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.48f, 0.45f, 0.36f, 1f));
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_ENABLED), connector.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                connector.OutputStoreEnabled = !connector.OutputStoreEnabled;
                UpdateProductionSettingsPanel(true);
            }, connector.OutputStoreEnabled);
            productionAutomationView ??= new ProductionAutomationCardsView();
            productionAutomationView.OutputDescription = CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_DESC));
            if (connector.OutputStoreEnabled && !string.IsNullOrEmpty(connector.LastOutputStatus))
            {
                productionAutomationView.OutputStatus = CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_STATUS), connector.LastOutputStatus));
                SetFinePrintPreferredHeight(productionAutomationView.OutputStatus, 22f);
            }
        }

        private void AddEnergyGeneratorMaterialCard(Storage ownerStorage, StorageNetworkEnergyGeneratorRequester requester)
        {
            bool enabled = requester != null && requester.RequestEnabled;
            GameObject card = CreateProductionCard("EnergyGeneratorMaterialCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(requester.LastStatus) ? 126f : 154f);

            CreateEnabledStatusStrip(card.transform, enabled);
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_ENABLED), enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                requester.RequestEnabled = !enabled;
                productionSettingsSignature = null;
                UpdateProductionSettingsPanel(true);
            }, enabled);
            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SOURCE_POLICY),
                StorageNetworkProductionSettingsText.GetEnergyGeneratorSourceModeName(requester),
                () => ShowEnergyGeneratorSourcePicker(ownerStorage, requester));
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED), requester.LimitEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                SetEnergyGeneratorMaterialRequestLimitEnabled(requester, !requester.LimitEnabled);
                UpdateProductionSettingsPanel(true);
            }, requester.LimitEnabled);
            if (requester.LimitEnabled)
            {
                CreateProductionActionRow(
                    card.transform,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SET_LIMIT),
                    () => ShowEnergyGeneratorMaterialRequestLimitDialog(requester));
            }
            productionAutomationView ??= new ProductionAutomationCardsView();
            if (!string.IsNullOrEmpty(requester.LastStatus))
            {
                productionAutomationView.MaterialStatus = CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), requester.LastStatus));
                SetFinePrintPreferredHeight(productionAutomationView.MaterialStatus, 22f);
            }
        }

        private void UpdateProductionAutomationCards(StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            if (productionAutomationView == null)
            {
                return;
            }

            if (productionAutomationView.MaterialStatus != null && requester != null)
            {
                SetTextIfChanged(
                    productionAutomationView.MaterialStatus,
                    string.IsNullOrEmpty(requester.LastStatus)
                        ? string.Empty
                        : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), requester.LastStatus));
            }

            if (productionAutomationView.MaterialStatus != null && energyRequester != null)
            {
                SetTextIfChanged(
                    productionAutomationView.MaterialStatus,
                    string.IsNullOrEmpty(energyRequester.LastStatus)
                        ? string.Empty
                        : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), energyRequester.LastStatus));
            }

            if (productionAutomationView.OutputDescription != null && requester != null)
            {
                SetTextIfChanged(
                    productionAutomationView.OutputDescription,
                    requester.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_DESC));
                SetFinePrintPreferredHeight(productionAutomationView.OutputDescription, requester.OutputStoreEnabled ? 42f : 22f);
            }

            if (productionAutomationView.OutputStatus != null)
            {
                string status = requester != null ? requester.LastOutputStatus : connector != null ? connector.LastOutputStatus : string.Empty;
                SetTextIfChanged(
                    productionAutomationView.OutputStatus,
                    string.IsNullOrEmpty(status)
                        ? string.Empty
                        : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_STATUS), status));
            }
        }

        private static float GetMaterialAutomationCardHeight(StorageNetworkMaterialRequester requester)
        {
            float height = requester != null && requester.LimitEnabled ? 214f : 176f;
            if (requester != null && !string.IsNullOrEmpty(requester.LastStatus))
            {
                height += 30f;
            }

            return height;
        }

        private static float GetOutputAutomationCardHeight(StorageNetworkMaterialRequester requester)
        {
            float height = 176f;
            if (requester != null && requester.OutputStoreEnabled)
            {
                height += 40f;
            }

            if (requester != null && !string.IsNullOrEmpty(requester.LastOutputStatus))
            {
                height += 30f;
            }

            return height;
        }

        private static void ApplyAutomationCardLayout(GameObject card, float minHeight)
        {
            LayoutElement layout = card.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = card.AddComponent<LayoutElement>();
            }

            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;
            layout.minHeight = minHeight;
            layout.preferredHeight = -1f;

            if (card.GetComponent<ContentSizeFitter>() == null)
            {
                card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private static void ApplyEqualAutomationCardLayout(GameObject card)
        {
            ApplyAutomationCardLayout(card, 0f);
        }

        private static void MakeProductionCardAutoHeight(GameObject card, float minHeight)
        {
            LayoutElement layout = card.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = card.AddComponent<LayoutElement>();
            }

            layout.minHeight = minHeight;
            layout.preferredHeight = -1f;

            if (card.GetComponent<ContentSizeFitter>() == null)
            {
                card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private static void SetFinePrintPreferredHeight(TextMeshProUGUI text, float height)
        {
            LayoutElement layout = text != null ? text.GetComponent<LayoutElement>() : null;
            if (layout == null)
            {
                return;
            }

            layout.minHeight = height;
            layout.preferredHeight = height;
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
                productionInventoryView = null;
                return;
            }

            productionInventoryView = new ProductionInventoryCardView();
            foreach (IGrouping<string, GameObject> group in items.GroupBy(GetStoredItemKey).OrderBy(group => GetStoredItemName(group.FirstOrDefault())))
            {
                float mass = group.Sum(GetStoredItemMass);
                ProductionInventoryRowView row = CreateProductionSettingsItemRow(
                    card.transform,
                    GetStoredItemName(group.FirstOrDefault()),
                    GameUtil.GetFormattedMass(mass),
                    group.FirstOrDefault());
                productionInventoryView.Rows[GetStoredItemKey(group.FirstOrDefault())] = row;
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
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI heading = CreateText("CardTitle", card.transform, title, 12, TextAlignmentOptions.MidlineLeft);
            heading.color = new Color(0.18f, 0.19f, 0.18f, 1f);
            heading.fontStyle = FontStyles.Bold;
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
            headingLayout.minHeight = 20f;
            headingLayout.preferredHeight = 20f;
            return card;
        }

        private TextMeshProUGUI CreateMetricTile(Transform parent, string label, string value, Color accent)
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
            return valueText;
        }

        private void CreateStatusStrip(Transform parent, string text, Color color)
        {
            GameObject strip = CreatePlainImage("StatusStrip", parent, color);
            LayoutElement stripLayout = strip.AddComponent<LayoutElement>();
            stripLayout.minHeight = 24f;
            stripLayout.preferredHeight = 24f;
            TextMeshProUGUI label = CreateText("Status", strip.transform, text, 11, TextAlignmentOptions.Center);
            label.color = new Color(0.96f, 0.96f, 0.90f, 1f);
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform(), 4f, 0f);
        }

        // 创建“已启用/已关闭”状态条，统一开关类卡片的状态颜色和文案。
        private void CreateEnabledStatusStrip(Transform parent, bool enabled)
        {
            CreateStatusStrip(
                parent,
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED),
                enabled ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.52f, 0.38f, 0.30f, 1f));
        }

        // 创建开关操作行，开启状态使用粉色按钮表示“关闭”，关闭状态使用蓝色按钮表示“开启”。
        private void CreateToggleActionRow(Transform parent, string label, string value, System.Action onClick, bool currentlyEnabled)
        {
            CreateProductionActionRow(parent, label, value, onClick, currentlyEnabled ? KleiPinkStyle() : KleiBlueStyle());
        }

        private void CreateProductionActionRow(Transform parent, string label, string value, System.Action onClick, ColorStyleSetting buttonStyle = null)
        {
            GameObject row = CreatePlainImage("ActionRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 30f;
            rowLayout.preferredHeight = 30f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 5, 3, 3);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 10, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = 24f;

            GameObject button = CreateStyledButton("Action", row.transform, value, onClick, buttonStyle ?? KleiBlueStyle());
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.minWidth = 126f;
            buttonLayout.flexibleWidth = 0f;
            buttonLayout.minHeight = 24f;
            buttonLayout.preferredHeight = 24f;
            buttonLayout.flexibleHeight = 0f;
        }

        private void CreateProductionReadOnlyRow(Transform parent, string label, string value)
        {
            GameObject row = CreatePlainImage("ReadOnlyRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 3, 3);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 10, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = 24f;

            TextMeshProUGUI valueText = CreateText("Value", row.transform, value, 10, TextAlignmentOptions.MidlineRight);
            valueText.color = new Color(0.26f, 0.28f, 0.27f, 1f);
            valueText.fontStyle = FontStyles.Bold;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;
        }

        private TextMeshProUGUI CreateFinePrint(Transform parent, string text)
        {
            TextMeshProUGUI label = CreateText("FinePrint", parent, text, 10, TextAlignmentOptions.TopLeft);
            label.color = new Color(0.34f, 0.35f, 0.33f, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 18f;
            layout.preferredHeight = -1f;
            label.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return label;
        }

        private static string GetProductionStateText(ComplexFabricator fabricator)
        {
            if (fabricator == null || fabricator.CurrentWorkingOrder == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_SHORT_IDLE);
            }

            return fabricator.WaitingForWorker ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_SHORT_WAITING_WORKER) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_SHORT_CRAFTING);
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
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NONE);
        }

        private static string GetNetworkStateText(Storage storage, StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            if (requester != null)
            {
                return requester.RequestEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REQUEST_ON_SHORT) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REQUEST_OFF_SHORT);
            }

            if (connector != null)
            {
                return connector.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_ON_SHORT) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_OFF_SHORT);
            }

            if (energyRequester != null)
            {
                return energyRequester.RequestEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REQUEST_ON_SHORT) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REQUEST_OFF_SHORT);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_COMPONENT);
        }

        private static LocString GetProductionInputMetricLabel(ComplexFabricator fabricator, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            return energyRequester != null && fabricator == null
                ? StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_REQUIRED
                : StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_RECIPE;
        }

        private static bool IsNetworkAutomationEnabled(Storage storage, StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            if (requester != null)
            {
                return requester.RequestEnabled || requester.OutputStoreEnabled;
            }

            if (connector != null)
            {
                return connector.OutputStoreEnabled;
            }

            if (energyRequester != null)
            {
                return energyRequester.RequestEnabled;
            }

            return false;
        }

        private static string GetEnergyGeneratorFuelText(EnergyGenerator generator)
        {
            if (generator == null || generator.formula.inputs == null || generator.formula.inputs.Length == 0)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NONE);
            }

            List<string> names = new List<string>();
            foreach (EnergyGenerator.InputItem input in generator.formula.inputs)
            {
                if (input.tag != Tag.Invalid)
                {
                    names.Add(input.tag.ProperName());
                }
            }

            return names.Count > 0 ? string.Join(", ", names) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NONE);
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
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE), StorageNetworkProductionSettingsText.GetMaterialRequestModeName(requester)),
                row => ShowMaterialSourcePicker(storage, requester));

            CreateProductionToggleRow(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED),
                requester.LimitEnabled,
                value =>
                {
                    SetMaterialRequestLimitEnabled(requester, value);
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
                    StorageNetworkProductionSettingsText.ColorizeMaterialRequestStatus(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), requester.LastStatus)),
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
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE), StorageNetworkProductionSettingsText.GetOutputStoreModeName(requester)),
                row => ShowOutputStorePicker(ownerStorage, requester));
        }

        private void AddProductionSettingsStatus(ComplexFabricator fabricator)
        {
            if (fabricator == null)
            {
                AddProductionSettingsText(StorageNetworkProductionSettingsText.ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_NO_RECIPE), "#6b6b63"), 12, FontStyles.Normal, 24f);
                return;
            }

            ComplexRecipe currentRecipe = fabricator.CurrentWorkingOrder;
            if (currentRecipe == null)
            {
                AddProductionSettingsText(StorageNetworkProductionSettingsText.ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_IDLE), "#5f665d"), 12, FontStyles.Normal, 22f);
                AddProductionSettingsText(StorageNetworkProductionSettingsText.ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_NO_RECIPE), "#6b6b63"), 12, FontStyles.Normal, 22f);
                return;
            }

            string statusText = fabricator.WaitingForWorker
                ? StorageNetworkProductionSettingsText.ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_WAITING_WORKER), "#b5753c")
                : StorageNetworkProductionSettingsText.ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_CRAFTING), "#3f7f4a");
            AddProductionSettingsText(statusText, 12, FontStyles.Normal, 22f);
            AddProductionSettingsText(
                StorageNetworkProductionSettingsText.ColorText(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_CURRENT_RECIPE), GetRecipeDisplayName(currentRecipe)), "#38485d"),
                12,
                FontStyles.Normal,
                22f);
            AddProductionSettingsText(
                StorageNetworkProductionSettingsText.ColorText(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_PROGRESS), Mathf.RoundToInt(Mathf.Clamp01(fabricator.OrderProgress) * 100f)), "#5a5f66"),
                12,
                FontStyles.Normal,
                22f);

            IReadOnlyList<string> orderUsages = productionOrderService.GetActiveOrderUsagesForFabricator(fabricator, 2);
            foreach (string usage in orderUsages)
            {
                AddProductionSettingsText(StorageNetworkProductionSettingsText.ColorText(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_USAGE_PREFIX), usage), "#7a4a66"), 10, FontStyles.Normal, 20f);
            }
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
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
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

        private void ShowEnergyGeneratorSourcePicker(Storage ownerStorage, StorageNetworkEnergyGeneratorRequester requester)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENERGY_GENERATOR_SOURCE_DESC),
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
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
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
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
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
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null || options == null || options.Count == 0)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("ProductionPicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 10f, 10f, 8f, 78f);

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

            GameObject closeButton = CreateCloseIconButton("PickerClose", header.transform, CloseProductionPicker);
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

            CreateProductionPickerFooter(content.transform, options.Count);
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
            GameObject row = new GameObject("StorageOptionRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            KImage background = row.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = selected
                ? KleiPinkStyle()
                : KleiBlueStyle();
            background.ColorState = KImage.ColorSelector.Inactive;
            row.AddComponent<LayoutElement>().preferredHeight = 42f;
            KButton button = row.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
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
            titleText.color = selected ? new Color(0.98f, 0.96f, 0.90f, 1f) : new Color(0.90f, 0.92f, 0.95f, 1f);
            titleText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI detailText = CreateText("Details", row.transform, details, 9, TextAlignmentOptions.MidlineLeft);
            detailText.color = selected ? new Color(0.88f, 0.84f, 0.78f, 1f) : new Color(0.70f, 0.73f, 0.78f, 1f);
            detailText.textWrappingMode = TextWrappingModes.NoWrap;
            detailText.overflowMode = TextOverflowModes.Ellipsis;
            detailText.gameObject.AddComponent<LayoutElement>().preferredWidth = 170f;
        }

        private void CreateProductionPickerFooter(Transform parent, int optionCount)
        {
            GameObject footer = CreatePlainImage("PickerFooter", parent, new Color(0.68f, 0.68f, 0.61f, 1f));
            LayoutElement footerLayout = footer.AddComponent<LayoutElement>();
            footerLayout.minHeight = 82f;
            footerLayout.preferredHeight = 82f;

            VerticalLayoutGroup layout = footer.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI countText = CreateText(
                "PickerFooterCount",
                footer.transform,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PICKER_OPTION_COUNT), Mathf.Max(0, optionCount - 1)),
                11,
                TextAlignmentOptions.MidlineLeft);
            countText.color = new Color(0.22f, 0.24f, 0.23f, 1f);
            countText.fontStyle = FontStyles.Bold;
            countText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            TextMeshProUGUI hintText = CreateText(
                "PickerFooterHint",
                footer.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PICKER_POLICY_HINT),
                10,
                TextAlignmentOptions.TopLeft);
            hintText.color = new Color(0.30f, 0.31f, 0.30f, 1f);
            hintText.textWrappingMode = TextWrappingModes.Normal;
            hintText.overflowMode = TextOverflowModes.Ellipsis;
            hintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        }

        private static List<Storage> GetNetworkStorageTargets(Storage ownerStorage)
        {
            return StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage);
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
            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null || requester == null)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("MaterialLimitPicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 84f, 84f, 118f, 96f);

            GameObject header = CreatePlainImage("LimitHeader", productionPickerRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 34f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("LimitTitle", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SET_LIMIT), 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("LimitClose", header.transform, CloseProductionPicker);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject body = CreatePlainImage("LimitBody", productionPickerRoot.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(8, 8, 8, 8);
            bodyLayout.spacing = 6f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            CreateLimitInfoRow(body.transform, string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT),
                GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())),
                GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))),
                () =>
                {
                    requester.ResetRequestedAmount();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                });

            float currentLimit = Mathf.Max(1f, requester.LimitKg <= 0f ? Config.Instance.DefaultMaterialRequestLimitKg : requester.LimitKg);
            KSlider slider = CreateLimitAmountRow(body.transform, currentLimit, 1f, 1000000f, out KInputTextField input);
            input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.inputType = TMP_InputField.InputType.Standard;

            bool syncingLimitControls = false;
            slider.onValueChanged.AddListener(value =>
            {
                if (syncingLimitControls)
                {
                    return;
                }

                syncingLimitControls = true;
                input.text = Mathf.RoundToInt(value).ToString();
                syncingLimitControls = false;
            });
            input.onValueChanged.AddListener(value =>
            {
                if (syncingLimitControls)
                {
                    return;
                }

                syncingLimitControls = true;
                slider.value = ParseMaterialLimitInput(value, slider.value);
                syncingLimitControls = false;
            });

            GameObject buttonRow = new GameObject("LimitButtonRow");
            buttonRow.transform.SetParent(body.transform, false);
            buttonRow.AddComponent<RectTransform>();
            LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
            buttonRowLayout.minHeight = 34f;
            buttonRowLayout.preferredHeight = 34f;
            buttonRowLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 6f;
            buttonLayout.childAlignment = TextAnchor.MiddleRight;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = false;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childForceExpandHeight = false;

            CreateFlexibleSpacer(buttonRow.transform);
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL), () =>
            {
                input.text = "1000000";
                slider.value = 1000000f;
            }, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), CloseProductionPicker, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                requester.LimitKg = ParseMaterialLimitInput(input.text, currentLimit);
                requester.LimitEnabled = true;
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            }, KleiPinkStyle());
        }

        private void ShowEnergyGeneratorMaterialRequestLimitDialog(StorageNetworkEnergyGeneratorRequester requester)
        {
            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null || requester == null)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("MaterialLimitPicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 84f, 84f, 118f, 96f);

            GameObject header = CreatePlainImage("LimitHeader", productionPickerRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 34f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("LimitTitle", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SET_LIMIT), 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("LimitClose", header.transform, CloseProductionPicker);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject body = CreatePlainImage("LimitBody", productionPickerRoot.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(8, 8, 8, 8);
            bodyLayout.spacing = 6f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            CreateLimitInfoRow(body.transform, string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT),
                GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())),
                GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))),
                () =>
                {
                    requester.ResetRequestedAmount();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                });

            float currentLimit = Mathf.Max(1f, requester.LimitKg <= 0f ? Config.Instance.DefaultMaterialRequestLimitKg : requester.LimitKg);
            KSlider slider = CreateLimitAmountRow(body.transform, currentLimit, 1f, 1000000f, out KInputTextField input);
            input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.inputType = TMP_InputField.InputType.Standard;

            bool syncingLimitControls = false;
            slider.onValueChanged.AddListener(value =>
            {
                if (syncingLimitControls)
                {
                    return;
                }

                syncingLimitControls = true;
                input.text = Mathf.RoundToInt(value).ToString();
                syncingLimitControls = false;
            });
            input.onValueChanged.AddListener(value =>
            {
                if (syncingLimitControls)
                {
                    return;
                }

                syncingLimitControls = true;
                slider.value = ParseMaterialLimitInput(value, slider.value);
                syncingLimitControls = false;
            });

            GameObject buttonRow = new GameObject("LimitButtonRow");
            buttonRow.transform.SetParent(body.transform, false);
            buttonRow.AddComponent<RectTransform>();
            LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
            buttonRowLayout.minHeight = 34f;
            buttonRowLayout.preferredHeight = 34f;
            buttonRowLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 6f;
            buttonLayout.childAlignment = TextAnchor.MiddleRight;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = false;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childForceExpandHeight = false;

            CreateFlexibleSpacer(buttonRow.transform);
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL), () =>
            {
                input.text = "1000000";
                slider.value = 1000000f;
            }, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), CloseProductionPicker, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                requester.LimitKg = ParseMaterialLimitInput(input.text, currentLimit);
                requester.LimitEnabled = true;
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            }, KleiPinkStyle());
        }

        private static void SetMaterialRequestLimitEnabled(StorageNetworkMaterialRequester requester, bool enabled)
        {
            if (requester == null)
            {
                return;
            }

            requester.LimitEnabled = enabled;
            if (enabled && requester.LimitKg <= 0f)
            {
                requester.LimitKg = Mathf.Max(1f, Config.Instance.DefaultMaterialRequestLimitKg);
            }
        }

        private static void SetEnergyGeneratorMaterialRequestLimitEnabled(StorageNetworkEnergyGeneratorRequester requester, bool enabled)
        {
            if (requester == null)
            {
                return;
            }

            requester.LimitEnabled = enabled;
            if (enabled && requester.LimitKg <= 0f)
            {
                requester.LimitKg = Mathf.Max(1f, Config.Instance.DefaultMaterialRequestLimitKg);
            }
        }

        private void CreateLimitInfoRow(Transform parent, string text, System.Action resetAction)
        {
            GameObject row = CreatePlainImage("LimitInfoRow", parent, new Color(0.68f, 0.68f, 0.61f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 32f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 8, 3, 3);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI label = CreateText("LimitInfo", row.transform, text, 11, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.22f, 0.24f, 0.23f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject button = CreateStyledButton("ResetLimitButton", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_RESET), resetAction, KleiBlueStyle());
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 100f;
            buttonLayout.minWidth = 92f;
            buttonLayout.preferredHeight = 24f;
            buttonLayout.minHeight = 24f;
        }

        private KSlider CreateLimitAmountRow(Transform parent, float value, float minValue, float maxValue, out KInputTextField input)
        {
            GameObject row = CreatePlainImage("LimitAmountRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 44f;
            rowLayout.preferredHeight = 44f;
            rowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI label = CreateText("SliderLabel", row.transform, "数量", 11, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 82f;

            KSlider slider = CreateAmountSlider(row.transform, maxValue);
            LayoutElement sliderLayout = slider.gameObject.GetComponent<LayoutElement>();
            if (sliderLayout != null)
            {
                sliderLayout.minWidth = 160f;
                sliderLayout.preferredHeight = 32f;
                sliderLayout.flexibleWidth = 1f;
                sliderLayout.flexibleHeight = 0f;
            }

            input = CreateFixedTextInput(row.transform, "MaterialLimitInput", Mathf.RoundToInt(Mathf.Clamp(value, minValue, maxValue)).ToString(), 170f, 24f, 11);

            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = true;
            slider.value = Mathf.Clamp(value, minValue, maxValue);
            return slider;
        }

        private void CreateLimitDialogButton(Transform parent, string text, System.Action onClick, ColorStyleSetting style)
        {
            GameObject button = CreateStyledButton("LimitButton", parent, text, onClick, style);
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = 96f;
            layout.minWidth = 82f;
            layout.minHeight = 26f;
            layout.preferredHeight = 26f;
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(96f, 26f);
            }
        }

        private static void CreateFlexibleSpacer(Transform parent)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static float ParseMaterialLimitInput(string text, float fallback)
        {
            string normalized = (text ?? string.Empty).Trim().Replace(',', '.');
            if (!float.TryParse(normalized, out float value))
            {
                value = fallback;
            }

            return Mathf.Clamp(value, 1f, 1000000f);
        }

        private ProductionInventoryRowView CreateProductionSettingsItemRow(string itemName, string formattedMass, GameObject representative)
        {
            return CreateProductionSettingsItemRow(productionSettingsContent, itemName, formattedMass, representative);
        }

        private ProductionInventoryRowView CreateProductionSettingsItemRow(Transform parent, string itemName, string formattedMass, GameObject representative)
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
            return new ProductionInventoryRowView
            {
                Name = name,
                Mass = mass,
                Icon = icon
            };
        }

        private void UpdateProductionInventoryCard(Storage storage, ComplexFabricator fabricator)
        {
            if (productionInventoryView == null)
            {
                return;
            }

            foreach (IGrouping<string, GameObject> group in GetProductionStorages(storage, fabricator)
                .SelectMany(itemStorage => itemStorage.items.Where(item => item != null))
                .GroupBy(GetStoredItemKey))
            {
                string key = group.Key;
                if (!productionInventoryView.Rows.TryGetValue(key, out ProductionInventoryRowView row))
                {
                    continue;
                }

                GameObject representative = group.FirstOrDefault();
                SetTextIfChanged(row.Name, GetStoredItemName(representative));
                SetTextIfChanged(row.Mass, GameUtil.GetFormattedMass(group.Sum(GetStoredItemMass)));
                SetStoredItemIcon(row.Icon, representative);
            }
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

        private static void SetTextIfChanged(TextMeshProUGUI text, string value)
        {
            if (text != null && text.text != value)
            {
                text.text = value;
            }
        }

        private sealed class ProductionOverviewCardView
        {
            public TextMeshProUGUI BuildingName { get; set; }

            public TextMeshProUGUI StorageValue { get; set; }

            public TextMeshProUGUI StateValue { get; set; }

            public TextMeshProUGUI RecipeValue { get; set; }

            public TextMeshProUGUI NetworkValue { get; set; }
        }

        private sealed class ProductionInventoryCardView
        {
            public Dictionary<string, ProductionInventoryRowView> Rows { get; } = new Dictionary<string, ProductionInventoryRowView>();
        }

        private sealed class ProductionAutomationCardsView
        {
            public TextMeshProUGUI MaterialStatus { get; set; }

            public TextMeshProUGUI OutputDescription { get; set; }

            public TextMeshProUGUI OutputStatus { get; set; }
        }

        private sealed class ProductionInventoryRowView
        {
            public TextMeshProUGUI Name { get; set; }

            public TextMeshProUGUI Mass { get; set; }

            public Image Icon { get; set; }
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
