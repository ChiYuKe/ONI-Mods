using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {
        private static readonly string[] PlanCategoryIds =
        {
            "Base",
            "Oxygen",
            "Power",
            "Food",
            "Plumbing",
            "HVAC",
            "Refining",
            "Medical",
            "Furniture",
            "Equipment",
            "Utilities",
            "Automation",
            "Conveyance",
            "Rocketry",
            "HEP"
        };

        private void ShowEnrollableBuildingsDialog()
        {
            EnsureEnrollableWindow();
            ClearEnrollableWindowContent();
            List<StorageNetworkEnrollment> enrollments = Object
                .FindObjectsByType<StorageNetworkEnrollment>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(enrollment => enrollment != null && enrollment.CanShowInEnrollableList())
                .ToList();

            TextMeshProUGUI header = CreateText("Header", enrollableWindowContent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_HEADER), 14, TextAlignmentOptions.MidlineLeft);
            header.color = new Color(0.34f, 0.39f, 0.38f, 1f);
            header.fontStyle = FontStyles.Normal;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            if (enrollments.Count == 0)
            {
                TextMeshProUGUI empty = CreateText("Empty", enrollableWindowContent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_EMPTY), 12, TextAlignmentOptions.TopLeft);
                empty.color = new Color(0.18f, 0.19f, 0.19f, 1f);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            }
            else
            {
                foreach (IGrouping<string, StorageNetworkEnrollment> categoryGroup in enrollments
                    .GroupBy(GetPlanCategoryKey)
                    .OrderBy(group => GetPlanCategorySortOrder(group.Key))
                    .ThenBy(group => GetPlanCategoryName(group.Key)))
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

            enrollableWindowRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(enrollableWindowContent);
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
            title.rectTransform().offsetMax = new Vector2(-42f, 0f);

            GameObject closeButton = CreateGameButton("CloseButton", header.transform, "X", CloseEnrollableWindow);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-10f, 0f);
            closeRect.sizeDelta = new Vector2(24f, 22f);

            GameObject viewport = CreateBox("Viewport", enrollableWindowRoot.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 10f, 10f, 10f, 58f);
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

            Scrollbar scrollbar = CreateScrollbar(enrollableWindowRoot.transform, 58f, 10f);

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
                    : string.Empty,
                11,
                TextAlignmentOptions.MidlineRight);
            capacity.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            capacity.textWrappingMode = TextWrappingModes.NoWrap;
            capacity.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

            GameObject locateButton = CreateGameButton("LocateButton", row.transform, string.Empty, () => FocusStorage(storage, 500f));
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
                    Refresh(true);
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

            TextMeshProUGUI title = CreateText("CategoryName", header.transform, GetPlanCategoryName(categoryKey), 13, TextAlignmentOptions.MidlineLeft);
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

        private static string GetPlanCategoryKey(StorageNetworkEnrollment enrollment)
        {
            KPrefabID prefabId = enrollment != null ? enrollment.GetComponent<KPrefabID>() : null;
            string buildingId = prefabId != null ? prefabId.PrefabID().ToString() : null;
            if (string.IsNullOrEmpty(buildingId))
            {
                return "Other";
            }

            foreach (PlanScreen.PlanInfo planInfo in global::TUNING.BUILDINGS.PLANORDER)
            {
                if (planInfo.buildingAndSubcategoryData != null &&
                    planInfo.buildingAndSubcategoryData.Any(entry => entry.Key == buildingId))
                {
                    return GetPlanCategoryId(planInfo);
                }
            }

            return "Other";
        }

        private static string GetBuildingWorldName(GameObject gameObject)
        {
            WorldContainer world = GetBuildingWorld(gameObject);
            if (world != null)
            {
                string worldName = world.GetProperName();
                if (!string.IsNullOrEmpty(worldName))
                {
                    return StripKleiLinkFormatting(worldName);
                }
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STARMAP_NAME_HINT);
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
                GetBuildingWorldName(gameObject),
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
            image.sprite = GetBuildingWorldSprite(gameObject);
            image.color = image.sprite != null ? Color.white : Color.clear;
        }

        private static Sprite GetBuildingWorldSprite(GameObject gameObject)
        {
            WorldContainer world = GetBuildingWorld(gameObject);
            ClusterGridEntity clusterEntity = world != null ? world.GetComponent<ClusterGridEntity>() : null;
            Sprite sprite = clusterEntity != null ? clusterEntity.GetUISprite() : null;
            return sprite != null ? sprite : Assets.GetSprite("unknown_far");
        }

        private static WorldContainer GetBuildingWorld(GameObject gameObject)
        {
            if (TryGetBuildingWorldId(gameObject, out int worldId) && ClusterManager.Instance != null)
            {
                return ClusterManager.Instance.GetWorld(worldId);
            }

            return null;
        }

        private static bool TryGetBuildingWorldId(GameObject gameObject, out int worldId)
        {
            worldId = byte.MaxValue;
            if (gameObject == null)
            {
                return false;
            }

            worldId = gameObject.GetMyWorldId();
            if (worldId != byte.MaxValue && worldId >= 0)
            {
                return true;
            }

            int cell = Grid.PosToCell(gameObject);
            if (!Grid.IsValidCell(cell))
            {
                return false;
            }

            worldId = Grid.WorldIdx[cell];
            return worldId != byte.MaxValue && worldId >= 0;
        }

        private static int GetPlanCategorySortOrder(string categoryKey)
        {
            for (int i = 0; i < PlanCategoryIds.Length; i++)
            {
                if (PlanCategoryIds[i] == categoryKey)
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private static string GetPlanCategoryId(PlanScreen.PlanInfo planInfo)
        {
            foreach (string categoryId in PlanCategoryIds)
            {
                if (planInfo.category == new HashedString(categoryId))
                {
                    return categoryId;
                }
            }

            return "Other";
        }

        private static string GetPlanCategoryName(string categoryKey)
        {
            if (string.IsNullOrEmpty(categoryKey) || categoryKey == "Other")
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_CATEGORY_OTHER);
            }

            string key = "STRINGS.UI.BUILDCATEGORIES." + categoryKey.ToUpperInvariant() + ".NAME";
            if (Strings.TryGet(key, out StringEntry entry) && entry != null && !string.IsNullOrEmpty(entry.String))
            {
                return StripKleiLinkFormatting(entry.String);
            }

            return categoryKey;
        }

        private static string StripKleiLinkFormatting(string text)
        {
            return StripKleiTagFormatting(StripKleiTagFormatting(text, "link"), "LINK");
        }

        private static string StripKleiTagFormatting(string text, string tag)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string openTag = "<" + tag + "=";
            string closeTag = "</" + tag + ">";
            while (text.Contains(openTag))
            {
                int closeIndex = text.IndexOf(closeTag);
                if (closeIndex >= 0)
                {
                    text = text.Remove(closeIndex, closeTag.Length);
                }

                int openIndex = text.IndexOf(openTag);
                if (openIndex < 0)
                {
                    break;
                }

                int openEndIndex = text.IndexOf("\">", openIndex);
                if (openEndIndex >= 0)
                {
                    text = text.Remove(openIndex, openEndIndex - openIndex + 2);
                }
                else
                {
                    text = text.Remove(openIndex, openTag.Length);
                }
            }

            return text;
        }
    }
}
