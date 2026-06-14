using UnityEngine;
using UnityEngine.EventSystems;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkPanZoom : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        private const float MinZoom = 0.55f;
        private const float MaxZoom = 2.0f;
        private const float ZoomStep = 0.12f;
        private const float DragSensitivity = 1.0f;
        private RectTransform viewport;
        private RectTransform content;

        // 绑定可视区域和实际内容；适用于材料树、追踪树这类需要平移缩放的画布。
        public void Configure(RectTransform targetViewport, RectTransform targetContent)
        {
            viewport = targetViewport;
            content = targetContent;
            if (content != null)
            {
                content.localScale = Vector3.one;
                ClampContent();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (content == null)
            {
                return;
            }

            content.anchoredPosition += eventData.delta * DragSensitivity;
            ClampContent();
            eventData.Use();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (viewport == null || content == null || Mathf.Abs(eventData.scrollDelta.y) < 0.01f)
            {
                return;
            }

            float oldScale = Mathf.Max(MinZoom, content.localScale.x);
            float newScale = Mathf.Clamp(oldScale * (1f + eventData.scrollDelta.y * ZoomStep), MinZoom, MaxZoom);
            if (Mathf.Approximately(oldScale, newScale))
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                Vector2 anchored = content.anchoredPosition;
                Vector2 contentPoint = (localPoint - anchored) / oldScale;
                content.localScale = new Vector3(newScale, newScale, 1f);
                content.anchoredPosition = localPoint - contentPoint * newScale;
            }
            else
            {
                content.localScale = new Vector3(newScale, newScale, 1f);
            }

            ClampContent();
            eventData.Use();
        }

        // 将内容限制在视口附近，避免拖动或缩放后整棵树完全跑出可见范围。
        private void ClampContent()
        {
            if (viewport == null || content == null)
            {
                return;
            }

            Vector2 position = content.anchoredPosition;
            float scale = Mathf.Max(MinZoom, content.localScale.x);
            float contentWidth = content.rect.width * scale;
            float contentHeight = content.rect.height * scale;
            float viewportWidth = viewport.rect.width;
            float viewportHeight = viewport.rect.height;
            float minX = Mathf.Min(0f, viewportWidth - contentWidth);
            float maxX = Mathf.Max(0f, viewportWidth - contentWidth);
            float minY = Mathf.Min(0f, contentHeight - viewportHeight);
            float maxY = Mathf.Max(0f, contentHeight - viewportHeight);

            position.x = contentWidth <= viewportWidth ? (viewportWidth - contentWidth) * 0.5f : Mathf.Clamp(position.x, minX, 0f);
            position.y = contentHeight <= viewportHeight ? -(viewportHeight - contentHeight) * 0.5f : Mathf.Clamp(position.y, 0f, maxY);
            content.anchoredPosition = position;
        }
    }
}
