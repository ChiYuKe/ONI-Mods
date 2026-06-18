using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ONIVisualEnhancer
{
    internal static class VisualEnhancerToggleButton
    {
        private const string RedAlertButtonField = "RedAlertButton";
        private const string IconName = "icon_category_lights";

        private static readonly Color ActiveColor = new Color(0.36f, 0.62f, 0.82f, 1f);
        private static MultiToggle button;

        public static void Create(MeterScreen meterScreen)
        {
            MultiToggle redAlertButton = Traverse.Create(meterScreen)
                .Field(RedAlertButtonField)
                .GetValue<MultiToggle>();

            if (redAlertButton == null)
            {
                Debug.LogWarning("[ONIVisualEnhancer] Could not find RedAlertButton");
                return;
            }

            Transform buttonTransform = Util.KInstantiateUI(
                redAlertButton.gameObject,
                redAlertButton.transform.parent.gameObject,
                true).transform;

            buttonTransform.name = "ONIVisualEnhancerButton";
            buttonTransform.SetSiblingIndex(redAlertButton.transform.GetSiblingIndex() + 1);
            SetIcon(buttonTransform);
            ConfigureButton(buttonTransform);
            ConfigureTooltip(buttonTransform, VisualEnhancerSettings.GetCurrentPreset());
            SetState(VisualEnhancerSettings.GetCurrentPreset());
        }

        public static void SetState(VisualPreset preset)
        {
            if (button == null)
            {
                return;
            }

            button.ChangeState(preset.Mode == VisualPresetMode.Off ? 0 : 1);
            ConfigureTooltip(button.transform, preset);
        }

        private static void SetIcon(Transform buttonTransform)
        {
            Transform foreground = buttonTransform.Find("FG");
            Image image = foreground == null ? null : foreground.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = Assets.GetSprite(IconName);
            }
        }

        private static void ConfigureButton(Transform buttonTransform)
        {
            if (!buttonTransform.TryGetComponent(out button))
            {
                return;
            }

            button.onClick = (System.Action)System.Delegate.Combine(button.onClick, new System.Action(VisualEnhancerController.ToggleSettingsWindow));
            if (button.states != null && button.states.Length > 1)
            {
                button.states[1].color = ActiveColor;
                button.states[1].color_on_hover = ActiveColor;
                button.states[1].use_color_on_hover = true;
            }
        }

        private static void ConfigureTooltip(Transform buttonTransform, VisualPreset preset)
        {
            if (buttonTransform.TryGetComponent(out ToolTip tooltip))
            {
                tooltip.SetSimpleTooltip("Visual Enhancer: " + preset.Name + "\nClick to open settings.");
            }
        }
    }
}
