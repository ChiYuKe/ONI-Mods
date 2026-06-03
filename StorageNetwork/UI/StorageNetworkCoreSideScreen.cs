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
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI capacityText;
        private TextMeshProUGUI buildingsText;
        private TextMeshProUGUI relayText;
        private GameObject targetObject;

        public StorageNetworkCoreSideScreen()
        {
            titleKey = StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_TITLE;
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
            Transform parent = ContentContainer != null ? ContentContainer.transform : transform;

            GameObject root = new GameObject("StorageNetworkCoreInfo");
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 118f);

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            statusText = CreateLine(root.transform, new Color(0.22f, 0.24f, 0.25f, 1f), FontStyles.Bold);
            capacityText = CreateLine(root.transform, new Color(0.22f, 0.24f, 0.25f, 1f), FontStyles.Normal);
            buildingsText = CreateLine(root.transform, new Color(0.22f, 0.24f, 0.25f, 1f), FontStyles.Normal);
            relayText = CreateLine(root.transform, new Color(0.42f, 0.25f, 0.36f, 1f), FontStyles.Normal);
        }

        private void Refresh()
        {
            if (targetObject == null)
            {
                return;
            }

            int worldId = StorageNetworkWorldUtility.GetObjectWorldId(targetObject);
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId, includeReachableWorlds: false);
            bool online = StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
            bool relayOnline = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            string worldName = GetWorldName(worldId);
            float remaining = Mathf.Max(0f, snapshot.TotalCapacityKg - snapshot.TotalStoredKg);
            int storageCount = snapshot.Storages.Count(info => info != null && info.Storage != null && info.Minion == null);
            int serverCount = snapshot.Storages.Count(info =>
                info != null &&
                info.Storage != null &&
                info.Minion == null &&
                IsStorageNetworkServer(info.Storage.gameObject));

            SetText(statusText, string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_STATUS),
                worldName,
                online
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_ONLINE)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_OFFLINE)));
            SetText(capacityText, string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_CAPACITY),
                GameUtil.GetFormattedMass(snapshot.TotalStoredKg),
                GameUtil.GetFormattedMass(snapshot.TotalCapacityKg),
                GameUtil.GetFormattedMass(remaining)));
            SetText(buildingsText, string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_BUILDINGS),
                serverCount,
                storageCount));
            SetText(relayText, string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_RELAY),
                relayOnline
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_RELAY_ONLINE)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CORE_SIDE_SCREEN_RELAY_OFFLINE)));
        }

        private static bool IsStorageNetworkServer(GameObject gameObject)
        {
            return gameObject != null && gameObject.GetComponent<StorageNetworkStorageConnector>() != null;
        }

        private static string GetWorldName(int worldId)
        {
            WorldContainer world = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(worldId) : null;
            if (world != null)
            {
                string name = world.GetProperName();
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STARMAP_NAME_HINT);
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static TextMeshProUGUI CreateLine(Transform parent, Color color, FontStyles style)
        {
            GameObject textObject = new GameObject("Line");
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 13f;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            return text;
        }
    }
}
