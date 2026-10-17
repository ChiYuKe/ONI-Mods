using System;
using StorageNetwork.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private readonly StorageNetworkFrameWorkQueue mainRowBuildQueue = new StorageNetworkFrameWorkQueue();
        private float? pendingMainBuildScrollOffset;
        private bool deferredInitialPanelRefresh;

        private void QueueMainRowBuild(Transform owner, System.Action build, Func<bool> ready = null)
        {
            mainRowBuildQueue.Enqueue(build, () => owner != null,
                () => owner.gameObject.activeInHierarchy && (ready == null || ready()));
        }

        private void RunMainRowBuildQueue()
        {
            if (windowRect == null || !windowRect.gameObject.activeInHierarchy || mainRowBuildQueue.Count == 0) return;
            using (StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.MainPanelBuild))
            {
                if (mainRowBuildQueue.Run(2, 0.75d) > 0)
                    RebuildStorageDropAreasFromActiveRows();
                if (mainRowBuildQueue.Count == 0 && pendingMainBuildScrollOffset.HasValue)
                {
                    float offset = pendingMainBuildScrollOffset.Value;
                    pendingMainBuildScrollOffset = null;
                    RestoreListScrollOffset(offset);
                }
            }
        }

        private GameObject CreateDeferredItemRow(Storage storage, StoredItemSectionLiveView section,
            string rowKey, string itemKey, string displayName, GameObject representative)
        {
            GameObject host = new GameObject("DeferredItemRow");
            host.transform.SetParent(section.Parent, false);
            host.AddComponent<RectTransform>();
            host.AddComponent<LayoutElement>().preferredHeight = StoredItemRowHeight;
            VerticalLayoutGroup layout = host.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            QueueMainRowBuild(host.transform, () =>
            {
                if (storage == null || !section.Aggregates.TryGetValue(itemKey, out StoredItemAggregate aggregate)) return;
                CreateStoredItemRow(storage, host.transform, itemKey, displayName,
                    string.Empty, string.Empty, aggregate.Representative);
                if (liveStoredItemRows.TryGetValue(new StorageItemLiveKey(storage, itemKey), out StoredItemLiveView view))
                {
                    section.Rows.SetMetadata(rowKey, view);
                    UpdateStoredItemRowLive(view, aggregate);
                }
            }, () => storage != null && section.Aggregates.ContainsKey(itemKey));
            return host;
        }
    }
}
