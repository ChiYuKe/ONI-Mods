using System.Collections.Generic;
using System.Linq;
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
        private void AddPortSettingsCard(Storage storage)
        {
            AddPortOverviewCard(storage);

            StorageNetworkSolidInputPortIngress solidInput = storage.GetComponent<StorageNetworkSolidInputPortIngress>();
            if (solidInput != null)
            {
                AddSolidInputPortSettingsCard(storage, solidInput);
                return;
            }

            StorageNetworkSolidOutputPortEgress solidOutput = storage.GetComponent<StorageNetworkSolidOutputPortEgress>();
            if (solidOutput != null)
            {
                AddSolidOutputPortSettingsCard(storage, solidOutput);
                return;
            }

            StorageNetworkLiquidInputPortIngress liquidInput = storage.GetComponent<StorageNetworkLiquidInputPortIngress>();
            if (liquidInput != null)
            {
                AddLiquidInputPortSettingsCard(storage, liquidInput);
                return;
            }

            StorageNetworkLiquidOutputPortEgress liquidOutput = storage.GetComponent<StorageNetworkLiquidOutputPortEgress>();
            if (liquidOutput != null)
            {
                AddLiquidOutputPortSettingsCard(storage, liquidOutput);
                return;
            }

            StorageNetworkGasInputPortIngress gasInput = storage.GetComponent<StorageNetworkGasInputPortIngress>();
            if (gasInput != null)
            {
                AddGasInputPortSettingsCard(storage, gasInput);
                return;
            }

            StorageNetworkGasOutputPortEgress gasOutput = storage.GetComponent<StorageNetworkGasOutputPortEgress>();
            if (gasOutput != null)
            {
                AddGasOutputPortSettingsCard(storage, gasOutput);
                return;
            }

            StorageNetworkPowerInputPortConsumer powerInput = storage.GetComponent<StorageNetworkPowerInputPortConsumer>();
            if (powerInput != null)
            {
                AddPowerInputPortSettingsCard(storage, powerInput);
                return;
            }

            StorageNetworkPowerOutputPortGenerator powerOutput = storage.GetComponent<StorageNetworkPowerOutputPortGenerator>();
            if (powerOutput != null)
            {
                AddPowerOutputPortSettingsCard(storage, powerOutput);
                return;
            }

            StorageNetworkParticleInputPortIngress particleInput = storage.GetComponent<StorageNetworkParticleInputPortIngress>();
            if (particleInput != null)
            {
                AddParticleInputPortSettingsCard(storage, particleInput);
                return;
            }

            StorageNetworkParticleOutputPortEgress particleOutput = storage.GetComponent<StorageNetworkParticleOutputPortEgress>();
            if (particleOutput != null)
            {
                AddParticleOutputPortSettingsCard(storage, particleOutput);
                return;
            }

            GameObject card = CreateProductionCard("PortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_PORT_SETTINGS_TITLE), 0f);
            MakeProductionCardAutoHeight(card, 88f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            CreateStatusStrip(
                card.transform,
                online
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_ONLINE)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                online ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            CreateProductionReadOnlyRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_TITLE), GetPortStatusName(storage));
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_COMPONENT));
        }

        private void AddPortOverviewCard(Storage storage)
        {
            GameObject card = CreateProductionCard("PortOverviewCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_TITLE), 132f);
            TextMeshProUGUI title = CreateText("PortName", card.transform, storage.GetProperName(), 16, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.14f, 0.15f, 0.14f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            GameObject metrics = new GameObject("PortMetrics");
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

            StorageNetworkLiquidInputPortIngress liquidInput = storage.GetComponent<StorageNetworkLiquidInputPortIngress>();
            StorageNetworkLiquidOutputPortEgress liquidOutput = storage.GetComponent<StorageNetworkLiquidOutputPortEgress>();
            StorageNetworkGasInputPortIngress gasInput = storage.GetComponent<StorageNetworkGasInputPortIngress>();
            StorageNetworkGasOutputPortEgress gasOutput = storage.GetComponent<StorageNetworkGasOutputPortEgress>();
            StorageNetworkSolidInputPortIngress solidInput = storage.GetComponent<StorageNetworkSolidInputPortIngress>();
            StorageNetworkSolidOutputPortEgress solidOutput = storage.GetComponent<StorageNetworkSolidOutputPortEgress>();
            StorageNetworkPowerInputPortConsumer powerInput = storage.GetComponent<StorageNetworkPowerInputPortConsumer>();
            StorageNetworkPowerOutputPortGenerator powerOutput = storage.GetComponent<StorageNetworkPowerOutputPortGenerator>();
            StorageNetworkParticleInputPortIngress particleInput = storage.GetComponent<StorageNetworkParticleInputPortIngress>();
            StorageNetworkParticleOutputPortEgress particleOutput = storage.GetComponent<StorageNetworkParticleOutputPortEgress>();
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = liquidInput != null
                ? liquidInput.InputStoreEnabled
                : liquidOutput != null
                    ? liquidOutput.OutputRequestEnabled
                    : gasInput != null
                        ? gasInput.InputStoreEnabled
                        : gasOutput != null
                            ? gasOutput.OutputRequestEnabled
                                : solidInput != null
                                    ? solidInput.InputStoreEnabled
                                    : solidOutput != null
                                        ? solidOutput.OutputRequestEnabled
                                    : powerInput != null
                                        ? powerInput.GetInputWattsSetting() > 0f
                                            : powerOutput != null
                                                ? powerOutput.GetOutputWattsSetting() > 0f
                                                : particleInput != null
                                                    ? particleInput.InputStoreEnabled
                                                    : particleOutput == null || particleOutput.OutputRequestEnabled;
            bool running = online && enabled;
            string automationLabel = particleOutput != null
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_REQUEST_ENABLED)
                : particleInput != null
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_INPUT_PORT_STORE_ENABLED)
                    : liquidOutput != null || gasOutput != null || solidOutput != null || powerOutput != null
                        ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_ENABLED)
                        : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_ENABLED);
            string automationValue = liquidInput != null
                ? liquidInput.InputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                : liquidOutput != null
                    ? liquidOutput.OutputRequestEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                    : gasInput != null
                        ? gasInput.InputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                        : gasOutput != null
                            ? gasOutput.OutputRequestEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                            : solidInput != null
                                ? solidInput.InputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                                : solidOutput != null
                                    ? solidOutput.OutputRequestEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                                    : powerInput != null
                                        ? powerInput.GetInputWattsSetting() > 0f ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                                        : powerOutput != null
                                            ? powerOutput.GetOutputWattsSetting() > 0f ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                                            : particleInput != null
                                                ? particleInput.InputStoreEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                                                : particleOutput != null
                                                    ? particleOutput.OutputRequestEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED)
                                                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_COMPONENT);
            string policyValue = liquidInput != null
                ? GetInputPortStoreModeName(liquidInput)
                : liquidOutput != null
                    ? GetOutputPortSourceModeName(liquidOutput)
                    : gasInput != null
                        ? GetInputPortStoreModeName(gasInput)
                        : gasOutput != null
                            ? GetOutputPortSourceModeName(gasOutput)
                            : solidInput != null
                                ? GetInputPortStoreModeName(solidInput)
                                : solidOutput != null
                                    ? GetOutputPortSourceModeName(solidOutput)
                                    : powerInput != null
                                        ? GetPowerInputRateName(powerInput)
                                        : powerOutput != null
                                            ? GetPowerOutputRateName(powerOutput)
                                            : particleOutput != null
                                                ? GetParticleThresholdName(particleOutput)
                                                : particleInput != null ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_CAPTURE_MODE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NONE);

            if (powerInput != null || powerOutput != null)
            {
                int worldId = GetStorageWorldId(storage);
                CreateMetricTile(
                    metrics.transform,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_POWER_STORED),
                    string.Format("{0} / {1}",
                        GameUtil.GetFormattedJoules(StorageNetworkPowerService.GetStoredJoules(worldId), "F1", GameUtil.TimeSlice.None),
                        GameUtil.GetFormattedJoules(StorageNetworkPowerService.GetCapacityJoules(worldId), "F1", GameUtil.TimeSlice.None)),
                    new Color(0.35f, 0.40f, 0.43f, 1f));
            }
            else if (particleInput != null || particleOutput != null)
            {
                CreateMetricTile(
                    metrics.transform,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_STORAGE_TITLE),
                    GetParticleNetworkStorageName(storage),
                    new Color(0.35f, 0.40f, 0.43f, 1f));
            }
            else
            {
                CreateMetricTile(
                    metrics.transform,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_STORAGE),
                    string.Format("{0} / {1}", GameUtil.GetFormattedMass(storage.MassStored()), GameUtil.GetFormattedMass(storage.Capacity())),
                    new Color(0.35f, 0.40f, 0.43f, 1f));
            }
            CreateMetricTile(
                metrics.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_RUNNING),
                running ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_SHORT_IDLE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE),
                running ? new Color(0.26f, 0.52f, 0.34f, 1f) : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));
            CreateMetricTile(
                metrics.transform,
                automationLabel,
                automationValue,
                enabled ? new Color(0.26f, 0.52f, 0.34f, 1f) : new Color(0.50f, 0.42f, 0.34f, 1f));
            CreateMetricTile(
                metrics.transform,
                powerInput != null || powerOutput != null
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_RATE)
                    : particleOutput != null
                        ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_THRESHOLD)
                        : liquidOutput != null || gasOutput != null || solidOutput != null ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SOURCE_POLICY) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_POLICY),
                policyValue,
                new Color(0.39f, 0.42f, 0.45f, 1f));
        }

        private void AddSolidInputPortSettingsCard(Storage storage, StorageNetworkSolidInputPortIngress ingress)
        {
            GameObject card = CreateProductionCard("SolidInputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(ingress.LastStatus) ? 150f : 174f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = ingress.InputStoreEnabled;

            CreateStatusStrip(
                card.transform,
                enabled && online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                enabled && online ? new Color(0.28f, 0.48f, 0.34f, 1f) : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_ENABLED), enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                ingress.InputStoreEnabled = !enabled;
                UpdateProductionSettingsPanel(true);
            }, enabled);
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_POLICY), GetInputPortStoreModeName(ingress), () => ShowInputPortStorePicker(storage, ingress));
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_DESC));
            if (!string.IsNullOrEmpty(ingress.LastStatus))
            {
                TextMeshProUGUI status = CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_STATUS), ingress.LastStatus));
                SetFinePrintPreferredHeight(status, 22f);
            }
        }

        private void AddSolidOutputPortSettingsCard(Storage storage, StorageNetworkSolidOutputPortEgress egress)
        {
            GameObject card = CreateProductionCard("SolidOutputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_REQUEST_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(egress.LastStatus) ? 264f : 288f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = egress.OutputRequestEnabled;

            CreateStatusStrip(
                card.transform,
                enabled && online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                enabled && online ? new Color(0.28f, 0.48f, 0.34f, 1f) : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            GameObject grid = new GameObject("SolidOutputPortSettingsGrid");
            grid.transform.SetParent(card.transform, false);
            grid.AddComponent<RectTransform>();
            bool compact = productionSettingsRoot != null && productionSettingsRoot.GetComponent<RectTransform>().rect.width < 620f;
            LayoutElement gridLayout = grid.AddComponent<LayoutElement>();
            float controlHeight = string.IsNullOrEmpty(egress.LastStatus) ? 218f : 242f;
            float limitHeight = egress.OutputLimitEnabled ? 154f : 118f;
            gridLayout.minHeight = compact ? controlHeight + limitHeight + 8f : Mathf.Max(controlHeight, limitHeight);
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

            GameObject controlCard = CreateProductionCard(grid.transform, "SolidOutputControlCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_REQUEST_TITLE), 0f);
            ApplyAutomationCardLayout(controlCard, controlHeight);
            CreateToggleActionRow(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_REQUEST_ENABLED), enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                egress.OutputRequestEnabled = !enabled;
                UpdateProductionSettingsPanel(true);
            }, enabled);
            CreateProductionActionRow(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SOURCE_POLICY), GetOutputPortSourceModeName(egress), () => ShowOutputPortSourcePicker(storage, egress));
            CreateProductionActionRow(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER), GetOutputPortFilterName(egress), () => ShowOutputPortMaterialFilterPicker(storage, egress));
            CreateProductionActionRow(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE), GetOutputPortRequestRateName(egress), () => ShowOutputPortRequestRateDialog(egress));
            CreateFinePrint(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_REQUEST_DESC));
            if (!string.IsNullOrEmpty(egress.LastStatus))
            {
                TextMeshProUGUI status = CreateFinePrint(controlCard.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_REQUEST_STATUS), egress.LastStatus));
                SetFinePrintPreferredHeight(status, 22f);
            }

            GameObject limitCard = CreateProductionCard(grid.transform, "SolidOutputLimitCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT_ENABLED), 0f);
            ApplyAutomationCardLayout(limitCard, limitHeight);
            CreateToggleActionRow(limitCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT_ENABLED), egress.OutputLimitEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                egress.SetOutputLimitEnabled(!egress.OutputLimitEnabled);
                UpdateProductionSettingsPanel(true);
            }, egress.OutputLimitEnabled);
            if (egress.OutputLimitEnabled)
            {
                CreateProductionActionRow(
                    limitCard.transform,
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT), GameUtil.GetFormattedMass(Mathf.Max(0f, egress.OutputLimitUsedKg)), GameUtil.GetFormattedMass(Mathf.Max(0f, egress.OutputLimitKg))),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SET_LIMIT),
                    () => ShowOutputPortLimitDialog(storage, egress));
            }
            CreateProductionActionRow(limitCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_OUTPUT_RATE), GetOutputPortRequestRateName(egress), () => ShowOutputPortRequestRateDialog(egress));
        }

        private void AddLiquidInputPortSettingsCard(Storage storage, StorageNetworkLiquidInputPortIngress ingress)
        {
            GameObject card = CreateProductionCard("LiquidInputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(ingress.LastStatus) ? 150f : 174f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = ingress.InputStoreEnabled;

            CreateStatusStrip(
                card.transform,
                enabled && online
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED)
                    : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                enabled && online
                    ? new Color(0.28f, 0.48f, 0.34f, 1f)
                    : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            CreateToggleActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_ENABLED),
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    ingress.InputStoreEnabled = !enabled;
                    UpdateProductionSettingsPanel(true);
                },
                enabled);
            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_POLICY),
                GetInputPortStoreModeName(ingress),
                () => ShowInputPortStorePicker(storage, ingress));
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_DESC));

            if (!string.IsNullOrEmpty(ingress.LastStatus))
            {
                TextMeshProUGUI status = CreateFinePrint(
                    card.transform,
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_PORT_STORE_STATUS), ingress.LastStatus));
                SetFinePrintPreferredHeight(status, 22f);
            }
        }

        private void AddLiquidOutputPortSettingsCard(Storage storage, StorageNetworkLiquidOutputPortEgress egress)
        {
            GameObject card = CreateProductionCard("LiquidOutputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(egress.LastStatus) ? 264f : 288f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = egress.OutputRequestEnabled;

            CreateStatusStrip(
                card.transform,
                enabled && online
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED)
                    : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                enabled && online
                    ? new Color(0.28f, 0.48f, 0.34f, 1f)
                    : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            GameObject grid = new GameObject("LiquidOutputPortSettingsGrid");
            grid.transform.SetParent(card.transform, false);
            grid.AddComponent<RectTransform>();
            bool compact = productionSettingsRoot != null && productionSettingsRoot.GetComponent<RectTransform>().rect.width < 620f;
            LayoutElement gridLayout = grid.AddComponent<LayoutElement>();
            float controlHeight = string.IsNullOrEmpty(egress.LastStatus) ? 186f : 210f;
            float limitHeight = egress.OutputLimitEnabled ? 154f : 118f;
            gridLayout.minHeight = compact ? controlHeight + limitHeight + 8f : Mathf.Max(controlHeight, limitHeight);
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

            GameObject controlCard = CreateProductionCard(grid.transform, "LiquidOutputControlCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_TITLE), 0f);
            ApplyAutomationCardLayout(controlCard, controlHeight);
            CreateToggleActionRow(
                controlCard.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_ENABLED),
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    egress.OutputRequestEnabled = !enabled;
                    UpdateProductionSettingsPanel(true);
                },
                enabled);
            CreateProductionActionRow(
                controlCard.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SOURCE_POLICY),
                GetOutputPortSourceModeName(egress),
                () => ShowOutputPortSourcePicker(storage, egress));
            CreateProductionActionRow(
                controlCard.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER),
                GetOutputPortFilterName(egress),
                () => ShowOutputPortLiquidFilterPicker(storage, egress));
            CreateProductionActionRow(
                controlCard.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE),
                GetOutputPortRequestRateName(egress),
                () => ShowOutputPortRequestRateDialog(egress));
            CreateFinePrint(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_DESC));
            if (!string.IsNullOrEmpty(egress.LastStatus))
            {
                TextMeshProUGUI status = CreateFinePrint(
                    controlCard.transform,
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_STATUS), egress.LastStatus));
                SetFinePrintPreferredHeight(status, 22f);
            }

            GameObject limitCard = CreateProductionCard(grid.transform, "LiquidOutputLimitCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT_ENABLED), 0f);
            ApplyAutomationCardLayout(limitCard, limitHeight);
            CreateToggleActionRow(
                limitCard.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT_ENABLED),
                egress.OutputLimitEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    egress.SetOutputLimitEnabled(!egress.OutputLimitEnabled);
                    UpdateProductionSettingsPanel(true);
                },
                egress.OutputLimitEnabled);
            if (egress.OutputLimitEnabled)
            {
                CreateProductionActionRow(
                    limitCard.transform,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, egress.OutputLimitUsedKg)),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, egress.OutputLimitKg))),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SET_LIMIT),
                    () => ShowOutputPortLimitDialog(storage, egress));
            }
            CreateProductionActionRow(
                limitCard.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_OUTPUT_RATE),
                GetOutputPortRequestRateName(egress),
                () => ShowOutputPortRequestRateDialog(egress));
        }

        private void AddGasInputPortSettingsCard(Storage storage, StorageNetworkGasInputPortIngress ingress)
        {
            GameObject card = CreateProductionCard("GasInputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_INPUT_PORT_STORE_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(ingress.LastStatus) ? 150f : 174f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = ingress.InputStoreEnabled;

            CreateStatusStrip(card.transform, enabled && online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE), enabled && online ? new Color(0.28f, 0.48f, 0.34f, 1f) : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_INPUT_PORT_STORE_ENABLED), enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                ingress.InputStoreEnabled = !enabled;
                UpdateProductionSettingsPanel(true);
            }, enabled);
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_POLICY), GetInputPortStoreModeName(ingress), () => ShowInputPortStorePicker(storage, ingress));
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_INPUT_PORT_STORE_DESC));
            if (!string.IsNullOrEmpty(ingress.LastStatus))
            {
                TextMeshProUGUI status = CreateFinePrint(card.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_INPUT_PORT_STORE_STATUS), ingress.LastStatus));
                SetFinePrintPreferredHeight(status, 22f);
            }
        }

        private void AddGasOutputPortSettingsCard(Storage storage, StorageNetworkGasOutputPortEgress egress)
        {
            GameObject card = CreateProductionCard("GasOutputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_REQUEST_TITLE), 0f);
            MakeProductionCardAutoHeight(card, string.IsNullOrEmpty(egress.LastStatus) ? 264f : 288f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = egress.OutputRequestEnabled;

            CreateStatusStrip(card.transform, enabled && online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE), enabled && online ? new Color(0.28f, 0.48f, 0.34f, 1f) : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            GameObject grid = new GameObject("GasOutputPortSettingsGrid");
            grid.transform.SetParent(card.transform, false);
            grid.AddComponent<RectTransform>();
            bool compact = productionSettingsRoot != null && productionSettingsRoot.GetComponent<RectTransform>().rect.width < 620f;
            LayoutElement gridLayout = grid.AddComponent<LayoutElement>();
            float controlHeight = string.IsNullOrEmpty(egress.LastStatus) ? 186f : 210f;
            float limitHeight = egress.OutputLimitEnabled ? 154f : 118f;
            gridLayout.minHeight = compact ? controlHeight + limitHeight + 8f : Mathf.Max(controlHeight, limitHeight);
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

            GameObject controlCard = CreateProductionCard(grid.transform, "GasOutputControlCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_REQUEST_TITLE), 0f);
            ApplyAutomationCardLayout(controlCard, controlHeight);
            CreateToggleActionRow(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_REQUEST_ENABLED), enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                egress.OutputRequestEnabled = !enabled;
                UpdateProductionSettingsPanel(true);
            }, enabled);
            CreateProductionActionRow(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SOURCE_POLICY), GetOutputPortSourceModeName(egress), () => ShowOutputPortSourcePicker(storage, egress));
            CreateProductionActionRow(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER), GetOutputPortFilterName(egress), () => ShowOutputPortGasFilterPicker(storage, egress));
            CreateProductionActionRow(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE), GetOutputPortRequestRateName(egress), () => ShowOutputPortRequestRateDialog(egress));
            CreateFinePrint(controlCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_REQUEST_DESC));
            if (!string.IsNullOrEmpty(egress.LastStatus))
            {
                TextMeshProUGUI status = CreateFinePrint(controlCard.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_REQUEST_STATUS), egress.LastStatus));
                SetFinePrintPreferredHeight(status, 22f);
            }

            GameObject limitCard = CreateProductionCard(grid.transform, "GasOutputLimitCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT_ENABLED), 0f);
            ApplyAutomationCardLayout(limitCard, limitHeight);
            CreateToggleActionRow(limitCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT_ENABLED), egress.OutputLimitEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                egress.SetOutputLimitEnabled(!egress.OutputLimitEnabled);
                UpdateProductionSettingsPanel(true);
            }, egress.OutputLimitEnabled);
            if (egress.OutputLimitEnabled)
            {
                CreateProductionActionRow(limitCard.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT), GameUtil.GetFormattedMass(Mathf.Max(0f, egress.OutputLimitUsedKg)), GameUtil.GetFormattedMass(Mathf.Max(0f, egress.OutputLimitKg))), Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SET_LIMIT), () => ShowOutputPortLimitDialog(storage, egress));
            }
            CreateProductionActionRow(limitCard.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_OUTPUT_RATE), GetOutputPortRequestRateName(egress), () => ShowOutputPortRequestRateDialog(egress));
        }

        private void AddPowerInputPortSettingsCard(Storage storage, StorageNetworkPowerInputPortConsumer input)
        {
            GameObject card = CreateProductionCard("PowerInputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STORE_TITLE), 0f);
            MakeProductionCardAutoHeight(card, 142f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = input.GetInputWattsSetting() > 0f;

            CreateStatusStrip(
                card.transform,
                enabled && online
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED)
                    : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                enabled && online
                    ? new Color(0.28f, 0.48f, 0.34f, 1f)
                    : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            CreateToggleActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STORE_ENABLED),
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    input.SetInputWatts(enabled ? 0f : Mathf.Min(StorageNetworkPowerInputPortConsumer.DefaultInputWatts, StorageNetworkPowerInputPortConsumer.GetMaxInputWatts()));
                    UpdateProductionSettingsPanel(true);
                },
                enabled);
            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_POLICY),
                GetPowerInputStoreModeName(input),
                () => ShowPowerInputStorePicker(storage, input));
            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_INPUT_RATE),
                GetPowerInputRateName(input),
                () => ShowPowerInputRateDialog(input));
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STORE_DESC));
        }

        private void AddPowerOutputPortSettingsCard(Storage storage, StorageNetworkPowerOutputPortGenerator output)
        {
            GameObject card = CreateProductionCard("PowerOutputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_REQUEST_TITLE), 0f);
            MakeProductionCardAutoHeight(card, output.OutputLimitEnabled ? 250f : 218f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = output.GetOutputWattsSetting() > 0f;

            CreateStatusStrip(
                card.transform,
                enabled && online
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED)
                    : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                enabled && online
                    ? new Color(0.28f, 0.48f, 0.34f, 1f)
                    : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            CreateToggleActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_REQUEST_ENABLED),
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    output.SetOutputWatts(enabled ? 0f : Mathf.Min(StorageNetworkPowerOutputPortGenerator.DefaultOutputWatts, StorageNetworkPowerOutputPortGenerator.GetMaxOutputWatts()));
                    UpdateProductionSettingsPanel(true);
                },
                enabled);
            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SOURCE_POLICY),
                GetPowerOutputSourceModeName(output),
                () => ShowPowerOutputSourcePicker(storage, output));
            CreateToggleActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT_ENABLED),
                output.OutputLimitEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    output.SetOutputLimitEnabled(!output.OutputLimitEnabled);
                    UpdateProductionSettingsPanel(true);
                },
                output.OutputLimitEnabled);
            if (output.OutputLimitEnabled)
            {
                CreateProductionActionRow(
                    card.transform,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_LIMIT),
                        GameUtil.GetFormattedJoules(Mathf.Max(0f, output.OutputLimitUsedJoules), "F1", GameUtil.TimeSlice.None),
                        GameUtil.GetFormattedJoules(Mathf.Max(0f, output.OutputLimitJoules), "F1", GameUtil.TimeSlice.None)),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SET_LIMIT),
                    () => ShowPowerOutputLimitDialog(output));
            }
            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_OUTPUT_RATE),
                GetPowerOutputRateName(output),
                () => ShowPowerOutputRateDialog(output));
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_REQUEST_DESC));
        }

        private void AddParticleInputPortSettingsCard(Storage storage, StorageNetworkParticleInputPortIngress input)
        {
            GameObject card = CreateProductionCard("ParticleInputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_INPUT_PORT_STORE_TITLE), 0f);
            MakeProductionCardAutoHeight(card, 142f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = input.InputStoreEnabled;

            CreateStatusStrip(
                card.transform,
                enabled && online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : online ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                enabled && online ? new Color(0.28f, 0.48f, 0.34f, 1f) : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            CreateToggleActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_INPUT_PORT_STORE_ENABLED),
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    input.InputStoreEnabled = !enabled;
                    UpdateProductionSettingsPanel(true);
                },
                enabled);
            CreateProductionReadOnlyRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_STORAGE_TITLE), GetParticleNetworkStorageName(storage));
            CreateProductionReadOnlyRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_INPUT_PORT_MODE), Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_CAPTURE_MODE));
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_INPUT_PORT_STORE_DESC));
        }

        private void AddParticleOutputPortSettingsCard(Storage storage, StorageNetworkParticleOutputPortEgress output)
        {
            GameObject card = CreateProductionCard("ParticleOutputPortSettingsCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_REQUEST_TITLE), 0f);
            MakeProductionCardAutoHeight(card, output.OutputLimitEnabled ? 300f : 264f);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(GetStorageWorldId(storage));
            bool enabled = output.OutputRequestEnabled;
            bool ready = enabled && online && output.AvailableParticles >= output.ParticleThreshold && !output.OutputLimitReached;

            CreateStatusStrip(
                card.transform,
                ready
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED)
                    : online ? enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_SHORT_IDLE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE),
                ready
                    ? new Color(0.28f, 0.48f, 0.34f, 1f)
                    : online ? new Color(0.50f, 0.42f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            CreateToggleActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_REQUEST_ENABLED),
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    output.OutputRequestEnabled = !enabled;
                    UpdateProductionSettingsPanel(true);
                },
                enabled);
            CreateProductionReadOnlyRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_STORAGE_TITLE), GetParticleNetworkStorageName(storage));
            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SOURCE_POLICY),
                GetParticleOutputSourceModeName(output),
                () => ShowParticleOutputSourcePicker(storage, output));
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_THRESHOLD), GetParticleThresholdName(output), () => ShowParticleThresholdDialog(output));
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_DIRECTION), GetParticleDirectionName(output.Direction), () => ShowParticleDirectionPicker(output));
            CreateToggleActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_LIMIT_ENABLED),
                output.OutputLimitEnabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON),
                () =>
                {
                    output.SetOutputLimitEnabled(!output.OutputLimitEnabled);
                    UpdateProductionSettingsPanel(true);
                },
                output.OutputLimitEnabled);
            if (output.OutputLimitEnabled)
            {
                CreateProductionActionRow(
                    card.transform,
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_LIMIT), FormatParticles(output.OutputLimitUsedParticles), FormatParticles(output.OutputLimitParticles)),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SET_LIMIT),
                    () => ShowParticleOutputLimitDialog(output));
            }
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_REQUEST_DESC));
        }

        private static int GetStorageWorldId(Storage storage)
        {
            if (storage == null)
            {
                return -1;
            }

            int worldId = storage.gameObject.GetMyWorldId();
            if (worldId != byte.MaxValue && worldId >= 0)
            {
                return worldId;
            }

            int cell = Grid.PosToCell(storage.gameObject);
            return Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
        }

        private static string GetPortStatusName(Storage storage)
        {
            if (StorageNetworkStorageRules.IsSolidInputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_PORT_INPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsSolidOutputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_PORT_OUTPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsLiquidInputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.LIQUID_PORT_INPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsLiquidOutputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.LIQUID_PORT_OUTPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsGasInputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_PORT_INPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsGasOutputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_PORT_OUTPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsPowerInputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_INPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsPowerOutputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_OUTPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsParticleInputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_INPUT_STATUS);
            }

            if (StorageNetworkStorageRules.IsParticleOutputPort(storage))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_OUTPUT_STATUS);
            }

            return storage != null ? storage.GetProperName() : string.Empty;
        }

        private static string GetInputPortStoreModeName(StorageNetworkLiquidInputPortIngress ingress)
        {
            if (ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = ingress.ResolveInputStorage();
                return target != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private static string GetInputPortStoreModeName(StorageNetworkGasInputPortIngress ingress)
        {
            if (ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = ingress.ResolveInputStorage();
                return target != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private static string GetInputPortStoreModeName(StorageNetworkSolidInputPortIngress ingress)
        {
            if (ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = ingress.ResolveInputStorage();
                return target != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private static string GetOutputPortSourceModeName(StorageNetworkLiquidOutputPortEgress egress)
        {
            if (egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = egress.ResolveSourceStorage();
                return source != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        private static string GetOutputPortSourceModeName(StorageNetworkGasOutputPortEgress egress)
        {
            if (egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = egress.ResolveSourceStorage();
                return source != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        private static string GetOutputPortSourceModeName(StorageNetworkSolidOutputPortEgress egress)
        {
            if (egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = egress.ResolveSourceStorage();
                return source != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        private static string GetOutputPortFilterName(StorageNetworkLiquidOutputPortEgress egress)
        {
            SimHashes? selected = egress.GetSelectedOutputElement();
            if (!selected.HasValue)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_ANY);
            }

            Element element = ElementLoader.FindElementByHash(selected.Value);
            return element != null ? element.name : selected.Value.ToString();
        }

        private static string GetOutputPortFilterName(StorageNetworkGasOutputPortEgress egress)
        {
            SimHashes? selected = egress.GetSelectedOutputElement();
            if (!selected.HasValue)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_FILTER_ANY);
            }

            Element element = ElementLoader.FindElementByHash(selected.Value);
            return element != null ? element.name : selected.Value.ToString();
        }

        private static string GetOutputPortFilterName(StorageNetworkSolidOutputPortEgress egress)
        {
            Tag? selected = egress.GetSelectedOutputTag();
            return selected.HasValue && selected.Value != Tag.Invalid
                ? StorageItemUtility.GetTagDisplayName(selected.Value)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_FILTER_ANY);
        }

        private static string GetOutputPortRequestRateName(StorageNetworkLiquidOutputPortEgress egress)
        {
            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE_VALUE),
                GameUtil.GetFormattedMass(egress.GetRequestRateKgPerSecond()));
        }

        private static string GetOutputPortRequestRateName(StorageNetworkGasOutputPortEgress egress)
        {
            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE_VALUE),
                GameUtil.GetFormattedMass(egress.GetRequestRateKgPerSecond()));
        }

        private static string GetOutputPortRequestRateName(StorageNetworkSolidOutputPortEgress egress)
        {
            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE_VALUE),
                GameUtil.GetFormattedMass(egress.GetRequestRateKgPerSecond()));
        }

        private static string GetPowerInputRateName(StorageNetworkPowerInputPortConsumer input)
        {
            return FormatPowerRate(input.GetInputWattsSetting());
        }

        private static string GetPowerInputStoreModeName(StorageNetworkPowerInputPortConsumer input)
        {
            if (input.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = input.ResolveInputStorage();
                return target != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private static string GetPowerOutputRateName(StorageNetworkPowerOutputPortGenerator output)
        {
            return FormatPowerRate(output.GetOutputWattsSetting());
        }

        private static string GetPowerOutputSourceModeName(StorageNetworkPowerOutputPortGenerator output)
        {
            if (output.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = output.ResolveSourceStorage();
                return source != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        private static string GetParticleOutputSourceModeName(StorageNetworkParticleOutputPortEgress output)
        {
            if (output.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = output.ResolveSourceStorage();
                return source != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        private static string FormatPowerRate(float watts)
        {
            return GameUtil.GetFormattedWattage(watts, GameUtil.WattageFormatterUnit.Automatic, true);
        }

        private static string GetParticleNetworkStorageName(Storage storage)
        {
            StorageNetworkParticleOutputPortEgress output = storage != null
                ? storage.GetComponent<StorageNetworkParticleOutputPortEgress>()
                : null;
            Storage source = output != null && output.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? output.ResolveSourceStorage()
                : null;
            return string.Format(
                "{0} / {1}",
                FormatParticles(StorageNetworkParticleStorageService.GetAvailable(storage != null ? storage.gameObject : null, source)),
                FormatParticles(StorageNetworkParticleStorageService.GetCapacity(storage != null ? storage.gameObject : null, source)));
        }

        private static string GetParticleThresholdName(StorageNetworkParticleOutputPortEgress output)
        {
            return FormatParticles(output != null ? output.ParticleThreshold : 0f);
        }

        private static string FormatParticles(float particles)
        {
            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_AMOUNT_VALUE), Mathf.FloorToInt(Mathf.Max(0f, particles)));
        }

        private static string GetParticleDirectionName(EightDirection direction)
        {
            switch (direction)
            {
                case EightDirection.Up:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_DIRECTION_UP);
                case EightDirection.Down:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_DIRECTION_DOWN);
                case EightDirection.Left:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_DIRECTION_LEFT);
                case EightDirection.Right:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_DIRECTION_RIGHT);
                case EightDirection.UpLeft:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_DIRECTION_UP_LEFT);
                case EightDirection.UpRight:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_DIRECTION_UP_RIGHT);
                case EightDirection.DownLeft:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_DIRECTION_DOWN_LEFT);
                case EightDirection.DownRight:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_DIRECTION_DOWN_RIGHT);
                default:
                    return direction.ToString();
            }
        }

        private static string GetPortFilterSummary(Storage storage)
        {
            List<Tag> filters = storage?.storageFilters;
            if (filters == null || filters.Count == 0)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_FILTERS_ANY);
            }

            string summary = string.Join(", ", filters
                .Where(tag => tag != Tag.Invalid)
                .Take(4)
                .Select(tag => tag.ProperName()));
            if (string.IsNullOrEmpty(summary))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_STATUS_FILTERS_EMPTY);
            }

            int remaining = filters.Count(tag => tag != Tag.Invalid) - 4;
            return remaining > 0 ? string.Format("{0} +{1}", summary, remaining) : summary;
        }
    }
}
