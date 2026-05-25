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
            CloseModal();
            List<StorageNetworkEnrollment> enrollments = Object
                .FindObjectsByType<StorageNetworkEnrollment>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(enrollment => enrollment != null && enrollment.CanShowInEnrollableList())
                .ToList();

            modalRoot = CreateModalFrame(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_TITLE), 760f, 560f, out GameObject body);
            TextMeshProUGUI header = AddModalText(body.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_HEADER), 14, FontStyles.Bold);
            header.color = new Color(0.95f, 0.91f, 0.78f, 1f);

            if (enrollments.Count == 0)
            {
                AddModalText(body.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_EMPTY), 12, FontStyles.Normal);
            }
            else
            {
                RectTransform content = CreateModalScrollList(body.transform, 420f);
                foreach (IGrouping<string, StorageNetworkEnrollment> categoryGroup in enrollments
                    .GroupBy(GetPlanCategoryKey)
                    .OrderBy(group => GetPlanCategorySortOrder(group.Key))
                    .ThenBy(group => GetPlanCategoryName(group.Key)))
                {
                    List<StorageNetworkEnrollment> categoryEnrollments = categoryGroup
                        .OrderBy(enrollment => enrollment.gameObject.GetProperName())
                        .ToList();
                    CreateEnrollableCategoryHeader(content, categoryGroup.Key, categoryEnrollments.Count);

                    foreach (StorageNetworkEnrollment enrollment in categoryEnrollments)
                    {
                        CreateEnrollableBuildingRow(content, enrollment);
                    }
                }
            }

            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CLOSE), 90f, CloseModal);
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
            capacity.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

            GameObject locateButton = CreateGameButton("LocateButton", row.transform, string.Empty, () => FocusStorage(storage));
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
