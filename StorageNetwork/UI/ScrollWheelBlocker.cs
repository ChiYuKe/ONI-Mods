using UnityEngine;
using UnityEngine.EventSystems;

namespace StorageNetwork.UI
{
    /// <summary>
    /// 阻止鼠标滚轮事件继续向下传播的组件。
    /// 用于防止 UI 滚动时游戏摄像机也跟着缩放。
    /// </summary>
    internal sealed class ScrollWheelBlocker : MonoBehaviour, IScrollHandler
    {
        /// <summary>
        /// 当鼠标滚轮在当前 UI 元素上滚动时调用。
        /// </summary>
        public void OnScroll(PointerEventData eventData)
        {
            eventData.Use();
        }
    }
}
