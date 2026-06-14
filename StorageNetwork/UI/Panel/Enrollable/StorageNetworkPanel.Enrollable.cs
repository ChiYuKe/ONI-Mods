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

    }
}
