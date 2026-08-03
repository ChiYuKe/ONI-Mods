using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.API;
using StorageNetwork.Core;
using StorageNetwork.Gameplay;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private readonly List<ProductionSettingsLiveBinding> productionSettingsLiveBindings =
            new List<ProductionSettingsLiveBinding>();
        private int productionSettingsObservedCapabilityVersion = -1;
        private int productionSettingsObservedReservationVersion = -1;
        private int productionSettingsObservedContentVersion = -1;

        private void ShowProductionSettingsPanel(Storage storage)
        {
            bool sameStorage = productionSettingsStorage == storage;
            productionSettingsStorage = storage;
            geyserSettingsGeyser = null;
            geyserSettingsSignature = null;
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
            productionSettingsTitle = window.Title;
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

            ComplexFabricator fabricator = storage.GetComponent<ComplexFabricator>();
            IStorageNetworkSettingsPanelProvider addonPanel = StorageNetworkInterfaceResolver.GetSettingsPanelProvider(storage);
            int capabilityVersion = StorageSceneRegistry.CapabilityVersion;
            int reservationVersion = StorageNetworkInputTargetReservationService.Version;
            int contentVersion = StorageNetworkContentIndexService.ChangeVersion;
            if (!force &&
                addonPanel == null &&
                productionSettingsSignature != null &&
                productionSettingsObservedCapabilityVersion == capabilityVersion &&
                productionSettingsObservedReservationVersion == reservationVersion &&
                productionSettingsObservedContentVersion == contentVersion)
            {
                UpdateProductionSettingsLive(storage, fabricator);
                return;
            }

            string signature = addonPanel != null
                ? "addon|" + (addonPanel.GetStorageNetworkSettingsPanelSignature(storage) ?? string.Empty)
                : StorageNetworkProductionSettingsSignatureBuilder.BuildProduction(storage, fabricator);
            if (!force && signature == productionSettingsSignature)
            {
                productionSettingsObservedCapabilityVersion = capabilityVersion;
                productionSettingsObservedReservationVersion = reservationVersion;
                productionSettingsObservedContentVersion = contentVersion;
                UpdateProductionSettingsLive(storage, fabricator);
                return;
            }

            productionSettingsSignature = signature;
            productionSettingsObservedCapabilityVersion = capabilityVersion;
            productionSettingsObservedReservationVersion = reservationVersion;
            productionSettingsObservedContentVersion = contentVersion;
            ClearProductionSettingsContent();
            SetProductionSettingsTitle(storage);
            if (addonPanel == null)
            {
                AddProductionSettingsTitleLiveBinding(storage);
            }
            KeepProductionSettingsPanelOnScreen();
            StorageNetworkColdStorageCooling coldStorageCooling = storage.GetComponent<StorageNetworkColdStorageCooling>();
            StorageNetworkMaterialRequester requester = storage.GetComponent<StorageNetworkMaterialRequester>();
            StorageNetworkStorageConnector connector = StorageNetworkStorageConnectorResolver.GetOrCreateForSettingsStorage(storage);
            StorageNetworkEnergyGeneratorRequester energyRequester = storage.GetComponent<StorageNetworkEnergyGeneratorRequester>();
            if (addonPanel != null)
            {
                StorageNetworkSettingsPanelBuilder builder = CreateAddonSettingsPanelBuilder();
                addonPanel.BuildStorageNetworkSettingsPanel(storage, builder);
                LayoutRebuilder.MarkLayoutForRebuild(productionSettingsContent);
                return;
            }

            if (coldStorageCooling != null)
            {
                AddColdStorageSettingsCard(storage, coldStorageCooling);
                AddServerAssignmentsSettingsCard(storage);
                AddInventoryCard(storage, fabricator);
                UpdateProductionSettingsLive(storage, fabricator);
                LayoutRebuilder.MarkLayoutForRebuild(productionSettingsContent);
                return;
            }

            if (StorageNetworkStorageRules.IsConfigurablePort(storage))
            {
                AddPortSettingsCard(storage);
                AddServerAssignmentsSettingsCard(storage);
                AddInventoryCard(storage, fabricator);
                UpdateProductionSettingsLive(storage, fabricator);
                LayoutRebuilder.MarkLayoutForRebuild(productionSettingsContent);
                return;
            }

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
            AddServerAssignmentsSettingsCard(storage);
            AddInventoryCard(storage, fabricator);
            UpdateProductionSettingsLive(storage, fabricator);

            LayoutRebuilder.MarkLayoutForRebuild(productionSettingsContent);
        }

        private void ClearProductionSettingsContent()
        {
            productionSettingsLiveBindings.Clear();
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

            if (StorageNetworkInterfaceResolver.GetSettingsPanelProvider(storage) != null)
            {
                return;
            }

            StorageNetworkMaterialRequester requester = storage.GetComponent<StorageNetworkMaterialRequester>();
            StorageNetworkStorageConnector connector = storage.GetComponent<StorageNetworkColdStorageCooling>() != null
                ? null
                : StorageNetworkStorageConnectorResolver.GetOrCreateForSettingsStorage(storage);
            StorageNetworkEnergyGeneratorRequester energyRequester = storage.GetComponent<StorageNetworkEnergyGeneratorRequester>();
            UpdateProductionOverviewCard(storage, fabricator, requester, connector, energyRequester);
            UpdateProductionAutomationCards(requester, connector, energyRequester);
            UpdateProductionInventoryCard(storage, fabricator);
            UpdateProductionSettingsLiveBindings();
        }

        private void AddProductionSettingsLiveBinding(
            System.Func<int> fingerprint,
            System.Action update)
        {
            if (fingerprint == null || update == null)
            {
                return;
            }

            productionSettingsLiveBindings.Add(
                new ProductionSettingsLiveBinding(
                    fingerprint,
                    update,
                    fingerprint()));
        }

        private void UpdateProductionSettingsLiveBindings()
        {
            for (int index = 0; index < productionSettingsLiveBindings.Count; index++)
            {
                ProductionSettingsLiveBinding binding =
                    productionSettingsLiveBindings[index];
                int fingerprint = binding.Fingerprint();
                if (fingerprint == binding.LastFingerprint)
                {
                    continue;
                }

                binding.LastFingerprint = fingerprint;
                binding.Update();
            }
        }

        private sealed class ProductionSettingsLiveBinding
        {
            public ProductionSettingsLiveBinding(
                System.Func<int> fingerprint,
                System.Action update,
                int lastFingerprint)
            {
                Fingerprint = fingerprint;
                Update = update;
                LastFingerprint = lastFingerprint;
            }

            public System.Func<int> Fingerprint { get; }

            public System.Action Update { get; }

            public int LastFingerprint { get; set; }
        }

        private void SetProductionSettingsTitle(Storage storage)
        {
            TextMeshProUGUI title = productionSettingsTitle;
            if (title != null)
            {
                bool powerPort = StorageNetworkStorageRules.IsPowerInputPort(storage) ||
                                 StorageNetworkStorageRules.IsPowerOutputPort(storage);
                if (powerPort)
                {
                    float storedJoules = GetPowerPortStoredJoules(storage);
                    float capacityJoules = GetPowerPortCapacityJoules(storage);
                    SetTextIfChanged(
                        title,
                        string.Format(
                            Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_STORAGE_DETAILS),
                            GameUtil.GetFormattedJoules(storedJoules, "F1", GameUtil.TimeSlice.None),
                            GameUtil.GetFormattedJoules(capacityJoules, "F1", GameUtil.TimeSlice.None),
                            GameUtil.GetFormattedJoules(Mathf.Max(0f, capacityJoules - storedJoules), "F1", GameUtil.TimeSlice.None)));
                    return;
                }

                SetTextIfChanged(
                    title,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_DETAILS),
                        GameUtil.GetFormattedMass(storage.MassStored()),
                        GameUtil.GetFormattedMass(storage.Capacity()),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, storage.RemainingCapacity()))));
            }
        }

        private void AddProductionSettingsTitleLiveBinding(Storage storage)
        {
            if (storage == null || productionSettingsTitle == null)
            {
                return;
            }

            bool powerPort = StorageNetworkStorageRules.IsPowerInputPort(storage) ||
                             StorageNetworkStorageRules.IsPowerOutputPort(storage);
            AddProductionSettingsLiveBinding(
                () => powerPort
                    ? CombineLiveFingerprint(
                        GetPowerPortStoredJoules(storage),
                        GetPowerPortCapacityJoules(storage))
                    : CombineLiveFingerprint(
                        storage.MassStored(),
                        storage.Capacity()),
                () => SetProductionSettingsTitle(storage));
        }

        private static int CombineLiveFingerprint(float first, float second)
        {
            unchecked
            {
                return (first.GetHashCode() * 397) ^ second.GetHashCode();
            }
        }

        private static float GetPowerPortStoredJoules(Storage storage)
        {
            StorageNetworkPowerInputPortConsumer input = storage != null ? storage.GetComponent<StorageNetworkPowerInputPortConsumer>() : null;
            if (input != null)
            {
                return input.PortJoulesAvailable;
            }

            StorageNetworkPowerOutputPortGenerator output = storage != null ? storage.GetComponent<StorageNetworkPowerOutputPortGenerator>() : null;
            return output != null ? Mathf.Clamp(output.StoredJoules, 0f, output.Capacity) : 0f;
        }

        private static float GetPowerPortCapacityJoules(Storage storage)
        {
            StorageNetworkPowerInputPortConsumer input = storage != null ? storage.GetComponent<StorageNetworkPowerInputPortConsumer>() : null;
            if (input != null)
            {
                return input.PortCapacityJoules;
            }

            StorageNetworkPowerOutputPortGenerator output = storage != null ? storage.GetComponent<StorageNetworkPowerOutputPortGenerator>() : null;
            return output != null ? output.Capacity : 0f;
        }

        private void AddProductionSettingsText(string text, int size, FontStyles style, float height)
        {
            TextMeshProUGUI label = CreateText("ProductionSettingsText", productionSettingsContent, text, size, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            label.fontStyle = style;
            label.richText = true;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static void SetTextIfChanged(TextMeshProUGUI text, string value)
        {
            if (text != null && text.text != value)
            {
                text.text = value;
            }
        }

    }
}
