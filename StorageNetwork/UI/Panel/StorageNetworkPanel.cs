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
        private bool mainListViewportDirty;
        private RectTransform windowRect;
        private GameObject modalRoot;
        private GameObject categorySummaryRoot;
        private RectTransform categorySummaryContent;
        private StorageNetworkKeyedRowCache categorySummaryRows;
        private TextMeshProUGUI categorySummaryTitle;
        private GameObject enrollableWindowRoot;
        private string enrollableWindowSignature;
        private int enrollableObservedRegistryVersion = -1;
        private StorageNetworkKeyedRowCache enrollableRows;
        private int enrollableWorldFilterId = UnsetEnrollableWorldFilterId;
        private RectTransform enrollableWorldFilterContent;
        private GameObject enrollableWorldDropdownRoot;
        private KInputTextField enrollableSearchInput;
        private string enrollableSearchText = string.Empty;
        private GameObject headerWindowRoot;
        private StorageNetworkOrderProductionCenter boundOrderProductionCenter;
        private int orderWorldFilterId = UnsetEnrollableWorldFilterId;
        private RectTransform orderWorldFilterContent;
        private GameObject orderWorldDropdownRoot;
        private GameObject productionSettingsRoot;
        private RectTransform productionSettingsContent;
        private TextMeshProUGUI productionSettingsTitle;
        private Storage productionSettingsStorage;
        private bool productionSettingsPositionInitialized;
        private GameObject geyserSettingsRoot;
        private RectTransform geyserSettingsContent;
        private Geyser geyserSettingsGeyser;
        private string geyserSettingsSignature;
        private GameObject productionPickerRoot;
        private static GameObject standaloneOutputFilterPickerRoot;
        private static GameObject standaloneOutputFilterPickerWindow;
        private static bool standaloneRightClickCloseCandidate;
        private static Vector3 standaloneRightClickStartPosition;
        private string productionSettingsSignature;
        private ProductionOverviewCardView productionOverviewView;
        private ProductionInventoryCardView productionInventoryView;
        private ProductionAutomationCardsView productionAutomationView;
        private int categorySummaryObservedContentVersion = -1;
        private int categorySummaryObservedStorageVersion = int.MinValue;
        private int categorySummaryObservedMembershipVersion = -1;
        private string categorySummaryObservedCategoryKey;
        private float categorySummaryNextTrendRefreshTime;
        private readonly List<string> categorySummaryStructureKeys = new List<string>();
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
        private RectTransform deferredMainLayoutRoot;
        private int deferredMainLayoutFrame = -1;
        private bool restoreMainScrollAfterLayout;
        private float deferredMainScrollOffset;
        private int deferredStructureRefreshFrame = -1;
        private int lastObservedRegistryVersion = -1;
        private int lastObservedContentChangeVersion = -1;
        private int mainWorldFilterValidatedMembershipVersion = -1;
        private int mainWorldFilterValidatedActiveWorld = int.MinValue;
        private float liveTotalStoredKg;
        private float liveTotalCapacityKg;
        private string lastListSignature;
        private readonly StorageNetworkDebounceGate mainSearchDebounce = new StorageNetworkDebounceGate(0.15f);
        private readonly StorageNetworkDebounceGate enrollableSearchDebounce = new StorageNetworkDebounceGate(0.15f);
        private readonly StorageNetworkDebounceGate orderTrackingSearchDebounce = new StorageNetworkDebounceGate(0.15f);
        private const float LiveRefreshSeconds = 0.5f;
        private const string EmptyListSignature = "empty";
        private const string CoreOfflineListSignature = "core_offline";
        private const string CrossWorldRelayOfflineListSignature = "cross_world_relay_offline";

        private enum StoragePanelRefreshMode
        {
            Live,
            StructureCheck,
            Structure
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
            lastObservedRegistryVersion = StorageSceneRegistry.Version;
            lastObservedContentChangeVersion = StorageNetworkContentIndexService.ChangeVersion;
            refreshElapsed = 0f;
            FocusStorageRow(focusStorage);
            RefreshStoragePanel(StoragePanelRefreshMode.Structure);
        }

        private void Update()
        {
            if (summaryText == null || listContent == null)
            {
                return;
            }

            UpdatePanelDrag();
            RunDeferredMainLayout();
            float deltaTime = Time.unscaledDeltaTime;
            ProcessSearchDebounces(deltaTime);
            UpdateOrderPanelAutoRefresh(deltaTime);

            if (windowRect == null || !windowRect.gameObject.activeInHierarchy)
            {
                return;
            }

            if (categorySummaryViewportDirty &&
                categorySummaryRoot != null &&
                categorySummaryRoot.activeInHierarchy)
            {
                UpdateCategorySummaryPanel();
            }

            if (enrollableViewportDirty &&
                enrollableWindowRoot != null &&
                enrollableWindowRoot.activeInHierarchy)
            {
                enrollableViewportDirty = false;
                ReconcileEnrollableRows();
            }

            if (mainListViewportDirty)
            {
                mainListViewportDirty = false;
                ReconcileVisibleStoredItemSections();
            }

            RunDeferredStoragePanelRefresh();
            refreshElapsed += deltaTime;
            if (refreshElapsed >= LiveRefreshSeconds)
            {
                refreshElapsed = 0f;
                int registryVersion = StorageSceneRegistry.Version;
                bool refreshStructure = registryVersion != lastObservedRegistryVersion;
                if (refreshStructure)
                {
                    lastObservedRegistryVersion = registryVersion;
                }

                int contentChangeVersion = StorageNetworkContentIndexService.ChangeVersion;
                bool contentChanged = contentChangeVersion != lastObservedContentChangeVersion;
                lastObservedContentChangeVersion = contentChangeVersion;
                if (contentChanged && !string.IsNullOrEmpty(mainSearchText))
                {
                    RequestDeferredStoragePanelStructureRefresh();
                }

                RefreshStoragePanel(refreshStructure ? StoragePanelRefreshMode.StructureCheck : StoragePanelRefreshMode.Live);
                UpdateProductionSettingsPanel();
                UpdateGeyserSettingsPanel();
                UpdateEnrollableWindowLive();
            }
        }

        private void ProcessSearchDebounces(float deltaTime)
        {
            if (mainSearchDebounce.Advance(deltaTime) &&
                windowRect != null &&
                windowRect.gameObject.activeInHierarchy)
            {
                selectedItemStorage = null;
                selectedItemKey = null;
                lastListSignature = null;
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
            }

            if (enrollableSearchDebounce.Advance(deltaTime) &&
                enrollableWindowRoot != null &&
                enrollableWindowRoot.activeInHierarchy)
            {
                enrollableWindowSignature = null;
                ShowEnrollableBuildingsDialog();
            }

            if (orderTrackingSearchDebounce.Advance(deltaTime) &&
                headerWindowRoot != null &&
                headerWindowRoot.activeInHierarchy)
            {
                orderTrackingSignature = null;
                RebuildOrderDetails();
            }
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

            using (StorageNetworkFrameProfileTool.BeginWork(
                       StorageNetworkPerformanceArea.MainPanel))
            {
                RefreshStoragePanelMain(mode);
            }

            UpdateCategorySummaryPanel();
        }

        private void RefreshStoragePanelMain(StoragePanelRefreshMode mode)
        {
            bool forceRebuild = mode == StoragePanelRefreshMode.Structure;
            bool checkStructure = forceRebuild || mode == StoragePanelRefreshMode.StructureCheck;
            EnsureValidMainWorldFilter();
            if (currentSnapshot == null || mode != StoragePanelRefreshMode.Live)
            {
                currentSnapshot = CollectMainSnapshot(forceRebuild);
            }

            RefreshMainPanelLiveMetrics();
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

        private void RequestDeferredStoragePanelStructureRefresh()
        {
            deferredStructureRefreshFrame = Mathf.Max(deferredStructureRefreshFrame, Time.frameCount + 1);
        }

        private void RunDeferredStoragePanelRefresh()
        {
            if (deferredStructureRefreshFrame < 0 || Time.frameCount < deferredStructureRefreshFrame)
            {
                return;
            }

            deferredStructureRefreshFrame = -1;
            StorageSceneCollector.InvalidateCache();
            RefreshStoragePanel(StoragePanelRefreshMode.StructureCheck);
        }

        private void RefreshEmptyStorageList(bool forceRebuild)
        {
            if (forceRebuild || lastListSignature != EmptyListSignature)
            {
                lastListSignature = EmptyListSignature;
                ClearCategories();
                ClearList();
                CreateInfoRow(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.EMPTY_TITLE),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.EMPTY_DETAILS));
                RebuildLayout();
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
                RebuildLayout();
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
                RebuildLayout();
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
                lastListSignature == CoreOfflineListSignature ||
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

        private void LiveUpdateStoragePanels()
        {
            UpdateMainStorageRowsLive();
        }

    }
}

