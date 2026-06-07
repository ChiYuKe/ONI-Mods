using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private StorageSceneSnapshot CollectMainSnapshot(bool force)
        {
            if (mainWorldFilterId == AllEnrollableWorldsFilterId)
            {
                return FilterSnapshotToDiscoveredWorlds(StorageSceneCollector.Collect(force));
            }

            return StorageSceneCollector.CollectForWorld(mainWorldFilterId, includeReachableWorlds: false);
        }

        private static StorageSceneSnapshot FilterSnapshotToDiscoveredWorlds(StorageSceneSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Storages == null)
            {
                return snapshot;
            }

            List<StorageInfo> storages = snapshot.Storages
                .Where(info => info != null && StorageNetworkWorldDisplay.IsWorldDiscovered(StorageNetworkWorldUtility.GetObjectWorldId(info.GameObject)))
                .ToList();
            float totalStoredKg = 0f;
            float totalCapacityKg = 0f;
            foreach (StorageInfo info in storages)
            {
                totalStoredKg += info.StoredKg;
                totalCapacityKg += info.CapacityKg;
            }

            return new StorageSceneSnapshot(storages, totalStoredKg, totalCapacityKg, snapshot.NetworkOnline);
        }

        private void CreateMainWorldFilter(Transform parent)
        {
            GameObject filter = new GameObject("MainWorldFilter");
            filter.transform.SetParent(parent, false);
            mainWorldFilterContent = filter.AddComponent<RectTransform>();
            mainWorldFilterContent.anchorMin = new Vector2(1f, 1f);
            mainWorldFilterContent.anchorMax = new Vector2(1f, 1f);
            mainWorldFilterContent.pivot = new Vector2(1f, 1f);
            mainWorldFilterContent.anchoredPosition = new Vector2(-74f, -14f);
            mainWorldFilterContent.sizeDelta = new Vector2(150f, 26f);
        }

        private void RebuildMainWorldFilter()
        {
            if (mainWorldFilterContent == null)
            {
                return;
            }

            for (int i = mainWorldFilterContent.childCount - 1; i >= 0; i--)
            {
                Destroy(mainWorldFilterContent.GetChild(i).gameObject);
            }

            GameObject dropdownButton = CreateStyledButton(
                "MainWorldFilterDropdown",
                mainWorldFilterContent,
                GetSelectedMainWorldFilterText(),
                ToggleMainWorldDropdown,
                CreateColorStyle(
                    new Color(0.17f, 0.19f, 0.25f, 1f),
                    new Color(0.25f, 0.28f, 0.35f, 1f),
                    new Color(0.11f, 0.12f, 0.16f, 1f)));
            RectTransform rect = dropdownButton.GetComponent<RectTransform>();
            Stretch(rect, 0f, 0f);
            SetButtonLabelColor(dropdownButton, new Color(0.92f, 0.93f, 0.90f, 1f), FontStyles.Normal);
            AddDropdownArrowIcon(dropdownButton.transform);
        }

        private void ToggleMainWorldDropdown()
        {
            if (mainWorldDropdownRoot != null)
            {
                CloseMainWorldDropdown();
                return;
            }

            ShowMainWorldDropdown();
        }

        private void ShowMainWorldDropdown()
        {
            if (windowRect == null)
            {
                return;
            }

            CloseMainWorldDropdown();
            List<int> worldIds = GetMainWorldIds();
            int optionCount = worldIds.Count + 1;
            float height = Mathf.Min(20f + optionCount * 30f, 250f);

            mainWorldDropdownRoot = CreatePlainImage("MainWorldFilterDropdownPanel", windowRect, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            mainWorldDropdownRoot.AddComponent<ScrollWheelBlocker>();
            ApplyThinBoxSprite(mainWorldDropdownRoot.GetComponent<Image>());
            RectTransform dropdownRect = mainWorldDropdownRoot.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(1f, 1f);
            dropdownRect.anchorMax = new Vector2(1f, 1f);
            dropdownRect.pivot = new Vector2(1f, 1f);
            dropdownRect.anchoredPosition = new Vector2(-94f, -94f);
            dropdownRect.sizeDelta = new Vector2(144f, height);

            GameObject viewport = CreatePlainImage("Viewport", mainWorldDropdownRoot.transform, new Color(0.73f, 0.73f, 0.67f, 1f));
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

            CreateMainWorldDropdownOption(content.transform, AllEnrollableWorldsFilterId, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_WORLD_ALL));
            foreach (int worldId in worldIds)
            {
                CreateMainWorldDropdownOption(content.transform, worldId, StorageNetworkWorldDisplay.GetWorldName(worldId));
            }

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            ConfigureSmoothVerticalScroll(scrollRect, 22f);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void CreateMainWorldDropdownOption(Transform parent, int worldId, string text)
        {
            bool selected = mainWorldFilterId == worldId;
            GameObject row = CreateStyledButton("MainWorldFilterOption", parent, text, () =>
            {
                mainWorldFilterId = worldId;
                CloseMainWorldDropdown();
                selectedItemStorage = null;
                selectedItemKey = null;
                lastListSignature = null;
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
            }, selected ? KleiPinkStyle() : CreateColorStyle(
                new Color(0.80f, 0.80f, 0.73f, 1f),
                new Color(0.87f, 0.87f, 0.80f, 1f),
                new Color(0.67f, 0.68f, 0.62f, 1f)));
            SetButtonLabelColor(row, selected ? Color.white : new Color(0.23f, 0.26f, 0.26f, 1f), FontStyles.Bold);
            AddWorldFilterOptionIcon(row.transform, worldId);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 26f;
        }

        private void CloseMainWorldDropdown()
        {
            if (mainWorldDropdownRoot != null)
            {
                Destroy(mainWorldDropdownRoot);
                mainWorldDropdownRoot = null;
            }
        }

        private string GetSelectedMainWorldFilterText()
        {
            return mainWorldFilterId == AllEnrollableWorldsFilterId
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_WORLD_ALL)
                : StorageNetworkWorldDisplay.GetWorldName(mainWorldFilterId);
        }

        private void EnsureValidMainWorldFilter()
        {
            if (mainWorldFilterId == UnsetEnrollableWorldFilterId)
            {
                int activeWorldId = GetActiveWorldFilterId();
                mainWorldFilterId = activeWorldId != UnsetEnrollableWorldFilterId ? activeWorldId : AllEnrollableWorldsFilterId;
                return;
            }

            if (mainWorldFilterId == AllEnrollableWorldsFilterId || GetMainWorldIds().Contains(mainWorldFilterId))
            {
                return;
            }

            int fallbackWorldId = GetActiveWorldFilterId();
            mainWorldFilterId = fallbackWorldId != UnsetEnrollableWorldFilterId ? fallbackWorldId : AllEnrollableWorldsFilterId;
        }

        private List<int> GetMainWorldIds()
        {
            HashSet<int> worldIds = new HashSet<int>();
            int activeWorldId = GetActiveWorldFilterId();
            if (StorageNetworkWorldDisplay.IsWorldDiscovered(activeWorldId))
            {
                worldIds.Add(activeWorldId);
            }

            StorageSceneRegistry.EnsureSceneSeeded();
            foreach (Storage storage in StorageSceneRegistry.GetStorages())
            {
                if (storage != null)
                {
                    int worldId = StorageNetworkWorldUtility.GetObjectWorldId(storage.gameObject);
                    if (StorageNetworkWorldDisplay.IsWorldDiscovered(worldId))
                    {
                        worldIds.Add(worldId);
                    }
                }
            }

            return worldIds.OrderBy(StorageNetworkWorldDisplay.GetWorldName).ToList();
        }

        private static int GetActiveWorldFilterId()
        {
            return ClusterManager.Instance != null ? ClusterManager.Instance.activeWorldId : UnsetEnrollableWorldFilterId;
        }

    }
}
