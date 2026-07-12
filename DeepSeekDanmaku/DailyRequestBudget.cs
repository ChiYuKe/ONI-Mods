using System;
using UnityEngine;

namespace DeepSeekDanmaku
{
    internal static class DailyRequestBudget
    {
        public static bool TryConsume(AiProvider provider, string model, out int used, out int limit)
        {
            limit = GetLimit(provider, model);
            string key = BuildKey(provider, model);
            used = PlayerPrefs.GetInt(key, 0);
            if (limit > 0 && used >= limit) return false;
            used++;
            PlayerPrefs.SetInt(key, used);
            PlayerPrefs.Save();
            return true;
        }

        private static int GetLimit(AiProvider provider, string model)
        {
            if (provider != AiProvider.Gemini) return 0;
            if (model.IndexOf("3.5-flash", StringComparison.OrdinalIgnoreCase) >= 0) return ModConfig.Instance.gemini35DailyBudget;
            if (model.IndexOf("flash-lite", StringComparison.OrdinalIgnoreCase) >= 0) return ModConfig.Instance.geminiLiteDailyBudget;
            return 0;
        }

        private static string BuildKey(AiProvider provider, string model)
        {
            // Gemini RPD 按太平洋时间重置；固定 UTC-8 会在夏令时期间最多偏差一小时，但不会突破预算。
            string date = global::System.DateTime.UtcNow.AddHours(-8).ToString("yyyyMMdd");
            return "DeepSeekDanmaku.Budget." + date + "." + provider + "." + (model ?? "default");
        }
    }
}
