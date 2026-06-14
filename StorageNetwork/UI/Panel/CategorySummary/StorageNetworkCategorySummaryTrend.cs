using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkCategorySummaryTrend
    {
        public static string Format(float? trendKgPerCycle)
        {
            if (!trendKgPerCycle.HasValue)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TREND_NO_DATA);
            }

            float value = trendKgPerCycle.Value;
            if (Mathf.Abs(value) < 0.001f)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TREND_ZERO);
            }

            string prefix = value > 0f ? "+" : "-";
            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TREND_PER_CYCLE), prefix, GameUtil.GetFormattedMass(Mathf.Abs(value)));
        }

        public static Color GetColor(float? trendKgPerCycle)
        {
            if (!trendKgPerCycle.HasValue || Mathf.Abs(trendKgPerCycle.Value) < 0.001f)
            {
                return new Color(0.38f, 0.39f, 0.39f, 1f);
            }

            return trendKgPerCycle.Value > 0f
                ? new Color(0.24f, 0.46f, 0.30f, 1f)
                : new Color(0.58f, 0.25f, 0.25f, 1f);
        }
    }
}
