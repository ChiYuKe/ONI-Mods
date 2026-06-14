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
