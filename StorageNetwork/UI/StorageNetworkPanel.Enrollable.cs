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

        private void ShowEnrollableBuildingsDialog()
        {
            EnsureEnrollableWindow();
            if (!enrollableWindowRoot.activeSelf)
            {
                int activeWorldId = GetActiveWorldFilterId();
                enrollableWorldFilterId = activeWorldId != UnsetEnrollableWorldFilterId ? activeWorldId : AllEnrollableWorldsFilterId;
            }

            List<StorageNetworkEnrollment> enrollments = StorageSceneRegistry
                .GetEnrollments()
                .Where(enrollment => enrollment != null && enrollment.CanShowInEnrollableList())
                .ToList();
            EnsureValidEnrollableWorldFilter(enrollments);

            string signature = StorageNetworkEnrollableWindowSignature.Build(enrollments, enrollableWorldFilterId, enrollableSearchText);
            if (signature != enrollableWindowSignature)
            {
                enrollableWindowSignature = signature;
                ClearEnrollableWindowContent();
                RebuildEnrollableWorldFilter(enrollments);
                BuildEnrollableWindowContent(enrollments);
            }

            enrollableWindowRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(enrollableWindowContent);
        }

        private void BuildEnrollableWindowContent(List<StorageNetworkEnrollment> enrollments)
        {
            List<StorageNetworkEnrollment> filteredEnrollments = FilterEnrollmentsByWorld(enrollments).ToList();
            if (filteredEnrollments.Count == 0)
            {
                TextMeshProUGUI empty = CreateText("Empty", enrollableWindowContent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_EMPTY), 12, TextAlignmentOptions.TopLeft);
                empty.color = new Color(0.18f, 0.19f, 0.19f, 1f);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            }
            else
            {
                foreach (IGrouping<string, StorageNetworkEnrollment> categoryGroup in filteredEnrollments
                    .GroupBy(StorageNetworkPlanCategoryOrder.GetCategoryKey)
                    .OrderBy(group => StorageNetworkPlanCategoryOrder.GetSortOrder(group.Key))
                    .ThenBy(group => StorageNetworkPlanCategoryOrder.GetDisplayName(group.Key)))
                {
                    List<StorageNetworkEnrollment> categoryEnrollments = categoryGroup
                        .OrderBy(enrollment => enrollment.gameObject.GetProperName())
                        .ToList();
                    CreateEnrollableCategoryHeader(enrollableWindowContent, categoryGroup.Key, categoryEnrollments.Count);

                    foreach (StorageNetworkEnrollment enrollment in categoryEnrollments)
                    {
                        CreateEnrollableBuildingRow(enrollableWindowContent, enrollment);
                    }
                }
            }
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

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = enrollableWindowContent;
            ConfigureSmoothVerticalScroll(scrollRect, 26f);
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
            viewport.AddComponent<ScrollWheelBlocker>();

            enrollableWindowRoot.SetActive(false);
        }

        private void ClearEnrollableWindowContent()
        {
            for (int i = enrollableWindowContent.childCount - 1; i >= 0; i--)
            {
                Destroy(enrollableWindowContent.GetChild(i).gameObject);
            }
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
                enrollableWindowSignature = null;
                ShowEnrollableBuildingsDialog();
            });
        }

        private void RebuildEnrollableWorldFilter(List<StorageNetworkEnrollment> enrollments)
        {
            if (enrollableWorldFilterContent == null)
            {
                return;
            }

            for (int i = enrollableWorldFilterContent.childCount - 1; i >= 0; i--)
            {
                Destroy(enrollableWorldFilterContent.GetChild(i).gameObject);
            }

            GameObject dropdownButton = CreateStyledButton(
                "WorldFilterDropdown",
                enrollableWorldFilterContent,
                GetSelectedWorldFilterText(),
                () => ToggleEnrollableWorldDropdown(enrollments),
                CreateColorStyle(
                    new Color(0.17f, 0.19f, 0.25f, 1f),
                    new Color(0.25f, 0.28f, 0.35f, 1f),
                    new Color(0.11f, 0.12f, 0.16f, 1f)));
            SetButtonLabelColor(dropdownButton, new Color(0.92f, 0.93f, 0.90f, 1f), FontStyles.Normal);
            AddDropdownArrowIcon(dropdownButton.transform);
            LayoutElement layout = dropdownButton.AddComponent<LayoutElement>();
            layout.preferredWidth = 194f;
            layout.preferredHeight = 22f;
        }

        private void ToggleEnrollableWorldDropdown(List<StorageNetworkEnrollment> enrollments)
        {
            if (enrollableWorldDropdownRoot != null)
            {
                CloseEnrollableWorldDropdown();
                return;
            }

            ShowEnrollableWorldDropdown(enrollments);
        }

        private void ShowEnrollableWorldDropdown(List<StorageNetworkEnrollment> enrollments)
        {
            if (enrollableWindowRoot == null)
            {
                return;
            }

            CloseEnrollableWorldDropdown();
            List<int> worldIds = GetEnrollableWorldIds(enrollments);
            int optionCount = worldIds.Count + 1;
            float height = Mathf.Min(20f + optionCount * 30f, 250f);

            enrollableWorldDropdownRoot = CreatePlainImage("WorldFilterDropdownPanel", enrollableWindowRoot.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            enrollableWorldDropdownRoot.AddComponent<ScrollWheelBlocker>();
            ApplyThinBoxSprite(enrollableWorldDropdownRoot.GetComponent<Image>());
            RectTransform dropdownRect = enrollableWorldDropdownRoot.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(1f, 1f);
            dropdownRect.anchorMax = new Vector2(1f, 1f);
            dropdownRect.pivot = new Vector2(1f, 1f);
            dropdownRect.anchoredPosition = new Vector2(-52f, -50f);
            dropdownRect.sizeDelta = new Vector2(194f, height);

            GameObject viewport = CreatePlainImage("Viewport", enrollableWorldDropdownRoot.transform, new Color(0.73f, 0.73f, 0.67f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 6f, 6f, 8f, 8f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateEnrollableWorldDropdownOption(content.transform, AllEnrollableWorldsFilterId, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_WORLD_ALL));
            foreach (int worldId in worldIds)
            {
                CreateEnrollableWorldDropdownOption(content.transform, worldId, StorageNetworkWorldDisplay.GetWorldName(worldId));
            }

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            ConfigureSmoothVerticalScroll(scrollRect, 22f);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void CreateEnrollableWorldDropdownOption(Transform parent, int worldId, string text)
        {
            bool selected = enrollableWorldFilterId == worldId;
            ColorStyleSetting style = selected
                ? KleiPinkStyle()
                : CreateColorStyle(
                    new Color(0.80f, 0.80f, 0.73f, 1f),
                    new Color(0.87f, 0.87f, 0.80f, 1f),
                    new Color(0.67f, 0.68f, 0.62f, 1f));
            GameObject row = CreateStyledButton("WorldFilterOption", parent, text, () =>
            {
                enrollableWorldFilterId = worldId;
                CloseEnrollableWorldDropdown();
                enrollableWindowSignature = null;
                ShowEnrollableBuildingsDialog();
            }, style);
            SetButtonLabelColor(row, selected ? Color.white : new Color(0.23f, 0.26f, 0.26f, 1f), FontStyles.Bold);
            AddWorldFilterOptionIcon(row.transform, worldId);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 26f;
        }

        private static void SetButtonLabelColor(GameObject button, Color color, FontStyles fontStyle)
        {
            TextMeshProUGUI label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (label != null)
            {
                label.color = color;
                label.fontStyle = fontStyle;
                label.rectTransform().offsetMax = new Vector2(-28f, 0f);
            }
        }

        private static void AddDropdownArrowIcon(Transform parent)
        {
            Sprite sprite = GetSpriteByName("dash_arrow_down");
            if (sprite == null)
            {
                return;
            }

            GameObject iconObject = new GameObject("DropdownArrow");
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-5f, 0f);
            rect.sizeDelta = new Vector2(24f, 24f);

            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = new Color(0.92f, 0.93f, 0.90f, 1f);
        }

        private static void AddWorldFilterOptionIcon(Transform parent, int worldId)
        {
            if (worldId == AllEnrollableWorldsFilterId)
            {
                return;
            }

            Sprite sprite = StorageNetworkWorldDisplay.GetWorldSprite(worldId);
            if (sprite == null)
            {
                return;
            }

            GameObject iconObject = new GameObject("WorldIcon");
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(10f, 0f);
            rect.sizeDelta = new Vector2(18f, 18f);

            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = Color.white;

            TextMeshProUGUI label = parent.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.rectTransform().offsetMin = new Vector2(30f, 0f);
            }
        }

        private void CloseEnrollableWorldDropdown()
        {
            if (enrollableWorldDropdownRoot != null)
            {
                Destroy(enrollableWorldDropdownRoot);
                enrollableWorldDropdownRoot = null;
            }
        }

        private string GetSelectedWorldFilterText()
        {
            return enrollableWorldFilterId == AllEnrollableWorldsFilterId
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_WORLD_ALL)
                : StorageNetworkWorldDisplay.GetWorldName(enrollableWorldFilterId);
        }

        private IEnumerable<StorageNetworkEnrollment> FilterEnrollmentsByWorld(IEnumerable<StorageNetworkEnrollment> enrollments)
        {
            if (enrollableWorldFilterId == AllEnrollableWorldsFilterId)
            {
                return FilterEnrollmentsBySearch(enrollments);
            }

            IEnumerable<StorageNetworkEnrollment> filtered = enrollments.Where(enrollment =>
            {
                if (enrollment == null)
                {
                    return false;
                }

                return TryGetBuildingWorldId(enrollment.gameObject, out int worldId) && worldId == enrollableWorldFilterId;
            });

            return FilterEnrollmentsBySearch(filtered);
        }

        private IEnumerable<StorageNetworkEnrollment> FilterEnrollmentsBySearch(IEnumerable<StorageNetworkEnrollment> enrollments)
        {
            string query = StorageNetworkTextFormatting.NormalizeSearchText(enrollableSearchText);
            if (string.IsNullOrEmpty(query))
            {
                return enrollments;
            }

            return enrollments.Where(enrollment => MatchesEnrollableSearch(enrollment, query));
        }

        private static bool MatchesEnrollableSearch(StorageNetworkEnrollment enrollment, string query)
        {
            if (enrollment == null)
            {
                return false;
            }

            return StorageNetworkTextFormatting.ContainsSearchText(enrollment.gameObject.GetProperName(), query) ||
                   StorageNetworkTextFormatting.ContainsSearchText(StorageNetworkWorldDisplay.GetObjectWorldName(enrollment.gameObject), query) ||
                   StorageNetworkTextFormatting.ContainsSearchText(StorageNetworkPlanCategoryOrder.GetDisplayName(StorageNetworkPlanCategoryOrder.GetCategoryKey(enrollment)), query) ||
                   StorageNetworkTextFormatting.ContainsSearchText(StorageNetworkGeyserText.GetEnrollmentDetails(enrollment), query);
        }

        private void EnsureValidEnrollableWorldFilter(List<StorageNetworkEnrollment> enrollments)
        {
            if (enrollableWorldFilterId == UnsetEnrollableWorldFilterId)
            {
                int initialWorldId = GetActiveWorldFilterId();
                enrollableWorldFilterId = initialWorldId != UnsetEnrollableWorldFilterId ? initialWorldId : AllEnrollableWorldsFilterId;
                return;
            }

            if (enrollableWorldFilterId == AllEnrollableWorldsFilterId)
            {
                return;
            }

            if (GetEnrollableWorldIds(enrollments).Contains(enrollableWorldFilterId))
            {
                return;
            }

            int activeWorldId = GetActiveWorldFilterId();
            enrollableWorldFilterId = activeWorldId != UnsetEnrollableWorldFilterId ? activeWorldId : AllEnrollableWorldsFilterId;
        }

        private static List<int> GetEnrollableWorldIds(IEnumerable<StorageNetworkEnrollment> enrollments)
        {
            HashSet<int> worldIds = new HashSet<int>();
            int activeWorldId = GetActiveWorldFilterId();
            if (StorageNetworkWorldDisplay.IsWorldDiscovered(activeWorldId))
            {
                worldIds.Add(activeWorldId);
            }

            foreach (StorageNetworkEnrollment enrollment in enrollments)
            {
                if (enrollment != null &&
                    TryGetBuildingWorldId(enrollment.gameObject, out int worldId) &&
                    StorageNetworkWorldDisplay.IsWorldDiscovered(worldId))
                {
                    worldIds.Add(worldId);
                }
            }

            return worldIds
                .OrderBy(StorageNetworkWorldDisplay.GetWorldName)
                .ToList();
        }

        private void CreateEnrollableBuildingRow(Transform parent, StorageNetworkEnrollment enrollment)
        {
            bool included = enrollment.IncludedInSceneNetwork;
            GameObject row = CreatePlainImage("EnrollableBuildingRow", parent, included ? new Color(0.71f, 0.78f, 0.70f, 1f) : new Color(0.83f, 0.82f, 0.76f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 38f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 18, 3, 3);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI name = CreateText("Name", row.transform, enrollment.gameObject.GetProperName(), 12, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.12f, 0.13f, 0.12f, 1f);
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI state = CreateText(
                "State",
                row.transform,
                included
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_CONNECTED)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_NOT_CONNECTED),
                11,
                TextAlignmentOptions.MidlineRight);
            state.color = included ? new Color(0.12f, 0.42f, 0.20f, 1f) : new Color(0.58f, 0.38f, 0.20f, 1f);
            state.fontStyle = FontStyles.Bold;
            state.textWrappingMode = TextWrappingModes.NoWrap;
            state.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

            CreateWorldCell(row.transform, enrollment.gameObject);

            Storage storage = enrollment.GetComponent<Storage>();
            TextMeshProUGUI capacity = CreateText(
                "Capacity",
                row.transform,
                storage != null
                    ? string.Format("{0} / {1}", GameUtil.GetFormattedMass(storage.MassStored()), GameUtil.GetFormattedMass(storage.Capacity()))
                    : StorageNetworkGeyserText.GetEnrollmentDetails(enrollment),
                11,
                TextAlignmentOptions.MidlineRight);
            capacity.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            capacity.textWrappingMode = TextWrappingModes.NoWrap;
            capacity.gameObject.AddComponent<LayoutElement>().preferredWidth = storage != null ? 120f : 150f;

            GameObject locateButton = CreateGameButton("LocateButton", row.transform, string.Empty, () => FocusObject(enrollment.gameObject, 500f));
            LayoutElement locateLayout = locateButton.AddComponent<LayoutElement>();
            locateLayout.preferredWidth = 28f;
            locateLayout.preferredHeight = 22f;
            AddButtonIcon(locateButton.transform, "action_follow_cam", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TARGET_FALLBACK));
            ToolTip locateTooltip = locateButton.AddComponent<ToolTip>();
            locateTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.LOCATE_TARGET_TOOLTIP);

            string actionText = included
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLL_REMOVE)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLL_ADD);
            GameObject actionButton = CreateStyledButton(
                "EnrollmentButton",
                row.transform,
                actionText,
                () =>
                {
                    enrollment.SetIncludedInSceneNetwork(!enrollment.IncludedInSceneNetwork);
                    enrollableWindowSignature = null;
                    RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                    UpdateEnrollableBuildingRow(row, enrollment);
                },
                included ? KleiPinkStyle() : KleiBlueStyle());
            LayoutElement actionLayout = actionButton.AddComponent<LayoutElement>();
            actionLayout.preferredWidth = 92f;
            actionLayout.preferredHeight = 22f;
        }

        private void UpdateEnrollableBuildingRow(GameObject row, StorageNetworkEnrollment enrollment)
        {
            if (row == null || enrollment == null)
            {
                ShowEnrollableBuildingsDialog();
                return;
            }

            Transform parent = row.transform.parent;
            int siblingIndex = row.transform.GetSiblingIndex();
            Destroy(row);
            CreateEnrollableBuildingRow(parent, enrollment);
            parent.GetChild(parent.childCount - 1).SetSiblingIndex(siblingIndex);
        }

        private static void CreateEnrollableCategoryHeader(Transform parent, string categoryKey, int count)
        {
            GameObject header = CreatePlainImage("EnrollableCategoryHeader", parent, new Color(0.43f, 0.48f, 0.47f, 1f));
            header.AddComponent<LayoutElement>().preferredHeight = 30f;

            HorizontalLayoutGroup layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 18, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI title = CreateText("CategoryName", header.transform, StorageNetworkPlanCategoryOrder.GetDisplayName(categoryKey), 13, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.96f, 0.91f, 0.78f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI countText = CreateText("CategoryCount", header.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_CATEGORY_COUNT), count), 11, TextAlignmentOptions.MidlineRight);
            countText.color = new Color(0.82f, 0.86f, 0.86f, 1f);
            countText.textWrappingMode = TextWrappingModes.NoWrap;
            countText.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        }

        private static void CreateWorldCell(Transform parent, GameObject gameObject)
        {
            GameObject cell = new GameObject("WorldCell");
            cell.transform.SetParent(parent, false);
            cell.AddComponent<RectTransform>();
            cell.AddComponent<LayoutElement>().preferredWidth = 118f;

            HorizontalLayoutGroup layout = cell.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateWorldIcon(cell.transform, gameObject);

            TextMeshProUGUI world = CreateText(
                "World",
                cell.transform,
                StorageNetworkWorldDisplay.GetObjectWorldName(gameObject),
                11,
                TextAlignmentOptions.MidlineLeft);
            world.color = new Color(0.30f, 0.34f, 0.34f, 1f);
            world.textWrappingMode = TextWrappingModes.NoWrap;
            world.overflowMode = TextOverflowModes.Ellipsis;
            world.gameObject.AddComponent<LayoutElement>().preferredWidth = 88f;
        }

        private static void CreateWorldIcon(Transform parent, GameObject gameObject)
        {
            GameObject iconObject = new GameObject("WorldIcon");
            iconObject.transform.SetParent(parent, false);
            iconObject.AddComponent<RectTransform>();
            LayoutElement layout = iconObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 22f;
            layout.preferredHeight = 22f;

            Image image = iconObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = StorageNetworkWorldDisplay.GetObjectWorldSprite(gameObject);
            image.color = image.sprite != null ? Color.white : Color.clear;
        }

        private static bool TryGetBuildingWorldId(GameObject gameObject, out int worldId)
        {
            worldId = StorageNetworkWorldUtility.GetObjectWorldId(gameObject);
            return worldId != byte.MaxValue && worldId >= 0;
        }

    }
}
