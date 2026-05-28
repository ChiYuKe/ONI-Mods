using StorageNetwork.API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI.WorldPanel
{
    /// <summary>
    /// 建筑世界文字面板视图。只处理 Unity UI、位置和可见性，不读取订单或网络数据。
    /// </summary>
    internal sealed class StorageNetworkWorldTextPanelView
    {
        private const float Width = 230f;
        private const float Height = 96f;
        private const float WorldScale = 0.025f;
        private const float VerticalOffset = 0.35f;

        private RectTransform rootRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI lineOneText;
        private TextMeshProUGUI lineTwoText;
        private TextMeshProUGUI lineThreeText;

        public bool IsCreated => rootRect != null;

        public bool IsVisible => rootRect != null && rootRect.gameObject.activeSelf;

        /// <summary>
        /// 创建世界空间 UI。worldSpaceCanvas 未就绪时返回 false，调用方稍后重试即可。
        /// </summary>
        public bool EnsureCreated()
        {
            if (rootRect != null)
            {
                return true;
            }

            Transform parent = GameScreenManager.Instance?.worldSpaceCanvas?.transform;
            if (parent == null)
            {
                Debug.Log("[StorageNetworkWorldTextPanel] worldSpaceCanvas is null; panel create deferred.");
                return false;
            }

            GameObject root = new GameObject("StorageNetworkWorldTextPanel");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * WorldScale;
            rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(Width, Height);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.10f, 0.13f, 0.88f);
            background.raycastTarget = false;

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = CreateText("Title", root.transform, 11, FontStyles.Bold, new Color(0.80f, 0.93f, 1f, 1f), 16f);
            lineOneText = CreateText("LineOne", root.transform, 9, FontStyles.Normal, new Color(0.94f, 0.96f, 0.98f, 1f), 18f);
            lineTwoText = CreateText("LineTwo", root.transform, 9, FontStyles.Normal, new Color(0.86f, 0.89f, 0.92f, 1f), 18f);
            lineThreeText = CreateText("LineThree", root.transform, 9, FontStyles.Normal, new Color(0.78f, 0.84f, 0.90f, 1f), 28f);

            SetVisible(false);
            Debug.Log("[StorageNetworkWorldTextPanel] Panel created under worldSpaceCanvas.");
            return true;
        }

        /// <summary>
        /// 把内容写入 TextMeshPro 控件。
        /// </summary>
        public void SetContent(StorageNetworkWorldPanelContent content)
        {
            if (content == null || titleText == null)
            {
                return;
            }

            titleText.text = content.Title;
            lineOneText.text = content.LineOne;
            lineTwoText.text = content.LineTwo;
            lineThreeText.text = content.LineThree;
        }

        /// <summary>
        /// 将面板挂在目标建筑上沿，效果接近生命条/氧气条这类世界空间 UI。
        /// </summary>
        public void UpdatePosition(GameObject target)
        {
            if (rootRect == null || target == null)
            {
                return;
            }

            Bounds bounds = Util.GetBounds(target);
            Vector3 targetPosition = target.transform.GetPosition();
            float top = bounds.size.y > 0f ? bounds.max.y : targetPosition.y + 1.2f;
            rootRect.transform.SetPosition(new Vector3(targetPosition.x, top + VerticalOffset, targetPosition.z));
        }

        /// <summary>
        /// 切换面板可见性，避免每帧重复 SetActive。
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (rootRect != null && rootRect.gameObject.activeSelf != visible)
            {
                rootRect.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 输出面板坐标诊断日志，用于玩家反馈“看不到/位置不对”时定位。
        /// </summary>
        public void LogDiagnostic(GameObject target)
        {
            if (rootRect == null || target == null)
            {
                return;
            }

            Debug.Log(string.Format(
                "[StorageNetworkWorldTextPanel] visible={0}, target={1}, panelPos={2}, targetPos={3}, localScale={4}, lossyScale={5}, size={6}, parent={7}",
                rootRect.gameObject.activeSelf,
                target.name,
                rootRect.transform.position,
                target.transform.position,
                rootRect.transform.localScale,
                rootRect.transform.lossyScale,
                rootRect.sizeDelta,
                rootRect.transform.parent != null ? rootRect.transform.parent.name : "<null>"));
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int size, FontStyles style, Color color, float height)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return text;
        }
    }
}
