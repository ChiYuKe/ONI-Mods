using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform target;
        private RectTransform parent;
        private Vector2 lastLocalPointerPosition;
        private string layoutKey;
        private Action<RectTransform> onDragEnded;

        // 绑定需要被拖动的窗口根节点；拖动组件通常挂在标题栏上。
        public StorageNetworkWindowDrag Configure(RectTransform targetRect, string key = null, Action<RectTransform> dragEnded = null)
        {
            target = targetRect;
            parent = targetRect != null ? targetRect.parent as RectTransform : null;
            layoutKey = key;
            onDragEnded = dragEnded;
            return this;
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

            ClampToScreen(target);
            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (target != null)
            {
                ClampToScreen(target);
                onDragEnded?.Invoke(target);
                SaveLayout(layoutKey, target);
            }

            eventData.Use();
        }

        // 拖动时至少保留标题栏可见，避免窗口被拖到屏幕外找不回来。
        public static void ClampToScreen(RectTransform rectTransform, float visibleMargin = 48f)
        {
            if (rectTransform == null)
            {
                return;
            }

            RectTransform parentRect = rectTransform.parent as RectTransform;
            float parentWidth = parentRect != null && parentRect.rect.width > 0f ? parentRect.rect.width : Screen.width;
            float parentHeight = parentRect != null && parentRect.rect.height > 0f ? parentRect.rect.height : Screen.height;
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            Vector2 anchor = rectTransform.anchorMin;
            Vector2 parentPivot = parentRect != null ? parentRect.pivot : new Vector2(0.5f, 0.5f);
            Vector2 anchorOffset = new Vector2(
                (anchor.x - parentPivot.x) * parentWidth,
                (anchor.y - parentPivot.y) * parentHeight);
            Vector2 localPosition = anchorOffset + rectTransform.anchoredPosition;

            float minX = -parentWidth * 0.5f - size.x * pivot.x + visibleMargin;
            float maxX = parentWidth * 0.5f + size.x * (1f - pivot.x) - visibleMargin;
            float minY = -parentHeight * 0.5f - size.y * pivot.y + visibleMargin;
            float maxY = parentHeight * 0.5f + size.y * (1f - pivot.y) - visibleMargin;

            Vector2 clampedLocalPosition = new Vector2(
                Mathf.Clamp(localPosition.x, minX, maxX),
                Mathf.Clamp(localPosition.y, minY, maxY));
            rectTransform.anchoredPosition = clampedLocalPosition - anchorOffset;
        }

        public static void SaveLayout(string key, RectTransform rectTransform)
        {
            if (string.IsNullOrEmpty(key) || rectTransform == null)
            {
                return;
            }

            Config config = Config.Instance;
            if (config.WindowLayouts == null)
            {
                config.WindowLayouts = new System.Collections.Generic.Dictionary<string, StorageNetworkWindowLayout>();
            }

            config.WindowLayouts[key] = new StorageNetworkWindowLayout
            {
                X = rectTransform.anchoredPosition.x,
                Y = rectTransform.anchoredPosition.y,
                Width = rectTransform.rect.width,
                Height = rectTransform.rect.height
            };
            Config.Save();
        }

        public static bool TryApplyLayout(string key, RectTransform rectTransform, Vector2 minSize, Vector2 maxSize)
        {
            if (string.IsNullOrEmpty(key) || rectTransform == null ||
                Config.Instance.WindowLayouts == null ||
                !Config.Instance.WindowLayouts.TryGetValue(key, out StorageNetworkWindowLayout layout))
            {
                return false;
            }

            if (layout.Width > 0f && layout.Height > 0f)
            {
                rectTransform.sizeDelta = new Vector2(
                    Mathf.Clamp(layout.Width, minSize.x, maxSize.x),
                    Mathf.Clamp(layout.Height, minSize.y, maxSize.y));
            }

            rectTransform.anchoredPosition = new Vector2(layout.X, layout.Y);
            ClampToScreen(rectTransform);
            return true;
        }
    }
}
