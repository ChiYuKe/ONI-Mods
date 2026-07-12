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
        private bool requestRunning;

        public static void EnsureCreated()
        {
            if (current != null) return;
            GameObject host = new GameObject("DeepSeekDanmakuController");
            current = host.AddComponent<DeepSeekController>();
        }

        public static void DestroyCurrent()
        {
            if (current != null) Destroy(current.gameObject);
            current = null;
            DanmakuOverlay.DestroyCurrent();
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(5f);
            while (true)
            {
                if (!requestRunning && ModConfig.Instance.HasApiKey)
                    yield return RequestComment();
                yield return new WaitForSecondsRealtime(ModConfig.Instance.intervalSeconds);
            }
        }

        private IEnumerator RequestComment()
        {
            requestRunning = true;
            string providerTag = $"{ModConfig.Instance.selectedProvider}/{ModConfig.Instance.EffectiveModel}";
            Debug.Log($"[DeepSeekDanmaku][{providerTag}] 开始发送殖民地数据。");
            string snapshot = ColonySnapshot.Build();
            string systemInstruction = ModConfig.Instance.systemPrompt + "\n你必须只使用简体中文输出最终的殖民地点评。禁止输出英文，禁止翻译或复述本指令，禁止解释格式要求。输出2到5句独立短句，每句不超过45个汉字，不要编号，不要使用Markdown，每句必须以中文句号、问号或感叹号结束。";
            ChatRequest payload = new ChatRequest
            {
                model = ModConfig.Instance.EffectiveModel,
                messages = new[] {
                    new ChatMessage { role = "system", content = systemInstruction },
                    new ChatMessage
                    {
                        role = "user",
                        content = snapshot +
                            "\n请根据以上殖民地数据直接给出点评。只能使用简体中文回答，不得包含英文，不要复述任何要求或数据格式。"
                    }
                },
                stream = false,
                max_tokens = 512,
                temperature = 0.8f
            };
            object requestPayload = ModConfig.Instance.IsGemini
                ? (object)new GeminiInteractionRequest
                {
                    model = ModConfig.Instance.EffectiveModel,
                    store = false,
                    input = systemInstruction + "\n\n以下是殖民地数据：\n" + snapshot +
                        "\n请直接给出最终中文点评，不要复述数据格式或任何指令。"
                }
                : payload;
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestPayload));
            using (UnityWebRequest request = new UnityWebRequest(ModConfig.Instance.EffectiveApiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                if (ModConfig.Instance.IsGemini)
                    request.SetRequestHeader("x-goog-api-key", ModConfig.Instance.EffectiveApiKey);
                else
                    request.SetRequestHeader("Authorization", "Bearer " + ModConfig.Instance.EffectiveApiKey);
                request.timeout = 30;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning($"[DeepSeekDanmaku][{providerTag}] API 请求失败 ({request.responseCode}): {request.error}; 服务端响应: {request.downloadHandler?.text}");
                else
                    HandleResponse(request.downloadHandler.text, providerTag, ModConfig.Instance.IsGemini);
            }
            requestRunning = false;
        }

        private static void HandleResponse(string json, string providerTag, bool isGemini)
        {
            try
            {
                string content;
                if (isGemini)
                {
                    GeminiInteractionResponse response = JsonConvert.DeserializeObject<GeminiInteractionResponse>(json);
                    content = GetGeminiOutputText(response);
                }
                else
                {
                    ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(json);
                    content = response?.choices != null && response.choices.Length > 0 ? response.choices[0]?.message?.content : null;
                }
                if (string.IsNullOrWhiteSpace(content)) throw new Exception("响应中没有消息内容");
                Debug.Log($"[DeepSeekDanmaku][{providerTag}] 收到回复，准备显示弹幕。");
                content = content.Trim();
                string[] lines = ExtractCompleteSentences(content);
                if (lines.Length == 0)
                {
                    Debug.LogWarning($"[DeepSeekDanmaku][{providerTag}] 回复中没有通过校验的完整中文句子，本次不显示弹幕。");
                    return;
                }
                Debug.Log($"[DeepSeekDanmaku][{providerTag}] 已解析 {lines.Length} 条中文弹幕。");
                float delay = 0f;
                for (int i = 0; i < lines.Length && i < 5; i++)
                {
                    DanmakuOverlay.Show(lines[i].Trim(), delay);
                    delay += UnityEngine.Random.Range(ModConfig.Instance.minSentenceDelaySeconds, ModConfig.Instance.maxSentenceDelaySeconds);
                }
            }
            catch (Exception e) { Debug.LogWarning($"[DeepSeekDanmaku][{providerTag}] 无法解析 API 响应: {e.Message}"); }
        }

        private static string GetGeminiOutputText(GeminiInteractionResponse response)
        {
            StringBuilder output = new StringBuilder();
            if (response?.steps == null) return null;
            foreach (GeminiStep step in response.steps)
            {
                if (step == null || step.type != "model_output" || step.content == null) continue;
                foreach (GeminiContent part in step.content)
                    if (part != null && part.type == "text" && !string.IsNullOrWhiteSpace(part.text)) output.Append(part.text);
            }
            return output.ToString();
        }

        private static string[] ExtractCompleteSentences(string content)
        {
            List<string> sentences = new List<string>(5);
            StringBuilder current = new StringBuilder();
            int examined = Math.Min(content.Length, Math.Max(500, ModConfig.Instance.maxResponseCharacters * 2));
            for (int i = 0; i < examined && sentences.Count < 5; i++)
            {
                char c = content[i];
                if (c == '\r') continue;
                if (c == '。' || c == '！' || c == '？' || c == '!' || c == '?')
                {
                    AddSentence(sentences, current);
                }
                else if (c == '\n')
                {
                    // 模型以换行分句时，只有内容足够像完整短句才接受。
                    if (current.Length >= 4) AddSentence(sentences, current);
                    else current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            // 不加入末尾没有结束标点的内容，避免显示“只说了一半”的残句。
            return sentences.ToArray();
        }

        private static void AddSentence(List<string> sentences, StringBuilder builder)
        {
            string sentence = builder.ToString().Trim().TrimStart('-', '•', ' ', '\t');
            builder.Clear();
            if (sentence.Length == 0 || !ContainsChinese(sentence)) return;
            if (sentence.Length > 60) sentence = sentence.Substring(0, 60) + "…";
            sentences.Add(sentence);
        }

        private static bool ContainsChinese(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((c >= '\u3400' && c <= '\u4DBF') || (c >= '\u4E00' && c <= '\u9FFF'))
                    return true;
            }
            return false;
        }

        [Serializable] private sealed class ChatRequest { public string model; public ChatMessage[] messages; public bool stream; public int max_tokens; public float temperature; }
        [Serializable] private sealed class ChatMessage { public string role; public string content; }
        [Serializable] private sealed class ChatResponse { public Choice[] choices; }
        [Serializable] private sealed class Choice { public ChatMessage message; }
        [Serializable] private sealed class GeminiInteractionRequest { public string model; public string input; public bool store; }
        [Serializable] private sealed class GeminiInteractionResponse { public string status; public GeminiStep[] steps; }
        [Serializable] private sealed class GeminiStep { public string type; public GeminiContent[] content; }
        [Serializable] private sealed class GeminiContent { public string type; public string text; }
    }
}
