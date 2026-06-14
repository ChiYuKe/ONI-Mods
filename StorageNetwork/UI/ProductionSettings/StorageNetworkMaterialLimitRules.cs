using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkMaterialLimitRules
    {
        public const float MinLimitKg = 1f;
        public const float MaxLimitKg = 1000000f;

        public static float GetCurrentLimitKg(float limitKg)
        {
            return Mathf.Max(MinLimitKg, limitKg <= 0f ? Config.Instance.DefaultMaterialRequestLimitKg : limitKg);
        }

        public static string FormatMaxLimitInput()
        {
            return Mathf.RoundToInt(MaxLimitKg).ToString();
        }

        public static float ParseInput(string text, float fallback)
        {
            return ParseInput(text, fallback, MinLimitKg, MaxLimitKg);
        }

        public static float ParseInput(string text, float fallback, float minValue, float maxValue)
        {
            string normalized = (text ?? string.Empty).Trim().Replace(',', '.');
            if (!float.TryParse(normalized, out float value))
            {
                value = fallback;
            }

            return Mathf.Clamp(value, minValue, maxValue);
        }

        public static void SetEnabled(StorageNetworkMaterialRequester requester, bool enabled)
        {
            if (requester == null)
            {
                return;
            }

            requester.LimitEnabled = enabled;
            EnsureLimitWhenEnabled(enabled, value => requester.LimitKg = value, requester.LimitKg);
        }

        public static void SetEnabled(StorageNetworkEnergyGeneratorRequester requester, bool enabled)
        {
            if (requester == null)
            {
                return;
            }

            requester.LimitEnabled = enabled;
            EnsureLimitWhenEnabled(enabled, value => requester.LimitKg = value, requester.LimitKg);
        }

        private static void EnsureLimitWhenEnabled(bool enabled, System.Action<float> setLimit, float limitKg)
        {
            if (enabled && limitKg <= 0f)
            {
                setLimit(Mathf.Max(MinLimitKg, Config.Instance.DefaultMaterialRequestLimitKg));
            }
        }
    }
}
