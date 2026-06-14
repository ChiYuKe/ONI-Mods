using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void AddProductionOverviewCard(Storage storage, ComplexFabricator fabricator, StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            bool isServerStorage = StorageNetworkStorageRules.IsServerStorage(storage);
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

            TextMeshProUGUI recipeValue = null;
            TextMeshProUGUI networkValue = null;
            productionOverviewView = new ProductionOverviewCardView
            {
                BuildingName = title,
                StorageValue = CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_STORAGE), string.Empty, new Color(0.35f, 0.40f, 0.43f, 1f)),
                StateValue = CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_RUNNING), string.Empty, StorageNetworkProductionSettingsText.GetProductionStateColor(storage, fabricator)),
                RecipeValue = recipeValue,
                NetworkValue = networkValue
            };

            if (!isServerStorage)
            {
                productionOverviewView.RecipeValue = CreateMetricTile(
                    metrics.transform,
                    Get(StorageNetworkProductionSettingsText.GetProductionInputMetricLabel(fabricator, energyRequester)),
                    string.Empty,
                    new Color(0.39f, 0.42f, 0.45f, 1f));
                productionOverviewView.NetworkValue = CreateMetricTile(
                    metrics.transform,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_NETWORK),
                    string.Empty,
                    StorageNetworkProductionSettingsStyle.GetNetworkAutomationColor(
                        StorageNetworkProductionSettingsText.IsNetworkAutomationEnabled(requester, connector, energyRequester)));
            }
        }

        private void UpdateProductionOverviewCard(Storage storage, ComplexFabricator fabricator, StorageNetworkMaterialRequester requester, StorageNetworkStorageConnector connector, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            if (productionOverviewView == null)
            {
                return;
            }

            SetTextIfChanged(productionOverviewView.BuildingName, storage.GetProperName());
            SetTextIfChanged(productionOverviewView.StorageValue, string.Format("{0} / {1}", GameUtil.GetFormattedMass(storage.MassStored()), GameUtil.GetFormattedMass(storage.Capacity())));
            SetTextIfChanged(productionOverviewView.StateValue, StorageNetworkProductionSettingsText.GetProductionStateText(storage, fabricator));
            productionOverviewView.StateValue.color = StorageNetworkProductionSettingsText.GetProductionStateColor(storage, fabricator);
            if (productionOverviewView.RecipeValue != null)
            {
                SetTextIfChanged(
                    productionOverviewView.RecipeValue,
                    energyRequester != null && fabricator == null
                        ? StorageNetworkProductionSettingsText.GetEnergyGeneratorFuelText(storage.GetComponent<EnergyGenerator>())
                        : StorageNetworkProductionSettingsText.GetCurrentRecipeText(fabricator));
            }

            if (productionOverviewView.NetworkValue != null)
            {
                SetTextIfChanged(productionOverviewView.NetworkValue, StorageNetworkProductionSettingsText.GetNetworkStateText(requester, connector, energyRequester));
                productionOverviewView.NetworkValue.color = StorageNetworkProductionSettingsStyle.GetNetworkAutomationColor(StorageNetworkProductionSettingsText.IsNetworkAutomationEnabled(requester, connector, energyRequester));
            }
        }

        private void AddAutomationCards(Storage storage, StorageNetworkMaterialRequester requester)
        {
            GameObject grid = new GameObject("AutomationGrid");
            grid.transform.SetParent(productionSettingsContent, false);
            grid.AddComponent<RectTransform>();
            bool compact = productionSettingsRoot != null && productionSettingsRoot.GetComponent<RectTransform>().rect.width < 620f;
            LayoutElement gridLayout = grid.AddComponent<LayoutElement>();
            float gridHeight = Mathf.Max(
                StorageNetworkProductionSettingsStyle.GetMaterialAutomationCardHeight(requester),
                StorageNetworkProductionSettingsStyle.GetOutputAutomationCardHeight(requester));
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
            ApplyAutomationCardLayout(card, StorageNetworkProductionSettingsStyle.GetMaterialAutomationCardHeight(requester));
            CreateEnabledStatusStrip(card.transform, requester.RequestEnabled);
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_ENABLED), requester.RequestEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                requester.RequestEnabled = !requester.RequestEnabled;
                UpdateProductionSettingsPanel(true);
            }, requester.RequestEnabled);
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SOURCE_POLICY), StorageNetworkProductionSettingsText.GetMaterialRequestModeName(requester), () => ShowMaterialSourcePicker(ownerStorage, requester));
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED), requester.LimitEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                StorageNetworkMaterialLimitRules.SetEnabled(requester, !requester.LimitEnabled);
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
            ApplyAutomationCardLayout(card, StorageNetworkProductionSettingsStyle.GetOutputAutomationCardHeight(requester));
            CreateStatusStrip(card.transform, requester.OutputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_STATUS) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_STATUS), StorageNetworkProductionSettingsStyle.GetOutputStoreColor(requester.OutputStoreEnabled));
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
    }
}
