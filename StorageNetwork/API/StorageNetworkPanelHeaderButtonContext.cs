using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.API
{
    /// <summary>
    /// 储存网络主面板顶部扩展按钮的点击上下文。附属模组可以用它把自定义 UI 挂到正确的画布或主面板上。
    /// </summary>
    public sealed class StorageNetworkPanelHeaderButtonContext
    {
        internal StorageNetworkPanelHeaderButtonContext(
            GameObject panelRoot,
            Transform header,
            GameObject button,
            Transform canvas)
        {
            PanelRoot = panelRoot;
            Header = header;
            Button = button;
            Canvas = canvas;
        }

        /// <summary>
        /// 储存网络主面板根对象。适合查找主面板位置、尺寸，或把附属窗口锚定在主面板附近。
        /// </summary>
        public GameObject PanelRoot { get; }

        /// <summary>
        /// 储存网络主面板根 RectTransform。主面板未创建时可能为空。
        /// </summary>
        public RectTransform PanelRootRect => PanelRoot != null ? PanelRoot.GetComponent<RectTransform>() : null;

        /// <summary>
        /// 主面板标题栏 Transform。
        /// </summary>
        public Transform Header { get; }

        /// <summary>
        /// 被点击的按钮对象。
        /// </summary>
        public GameObject Button { get; }

        /// <summary>
        /// 被点击按钮的 RectTransform。
        /// </summary>
        public RectTransform ButtonRect => Button != null ? Button.GetComponent<RectTransform>() : null;

        /// <summary>
        /// 当前 UI 所在画布 Transform。附属弹窗通常应挂到这里。
        /// </summary>
        public Transform Canvas { get; }

        /// <summary>
        /// 当前 UI 所在画布对象。
        /// </summary>
        public GameObject CanvasObject => Canvas != null ? Canvas.gameObject : null;

        /// <summary>
        /// 把 AssetBundle 中加载出来的 prefab 实例化到主 UI 画布下，并自动禁用 prefab 内多余的 Canvas、CanvasScaler、GraphicRaycaster。
        /// </summary>
        public GameObject InstantiatePanelPrefab(GameObject prefab, string name = null)
        {
            if (prefab == null || Canvas == null)
            {
                return null;
            }

            GameObject panel = Object.Instantiate(prefab, Canvas, false);
            if (!string.IsNullOrEmpty(name))
            {
                panel.name = name;
            }

            PrepareEmbeddedPanel(panel);
            return panel;
        }

        /// <summary>
        /// 把 prefab 实例化到指定父级下，并自动禁用 prefab 内多余的 Canvas、CanvasScaler、GraphicRaycaster。
        /// </summary>
        public GameObject InstantiatePanelPrefab(GameObject prefab, Transform parent, string name = null)
        {
            if (prefab == null || parent == null)
            {
                return null;
            }

            GameObject panel = Object.Instantiate(prefab, parent, false);
            if (!string.IsNullOrEmpty(name))
            {
                panel.name = name;
            }

            PrepareEmbeddedPanel(panel);
            return panel;
        }

        /// <summary>
        /// 准备从 AssetBundle 实例化出来的 UI，使它能作为当前游戏 UI 的一部分正常接收布局和点击。
        /// </summary>
        public static void PrepareEmbeddedPanel(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            foreach (CanvasScaler scaler in panel.GetComponentsInChildren<CanvasScaler>(true))
            {
                scaler.enabled = false;
            }

            foreach (GraphicRaycaster raycaster in panel.GetComponentsInChildren<GraphicRaycaster>(true))
            {
                raycaster.enabled = false;
            }

            foreach (Canvas canvas in panel.GetComponentsInChildren<Canvas>(true))
            {
                canvas.overrideSorting = false;
                canvas.enabled = false;
            }

            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }
    }
}
