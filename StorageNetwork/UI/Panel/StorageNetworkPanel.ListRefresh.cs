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
        private void RebuildStorageListPreservingScroll()
        {
            float scrollOffset = GetListScrollOffset();
            lastListSignature = BuildListSignature(currentSnapshot.Storages);
            RebuildStorageRows(currentSnapshot.Storages);
            RestoreListScrollOffset(scrollOffset);
        }

        private void RebuildStorageRows(IEnumerable<StorageInfo> storages)
        {
            ClearStorageDropAreas();
            ClearCategories();
            ClearList();

            List<StorageInfo> filteredStorages = FilterStorageInfosBySearch(storages).ToList();
            List<StorageNetworkCategoryGroup> groups = BuildCategoryGroups(filteredStorages).ToList();
            EnsureSelectedCategory(groups);
            foreach (StorageNetworkCategoryGroup group in groups)
            {
                CreateCategoryButton(group);
            }

            StorageNetworkCategoryGroup selectedGroup = groups.FirstOrDefault(group => group.Key == selectedCategoryKey);
            if (selectedGroup == null)
            {
                CreateInfoRow(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT), string.Empty);
                RebuildLayout();
                return;
            }

            foreach (IGrouping<string, StorageInfo> group in selectedGroup.Storages.GroupBy(StorageNetworkStorageDisplay.GetTypeKey).OrderBy(group => StorageNetworkStorageDisplay.GetTypeName(group.First())))
            {
                List<StorageInfo> typeStorages = group.ToList();
                if (typeStorages.Count == 1)
                {
                    CreateStorageRow(typeStorages[0], listContent);
                }
                else
                {
                    CreateStorageTypeRow(typeStorages);
                }
            }

            RebuildLayout();
        }

        private static string BuildListSignature(IEnumerable<StorageInfo> storages)
        {
            return StorageNetworkPanelListSignature.BuildStorageListSignature(
                storages,
                instance != null ? instance.mainSearchText : string.Empty,
                StorageNetworkStorageDisplay.GetTypeKey,
                StorageItemUtility.GetStoredItemKey,
                StorageNetworkStorageRules.IsOfflineNetworkServer);
        }

        private void RebuildLayout()
        {
            if (listContent == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
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

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
            listScrollRect.StopMovement();
            float viewportHeight = listScrollRect.viewport != null ? listScrollRect.viewport.rect.height : 0f;
            float maxOffset = Mathf.Max(0f, listContent.rect.height - viewportHeight);
            Vector2 position = listContent.anchoredPosition;
            position.y = Mathf.Clamp(scrollOffset, 0f, maxOffset);
            listContent.anchoredPosition = position;
        }

        private void ClearList()
        {
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
