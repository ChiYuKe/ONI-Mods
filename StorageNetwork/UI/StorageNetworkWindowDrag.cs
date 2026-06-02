using UnityEngine;
using UnityEngine.EventSystems;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform target;
        private RectTransform parent;
        private Vector2 lastLocalPointerPosition;

        // 绑定需要被拖动的窗口根节点；拖动组件通常挂在标题栏上。
        public void Configure(RectTransform targetRect)
        {
            target = targetRect;
            parent = targetRect != null ? targetRect.parent as RectTransform : null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (parent != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out lastLocalPointerPosition);
            }

            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            if (parent != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition))
            {
                target.anchoredPosition += localPointerPosition - lastLocalPointerPosition;
                lastLocalPointerPosition = localPointerPosition;
            }
            else
            {
                target.anchoredPosition += eventData.delta;
            }

            eventData.Use();
        }
    }
}
