using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private void ShowGeyserSettingsPanel(Geyser geyser)
        {
            if (geyser == null)
            {
                return;
            }

            CloseProductionSettingsPanel();
            CloseModal();
            geyserSettingsGeyser = geyser;
            productionSettingsStorage = null;
            productionSettingsMinion = null;
            productionSettingsSignature = null;
            EnsureProductionSettingsPanel();
            productionSettingsRoot.SetActive(true);
            KeepProductionSettingsPanelOnScreen();
            UpdateGeyserSettingsPanel(true);
        }

        private void CloseGeyserSettingsPanel()
        {
            if (geyserSettingsGeyser != null)
            {
                CloseProductionSettingsPanel();
            }
        }

        private void EnsureGeyserSettingsPanel()
        {
            EnsureProductionSettingsPanel();
            geyserSettingsRoot = productionSettingsRoot;
            geyserSettingsContent = productionSettingsContent;
        }

        private void KeepGeyserSettingsPanelOnScreen()
        {
            KeepProductionSettingsPanelOnScreen();
        }

        private void UpdateGeyserSettingsPanel(bool force = false)
        {
            if (geyserSettingsGeyser == null)
            {
                return;
            }

            EnsureGeyserSettingsPanel();
            if (productionSettingsRoot == null || !productionSettingsRoot.activeSelf || productionSettingsContent == null)
            {
                return;
            }

            Geyser geyser = geyserSettingsGeyser;
            StorageNetworkEnrollment enrollment = geyser != null ? geyser.GetComponent<StorageNetworkEnrollment>() : null;
            if (geyser == null || enrollment == null)
            {
                CloseProductionSettingsPanel();
                return;
            }

            string signature = string.Join(
                "~",
                geyser.GetInstanceID().ToString(),
                enrollment.IncludedInSceneNetwork ? "in1" : "in0",
                enrollment.DirectGeyserOutputToNetwork ? "direct1" : "direct0",
                enrollment.GeyserOutputStoreModeValue.ToString(),
                enrollment.GeyserOutputStorageInstanceId.ToString());
            if (!force && signature == geyserSettingsSignature)
            {
                SetGeyserSettingsTitle(geyser);
                return;
            }

            geyserSettingsSignature = signature;
            ClearGeyserSettingsContent();
            SetGeyserSettingsTitle(geyser);
            KeepGeyserSettingsPanelOnScreen();
            AddGeyserOverviewCard(geyser, enrollment);
            AddGeyserAutomationCards(geyser, enrollment);
            AddGeyserOutputCard(geyser);
            LayoutRebuilder.MarkLayoutForRebuild(productionSettingsContent);
        }

        private void ClearGeyserSettingsContent()
        {
            for (int i = productionSettingsContent.childCount - 1; i >= 0; i--)
            {
                Destroy(productionSettingsContent.GetChild(i).gameObject);
            }
        }

        private void SetGeyserSettingsTitle(Geyser geyser)
        {
            TextMeshProUGUI title = productionSettingsRoot.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.name == "ProductionSettingsTitle");
            if (title != null)
            {
                title.text = geyser.GetProperName() + "\n" + GetGeyserDetails(geyser);
            }
        }

        private void AddGeyserOverviewCard(Geyser geyser, StorageNetworkEnrollment enrollment)
        {
            GameObject card = CreateProductionCard(productionSettingsContent, "GeyserOverviewCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_TITLE), 116f);
            TextMeshProUGUI title = CreateText("BuildingName", card.transform, geyser.GetProperName(), 16, TextAlignmentOptions.MidlineLeft);
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

            CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_METRIC_OUTPUT), GetGeyserElementName(geyser), new Color(0.35f, 0.40f, 0.43f, 1f));
            CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_METRIC_RATE), GetGeyserAverageRate(geyser), new Color(0.35f, 0.40f, 0.43f, 1f));
            CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_NETWORK), enrollment.IncludedInSceneNetwork ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_STATUS) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_STATUS), enrollment.IncludedInSceneNetwork ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.50f, 0.42f, 0.34f, 1f));
            CreateMetricTile(metrics.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_METRIC_DIRECT_OUTPUT), enrollment.DirectGeyserOutputToNetwork ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED), enrollment.DirectGeyserOutputToNetwork ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.50f, 0.42f, 0.34f, 1f));
        }

        private void AddGeyserAutomationCards(Geyser geyser, StorageNetworkEnrollment enrollment)
        {
            GameObject grid = new GameObject("AutomationGrid");
            grid.transform.SetParent(productionSettingsContent, false);
            grid.AddComponent<RectTransform>();
            bool compact = productionSettingsRoot != null && productionSettingsRoot.GetComponent<RectTransform>().rect.width < 620f;
            LayoutElement gridLayout = grid.AddComponent<LayoutElement>();
            gridLayout.minHeight = compact ? 180f : 150f;
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

            AddGeyserNetworkCard(grid.transform, geyser, enrollment);
        }

        private void AddGeyserNetworkCard(Transform parent, Geyser geyser, StorageNetworkEnrollment enrollment)
        {
            GameObject card = CreateProductionCard(parent, "GeyserNetworkCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_OUTPUT_STORE_TITLE), 0f);
            ApplyEqualAutomationCardLayout(card);
            CreateStatusStrip(card.transform, enrollment.DirectGeyserOutputToNetwork ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_OUTPUT_DIRECT_STATUS) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_OUTPUT_WORLD_STATUS), enrollment.DirectGeyserOutputToNetwork ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.48f, 0.45f, 0.36f, 1f));
            CreateToggleActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_DIRECT_OUTPUT_ENABLED), enrollment.DirectGeyserOutputToNetwork ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ACTION_CLOSE) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON), () =>
            {
                enrollment.SetDirectGeyserOutputToNetwork(!enrollment.DirectGeyserOutputToNetwork);
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                UpdateGeyserSettingsPanel(true);
            }, enrollment.DirectGeyserOutputToNetwork);
            CreateProductionActionRow(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_POLICY), GetGeyserOutputStoreModeName(enrollment), () => ShowGeyserOutputStorePicker(geyser, enrollment));
            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_DIRECT_OUTPUT_DESC));
        }

        private void ShowGeyserOutputStorePicker(Geyser geyser, StorageNetworkEnrollment enrollment)
        {
            System.Collections.Generic.List<ProductionPickerOption> options = new System.Collections.Generic.List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC),
                    enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork,
                    () =>
                    {
                        enrollment.UseAutomaticGeyserOutputStorage();
                        CloseProductionPicker();
                        UpdateGeyserSettingsPanel(true);
                    })
            };

            foreach (Storage target in GetNetworkStorageTargets(null))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage && enrollment.ResolveGeyserOutputStorage() == captured,
                    () =>
                    {
                        enrollment.SetGeyserOutputStorage(captured);
                        CloseProductionPicker();
                        UpdateGeyserSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC), options);
        }

        private void AddGeyserOutputCard(Geyser geyser)
        {
            GameObject card = CreateProductionCard(productionSettingsContent, "GeyserOutputCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_OUTPUT_CONTENT_TITLE), 82f);
            GameObject row = CreatePlainImage("GeyserOutputRow", card.transform, new Color(0.76f, 0.76f, 0.70f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 30f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 2, 2);
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
            SetGeyserElementIcon(icon, geyser);

            TextMeshProUGUI name = CreateText("Name", row.transform, GetGeyserElementName(geyser), 11, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI mass = CreateText("Rate", row.transform, GetGeyserAverageRate(geyser), 11, TextAlignmentOptions.MidlineRight);
            mass.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            mass.textWrappingMode = TextWrappingModes.NoWrap;
            mass.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
        }

        private static string GetGeyserElementName(Geyser geyser)
        {
            if (geyser == null || geyser.configuration == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_UNKNOWN);
            }

            Element element = ElementLoader.FindElementByHash(geyser.configuration.GetElement());
            string elementName = element != null ? element.name : geyser.configuration.GetElement().CreateTag().ProperName();
            return StorageNetworkTextFormatting.StripKleiLinkFormatting(elementName);
        }

        private static string GetGeyserAverageRate(Geyser geyser)
        {
            return geyser != null && geyser.configuration != null
                ? GameUtil.GetFormattedMass(geyser.configuration.GetAverageEmission(), GameUtil.TimeSlice.PerSecond, GameUtil.MetricMassFormat.UseThreshold, true, "{0:0.#}")
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_UNKNOWN);
        }

        private static string GetGeyserOutputStoreModeName(StorageNetworkEnrollment enrollment)
        {
            if (enrollment != null && enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = enrollment.ResolveGeyserOutputStorage();
                return target != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private static void SetGeyserElementIcon(Image icon, Geyser geyser)
        {
            if (icon == null || geyser == null || geyser.configuration == null)
            {
                return;
            }

            var uiSprite = Def.GetUISprite(geyser.configuration.GetElement().CreateTag(), "ui", false);
            icon.sprite = uiSprite.first;
            icon.color = uiSprite.first != null ? uiSprite.second : Color.clear;
        }
    }
}
