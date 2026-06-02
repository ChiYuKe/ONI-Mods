using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModConfig
{
    internal sealed class SmoothScrollEdgeBounce : MonoBehaviour, IScrollHandler
    {
        private const float MaxBounce = 26f;
        private const float BounceScale = 4.5f;
        private const float ReturnSpeed = 18f;

        private ScrollRect scrollRect;
        private float appliedOffset;
        private float renderedOffset;

        public void Configure(ScrollRect target)
        {
            scrollRect = target;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (scrollRect == null || scrollRect.content == null || !scrollRect.vertical)
            {
                return;
            }

            float wheel = eventData.scrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f)
            {
                return;
            }

            bool pushingPastTop = scrollRect.verticalNormalizedPosition >= 0.999f && wheel > 0f;
            bool pushingPastBottom = scrollRect.verticalNormalizedPosition <= 0.001f && wheel < 0f;
            if (!pushingPastTop && !pushingPastBottom)
            {
                return;
            }

            float direction = pushingPastTop ? -1f : 1f;
            appliedOffset = Mathf.Clamp(appliedOffset + direction * Mathf.Abs(wheel) * BounceScale, -MaxBounce, MaxBounce);
        }

        private void LateUpdate()
        {
            if (scrollRect == null || scrollRect.content == null || Mathf.Abs(appliedOffset) < 0.01f)
            {
                if (scrollRect != null && scrollRect.content != null && Mathf.Abs(renderedOffset) > 0.01f)
                {
                    Vector2 resetPosition = scrollRect.content.anchoredPosition;
                    resetPosition.y -= renderedOffset;
                    scrollRect.content.anchoredPosition = resetPosition;
                }

                appliedOffset = 0f;
                renderedOffset = 0f;
                return;
            }

            Vector2 position = scrollRect.content.anchoredPosition;
            position.y -= renderedOffset;
            position.y += appliedOffset;
            scrollRect.content.anchoredPosition = position;
            renderedOffset = appliedOffset;

            appliedOffset = Mathf.Lerp(appliedOffset, 0f, Time.unscaledDeltaTime * ReturnSpeed);
        }
    }
}
