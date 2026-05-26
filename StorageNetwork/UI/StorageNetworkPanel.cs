using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {

        private static StorageNetworkPanel instance;
        private static Dictionary<string, Sprite> spriteCache;
        private StorageSceneSnapshot currentSnapshot;
        private TextMeshProUGUI summaryText;
        private RectTransform categoryContent;
        private RectTransform listContent;
        private ScrollRect listScrollRect;
        private RectTransform windowRect;
        private GameObject modalRoot;
        private GameObject categorySummaryRoot;
        private RectTransform categorySummaryContent;
        private GameObject enrollableWindowRoot;
        private GameObject headerWindowRoot;
        private GameObject productionSettingsRoot;
        private RectTransform productionSettingsContent;
        private Storage productionSettingsStorage;
        private GameObject productionPickerRoot;
        private string productionSettingsSignature;
        private KInputController registeredController;
        private const int InputPriority = int.MaxValue - 100;
        private bool rightClickCloseCandidate;
        private Vector3 rightClickStartPosition;
        private const float RightClickDragThresholdPixels = 8f;
        private string selectedCategoryKey;
        private string selectedItemKey;
        private Storage selectedItemStorage;
        private readonly Dictionary<string, bool> expandedStorageTypes = new Dictionary<string, bool>();
        private readonly Dictionary<Storage, bool> expandedStorages = new Dictionary<Storage, bool>();
        private float refreshElapsed;
        private string lastListSignature;
        private const bool DebugLogging = true;
        public string handlerName => gameObject.name;

        public KInputHandler inputHandler { get; set; }

        public static void Show(Storage focusStorage = null)
        {
            if (instance == null)
            {
                instance = Create();
            }

            instance.SetSnapshot(focusStorage);
            instance.gameObject.SetActive(true);
        }

        public static bool IsOpen()
        {
            return instance != null && instance.gameObject != null && instance.gameObject.activeInHierarchy;
        }

        public static bool CloseFromRightClick()
        {
            if (!IsOpen())
            {
                return false;
            }

            if (instance.modalRoot != null)
            {
                instance.CloseModal();
            }
            else if (instance.productionPickerRoot != null)
            {
                instance.CloseProductionPicker();
            }
            else if (instance.productionSettingsRoot != null && instance.productionSettingsRoot.activeSelf)
            {
                instance.CloseProductionSettingsPanel();
            }
            else if (instance.enrollableWindowRoot != null && instance.enrollableWindowRoot.activeSelf)
            {
                instance.CloseEnrollableWindow();
            }
            else if (instance.headerWindowRoot != null && instance.headerWindowRoot.activeSelf)
            {
                instance.CloseHeaderWindow();
            }
            else
            {
                instance.Close();
            }

            return true;
        }

        public static bool BeginRightClickCloseCandidate()
        {
            if (!IsOpen())
            {
                return false;
            }

            instance.rightClickCloseCandidate = true;
            instance.rightClickStartPosition = KInputManager.GetMousePos();
            return true;
        }

        public static bool FinishRightClickCloseCandidate(out bool closed)
        {
            closed = false;
            if (!IsOpen() || !instance.rightClickCloseCandidate)
            {
                return false;
            }

            instance.rightClickCloseCandidate = false;
            Vector3 delta = KInputManager.GetMousePos() - instance.rightClickStartPosition;
            if (delta.sqrMagnitude > RightClickDragThresholdPixels * RightClickDragThresholdPixels)
            {
                return true;
            }

            closed = CloseFromRightClick();
            return true;
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

        private void SetSnapshot(Storage focusStorage = null)
        {
            currentSnapshot = null;
            lastListSignature = null;
            FocusStorageRow(focusStorage);
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
            if (summaryText == null || listContent == null)
            {
                return;
            }

            TrackRightClickCloseGesture();
            UpdatePanelDrag();

            refreshElapsed += Time.unscaledDeltaTime;
            if (refreshElapsed >= 1f)
            {
                refreshElapsed = 0f;
                Refresh();
                UpdateProductionSettingsPanel();
                UpdateOrderPanelAutoRefresh(1f);
            }
        }

        private void TrackRightClickCloseGesture()
        {
            if (Input.GetMouseButtonDown(1))
            {
                BeginRightClickCloseCandidate();
                return;
            }

            if (Input.GetMouseButtonUp(1))
            {
                FinishRightClickCloseCandidate(out _);
            }
        }

        private bool IsMouseOverAnyPanel()
        {
            Vector2 mousePosition = KInputManager.GetMousePos();
            return ContainsScreenPoint(windowRect, mousePosition) ||
                ContainsScreenPoint(productionSettingsRoot, mousePosition) ||
                ContainsScreenPoint(categorySummaryRoot, mousePosition) ||
                ContainsScreenPoint(enrollableWindowRoot, mousePosition) ||
                ContainsScreenPoint(headerWindowRoot, mousePosition) ||
                ContainsScreenPoint(modalRoot, mousePosition);
        }

        private static bool ContainsScreenPoint(GameObject gameObject, Vector2 screenPoint)
        {
            return gameObject != null &&
                gameObject.activeInHierarchy &&
                ContainsScreenPoint(gameObject.GetComponent<RectTransform>(), screenPoint);
        }

        private static bool ContainsScreenPoint(RectTransform rectTransform, Vector2 screenPoint)
        {
            return rectTransform != null &&
                rectTransform.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, null);
        }

        private void BuildWindow(Transform parent)
        {
            GameObject window = CreateBox("Window", parent, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(window.GetComponent<Image>());
            windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(960f, 850f); 

            GameObject header = CreateBox("Header", window.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 6f, 6f, 6f, 28f);

            TextMeshProUGUI title = CreateText("Title", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TITLE), 14, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);
            title.rectTransform().offsetMax = new Vector2(-92f, 0f);

            GameObject enrollableButton = CreateGameButton("EnrollableButton", header.transform, string.Empty, ShowEnrollableBuildingsDialog);
            RectTransform enrollableRect = enrollableButton.GetComponent<RectTransform>();
            enrollableRect.anchorMin = new Vector2(0f, 0.5f);
            enrollableRect.anchorMax = new Vector2(0f, 0.5f);
            enrollableRect.pivot = new Vector2(0f, 0.5f);
            enrollableRect.anchoredPosition = new Vector2(92f, 0f);
            enrollableRect.sizeDelta = new Vector2(26f, 22f);
            AddButtonIcon(enrollableButton.transform, "storage_network_overlay", "+");
            ToolTip enrollableTooltip = enrollableButton.AddComponent<ToolTip>();
            enrollableTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_BUTTON_TOOLTIP);

            GameObject headerWindowButton = CreateGameButton("HeaderWindowButton", header.transform, string.Empty, ToggleHeaderWindow);
            RectTransform headerWindowRect = headerWindowButton.GetComponent<RectTransform>();
            headerWindowRect.anchorMin = new Vector2(0f, 0.5f);
            headerWindowRect.anchorMax = new Vector2(0f, 0.5f);
            headerWindowRect.pivot = new Vector2(0f, 0.5f);
            headerWindowRect.anchoredPosition = new Vector2(124f, 0f);
            headerWindowRect.sizeDelta = new Vector2(26f, 22f);
            AddButtonIcon(headerWindowButton.transform, "icon_action_building_disabled", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEADER_WINDOW_BUTTON));
            ToolTip headerWindowTooltip = headerWindowButton.AddComponent<ToolTip>();
            headerWindowTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEADER_WINDOW_TOOLTIP);

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
            summaryText.rectTransform().anchorMin = Vector2.zero;
            summaryText.rectTransform().anchorMax = Vector2.one;
            summaryText.rectTransform().offsetMin = new Vector2(12f, 7f);
            summaryText.rectTransform().offsetMax = new Vector2(-76f, -7f);
            CreateCategorySummaryButton(summary.transform);

            GameObject list = CreateBox("List", content.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(list.GetComponent<RectTransform>(), 8f, 8f, 8f, 70f);

            HorizontalLayoutGroup listColumns = list.AddComponent<HorizontalLayoutGroup>();
            listColumns.padding = new RectOffset(8, 8, 8, 8);
            listColumns.spacing = 8f;
            listColumns.childAlignment = TextAnchor.UpperLeft;
            listColumns.childControlWidth = true;
            listColumns.childControlHeight = true;
            listColumns.childForceExpandWidth = false;
            listColumns.childForceExpandHeight = true;

            GameObject categories = CreateBox("Categories", list.transform, new Color(0.74f, 0.73f, 0.68f, 1f));
            LayoutElement categoryLayout = categories.AddComponent<LayoutElement>();
            categoryLayout.minWidth = 130f;
            categoryLayout.preferredWidth = 130f;
            categoryLayout.flexibleWidth = 0f;
            AddVerticalContainer(categories, 4f, 4, 4, 4, 4);

            GameObject categoryContentObject = new GameObject("CategoryContent");
            categoryContentObject.transform.SetParent(categories.transform, false);
            categoryContent = categoryContentObject.AddComponent<RectTransform>();
            categoryContent.anchorMin = new Vector2(0f, 1f);
            categoryContent.anchorMax = new Vector2(1f, 1f);
            categoryContent.pivot = new Vector2(0.5f, 1f);
            categoryContent.offsetMin = Vector2.zero;
            categoryContent.offsetMax = Vector2.zero;
            AddVerticalContainer(categoryContentObject, 5f, 0, 0, 0, 0);
            categoryContentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject rightList = CreateBox("RightList", list.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            rightList.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(rightList.transform, false);
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

            Scrollbar scrollbar = CreateScrollbar(rightList.transform);

            ScrollRect scrollRect = rightList.AddComponent<ScrollRect>();
            listScrollRect = scrollRect;
            scrollRect.viewport = viewportRect;
            scrollRect.content = listContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 4f;

            rightList.AddComponent<ScrollWheelBlocker>();
        }

        private void FocusStorageRow(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            selectedCategoryKey = StorageCategories.GetKey(storage);
            selectedItemStorage = storage;
            selectedItemKey = null;
            expandedStorageTypes[GetStoragePrefabKey(storage)] = true;
            expandedStorages[storage] = true;
        }

        private void Refresh(bool forceRebuild = false)
        {
            if (summaryText == null || listContent == null)
            {
                return;
            }

            currentSnapshot = StorageSceneCollector.Collect(forceRebuild);
            summaryText.text =
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_TITLE) + "\n" +
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_LINE),
                    currentSnapshot.Storages.Count,
                    GameUtil.GetFormattedMass(currentSnapshot.TotalStoredKg),
                    GameUtil.GetFormattedMass(currentSnapshot.TotalCapacityKg));

            if (currentSnapshot.Storages.Count == 0)
            {
                if (forceRebuild || lastListSignature != "empty")
                {
                    lastListSignature = "empty";
                    ClearCategories();
                    ClearList();
                    CreateInfoRow(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.EMPTY_TITLE),
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.EMPTY_DETAILS));
                    UpdateCategorySummaryPanel();
                }

                return;
            }

            string listSignature = BuildListSignature(currentSnapshot.Storages);
            if (forceRebuild || listSignature != lastListSignature)
            {
                lastListSignature = listSignature;
                RebuildStorageRows(currentSnapshot.Storages);
            }

            UpdateCategorySummaryPanel();
        }

        private void RebuildStorageRows(IEnumerable<StorageInfo> storages)
        {
            ClearStorageDropAreas();
            ClearList();
            ClearCategories();

            List<StorageNetworkCategoryGroup> groups = BuildCategoryGroups(storages).ToList();
            EnsureSelectedCategory(groups);
            foreach (StorageNetworkCategoryGroup group in groups)
            {
                CreateCategoryButton(group);
            }

            StorageNetworkCategoryGroup selectedGroup = groups.FirstOrDefault(group => group.Key == selectedCategoryKey);
            if (selectedGroup == null)
            {
                CreateInfoRow(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT), string.Empty);
                RebuildLayout();
                return;
            }

            foreach (IGrouping<string, StorageInfo> group in selectedGroup.Storages.GroupBy(GetStorageTypeKey).OrderBy(group => GetStorageTypeName(group.First())))
            {
                List<StorageInfo> typeStorages = group.ToList();
                if (typeStorages.Count == 1)
                {
                    CreateStorageRow(typeStorages[0], listContent);
                }
                else
                {
                    CreateStorageTypeRow(typeStorages);
                }
            }

            RebuildLayout();
        }

        private static string BuildListSignature(IEnumerable<StorageInfo> storages)
        {
            return string.Join("|", storages
                .OrderBy(GetStorageTypeKey)
                .ThenBy(storage => storage.Storage != null ? storage.Storage.GetInstanceID() : 0)
                .Select(storage =>
                {
                    IEnumerable<GameObject> storedItems = storage.StoredItems ?? Enumerable.Empty<GameObject>();
                    string items = string.Join(",", storedItems
                        .GroupBy(GetStoredItemKey)
                        .OrderBy(group => group.Key)
                        .Select(group => string.Format("{0}:{1}:{2:0.###}",
                            group.Key,
                            group.Count(),
                            group.Sum(GetStoredItemMass))));

                    return string.Format("{0}:{1}:{2:0.###}:{3:0.###}:{4}",
                        GetStorageTypeKey(storage),
                        storage.Storage != null ? storage.Storage.GetInstanceID() : 0,
                        storage.StoredKg,
                        storage.CapacityKg,
                        items);
                }));
        }

        private void RebuildLayout()
        {
            if (listContent == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
            if (listScrollRect != null && selectedItemStorage != null)
            {
                listScrollRect.verticalNormalizedPosition = 1f;
            }
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

        private void ClearCategories()
        {
            if (categoryContent == null)
            {
                return;
            }

            for (int i = categoryContent.childCount - 1; i >= 0; i--)
            {
                Destroy(categoryContent.GetChild(i).gameObject);
            }
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
            CloseCategorySummaryPanel();
            CloseProductionSettingsPanel();
            CloseEnrollableWindow();
            CloseHeaderWindow();
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

            if (e.TryConsume(global::Action.Escape))
            {
                if (enrollableWindowRoot != null && enrollableWindowRoot.activeSelf)
                {
                    CloseEnrollableWindow();
                    return;
                }

                if (headerWindowRoot != null && headerWindowRoot.activeSelf)
                {
                    CloseHeaderWindow();
                    return;
                }

                Close();
                return;
            }

            if (!IsMouseOverAnyPanel())
            {
                return;
            }

            if (!e.TryConsume(global::Action.ZoomIn))
            {
                e.TryConsume(global::Action.ZoomOut);
            }
        }

        private void RegisterInputHandler()
        {
            if (registeredController != null || KInputManager.currentController == null)
            {
                return;
            }

            registeredController = KInputManager.currentController;
            KInputHandler.Add(registeredController, this, InputPriority);
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

        private static void LogDebug(string message)
        {
            if (DebugLogging)
            {
                Debug.Log("[StorageNetworkPanel] " + message);
            }
        }
    }
}
