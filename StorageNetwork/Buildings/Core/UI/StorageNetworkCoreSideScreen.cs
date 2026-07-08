using System.Collections.Generic;
using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkCoreSideScreen : SideScreenContent
    {
        private TextMeshProUGUI worldValue;
        private TextMeshProUGUI statusValue;
        private TextMeshProUGUI capacityValue;
        private TextMeshProUGUI remainingValue;
        private TextMeshProUGUI serversValue;
        private TextMeshProUGUI relayValue;
        private TextMeshProUGUI internalBatteryValue;
        private TextMeshProUGUI internalBatteryStateValue;
        private GameObject targetObject;
        private GameObject contentRoot;

        public StorageNetworkCoreSideScreen()
        {
            titleKey = "STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_TITLE";
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            BuildContent();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<StorageNetworkCore>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            base.SetTarget(target);
            targetObject = target;
            Refresh();
        }

        public override void ClearTarget()
        {
            targetObject = null;
            base.ClearTarget();
        }

        private void BuildContent()
        {
            if (contentRoot != null)
            {
                return;
            }

            EnsureRootLayout();
            Transform parent = ContentContainer != null ? ContentContainer.transform : transform;

            contentRoot = new GameObject("StorageNetworkCoreInfo");
            contentRoot.transform.SetParent(parent, false);
            RectTransform rect = contentRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 139f);

            LayoutElement rootLayout = contentRoot.AddComponent<LayoutElement>();
            rootLayout.minHeight = 139f;
            rootLayout.preferredHeight = 145f;
            rootLayout.flexibleWidth = 1f;

            VerticalLayoutGroup layout = contentRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject panel = CreatePanel(contentRoot.transform);
            CreateHeader(panel.transform, out worldValue, out statusValue);
            CreateDivider(panel.transform);
            CreateMetricRow(
                panel.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_STORED_LABEL),
                out capacityValue,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_REMAINING_LABEL),
                out remainingValue);
            CreateMetricRow(
                panel.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_SERVERS_LABEL),
                out serversValue,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_RELAY_LABEL),
                out relayValue);
            CreateMetricRow(
                panel.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_INTERNAL_BATTERY_LABEL),
                out internalBatteryValue,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_POWER_LABEL),
                out internalBatteryStateValue);
        }

        private void EnsureRootLayout()
        {
            RectTransform screenRect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            screenRect.anchorMin = new Vector2(0f, 1f);
            screenRect.anchorMax = new Vector2(1f, 1f);
            screenRect.pivot = new Vector2(0.5f, 1f);
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            LayoutElement screenLayout = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            screenLayout.minHeight = 145f;
            screenLayout.preferredHeight = 151f;
            screenLayout.flexibleWidth = 1f;

            VerticalLayoutGroup screenGroup = gameObject.GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
            screenGroup.childControlWidth = true;
            screenGroup.childControlHeight = true;
            screenGroup.childForceExpandWidth = true;
            screenGroup.childForceExpandHeight = false;
        }

        private void EnsureContentBuilt()
        {
            if (contentRoot == null)
            {
                BuildContent();
            }
        }

        private void Refresh()
        {
            EnsureContentBuilt();
            if (targetObject == null)
            {
                return;
            }

            int worldId = StorageNetworkWorldUtility.GetObjectWorldId(targetObject);
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId, includeReachableWorlds: false);
            StorageNetworkCore core = targetObject.GetComponent<StorageNetworkCore>();
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
            bool relayOnline = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            string worldName = StorageNetworkWorldDisplay.GetWorldName(worldId);
            float remaining = Mathf.Max(0f, snapshot.TotalCapacityKg - snapshot.TotalStoredKg);
            int storageCount = snapshot.Storages.Count(info => info != null && info.Storage != null && info.Minion == null);
            int serverCount = snapshot.Storages.Count(info =>
                info != null &&
                info.Storage != null &&
                info.Minion == null &&
                IsStorageNetworkServer(info.Storage.gameObject));

            SetText(worldValue, worldName);
            SetText(statusValue, online
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_ONLINE)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_OFFLINE));
            SetColor(statusValue, online ? new Color(0.24f, 0.48f, 0.32f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));
            SetText(capacityValue, string.Format("{0} / {1}", GameUtil.GetFormattedMass(snapshot.TotalStoredKg), GameUtil.GetFormattedMass(snapshot.TotalCapacityKg)));
            SetText(remainingValue, GameUtil.GetFormattedMass(remaining));
            SetText(serversValue, string.Format("{0} / {1}", serverCount, storageCount));
            SetText(relayValue, relayOnline
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_RELAY_ONLINE)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_RELAY_OFFLINE));
            SetColor(relayValue, relayOnline ? new Color(0.24f, 0.48f, 0.32f, 1f) : new Color(0.52f, 0.44f, 0.34f, 1f));
            SetText(internalBatteryValue, core != null
                ? string.Format("{0} / {1}",
                    GameUtil.GetFormattedJoules(core.InternalBatteryJoulesAvailable, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(StorageNetworkCore.InternalBatteryCapacityJoules, "F1", GameUtil.TimeSlice.None))
                : string.Empty);
            SetText(internalBatteryStateValue, core?.HasExternalPower == true
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_POWER_EXTERNAL)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_POWER_INTERNAL));
            SetColor(internalBatteryStateValue, core?.HasExternalPower == true
                ? new Color(0.24f, 0.48f, 0.32f, 1f)
                : new Color(0.52f, 0.44f, 0.34f, 1f));
        }

        private static bool IsStorageNetworkServer(GameObject gameObject)
        {
            return gameObject != null && gameObject.GetComponent<StorageNetworkStorageConnector>() != null;
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.SetText(value ?? string.Empty);
            }
        }

        private static void SetColor(TextMeshProUGUI text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private static GameObject CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject("SummaryPanel");
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.93f, 0.92f, 0.86f, 1f);

            LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
            panelLayout.minHeight = 127f;
            panelLayout.preferredHeight = 127f;

            VerticalLayoutGroup panelGroup = panel.AddComponent<VerticalLayoutGroup>();
            panelGroup.padding = new RectOffset(10, 10, 8, 8);
            panelGroup.spacing = 5f;
            panelGroup.childControlWidth = true;
            panelGroup.childControlHeight = true;
            panelGroup.childForceExpandWidth = true;
            panelGroup.childForceExpandHeight = false;
            return panel;
        }

        private static void CreateHeader(Transform parent, out TextMeshProUGUI world, out TextMeshProUGUI status)
        {
            GameObject row = CreateRow(parent, 24f);
            world = CreateText("World", row.transform, 13, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.20f, 0.22f, 0.23f, 1f));
            world.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            status = CreateBadgeText(row.transform);
        }

        private static void CreateMetricRow(Transform parent, string leftLabel, out TextMeshProUGUI leftValue, string rightLabel, out TextMeshProUGUI rightValue)
        {
            GameObject row = CreateRow(parent, 23f);
            CreateMetric(row.transform, leftLabel, out leftValue);
            CreateMetric(row.transform, rightLabel, out rightValue);
        }

        private static void CreateMetric(Transform parent, string label, out TextMeshProUGUI value)
        {
            GameObject cell = new GameObject("Metric");
            cell.transform.SetParent(parent, false);
            cell.AddComponent<RectTransform>();
            cell.AddComponent<LayoutElement>().flexibleWidth = 1f;

            HorizontalLayoutGroup layout = cell.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI labelText = CreateText("Label", cell.transform, 10, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.45f, 0.45f, 0.40f, 1f));
            labelText.SetText(label);
            labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;

            value = CreateText("Value", cell.transform, 11, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.30f, 0.43f, 0.52f, 1f));
            value.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static GameObject CreateRow(Transform parent, float height)
        {
            GameObject row = new GameObject("Row");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = height;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return row;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            divider.AddComponent<RectTransform>();
            Image image = divider.AddComponent<Image>();
            image.color = new Color(0.68f, 0.68f, 0.62f, 1f);
            divider.AddComponent<LayoutElement>().preferredHeight = 1f;
        }

        private static TextMeshProUGUI CreateBadgeText(Transform parent)
        {
            TextMeshProUGUI text = CreateText("Status", parent, 11, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.24f, 0.48f, 0.32f, 1f));
            text.gameObject.AddComponent<LayoutElement>().preferredWidth = 62f;
            return text;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int size, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<RectTransform>();
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }
    }
}
