using System.Text;
using System.Linq;
using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {
        private static StorageNetworkPanel instance;

        private StorageNetworkHub targetHub;
        private bool overviewMode;
        private TextMeshProUGUI summaryText;
        private RectTransform listContent;
        private RectTransform windowRect;
        private KInputController registeredController;
        private readonly Dictionary<Storage, bool> expandedStorages = new Dictionary<Storage, bool>();
        private float refreshElapsed;

        public string handlerName => gameObject.name;

        public KInputHandler inputHandler { get; set; }

        public static void Show(StorageNetworkHub hub)
        {
            if (hub == null)
            {
                return;
            }

            if (instance == null)
            {
                instance = Create();
            }

            instance.SetTarget(hub);
            instance.gameObject.SetActive(true);
        }

        public static void ShowOverview()
        {
            if (instance == null)
            {
                instance = Create();
            }

            instance.SetOverview();
            instance.gameObject.SetActive(true);
        }

        public static void CloseOverview()
        {
            if (instance != null && instance.overviewMode)
            {
                instance.Close();
            }
        }

        public static void CloseIfTarget(StorageNetworkHub hub)
        {
            if (instance != null && instance.targetHub == hub)
            {
                instance.Close();
            }
        }

        private static StorageNetworkPanel Create()
        {
            Transform parent = GameScreenManager.Instance?.ssOverlayCanvas?.transform;
            GameObject root = new GameObject("StorageNetworkPanel");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.08f);

            StorageNetworkPanel panel = root.AddComponent<StorageNetworkPanel>();
            panel.BuildWindow(root.transform);
            root.SetActive(false);
            return panel;
        }

        private void SetTarget(StorageNetworkHub hub)
        {
            targetHub = hub;
            overviewMode = false;
            Refresh();
        }

        private void SetOverview()
        {
            targetHub = null;
            overviewMode = true;
            Refresh();
        }

        private void OnEnable()
        {
            RegisterInputHandler();
        }

        private void OnDisable()
        {
            UnregisterInputHandler();
        }

        private void Update()
        {
            if (targetHub == null && !overviewMode)
            {
                return;
            }

            refreshElapsed += Time.unscaledDeltaTime;
            if (refreshElapsed >= 0.5f)
            {
                refreshElapsed = 0f;
                Refresh();
            }
        }

        private void BuildWindow(Transform parent)
        {
            GameObject window = CreateBox("Window", parent, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(880f, 500f);

            GameObject header = CreateBox("Header", window.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 6f, 6f, 6f, 28f);

            TextMeshProUGUI title = CreateText("Title", header.transform, "储存网络", 14, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);

            GameObject closeButton = CreateButton("CloseButton", header.transform, "X", Close);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-4f, 0f);
            closeRect.sizeDelta = new Vector2(24f, 22f);

            GameObject content = CreateBox("Content", window.transform, new Color(0.88f, 0.89f, 0.91f, 0.98f));
            SetStretch(content.GetComponent<RectTransform>(), 8f, 8f, 8f, 42f);

            GameObject summary = CreateBox("Summary", content.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(summary.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);
            summaryText = CreateText("SummaryText", summary.transform, string.Empty, 14, TextAlignmentOptions.TopLeft);
            summaryText.lineSpacing = 4f;
            Stretch(summaryText.rectTransform(), 12f, 7f);

            GameObject list = CreateBox("List", content.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(list.GetComponent<RectTransform>(), 8f, 8f, 8f, 70f);

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(list.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            SetStretch(viewportRect, 8f, 22f, 8f, 8f);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewport.transform, false);
            listContent = contentObject.AddComponent<RectTransform>();
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.offsetMin = Vector2.zero;
            listContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup listLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 5f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(list.transform);

            ScrollRect scrollRect = list.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = listContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 4f;

            list.AddComponent<ScrollWheelBlocker>();
        }

        private void Refresh()
        {
            if (targetHub == null || summaryText == null || listContent == null)
            {
                if (overviewMode)
                {
                    RefreshOverview();
                }

                return;
            }

            targetHub.RefreshNetwork();
            summaryText.text =
                "<b>网络总览</b>\n" +
                string.Format("已连接储存：{0}    容量：{1} / {2}",
                    targetHub.ConnectedStorages.Count,
                    GameUtil.GetFormattedMass(targetHub.TotalStoredKg),
                    GameUtil.GetFormattedMass(targetHub.TotalCapacityKg));

            if (targetHub.ConnectedStorages.Count == 0)
            {
                ClearList();
                CreateInfoRow("未连接储存建筑", "把储存建筑贴近储存网络线缆，或让线缆经过建筑相邻格。");
                return;
            }

            RebuildStorageRows(targetHub.ConnectedStorages);
        }

        private void RebuildStorageRows(IEnumerable<StorageNetworkStorageInfo> storages)
        {
            ClearList();

            foreach (StorageNetworkStorageInfo storage in targetHub.ConnectedStorages)
            {
                CreateStorageRow(storage);
            }
        }

        private void RefreshOverview()
        {
            if (summaryText == null || listContent == null)
            {
                return;
            }

            StorageNetworkHub[] hubs = StorageNetworkRegistry.RegisteredHubs
                .Where(hub => hub != null)
                .OrderBy(hub => hub.GetProperName())
                .ToArray();

            float totalStored = 0f;
            float totalCapacity = 0f;
            foreach (StorageNetworkHub hub in hubs)
            {
                hub.RefreshNetwork();
                totalStored += hub.TotalStoredKg;
                totalCapacity += hub.TotalCapacityKg;
            }

            summaryText.text =
                "<b>储存网络概览</b>\n" +
                string.Format("网络核心：{0}    总容量：{1} / {2}",
                    hubs.Length,
                    GameUtil.GetFormattedMass(totalStored),
                    GameUtil.GetFormattedMass(totalCapacity));

            if (hubs.Length == 0)
            {
                ClearList();
                CreateInfoRow("未建造储存网络核心", string.Empty);
                return;
            }

            ClearList();
            foreach (StorageNetworkHub hub in hubs)
            {
                float percent = hub.TotalCapacityKg > 0f ? hub.TotalStoredKg / hub.TotalCapacityKg : 0f;
                CreateInfoRow(
                    hub.GetProperName(),
                    string.Format("{0} / {1}  {2}%  {3} 个储存",
                        GameUtil.GetFormattedMass(hub.TotalStoredKg),
                        GameUtil.GetFormattedMass(hub.TotalCapacityKg),
                        Mathf.RoundToInt(percent * 100f),
                        hub.ConnectedStorages.Count));
            }
        }

        private void CreateStorageRow(StorageNetworkStorageInfo storageInfo)
        {
            Storage storage = storageInfo.Storage;
            if (storage == null)
            {
                return;
            }

            bool expanded = expandedStorages.TryGetValue(storage, out bool isExpanded) && isExpanded;
            float percent = storageInfo.CapacityKg > 0f ? storageInfo.StoredKg / storageInfo.CapacityKg : 0f;

            GameObject row = CreateBox("StorageRow", listContent, new Color(0.88f, 0.87f, 0.82f, 1f));
            VerticalLayoutGroup rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = true;

            GameObject header = CreateBox("Header", row.transform, new Color(0.72f, 0.72f, 0.68f, 1f));
            header.AddComponent<LayoutElement>().preferredHeight = 34f;
            Button button = header.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                expandedStorages[storage] = !expanded;
                Refresh();
            });

            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 10, 0, 0);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI arrow = CreateText("Arrow", header.transform, expanded ? "▼" : "▶", 14, TextAlignmentOptions.Center);
            arrow.color = new Color(0.1f, 0.11f, 0.12f, 1f);
            arrow.gameObject.AddComponent<LayoutElement>().preferredWidth = 20f;

            TextMeshProUGUI name = CreateText("Name", header.transform, storageInfo.Name, 13, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            name.fontStyle = FontStyles.Bold;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI amount = CreateText(
                "Amount",
                header.transform,
                string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedMass(storageInfo.StoredKg),
                    GameUtil.GetFormattedMass(storageInfo.CapacityKg),
                    Mathf.RoundToInt(percent * 100f)),
                13,
                TextAlignmentOptions.MidlineRight);
            amount.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = 260f;

            if (!expanded)
            {
                row.AddComponent<LayoutElement>().preferredHeight = 34f;
                return;
            }

            GameObject details = CreateBox("Details", row.transform, new Color(0.82f, 0.82f, 0.77f, 1f));
            VerticalLayoutGroup detailsLayout = details.AddComponent<VerticalLayoutGroup>();
            detailsLayout.padding = new RectOffset(12, 12, 8, 8);
            detailsLayout.spacing = 3f;
            detailsLayout.childControlHeight = true;
            detailsLayout.childControlWidth = true;
            detailsLayout.childForceExpandHeight = false;
            detailsLayout.childForceExpandWidth = true;

            List<GameObject> items = storage.items.Where(item => item != null).ToList();
            if (items.Count == 0)
            {
                TextMeshProUGUI empty = CreateText("Empty", details.transform, "没有储存内容", 12, TextAlignmentOptions.MidlineLeft);
                empty.color = new Color(0.34f, 0.35f, 0.35f, 1f);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            }
            else
            {
                foreach (IGrouping<string, GameObject> group in items.GroupBy(GetStoredItemName).OrderBy(group => group.Key))
                {
                    float mass = group.Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f);
                    CreateStoredItemRow(details.transform, group.Key, GameUtil.GetFormattedMass(mass), group.FirstOrDefault());
                }
            }

            ContentSizeFitter fitter = details.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void CreateInfoRow(string title, string details)
        {
            GameObject row = CreateBox("InfoRow", listContent, new Color(0.88f, 0.87f, 0.82f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 42f;
            TextMeshProUGUI text = CreateText("Text", row.transform, string.IsNullOrEmpty(details) ? title : title + "\n" + details, 13, TextAlignmentOptions.MidlineLeft);
            text.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            Stretch(text.rectTransform(), 12f, 6f);
        }

        private void ClearList()
        {
            if (listContent == null)
            {
                return;
            }

            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
        }

        private static string GetStoredItemName(GameObject item)
        {
            return item != null ? item.GetProperName() : string.Empty;
        }

        private static void CreateStoredItemRow(Transform parent, string itemName, string formattedMass, GameObject representative)
        {
            GameObject row = new GameObject("ItemRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 24f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>();
            iconObject.AddComponent<LayoutElement>().preferredWidth = 22f;

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            SetStoredItemIcon(icon, representative);

            TextMeshProUGUI itemText = CreateText(
                "Text",
                row.transform,
                string.Format("{0}    {1}", itemName, formattedMass),
                12,
                TextAlignmentOptions.MidlineLeft);
            itemText.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            itemText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static void SetStoredItemIcon(Image icon, GameObject item)
        {
            if (icon == null || item == null)
            {
                return;
            }

            Sprite sprite = null;
            Color tint = Color.white;

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                var uiSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                sprite = uiSprite.first;
                tint = uiSprite.second;
            }

            if (sprite == null)
            {
                PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                if (primaryElement != null)
                {
                    var uiSprite = Def.GetUISprite(primaryElement.ElementID.CreateTag(), "ui", false);
                    sprite = uiSprite.first;
                    tint = uiSprite.second;
                }
            }

            icon.sprite = sprite;
            icon.color = sprite != null ? tint : Color.clear;
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.Consumed || !IsMouseOverWindow())
            {
                return;
            }

            if (!e.TryConsume(global::Action.ZoomIn))
            {
                e.TryConsume(global::Action.ZoomOut);
            }
        }

        private bool IsMouseOverWindow()
        {
            if (windowRect == null)
            {
                return false;
            }

            Vector2 localMousePosition = windowRect.InverseTransformPoint(KInputManager.GetMousePos());
            return windowRect.rect.Contains(localMousePosition);
        }

        private void RegisterInputHandler()
        {
            if (registeredController != null || KInputManager.currentController == null)
            {
                return;
            }

            registeredController = KInputManager.currentController;
            KInputHandler.Add(registeredController, this, 100);
        }

        private void UnregisterInputHandler()
        {
            if (registeredController == null)
            {
                return;
            }

            KInputHandler.Remove(registeredController, this);
            registeredController = null;
        }

        private static GameObject CreateBox(string name, Transform parent, Color color)
        {
            GameObject box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.AddComponent<RectTransform>();
            Image image = box.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Sliced;
            return box;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = size;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, System.Action onClick)
        {
            GameObject buttonObject = CreateBox(name, parent, new Color(0.2f, 0.22f, 0.3f, 1f));
            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            TextMeshProUGUI label = CreateText("Label", buttonObject.transform, text, 16, TextAlignmentOptions.Center);
            Stretch(label.rectTransform(), 0f, 0f);
            return buttonObject;
        }

        private static Scrollbar CreateScrollbar(Transform parent)
        {
            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-18f, 8f);
            scrollbarRect.offsetMax = new Vector2(-8f, -8f);

            Image background = scrollbarObject.AddComponent<Image>();
            background.color = new Color(0.48f, 0.49f, 0.50f, 1f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(0.22f, 0.25f, 0.34f, 1f);

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }

        private static void Stretch(RectTransform rectTransform, float horizontal, float vertical)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(horizontal, vertical);
            rectTransform.offsetMax = new Vector2(-horizontal, -vertical);
        }

        private static void SetStretch(RectTransform rectTransform, float left, float right, float bottom, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopStretch(RectTransform rectTransform, float left, float right, float top, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -top - height);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private sealed class ScrollWheelBlocker : MonoBehaviour, IScrollHandler
        {
            public void OnScroll(PointerEventData eventData)
            {
                eventData.Use();
            }
        }
    }
}
