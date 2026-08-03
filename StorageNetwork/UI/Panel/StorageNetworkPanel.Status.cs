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
        private HealthTileView capacityHealthTile;
        private HealthTileView remainingHealthTile;
        private HealthTileView powerStoredHealthTile;
        private HealthTileView powerLeakHealthTile;
        private int lastSummaryFingerprint = int.MinValue;
        private int lastCapacityHealthFingerprint = int.MinValue;
        private int lastPowerHealthFingerprint = int.MinValue;

        private void UpdateStorageSummaryText()
        {
            RebuildMainWorldFilter();
            int fingerprint = CombineLiveFingerprint(
                liveTotalStoredKg,
                liveTotalCapacityKg,
                currentSnapshot.Storages.Count);
            if (lastSummaryFingerprint != fingerprint)
            {
                lastSummaryFingerprint = fingerprint;
                SetTextIfChanged(
                    summaryText,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_TITLE) + "\n" +
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_LINE),
                        currentSnapshot.Storages.Count,
                        GameUtil.GetFormattedMass(liveTotalStoredKg),
                        GameUtil.GetFormattedMass(liveTotalCapacityKg)));
            }
            UpdateNetworkHealthBar();
        }

        private void UpdateNetworkHealthBar()
        {
            if (healthContent == null || currentSnapshot == null)
            {
                return;
            }

            float fillRatio = liveTotalCapacityKg > 0f
                ? liveTotalStoredKg / liveTotalCapacityKg
                : 0f;
            float remainingCapacityKg = Mathf.Max(0f, liveTotalCapacityKg - liveTotalStoredKg);
            int capacityFingerprint = CombineLiveFingerprint(
                liveTotalStoredKg,
                liveTotalCapacityKg,
                0);
            if (lastCapacityHealthFingerprint != capacityFingerprint)
            {
                lastCapacityHealthFingerprint = capacityFingerprint;
                UpdateHealthTile(
                    ref capacityHealthTile,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_CAPACITY),
                    string.Format("{0:P0}", Mathf.Clamp01(fillRatio)),
                    fillRatio >= 0.92f ? DangerColor() : fillRatio >= 0.80f ? WarningColor() : PositiveColor());
                UpdateHealthTile(
                    ref remainingHealthTile,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_REMAINING),
                    GameUtil.GetFormattedMass(remainingCapacityKg),
                    remainingCapacityKg <= 1000f ? WarningColor() : NeutralBlue());
            }
            AddPowerStorageTiles();
            EnsureMainSearchTile();
        }

        private void AddPowerStorageTiles()
        {
            int worldId = GetMainPowerStorageWorldId();
            float capacity = StorageNetworkPowerService.GetCapacityJoules(worldId);
            if (capacity <= 0f)
            {
                SetHealthTileActive(powerStoredHealthTile, false);
                SetHealthTileActive(powerLeakHealthTile, false);
                lastPowerHealthFingerprint = int.MinValue;
                return;
            }

            float stored = StorageNetworkPowerService.GetStoredJoules(worldId);
            float leak = StorageNetworkPowerService.GetJoulesLostPerCycle(worldId);
            int fingerprint = CombineLiveFingerprint(stored, capacity, leak.GetHashCode() ^ worldId);
            if (lastPowerHealthFingerprint == fingerprint)
            {
                return;
            }

            lastPowerHealthFingerprint = fingerprint;
            UpdateHealthTile(
                ref powerStoredHealthTile,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_POWER_STORED),
                string.Format("{0} / {1}",
                    GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(capacity, "F1", GameUtil.TimeSlice.None)),
                stored <= 0f ? WarningColor() : NeutralBlue(),
                112f);
            UpdateHealthTile(
                ref powerLeakHealthTile,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.HEALTH_POWER_LEAK),
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TREND_PER_CYCLE), string.Empty, GameUtil.GetFormattedJoules(leak, "F1", GameUtil.TimeSlice.None)),
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

        private static void SetHealthTileActive(HealthTileView tile, bool active)
        {
            if (tile?.Root != null && tile.Root.activeSelf != active)
            {
                tile.Root.SetActive(active);
            }
        }

        private void UpdateHealthTile(ref HealthTileView tile, string label, string value, Color valueColor, float valueWidth = 58f)
        {
            if (tile == null || tile.Root == null)
            {
                tile = CreateHealthTile(valueWidth);
            }

            SetHealthTileActive(tile, true);
            if (tile.Label.text != label)
            {
                tile.Label.text = label;
            }
            if (tile.Value.text != value)
            {
                tile.Value.text = value;
            }
            if (tile.Value.color != valueColor)
            {
                tile.Value.color = valueColor;
            }
        }

        private HealthTileView CreateHealthTile(float valueWidth)
        {
            GameObject root = CreatePlainImage("HealthTile", healthContent, new Color(0.82f, 0.82f, 0.76f, 1f));
            root.AddComponent<LayoutElement>().flexibleWidth = 1f;
            HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 2, 2);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI label = CreateText("Label", root.transform, string.Empty, 9, TextAlignmentOptions.MidlineLeft);
            label.color = MutedTextColor();
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI amount = CreateText("Value", root.transform, string.Empty, 10, TextAlignmentOptions.MidlineRight);
            amount.fontStyle = FontStyles.Bold;
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = valueWidth;
            return new HealthTileView(root, label, amount);
        }

        private void EnsureMainSearchTile()
        {
            if (healthContent == null)
            {
                return;
            }

            if (mainSearchInput != null)
            {
                return;
            }

            Transform existing = healthContent.Find("MainSearchTile");
            if (existing != null)
            {
                if (existing.GetSiblingIndex() != healthContent.childCount - 1)
                {
                    existing.SetAsLastSibling();
                }
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
                mainSearchDebounce.Request();
            });

            ToolTip tooltip = tile.AddComponent<ToolTip>();
            tooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MAIN_SEARCH_TOOLTIP);
            tile.transform.SetAsLastSibling();
        }

        private static int CombineLiveFingerprint(float first, float second, int discriminator)
        {
            unchecked
            {
                return ((first.GetHashCode() * 397) ^ second.GetHashCode()) * 397 ^
                       discriminator;
            }
        }

        private sealed class HealthTileView
        {
            public HealthTileView(GameObject root, TextMeshProUGUI label, TextMeshProUGUI value)
            {
                Root = root;
                Label = label;
                Value = value;
            }

            public GameObject Root { get; }
            public TextMeshProUGUI Label { get; }
            public TextMeshProUGUI Value { get; }
        }
    }
}
