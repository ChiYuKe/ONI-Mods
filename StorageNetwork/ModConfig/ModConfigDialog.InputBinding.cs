using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.ModConfig
{
    public static partial class ModConfigDialog
    {
        internal sealed class ModConfigInputBinding : MonoBehaviour
        {
            private TMP_InputField input;
            private Slider slider;
            private float min;
            private float max;
            private bool integer;
            private bool updating;
            private bool listenersRegistered;

            public void Configure(TMP_InputField inputField, Slider valueSlider, float minValue, float maxValue, bool integerValue)
            {
                UnregisterListeners();
                input = inputField;
                slider = valueSlider;
                min = minValue;
                max = maxValue;
                integer = integerValue;

                if (slider != null)
                {
                    slider.minValue = min;
                    slider.maxValue = max;
                    slider.wholeNumbers = integer;
                }

                RegisterListeners();
            }

            private void OnDestroy()
            {
                UnregisterListeners();
            }

            private void RegisterListeners()
            {
                if (listenersRegistered)
                {
                    return;
                }

                slider?.onValueChanged.AddListener(OnSliderChanged);
                input?.onEndEdit.AddListener(OnInputEndEdit);
                listenersRegistered = true;
            }

            private void UnregisterListeners()
            {
                if (!listenersRegistered)
                {
                    return;
                }

                slider?.onValueChanged.RemoveListener(OnSliderChanged);
                input?.onEndEdit.RemoveListener(OnInputEndEdit);
                listenersRegistered = false;
            }

            public void SetValue(float value)
            {
                value = Normalize(value);
                updating = true;
                if (input != null)
                {
                    input.text = Format(value);
                }

                if (slider != null)
                {
                    slider.value = value;
                }

                updating = false;
            }

            private void OnSliderChanged(float value)
            {
                if (!updating)
                {
                    SetValue(value);
                }
            }

            private void OnInputEndEdit(string text)
            {
                if (updating || input == null)
                {
                    return;
                }

                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out float value) &&
                    !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    value = min;
                }

                SetValue(value);
            }

            private float Normalize(float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    value = min;
                }

                value = Mathf.Clamp(value, min, max);
                return integer ? Mathf.Round(value) : value;
            }
        }
    }
}
