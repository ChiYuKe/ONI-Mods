using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private const float CategorySummaryTrendRefreshSeconds = 10f;
        private const int CategorySummaryVirtualizationThreshold = 32;
        private const int CategorySummaryVirtualizationOverscan = 3;
        private const float CategorySummaryRowHeight = 24f;
        private const float CategorySummaryRowSpacing = 3f;
        private const float CategorySummaryVerticalPadding = 8f;
        private readonly StorageNetworkCategorySummaryTrendSampler categorySummaryTrendSampler = new StorageNetworkCategorySummaryTrendSampler();
        private readonly List<Storage> categorySummaryStorages = new List<Storage>();
        private readonly Dictionary<string, ItemTotalAccumulator> categorySummaryTotalsByKey = new Dictionary<string, ItemTotalAccumulator>();
        private readonly Dictionary<Tag, StorageNetworkIndexedItemTotal> categorySummaryIndexedTotals =
            new Dictionary<Tag, StorageNetworkIndexedItemTotal>();
        private readonly List<StorageNetworkCategorySummaryItemTotal> categorySummaryTotals = new List<StorageNetworkCategorySummaryItemTotal>();
        private readonly Dictionary<string, CategorySummaryRowView> categorySummaryLiveRows = new Dictionary<string, CategorySummaryRowView>();
        private int categorySummaryTitleFingerprint = int.MinValue;
        private float categorySummaryStoredKg;
        private ScrollRect categorySummaryScrollRect;
        private bool categorySummaryViewportDirty;

        private void CreateCategorySummaryButton(Transform parent)
        {
            GameObject button = CreateGameButton("CategorySummaryButton", parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_BUTTON), ToggleCategorySummaryPanel);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -14f);
            rect.sizeDelta = new Vector2(56f, 26f);

            ToolTip tooltip = button.AddComponent<ToolTip>();
            tooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_TOOLTIP);
        }

        private void ToggleCategorySummaryPanel()
        {
            if (categorySummaryRoot != null && categorySummaryRoot.activeSelf)
            {
                CloseCategorySummaryPanel();
                return;
            }

            ShowCategorySummaryPanel();
        }

        private void ShowCategorySummaryPanel()
        {
            EnsureCategorySummaryPanel();
            InvalidateCategorySummaryValues();
            categorySummaryRoot.SetActive(true);
            categorySummaryRoot.transform.SetAsLastSibling();
            UpdateCategorySummaryPanel();
        }

        private void CloseCategorySummaryPanel()
        {
            if (categorySummaryRoot != null)
            {
                categorySummaryRoot.SetActive(false);
            }

            InvalidateCategorySummaryValues();
        }

        private void EnsureCategorySummaryPanel()
        {
            if (categorySummaryRoot != null)
            {
                return;
            }

            categorySummaryRoot = CreateBox("CategorySummaryPanel", windowRect, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(categorySummaryRoot.GetComponent<Image>());
            RectTransform panelRect = categorySummaryRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(488f, 0f);
            panelRect.sizeDelta = new Vector2(320f, 720f);

            GameObject header = CreateBox("Header", categorySummaryRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);
            TextMeshProUGUI title = CreateText("Title", header.transform, string.Empty, 13, TextAlignmentOptions.TopLeft);
            title.name = "CategorySummaryTitle";
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = 2f;
            Stretch(title.rectTransform(), 10f, 7f);
            categorySummaryTitle = title;

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, CloseCategorySummaryPanel);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-4f, -4f);
            closeRect.sizeDelta = new Vector2(22f, 20f);

            GameObject viewport = CreateBox("Viewport", categorySummaryRoot.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 10f, 10f, 10f, 70f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            categorySummaryContent = content.AddComponent<RectTransform>();
            categorySummaryRows = new StorageNetworkKeyedRowCache(categorySummaryContent, 32, 120);
            categorySummaryContent.anchorMin = new Vector2(0f, 1f);
            categorySummaryContent.anchorMax = new Vector2(1f, 1f);
            categorySummaryContent.pivot = new Vector2(0.5f, 1f);
            categorySummaryContent.offsetMin = Vector2.zero;
            categorySummaryContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(categorySummaryRoot.transform, 70f, 10f);

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            categorySummaryScrollRect = scrollRect;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = categorySummaryContent;
            ConfigureSmoothVerticalScroll(scrollRect, 26f);
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
            scrollRect.onValueChanged.AddListener(OnCategorySummaryScroll);
            viewport.AddComponent<ScrollWheelBlocker>();

            categorySummaryRoot.SetActive(false);
        }

        private void UpdateCategorySummaryPanel()
        {
            if (categorySummaryRoot == null || !categorySummaryRoot.activeSelf || categorySummaryContent == null)
            {
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.CategorySummary);
            int contentVersion = StorageNetworkContentIndexService.ChangeVersion;
            int membershipVersion = StorageSceneRegistry.MembershipVersion;
            string categoryKey = selectedCategoryKey ?? string.Empty;
            float now = Time.unscaledTime;
            bool categoryChanged = !string.Equals(
                categorySummaryObservedCategoryKey,
                categoryKey,
                System.StringComparison.Ordinal);
            bool membershipChanged =
                categorySummaryObservedMembershipVersion != membershipVersion;
            int scopedStorageVersion = int.MinValue;
            bool hasScopedStorageVersion = !categoryChanged &&
                                           !membershipChanged &&
                                           StorageNetworkContentIndexService.TryGetStorageDisplayVersion(
                                               categorySummaryStorages,
                                               out scopedStorageVersion);
            bool valuesDirty = categoryChanged ||
                               membershipChanged ||
                               (hasScopedStorageVersion
                                   ? categorySummaryObservedStorageVersion != scopedStorageVersion
                                   : categorySummaryObservedContentVersion != contentVersion);
            bool trendRefreshDue = now >= categorySummaryNextTrendRefreshTime;
            bool viewportDirty = categorySummaryViewportDirty;
            if (!valuesDirty && !trendRefreshDue && !viewportDirty)
            {
                return;
            }

            List<Storage> storages = categorySummaryStorages;
            List<StorageNetworkCategorySummaryItemTotal> totals = categorySummaryTotals;
            if (valuesDirty)
            {
                RebuildCategorySummaryValues(storages, totals);
                // Stable key order prevents live mass changes from moving siblings.
                totals.Sort((left, right) =>
                    string.Compare(left.Key, right.Key, System.StringComparison.Ordinal));
                int titleFingerprint = CombineCategorySummaryFingerprint(
                    storages.Count,
                    categorySummaryStoredKg);
                if (titleFingerprint != categorySummaryTitleFingerprint || categoryChanged)
                {
                    categorySummaryTitleFingerprint = titleFingerprint;
                    SetCategorySummaryTitle(
                        StorageCategories.GetName(selectedCategoryKey),
                        storages.Count,
                        categorySummaryStoredKg);
                }

                categorySummaryObservedContentVersion = contentVersion;
                categorySummaryObservedMembershipVersion = membershipVersion;
                categorySummaryObservedStorageVersion =
                    StorageNetworkContentIndexService.TryGetStorageDisplayVersion(
                        storages,
                        out int rebuiltStorageVersion)
                        ? rebuiltStorageVersion
                        : int.MinValue;
            }

            if (valuesDirty || trendRefreshDue)
            {
                categorySummaryTrendSampler.Record(categoryKey, totals);
                if (trendRefreshDue)
                {
                    categorySummaryNextTrendRefreshTime = now +
                        CategorySummaryTrendRefreshSeconds;
                }
            }

            bool structureChanged = valuesDirty &&
                                    HasCategorySummaryStructureChanged(categoryKey, totals);
            if (structureChanged || viewportDirty)
            {
                ReconcileCategorySummaryRows(totals, categoryKey);
            }
            else
            {
                UpdateCategorySummaryRowsLive(totals, categoryKey);
            }

            categorySummaryObservedCategoryKey = categoryKey;
            categorySummaryViewportDirty = false;
            // Activating/deactivating keyed rows already dirties Unity's layout.
            // Let the canvas coalesce that work at the end of the frame instead
            // of forcing a synchronous rebuild for every transient item key.
        }

        private void RebuildCategorySummaryValues(
            List<Storage> storages,
            List<StorageNetworkCategorySummaryItemTotal> totals)
        {
            storages.Clear();
            totals.Clear();
            categorySummaryTotalsByKey.Clear();
            categorySummaryIndexedTotals.Clear();
            if (currentSnapshot?.Storages != null)
            {
                foreach (StorageInfo info in currentSnapshot.Storages)
                {
                    Storage storage = info?.Storage;
                    if (storage != null &&
                        StorageNetworkStorageDisplay.GetCategoryKey(info) == selectedCategoryKey)
                    {
                        storages.Add(storage);
                    }
                }
            }

            if (StorageNetworkContentIndexService.TryFillStorageItemTotals(
                    storages,
                    categorySummaryIndexedTotals,
                    allowStaleContent: true,
                    out categorySummaryStoredKg))
            {
                foreach (StorageNetworkIndexedItemTotal indexed in
                         categorySummaryIndexedTotals.Values)
                {
                    string key = indexed.KeyTag.Name;
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    totals.Add(new StorageNetworkCategorySummaryItemTotal(
                        key,
                        StorageNetworkStorageDisplay.GetStoredItemName(
                            indexed.Representative),
                        indexed.MassKg,
                        indexed.Representative));
                }

                return;
            }

            categorySummaryStoredKg = GetCategorySummaryStoredMass(storages);

            foreach (Storage storage in storages)
            {
                if (storage?.items == null)
                {
                    continue;
                }

                foreach (GameObject item in storage.items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    string key = StorageItemUtility.GetStoredItemKey(item);
                    float mass = GetStoredItemMass(item);
                    if (categorySummaryTotalsByKey.TryGetValue(
                            key,
                            out ItemTotalAccumulator accumulator))
                    {
                        accumulator.MassKg += mass;
                        categorySummaryTotalsByKey[key] = accumulator;
                    }
                    else
                    {
                        categorySummaryTotalsByKey.Add(
                            key,
                            new ItemTotalAccumulator(
                                key,
                                StorageNetworkStorageDisplay.GetStoredItemName(item),
                                mass,
                                item));
                    }
                }
            }

            foreach (ItemTotalAccumulator total in categorySummaryTotalsByKey.Values)
            {
                totals.Add(new StorageNetworkCategorySummaryItemTotal(
                    total.Key,
                    total.Name,
                    total.MassKg,
                    total.Representative));
            }

        }

        private static float GetCategorySummaryStoredMass(List<Storage> storages)
        {
            float storedKg = 0f;
            foreach (Storage storage in storages)
            {
                if (storage != null)
                {
                    storedKg += storage.MassStored();
                }
            }

            return storedKg;
        }

        private bool HasCategorySummaryStructureChanged(
            string categoryKey,
            List<StorageNetworkCategorySummaryItemTotal> totals)
        {
            bool changed = !string.Equals(
                               categorySummaryObservedCategoryKey,
                               categoryKey,
                               System.StringComparison.Ordinal) ||
                           categorySummaryStructureKeys.Count != totals.Count;
            if (!changed)
            {
                for (int index = 0; index < totals.Count; index++)
                {
                    if (!string.Equals(
                            categorySummaryStructureKeys[index],
                            totals[index].Key,
                            System.StringComparison.Ordinal))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return false;
            }

            categorySummaryStructureKeys.Clear();
            foreach (StorageNetworkCategorySummaryItemTotal total in totals)
            {
                categorySummaryStructureKeys.Add(total.Key);
            }

            return true;
        }

        private void ReconcileCategorySummaryRows(
            List<StorageNetworkCategorySummaryItemTotal> totals,
            string categoryKey)
        {
            categorySummaryRows ??= new StorageNetworkKeyedRowCache(categorySummaryContent, 32, 120);
            categorySummaryRows.Begin();
            categorySummaryLiveRows.Clear();
            if (totals.Count == 0)
            {
                UpdateSummaryText(
                    "empty",
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_EMPTY),
                    12,
                    FontStyles.Normal,
                    26f);
                categorySummaryRows.Commit();
                return;
            }

            GetCategorySummaryVisibleRange(
                totals.Count,
                out int firstVisible,
                out int lastVisibleExclusive);
            if (firstVisible > 0)
            {
                UseCategorySummarySpacer(
                    "\0virtual-top",
                    firstVisible * (CategorySummaryRowHeight + CategorySummaryRowSpacing) -
                    CategorySummaryRowSpacing);
            }

            for (int totalIndex = firstVisible;
                 totalIndex < lastVisibleExclusive;
                 totalIndex++)
            {
                StorageNetworkCategorySummaryItemTotal total = totals[totalIndex];
                GameObject row = categorySummaryRows.Use(
                    "item:" + total.Key,
                    CreateCategorySummaryItemRow);
                CategorySummaryRowView view = row.GetComponent<CategorySummaryRowView>();
                if (view == null)
                {
                    continue;
                }

                categorySummaryLiveRows[total.Key] = view;
                StorageNetworkStorageDisplay.SetStoredItemIcon(view.Icon, total.Representative);
                SetTextIfChanged(view.Name, total.Name);
                UpdateCategorySummaryItemRowLive(
                    view,
                    total,
                    categorySummaryTrendSampler.GetTrendPerCycle(categoryKey, total.Key));
            }

            int hiddenAfter = totals.Count - lastVisibleExclusive;
            if (hiddenAfter > 0)
            {
                UseCategorySummarySpacer(
                    "\0virtual-bottom",
                    hiddenAfter * (CategorySummaryRowHeight + CategorySummaryRowSpacing) -
                    CategorySummaryRowSpacing);
            }

            categorySummaryRows.Commit();
        }

        private void GetCategorySummaryVisibleRange(
            int totalCount,
            out int firstVisible,
            out int lastVisibleExclusive)
        {
            firstVisible = 0;
            lastVisibleExclusive = totalCount;
            if (totalCount <= CategorySummaryVirtualizationThreshold ||
                categorySummaryContent == null ||
                categorySummaryScrollRect?.viewport == null)
            {
                return;
            }

            float viewportHeight = categorySummaryScrollRect.viewport.rect.height;
            if (viewportHeight <= 1f)
            {
                viewportHeight = 600f;
            }

            float scrollOffset = Mathf.Max(0f, categorySummaryContent.anchoredPosition.y);
            StorageNetworkVirtualizedRange range =
                StorageNetworkVirtualizedRange.Calculate(
                    totalCount,
                    CategorySummaryVirtualizationThreshold,
                    CategorySummaryVirtualizationOverscan,
                    CategorySummaryRowHeight,
                    CategorySummaryRowSpacing,
                    CategorySummaryVerticalPadding,
                    scrollOffset,
                    viewportHeight);
            firstVisible = range.First;
            lastVisibleExclusive = range.LastExclusive;
        }

        private void UseCategorySummarySpacer(string key, float height)
        {
            GameObject spacer = categorySummaryRows.Use(key, () =>
            {
                GameObject created = new GameObject("VirtualSpacer");
                created.transform.SetParent(categorySummaryContent, false);
                created.AddComponent<RectTransform>();
                created.AddComponent<LayoutElement>();
                return created;
            });
            LayoutElement layout = spacer.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = Mathf.Max(0f, height);
            }
        }

        private void OnCategorySummaryScroll(Vector2 _)
        {
            categorySummaryViewportDirty = true;
        }

        private void UpdateCategorySummaryRowsLive(
            List<StorageNetworkCategorySummaryItemTotal> totals,
            string categoryKey)
        {
            foreach (StorageNetworkCategorySummaryItemTotal total in totals)
            {
                if (categorySummaryLiveRows.TryGetValue(
                        total.Key,
                        out CategorySummaryRowView view) &&
                    view != null)
                {
                    UpdateCategorySummaryItemRowLive(
                        view,
                        total,
                        categorySummaryTrendSampler.GetTrendPerCycle(categoryKey, total.Key));
                }
            }
        }

        private static int CombineCategorySummaryFingerprint(int storageCount, float storedKg)
        {
            unchecked
            {
                return (storageCount * 397) ^ storedKg.GetHashCode();
            }
        }

        private void InvalidateCategorySummaryValues()
        {
            categorySummaryObservedContentVersion = -1;
            categorySummaryObservedStorageVersion = int.MinValue;
            categorySummaryObservedMembershipVersion = -1;
            categorySummaryNextTrendRefreshTime = 0f;
            categorySummaryTitleFingerprint = int.MinValue;
            categorySummaryViewportDirty = true;
        }

        private void ClearCategorySummaryContent()
        {
            categorySummaryRows?.ClearDestroy();
            categorySummaryRows = categorySummaryContent != null
                ? new StorageNetworkKeyedRowCache(categorySummaryContent, 32, 120)
                : null;
            categorySummaryLiveRows.Clear();
            categorySummaryStructureKeys.Clear();
            categorySummaryObservedCategoryKey = null;
            categorySummaryViewportDirty = true;
            InvalidateCategorySummaryValues();
        }

        private void SetCategorySummaryTitle(string categoryName, int storageCount, float storedKg)
        {
            if (categorySummaryTitle != null)
            {
                SetTextIfChanged(
                    categorySummaryTitle,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_TITLE_LINE),
                        categoryName,
                        storageCount,
                        GameUtil.GetFormattedMass(storedKg)));
            }
        }

        private void AddSummaryText(string text, int size, FontStyles style, float height)
        {
            TextMeshProUGUI label = CreateText("SummaryText", categorySummaryContent, text, size, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            label.fontStyle = style;
            label.richText = true;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private void UpdateSummaryText(string key, string text, int size, FontStyles style, float height)
        {
            GameObject row = categorySummaryRows.Use("summary:" + key, () =>
            {
                TextMeshProUGUI created = CreateText("SummaryText", categorySummaryContent, text, size, TextAlignmentOptions.MidlineLeft);
                created.color = new Color(0.18f, 0.19f, 0.19f, 1f);
                created.richText = true;
                created.gameObject.AddComponent<LayoutElement>();
                return created.gameObject;
            });

            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                SetTextIfChanged(label, text);
                label.fontSize = size;
                label.fontStyle = style;
            }

            LayoutElement layout = row.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = height;
            }
        }

        private static void UpdateCategorySummaryItemRowLive(
            CategorySummaryRowView view,
            StorageNetworkCategorySummaryItemTotal total,
            float? trendKgPerCycle)
        {
            int massFingerprint = total.MassKg.GetHashCode();
            if (view.LastMassFingerprint != massFingerprint)
            {
                view.LastMassFingerprint = massFingerprint;
                SetTextIfChanged(view.Mass, GameUtil.GetFormattedMass(total.MassKg));
            }

            int trendFingerprint = trendKgPerCycle.HasValue
                ? trendKgPerCycle.Value.GetHashCode()
                : int.MinValue + 1;
            if (view.LastTrendFingerprint != trendFingerprint)
            {
                view.LastTrendFingerprint = trendFingerprint;
                SetTextIfChanged(
                    view.Trend,
                    StorageNetworkCategorySummaryTrend.Format(trendKgPerCycle));
                Color trendColor = StorageNetworkCategorySummaryTrend.GetColor(trendKgPerCycle);
                if (view.Trend.color != trendColor)
                {
                    view.Trend.color = trendColor;
                }
            }
        }

        private GameObject CreateCategorySummaryItemRow()
        {
            GameObject row = CreatePlainImage("SummaryItemRow", categorySummaryContent, new Color(0.86f, 0.85f, 0.80f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 24f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 1, 1);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>();
            iconObject.AddComponent<LayoutElement>().preferredWidth = 20f;
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            TextMeshProUGUI name = CreateText("Name", row.transform, string.Empty, 11, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI mass = CreateText("Mass", row.transform, string.Empty, 11, TextAlignmentOptions.MidlineRight);
            mass.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            mass.textWrappingMode = TextWrappingModes.NoWrap;
            mass.gameObject.AddComponent<LayoutElement>().preferredWidth = 84f;

            TextMeshProUGUI trend = CreateText("Trend", row.transform, string.Empty, 10, TextAlignmentOptions.MidlineRight);
            trend.textWrappingMode = TextWrappingModes.NoWrap;
            trend.gameObject.AddComponent<LayoutElement>().preferredWidth = 86f;

            row.AddComponent<CategorySummaryRowView>().Configure(icon, name, mass, trend);
            return row;
        }

        private struct ItemTotalAccumulator
        {
            public ItemTotalAccumulator(string key, string name, float massKg, GameObject representative)
            {
                Key = key;
                Name = name;
                MassKg = massKg;
                Representative = representative;
            }

            public string Key { get; }

            public string Name { get; }

            public float MassKg { get; set; }

            public GameObject Representative { get; }
        }

        private sealed class CategorySummaryRowView : MonoBehaviour
        {
            public Image Icon { get; private set; }

            public TextMeshProUGUI Name { get; private set; }

            public TextMeshProUGUI Mass { get; private set; }

            public TextMeshProUGUI Trend { get; private set; }

            public int LastMassFingerprint { get; set; } = int.MinValue;

            public int LastTrendFingerprint { get; set; } = int.MinValue;

            public void Configure(Image icon, TextMeshProUGUI name, TextMeshProUGUI mass, TextMeshProUGUI trend)
            {
                Icon = icon;
                Name = name;
                Mass = mass;
                Trend = trend;
            }
        }
    }
}

