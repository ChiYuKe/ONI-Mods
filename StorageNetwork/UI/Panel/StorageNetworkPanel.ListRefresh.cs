using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private StorageNetworkKeyedRowCache mainCategoryRows;
        private RectTransform mainCategoryRowsContent;
        private StorageNetworkKeyedRowCache mainStorageRows;
        private RectTransform mainStorageRowsContent;

        private void RebuildStorageListPreservingScroll()
        {
            float scrollOffset = GetListScrollOffset();
            lastListSignature = BuildListSignature(currentSnapshot.Storages);
            RebuildStorageRows(currentSnapshot.Storages);
            RestoreListScrollOffset(scrollOffset);
        }

        private void RebuildStorageRows(IEnumerable<StorageInfo> storages)
        {
            List<StorageInfo> filteredStorages = FilterStorageInfosBySearch(storages).ToList();
            List<StorageNetworkCategoryGroup> groups = BuildCategoryGroups(filteredStorages).ToList();
            EnsureSelectedCategory(groups);
            EnsureMainRowCaches();
            mainCategoryRows.Begin();
            foreach (StorageNetworkCategoryGroup group in groups)
            {
                string key = string.Format(
                    "category:{0}:{1}:{2}",
                    group.Key,
                    group.Key == selectedCategoryKey ? "selected" : "normal",
                    group.Storages.Count);
                mainCategoryRows.Use(key, () => CreateCategoryButton(group));
            }
            mainCategoryRows.Commit();

            mainStorageRows.Begin();
            StorageNetworkCategoryGroup selectedGroup = groups.FirstOrDefault(group => group.Key == selectedCategoryKey);
            if (selectedGroup == null)
            {
                mainStorageRows.Use(
                    "info:no-content",
                    () => CreateMainRowHost(
                        "NoContentHost",
                        parent => CreateInfoRow(
                            Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT),
                            string.Empty,
                            parent)));
                mainStorageRows.Commit();
                RebuildStorageDropAreasFromActiveRows();
                return;
            }

            foreach (IGrouping<string, StorageInfo> group in selectedGroup.Storages.GroupBy(StorageNetworkStorageDisplay.GetTypeKey).OrderBy(group => StorageNetworkStorageDisplay.GetTypeName(group.First())))
            {
                List<StorageInfo> typeStorages = group.ToList();
                string rowKey = BuildMainStorageRowKey(typeStorages);
                if (typeStorages.Count == 1)
                {
                    mainStorageRows.Use(
                        "storage:" + rowKey,
                        () => CreateMainRowHost(
                            "StorageHost",
                            parent => CreateStorageRow(typeStorages[0], parent)));
                }
                else
                {
                    mainStorageRows.Use(
                        "type:" + rowKey,
                        () => CreateMainRowHost(
                            "StorageTypeHost",
                            parent => CreateStorageTypeRow(typeStorages, parent)));
                }
            }

            mainStorageRows.Commit();
            RebuildStorageDropAreasFromActiveRows();
        }

        private void EnsureMainRowCaches()
        {
            if (mainCategoryRows == null || mainCategoryRowsContent != categoryContent)
            {
                DestroyChildren(categoryContent);
                mainCategoryRows = new StorageNetworkKeyedRowCache(categoryContent, 32, 3);
                mainCategoryRowsContent = categoryContent;
            }

            if (mainStorageRows == null || mainStorageRowsContent != listContent)
            {
                ClearMainStorageLiveViews();
                DestroyChildren(listContent);
                // Active roots are reconciled by key. Inactive complex roots are destroyed
                // immediately so their captured callbacks and live bindings cannot go stale.
                mainStorageRows = new StorageNetworkKeyedRowCache(listContent, 0, 0);
                mainStorageRowsContent = listContent;
            }
        }

        private static void DestroyChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private GameObject CreateMainRowHost(string name, System.Action<Transform> build)
        {
            GameObject host = new GameObject(name);
            host.transform.SetParent(listContent, false);
            host.AddComponent<RectTransform>();
            VerticalLayoutGroup layout = host.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            host.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            build(host.transform);
            return host;
        }

        private string BuildMainStorageRowKey(IList<StorageInfo> storages)
        {
            string structuralSignature = StorageNetworkPanelListSignature.BuildStorageListSignature(
                storages,
                string.Empty,
                StorageNetworkStorageDisplay.GetTypeKey,
                StorageItemUtility.GetStoredItemKey);
            string stateSignature = string.Join(",", storages
                .OrderBy(info => info?.Name)
                .Select(info =>
                {
                    Storage storage = info?.Storage;
                    int instanceId = storage != null ? storage.GetInstanceID() : 0;
                    bool expanded = storage != null &&
                                    expandedStorages.TryGetValue(storage, out bool storageExpanded) &&
                                    storageExpanded;
                    bool geyserExpanded = info?.Geyser != null &&
                                          expandedGeysers.TryGetValue(info.Geyser, out bool isGeyserExpanded) &&
                                          isGeyserExpanded;
                    bool selected = storage != null && selectedItemStorage == storage;
                    return string.Format(
                        "{0}:{1}:{2}:{3}:{4}:{5}",
                        instanceId,
                        info?.Name ?? string.Empty,
                        expanded,
                        geyserExpanded,
                        selected,
                        selected ? selectedItemKey ?? string.Empty : string.Empty);
                }));
            string typeKey = storages.Count > 0
                ? StorageNetworkStorageDisplay.GetTypeKey(storages[0])
                : string.Empty;
            bool typeExpanded = expandedStorageTypes.TryGetValue(typeKey, out bool isTypeExpanded) &&
                                isTypeExpanded;
            return string.Format("{0}|{1}|{2}", structuralSignature, typeExpanded, stateSignature);
        }

        private string BuildListSignature(IEnumerable<StorageInfo> storages)
        {
            return StorageNetworkPanelListSignature.BuildStorageListSignature(
                FilterStorageInfosBySearch(storages),
                mainSearchText,
                StorageNetworkStorageDisplay.GetTypeKey,
                StorageItemUtility.GetStoredItemKey);
        }

        private void RebuildLayout()
        {
            if (listContent == null)
            {
                return;
            }

            RequestMainLayout(listContent);
        }

        private float GetListScrollOffset()
        {
            return listContent != null ? Mathf.Max(0f, listContent.anchoredPosition.y) : 0f;
        }

        private void RestoreListScrollOffset(float scrollOffset)
        {
            if (listScrollRect == null || listContent == null)
            {
                return;
            }

            deferredMainScrollOffset = scrollOffset;
            restoreMainScrollAfterLayout = true;
            RequestMainLayout(listContent);
        }

        private void RequestMainLayout(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            if (deferredMainLayoutRoot == null ||
                deferredMainLayoutRoot.IsChildOf(root))
            {
                deferredMainLayoutRoot = root;
            }

            deferredMainLayoutFrame = deferredMainLayoutFrame < 0
                ? Time.frameCount + 1
                : Mathf.Min(deferredMainLayoutFrame, Time.frameCount + 1);
        }

        private void RunDeferredMainLayout()
        {
            if (deferredMainLayoutFrame < 0 ||
                Time.frameCount < deferredMainLayoutFrame)
            {
                return;
            }

            RectTransform root = deferredMainLayoutRoot;
            deferredMainLayoutRoot = null;
            deferredMainLayoutFrame = -1;
            if (root == null || !root.gameObject.activeInHierarchy)
            {
                restoreMainScrollAfterLayout = false;
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Layout);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            mainListViewportDirty = true;
            if (!restoreMainScrollAfterLayout ||
                listScrollRect == null ||
                listContent == null)
            {
                return;
            }

            restoreMainScrollAfterLayout = false;
            listScrollRect.StopMovement();
            float viewportHeight = listScrollRect.viewport != null
                ? listScrollRect.viewport.rect.height
                : 0f;
            float maxOffset = Mathf.Max(0f, listContent.rect.height - viewportHeight);
            Vector2 position = listContent.anchoredPosition;
            position.y = Mathf.Clamp(deferredMainScrollOffset, 0f, maxOffset);
            listContent.anchoredPosition = position;
        }

        private void ClearList()
        {
            ClearMainStorageLiveViews();
            mainStorageRows = null;
            mainStorageRowsContent = null;
            if (listContent == null)
            {
                return;
            }

            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = listContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void ClearCategories()
        {
            mainCategoryRows = null;
            mainCategoryRowsContent = null;
            if (categoryContent == null)
            {
                return;
            }

            for (int i = categoryContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = categoryContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
