using StorageNetwork.Components;
using StorageNetwork.Services;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void AddStorageOutputCard(Storage ownerStorage, StorageNetworkStorageConnector connector)
        {
            bool outputStoreEnabled = connector.IsOutputStoreEnabled();
            GameObject card = CreateProductionCard("StorageOutputCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(connector.LastOutputStatus) ? 170f : 194f);
            CreateStatusStrip(card.transform, outputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_STATUS) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_STATUS), StorageNetworkProductionSettingsStyle.GetOutputStoreColor(outputStoreEnabled));
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_ENABLED), outputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                connector.SetOutputStoreEnabled(!outputStoreEnabled);
                UpdateProductionSettingsPanel(true);
            }, outputStoreEnabled);
            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_POLICY),
                StorageNetworkProductionSettingsText.GetOutputStoreModeName(connector),
                () => ShowStorageConnectorOutputStorePicker(ownerStorage, connector));
            productionAutomationView ??= new ProductionAutomationCardsView();
            productionAutomationView.OutputDescription = CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_OUTPUT_STORE_DESC));
            if (outputStoreEnabled && !string.IsNullOrEmpty(connector.LastOutputStatus))
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
                    () => ShowEnergyGeneratorMaterialRequestLimitDialog(requester));
            }
            productionAutomationView ??= new ProductionAutomationCardsView();
            if (!string.IsNullOrEmpty(requester.LastStatus))
            {
                productionAutomationView.MaterialStatus = CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), requester.LastStatus));
                SetFinePrintPreferredHeight(productionAutomationView.MaterialStatus, 22f);
            }
        }
    }
}
