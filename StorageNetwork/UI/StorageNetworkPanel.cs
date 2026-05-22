using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
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
        private GameObject modalRoot;
        private KInputController registeredController;
        private string selectedItemKey;
        private Storage selectedItemStorage;
        private readonly Dictionary<string, bool> expandedStorageTypes = new Dictionary<string, bool>();
        private readonly Dictionary<Storage, bool> expandedStorages = new Dictionary<Storage, bool>();
        private float refreshElapsed;
        private string lastListSignature;
        private const bool DebugLogging = true;

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
            lastListSignature = null;
            Refresh(true);
        }

        private void SetOverview()
        {
            targetHub = null;
            overviewMode = true;
            lastListSignature = null;
            Refresh(true);
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
            if (refreshElapsed >= 1f)
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

            GameObject closeButton = CreateGameButton("CloseButton", header.transform, "X", Close);
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

        private void Refresh(bool forceRebuild = false)
        {
            if (targetHub == null || summaryText == null || listContent == null)
            {
                if (overviewMode)
                {
                    RefreshOverview(forceRebuild);
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
                if (forceRebuild || lastListSignature != "empty")
                {
                    lastListSignature = "empty";
                    ClearList();
                    CreateInfoRow("未连接储存建筑", "把储存建筑贴近储存网络线缆，或让线缆经过建筑相邻格。");
                }

                return;
            }

            string listSignature = BuildListSignature(targetHub.ConnectedStorages);
            if (forceRebuild || listSignature != lastListSignature)
            {
                lastListSignature = listSignature;
                RebuildStorageRows(targetHub.ConnectedStorages);
            }
        }

        private void RebuildStorageRows(IEnumerable<StorageNetworkStorageInfo> storages)
        {
            ClearList();

            foreach (IGrouping<string, StorageNetworkStorageInfo> group in storages.GroupBy(GetStorageTypeKey).OrderBy(group => GetStorageTypeName(group.First())))
            {
                CreateStorageTypeRow(group.ToList());
            }

            RebuildLayout();
        }

        private static string BuildListSignature(IEnumerable<StorageNetworkStorageInfo> storages)
        {
            return string.Join("|", storages
                .OrderBy(GetStorageTypeKey)
                .ThenBy(storage => storage.Storage != null ? storage.Storage.GetInstanceID() : 0)
                .Select(storage =>
                {
                    string items = string.Join(",", storage.Storage.items
                        .Where(item => item != null)
                        .GroupBy(GetStoredItemKey)
                        .OrderBy(group => group.Key)
                        .Select(group => string.Format("{0}:{1}:{2:0.###}",
                            group.Key,
                            group.Count(),
                            group.Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f))));

                    return string.Format("{0}:{1}:{2:0.###}:{3:0.###}:{4}",
                        GetStorageTypeKey(storage),
                        storage.Storage != null ? storage.Storage.GetInstanceID() : 0,
                        storage.StoredKg,
                        storage.CapacityKg,
                        items);
                }));
        }

        private void RefreshOverview(bool forceRebuild = false)
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
                if (forceRebuild || lastListSignature != "overview-empty")
                {
                    lastListSignature = "overview-empty";
                    ClearList();
                    CreateInfoRow("未建造储存网络核心", string.Empty);
                }

                return;
            }

            string overviewSignature = "overview|" + string.Join("|", hubs.Select(hub =>
                string.Format("{0}:{1:0.###}:{2:0.###}:{3}",
                    hub.GetInstanceID(),
                    hub.TotalStoredKg,
                    hub.TotalCapacityKg,
                    hub.ConnectedStorages.Count)));

            if (!forceRebuild && overviewSignature == lastListSignature)
            {
                return;
            }

            lastListSignature = overviewSignature;
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

            RebuildLayout();
        }

        private void CreateStorageTypeRow(List<StorageNetworkStorageInfo> storages)
        {
            if (storages == null || storages.Count == 0)
            {
                return;
            }

            string typeKey = GetStorageTypeKey(storages[0]);
            string typeName = GetStorageTypeName(storages[0]);
            bool expanded = expandedStorageTypes.TryGetValue(typeKey, out bool isExpanded) && isExpanded;
            float storedKg = storages.Sum(storage => storage.StoredKg);
            float capacityKg = storages.Sum(storage => storage.CapacityKg);
            float percent = capacityKg > 0f ? storedKg / capacityKg : 0f;

            GameObject row = CreateBox("StorageTypeRow", listContent, new Color(0.86f, 0.85f, 0.80f, 1f));
            AddVerticalContainer(row, 0f, 0, 0, 0, 0);

            GameObject header = CreateFoldoutHeader(
                row.transform,
                expanded,
                string.Format("{0}  x{1}", typeName, storages.Count),
                string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedMass(storedKg),
                    GameUtil.GetFormattedMass(capacityKg),
                    Mathf.RoundToInt(percent * 100f)),
                new Color(0.66f, 0.67f, 0.62f, 1f),
                14,
                300f,
                () =>
                {
                    expandedStorageTypes[typeKey] = !expanded;
                    Refresh(true);
                });

            header.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);

            if (!expanded)
            {
                row.AddComponent<LayoutElement>().preferredHeight = 34f;
                return;
            }

            GameObject storageList = CreateBox("Storages", row.transform, new Color(0.80f, 0.80f, 0.75f, 1f));
            AddVerticalContainer(storageList, 4f, 18, 0, 4, 4);
            storageList.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (StorageNetworkStorageInfo storage in storages.OrderBy(storage => storage.Name))
            {
                CreateStorageRow(storage, storageList.transform);
            }

            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void CreateStorageRow(StorageNetworkStorageInfo storageInfo, Transform parent)
        {
            Storage storage = storageInfo.Storage;
            if (storage == null)
            {
                return;
            }

            bool expanded = expandedStorages.TryGetValue(storage, out bool isExpanded) && isExpanded;
            float percent = storageInfo.CapacityKg > 0f ? storageInfo.StoredKg / storageInfo.CapacityKg : 0f;

            GameObject row = CreateBox("StorageRow", parent, new Color(0.88f, 0.87f, 0.82f, 1f));
            AddVerticalContainer(row, 0f, 0, 0, 0, 0);

            CreateFoldoutHeader(
                row.transform,
                expanded,
                storageInfo.Name,
                string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedMass(storageInfo.StoredKg),
                    GameUtil.GetFormattedMass(storageInfo.CapacityKg),
                    Mathf.RoundToInt(percent * 100f)),
                new Color(0.72f, 0.72f, 0.68f, 1f),
                13,
                210f,
                () =>
                {
                    expandedStorages[storage] = !expanded;
                    Refresh(true);
                },
                "设置",
                () => ShowStorageSettingsDialog(storage));

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
                foreach (IGrouping<string, GameObject> group in items.GroupBy(GetStoredItemKey).OrderBy(group => GetStoredItemName(group.FirstOrDefault())))
                {
                    float mass = group.Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f);
                    CreateStoredItemRow(
                        storage,
                        details.transform,
                        group.Key,
                        GetStoredItemName(group.FirstOrDefault()),
                        GameUtil.GetFormattedMass(mass),
                        group.FirstOrDefault());
                }
            }

            ContentSizeFitter fitter = details.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private GameObject CreateFoldoutHeader(
            Transform parent,
            bool expanded,
            string title,
            string amountText,
            Color backgroundColor,
            int fontSize,
            float amountWidth,
            System.Action onClick,
            string actionText = null,
            System.Action actionClick = null)
        {
            GameObject header = CreateBox("Header", parent, backgroundColor);
            header.AddComponent<LayoutElement>().preferredHeight = 34f;

            UnityEngine.Object.DestroyImmediate(header.GetComponent<Image>());
            KImage headerImage = header.AddComponent<KImage>();
            headerImage.type = Image.Type.Sliced;
            ApplySprite(headerImage, "plan_menu_heading_bg_gray");
            headerImage.colorStyleSetting = CreateColorStyle(backgroundColor, Lighten(backgroundColor, 0.08f), Darken(backgroundColor, 0.08f));
            headerImage.ColorState = KImage.ColorSelector.Inactive;

            KButton button = header.AddComponent<KButton>();
            button.bgImage = headerImage;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 28, 0, 0);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            GameObject arrowObject = new GameObject("Arrow");
            arrowObject.transform.SetParent(header.transform, false);
            arrowObject.AddComponent<RectTransform>();
            LayoutElement arrowLayout = arrowObject.AddComponent<LayoutElement>();
            arrowLayout.preferredWidth = 20f;
            arrowLayout.preferredHeight = 18f;
            Image arrow = arrowObject.AddComponent<Image>();
            arrow.raycastTarget = false;
            arrow.preserveAspect = true;
            ApplySprite(arrow, expanded ? "stresspanel_icon_expand_arrow_up" : "stresspanel_icon_expand_arrow_right");
            if (arrow.sprite == null)
            {
                TextMeshProUGUI fallbackArrow = CreateText("FallbackArrow", arrowObject.transform, expanded ? "▼" : "▶", 14, TextAlignmentOptions.Center);
                fallbackArrow.color = new Color(0.1f, 0.11f, 0.12f, 1f);
                Stretch(fallbackArrow.rectTransform(), 0f, 0f);
            }

            TextMeshProUGUI name = CreateText("Name", header.transform, title, fontSize, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            name.fontStyle = FontStyles.Bold;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            if (!string.IsNullOrEmpty(actionText) && actionClick != null)
            {
                GameObject actionButton = CreateGameButton("HeaderActionButton", header.transform, actionText, actionClick);
                LayoutElement actionLayout = actionButton.AddComponent<LayoutElement>();
                actionLayout.preferredWidth = 50f;
                actionLayout.preferredHeight = 20f;
            }

            TextMeshProUGUI amount = CreateText("Amount", header.transform, amountText, fontSize, TextAlignmentOptions.MidlineRight);
            amount.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = amountWidth;

            return header;
        }

        private static void AddVerticalContainer(GameObject gameObject, float spacing, int left, int right, int top, int bottom)
        {
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(left, right, top, bottom);
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
        }

        private void CreateInfoRow(string title, string details)
        {
            GameObject row = CreateBox("InfoRow", listContent, new Color(0.88f, 0.87f, 0.82f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 42f;
            TextMeshProUGUI text = CreateText("Text", row.transform, string.IsNullOrEmpty(details) ? title : title + "\n" + details, 13, TextAlignmentOptions.MidlineLeft);
            text.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            Stretch(text.rectTransform(), 12f, 6f);
            RebuildLayout();
        }

        private void RebuildLayout()
        {
            if (listContent == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
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

        private void CreateStoredItemRow(Storage storage, Transform parent, string itemKey, string itemName, string formattedMass, GameObject representative)
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

            bool selected = selectedItemStorage == storage && selectedItemKey == itemKey;
            KImage background = row.AddComponent<KImage>();
            background.color = selected ? new Color(0.62f, 0.67f, 0.70f, 1f) : Color.clear;
            background.colorStyleSetting = CreateColorStyle(
                background.color,
                new Color(0.68f, 0.72f, 0.74f, 1f),
                new Color(0.54f, 0.59f, 0.62f, 1f));

            KButton rowButton = row.AddComponent<KButton>();
            rowButton.bgImage = background;
            rowButton.additionalKImages = new KImage[0];
            rowButton.soundPlayer = new ButtonSoundPlayer();
            rowButton.onClick += () =>
            {
                selectedItemStorage = storage;
                selectedItemKey = itemKey;
                LogDebug(string.Format(
                    "Select item row storage={0} itemKey={1} itemName={2} mass={3}",
                    storage != null ? storage.gameObject.GetProperName() : "null",
                    itemKey,
                    itemName,
                    formattedMass));
                Refresh(true);
            };

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

            if (selected)
            {
                GameObject transferButton = CreateGameButton("TransferButton", row.transform, "转移", () => ShowTransferDialog(storage, itemKey));
                LayoutElement transferLayout = transferButton.AddComponent<LayoutElement>();
                transferLayout.preferredWidth = 58f;
                transferLayout.preferredHeight = 20f;
                ToolTip transferTooltip = transferButton.AddComponent<ToolTip>();
                transferTooltip.toolTip = "把这个物品转移到同一网络中的目标储存建筑";

                GameObject dropButton = CreateGameButton("DropButton", row.transform, "丢弃", () => ShowDropDialog(storage, itemKey));
                LayoutElement dropLayout = dropButton.AddComponent<LayoutElement>();
                dropLayout.preferredWidth = 58f;
                dropLayout.preferredHeight = 20f;
                ToolTip tooltip = dropButton.AddComponent<ToolTip>();
                tooltip.toolTip = "丢弃这个储存建筑中的目标物品";
            }
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

        private void ShowDropDialog(Storage storage, string itemKey)
        {
            List<GameObject> items = FindStoredItems(storage, itemKey);
            if (storage == null || items.Count == 0)
            {
                Refresh(true);
                return;
            }

            string itemName = GetStoredItemName(items[0]);
            float availableMass = GetStoredItemsMass(items);
            ShowAmountDialog(
                "丢弃数量",
                itemName,
                string.Format("当前箱子可丢弃：{0}", GameUtil.GetFormattedMass(availableMass)),
                availableMass,
                amount => DropSelectedItem(storage, itemKey, amount));
        }

        private void ShowTransferDialog(Storage source, string itemKey)
        {
            List<GameObject> items = FindStoredItems(source, itemKey);
            if (targetHub == null || source == null || items.Count == 0)
            {
                Refresh(true);
                return;
            }

            targetHub.RefreshNetwork();
            List<Storage> targets = targetHub.ConnectedStorages
                .Select(info => info.Storage)
                .Where(storage => storage != null && storage != source && storage.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .OrderBy(storage => storage.GetProperName())
                .ToList();

            if (targets.Count == 0)
            {
                ShowMessageDialog("转移物品", "同一网络中没有可接收物品的目标箱子。");
                return;
            }

            string itemName = GetStoredItemName(items[0]);
            float sourceMass = GetStoredItemsMass(items);

            void ShowTransferAmountDialog(Storage destination)
            {
                float remainingCapacity = Mathf.Max(0f, destination.RemainingCapacity());
                float maxTransfer = Mathf.Min(sourceMass, remainingCapacity);
                string details = string.Format(
                    "目标容量：{0} / {1}\n最大可转移：{2}",
                    GameUtil.GetFormattedMass(destination.MassStored()),
                    GameUtil.GetFormattedMass(destination.Capacity()),
                    GameUtil.GetFormattedMass(maxTransfer));

                ShowAmountDialog(
                    "转移数量",
                    itemName,
                    details,
                    maxTransfer,
                    amount => TransferSelectedItem(source, itemKey, destination, amount),
                    "目标：" + destination.GetProperName(),
                    () => ShowTargetSelectionDialog(targets, destination, ShowTransferAmountDialog));
            }

            ShowTransferAmountDialog(targets[0]);
        }

        private void ShowTargetSelectionDialog(List<Storage> targets, Storage selectedTarget, System.Action<Storage> onSelected)
        {
            CloseModal();
            modalRoot = CreateModalFrame("选择目标箱子", 500f, 360f, out GameObject body);
            AddModalText(body.transform, "同一网络中的可接收目标", 14, FontStyles.Bold);

            RectTransform targetContent = CreateModalScrollList(body.transform, 230f);

            foreach (Storage target in targets)
            {
                CreateTargetSelectionRow(targetContent, target, target == selectedTarget, () => onSelected?.Invoke(target));
            }

            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, "取消", 90f, () => onSelected?.Invoke(selectedTarget));
        }

        private void ShowStorageSettingsDialog(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            CloseModal();
            modalRoot = CreateModalFrame("箱子设置", 420f, 335f, out GameObject body);
            AddModalText(body.transform, storage.GetProperName(), 15, FontStyles.Bold);
            AddModalText(
                body.transform,
                string.Format("储存：{0} / {1}\n剩余容量：{2}",
                    GameUtil.GetFormattedMass(storage.MassStored()),
                    GameUtil.GetFormattedMass(storage.Capacity()),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, storage.RemainingCapacity()))),
                12,
                FontStyles.Normal);

            AddSettingToggleRow(body.transform, "允许网络取出", storage.allowItemRemoval, value => storage.allowItemRemoval = value);
            AddSettingToggleRow(body.transform, "允许界面移除", storage.allowUIItemRemoval, value => storage.allowUIItemRemoval = value);
            AddSettingToggleRow(body.transform, "只收已标记清扫", storage.GetOnlyFetchMarkedItems(), value =>
            {
                if (storage.allowSettingOnlyFetchMarkedItems)
                {
                    storage.SetOnlyFetchMarkedItems(value);
                }
            });
            AddSettingToggleRow(body.transform, "忽略来源优先级", storage.ignoreSourcePriority, value => storage.ignoreSourcePriority = value);
            AddSettingToggleRow(body.transform, "只从低优先级转入", storage.onlyTransferFromLowerPriority, value => storage.onlyTransferFromLowerPriority = value);

            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, "关闭", 90f, CloseModal);
        }

        private void ShowAmountDialog(
            string title,
            string itemName,
            string details,
            float maxAmount,
            System.Action<float> onConfirm,
            string targetButtonText = null,
            System.Action targetButtonAction = null)
        {
            CloseModal();
            maxAmount = Mathf.Max(0f, maxAmount);
            modalRoot = CreateModalFrame(title, 430f, 300f, out GameObject body);

            AddModalText(body.transform, itemName, 15, FontStyles.Bold);
            TextMeshProUGUI detailsText = AddModalText(body.transform, details, 12, FontStyles.Normal);
            detailsText.color = new Color(0.82f, 0.85f, 0.88f, 1f);

            if (!string.IsNullOrEmpty(targetButtonText) && targetButtonAction != null)
            {
                GameObject targetRow = AddHorizontalRow(body.transform, 6f);
                AddModalButton(targetRow.transform, targetButtonText, 240f, targetButtonAction);
                AddFooterSpacer(targetRow.transform);
            }

            float currentAmount = maxAmount;
            bool updating = false;
            TextMeshProUGUI valueLabel = AddModalText(body.transform, string.Empty, 13, FontStyles.Bold);
            KSlider slider = CreateAmountSlider(body.transform, maxAmount);
            KInputTextField input = CreateAmountInputRow(body.transform);

            System.Action<float> setAmount = value =>
            {
                currentAmount = Mathf.Clamp(value, 0f, maxAmount);
                valueLabel.text = string.Format("数量：{0}", GameUtil.GetFormattedMass(currentAmount));
                if (!updating)
                {
                    updating = true;
                    slider.value = currentAmount;
                    input.text = FormatAmount(currentAmount);
                    updating = false;
                }
            };

            slider.onValueChanged.AddListener(value =>
            {
                if (!updating)
                {
                    setAmount(value);
                }
            });

            input.onEndEdit.AddListener(value =>
            {
                if (TryParseAmount(value, out float parsed))
                {
                    setAmount(parsed);
                }
                else
                {
                    setAmount(currentAmount);
                }
            });

            setAmount(currentAmount);

            GameObject shortcutRow = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(shortcutRow.transform);
            AddModalButton(shortcutRow.transform, "全部", 80f, () => setAmount(maxAmount));

            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, "取消", 80f, CloseModal);
            AddModalButton(footer.transform, "确定", 90f, () =>
            {
                float finalAmount = Mathf.Clamp(currentAmount, 0f, maxAmount);
                CloseModal();
                if (finalAmount > 0f)
                {
                    onConfirm?.Invoke(finalAmount);
                }
            });
        }

        private void ShowMessageDialog(string title, string message)
        {
            CloseModal();
            modalRoot = CreateModalFrame(title, 360f, 170f, out GameObject body);
            AddModalText(body.transform, message, 13, FontStyles.Normal);
            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, "确定", 90f, CloseModal);
        }

        private void DropSelectedItem(Storage storage, string itemKey, float requestedMass)
        {
            LogDebug(string.Format(
                "DropSelectedItem begin storage={0} storageGO={1} itemKey={2} requestedMass={3:0.###}",
                storage != null ? storage.GetInstanceID().ToString() : "null",
                storage != null ? storage.gameObject.GetProperName() : "null",
                itemKey,
                requestedMass));

            List<GameObject> items = FindStoredItems(storage, itemKey);
            LogDebug(string.Format("DropSelectedItem matched items={0}", items.Count));
            if (storage == null || items.Count == 0)
            {
                selectedItemStorage = null;
                selectedItemKey = null;
                LogDebug("DropSelectedItem abort: missing storage or items");
                Refresh(true);
                return;
            }

            Tag dropTag = GetStoredItemTag(items[0]);
            float mass = Mathf.Min(requestedMass, GetStoredItemsMass(items));
            LogDebug(string.Format(
                "DropSelectedItem resolved tag={0} tagValid={1} mass={2:0.###} firstItem={3}",
                dropTag,
                dropTag.IsValid,
                mass,
                items[0] != null ? items[0].GetProperName() : "null"));

            bool dropped = dropTag.IsValid && mass > 0f && storage.DropSome(dropTag, mass, false, false, default(Vector3), true, true);
            LogDebug(string.Format("DropSelectedItem DropSome result={0} remainingItems={1}", dropped, storage.items.Count));

            if (!dropped)
            {
                foreach (GameObject item in items.ToList())
                {
                    GameObject droppedItem = storage.Drop(item, true);
                    LogDebug(string.Format(
                        "DropSelectedItem fallback Drop item={0} result={1}",
                        item != null ? item.GetProperName() : "null",
                        droppedItem != null ? droppedItem.GetProperName() : "null"));
                    if (droppedItem != null)
                    {
                        droppedItem.transform.SetPosition(storage.transform.GetPosition());
                    }
                }
            }

            selectedItemStorage = null;
            selectedItemKey = null;
            targetHub?.RefreshNetwork();
            lastListSignature = null;
            Refresh(true);
            LogDebug("DropSelectedItem end");
        }

        private void TransferSelectedItem(Storage source, string itemKey, Storage destination, float requestedMass)
        {
            List<GameObject> items = FindStoredItems(source, itemKey);
            if (source == null || destination == null || items.Count == 0)
            {
                Refresh(true);
                return;
            }

            Tag tag = GetStoredItemTag(items[0]);
            float maxTransfer = Mathf.Min(GetStoredItemsMass(items), Mathf.Max(0f, destination.RemainingCapacity()));
            float remaining = Mathf.Clamp(requestedMass, 0f, maxTransfer);
            float transferred = 0f;

            while (tag.IsValid && remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                float moved = source.Transfer(destination, tag, remaining, block_events: false, hide_popups: true);
                if (moved <= 0f)
                {
                    break;
                }

                transferred += moved;
                remaining -= moved;
            }

            LogDebug(string.Format(
                "TransferSelectedItem source={0} destination={1} tag={2} requested={3:0.###} transferred={4:0.###}",
                source.gameObject.GetProperName(),
                destination.gameObject.GetProperName(),
                tag,
                requestedMass,
                transferred));

            selectedItemStorage = null;
            selectedItemKey = null;
            targetHub?.RefreshNetwork();
            lastListSignature = null;
            Refresh(true);
        }

        private static float GetStoredItemsMass(IEnumerable<GameObject> items)
        {
            return items.Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f);
        }

        private static List<GameObject> FindStoredItems(Storage storage, string itemKey)
        {
            return storage?.items
                .Where(item => item != null && GetStoredItemKey(item) == itemKey)
                .ToList() ?? new List<GameObject>();
        }

        private static Tag GetStoredItemTag(GameObject item)
        {
            if (item == null)
            {
                return Tag.Invalid;
            }

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                return prefabId.PrefabID();
            }

            PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
            return primaryElement != null ? primaryElement.ElementID.CreateTag() : Tag.Invalid;
        }

        private static string GetStoredItemKey(GameObject item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                return prefabId.PrefabID().ToString();
            }

            PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
            return primaryElement != null ? primaryElement.ElementID.ToString() : item.name;
        }

        private static string GetStorageTypeKey(StorageNetworkStorageInfo storageInfo)
        {
            KPrefabID prefabId = storageInfo.Storage?.GetComponent<KPrefabID>();
            return prefabId != null ? prefabId.PrefabID().ToString() : GetStorageTypeName(storageInfo);
        }

        private static string GetStorageTypeName(StorageNetworkStorageInfo storageInfo)
        {
            GameObject gameObject = storageInfo.Storage?.gameObject;
            return gameObject != null ? gameObject.GetProperName() : storageInfo.Name;
        }

        private GameObject CreateModalFrame(string title, float width, float height, out GameObject body)
        {
            GameObject overlay = new GameObject("ModalOverlay");
            overlay.transform.SetParent(transform, false);
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            Stretch(overlayRect, 0f, 0f);
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.34f);

            GameObject dialog = CreateBox("Dialog", overlay.transform, new Color(0.22f, 0.24f, 0.28f, 0.98f));
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(width, height);

            GameObject header = CreateBox("ModalHeader", dialog.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 0f, 0f, 0f, 30f);
            TextMeshProUGUI titleText = CreateText("Title", header.transform, title, 14, TextAlignmentOptions.MidlineLeft);
            titleText.fontStyle = FontStyles.Bold;
            Stretch(titleText.rectTransform(), 12f, 0f);

            GameObject closeButton = CreateGameButton("CloseButton", header.transform, "X", CloseModal);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-4f, 0f);
            closeRect.sizeDelta = new Vector2(24f, 22f);

            body = CreateBox("ModalBody", dialog.transform, new Color(0.34f, 0.38f, 0.40f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 38f);
            VerticalLayoutGroup layout = body.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return overlay;
        }

        private static TextMeshProUGUI AddModalText(Transform parent, string text, int size, FontStyles style)
        {
            TextMeshProUGUI label = CreateText("ModalText", parent, text, size, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = style;
            label.color = Color.white;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = text.Contains("\n") ? 48f : 24f;
            return label;
        }

        private static GameObject AddHorizontalRow(Transform parent, float spacing)
        {
            GameObject row = new GameObject("ModalRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 28f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private RectTransform CreateModalScrollList(Transform parent, float height)
        {
            GameObject list = CreatePlainImage("TargetList", parent, new Color(0.22f, 0.25f, 0.27f, 1f));
            list.AddComponent<LayoutElement>().preferredHeight = height;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(list.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            SetStretch(viewportRect, 6f, 20f, 6f, 6f);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(list.transform);

            ScrollRect scrollRect = list.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
            list.AddComponent<ScrollWheelBlocker>();
            return content;
        }

        private void CreateTargetSelectionRow(Transform parent, Storage target, bool selected, System.Action onClick)
        {
            Color baseColor = selected
                ? new Color(0.33f, 0.39f, 0.45f, 1f)
                : new Color(0.25f, 0.29f, 0.34f, 1f);
            GameObject row = CreateStyledButton("TargetButton", parent, string.Empty, onClick, baseColor);
            row.AddComponent<LayoutElement>().preferredHeight = 30f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI name = CreateText("Name", row.transform, target.GetProperName(), 12, TextAlignmentOptions.MidlineLeft);
            name.color = Color.white;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI remaining = CreateText(
                "Remaining",
                row.transform,
                "剩余 " + GameUtil.GetFormattedMass(Mathf.Max(0f, target.RemainingCapacity())),
                11,
                TextAlignmentOptions.MidlineRight);
            remaining.color = new Color(0.88f, 0.90f, 0.92f, 1f);
            remaining.textWrappingMode = TextWrappingModes.NoWrap;
            remaining.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

            TextMeshProUGUI capacity = CreateText(
                "Capacity",
                row.transform,
                string.Format("{0} / {1}", GameUtil.GetFormattedMass(target.MassStored()), GameUtil.GetFormattedMass(target.Capacity())),
                11,
                TextAlignmentOptions.MidlineRight);
            capacity.color = new Color(0.78f, 0.82f, 0.85f, 1f);
            capacity.textWrappingMode = TextWrappingModes.NoWrap;
            capacity.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;
        }

        private static void AddFooterSpacer(Transform parent)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static GameObject AddModalButton(Transform parent, string text, float width, System.Action onClick)
        {
            GameObject button = CreateGameButton("ModalButton", parent, text, onClick);
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 26f;
            return button;
        }

        private void AddSettingToggleRow(Transform parent, string label, bool initialValue, System.Action<bool> onChanged)
        {
            bool current = initialValue;
            GameObject stateButton = null;
            System.Action refreshLabel = () =>
            {
                TextMeshProUGUI text = stateButton?.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = current ? "开" : "关";
                }

                KImage image = stateButton?.GetComponent<KImage>();
                if (image != null)
                {
                    Color baseColor = current
                        ? new Color(0.42f, 0.55f, 0.48f, 1f)
                        : new Color(0.30f, 0.33f, 0.37f, 1f);
                    image.colorStyleSetting = CreateColorStyle(baseColor, Lighten(baseColor, 0.07f), Darken(baseColor, 0.08f));
                }
            };

            GameObject row = CreatePlainImage("SettingRow", parent, new Color(0.27f, 0.31f, 0.33f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 32f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 8, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 12, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.93f, 0.95f, 0.95f, 1f);
            labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            stateButton = AddModalButton(row.transform, string.Empty, 58f, () =>
            {
                current = !current;
                onChanged?.Invoke(current);
                refreshLabel();
                targetHub?.RefreshNetwork();
                lastListSignature = null;
            });
            refreshLabel();
        }

        private static KSlider CreateAmountSlider(Transform parent, float maxAmount)
        {
            GameObject sliderObject = new GameObject("AmountSlider");
            sliderObject.SetActive(false);
            sliderObject.transform.SetParent(parent, false);
            sliderObject.AddComponent<RectTransform>();
            sliderObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            GameObject background = CreatePlainImage("Background", sliderObject.transform, new Color(0.10f, 0.11f, 0.13f, 1f));
            Stretch(background.GetComponent<RectTransform>(), 0f, 9f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            Stretch(fillAreaRect, 3f, 8f);

            GameObject fill = CreatePlainImage("Fill", fillArea.transform, new Color(0.58f, 0.22f, 0.43f, 1f));
            Stretch(fill.GetComponent<RectTransform>(), 0f, 0f);

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            Stretch(handleAreaRect, 8f, 0f);

            GameObject handle = CreatePlainImage("Handle", handleArea.transform, new Color(0.66f, 0.37f, 0.55f, 1f));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16f, 18f);

            KSlider slider = sliderObject.AddComponent<KSlider>();
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(0.001f, maxAmount);
            slider.value = maxAmount;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            sliderObject.SetActive(true);
            return slider;
        }

        private static KInputTextField CreateAmountInputRow(Transform parent)
        {
            GameObject row = AddHorizontalRow(parent, 8f);
            TextMeshProUGUI label = CreateText("AmountInputLabel", row.transform, "输入数量", 12, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.92f, 0.94f, 0.95f, 1f);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return CreateAmountInput(row.transform);
        }

        private static KInputTextField CreateAmountInput(Transform parent)
        {
            GameObject inputObject = CreatePlainImage("AmountInput", parent, new Color(0.14f, 0.16f, 0.20f, 1f));
            ApplySprite(inputObject.GetComponent<Image>(), "InputFieldBackground");
            LayoutElement inputLayout = inputObject.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 150f;
            inputLayout.preferredHeight = 28f;

            TextMeshProUGUI text = CreateText("Text", inputObject.transform, string.Empty, 13, TextAlignmentOptions.MidlineLeft);
            text.color = Color.white;
            Stretch(text.rectTransform(), 8f, 3f);

            KInputTextField input = inputObject.AddComponent<KInputTextField>();
            input.textComponent = text;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.55f, 0.67f, 0.76f, 0.55f);
            return input;
        }

        private static GameObject CreatePlainImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<RectTransform>();
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Sliced;
            return gameObject;
        }

        private static string FormatAmount(float amount)
        {
            return amount.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryParseAmount(string value, out float amount)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out amount) ||
                   float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
        }

        private void CloseModal()
        {
            if (modalRoot != null)
            {
                Destroy(modalRoot);
                modalRoot = null;
            }
        }

        private void Close()
        {
            CloseModal();
            gameObject.SetActive(false);
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.Consumed)
            {
                return;
            }

            if (modalRoot != null)
            {
                if (e.TryConsume(global::Action.Escape))
                {
                    CloseModal();
                    return;
                }

                e.Consumed = true;
                return;
            }

            if (!IsMouseOverWindow())
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

        private static GameObject CreateGameButton(string name, Transform parent, string text, System.Action onClick)
        {
            return CreateStyledButton(name, parent, text, onClick, new Color(0.20f, 0.23f, 0.33f, 1f));
        }

        private static GameObject CreateStyledButton(string name, Transform parent, string text, System.Action onClick, Color baseColor)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage image = buttonObject.AddComponent<KImage>();
            image.type = Image.Type.Sliced;
            ApplySprite(image, "skin_button_action");
            image.colorStyleSetting = CreateColorStyle(baseColor, Lighten(baseColor, 0.07f), Darken(baseColor, 0.08f));
            image.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = image;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.58f, 0.62f, 0.70f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);

            if (!string.IsNullOrEmpty(text))
            {
                TextMeshProUGUI label = CreateText("Label", buttonObject.transform, text, 12, TextAlignmentOptions.Center);
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(0.94f, 0.96f, 0.98f, 1f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                Stretch(label.rectTransform(), 4f, 0f);
            }

            return buttonObject;
        }

        private static void LogDebug(string message)
        {
            if (DebugLogging)
            {
                Debug.Log("[StorageNetworkPanel] " + message);
            }
        }

        private static bool ApplySprite(Image image, string spriteName)
        {
            if (image == null || string.IsNullOrEmpty(spriteName))
            {
                return false;
            }

            Sprite sprite = Assets.GetSprite(spriteName);
            if (sprite == null)
            {
                return false;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            return true;
        }

        private static ColorStyleSetting CreateColorStyle(Color normal, Color hover, Color pressed)
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = normal;
            style.hoverColor = hover;
            style.activeColor = pressed;
            style.disabledColor = Darken(normal, 0.08f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
        }

        private static Color Lighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a);
        }

        private static Color Darken(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r - amount),
                Mathf.Clamp01(color.g - amount),
                Mathf.Clamp01(color.b - amount),
                color.a);
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
            ApplySprite(background, "build_menu_scrollbar_frame");

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(0.22f, 0.25f, 0.34f, 1f);
            ApplySprite(handleImage, "build_menu_scrollbar_inner");

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
