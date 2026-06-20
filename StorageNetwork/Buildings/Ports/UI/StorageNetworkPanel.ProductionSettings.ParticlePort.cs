using System.Collections.Generic;
using StorageNetwork.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void ShowParticleDirectionPicker(StorageNetworkParticleOutputPortEgress output)
        {
            if (output == null)
            {
                return;
            }

            List<ProductionPickerOption> options = new List<ProductionPickerOption>();
            AddParticleDirectionOption(options, output, EightDirection.Up);
            AddParticleDirectionOption(options, output, EightDirection.UpRight);
            AddParticleDirectionOption(options, output, EightDirection.Right);
            AddParticleDirectionOption(options, output, EightDirection.DownRight);
            AddParticleDirectionOption(options, output, EightDirection.Down);
            AddParticleDirectionOption(options, output, EightDirection.DownLeft);
            AddParticleDirectionOption(options, output, EightDirection.Left);
            AddParticleDirectionOption(options, output, EightDirection.UpLeft);
            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_SELECT_DIRECTION), options);
        }

        private void AddParticleDirectionOption(List<ProductionPickerOption> options, StorageNetworkParticleOutputPortEgress output, EightDirection direction)
        {
            options.Add(new ProductionPickerOption(
                GetParticleDirectionName(direction),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_DIRECTION_DESC),
                output.Direction == direction,
                () =>
                {
                    output.Direction = direction;
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                }));
        }

        private void ShowParticleThresholdDialog(StorageNetworkParticleOutputPortEgress output)
        {
            if (output == null)
            {
                return;
            }

            ShowParticleAmountDialog(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_SET_THRESHOLD),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_THRESHOLD),
                output.ParticleThreshold,
                StorageNetworkParticleOutputPortEgress.MinThresholdParticles,
                StorageNetworkParticleOutputPortEgress.MaxThresholdParticles,
                StorageNetworkParticleOutputPortEgress.DefaultThresholdParticles,
                value => output.SetParticleThreshold(value),
                null,
                () => output.AvailableParticles,
                () => output.ParticleThreshold);
        }

        private void ShowParticleOutputLimitDialog(StorageNetworkParticleOutputPortEgress output)
        {
            if (output == null)
            {
                return;
            }

            ShowParticleAmountDialog(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_SET_LIMIT),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_LIMIT_LABEL),
                output.OutputLimitParticles <= 0f ? StorageNetworkParticleOutputPortEgress.DefaultOutputLimitParticles : output.OutputLimitParticles,
                StorageNetworkParticleOutputPortEgress.MinOutputLimitParticles,
                StorageNetworkParticleOutputPortEgress.MaxOutputLimitParticles,
                StorageNetworkParticleOutputPortEgress.DefaultOutputLimitParticles,
                value =>
                {
                    output.OutputLimitParticles = value;
                    output.OutputLimitEnabled = true;
                },
                output.ResetOutputLimitUsed,
                () => output.OutputLimitUsedParticles,
                () => output.OutputLimitParticles);
        }

        private void ShowParticleAmountDialog(
            string title,
            string label,
            float currentValue,
            float minValue,
            float maxValue,
            float defaultValue,
            System.Action<float> applyValue,
            System.Action resetUsed,
            System.Func<float> getUsed,
            System.Func<float> getLimit)
        {
            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null || applyValue == null)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("ParticleAmountPicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 84f, 84f, 118f, 96f);

            GameObject header = CreatePlainImage("AmountHeader", productionPickerRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 34f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("AmountTitle", header.transform, title, 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("AmountClose", header.transform, CloseProductionPicker);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject body = CreatePlainImage("AmountBody", productionPickerRoot.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            VerticalLayoutGroup bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(8, 8, 8, 8);
            bodyLayout.spacing = 6f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            if (getUsed != null && getLimit != null)
            {
                CreateLimitInfoRow(
                    body.transform,
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_LIMIT), FormatParticles(getUsed()), FormatParticles(getLimit())),
                    () =>
                    {
                        resetUsed?.Invoke();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    });
            }

            KSlider slider = CreateLimitAmountRow(body.transform, currentValue, minValue, maxValue, out KInputTextField input, label);
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
                slider.value = StorageNetworkMaterialLimitRules.ParseInput(value, slider.value, minValue, maxValue);
                syncingControls = false;
            });

            GameObject buttonRow = new GameObject("AmountButtonRow");
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
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_DEFAULT), () =>
            {
                input.text = Mathf.RoundToInt(defaultValue).ToString();
                slider.value = defaultValue;
            }, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), CloseProductionPicker, KleiBlueStyle());
            CreateLimitDialogButton(buttonRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                applyValue(StorageNetworkMaterialLimitRules.ParseInput(input.text, currentValue, minValue, maxValue));
                CloseProductionPicker();
                UpdateProductionSettingsPanel(true);
            }, KleiPinkStyle());
        }
    }
}
