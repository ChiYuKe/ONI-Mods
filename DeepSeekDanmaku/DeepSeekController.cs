using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace DeepSeekDanmaku
{
    internal sealed class DeepSeekController : MonoBehaviour
    {
        private static DeepSeekController current;
        private bool requestRunning, budgetWarningLogged;
        private float nextScheduledRequest, lastEventRequest;

        public static void EnsureCreated()
        {
            if (current != null) return;
            GameObject host = new GameObject("DeepSeekDanmakuController");
            current = host.AddComponent<DeepSeekController>();
            ColonyEventTracker.Attach(host);
        }

        public static void DestroyCurrent()
        {
            if (current != null) Destroy(current.gameObject);
            current = null;
            ColonyEventTracker.ResetCurrent();
            DanmakuOverlay.DestroyCurrent();
        }

        private IEnumerator Start()
        {
            if (ModConfig.Instance.showTestDanmakuOnLoad)
                DanmakuOverlay.Show("弹幕显示测试：结构化殖民地分析已启用", 0f, DanmakuSeverity.Notice);
            float initial = ModConfig.Instance.testApiOnLoad ? 0.5f : 5f;
            nextScheduledRequest = Time.unscaledTime + initial;
            while (true)
            {
                if (!requestRunning && ModConfig.Instance.HasApiKey)
                {
                    bool scheduled = Time.unscaledTime >= nextScheduledRequest;
                    bool eventDue = ModConfig.Instance.eventTriggeredRequests && ColonyEventTracker.HasPending &&
                                    Time.unscaledTime - lastEventRequest >= ModConfig.Instance.eventCooldownSeconds;
                    if (scheduled || eventDue)
                    {
                        if (eventDue) lastEventRequest = Time.unscaledTime;
                        yield return RequestWithPolicy(ModConfig.Instance.selectedProvider);
                        nextScheduledRequest = Time.unscaledTime + ModConfig.Instance.intervalSeconds;
                    }
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private IEnumerator RequestWithPolicy(AiProvider provider)
        {
            requestRunning = true;
            bool succeeded = false;
            yield return TryProvider(provider, value => succeeded = value);
            if (!succeeded && ModConfig.Instance.fallbackProvider)
            {
                AiProvider fallback = provider == AiProvider.Gemini ? AiProvider.DeepSeek : AiProvider.Gemini;
                if (HasKey(fallback))
                {
                    Debug.LogWarning($"[DeepSeekDanmaku][{provider}] 请求失败，回退到 {fallback}。");
                    yield return TryProvider(fallback, value => succeeded = value);
                }
            }
            if (succeeded) ColonyEventTracker.ConfirmSent();
            requestRunning = false;
        }

        private IEnumerator TryProvider(AiProvider provider, Action<bool> completed)
        {
            string model = Model(provider), tag = $"{provider}/{model}";
            string snapshot = ColonySnapshot.Build();
            if (ModConfig.Instance.logSnapshotSummary)
            {
                ColonySnapshotData summary = ColonySnapshot.BuildData(false);
                Debug.Log($"[DeepSeekDanmaku][{tag}] 快照摘要：{snapshot.Length}字符，{summary.worlds.Count}个星球，{summary.totals.duplicants}名复制人，{summary.risks.Count}项风险。");
            }
            for (int attempt = 0; attempt <= ModConfig.Instance.maxRetries; attempt++)
            {
                if (!DailyRequestBudget.TryConsume(provider, model, out int used, out int limit))
                {
                    if (!budgetWarningLogged) Debug.LogWarning($"[DeepSeekDanmaku][{tag}] 今日请求预算已用完（{used}/{limit}），停止请求直到额度日期重置。");
                    budgetWarningLogged = true;
                    completed(false);
                    yield break;
                }
                Debug.Log($"[DeepSeekDanmaku][{tag}] 开始发送殖民地数据，输入={snapshot.Length}字符，尝试={attempt + 1}/{ModConfig.Instance.maxRetries + 1}，预算={(limit > 0 ? used + "/" + limit : "不限")}。");
                ApiAttempt result = null;
                yield return SendAttempt(provider, model, snapshot, value => result = value);
                if (result.success)
                {
                    bool parsed = HandleResponse(result.body, tag, provider == AiProvider.Gemini);
                    completed(parsed);
                    yield break;
                }
                Debug.LogWarning($"[DeepSeekDanmaku][{tag}] API 请求失败 ({result.status}): {result.error}; 服务端响应: {TrimLog(result.body)}");
                if (!result.transient || attempt >= ModConfig.Instance.maxRetries) break;
                float wait = result.status == 429 ? Mathf.Max(60f, result.retryAfter) : Mathf.Pow(2f, attempt) + UnityEngine.Random.Range(0.2f, 1.2f);
                Debug.LogWarning($"[DeepSeekDanmaku][{tag}] {wait:0.0}秒后重试。");
                yield return new WaitForSecondsRealtime(wait);
            }
            completed(false);
        }

        private IEnumerator SendAttempt(AiProvider provider, string model, string snapshot, Action<ApiAttempt> completed)
        {
            bool gemini = provider == AiProvider.Gemini;
            string instruction = ModConfig.Instance.systemPrompt + "\n只输出JSON对象，不要Markdown：{\"comments\":[\"2到5条简体中文短句\"],\"severity\":\"普通|提醒|警告\",\"topic\":\"食物|氧气|电力|复制人|效率|综合\"}。每句不超过45个汉字，禁止英文和复述指令。";
            object payload = gemini
                ? (object)new GeminiInteractionRequest { model = model, store = false, input = instruction + "\n殖民地数据JSON：\n" + snapshot }
                : new ChatRequest
                {
                    model = model, stream = false, max_tokens = 512, temperature = 0.8f,
                    response_format = new ResponseFormat { type = "json_object" },
                    messages = new[] { new ChatMessage { role = "system", content = instruction }, new ChatMessage { role = "user", content = snapshot } }
                };
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            using (UnityWebRequest request = new UnityWebRequest(Url(provider), "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                if (gemini) request.SetRequestHeader("x-goog-api-key", Key(provider));
                else request.SetRequestHeader("Authorization", "Bearer " + Key(provider));
                request.timeout = 30;
                yield return request.SendWebRequest();
                long status = request.responseCode;
                float retryAfter = 0f;
                float.TryParse(request.GetResponseHeader("Retry-After"), out retryAfter);
                string responseBody = request.downloadHandler?.text;
                bool dailyQuota = status == 429 && IsDailyQuotaError(responseBody);
                completed(new ApiAttempt
                {
                    success = request.result == UnityWebRequest.Result.Success,
                    status = status,
                    error = request.error,
                    body = responseBody,
                    retryAfter = retryAfter,
                    transient = status == 0 || status == 408 || (status == 429 && !dailyQuota) || status >= 500
                });
            }
        }

        private static bool HandleResponse(string json, string tag, bool gemini)
        {
            try
            {
                string content = gemini ? GetGeminiOutputText(JsonConvert.DeserializeObject<GeminiInteractionResponse>(json)) : GetChatText(json);
                if (string.IsNullOrWhiteSpace(content)) throw new Exception("响应中没有消息内容");
                AiDanmakuResult result = ParseStructured(content);
                if (result.comments.Count == 0)
                {
                    result.comments.AddRange(ExtractCompleteSentences(content));
                    Debug.LogWarning($"[DeepSeekDanmaku][{tag}] 结构化解析失败，已使用宽松中文解析。");
                }
                if (result.comments.Count == 0)
                {
                    Debug.LogWarning($"[DeepSeekDanmaku][{tag}] 回复中没有通过校验的完整中文句子，本次不显示弹幕。");
                    return false;
                }
                Debug.Log($"[DeepSeekDanmaku][{tag}] 响应成功，主题={result.topic}，严重度={result.severity}，弹幕={result.comments.Count}条。");
                float delay = 0f;
                foreach (string comment in result.comments)
                {
                    DanmakuOverlay.Show(comment, delay, result.severity);
                    delay += UnityEngine.Random.Range(ModConfig.Instance.minSentenceDelaySeconds, ModConfig.Instance.maxSentenceDelaySeconds);
                }
                return true;
            }
            catch (Exception e) { Debug.LogWarning($"[DeepSeekDanmaku][{tag}] 无法解析 API 响应: {e.Message}"); return false; }
        }

        private static AiDanmakuResult ParseStructured(string content)
        {
            AiDanmakuResult result = new AiDanmakuResult();
            try
            {
                int start = content.IndexOf('{'), end = content.LastIndexOf('}');
                if (start < 0 || end <= start) return result;
                StructuredResponse parsed = JsonConvert.DeserializeObject<StructuredResponse>(content.Substring(start, end - start + 1));
                if (parsed?.comments != null)
                    foreach (string text in parsed.comments) if (ValidChinese(text) && result.comments.Count < 5) result.comments.Add(Clean(text));
                result.severity = parsed?.severity == "警告" ? DanmakuSeverity.Warning : parsed?.severity == "提醒" ? DanmakuSeverity.Notice : DanmakuSeverity.Normal;
                result.topic = string.IsNullOrWhiteSpace(parsed?.topic) ? "综合" : parsed.topic;
            }
            catch { }
            return result;
        }

        private static List<string> ExtractCompleteSentences(string content)
        {
            List<string> result = new List<string>(); StringBuilder current = new StringBuilder();
            foreach (char c in content)
            {
                if (c == '。' || c == '！' || c == '？' || c == '!' || c == '?' || c == '\n')
                {
                    string value = current.ToString().Trim().TrimStart('-', '•', ' ', '\t'); current.Clear();
                    if (ValidChinese(value) && result.Count < 5) result.Add(Clean(value));
                }
                else if (c != '\r') current.Append(c);
            }
            return result;
        }

        private static bool ValidChinese(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            int chinese = 0, latin = 0;
            foreach (char c in value)
            {
                if ((c >= '\u3400' && c <= '\u4DBF') || (c >= '\u4E00' && c <= '\u9FFF')) chinese++;
                else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) latin++;
            }
            return chinese > 0 && latin <= 5;
        }
        private static string Clean(string value) => value.Length > 60 ? value.Substring(0, 60) + "…" : value.Trim();
        private static string GetChatText(string json) { ChatResponse r = JsonConvert.DeserializeObject<ChatResponse>(json); return r?.choices != null && r.choices.Length > 0 ? r.choices[0]?.message?.content : null; }
        private static string GetGeminiOutputText(GeminiInteractionResponse response) { StringBuilder b = new StringBuilder(); if (response?.steps != null) foreach (GeminiStep s in response.steps) if (s?.type == "model_output" && s.content != null) foreach (GeminiContent c in s.content) if (c?.type == "text") b.Append(c.text); return b.ToString(); }
        private static string Model(AiProvider p) => p == AiProvider.Gemini ? ModConfig.Instance.geminiModel : ModConfig.Instance.deepseekModel;
        private static string Key(AiProvider p) => p == AiProvider.Gemini ? ModConfig.Instance.geminiApiKey : (!string.IsNullOrWhiteSpace(ModConfig.Instance.deepseekApiKey) ? ModConfig.Instance.deepseekApiKey : ModConfig.Instance.apiKey);
        private static string Url(AiProvider p) => p == AiProvider.Gemini ? "https://generativelanguage.googleapis.com/v1beta/interactions" : ModConfig.Instance.apiUrl;
        private static bool HasKey(AiProvider p) => !string.IsNullOrWhiteSpace(Key(p)) && !Key(p).Contains("请填写");
        private static string TrimLog(string text) => string.IsNullOrEmpty(text) ? "" : (text.Length > 500 ? text.Substring(0, 500) + "…" : text);
        private static bool IsDailyQuotaError(string body) => !string.IsNullOrEmpty(body) &&
            (body.IndexOf("per_day", StringComparison.OrdinalIgnoreCase) >= 0 || body.IndexOf("requests per day", StringComparison.OrdinalIgnoreCase) >= 0 || body.IndexOf("RPD", StringComparison.OrdinalIgnoreCase) >= 0);

        private sealed class ApiAttempt { public bool success, transient; public long status; public string error, body; public float retryAfter; }
        [Serializable] private sealed class ResponseFormat { public string type; }
        [Serializable] private sealed class ChatRequest { public string model; public ChatMessage[] messages; public bool stream; public int max_tokens; public float temperature; public ResponseFormat response_format; }
        [Serializable] private sealed class ChatMessage { public string role; public string content; }
        [Serializable] private sealed class ChatResponse { public Choice[] choices; }
        [Serializable] private sealed class Choice { public ChatMessage message; }
        [Serializable] private sealed class StructuredResponse { public string[] comments; public string severity; public string topic; }
        [Serializable] private sealed class GeminiInteractionRequest { public string model; public string input; public bool store; }
        [Serializable] private sealed class GeminiInteractionResponse { public GeminiStep[] steps; }
        [Serializable] private sealed class GeminiStep { public string type; public GeminiContent[] content; }
        [Serializable] private sealed class GeminiContent { public string type; public string text; }
    }
}
