using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace TestMod
{
    internal static class TestModToggleButton
    {
        private const string RedAlertButtonField = "RedAlertButton";
        private const string ButtonName = "TestModAbButton";
        private const string IconName = "icon_category_blank";
        private static readonly Color ActiveColor = new Color(0.82f, 0.55f, 0.22f, 1f);
        private static MultiToggle button;

        public static void Create(MeterScreen meterScreen)
        {
            MultiToggle redAlertButton = Traverse.Create(meterScreen)
                .Field(RedAlertButtonField)
                .GetValue<MultiToggle>();

            if (redAlertButton == null)
            {
                Debug.LogWarning("[TestMod] Could not find RedAlertButton.");
                return;
            }

            Transform parent = redAlertButton.transform.parent;
            if (parent.Find(ButtonName) != null)
            {
                return;
            }

            Transform buttonTransform = Util.KInstantiateUI(
                redAlertButton.gameObject,
                parent.gameObject,
                true).transform;

            buttonTransform.name = ButtonName;
            buttonTransform.SetSiblingIndex(redAlertButton.transform.GetSiblingIndex() + 2);
            SetIcon(buttonTransform);
            ConfigureButton(buttonTransform);
            UpdateState();
        }

        public static void UpdateState()
        {
            if (button == null)
            {
                return;
            }

            bool visible = TestModWindow.IsOpen;
            button.ChangeState(visible ? 1 : 0);

            if (button.TryGetComponent(out ToolTip tooltip))
            {
                tooltip.SetSimpleTooltip(visible ? "关闭 Base_Screen_820x664" : "打开 Base_Screen_820x664");
            }
        }

        private static void SetIcon(Transform buttonTransform)
        {
            Transform foreground = buttonTransform.Find("FG");
            Image image = foreground == null ? null : foreground.GetComponent<Image>();
            if (image != null)
            {
                Sprite sprite = Assets.GetSprite(IconName);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }
        }

        private static void ConfigureButton(Transform buttonTransform)
        {
            if (!buttonTransform.TryGetComponent(out button))
            {
                return;
            }

            button.onClick = (System.Action)System.Delegate.Combine(button.onClick, new System.Action(TestModWindow.Toggle));
            if (button.states != null && button.states.Length > 1)
            {
                button.states[1].color = ActiveColor;
                button.states[1].color_on_hover = ActiveColor;
                button.states[1].use_color_on_hover = true;
            }

            if (buttonTransform.TryGetComponent(out ToolTip tooltip))
            {
                tooltip.SetSimpleTooltip("打开 Base_Screen_820x664");
            }
        }
    }
}
