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
                SaveEnrollableWorldFilter();
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
            int activeWorldId = GetActiveWorldFilterId();
            if (enrollableWorldFilterId == UnsetEnrollableWorldFilterId)
            {
                int savedWorldId = Config.Instance.EnrollableWorldFilterId;
                if (savedWorldId != UnsetEnrollableWorldFilterId &&
                    Config.Instance.EnrollableWorldFilterContextWorldId == activeWorldId)
                {
                    enrollableWorldFilterId = savedWorldId;
                }
                else
                {
                    enrollableWorldFilterId = activeWorldId != UnsetEnrollableWorldFilterId ? activeWorldId : AllEnrollableWorldsFilterId;
                    SaveEnrollableWorldFilter();
                }

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

            enrollableWorldFilterId = activeWorldId != UnsetEnrollableWorldFilterId ? activeWorldId : AllEnrollableWorldsFilterId;
            SaveEnrollableWorldFilter();
        }

        private void SaveEnrollableWorldFilter()
        {
            int activeWorldId = GetActiveWorldFilterId();
            if (Config.Instance.EnrollableWorldFilterId == enrollableWorldFilterId &&
                Config.Instance.EnrollableWorldFilterContextWorldId == activeWorldId)
            {
                return;
            }

            Config.Instance.EnrollableWorldFilterId = enrollableWorldFilterId;
            Config.Instance.EnrollableWorldFilterContextWorldId = activeWorldId;
            Config.Save();
        }

        private static List<int> GetEnrollableWorldIds(IEnumerable<StorageNetworkEnrollment> enrollments)
        {
            if (ClusterManager.Instance != null)
            {
                return ClusterManager.Instance.GetDiscoveredAsteroidIDsSorted()
                    .Where(StorageNetworkWorldDisplay.IsWorldDiscovered)
                    .OrderBy(StorageNetworkWorldDisplay.GetWorldName)
                    .ToList();
            }

            HashSet<int> worldIds = new HashSet<int>();
            int activeWorldId = GetActiveWorldFilterId();
            if (StorageNetworkWorldDisplay.IsWorldDiscovered(activeWorldId))
            {
                worldIds.Add(activeWorldId);
            }

            return worldIds
                .OrderBy(StorageNetworkWorldDisplay.GetWorldName)
                .ToList();
        }
    }
}
