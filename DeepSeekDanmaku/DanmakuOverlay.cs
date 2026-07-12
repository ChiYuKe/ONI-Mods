using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeekDanmaku
{
    internal sealed class DanmakuOverlay : MonoBehaviour
    {
        private static DanmakuOverlay current;
        private float lastY = float.NaN;
        private static readonly Color[] Colors =
        {
            new Color32(91, 221, 255, 255),
            new Color32(255, 222, 89, 255),
            new Color32(255, 126, 182, 255),
            new Color32(137, 255, 151, 255),
            new Color32(196, 151, 255, 255),
            new Color32(255, 166, 92, 255),
            Color.white
        };

        public static void Show(string message, float delaySeconds = 0f, DanmakuSeverity severity = DanmakuSeverity.Normal)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            EnsureCreated();
            if (current != null) current.StartCoroutine(current.Spawn(message, delaySeconds, severity));
        }

        private static void EnsureCreated()
        {
            if (current != null) return;
            Transform parent = GameScreenManager.Instance?.ssOverlayCanvas?.transform ?? Global.Instance?.globalCanvas?.transform;
            if (parent == null) return;
            GameObject root = new GameObject("DeepSeekDanmakuOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true; canvas.sortingOrder = 30000;
            root.GetComponent<GraphicRaycaster>().enabled = false;
            current = root.AddComponent<DanmakuOverlay>();
            root.transform.SetAsLastSibling();
            Debug.Log($"[DeepSeekDanmaku] 弹幕画布已创建，父级={parent.name}，尺寸={rect.rect.width:0}x{rect.rect.height:0}。");
        }

        public static void DestroyCurrent()
        {
            if (current != null) Destroy(current.gameObject);
            current = null;
        }

        private IEnumerator Spawn(string message, float delaySeconds, DanmakuSeverity severity)
        {
            if (delaySeconds > 0f)
                yield return new WaitForSecondsRealtime(delaySeconds);
            GameObject item = new GameObject("DeepSeekDanmaku", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
            item.transform.SetParent(transform, false);
            transform.SetAsLastSibling();
            TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
            int fontSize = Random.Range(ModConfig.Instance.minFontSize, ModConfig.Instance.maxFontSize + 1);
            text.text = message; text.fontSize = fontSize; text.color = PickColor(severity); text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.enableWordWrapping = false; text.raycastTarget = false;
            text.outlineWidth = 0.18f; text.outlineColor = new Color32(0, 0, 0, 220);
            float width = Mathf.Max(100f, text.preferredWidth + 24f);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f); rect.sizeDelta = new Vector2(width, fontSize * 1.8f);
            float halfHeight = ((RectTransform)transform).rect.height * 0.5f;
            float y = Random.Range(-halfHeight + 60f, halfHeight - 60f);
            if (!float.IsNaN(lastY) && Mathf.Abs(y - lastY) < 48f) y = Mathf.Clamp(y + 64f, -halfHeight + 60f, halfHeight - 60f);
            lastY = y;
            float x = ((RectTransform)transform).rect.width + 20f;
            rect.anchoredPosition = new Vector2(x, y);
            Debug.Log($"[DeepSeekDanmaku] 创建弹幕：字号={fontSize}，轨道Y={y:0}，起点X={x:0}，内容={message}");
            while (rect.anchoredPosition.x > -width)
            {
                rect.anchoredPosition += Vector2.left * ModConfig.Instance.danmakuSpeed * Time.unscaledDeltaTime;
                yield return null;
            }
            Destroy(item);
        }

        private static Color PickColor(DanmakuSeverity severity)
        {
            if (severity == DanmakuSeverity.Warning)
                return Random.value < 0.5f ? new Color32(255, 84, 100, 255) : new Color32(255, 126, 182, 255);
            if (severity == DanmakuSeverity.Notice)
                return Random.value < 0.5f ? new Color32(255, 222, 89, 255) : new Color32(255, 166, 92, 255);
            Color[] normal = { Colors[0], Colors[3], Colors[6] };
            return normal[Random.Range(0, normal.Length)];
        }
    }
}
