using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {

        private static StorageNetworkPanel instance;
        private static Dictionary<string, Sprite> spriteCache;
        private StorageSceneSnapshot currentSnapshot;
        private TextMeshProUGUI summaryText;
        private RectTransform healthContent;
        private KInputTextField mainSearchInput;
        private string mainSearchText = string.Empty;
        private int mainWorldFilterId = UnsetEnrollableWorldFilterId;
        private RectTransform mainWorldFilterContent;
        private GameObject mainWorldDropdownRoot;
        private RectTransform categoryContent;
        private RectTransform listContent;
        private ScrollRect listScrollRect;
        private RectTransform windowRect;
        private GameObject modalRoot;
        private GameObject categorySummaryRoot;
        private RectTransform categorySummaryContent;
        private StorageNetworkKeyedRowCache categorySummaryRows;
        private TextMeshProUGUI categorySummaryTitle;
        private GameObject enrollableWindowRoot;
        private string enrollableWindowSignature;
        private int enrollableWorldFilterId = UnsetEnrollableWorldFilterId;
        private RectTransform enrollableWorldFilterContent;
        private GameObject enrollableWorldDropdownRoot;
        private KInputTextField enrollableSearchInput;
        private string enrollableSearchText = string.Empty;
        private GameObject headerWindowRoot;
        private int orderWorldFilterId = UnsetEnrollableWorldFilterId;
        private RectTransform orderWorldFilterContent;
        private GameObject orderWorldDropdownRoot;
        private GameObject productionSettingsRoot;
        private RectTransform productionSettingsContent;
        private Storage productionSettingsStorage;
        private bool productionSettingsPositionInitialized;
        private GameObject geyserSettingsRoot;
        private RectTransform geyserSettingsContent;
        private Geyser geyserSettingsGeyser;
        private string geyserSettingsSignature;
        private GameObject productionPickerRoot;
        private string productionSettingsSignature;
        private ProductionOverviewCardView productionOverviewView;
        private ProductionInventoryCardView productionInventoryView;
        private ProductionAutomationCardsView productionAutomationView;
        private string categorySummarySignature;
        private bool rightClickCloseCandidate;
        private Vector3 rightClickStartPosition;
        private const float RightClickDragThresholdPixels = 8f;
        private string selectedCategoryKey;
        private string selectedItemKey;
        private Storage selectedItemStorage;
        private readonly Dictionary<string, bool> expandedStorageTypes = new Dictionary<string, bool>();
        private readonly Dictionary<Storage, bool> expandedStorages = new Dictionary<Storage, bool>();
        private readonly Dictionary<Geyser, bool> expandedGeysers = new Dictionary<Geyser, bool>();
        private float refreshElapsed;
        private float structureRefreshElapsed;
        private string lastListSignature;
        private const float LiveRefreshSeconds = 1f;
        private const float StructureRefreshSeconds = 5f;
        private const string EmptyListSignature = "empty";
        private const string CoreOfflineListSignature = "core_offline";
        private const string CrossWorldRelayOfflineListSignature = "cross_world_relay_offline";
        private static readonly bool DebugLogging = false;

        private enum StoragePanelRefreshMode
        {
            Live,
            StructureCheck,
            Structure
        }

        public static void Show(Storage focusStorage = null)
        {
            if (instance == null)
            {
                instance = Create();
            }

            instance.SetSnapshot(focusStorage);
            if (!instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }

            if (!instance.IsActive())
            {
                instance.Activate();
            }
        }

        public static bool IsOpen()
        {
            return instance != null && instance.gameObject != null && instance.gameObject.activeInHierarchy;
        }

        public static void ResetRuntimeState()
        {
            if (instance != null && instance.gameObject != null)
            {
                Destroy(instance.gameObject);
            }

            instance = null;
            spriteCache?.Clear();
        }

        public static bool IsTextInputFocused()
        {
            return IsOpen() && (StorageNetworkNumberInputField.IsAnyEditing || StorageNetworkTextInputGuard.IsAnyFocused);
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
            panel.activateOnSpawn = false;
            panel.ConsumeMouseScroll = true;
            panel.BuildWindow(root.transform);
            root.SetActive(false);
            return panel;
        }

        private void SetSnapshot(Storage focusStorage = null)
        {
            if (mainWorldFilterId == UnsetEnrollableWorldFilterId)
            {
                int activeWorldId = GetActiveWorldFilterId();
                int savedWorldId = Config.Instance.MainWorldFilterId;
                if (savedWorldId != UnsetEnrollableWorldFilterId &&
                    Config.Instance.MainWorldFilterContextWorldId == activeWorldId)
                {
                    mainWorldFilterId = savedWorldId;
                }
                else
                {
                    mainWorldFilterId = activeWorldId != UnsetEnrollableWorldFilterId ? activeWorldId : AllEnrollableWorldsFilterId;
                    SaveMainWorldFilter();
                }
            }

            currentSnapshot = null;
            lastListSignature = null;
            refreshElapsed = 0f;
            structureRefreshElapsed = 0f;
            FocusStorageRow(focusStorage);
            RefreshStoragePanel(StoragePanelRefreshMode.Structure);
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
            structureRefreshElapsed += Time.unscaledDeltaTime;
            if (refreshElapsed >= LiveRefreshSeconds)
            {
                refreshElapsed = 0f;
                bool refreshStructure = structureRefreshElapsed >= StructureRefreshSeconds;
                if (refreshStructure)
                {
                    structureRefreshElapsed = 0f;
                }

                RefreshStoragePanel(refreshStructure ? StoragePanelRefreshMode.StructureCheck : StoragePanelRefreshMode.Live);
                UpdateProductionSettingsPanel();
                UpdateGeyserSettingsPanel();
                UpdateOrderPanelAutoRefresh(LiveRefreshSeconds);
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
            StorageNetworkWindowDrag.TryApplyLayout("mainWindow", windowRect, new Vector2(760f, 520f), new Vector2(1400f, 1100f));

            GameObject header = CreateBox("Header", window.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 6f, 6f, 6f, 28f);
            header.AddComponent<StorageNetworkWindowDrag>().Configure(windowRect, "mainWindow");

            TextMeshProUGUI title = CreateText("Title", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TITLE), 14, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.raycastTarget = false;
            Stretch(title.rectTransform(), 12f, 0f);
            title.rectTransform().offsetMax = new Vector2(-210f, 0f);

            GameObject enrollableButton = CreateGameButton("EnrollableButton", header.transform, string.Empty, ShowEnrollableBuildingsDialog);
            RectTransform enrollableRect = enrollableButton.GetComponent<RectTransform>();
            enrollableRect.anchorMin = new Vector2(0f, 0.5f);
            enrollableRect.anchorMax = new Vector2(0f, 0.5f);
            enrollableRect.pivot = new Vector2(0f, 0.5f);
            enrollableRect.anchoredPosition = new Vector2(92f, 0f);
            enrollableRect.sizeDelta = new Vector2(72f, 22f);
            AddButtonIconLabel(enrollableButton.transform, "storage_network_overlay", "+", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_SHORT));
            ToolTip enrollableTooltip = enrollableButton.AddComponent<ToolTip>();
            enrollableTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_BUTTON_TOOLTIP);

            GameObject headerWindowButton = CreateGameButton("HeaderWindowButton", header.transform, string.Empty, ToggleHeaderWindow);
            RectTransform headerWindowRect = headerWindowButton.GetComponent<RectTransform>();
            headerWindowRect.anchorMin = new Vector2(0f, 0.5f);
            headerWindowRect.anchorMax = new Vector2(0f, 0.5f);
            headerWindowRect.pivot = new Vector2(0f, 0.5f);
            headerWindowRect.anchoredPosition = new Vector2(170f, 0f);
            headerWindowRect.sizeDelta = new Vector2(58f, 22f);
            AddButtonIconLabel(headerWindowButton.transform, "action_select_research", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEADER_WINDOW_BUTTON), Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_SHORT));
            ToolTip headerWindowTooltip = headerWindowButton.AddComponent<ToolTip>();
            headerWindowTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEADER_WINDOW_TOOLTIP);

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, Close);
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
            summaryText.rectTransform().offsetMax = new Vector2(-256f, -7f);
            CreateMainWorldFilter(summary.transform);
            CreateCategorySummaryButton(summary.transform);

            GameObject healthBar = CreateBox("HealthBar", content.transform, new Color(0.74f, 0.75f, 0.69f, 1f));
            SetTopStretch(healthBar.GetComponent<RectTransform>(), 8f, 8f, 64f, 36f);
            HorizontalLayoutGroup healthLayout = healthBar.AddComponent<HorizontalLayoutGroup>();
            healthLayout.padding = new RectOffset(8, 8, 4, 4);
            healthLayout.spacing = 6f;
            healthLayout.childAlignment = TextAnchor.MiddleLeft;
            healthLayout.childControlWidth = true;
            healthLayout.childControlHeight = true;
            healthLayout.childForceExpandWidth = true;
            healthLayout.childForceExpandHeight = true;
            healthContent = healthBar.GetComponent<RectTransform>();

            GameObject list = CreateBox("List", content.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(list.GetComponent<RectTransform>(), 8f, 8f, 8f, 106f);

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
            ConfigureSmoothVerticalScroll(scrollRect, 30f);
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
            expandedStorageTypes[StorageNetworkStorageDisplay.GetPrefabKey(storage)] = true;
            expandedStorages[storage] = true;
        }

        private void RefreshStoragePanel(StoragePanelRefreshMode mode = StoragePanelRefreshMode.Live)
        {
            if (summaryText == null || listContent == null)
            {
                return;
            }

            bool forceRebuild = mode == StoragePanelRefreshMode.Structure;
            bool checkStructure = forceRebuild || mode == StoragePanelRefreshMode.StructureCheck;
            EnsureValidMainWorldFilter();
            currentSnapshot = CollectMainSnapshot(checkStructure);
            UpdateStorageSummaryText();

            if (IsMainWorldFilterBlockedByRelay())
            {
                RefreshCrossWorldRelayOfflineStorageList(forceRebuild);
                return;
            }

            if (!currentSnapshot.NetworkOnline)
            {
                RefreshCoreOfflineStorageList(forceRebuild);
                return;
            }

            if (currentSnapshot.Storages.Count == 0)
            {
                RefreshEmptyStorageList(forceRebuild);
                return;
            }

            if (ShouldRebuildStorageList(forceRebuild, checkStructure))
            {
                RebuildStorageListPreservingScroll();
            }

            LiveUpdateStoragePanels();
        }

        private void UpdateStorageSummaryText()
        {
            RebuildMainWorldFilter();
            summaryText.text =
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_TITLE) + "\n" +
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_LINE),
                    currentSnapshot.Storages.Count,
                    GameUtil.GetFormattedMass(currentSnapshot.TotalStoredKg),
                    GameUtil.GetFormattedMass(currentSnapshot.TotalCapacityKg));
            UpdateNetworkHealthBar();
        }

        private void UpdateNetworkHealthBar()
        {
            if (healthContent == null || currentSnapshot == null)
            {
                return;
            }

            ClearHealthBar();
            StorageNetworkPanelHealthMetrics metrics = StorageNetworkPanelHealthMetrics.Create(
                currentSnapshot,
                productionOrderService.Orders,
                StorageNetworkStorageRules.IsOfflineNetworkServer);

            AddHealthTile(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_CAPACITY), string.Format("{0:P0}", Mathf.Clamp01(metrics.FillRatio)), metrics.FillRatio >= 0.92f ? DangerColor() : metrics.FillRatio >= 0.80f ? WarningColor() : PositiveColor());
            AddHealthTile(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_REMAINING), GameUtil.GetFormattedMass(metrics.RemainingCapacityKg), metrics.RemainingCapacityKg <= 1000f ? WarningColor() : NeutralBlue());
            AddHealthTile(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_ORDERS), metrics.ActiveOrders.ToString(), metrics.ActiveOrders > 0 ? NeutralBlue() : MutedTextColor());
            AddHealthTile(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_WAITING), metrics.WaitingOrders.ToString(), metrics.WaitingOrders > 0 ? WarningColor() : PositiveColor());
            AddHealthTile(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_ABNORMAL), metrics.AbnormalOrders.ToString(), metrics.AbnormalOrders > 0 ? DangerColor() : PositiveColor());
            AddHealthTile(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_OFFLINE), metrics.OfflineServers.ToString(), metrics.OfflineServers > 0 ? DangerColor() : PositiveColor());
            EnsureMainSearchTile();
        }

        private void ClearHealthBar()
        {
            for (int i = healthContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = healthContent.GetChild(i).gameObject;
                if (child.name == "MainSearchTile")
                {
                    continue;
                }

                Destroy(child);
            }
        }

        private void AddHealthTile(string label, string value, Color valueColor)
        {
            GameObject tile = CreatePlainImage("HealthTile", healthContent, new Color(0.82f, 0.82f, 0.76f, 1f));
            tile.AddComponent<LayoutElement>().flexibleWidth = 1f;
            HorizontalLayoutGroup layout = tile.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 2, 2);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI name = CreateText("Label", tile.transform, label, 9, TextAlignmentOptions.MidlineLeft);
            name.color = MutedTextColor();
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI amount = CreateText("Value", tile.transform, value, 10, TextAlignmentOptions.MidlineRight);
            amount.color = valueColor;
            amount.fontStyle = FontStyles.Bold;
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = 58f;
        }

        private void EnsureMainSearchTile()
        {
            if (healthContent == null)
            {
                return;
            }

            Transform existing = healthContent.Find("MainSearchTile");
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return;
            }

            GameObject tile = CreatePlainImage("MainSearchTile", healthContent, new Color(0.82f, 0.82f, 0.76f, 1f));
            LayoutElement tileLayout = tile.AddComponent<LayoutElement>();
            tileLayout.minWidth = 150f;
            tileLayout.preferredWidth = 170f;
            tileLayout.flexibleWidth = 1f;

            HorizontalLayoutGroup layout = tile.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 3, 3);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            mainSearchInput = CreateFixedTextInput(
                tile.transform,
                "MainSearchInput",
                mainSearchText,
                154f,
                22f,
                10);
            mainSearchInput.onValueChanged.AddListener(value =>
            {
                mainSearchText = value ?? string.Empty;
                selectedItemStorage = null;
                selectedItemKey = null;
                lastListSignature = null;
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
            });

            ToolTip tooltip = tile.AddComponent<ToolTip>();
            tooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MAIN_SEARCH_TOOLTIP);
            tile.transform.SetAsLastSibling();
        }

        private void RefreshEmptyStorageList(bool forceRebuild)
        {
            if (forceRebuild || string.IsNullOrEmpty(lastListSignature))
            {
                lastListSignature = EmptyListSignature;
                ClearCategories();
                ClearList();
                CreateInfoRow(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.EMPTY_TITLE),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.EMPTY_DETAILS));
                LiveUpdateStoragePanels();
            }
        }

        private void RefreshCoreOfflineStorageList(bool forceRebuild)
        {
            if (forceRebuild || lastListSignature != CoreOfflineListSignature)
            {
                lastListSignature = CoreOfflineListSignature;
                ClearCategories();
                ClearList();
                CreateInfoRow(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_OFFLINE_TITLE),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_OFFLINE_DETAILS));
                LiveUpdateStoragePanels();
            }
        }

        private void RefreshCrossWorldRelayOfflineStorageList(bool forceRebuild)
        {
            if (forceRebuild || lastListSignature != CrossWorldRelayOfflineListSignature)
            {
                lastListSignature = CrossWorldRelayOfflineListSignature;
                ClearCategories();
                ClearList();
                CreateInfoRow(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CROSS_WORLD_RELAY_OFFLINE),
                    string.Empty);
                LiveUpdateStoragePanels();
            }
        }

        private bool IsMainWorldFilterBlockedByRelay()
        {
            int activeWorldId = GetActiveWorldFilterId();
            return mainWorldFilterId != AllEnrollableWorldsFilterId &&
                   mainWorldFilterId != activeWorldId &&
                   !StorageSceneRegistry.IsCrossPlanetRelayOnline();
        }

        private bool ShouldRebuildStorageList(bool forceRebuild, bool checkStructure)
        {
            if (forceRebuild ||
                string.IsNullOrEmpty(lastListSignature) ||
                lastListSignature == EmptyListSignature ||
                lastListSignature == CrossWorldRelayOfflineListSignature)
            {
                return true;
            }

            if (!checkStructure)
            {
                return false;
            }

            return BuildListSignature(currentSnapshot.Storages) != lastListSignature;
        }

        private void RebuildStorageListPreservingScroll()
        {
            float scrollOffset = GetListScrollOffset();
            lastListSignature = BuildListSignature(currentSnapshot.Storages);
            RebuildStorageRows(currentSnapshot.Storages);
            RestoreListScrollOffset(scrollOffset);
        }

        private void LiveUpdateStoragePanels()
        {
            UpdateCategorySummaryPanel();
        }

        private void RebuildStorageRows(IEnumerable<StorageInfo> storages)
        {
            ClearStorageDropAreas();
            ClearCategories();
            ClearList();

            List<StorageInfo> filteredStorages = FilterStorageInfosBySearch(storages).ToList();
            List<StorageNetworkCategoryGroup> groups = BuildCategoryGroups(filteredStorages).ToList();
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

            foreach (IGrouping<string, StorageInfo> group in selectedGroup.Storages.GroupBy(StorageNetworkStorageDisplay.GetTypeKey).OrderBy(group => StorageNetworkStorageDisplay.GetTypeName(group.First())))
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
            return StorageNetworkPanelListSignature.BuildStorageListSignature(
                storages,
                instance != null ? instance.mainSearchText : string.Empty,
                StorageNetworkStorageDisplay.GetTypeKey,
                StorageItemUtility.GetStoredItemKey,
                StorageNetworkStorageRules.IsOfflineNetworkServer);
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

        private float GetListScrollOffset()
        {
            return listContent != null ? Mathf.Max(0f, listContent.anchoredPosition.y) : 0f;
        }

        private void RestoreListScrollOffset(float scrollOffset)
        {
            if (listScrollRect == null || listContent == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
            listScrollRect.StopMovement();
            float viewportHeight = listScrollRect.viewport != null ? listScrollRect.viewport.rect.height : 0f;
            float maxOffset = Mathf.Max(0f, listContent.rect.height - viewportHeight);
            Vector2 position = listContent.anchoredPosition;
            position.y = Mathf.Clamp(scrollOffset, 0f, maxOffset);
            listContent.anchoredPosition = position;
        }

        private void ClearList()
        {
            if (listContent == null)
            {
                return;
            }

            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = listContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
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
                GameObject child = categoryContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
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
            CloseGeyserSettingsPanel();
            CloseEnrollableWindow();
            CloseMainWorldDropdown();
            CloseHeaderWindow();
            if (IsActive())
            {
                Deactivate();
            }

            gameObject.SetActive(false);
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (e.Consumed)
            {
                return;
            }

            if (IsTextInputFocused())
            {
                e.Consumed = true;
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

        private static void LogDebug(string message)
        {
            if (DebugLogging)
            {
                Debug.Log("[StorageNetworkPanel] " + message);
            }
        }
    }
}

