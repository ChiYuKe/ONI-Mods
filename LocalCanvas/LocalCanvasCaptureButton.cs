using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace LocalCanvas
{
    internal static class LocalCanvasCaptureController
    {
        private static LocalCanvasCaptureOverlay activeOverlay;

        public static void Begin(Artable target)
        {
            if (target == null)
            {
                Debug.LogWarning("[LocalCanvas] capture was requested without a canvas target");
                return;
            }

            KPrefabID prefabComponent = target.GetComponent<KPrefabID>();
            string prefabId = prefabComponent == null ? null : prefabComponent.PrefabID().ToString();
            if (prefabId != "Canvas" && prefabId != "CanvasTall" && prefabId != "CanvasWide")
            {
                Debug.LogWarning("[LocalCanvas] capture target is not a supported canvas: " + prefabId);
                return;
            }

            SelectTool.Instance.Select(null, true);
            if (!DebugHandler.ScreenshotMode)
            {
                DebugHandler.ToggleScreenshotMode();
            }

            GameObject root = new GameObject("LocalCanvasCaptureOverlay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image blocker = root.AddComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true;

            LocalCanvasCaptureOverlay overlay = root.AddComponent<LocalCanvasCaptureOverlay>();
            activeOverlay = overlay;
            overlay.Initialize(prefabId);
        }

        public static bool TryHandleInput(KButtonEvent input)
        {
            if (activeOverlay == null)
            {
                return false;
            }

            // 右键留给游戏本身，用于拖动摄像机，不退出截图模式。
            if (input.IsAction(global::Action.MouseRight))
            {
                return false;
            }

            if (input.IsAction(global::Action.MouseLeft))
            {
                input.TryConsume(global::Action.MouseLeft);
                activeOverlay.BeginPointer(Input.mousePosition);
                return true;
            }

            if (input.IsAction(global::Action.Escape))
            {
                input.TryConsume(input.GetAction());
                activeOverlay.Cancel();
                return true;
            }

            return true;
        }

        public static bool TryHandleInputUp(KButtonEvent input)
        {
            if (activeOverlay == null)
            {
                return false;
            }

            // 右键释放也必须放行，否则摄像机拖动会被截图模式截断。
            if (input.IsAction(global::Action.MouseRight))
            {
                return false;
            }

            if (input.IsAction(global::Action.MouseLeft))
            {
                input.TryConsume(global::Action.MouseLeft);
                activeOverlay.EndPointer(Input.mousePosition);
                return true;
            }

            return true;
        }

        internal static void ClearActive(LocalCanvasCaptureOverlay overlay)
        {
            if (activeOverlay == overlay)
            {
                activeOverlay = null;
            }
        }
    }

    internal sealed class LocalCanvasCaptureOverlay : MonoBehaviour
    {
        private const float BorderThickness = 4f;
        private const float ButtonWidth = 100f;
        private const float ButtonHeight = 36f;
        private const float ButtonGap = 8f;
        private const float ResizeHandleSize = 18f;
        private const float ResizeHitSize = 30f;
        private const float MinimumCaptureWidth = 120f;
        private const float OutsideShadeAlpha = 0.45f;
        private static readonly Color FrameColor = new Color(0.15f, 0.65f, 1f, 1f);

        private enum PointerMode
        {
            None,
            Move,
            Resize
        }

        private string prefabId;
        private float aspectRatio;
        private Rect captureRect;
        private Rect cancelRect;
        private Rect saveRect;
        private Rect[] resizeRects;
        private RectTransform[] borders;
        private RectTransform[] resizeHandles;
        private RectTransform[] outsideShades;
        private RectTransform cancelButton;
        private RectTransform saveButton;
        private Vector2 center;
        private Vector2 pointerLast;
        private bool pointerDown;
        private PointerMode pointerMode;
        private int resizeCorner;
        private Vector2 resizeOpposite;
        private float resizeDirectionX;
        private float resizeDirectionY;
        private bool closing;

        public void Initialize(string targetPrefabId)
        {
            prefabId = targetPrefabId;
            aspectRatio = prefabId == "CanvasTall" ? 2f / 3f : prefabId == "CanvasWide" ? 3f / 2f : 1f;
            float longSide = Mathf.Min(Screen.width * 0.55f, Screen.height * 0.55f);
            float shortSide = longSide * 2f / 3f;
            float width = prefabId == "CanvasTall" ? shortSide : longSide;
            float height = prefabId == "CanvasWide" ? shortSide : longSide;
            center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            outsideShades = new RectTransform[4];
            outsideShades[0] = CreateOutsideShade("Top");
            outsideShades[1] = CreateOutsideShade("Bottom");
            outsideShades[2] = CreateOutsideShade("Left");
            outsideShades[3] = CreateOutsideShade("Right");
            borders = new RectTransform[4];
            borders[0] = CreateBorder("Top");
            borders[1] = CreateBorder("Bottom");
            borders[2] = CreateBorder("Left");
            borders[3] = CreateBorder("Right");
            resizeHandles = new RectTransform[4];
            resizeHandles[0] = CreateResizeHandle("TopLeft");
            resizeHandles[1] = CreateResizeHandle("TopRight");
            resizeHandles[2] = CreateResizeHandle("BottomLeft");
            resizeHandles[3] = CreateResizeHandle("BottomRight");
            resizeRects = new Rect[4];
            cancelButton = CreateButton("CancelButton", "取消");
            saveButton = CreateButton("SaveButton", "保存");
            UpdateLayout(width, height);

        }

        private RectTransform CreateBorder(string name)
        {
            GameObject bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(transform, false);
            Image image = bar.GetComponent<Image>();
            image.color = FrameColor;
            image.raycastTarget = false;
            return bar.GetComponent<RectTransform>();
        }

        private RectTransform CreateOutsideShade(string name)
        {
            GameObject shade = new GameObject("OutsideShade" + name, typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(transform, false);
            Image image = shade.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, OutsideShadeAlpha);
            image.raycastTarget = false;
            return shade.GetComponent<RectTransform>();
        }

        private RectTransform CreateButton(string name, string labelText)
        {
            GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image));
            button.transform.SetParent(transform, false);
            Image background = button.GetComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
            background.raycastTarget = false;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(button.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.text = labelText;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 18;
            label.color = Color.white;
            label.raycastTarget = false;
            return button.GetComponent<RectTransform>();
        }

        private RectTransform CreateResizeHandle(string name)
        {
            GameObject handle = new GameObject("Resize" + name, typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(transform, false);
            Image image = handle.GetComponent<Image>();
            image.color = FrameColor;
            image.raycastTarget = false;
            return handle.GetComponent<RectTransform>();
        }

        private void UpdateLayout(float width, float height)
        {
            float buttonRowWidth = ButtonWidth * 2f + ButtonGap;
            float minCenterY = height * 0.5f + ButtonHeight + 20f;
            center.x = Mathf.Clamp(center.x, width * 0.5f + 8f, Screen.width - width * 0.5f - 8f);
            center.y = Mathf.Clamp(center.y, minCenterY, Screen.height - height * 0.5f - 8f);
            captureRect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);

            SetScreenRect(outsideShades[0], new Rect(0f, captureRect.yMax, Screen.width, Screen.height - captureRect.yMax));
            SetScreenRect(outsideShades[1], new Rect(0f, 0f, Screen.width, captureRect.yMin));
            SetScreenRect(outsideShades[2], new Rect(0f, captureRect.yMin, captureRect.xMin, captureRect.height));
            SetScreenRect(outsideShades[3], new Rect(captureRect.xMax, captureRect.yMin, Screen.width - captureRect.xMax, captureRect.height));

            SetRect(borders[0], new Vector2(width + BorderThickness, BorderThickness), new Vector2(0f, height * 0.5f));
            SetRect(borders[1], new Vector2(width + BorderThickness, BorderThickness), new Vector2(0f, -height * 0.5f));
            SetRect(borders[2], new Vector2(BorderThickness, height), new Vector2(-width * 0.5f, 0f));
            SetRect(borders[3], new Vector2(BorderThickness, height), new Vector2(width * 0.5f, 0f));

            SetRect(resizeHandles[0], new Vector2(ResizeHandleSize, ResizeHandleSize), new Vector2(-width * 0.5f, height * 0.5f));
            SetRect(resizeHandles[1], new Vector2(ResizeHandleSize, ResizeHandleSize), new Vector2(width * 0.5f, height * 0.5f));
            SetRect(resizeHandles[2], new Vector2(ResizeHandleSize, ResizeHandleSize), new Vector2(-width * 0.5f, -height * 0.5f));
            SetRect(resizeHandles[3], new Vector2(ResizeHandleSize, ResizeHandleSize), new Vector2(width * 0.5f, -height * 0.5f));
            resizeRects[0] = new Rect(captureRect.xMin - ResizeHitSize * 0.5f, captureRect.yMax - ResizeHitSize * 0.5f, ResizeHitSize, ResizeHitSize);
            resizeRects[1] = new Rect(captureRect.xMax - ResizeHitSize * 0.5f, captureRect.yMax - ResizeHitSize * 0.5f, ResizeHitSize, ResizeHitSize);
            resizeRects[2] = new Rect(captureRect.xMin - ResizeHitSize * 0.5f, captureRect.yMin - ResizeHitSize * 0.5f, ResizeHitSize, ResizeHitSize);
            resizeRects[3] = new Rect(captureRect.xMax - ResizeHitSize * 0.5f, captureRect.yMin - ResizeHitSize * 0.5f, ResizeHitSize, ResizeHitSize);

            float buttonY = -height * 0.5f - 20f - ButtonHeight * 0.5f;
            SetRect(cancelButton, new Vector2(ButtonWidth, ButtonHeight), new Vector2(-(buttonRowWidth - ButtonWidth) * 0.5f, buttonY));
            SetRect(saveButton, new Vector2(ButtonWidth, ButtonHeight), new Vector2((buttonRowWidth - ButtonWidth) * 0.5f, buttonY));
            cancelRect = new Rect(center.x - buttonRowWidth * 0.5f, center.y + buttonY - ButtonHeight * 0.5f, ButtonWidth, ButtonHeight);
            saveRect = new Rect(center.x + ButtonGap * 0.5f, center.y + buttonY - ButtonHeight * 0.5f, ButtonWidth, ButtonHeight);
        }

        private void SetRect(RectTransform rect, Vector2 size, Vector2 localPosition)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(center.x - Screen.width * 0.5f, center.y - Screen.height * 0.5f) + localPosition;
        }

        private void SetScreenRect(RectTransform rect, Rect screenRect)
        {
            float width = Mathf.Max(0f, screenRect.width);
            float height = Mathf.Max(0f, screenRect.height);
            Vector2 screenCenter = new Vector2(screenRect.x + width * 0.5f, screenRect.y + height * 0.5f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = screenCenter - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        public void BeginPointer(Vector2 screenPosition)
        {
            if (closing)
            {
                return;
            }

            pointerDown = true;
            pointerLast = screenPosition;
            pointerMode = PointerMode.None;
            for (int i = 0; i < resizeRects.Length; i++)
            {
                if (resizeRects[i].Contains(screenPosition))
                {
                    pointerMode = PointerMode.Resize;
                    resizeCorner = i;
                    SetResizeAnchor(i);
                    break;
                }
            }

            if (pointerMode == PointerMode.None && captureRect.Contains(screenPosition))
            {
                pointerMode = PointerMode.Move;
            }
        }

        public void EndPointer(Vector2 screenPosition)
        {
            if (!pointerDown || closing)
            {
                return;
            }

            pointerDown = false;
            if (pointerMode != PointerMode.None)
            {
                pointerMode = PointerMode.None;
                return;
            }

            if (cancelRect.Contains(screenPosition))
            {
                Cancel();
            }
            else if (saveRect.Contains(screenPosition))
            {
                Confirm();
            }
        }

        private void Update()
        {
            if (pointerDown)
            {
                Vector2 current = Input.mousePosition;
                if (pointerMode == PointerMode.Move)
                {
                    center += current - pointerLast;
                    pointerLast = current;
                    UpdateLayout(captureRect.width, captureRect.height);
                }
                else if (pointerMode == PointerMode.Resize)
                {
                    ResizeFromCorner(current);
                }
            }

            // Esc 是截图模式的备用退出方式；右键保留给游戏拖动摄像机。
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
            }
        }

        private void ResizeFromCorner(Vector2 pointer)
        {
            float requestedWidth = Mathf.Max(Mathf.Abs(pointer.x - resizeOpposite.x), Mathf.Abs(pointer.y - resizeOpposite.y) * aspectRatio);
            float maxWidthFromX = resizeDirectionX < 0f ? resizeOpposite.x - 8f : Screen.width - resizeOpposite.x - 8f;
            float maxHeightFromY = resizeDirectionY < 0f ? resizeOpposite.y - 8f : Screen.height - resizeOpposite.y - 8f;
            float maxWidth = Mathf.Min(maxWidthFromX, maxHeightFromY * aspectRatio);
            if (maxWidth <= 1f)
            {
                return;
            }

            float minimumWidth = Mathf.Min(MinimumCaptureWidth, maxWidth);
            float width = Mathf.Clamp(requestedWidth, minimumWidth, maxWidth);
            float height = width / aspectRatio;
            Vector2 newCorner = resizeOpposite + new Vector2(resizeDirectionX * width, resizeDirectionY * height);
            center = (resizeOpposite + newCorner) * 0.5f;
            UpdateLayout(width, height);
        }

        private void SetResizeAnchor(int corner)
        {
            switch (corner)
            {
                case 0:
                    resizeOpposite = new Vector2(captureRect.xMax, captureRect.yMin);
                    resizeDirectionX = -1f;
                    resizeDirectionY = 1f;
                    break;
                case 1:
                    resizeOpposite = new Vector2(captureRect.xMin, captureRect.yMin);
                    resizeDirectionX = 1f;
                    resizeDirectionY = 1f;
                    break;
                case 2:
                    resizeOpposite = new Vector2(captureRect.xMax, captureRect.yMax);
                    resizeDirectionX = -1f;
                    resizeDirectionY = -1f;
                    break;
                default:
                    resizeOpposite = new Vector2(captureRect.xMin, captureRect.yMax);
                    resizeDirectionX = 1f;
                    resizeDirectionY = -1f;
                    break;
            }
        }

        private void Confirm()
        {
            if (!closing)
            {
                StartCoroutine(Capture());
            }
        }

        public void Cancel()
        {
            Close();
        }

        private IEnumerator Capture()
        {
            closing = true;
            Canvas overlayCanvas = GetComponent<Canvas>();
            if (overlayCanvas != null)
            {
                overlayCanvas.enabled = false;
            }
            yield return new WaitForEndOfFrame();

            Texture2D screenshot = null;
            try
            {
                int x = Mathf.Clamp(Mathf.RoundToInt(captureRect.x), 0, Screen.width - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(captureRect.y), 0, Screen.height - 1);
                int width = Mathf.Clamp(Mathf.RoundToInt(captureRect.width), 1, Screen.width - x);
                int height = Mathf.Clamp(Mathf.RoundToInt(captureRect.height), 1, Screen.height - y);
                screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(x, y, width, height), 0, 0, false);
                screenshot.Apply(false, false);

                string folder = LocalCanvasConfig.GetImageFolderPath(prefabId);
                if (string.IsNullOrWhiteSpace(folder))
                {
                    throw new InvalidOperationException("image folder is empty for " + prefabId);
                }

                Directory.CreateDirectory(folder);
                string fileName = "Capture_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png";
                string path = Path.Combine(folder, fileName);
                File.WriteAllBytes(path, screenshot.EncodeToPNG());
            }
            catch (Exception ex)
            {
                Debug.LogError("[LocalCanvas] screenshot save failed: " + ex);
            }
            finally
            {
                if (screenshot != null)
                {
                    UnityEngine.Object.Destroy(screenshot);
                }

                Close();
            }
        }

        private void Close()
        {
            if (closing && gameObject == null)
            {
                return;
            }

            closing = true;
            if (DebugHandler.ScreenshotMode)
            {
                DebugHandler.ToggleScreenshotMode();
            }

            LocalCanvasCaptureController.ClearActive(this);
            UnityEngine.Object.Destroy(gameObject);
        }
    }
}
