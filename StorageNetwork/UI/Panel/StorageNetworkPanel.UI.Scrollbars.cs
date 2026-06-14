using StorageNetwork.Components;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private static Scrollbar CreateScrollbar(Transform parent)
        {
            return CreateScrollbar(parent, 4f, 4f);
        }

        private static Scrollbar CreateScrollbar(Transform parent, float topInset, float bottomInset)
        {
            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-13f, bottomInset);
            scrollbarRect.offsetMax = new Vector2(-4f, -topInset);

            Image background = scrollbarObject.AddComponent<Image>();
            ApplyVerticalScrollbarFrame(background);

            GameObject slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingRect = slidingArea.AddComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.anchoredPosition = Vector2.zero;
            slidingRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(16f, -10f);

            Image handleImage = handleObject.AddComponent<Image>();
            ApplyVerticalScrollbarHandle(handleImage);

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.interactable = true;
            scrollbar.transition = Selectable.Transition.None;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }

        private static void ConfigureSmoothVerticalScroll(ScrollRect scrollRect, float sensitivity)
        {
            if (scrollRect == null)
            {
                return;
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.10f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.08f;
            scrollRect.scrollSensitivity = sensitivity;

            SmoothScrollEdgeBounce edgeBounce = scrollRect.gameObject.GetComponent<SmoothScrollEdgeBounce>();
            if (edgeBounce == null)
            {
                edgeBounce = scrollRect.gameObject.AddComponent<SmoothScrollEdgeBounce>();
            }

            edgeBounce.Configure(scrollRect);
        }

        private static void ApplyVerticalScrollbarFrame(Image image)
        {
            ApplyScrollbarSprite(image, "build_menu_scrollbar_frame", Color.white, new Color(0.09f, 0.1f, 0.12f, 1f));
        }

        private static void ApplyVerticalScrollbarHandle(Image image)
        {
            ApplyScrollbarSprite(image, "build_menu_scrollbar_inner", new Color(0.6313726f, 0.6392157f, 0.682353f, 1f), new Color(0.6313726f, 0.6392157f, 0.682353f, 1f));
        }

        private static void ApplyScrollbarSprite(Image image, string spriteName, Color spriteColor, Color fallbackColor)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSpriteByName(spriteName);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.pixelsPerUnitMultiplier = 1f;
                image.color = spriteColor;
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = fallbackColor;
        }
    }
}
