using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace VignetteBegone
{
    public static class VignetteToggleButton
    {
        private const string RedAlertButtonField = "RedAlertButton";
        private const string IconName = "icon_category_lights_disabled";
        private const string TooltipText = "切换关闭屏幕晕影";
        private const int EnabledState = 1;
        private const int DisabledState = 0;

        private static readonly Color EnabledColor = new Color(0.37f, 0.6f, 0.25f, 1f);

        private static MultiToggle customButton;

        public static void Create(MeterScreen meterScreen)
        {
            MultiToggle redAlertButton = Traverse.Create(meterScreen)
                .Field(RedAlertButtonField)
                .GetValue<MultiToggle>();

            if (redAlertButton == null)
            {
                Debug.LogError("[VignetteBegone] 没有找到 RedAlertButton!");
                return;
            }

            Transform buttonTransform = Util.KInstantiateUI(
                redAlertButton.gameObject,
                redAlertButton.transform.parent.gameObject,
                true).transform;

            buttonTransform.SetSiblingIndex(redAlertButton.transform.GetSiblingIndex() + 1);
            SetIcon(buttonTransform);
            ConfigureButton(buttonTransform, new System.Action(VignetteController.Toggle));
            ConfigureTooltip(buttonTransform);
        }

        public static void SetState(bool hidden)
        {
            customButton?.ChangeState(hidden ? EnabledState : DisabledState);
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

        private static void ConfigureButton(Transform buttonTransform, System.Action onClick)
        {
            if (!buttonTransform.TryGetComponent(out MultiToggle button))
            {
                return;
            }

            button.onClick = (System.Action)System.Delegate.Combine(button.onClick, onClick);
            customButton = button;
            ConfigureEnabledState(button);
        }

        private static void ConfigureEnabledState(MultiToggle button)
        {
            if (button.states == null || button.states.Length <= EnabledState)
            {
                return;
            }

            button.states[EnabledState].color = EnabledColor;
            button.states[EnabledState].color_on_hover = EnabledColor;
            button.states[EnabledState].use_color_on_hover = true;
        }

        private static void ConfigureTooltip(Transform buttonTransform)
        {
            if (buttonTransform.TryGetComponent(out ToolTip tooltip))
            {
                tooltip.SetSimpleTooltip(TooltipText);
            }
        }
    }
}
