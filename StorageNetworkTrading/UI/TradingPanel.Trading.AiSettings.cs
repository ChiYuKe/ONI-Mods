using StorageNetwork.API;
using StorageNetwork.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetworkTrading.UI
{
    public sealed partial class TradingPanel
    {
        private GameObject tradingAiSettingsRoot;
        private static string AiText(string key) => TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING." + key);

        private void BuildTradingAiSettingsButton(Transform header)
        {
            Sprite sprite = Assets.GetSprite("icon_gear") ?? Assets.GetSprite("icon_settings");
            GameObject button = sprite != null ? CreateIconOnlyButton("AiSettings", header, sprite, ShowTradingAiSettings)
                : CreateGameButton("AiSettings", header, "AI", ShowTradingAiSettings);
            button.AddComponent<ToolTip>().toolTip = TradingText.Render(AiText("AI_TITLE"));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(28, 26);
            rect.anchoredPosition = new Vector2(-44, 0);
        }

        private void ShowTradingAiSettings()
        {
            if (!TradingAiFeatureGate.Enabled) return;
            if (modalRoot != null) modalRoot.SetActive(false);
            CloseModal();
            bool loaded = TradingAiSettingsStore.Local.Load(out TradingAiSettings settings);
            modalRoot = CreateAmountModalFrame(AiText("AI_TITLE"), 640, 442, out GameObject body);
            tradingAiSettingsRoot = modalRoot;
            KInputTextField provider = AddTradingAiInput(body.transform, "Provider", AiText("AI_PROVIDER"), settings.Provider, 128);
            KInputTextField endpoint = AddTradingAiInput(body.transform, "Endpoint", AiText("AI_ENDPOINT"), settings.Endpoint, 2048);
            KInputTextField model = AddTradingAiInput(body.transform, "Model", AiText("AI_MODEL"), settings.Model, 256);
            KInputTextField probability = AddTradingAiInput(body.transform, "ReplyProbability", AiText("AI_REPLY_PROBABILITY"), settings.ReplyProbability.ToString(), 3);
            probability.characterValidation = TMP_InputField.CharacterValidation.Integer;
            GameObject keyRow = AddAmountModalRow(body.transform, 8, 32);
            AddTradingAiLabel(keyRow.transform, "API Key");
            KInputTextField key = CreateFixedTextInput(keyRow.transform, "ApiKey", settings.ApiKey, 330, 28, 13);
            key.characterLimit = 4096;
            key.textComponent.richText = false;
            key.contentType = TMP_InputField.ContentType.Password;
            key.inputType = TMP_InputField.InputType.Password;
            key.ForceLabelUpdate();
            TextMeshProUGUI showLabel = null;
            GameObject show = AddAmountModalButton(keyRow.transform, AiText("AI_SHOW"), 64, () =>
            {
                bool hidden = key.inputType == TMP_InputField.InputType.Password;
                key.contentType = hidden ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
                key.inputType = hidden ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.Password;
                key.ForceLabelUpdate();
                if (showLabel != null) SetTextIfChanged(showLabel, AiText(hidden ? "AI_HIDE" : "AI_SHOW"));
            });
            showLabel = show.GetComponentInChildren<TextMeshProUGUI>();
            var hint = CreateAmountModalText(body.transform, AiText("AI_HELP"), 12, FontStyles.Normal);
            hint.color = StorageNetworkPanelPalette.MutedText;
            hint.GetComponent<LayoutElement>().preferredHeight = 64;
            var status = CreateAmountModalText(body.transform, loaded ? AiText("AI_READY") : AiText("AI_LOAD_FAILED"), 12, FontStyles.Normal);
            status.color = loaded ? StorageNetworkPanelPalette.MutedText : StorageNetworkPanelPalette.DangerStatus;
            status.GetComponent<LayoutElement>().preferredHeight = 36;
            GameObject buttons = AddAmountModalRow(body.transform, 8, 28);
            AddFooterSpacer(buttons.transform);
            AddAmountModalButton(buttons.transform, AiText("AI_CANCEL"), 100, () => { modalRoot?.SetActive(false); CloseModal(); });
            AddAmountModalButton(buttons.transform, AiText("AI_SAVE"), 110, () =>
            {
                var draft = new TradingAiSettings { Provider = provider.text.Trim(), Endpoint = endpoint.text.Trim(), Model = model.text.Trim(), ApiKey = key.text.Trim() };
                if (!int.TryParse(probability.text, out int chance) || chance < 0 || chance > 100)
                { status.color = StorageNetworkPanelPalette.DangerStatus; SetTextIfChanged(status, AiText("AI_INVALID_PROBABILITY")); return; }
                draft.ReplyProbability = chance;
                string problem = draft.ValidationKey();
                if (problem != null) { status.color = StorageNetworkPanelPalette.DangerStatus; SetTextIfChanged(status, AiText(problem)); return; }
                try
                {
                    TradingAiSettingsStore.Local.Save(draft);
                    status.color = StorageNetworkPanelPalette.PositiveStatus;
                    SetTextIfChanged(status, AiText("AI_SAVED"));
                    provider.DeactivateInputField(); endpoint.DeactivateInputField(); model.DeactivateInputField(); probability.DeactivateInputField(); key.DeactivateInputField();
                }
                catch { status.color = StorageNetworkPanelPalette.DangerStatus; SetTextIfChanged(status, AiText("AI_SAVE_FAILED")); }
            });
        }

        private KInputTextField AddTradingAiInput(Transform parent, string name, string label, string value, int limit)
        {
            GameObject row = AddAmountModalRow(parent, 8, 32);
            AddTradingAiLabel(row.transform, label);
            var input = CreateFixedTextInput(row.transform, name, value, 402, 28, 13);
            input.characterLimit = limit;
            return input;
        }
        private void AddTradingAiLabel(Transform parent, string text)
        {
            var label = CreateAmountModalText(parent, text, 13, FontStyles.Normal);
            label.color = StorageNetworkPanelPalette.BodyText;
            var layout = label.GetComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = 116;
            layout.flexibleWidth = 0;
        }
    }
}
