using UnityEngine;
using UnityEngine.EventSystems;

namespace StorageNetwork.UI
{
    /// <summary>
    /// 阻止鼠标滚轮事件继续向下传播的组件。
    /// 用于防止 UI 滚动时游戏摄像机也跟着缩放。
    /// </summary>
    internal sealed class ScrollWheelBlocker : MonoBehaviour, IScrollHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static int hoverDepth;
        private bool pointerInside;

        public static bool IsPointerOverBlocker => hoverDepth > 0;

        /// <summary>
        /// 当鼠标滚轮在当前 UI 元素上滚动时调用。
        /// </summary>
        public void OnScroll(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (pointerInside)
            {
                return;
            }

            pointerInside = true;
            hoverDepth++;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearPointerInside();
        }

        private void OnDisable()
        {
            ClearPointerInside();
        }

        private void OnDestroy()
        {
            ClearPointerInside();
        }

        private void ClearPointerInside()
        {
            if (!pointerInside)
            {
                return;
            }

            pointerInside = false;
            hoverDepth = Mathf.Max(0, hoverDepth - 1);
        }
    }
}
