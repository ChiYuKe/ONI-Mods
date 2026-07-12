using System;
using System.IO;
using Newtonsoft.Json;
using PeterHan.PLib.Options;
using UnityEngine;

namespace DeepSeekDanmaku
{
    public enum AiProvider
    {
        Gemini,
        DeepSeek
    }

    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public sealed class ModConfig
    {
        private const string ApiCategory = "API 设置";
        private const string DanmakuCategory = "弹幕设置";
        private const string ContentCategory = "内容设置";

        [Option("提供商", "选择当前用于生成弹幕的 AI 服务。", ApiCategory)]
        [JsonProperty]
        public AiProvider selectedProvider { get; set; } = AiProvider.Gemini;

        [JsonProperty]
        public string provider { get; set; } = "gemini";

        [Option("Gemini API Key", "从 Google AI Studio 获取。只保存在本机配置文件中。", ApiCategory)]
        [JsonProperty]
        public string geminiApiKey { get; set; } = "";

        [Option("DeepSeek API Key", "切换回 DeepSeek 时使用。只保存在本机配置文件中。", ApiCategory)]
        [JsonProperty]
        public string deepseekApiKey { get; set; } = "";

        [JsonProperty]
        public string apiKey { get; set; } = "请填写 sk-...";

        [Option("DeepSeek API 地址", "DeepSeek 的 OpenAI 兼容 chat/completions 地址。", ApiCategory)]
        [JsonProperty]
        public string apiUrl { get; set; } = "https://api.deepseek.com/chat/completions";

        [Option("Gemini 模型", "例如 gemini-3.5-flash。", ApiCategory)]
        [JsonProperty]
        public string geminiModel { get; set; } = "gemini-3.5-flash";

        [Option("DeepSeek 模型", "例如 deepseek-chat 或 deepseek-reasoner。", ApiCategory)]
        [JsonProperty]
        public string deepseekModel { get; set; } = "deepseek-chat";

        [JsonProperty]
        public string model { get; set; } = "gemini-3.5-flash";

        [Option("请求间隔（秒）", "向 AI 发送殖民地数据的间隔。", ApiCategory, Format = "F0")]
        [Limit(10, 3600)]
        [JsonProperty]
        public float intervalSeconds { get; set; } = 60f;

        [Option("系统提示词", "控制 AI 点评的风格。Mod 会自动追加输出 2–5 句的格式要求。", ContentCategory)]
        [JsonProperty]
        public string systemPrompt { get; set; } = "你是《缺氧》的殖民地观察员。根据数据给出简短、有趣且有帮助的中文点评。不要使用Markdown。";

        [Option("回复最大字符数", "过长的回复会被截断。", ContentCategory, Format = "F0")]
        [Limit(250, 500)]
        [JsonProperty]
        public int maxResponseCharacters { get; set; } = 280;

        [Option("移动速度", "弹幕从右向左移动的像素速度。", DanmakuCategory, Format = "F0")]
        [Limit(60, 600)]
        [JsonProperty]
        public float danmakuSpeed { get; set; } = 180f;

        [JsonProperty]
        public int fontSize { get; set; } = 24;

        [Option("最小字号", "每条弹幕会在最小和最大字号之间随机。", DanmakuCategory, Format = "F0")]
        [Limit(14, 48)]
        [JsonProperty]
        public int minFontSize { get; set; } = 20;

        [Option("最大字号", "必须不小于最小字号。", DanmakuCategory, Format = "F0")]
        [Limit(14, 56)]
        [JsonProperty]
        public int maxFontSize { get; set; } = 34;

        [Option("最短句间隔（秒）", "同一批相邻弹幕出现的最短随机间隔。", DanmakuCategory, Format = "F1")]
        [Limit(0, 10)]
        [JsonProperty]
        public float minSentenceDelaySeconds { get; set; } = 0.8f;

        [Option("最长句间隔（秒）", "同一批相邻弹幕出现的最长随机间隔。", DanmakuCategory, Format = "F1")]
        [Limit(0, 15)]
        [JsonProperty]
        public float maxSentenceDelaySeconds { get; set; } = 2.5f;

        public static ModConfig Instance { get; private set; } = new ModConfig();
        public static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Klei", "OxygenNotIncluded", "mods", "config", "DeepSeekDanmaku.json");

        public static void Load()
        {
            try
            {
                Instance = POptions.ReadSettings<ModConfig>() ?? new ModConfig();
                if (string.IsNullOrWhiteSpace(Instance.deepseekApiKey) && !string.IsNullOrWhiteSpace(Instance.apiKey) && Instance.apiKey.StartsWith("sk-", StringComparison.OrdinalIgnoreCase))
                    Instance.deepseekApiKey = Instance.apiKey;
                if (string.Equals(Instance.provider, "deepseek", StringComparison.OrdinalIgnoreCase))
                    Instance.selectedProvider = AiProvider.DeepSeek;
                if (!string.IsNullOrWhiteSpace(Instance.model))
                {
                    if (Instance.model.StartsWith("deepseek", StringComparison.OrdinalIgnoreCase)) Instance.deepseekModel = Instance.model;
                    else if (Instance.model.StartsWith("gemini", StringComparison.OrdinalIgnoreCase)) Instance.geminiModel = Instance.model;
                }
                Instance.Normalize();
                POptions.WriteSettings(Instance);
            }
            catch (Exception e) { Debug.LogError("[DeepSeekDanmaku] PLib 配置读取失败: " + e); }
        }

        private void Normalize()
        {
            intervalSeconds = Mathf.Max(10f, intervalSeconds);
            danmakuSpeed = Mathf.Clamp(danmakuSpeed, 60f, 600f);
            if (minFontSize <= 0) minFontSize = 20;
            if (maxFontSize <= 0) maxFontSize = 34;
            minFontSize = Mathf.Clamp(minFontSize, 14, 48);
            maxFontSize = Mathf.Clamp(maxFontSize, minFontSize, 56);
            maxResponseCharacters = Mathf.Clamp(maxResponseCharacters, 250, 500);
            if (maxSentenceDelaySeconds <= 0f) { minSentenceDelaySeconds = 0.8f; maxSentenceDelaySeconds = 2.5f; }
            minSentenceDelaySeconds = Mathf.Clamp(minSentenceDelaySeconds, 0f, 10f);
            maxSentenceDelaySeconds = Mathf.Clamp(maxSentenceDelaySeconds, minSentenceDelaySeconds, 15f);
        }

        public bool IsGemini => selectedProvider == AiProvider.Gemini;
        public string EffectiveApiKey => IsGemini ? geminiApiKey : (!string.IsNullOrWhiteSpace(deepseekApiKey) ? deepseekApiKey : apiKey);
        public bool HasApiKey => !string.IsNullOrWhiteSpace(EffectiveApiKey) && !EffectiveApiKey.Contains("请填写");
        public string EffectiveApiUrl => IsGemini ? "https://generativelanguage.googleapis.com/v1beta/interactions" : apiUrl;
        public string EffectiveModel => IsGemini
            ? (string.IsNullOrWhiteSpace(geminiModel) ? "gemini-3.5-flash" : geminiModel)
            : (string.IsNullOrWhiteSpace(deepseekModel) ? "deepseek-chat" : deepseekModel);
    }
}
