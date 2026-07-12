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

    public enum DataScope { CurrentAndRelated, AllWorlds }

    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public sealed class ModConfig
    {
        private const string ApiCategory = "API 设置";
        private const string DanmakuCategory = "弹幕设置";
        private const string ContentCategory = "内容设置";
        private const string DataCategory = "数据与事件";
        private const string StabilityCategory = "稳定性与额度";
        private const string DebugCategory = "调试";

        [JsonProperty]
        public int configSchemaVersion { get; set; } = 0;

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
        public string geminiModel { get; set; } = "gemini-3.1-flash-lite";

        [Option("DeepSeek 模型", "例如 deepseek-chat 或 deepseek-reasoner。", ApiCategory)]
        [JsonProperty]
        public string deepseekModel { get; set; } = "deepseek-chat";

        [JsonProperty]
        public string model { get; set; } = "gemini-3.5-flash";

        [Option("请求间隔（秒）", "向 AI 发送殖民地数据的间隔。", ApiCategory, Format = "F0")]
        [Limit(10, 3600)]
        [JsonProperty]
        public float intervalSeconds { get; set; } = 180f;

        [Option("数据范围", "当前星球及关联世界，或全部星球。", DataCategory)]
        [JsonProperty] public DataScope dataScope { get; set; } = DataScope.CurrentAndRelated;

        [Option("趋势周期数", "使用最近多少个完整周期判断趋势。", DataCategory, Format = "F0")]
        [Limit(2, 20)] [JsonProperty] public int trendCycles { get; set; } = 5;

        [Option("发送复制人数据", "发送压力、生命、体内千卡和当前任务。", DataCategory)]
        [JsonProperty] public bool includeDuplicants { get; set; } = true;

        [Option("发送复制人姓名", "关闭后使用匿名编号。", DataCategory)]
        [JsonProperty] public bool includeDuplicantNames { get; set; } = true;

        [Option("发送资源数据", "发送食物、氧气、材料和组件库存。", DataCategory)]
        [JsonProperty] public bool includeResources { get; set; } = true;

        [Option("发送周期报告", "发送氧气、电力、时间分配和任务数据。", DataCategory)]
        [JsonProperty] public bool includeDailyReport { get; set; } = true;

        [Option("发送趋势", "发送最近多个周期的上升、下降或稳定趋势。", DataCategory)]
        [JsonProperty] public bool includeTrends { get; set; } = true;

        [Option("发送事件", "记录两次请求之间的重要殖民地事件。", DataCategory)]
        [JsonProperty] public bool includeEvents { get; set; } = true;

        [Option("事件触发请求", "有新事件时允许在冷却结束后提前请求。", DataCategory)]
        [JsonProperty] public bool eventTriggeredRequests { get; set; } = true;

        [Option("事件请求冷却（秒）", "事件触发 AI 请求的最短间隔。", DataCategory, Format = "F0")]
        [Limit(30, 3600)] [JsonProperty] public float eventCooldownSeconds { get; set; } = 180f;

        [Option("失败时回退提供商", "当前提供商失败且另一提供商配置了 Key 时尝试回退。", StabilityCategory)]
        [JsonProperty] public bool fallbackProvider { get; set; } = false;

        [Option("最大重试次数", "仅重试网络、408、429 和 5xx。", StabilityCategory, Format = "F0")]
        [Limit(0, 2)] [JsonProperty] public int maxRetries { get; set; } = 2;

        [Option("Gemini 3.5 每日预算", "为免费层预留余量，0 表示不限制。", StabilityCategory, Format = "F0")]
        [Limit(0, 10000)] [JsonProperty] public int gemini35DailyBudget { get; set; } = 18;

        [Option("Gemini Flash Lite 每日预算", "默认低于免费层 500 RPD。", StabilityCategory, Format = "F0")]
        [Limit(0, 10000)] [JsonProperty] public int geminiLiteDailyBudget { get; set; } = 450;

        [Option("记录脱敏快照摘要", "只记录长度、星球数、复制人数和风险数。", DebugCategory)]
        [JsonProperty] public bool logSnapshotSummary { get; set; } = false;

        [Option("启动存档时显示测试弹幕", "用于检查 UI，不调用 API。", DebugCategory)]
        [JsonProperty] public bool showTestDanmakuOnLoad { get; set; } = false;

        [Option("启动存档时立即测试 API", "忽略首次等待，但仍遵守每日预算。", DebugCategory)]
        [JsonProperty] public bool testApiOnLoad { get; set; } = false;

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
                if (Instance.configSchemaVersion < 2)
                {
                    if (!string.IsNullOrWhiteSpace(Instance.model))
                    {
                        if (Instance.model.StartsWith("deepseek", StringComparison.OrdinalIgnoreCase)) Instance.deepseekModel = Instance.model;
                        else if (Instance.model.StartsWith("gemini", StringComparison.OrdinalIgnoreCase)) Instance.geminiModel = Instance.model;
                    }
                    if (Instance.intervalSeconds <= 60f) Instance.intervalSeconds = 180f;
                    if (string.Equals(Instance.geminiModel, "gemini-3.5-flash", StringComparison.OrdinalIgnoreCase)) Instance.geminiModel = "gemini-3.1-flash-lite";
                    Instance.configSchemaVersion = 2;
                }
                Instance.Normalize();
                POptions.WriteSettings(Instance);
            }
            catch (Exception e) { Debug.LogError("[DeepSeekDanmaku] PLib 配置读取失败: " + e); }
        }

        private void Normalize()
        {
            intervalSeconds = Mathf.Max(10f, intervalSeconds);
            eventCooldownSeconds = Mathf.Clamp(eventCooldownSeconds, 30f, 3600f);
            trendCycles = Mathf.Clamp(trendCycles, 2, 20);
            maxRetries = Mathf.Clamp(maxRetries, 0, 2);
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
            ? (string.IsNullOrWhiteSpace(geminiModel) ? "gemini-3.1-flash-lite" : geminiModel)
            : (string.IsNullOrWhiteSpace(deepseekModel) ? "deepseek-chat" : deepseekModel);
    }
}
