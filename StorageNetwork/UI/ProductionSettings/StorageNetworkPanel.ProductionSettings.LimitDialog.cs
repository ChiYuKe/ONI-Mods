using StorageNetwork.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void ShowMaterialRequestLimitDialog(StorageNetworkMaterialRequester requester)
        {
            if (requester == null)
            {
                return;
            }

            ShowMaterialLimitDialog(
                () => requester.GetRequestedAmountForDisplay(),
                () => requester.LimitKg,
                () => requester.ResetRequestedAmount(),
                value =>
                {
                    requester.LimitKg = value;
                    requester.LimitEnabled = true;
                });
        }

        private void ShowEnergyGeneratorMaterialRequestLimitDialog(StorageNetworkEnergyGeneratorRequester requester)
        {
            if (requester == null)
            {
                return;
            }

            ShowMaterialLimitDialog(
                () => requester.GetRequestedAmountForDisplay(),
                () => requester.LimitKg,
                () => requester.ResetRequestedAmount(),
                value =>
                {
                    requester.LimitKg = value;
                    requester.LimitEnabled = true;
                });
        }

        private void ShowOutputPortLimitDialog(Storage storage, StorageNetworkLiquidOutputPortEgress egress)
        {
            if (storage == null || egress == null)
            {
                return;
            }

            ShowMaterialLimitDialog(
                () => egress.OutputLimitUsedKg,
                () => egress.OutputLimitKg,
                () => egress.ResetOutputLimitUsed(),
                value =>
                {
                    egress.OutputLimitKg = value;
                    egress.OutputLimitEnabled = true;
                });
        }

        private void ShowOutputPortLimitDialog(Storage storage, StorageNetworkGasOutputPortEgress egress)
        {
            if (storage == null || egress == null)
            {
                return;
            }

            ShowMaterialLimitDialog(
                () => egress.OutputLimitUsedKg,
                () => egress.OutputLimitKg,
                () => egress.ResetOutputLimitUsed(),
                value =>
                {
                    egress.OutputLimitKg = value;
                    egress.OutputLimitEnabled = true;
                });
        }

        private void ShowOutputPortLimitDialog(Storage storage, StorageNetworkSolidOutputPortEgress egress)
        {
            if (storage == null || egress == null)
            {
                return;
            }

            ShowMaterialLimitDialog(
                () => egress.OutputLimitUsedKg,
                () => egress.OutputLimitKg,
                () => egress.ResetOutputLimitUsed(),
                value =>
                {
                    egress.OutputLimitKg = value;
                    egress.OutputLimitEnabled = true;
                });
        }

        private void ShowOutputPortRequestRateDialog(StorageNetworkLiquidOutputPortEgress egress)
        {
            if (egress == null)
            {
                return;
            }

            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("OutputPortRatePicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 84f, 84f, 138f, 116f);

            GameObject header = CreatePlainImage("RateHeader", productionPickerRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 34f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("RateTitle", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SET_REQUEST_RATE), 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("RateClose", header.transform, CloseProductionPicker);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject body = CreatePlainImage("RateBody", productionPickerRoot.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(8, 8, 8, 8);
            bodyLayout.spacing = 6f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            CreateLimitInfoRow(
                body.transform,
                string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE_VALUE),
                    GameUtil.GetFormattedMass(egress.GetRequestRateKgPerSecond())),
                () =>
                {
                    egress.SetRequestRateKgPerSecond(StorageNetworkLiquidOutputPortEgress.DefaultRequestRateKgPerSecond);
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                });

            float currentRate = egress.GetRequestRateKgPerSecond();
            KSlider slider = CreateLimitAmountRow(
                body.transform,
                currentRate,
                StorageNetworkLiquidOutputPortEgress.MinRequestRateKgPerSecond,
                StorageNetworkLiquidOutputPortEgress.GetMaxRequestRateKgPerSecond(),
                out KInputTextField input,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE));
            input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.inputType = TMP_InputField.InputType.Standard;

            bool syncingControls = false;
            slider.onValueChanged.AddListener(value =>
            {
                if (syncingControls)
                {
                    return;
                }

                syncingControls = true;
                input.text = value.ToString("0.###");
                syncingControls = false;
            });
            input.onValueChanged.AddListener(value =>
            {
                if (syncingControls)
                {
                    return;
                }

                syncingControls = true;
                slider.value = StorageNetworkMaterialLimitRules.ParseInput(
                    value,
                    slider.value,
                    StorageNetworkLiquidOutputPortEgress.MinRequestRateKgPerSecond,
                    StorageNetworkLiquidOutputPortEgress.GetMaxRequestRateKgPerSecond());
                syncingControls = false;
            });

            GameObject buttonRow = new GameObject("RateButtonRow");
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
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), CloseProductionPicker, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                egress.SetRequestRateKgPerSecond(StorageNetworkMaterialLimitRules.ParseInput(
                    input.text,
                    currentRate,
                    StorageNetworkLiquidOutputPortEgress.MinRequestRateKgPerSecond,
                    StorageNetworkLiquidOutputPortEgress.GetMaxRequestRateKgPerSecond()));
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            }, KleiPinkStyle());
        }

        private void ShowOutputPortRequestRateDialog(StorageNetworkGasOutputPortEgress egress)
        {
            if (egress == null)
            {
                return;
            }

            ShowOutputPortRequestRateDialog(
                egress.GetRequestRateKgPerSecond,
                egress.SetRequestRateKgPerSecond,
                StorageNetworkGasOutputPortEgress.DefaultRequestRateKgPerSecond,
                StorageNetworkGasOutputPortEgress.MinRequestRateKgPerSecond,
                StorageNetworkGasOutputPortEgress.GetMaxRequestRateKgPerSecond());
        }

        private void ShowOutputPortRequestRateDialog(StorageNetworkSolidOutputPortEgress egress)
        {
            if (egress == null)
            {
                return;
            }

            ShowOutputPortRequestRateDialog(
                egress.GetRequestRateKgPerSecond,
                egress.SetRequestRateKgPerSecond,
                StorageNetworkSolidOutputPortEgress.DefaultRequestRateKgPerSecond,
                StorageNetworkSolidOutputPortEgress.MinRequestRateKgPerSecond,
                StorageNetworkSolidOutputPortEgress.GetMaxRequestRateKgPerSecond());
        }

        private void ShowOutputPortRequestRateDialog(
            System.Func<float> getRate,
            System.Action<float> setRate,
            float defaultRate,
            float minRate,
            float maxRate)
        {
            if (getRate == null || setRate == null)
            {
                return;
            }

            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("OutputPortRatePicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 84f, 84f, 138f, 116f);

            GameObject header = CreatePlainImage("RateHeader", productionPickerRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 34f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("RateTitle", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_SET_REQUEST_RATE), 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("RateClose", header.transform, CloseProductionPicker);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject body = CreatePlainImage("RateBody", productionPickerRoot.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(8, 8, 8, 8);
            bodyLayout.spacing = 6f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            CreateLimitInfoRow(body.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE_VALUE), GameUtil.GetFormattedMass(getRate())), () =>
            {
                setRate(defaultRate);
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            });

            float currentRate = getRate();
            KSlider slider = CreateLimitAmountRow(body.transform, currentRate, minRate, maxRate, out KInputTextField input, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_REQUEST_RATE));
            input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.inputType = TMP_InputField.InputType.Standard;

            bool syncingControls = false;
            slider.onValueChanged.AddListener(value =>
            {
                if (syncingControls)
                {
                    return;
                }

                syncingControls = true;
                input.text = value.ToString("0.###");
                syncingControls = false;
            });
            input.onValueChanged.AddListener(value =>
            {
                if (syncingControls)
                {
                    return;
                }

                syncingControls = true;
                slider.value = StorageNetworkMaterialLimitRules.ParseInput(value, slider.value, minRate, maxRate);
                syncingControls = false;
            });

            GameObject buttonRow = new GameObject("RateButtonRow");
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
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), CloseProductionPicker, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                setRate(StorageNetworkMaterialLimitRules.ParseInput(input.text, currentRate, minRate, maxRate));
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            }, KleiPinkStyle());
        }

        private void ShowPowerInputRateDialog(StorageNetworkPowerInputPortConsumer input)
        {
            if (input == null)
            {
                return;
            }

            ShowPowerPortRateDialog(
                input.GetInputWattsSetting,
                input.SetInputWatts,
                StorageNetworkPowerInputPortConsumer.DefaultInputWatts,
                StorageNetworkPowerInputPortConsumer.MinInputWatts,
                StorageNetworkPowerInputPortConsumer.GetMaxInputWatts(),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_INPUT_RATE));
        }

        private void ShowPowerOutputLimitDialog(StorageNetworkPowerOutputPortGenerator output)
        {
            if (output == null)
            {
                return;
            }

            ShowPowerLimitDialog(
                () => output.OutputLimitUsedJoules,
                () => output.OutputLimitJoules,
                output.ResetOutputLimitUsed,
                value =>
                {
                    output.OutputLimitJoules = value;
                    output.OutputLimitEnabled = true;
                });
        }

        private void ShowPowerOutputRateDialog(StorageNetworkPowerOutputPortGenerator output)
        {
            if (output == null)
            {
                return;
            }

            ShowPowerPortRateDialog(
                output.GetOutputWattsSetting,
                output.SetOutputWatts,
                StorageNetworkPowerOutputPortGenerator.DefaultOutputWatts,
                StorageNetworkPowerOutputPortGenerator.MinOutputWatts,
                StorageNetworkPowerOutputPortGenerator.GetMaxOutputWatts(),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_OUTPUT_RATE));
        }

        private void ShowPowerLimitDialog(System.Func<float> getUsedJoules, System.Func<float> getLimitJoules, System.Action resetUsed, System.Action<float> applyLimit)
        {
            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null || getUsedJoules == null || getLimitJoules == null || resetUsed == null || applyLimit == null)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("PowerLimitPicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
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

            TextMeshProUGUI headerText = CreateText("LimitTitle", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_SET_LIMIT), 12, TextAlignmentOptions.MidlineLeft);
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
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_LIMIT),
                GameUtil.GetFormattedJoules(Mathf.Max(0f, getUsedJoules()), "F1", GameUtil.TimeSlice.None),
                GameUtil.GetFormattedJoules(Mathf.Max(0f, getLimitJoules()), "F1", GameUtil.TimeSlice.None)),
                () =>
                {
                    resetUsed();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                });

            float currentLimit = Mathf.Clamp(getLimitJoules() <= 0f ? StorageNetworkPowerOutputPortGenerator.DefaultOutputWatts : getLimitJoules(), 1f, 1000000f);
            KSlider slider = CreateLimitAmountRow(body.transform, currentLimit, 1f, 1000000f, out KInputTextField input, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_LIMIT_LABEL));
            input.characterValidation = TMP_InputField.CharacterValidation.Integer;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.inputType = TMP_InputField.InputType.Standard;

            bool syncingControls = false;
            slider.onValueChanged.AddListener(value =>
            {
                if (syncingControls)
                {
                    return;
                }

                syncingControls = true;
                input.text = Mathf.RoundToInt(value).ToString();
                syncingControls = false;
            });
            input.onValueChanged.AddListener(value =>
            {
                if (syncingControls)
                {
                    return;
                }

                syncingControls = true;
                slider.value = StorageNetworkMaterialLimitRules.ParseInput(value, slider.value, 1f, 1000000f);
                syncingControls = false;
            });

            GameObject buttonRow = new GameObject("LimitButtonRow");
            buttonRow.transform.SetParent(body.transform, false);
            buttonRow.AddComponent<RectTransform>();
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 34f;
            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 6f;
            buttonLayout.childAlignment = TextAnchor.MiddleRight;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = false;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childForceExpandHeight = false;

            CreateFlexibleSpacer(buttonRow.transform);
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), CloseProductionPicker, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                applyLimit(StorageNetworkMaterialLimitRules.ParseInput(input.text, currentLimit, 1f, 1000000f));
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            }, KleiPinkStyle());
        }

        private void ShowPowerPortRateDialog(
            System.Func<float> getRate,
            System.Action<float> setRate,
            float defaultRate,
            float minRate,
            float maxRate,
            string label)
        {
            if (getRate == null || setRate == null)
            {
                return;
            }

            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("PowerPortRatePicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 84f, 84f, 138f, 116f);

            GameObject header = CreatePlainImage("RateHeader", productionPickerRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 34f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("RateTitle", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_SET_RATE), 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("RateClose", header.transform, CloseProductionPicker);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject body = CreatePlainImage("RateBody", productionPickerRoot.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(8, 8, 8, 8);
            bodyLayout.spacing = 6f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            CreateLimitInfoRow(body.transform, GameUtil.GetFormattedWattage(getRate(), GameUtil.WattageFormatterUnit.Automatic, true), () =>
            {
                setRate(defaultRate);
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            });

            float currentRate = getRate();
            KSlider slider = CreateLimitAmountRow(body.transform, currentRate, minRate, maxRate, out KInputTextField input, label);
            input.characterValidation = TMP_InputField.CharacterValidation.Integer;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.inputType = TMP_InputField.InputType.Standard;

            bool syncingControls = false;
            slider.onValueChanged.AddListener(value =>
            {
                if (syncingControls)
                {
                    return;
                }

                syncingControls = true;
                input.text = Mathf.RoundToInt(value).ToString();
                syncingControls = false;
            });
            input.onValueChanged.AddListener(value =>
            {
                if (syncingControls)
                {
                    return;
                }

                syncingControls = true;
                slider.value = StorageNetworkMaterialLimitRules.ParseInput(value, slider.value, minRate, maxRate);
                syncingControls = false;
            });

            GameObject buttonRow = new GameObject("RateButtonRow");
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
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), CloseProductionPicker, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                setRate(StorageNetworkMaterialLimitRules.ParseInput(input.text, currentRate, minRate, maxRate));
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            }, KleiPinkStyle());
        }

        private void ShowMaterialLimitDialog(System.Func<float> getRequestedAmount, System.Func<float> getLimitKg, System.Action resetRequestedAmount, System.Action<float> applyLimit)
        {
            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null || getRequestedAmount == null || getLimitKg == null || resetRequestedAmount == null || applyLimit == null)
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
                GameUtil.GetFormattedMass(Mathf.Max(0f, getRequestedAmount())),
                GameUtil.GetFormattedMass(Mathf.Max(0f, getLimitKg()))),
                () =>
                {
                    resetRequestedAmount();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                });

            float currentLimit = StorageNetworkMaterialLimitRules.GetCurrentLimitKg(getLimitKg());
            KSlider slider = CreateLimitAmountRow(body.transform, currentLimit, StorageNetworkMaterialLimitRules.MinLimitKg, StorageNetworkMaterialLimitRules.MaxLimitKg, out KInputTextField input);
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
                slider.value = StorageNetworkMaterialLimitRules.ParseInput(value, slider.value);
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
                input.text = StorageNetworkMaterialLimitRules.FormatMaxLimitInput();
                slider.value = StorageNetworkMaterialLimitRules.MaxLimitKg;
            }, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), CloseProductionPicker, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                applyLimit(StorageNetworkMaterialLimitRules.ParseInput(input.text, currentLimit));
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            }, KleiPinkStyle());
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

        private KSlider CreateLimitAmountRow(Transform parent, float value, float minValue, float maxValue, out KInputTextField input, string labelText = null)
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

            TextMeshProUGUI label = CreateText("SliderLabel", row.transform, labelText ?? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.AMOUNT_TITLE), 11, TextAlignmentOptions.MidlineLeft);
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

            input = CreateFixedTextInput(row.transform, "MaterialLimitInput", Mathf.Clamp(value, minValue, maxValue).ToString("0.###"), 170f, 24f, 11);

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
    }
}
