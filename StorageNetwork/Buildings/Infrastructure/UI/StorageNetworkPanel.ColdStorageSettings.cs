using System.Globalization;
using StorageNetwork.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void AddColdStorageSettingsCard(Storage storage, StorageNetworkColdStorageCooling cooling)
        {
            GameObject card = CreateProductionCard("ColdStorageCoolingCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_TITLE), 0f);
            MakeProductionCardAutoHeight(card, 156f);

            CreateProductionReadOnlyRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_CURRENT),
                cooling.GetFormattedTargetTemperature());

            KSlider slider = CreateColdStorageCoolingSliderRow(card.transform, cooling.TargetTemperature, out KInputTextField input);
            bool updating = false;

            void SetCooling(float value, bool refreshPanel)
            {
                float normalized = StorageNetworkColdStorageCooling.NormalizeDisplayTemperature(value);
                cooling.TargetTemperature = GameUtil.GetTemperatureConvertedToKelvin(normalized);
                if (!updating)
                {
                    updating = true;
                    slider.value = normalized;
                    input.text = normalized.ToString("0.##", CultureInfo.InvariantCulture);
                    updating = false;
                }

                if (refreshPanel)
                {
                    UpdateProductionSettingsPanel(true);
                }
            }

            slider.onValueChanged.AddListener(value =>
            {
                if (!updating)
                {
                    SetCooling(value, refreshPanel: false);
                }
            });

            input.onEndEdit.AddListener(text =>
            {
                if (updating)
                {
                    return;
                }

                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) &&
                    !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    value = GameUtil.GetConvertedTemperature(cooling.TargetTemperature, true);
                }

                SetCooling(value, refreshPanel: true);
            });

            CreateProductionActionRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_DEFAULT),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RESET_DEFAULTS),
                () =>
                {
                    cooling.ResetToDefault();
                    UpdateProductionSettingsPanel(true);
                });

            CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_DESC));
        }

        private KSlider CreateColdStorageCoolingSliderRow(Transform parent, float value, out KInputTextField input)
        {
            GameObject row = CreatePlainImage("ColdStorageCoolingRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 44f;
            rowLayout.preferredHeight = 44f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI label = CreateText("CoolingSliderLabel", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_AMOUNT), 11, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 82f;

            KSlider slider = CreateAmountSlider(row.transform, GameUtil.GetConvertedTemperature(StorageNetworkColdStorageCooling.GetMaxTargetTemperature(), true));
            LayoutElement sliderLayout = slider.gameObject.GetComponent<LayoutElement>();
            if (sliderLayout != null)
            {
                sliderLayout.minWidth = 455f;
                sliderLayout.preferredWidth = 455f;
                sliderLayout.preferredHeight = 32f;
                sliderLayout.flexibleWidth = 0f;
            }

            input = CreateFixedTextInput(
                row.transform,
                "ColdStorageCoolingInput",
                GameUtil.GetConvertedTemperature(value, true).ToString("0.##", CultureInfo.InvariantCulture),
                145f,
                24f,
                11);

            slider.minValue = GameUtil.GetConvertedTemperature(StorageNetworkColdStorageCooling.GetMinTargetTemperature(), true);
            slider.maxValue = GameUtil.GetConvertedTemperature(StorageNetworkColdStorageCooling.GetMaxTargetTemperature(), true);
            slider.wholeNumbers = true;
            slider.value = GameUtil.GetConvertedTemperature(value, true);
            return slider;
        }
    }
}
