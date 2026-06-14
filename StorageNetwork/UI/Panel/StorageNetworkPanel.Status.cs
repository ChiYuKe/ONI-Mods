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
        private void UpdateStorageSummaryText()
        {
            RebuildMainWorldFilter();
            summaryText.text =
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_TITLE) + "\n" +
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_LINE),
                    currentSnapshot.Storages.Count,
                    GameUtil.GetFormattedMass(currentSnapshot.TotalStoredKg),
                    GameUtil.GetFormattedMass(currentSnapshot.TotalCapacityKg));
            UpdateNetworkHealthBar();
        }

        private void UpdateNetworkHealthBar()
        {
            if (healthContent == null || currentSnapshot == null)
            {
                return;
            }

            ClearHealthBar();
            StorageNetworkPanelHealthMetrics metrics = StorageNetworkPanelHealthMetrics.Create(
                currentSnapshot,
                productionOrderService.Orders,
                StorageNetworkStorageRules.IsOfflineNetworkServer);

            AddHealthTile(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_CAPACITY), string.Format("{0:P0}", Mathf.Clamp01(metrics.FillRatio)), metrics.FillRatio >= 0.92f ? DangerColor() : metrics.FillRatio >= 0.80f ? WarningColor() : PositiveColor());
            AddHealthTile(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_REMAINING), GameUtil.GetFormattedMass(metrics.RemainingCapacityKg), metrics.RemainingCapacityKg <= 1000f ? WarningColor() : NeutralBlue());
            AddPowerStorageTiles();
            EnsureMainSearchTile();
        }

        private void AddPowerStorageTiles()
        {
            int worldId = GetMainPowerStorageWorldId();
            float capacity = StorageNetworkPowerService.GetCapacityJoules(worldId);
            if (capacity <= 0f)
            {
                return;
            }

            float stored = StorageNetworkPowerService.GetStoredJoules(worldId);
            float leak = StorageNetworkPowerService.GetJoulesLostPerCycle(worldId);
            AddHealthTile(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_POWER_STORED),
                string.Format("{0} / {1}",
                    GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(capacity, "F1", GameUtil.TimeSlice.None)),
                stored <= 0f ? WarningColor() : NeutralBlue(),
                112f);
            AddHealthTile(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_POWER_LEAK),
                GameUtil.GetFormattedJoules(leak, "F1", GameUtil.TimeSlice.None) + "/周期",
                leak > 0f ? WarningColor() : PositiveColor(),
                78f);
        }

        private int GetMainPowerStorageWorldId()
        {
            if (mainWorldFilterId == AllEnrollableWorldsFilterId && StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                return -1;
            }

            int worldId = mainWorldFilterId;
            if (worldId == AllEnrollableWorldsFilterId || worldId == UnsetEnrollableWorldFilterId)
            {
                worldId = GetActiveWorldFilterId();
            }

            return worldId;
        }

        private void ClearHealthBar()
        {
            for (int i = healthContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = healthContent.GetChild(i).gameObject;
                if (child.name == "MainSearchTile")
                {
                    continue;
                }

                Destroy(child);
            }
        }

        private void AddHealthTile(string label, string value, Color valueColor, float valueWidth = 58f)
        {
            GameObject tile = CreatePlainImage("HealthTile", healthContent, new Color(0.82f, 0.82f, 0.76f, 1f));
            tile.AddComponent<LayoutElement>().flexibleWidth = 1f;
            HorizontalLayoutGroup layout = tile.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 2, 2);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI name = CreateText("Label", tile.transform, label, 9, TextAlignmentOptions.MidlineLeft);
            name.color = MutedTextColor();
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI amount = CreateText("Value", tile.transform, value, 10, TextAlignmentOptions.MidlineRight);
            amount.color = valueColor;
            amount.fontStyle = FontStyles.Bold;
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = valueWidth;
        }

        private void EnsureMainSearchTile()
        {
            if (healthContent == null)
            {
                return;
            }

            Transform existing = healthContent.Find("MainSearchTile");
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return;
            }

            GameObject tile = CreatePlainImage("MainSearchTile", healthContent, new Color(0.82f, 0.82f, 0.76f, 1f));
            LayoutElement tileLayout = tile.AddComponent<LayoutElement>();
            tileLayout.minWidth = 150f;
            tileLayout.preferredWidth = 170f;
            tileLayout.flexibleWidth = 1f;

            HorizontalLayoutGroup layout = tile.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 3, 3);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            mainSearchInput = CreateFixedTextInput(
                tile.transform,
                "MainSearchInput",
                mainSearchText,
                154f,
                22f,
                10);
            mainSearchInput.onValueChanged.AddListener(value =>
            {
                mainSearchText = value ?? string.Empty;
                selectedItemStorage = null;
                selectedItemKey = null;
                lastListSignature = null;
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
            });

            ToolTip tooltip = tile.AddComponent<ToolTip>();
            tooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MAIN_SEARCH_TOOLTIP);
            tile.transform.SetAsLastSibling();
        }
    }
}
