using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private const int AllEnrollableWorldsFilterId = -1;
        private const int UnsetEnrollableWorldFilterId = -2;
        private const int EnrollableVirtualizationThreshold = 32;
        private const int EnrollableVirtualizationOverscan = 3;
        private const float EnrollableRowSpacing = 5f;
        private const float EnrollableVerticalPadding = 20f;
        private readonly List<EnrollableListEntry> enrollableListEntries =
            new List<EnrollableListEntry>();
        private ScrollRect enrollableScrollRect;
        private bool enrollableViewportDirty;

        private void ShowEnrollableBuildingsDialog()
        {
            EnsureEnrollableWindow();
            bool wasVisible = enrollableWindowRoot.activeInHierarchy;

            List<StorageNetworkEnrollment> enrollments = StorageSceneRegistry
                .GetEnrollments()
                .Where(enrollment => enrollment != null && enrollment.CanShowInEnrollableList())
                .ToList();
            EnsureValidEnrollableWorldFilter(enrollments);

            string signature = StorageNetworkEnrollableWindowSignature.Build(enrollments, enrollableWorldFilterId, enrollableSearchText);
            bool structureChanged = signature != enrollableWindowSignature;
            if (structureChanged)
            {
                enrollableWindowSignature = signature;
                RebuildEnrollableWorldFilter(enrollments);
                BuildEnrollableWindowContent(enrollments);
            }

            enrollableObservedRegistryVersion = StorageSceneRegistry.Version;

            enrollableWindowRoot.SetActive(true);
            if (structureChanged || !wasVisible)
            {
                RequestMainLayout(enrollableWindowContent);
            }
        }

        private void BuildEnrollableWindowContent(List<StorageNetworkEnrollment> enrollments)
        {
            enrollableListEntries.Clear();
            List<StorageNetworkEnrollment> filteredEnrollments = FilterEnrollmentsByWorld(enrollments).ToList();
            if (filteredEnrollments.Count > 0)
            {
                foreach (IGrouping<string, StorageNetworkEnrollment> categoryGroup in filteredEnrollments
                    .GroupBy(StorageNetworkPlanCategoryOrder.GetCategoryKey)
                    .OrderBy(group => StorageNetworkPlanCategoryOrder.GetSortOrder(group.Key))
                    .ThenBy(group => StorageNetworkPlanCategoryOrder.GetDisplayName(group.Key)))
                {
                    List<StorageNetworkEnrollment> categoryEnrollments = categoryGroup
                        .OrderBy(enrollment => enrollment.gameObject.GetProperName())
                        .ToList();
                    enrollableListEntries.Add(EnrollableListEntry.Category(
                        categoryGroup.Key,
                        categoryEnrollments.Count));

                    foreach (StorageNetworkEnrollment enrollment in categoryEnrollments)
                    {
                        enrollableListEntries.Add(
                            EnrollableListEntry.Building(enrollment));
                    }
                }
            }

            ReconcileEnrollableRows();
        }

        private void ReconcileEnrollableRows()
        {
            enrollableRows ??= new StorageNetworkKeyedRowCache(enrollableWindowContent);
            enrollableRows.Begin();
            enrollableStorageLiveViews.Clear();
            if (enrollableListEntries.Count == 0)
            {
                enrollableRows.Use("info:empty", () =>
                {
                    TextMeshProUGUI empty = CreateText("Empty", enrollableWindowContent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_EMPTY), 12, TextAlignmentOptions.TopLeft);
                    empty.color = new Color(0.18f, 0.19f, 0.19f, 1f);
                    empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
                    return empty.gameObject;
                });
                enrollableRows.Commit();
                return;
            }

            GetEnrollableVisibleRange(
                out int firstVisible,
                out int lastVisibleExclusive);
            if (firstVisible > 0)
            {
                UseEnrollableSpacer(
                    "\0virtual-top",
                    GetEnrollableHiddenHeight(0, firstVisible));
            }

            for (int index = firstVisible; index < lastVisibleExclusive; index++)
            {
                EnrollableListEntry entry = enrollableListEntries[index];
                if (entry.IsCategory)
                {
                    string headerKey =
                        "category:" + entry.CategoryKey + ":" + entry.CategoryCount;
                    enrollableRows.Use(
                        headerKey,
                        () => CreateEnrollableCategoryHeader(
                            enrollableWindowContent,
                            entry.CategoryKey,
                            entry.CategoryCount));
                    continue;
                }

                StorageNetworkEnrollment enrollment = entry.Enrollment;
                if (enrollment == null)
                {
                    continue;
                }

                string rowKey = "enrollment:" + enrollment.GetInstanceID();
                GameObject row = enrollableRows.Use(
                    rowKey,
                    () => CreateEnrollableBuildingRow(
                        enrollableWindowContent,
                        enrollment));
                EnrollableBuildingRowView view =
                    row.GetComponent<EnrollableBuildingRowView>();
                if (view != null)
                {
                    enrollableStorageLiveViews.Add(view);
                    UpdateEnrollableBuildingRow(row, enrollment);
                }
            }

            int hiddenAfter = enrollableListEntries.Count - lastVisibleExclusive;
            if (hiddenAfter > 0)
            {
                UseEnrollableSpacer(
                    "\0virtual-bottom",
                    GetEnrollableHiddenHeight(lastVisibleExclusive, hiddenAfter));
            }

            enrollableRows.Commit();
        }

        private void GetEnrollableVisibleRange(
            out int firstVisible,
            out int lastVisibleExclusive)
        {
            firstVisible = 0;
            lastVisibleExclusive = enrollableListEntries.Count;
            if (enrollableListEntries.Count <= EnrollableVirtualizationThreshold ||
                enrollableWindowContent == null ||
                enrollableScrollRect?.viewport == null)
            {
                return;
            }

            float viewportHeight = enrollableScrollRect.viewport.rect.height;
            if (viewportHeight <= 1f)
            {
                viewportHeight = 600f;
            }

            float startOffset = Mathf.Max(
                0f,
                enrollableWindowContent.anchoredPosition.y -
                EnrollableVerticalPadding * 0.5f);
            float endOffset = startOffset + viewportHeight;
            float cursor = 0f;
            int first = 0;
            while (first < enrollableListEntries.Count &&
                   cursor + enrollableListEntries[first].Height < startOffset)
            {
                cursor += enrollableListEntries[first].Height + EnrollableRowSpacing;
                first++;
            }

            int last = first;
            float visibleCursor = cursor;
            while (last < enrollableListEntries.Count && visibleCursor < endOffset)
            {
                visibleCursor += enrollableListEntries[last].Height +
                                 EnrollableRowSpacing;
                last++;
            }

            firstVisible = Mathf.Max(0, first - EnrollableVirtualizationOverscan);
            lastVisibleExclusive = Mathf.Min(
                enrollableListEntries.Count,
                last + EnrollableVirtualizationOverscan);
        }

        private float GetEnrollableHiddenHeight(int start, int count)
        {
            float height = 0f;
            int end = Mathf.Min(enrollableListEntries.Count, start + count);
            for (int index = start; index < end; index++)
            {
                height += enrollableListEntries[index].Height;
            }

            if (count > 1)
            {
                height += (count - 1) * EnrollableRowSpacing;
            }

            return height;
        }

        private void UseEnrollableSpacer(string key, float height)
        {
            GameObject spacer = enrollableRows.Use(key, () =>
            {
                GameObject created = new GameObject("VirtualSpacer");
                created.transform.SetParent(enrollableWindowContent, false);
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

        private void OnEnrollableScroll(Vector2 _)
        {
            enrollableViewportDirty = true;
        }

        private RectTransform enrollableWindowContent;

        private void EnsureEnrollableWindow()
        {
            if (enrollableWindowRoot != null)
            {
                return;
            }

            enrollableWindowRoot = CreateBox("EnrollableWindowPanel", windowRect, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(enrollableWindowRoot.GetComponent<Image>());
            RectTransform panelRect = enrollableWindowRoot.GetComponent<RectTransform>();
            SetStretch(panelRect, 8f, 8f, 8f, 42f);

            GameObject header = CreateBox("Header", enrollableWindowRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 42f);

            TextMeshProUGUI title = CreateText("Title", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_TITLE), 14, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);
            title.rectTransform().offsetMax = new Vector2(-250f, 0f);

            GameObject worldFilter = new GameObject("WorldFilter");
            worldFilter.transform.SetParent(header.transform, false);
            enrollableWorldFilterContent = worldFilter.AddComponent<RectTransform>();
            enrollableWorldFilterContent.anchorMin = new Vector2(1f, 0f);
            enrollableWorldFilterContent.anchorMax = new Vector2(1f, 1f);
            enrollableWorldFilterContent.pivot = new Vector2(1f, 0.5f);
            enrollableWorldFilterContent.offsetMin = new Vector2(-238f, 6f);
            enrollableWorldFilterContent.offsetMax = new Vector2(-44f, -6f);

            HorizontalLayoutGroup filterLayout = worldFilter.AddComponent<HorizontalLayoutGroup>();
            filterLayout.padding = new RectOffset(0, 0, 0, 0);
            filterLayout.spacing = 6f;
            filterLayout.childAlignment = TextAnchor.MiddleLeft;
            filterLayout.childControlWidth = true;
            filterLayout.childControlHeight = true;
            filterLayout.childForceExpandWidth = false;
            filterLayout.childForceExpandHeight = true;

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, CloseEnrollableWindow);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-10f, 0f);
            closeRect.sizeDelta = new Vector2(30f, 30f);

            CreateEnrollableSearchBar(enrollableWindowRoot.transform);

            GameObject viewport = CreateBox("Viewport", enrollableWindowRoot.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 10f, 10f, 10f, 92f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            enrollableWindowContent = content.AddComponent<RectTransform>();
            enrollableRows = new StorageNetworkKeyedRowCache(enrollableWindowContent);
            enrollableWindowContent.anchorMin = new Vector2(0f, 1f);
            enrollableWindowContent.anchorMax = new Vector2(1f, 1f);
            enrollableWindowContent.pivot = new Vector2(0.5f, 1f);
            enrollableWindowContent.offsetMin = Vector2.zero;
            enrollableWindowContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(enrollableWindowRoot.transform, 92f, 10f);

            enrollableScrollRect = viewport.AddComponent<ScrollRect>();
            enrollableScrollRect.viewport = viewport.GetComponent<RectTransform>();
            enrollableScrollRect.content = enrollableWindowContent;
            ConfigureSmoothVerticalScroll(enrollableScrollRect, 26f);
            enrollableScrollRect.verticalScrollbar = scrollbar;
            enrollableScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            enrollableScrollRect.verticalScrollbarSpacing = 2f;
            enrollableScrollRect.onValueChanged.AddListener(OnEnrollableScroll);
            viewport.AddComponent<ScrollWheelBlocker>();

            enrollableWindowRoot.SetActive(false);
        }

        private void ClearEnrollableWindowContent()
        {
            enrollableStorageLiveViews.Clear();
            enrollableListEntries.Clear();
            enrollableRows?.ClearDestroy();
            enrollableRows = enrollableWindowContent != null
                ? new StorageNetworkKeyedRowCache(enrollableWindowContent)
                : null;
        }

        private void CloseEnrollableWindow()
        {
            if (enrollableWindowRoot != null)
            {
                enrollableWindowRoot.SetActive(false);
            }

            CloseEnrollableWorldDropdown();
            enrollableWindowSignature = null;
            enrollableWorldFilterId = UnsetEnrollableWorldFilterId;
            enrollableSearchText = string.Empty;
            if (enrollableSearchInput != null)
            {
                enrollableSearchInput.SetTextWithoutNotify(string.Empty);
            }
        }

        private readonly struct EnrollableListEntry
        {
            private EnrollableListEntry(
                string categoryKey,
                int categoryCount,
                StorageNetworkEnrollment enrollment,
                float height)
            {
                CategoryKey = categoryKey;
                CategoryCount = categoryCount;
                Enrollment = enrollment;
                Height = height;
            }

            public string CategoryKey { get; }
            public int CategoryCount { get; }
            public StorageNetworkEnrollment Enrollment { get; }
            public float Height { get; }
            public bool IsCategory => Enrollment == null;

            public static EnrollableListEntry Category(
                string categoryKey,
                int categoryCount)
            {
                return new EnrollableListEntry(
                    categoryKey,
                    categoryCount,
                    null,
                    30f);
            }

            public static EnrollableListEntry Building(
                StorageNetworkEnrollment enrollment)
            {
                return new EnrollableListEntry(
                    null,
                    0,
                    enrollment,
                    38f);
            }
        }

        private void CreateEnrollableSearchBar(Transform parent)
        {
            GameObject bar = CreatePlainImage("SearchBar", parent, new Color(0.80f, 0.79f, 0.74f, 1f));
            RectTransform barRect = bar.GetComponent<RectTransform>();
            SetTopStretch(barRect, 10f, 10f, 58f, 30f);

            TextMeshProUGUI header = CreateText("SearchBarHeader", bar.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_HEADER), 13, TextAlignmentOptions.MidlineLeft);
            header.color = new Color(0.34f, 0.39f, 0.38f, 1f);
            header.fontStyle = FontStyles.Normal;
            Stretch(header.rectTransform(), 12f, 0f);
            header.rectTransform().offsetMax = new Vector2(-236f, 0f);

            GameObject inputSlot = new GameObject("SearchInputSlot");
            inputSlot.transform.SetParent(bar.transform, false);
            RectTransform slotRect = inputSlot.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(1f, 0.5f);
            slotRect.anchorMax = new Vector2(1f, 0.5f);
            slotRect.pivot = new Vector2(1f, 0.5f);
            slotRect.anchoredPosition = new Vector2(-12f, 0f);
            slotRect.sizeDelta = new Vector2(210f, 24f);

            enrollableSearchInput = StorageNetworkInputBuilder.CreateKNumberInput(
                inputSlot.transform,
                "EnrollableSearchInput",
                enrollableSearchText,
                210f,
                24f,
                11,
                TextAlignmentOptions.MidlineLeft,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                "web_box",
                Color.white,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                new Vector2(7f, 2f),
                true);
            enrollableSearchInput.characterLimit = 64;
            enrollableSearchInput.characterValidation = TMP_InputField.CharacterValidation.None;
            enrollableSearchInput.contentType = TMP_InputField.ContentType.Standard;
            enrollableSearchInput.inputType = TMP_InputField.InputType.Standard;
            enrollableSearchInput.keyboardType = TouchScreenKeyboardType.Default;
            enrollableSearchInput.lineType = TMP_InputField.LineType.SingleLine;
            if (enrollableSearchInput.textComponent != null)
            {
                enrollableSearchInput.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
                enrollableSearchInput.textComponent.overflowMode = TextOverflowModes.Ellipsis;
            }

            RectTransform inputRect = enrollableSearchInput.gameObject.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = Vector2.zero;
            inputRect.sizeDelta = new Vector2(210f, 24f);
            enrollableSearchInput.gameObject.AddComponent<StorageNetworkTextInputGuard>().Configure(enrollableSearchInput, enrollableSearchInput.gameObject.GetComponent<Image>());
            enrollableSearchInput.onValueChanged.AddListener(value =>
            {
                enrollableSearchText = value ?? string.Empty;
                enrollableSearchDebounce.Request();
            });
        }

    }
}
