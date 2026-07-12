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
            ChatRequest payload = new ChatRequest
            {
                model = ModConfig.Instance.EffectiveModel,
                messages = new[] {
                    new ChatMessage { role = "system", content = ModConfig.Instance.systemPrompt + "\n必须输出2到5句独立的中文短句，每句不超过45字，不要编号，不要使用Markdown，每句必须以句号、问号或感叹号结束。" },
                    new ChatMessage { role = "user", content = ColonySnapshot.Build() }
                },
                stream = false,
                max_tokens = 512,
                temperature = 0.8f
            };
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            using (UnityWebRequest request = new UnityWebRequest(ModConfig.Instance.EffectiveApiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + ModConfig.Instance.EffectiveApiKey);
                request.timeout = 30;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning($"[DeepSeekDanmaku] API 请求失败 ({request.responseCode}): {request.error}; 服务端响应: {request.downloadHandler?.text}");
                else
                    HandleResponse(request.downloadHandler.text);
            }
            requestRunning = false;
        }

        private static void HandleResponse(string json)
        {
            try
            {
                ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(json);
                string content = response?.choices != null && response.choices.Length > 0 ? response.choices[0]?.message?.content : null;
                if (string.IsNullOrWhiteSpace(content)) throw new Exception("响应中没有消息内容");
                Debug.Log("[DeepSeekDanmaku] 收到 DeepSeek 回复，准备显示弹幕。");
                content = content.Trim();
                string[] lines = ExtractCompleteSentences(content);
                float delay = 0f;
                for (int i = 0; i < lines.Length && i < 5; i++)
                {
                    DanmakuOverlay.Show(lines[i].Trim(), delay);
                    delay += UnityEngine.Random.Range(ModConfig.Instance.minSentenceDelaySeconds, ModConfig.Instance.maxSentenceDelaySeconds);
                }
            }
            catch (Exception e) { Debug.LogWarning("[DeepSeekDanmaku] 无法解析 API 响应: " + e.Message); }
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
            if (sentence.Length == 0) return;
            if (sentence.Length > 60) sentence = sentence.Substring(0, 60) + "…";
            sentences.Add(sentence);
        }

        [Serializable] private sealed class ChatRequest { public string model; public ChatMessage[] messages; public bool stream; public int max_tokens; public float temperature; }
        [Serializable] private sealed class ChatMessage { public string role; public string content; }
        [Serializable] private sealed class ChatResponse { public Choice[] choices; }
        [Serializable] private sealed class Choice { public ChatMessage message; }
    }
}
