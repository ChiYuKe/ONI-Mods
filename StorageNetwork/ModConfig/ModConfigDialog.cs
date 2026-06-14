using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.ModConfig
{
    public sealed class ModConfigField
    {
        public string Label { get; set; }
        public string Description { get; set; }
        public float Value { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public bool Integer { get; set; }
        public bool IsBoolean { get; set; }
        public bool BoolValue { get; set; }
        public System.Action<float> Apply { get; set; }
        public System.Action<bool> ApplyBool { get; set; }
    }

    public sealed class ModConfigDialogDefinition
    {
        public string OverlayName { get; set; } = "ModConfigOverlay";
        public string Title { get; set; }
        public string Hint { get; set; }
        public List<ModConfigField> Fields { get; } = new List<ModConfigField>();
        public System.Action<List<ModConfigFieldControl>> Reset { get; set; }
        public System.Action Save { get; set; }
        public bool RestartRequired { get; set; } = true;
    }

    public sealed class ModConfigFieldControl
    {
        private readonly TMP_InputField input;
        private readonly Toggle toggle;
        private readonly TextMeshProUGUI toggleText;

        public ModConfigFieldControl(TMP_InputField input)
        {
            this.input = input;
        }

        public ModConfigFieldControl(Toggle toggle, TextMeshProUGUI toggleText)
        {
            this.toggle = toggle;
            this.toggleText = toggleText;
            RefreshToggleText();
        }

        public string NumberText
        {
            get { return input != null ? input.text : string.Empty; }
        }

        public bool BoolValue
        {
            get { return toggle != null && toggle.isOn; }
        }

        public void SetNumber(float value)
        {
            if (input == null)
            {
                return;
            }

            ModConfigDialog.ModConfigInputBinding binding = input.GetComponent<ModConfigDialog.ModConfigInputBinding>();
            if (binding != null)
            {
                binding.SetValue(value);
                return;
            }

            input.text = value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public void SetBool(bool value)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.isOn = value;
            RefreshToggleText();
        }

        private void RefreshToggleText()
        {
            if (toggleText != null)
            {
                toggleText.text = BoolValue
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIG_TOGGLE_ON)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIG_TOGGLE_OFF);
            }
        }
    }

    public static partial class ModConfigDialog
    {
        private static readonly Color DialogBackgroundColor = new Color(0.18f, 0.20f, 0.25f, 0.98f);
        private static readonly Color HeaderColor = new Color(0.5294118f, 0.2724914f, 0.4009516f, 1f);
        private static readonly Color ContentColor = new Color(0.24f, 0.27f, 0.33f, 1f);
        private static readonly Color RowColor = new Color(0.30f, 0.33f, 0.41f, 1f);
        private static readonly Color InputColor = Color.white;
        private static readonly Color InputTextColor = new Color(0.08f, 0.09f, 0.10f, 1f);
        private static readonly Color SliderFillColor = HeaderColor;
        private static readonly Vector2 BaseDialogSize = new Vector2(760f, 600f);
        private static readonly Vector2 MinDialogSize = new Vector2(620f, 440f);
        private const float ScreenMargin = 80f;
        private static GameObject currentDialog;

        public static void Show(ModConfigDialogDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            Close();
            List<ModConfigFieldControl> inputs = new List<ModConfigFieldControl>();
            Transform canvas = Global.Instance.globalCanvas.transform;

            GameObject overlay = new GameObject(definition.OverlayName);
            currentDialog = overlay;
            overlay.transform.SetParent(canvas, false);
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            Stretch(overlayRect, 0f, 0f);
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.45f);

            GameObject dialog = CreatePanel("Dialog", overlay.transform, DialogBackgroundColor);
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = GetDialogSize(canvas);

            VerticalLayoutGroup layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject header = CreatePanel("Header", dialog.transform, HeaderColor);
            header.AddComponent<LayoutElement>().preferredHeight = 36f;
            TextMeshProUGUI title = CreateText("Title", header.transform, definition.Title, 18, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);

            RectTransform bodyContent = CreateScrollBody(dialog.transform);
            foreach (ModConfigField field in definition.Fields)
            {
                inputs.Add(AddField(bodyContent, field));
            }

            if (!string.IsNullOrEmpty(definition.Hint))
            {
                TextMeshProUGUI hint = CreateText("Hint", bodyContent, definition.Hint, 11, TextAlignmentOptions.MidlineLeft);
                hint.color = new Color(0.78f, 0.82f, 0.86f, 1f);
                hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            }

            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(dialog.transform, false);
            footer.AddComponent<RectTransform>();
            footer.AddComponent<LayoutElement>().preferredHeight = 40f;
            HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 10f;
            footerLayout.childAlignment = TextAnchor.MiddleRight;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childForceExpandHeight = false;

            AddSpacer(footer.transform);
            CreateButton("ResetButton", footer.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.RESET_DEFAULTS), () => definition.Reset?.Invoke(inputs), false);
            CreateButton("CancelButton", footer.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.CANCEL), Close, false);
            CreateButton("SaveButton", footer.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIRM), () =>
            {
                for (int i = 0; i < definition.Fields.Count && i < inputs.Count; i++)
                {
                    ApplyInput(definition.Fields[i], inputs[i]);
                }

                definition.Save?.Invoke();
                Close();
                if (definition.RestartRequired)
                {
                    ShowRestartRequiredDialog();
                }
            }, true);
        }

        public static void ResetRuntimeState()
        {
            Close();
        }

        private static void Close()
        {
            if (currentDialog != null)
            {
                Object.Destroy(currentDialog);
                currentDialog = null;
            }
        }

        private static void ShowRestartRequiredDialog()
        {
            GameObject parent = Global.Instance != null && Global.Instance.globalCanvas != null
                ? Global.Instance.globalCanvas.gameObject
                : null;
            GameObject dialogObject = Util.KInstantiateUI(ScreenPrefabs.Instance.ConfirmDialogScreen.gameObject, parent, false);
            ConfirmDialogScreen dialog = dialogObject != null ? dialogObject.GetComponent<ConfirmDialogScreen>() : null;
            if (dialog == null)
            {
                return;
            }

            dialog.PopupConfirmDialog(
                Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIG_RESTART_REQUIRED),
                () => App.instance.Restart(),
                () => { },
                null,
                null,
                Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIG_RESTART_DIALOG_TITLE),
                Loc.Get(Loc.UI.STORAGE_NETWORK.RESTART),
                Loc.Get(Loc.UI.STORAGE_NETWORK.CONFIG_RESTART_CONTINUE));
            dialogObject.SetActive(true);
        }

    }
}
