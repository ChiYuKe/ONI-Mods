using UnityEngine;
using UnityEngine.UI;

namespace TestMod
{
    public sealed class UiPrefabScreen
    {
        internal UiPrefabScreen(GameObject rootObject)
        {
            RootObject = rootObject;
            RootTransform = rootObject != null ? rootObject.transform : null;
            RootRect = RootTransform != null ? RootTransform.GetComponent<RectTransform>() : null;

            Panel = Find("Panel");
            Header = Find("Panel/Header");
            Label = Find("Panel/Header/Label");
            CloseButton = Find("Panel/Header/CloseButton");
            Content = Find("Panel/Content");
        }

        public GameObject RootObject { get; }
        public Transform RootTransform { get; }
        public RectTransform RootRect { get; }
        public Transform Panel { get; }
        public Transform Header { get; }
        public Transform Label { get; }
        public Transform CloseButton { get; }
        public Transform Content { get; }

        public Transform Find(string path)
        {
            if (RootTransform == null || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string normalizedPath = path.Trim().Replace('\\', '/');
            if (normalizedPath == RootTransform.name)
            {
                return RootTransform;
            }

            if (normalizedPath.StartsWith(RootTransform.name + "/"))
            {
                normalizedPath = normalizedPath.Substring(RootTransform.name.Length + 1);
            }

            return RootTransform.Find(normalizedPath);
        }

        public T Get<T>(string path) where T : Component
        {
            Transform target = Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }

        public KButton GetKButton(string path)
        {
            return Get<KButton>(path);
        }

        public LocText GetLocText(string path)
        {
            return Get<LocText>(path);
        }

        public void SetHeaderText(string text)
        {
            LocText labelText = Label != null ? Label.GetComponent<LocText>() : null;
            if (labelText != null)
            {
                labelText.text = text;
            }
        }

        public void BindKButton(string path, System.Action onClick, bool createIfMissing = false)
        {
            Transform target = Find(path);
            if (target == null)
            {
                return;
            }

            KButton button = target.GetComponent<KButton>();
            if (button == null)
            {
                button = target.GetComponentInChildren<KButton>(true);
            }

            if (button == null)
            {
                if (createIfMissing)
                {
                    Debug.LogWarning("[UiPrefabToolkit] Missing KButton at path: " + path + ". Please add a configured KButton in the prefab.");
                }

                return;
            }

            if (!TryPrepareExistingKButton(button, target.gameObject, path))
            {
                return;
            }

            button.onClick -= onClick;
            button.onClick += onClick;
        }

        public void BindClose(System.Action onClose)
        {
            if (CloseButton == null)
            {
                return;
            }

            if (TryBindExistingButton("Panel/Header/CloseButton", onClose))
            {
                return;
            }

            CreateOverlayCloseButton(onClose);
        }

        public void CenterOnScreen()
        {
            if (RootRect == null)
            {
                return;
            }

            RootRect.anchorMin = new Vector2(0.5f, 0.5f);
            RootRect.anchorMax = new Vector2(0.5f, 0.5f);
            RootRect.pivot = new Vector2(0.5f, 0.5f);
            RootRect.anchoredPosition = Vector2.zero;
        }

        public void SetSize(float width, float height)
        {
            RectTransform targetRect = Panel != null ? Panel.GetComponent<RectTransform>() : null;
            if (targetRect == null)
            {
                targetRect = RootRect;
            }

            if (targetRect == null)
            {
                return;
            }

            targetRect.anchorMin = new Vector2(0.5f, 0.5f);
            targetRect.anchorMax = new Vector2(0.5f, 0.5f);
            targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.sizeDelta = new Vector2(width, height);
            targetRect.anchoredPosition = Vector2.zero;

            ApplyStandardBaseScreenLayout(targetRect);
        }

        public void Destroy()
        {
            if (RootObject != null)
            {
                Object.Destroy(RootObject);
            }
        }

        private bool TryBindExistingButton(string path, System.Action onClick)
        {
            Transform target = Find(path);
            if (target == null)
            {
                return false;
            }

            KButton button = target.GetComponent<KButton>();
            if (button == null)
            {
                button = target.GetComponentInChildren<KButton>(true);
            }

            if (button == null)
            {
                return false;
            }

            if (!TryPrepareExistingKButton(button, target.gameObject, path))
            {
                return false;
            }

            button.onClick -= onClick;
            button.onClick += onClick;
            return true;
        }

        private void CreateOverlayCloseButton(System.Action onClick)
        {
            if (CloseButton == null)
            {
                return;
            }

            Transform existing = CloseButton.Find("UiPrefabToolkitOverlayButton");
            GameObject overlay = existing != null ? existing.gameObject : new GameObject("UiPrefabToolkitOverlayButton");
            overlay.transform.SetParent(CloseButton, false);

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            if (overlayRect == null)
            {
                overlayRect = overlay.AddComponent<RectTransform>();
            }

            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);

            KImage background = overlay.GetComponent<KImage>();
            if (background == null)
            {
                background = overlay.AddComponent<KImage>();
            }

            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = new Color(1f, 1f, 1f, 0.01f);
            background.raycastTarget = true;
            background.colorStyleSetting = background.colorStyleSetting ?? CreateFallbackButtonStyle();
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton button = overlay.GetComponent<KButton>();
            if (button == null)
            {
                button = overlay.AddComponent<KButton>();
            }

            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = button.soundPlayer ?? new ButtonSoundPlayer();
            button.onClick -= onClick;
            button.onClick += onClick;
        }

        private static bool TryPrepareExistingKButton(KButton button, GameObject owner, string path)
        {
            if (button == null || owner == null)
            {
                return false;
            }

            KImage background = button.bgImage;
            if (background == null)
            {
                background = button.GetComponent<KImage>();
            }

            if (background == null || background.gameObject != button.gameObject)
            {
                Debug.LogWarning("[UiPrefabToolkit] KButton at path '" + path + "' is not configured in ONI style (KButton + KImage on the same object).");
                return false;
            }

            button.bgImage = background;
            button.additionalKImages = button.additionalKImages ?? new KImage[0];
            button.soundPlayer = button.soundPlayer ?? new ButtonSoundPlayer();

            if (button.bgImage.colorStyleSetting == null)
            {
                button.bgImage.colorStyleSetting = CreateFallbackButtonStyle();
            }

            button.bgImage.ColorState = KImage.ColorSelector.Inactive;
            return true;
        }

        private static ColorStyleSetting CreateFallbackButtonStyle()
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.activeColor = new Color(0.20f, 0.24f, 0.30f, 1f);
            style.inactiveColor = new Color(1f, 1f, 1f, 0.01f);
            style.hoverColor = new Color(0.35f, 0.42f, 0.52f, 0.45f);
            style.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.25f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
        }

        private void ApplyStandardBaseScreenLayout(RectTransform panelRect)
        {
            if (panelRect == null)
            {
                return;
            }

            RectTransform headerRect = Header != null ? Header.GetComponent<RectTransform>() : null;
            if (headerRect != null)
            {
                float headerHeight = headerRect.sizeDelta.y > 0f ? headerRect.sizeDelta.y : 24f;
                float topOffset = Mathf.Abs(headerRect.anchoredPosition.y);
                if (topOffset < 0.01f)
                {
                    topOffset = 0f;
                }

                headerRect.anchorMin = new Vector2(0f, 1f);
                headerRect.anchorMax = new Vector2(1f, 1f);
                headerRect.pivot = new Vector2(0.5f, 1f);
                headerRect.offsetMin = new Vector2(0f, -headerHeight - topOffset);
                headerRect.offsetMax = new Vector2(0f, -topOffset);
            }

            RectTransform closeRect = CloseButton != null ? CloseButton.GetComponent<RectTransform>() : null;
            if (closeRect != null)
            {
                float width = closeRect.sizeDelta.x > 0f ? closeRect.sizeDelta.x : 24f;
                float height = closeRect.sizeDelta.y > 0f ? closeRect.sizeDelta.y : 24f;

                closeRect.anchorMin = new Vector2(1f, 0.5f);
                closeRect.anchorMax = new Vector2(1f, 0.5f);
                closeRect.pivot = new Vector2(1f, 0.5f);
                closeRect.sizeDelta = new Vector2(width, height);
                closeRect.anchoredPosition = new Vector2(-4f, 0f);
            }
        }
    }
}
