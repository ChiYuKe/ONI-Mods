using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private const int StoredItemVirtualizationThreshold = 32;
        private const int StoredItemVirtualizationOverscan = 4;
        private const float StoredItemRowHeight = 24f;
        private readonly Dictionary<Storage, StorageInfo> liveStorageInfos = new Dictionary<Storage, StorageInfo>();
        private readonly Dictionary<Storage, LiveStorageMetrics> liveStorageMetrics =
            new Dictionary<Storage, LiveStorageMetrics>();
        private StorageSceneSnapshot liveStorageInfoSnapshot;
        private readonly Dictionary<Storage, StorageRowLiveView> liveStorageRows = new Dictionary<Storage, StorageRowLiveView>();
        private readonly Dictionary<string, StorageTypeLiveView> liveStorageTypeRows = new Dictionary<string, StorageTypeLiveView>();
        private readonly Dictionary<Storage, StoredItemSectionLiveView> liveStoredItemSections =
            new Dictionary<Storage, StoredItemSectionLiveView>();
        private readonly Dictionary<StorageItemLiveKey, StoredItemLiveView> liveStoredItemRows = new Dictionary<StorageItemLiveKey, StoredItemLiveView>();
        private readonly List<StorageItemLiveKey> deadStoredItemLiveRows = new List<StorageItemLiveKey>();
        private readonly List<Storage> deadStoredItemSections = new List<Storage>();
        private readonly Dictionary<Storage, PowerStorageLiveView> livePowerStorageRows = new Dictionary<Storage, PowerStorageLiveView>();
        private readonly Dictionary<Storage, PortStorageLiveView> livePowerPortRows = new Dictionary<Storage, PortStorageLiveView>();
        private readonly Dictionary<Storage, PortStorageLiveView> liveParticleRows = new Dictionary<Storage, PortStorageLiveView>();
        private readonly Dictionary<Geyser, GeyserLiveView> liveGeyserRows = new Dictionary<Geyser, GeyserLiveView>();

        private void ClearMainStorageLiveViews()
        {
            liveStorageInfos.Clear();
            liveStorageMetrics.Clear();
            liveStorageInfoSnapshot = null;
            liveStorageRows.Clear();
            liveStorageTypeRows.Clear();
            liveStoredItemSections.Clear();
            liveStoredItemRows.Clear();
            deadStoredItemLiveRows.Clear();
            deadStoredItemSections.Clear();
            livePowerStorageRows.Clear();
            livePowerPortRows.Clear();
            liveParticleRows.Clear();
            liveGeyserRows.Clear();
        }

        private void RegisterStorageTypeLiveView(
            string typeKey,
            IList<StorageInfo> storageInfos,
            TextMeshProUGUI amount,
            TextMeshProUGUI info,
            StorageTypeDisplayKind kind)
        {
            if (string.IsNullOrEmpty(typeKey) || amount == null || storageInfos == null)
            {
                return;
            }

            Storage[] storages = new Storage[storageInfos.Count];
            for (int i = 0; i < storageInfos.Count; i++)
            {
                storages[i] = storageInfos[i]?.Storage;
            }

            liveStorageTypeRows[typeKey] = new StorageTypeLiveView(storages, amount, info, kind);
        }

        private void RegisterStorageLiveView(
            Storage storage,
            TextMeshProUGUI amount,
            TextMeshProUGUI info,
            string onlineInfo)
        {
            if (storage != null && amount != null)
            {
                liveStorageRows[storage] = new StorageRowLiveView(amount, info, onlineInfo);
            }
        }

        private void RegisterStoredItemLiveView(
            Storage storage,
            string itemKey,
            GameObject row,
            TextMeshProUGUI mass,
            TextMeshProUGUI temperature)
        {
            if (storage != null && !string.IsNullOrEmpty(itemKey) && row != null && mass != null)
            {
                liveStoredItemRows[new StorageItemLiveKey(storage, itemKey)] =
                    new StoredItemLiveView(row, mass, temperature);
            }
        }

        private void CreateStoredItemRowsSection(
            Storage storage,
            Transform parent,
            IEnumerable<GameObject> items,
            bool showEmptyWhenNoItems)
        {
            if (storage == null || parent == null)
            {
                return;
            }

            GameObject container = new GameObject("StoredItemRows");
            container.transform.SetParent(parent, false);
            container.AddComponent<RectTransform>();
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            container.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            StoredItemSectionLiveView section = new StoredItemSectionLiveView(
                container.transform,
                showEmptyWhenNoItems);
            liveStoredItemSections[storage] = section;
            PopulateStoredItemSection(section, items);
            ReconcileStoredItemSection(storage, section);
        }

        private static void PopulateStoredItemSection(
            StoredItemSectionLiveView section,
            IEnumerable<GameObject> items)
        {
            section.ClearLiveValues();
            if (items == null)
            {
                return;
            }

            foreach (GameObject item in items)
            {
                if (item == null)
                {
                    continue;
                }

                string itemKey = StorageItemUtility.GetStoredItemKey(item);
                if (!section.Aggregates.TryGetValue(itemKey, out StoredItemAggregate aggregate))
                {
                    section.ActiveKeys.Add(itemKey);
                }

                aggregate.Add(item);
                section.Aggregates[itemKey] = aggregate;
            }
        }

        private static bool PopulateStoredItemSectionFromIndex(
            StoredItemSectionLiveView section,
            IReadOnlyList<Storage> storages)
        {
            section.ClearLiveValues();
            if (!StorageNetworkContentIndexService.TryFillStorageItemTotals(
                    storages,
                    section.IndexedTotals,
                    allowStaleContent: false,
                    out _))
            {
                return false;
            }

            foreach (StorageNetworkIndexedItemTotal indexed in section.IndexedTotals.Values)
            {
                string itemKey = indexed.KeyTag.Name;
                if (string.IsNullOrEmpty(itemKey))
                {
                    continue;
                }

                StoredItemAggregate aggregate = default;
                aggregate.Add(indexed);
                section.ActiveKeys.Add(itemKey);
                section.Aggregates[itemKey] = aggregate;
            }

            return true;
        }

        private GameObject CreateStoredItemEmptyRow(Transform parent)
        {
            TextMeshProUGUI empty = CreateText(
                "Empty",
                parent,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT),
                12,
                TextAlignmentOptions.MidlineLeft);
            empty.color = new Color(0.34f, 0.35f, 0.35f, 1f);
            empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            return empty.gameObject;
        }

        private void ReconcileStoredItemSection(Storage storage, StoredItemSectionLiveView section)
        {
            section.ActiveKeys.Sort(StringComparer.Ordinal);
            section.Rows.Begin();
            if (section.ActiveKeys.Count == 0 && section.ShowEmptyWhenNoItems)
            {
                if (!section.Rows.TryUse(StoredItemSectionLiveView.EmptyRowKey, out _))
                {
                    section.Rows.Use(
                        StoredItemSectionLiveView.EmptyRowKey,
                        () => CreateStoredItemEmptyRow(section.Parent));
                }
            }

            GetStoredItemVisibleRange(
                section,
                out int firstVisible,
                out int lastVisibleExclusive);
            if (firstVisible > 0)
            {
                UseStoredItemSpacer(
                    section,
                    "\0virtual-top",
                    firstVisible * StoredItemRowHeight);
            }

            for (int index = firstVisible; index < lastVisibleExclusive; index++)
            {
                string itemKey = section.ActiveKeys[index];
                StoredItemAggregate aggregate = section.Aggregates[itemKey];
                string rowKey = StoredItemSectionLiveView.GetRowKey(itemKey);
                if (!section.Rows.TryUse(rowKey, out _))
                {
                    section.Rows.Use(
                        rowKey,
                        () => CreateStoredItemRow(
                            storage,
                            section.Parent,
                            itemKey,
                            StorageNetworkStorageDisplay.GetStoredItemName(
                                aggregate.Representative),
                            string.Empty,
                            string.Empty,
                            aggregate.Representative));
                    if (liveStoredItemRows.TryGetValue(
                            new StorageItemLiveKey(storage, itemKey),
                            out StoredItemLiveView createdView))
                    {
                        section.Rows.SetMetadata(rowKey, createdView);
                    }
                }

                if (section.Rows.TryGetMetadata(rowKey, out StoredItemLiveView view))
                {
                    UpdateStoredItemRowLive(view, aggregate);
                }
            }

            int hiddenAfter = section.ActiveKeys.Count - lastVisibleExclusive;
            if (hiddenAfter > 0)
            {
                UseStoredItemSpacer(
                    section,
                    "\0virtual-bottom",
                    hiddenAfter * StoredItemRowHeight);
            }

            section.Rows.Commit();
            section.StructureKeys.Clear();
            section.StructureKeys.AddRange(section.ActiveKeys);
        }

        private void GetStoredItemVisibleRange(
            StoredItemSectionLiveView section,
            out int firstVisible,
            out int lastVisibleExclusive)
        {
            int count = section?.ActiveKeys.Count ?? 0;
            firstVisible = 0;
            lastVisibleExclusive = count;
            RectTransform sectionRect = section?.Parent as RectTransform;
            RectTransform viewport = listScrollRect?.viewport;
            if (count <= StoredItemVirtualizationThreshold ||
                sectionRect == null ||
                viewport == null ||
                !sectionRect.gameObject.activeInHierarchy)
            {
                return;
            }

            Bounds sectionBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    viewport,
                    sectionRect);
            Rect viewportRect = viewport.rect;
            float visibleStart = sectionBounds.max.y - viewportRect.yMax;
            float visibleEnd = sectionBounds.max.y - viewportRect.yMin;
            if (visibleEnd <= 0f)
            {
                lastVisibleExclusive = 0;
                return;
            }

            if (visibleStart >= count * StoredItemRowHeight)
            {
                firstVisible = count;
                lastVisibleExclusive = count;
                return;
            }

            int first = Mathf.Clamp(
                Mathf.FloorToInt(visibleStart / StoredItemRowHeight),
                0,
                count);
            int last = Mathf.Clamp(
                Mathf.CeilToInt(visibleEnd / StoredItemRowHeight),
                first,
                count);
            firstVisible = Mathf.Max(0, first - StoredItemVirtualizationOverscan);
            lastVisibleExclusive = Mathf.Min(
                count,
                last + StoredItemVirtualizationOverscan);
        }

        private static void UseStoredItemSpacer(
            StoredItemSectionLiveView section,
            string key,
            float height)
        {
            GameObject spacer = section.Rows.Use(key, () =>
            {
                GameObject created = new GameObject("StoredItemVirtualSpacer");
                created.transform.SetParent(section.Parent, false);
                created.AddComponent<RectTransform>();
                created.AddComponent<LayoutElement>();
                return created;
            });
            LayoutElement layout = spacer.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = Mathf.Max(0f, height);
            }
        }

        private void ReconcileVisibleStoredItemSections()
        {
            foreach (KeyValuePair<Storage, StoredItemSectionLiveView> pair in
                     liveStoredItemSections)
            {
                StoredItemSectionLiveView section = pair.Value;
                if (pair.Key != null &&
                    section?.Parent != null &&
                    section.Parent.gameObject.activeInHierarchy &&
                    section.ActiveKeys.Count > StoredItemVirtualizationThreshold)
                {
                    ReconcileStoredItemSection(pair.Key, section);
                }
            }
        }

        private void OnMainListScroll(Vector2 _)
        {
            mainListViewportDirty = true;
        }

        private static bool HasStoredItemStructureChanged(StoredItemSectionLiveView section)
        {
            if (section.StructureKeys.Count != section.Aggregates.Count)
            {
                return true;
            }

            for (int index = 0; index < section.StructureKeys.Count; index++)
            {
                if (!section.Aggregates.ContainsKey(section.StructureKeys[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateStoredItemSectionLive(
            Storage storage,
            StoredItemSectionLiveView section)
        {
            foreach (KeyValuePair<string, StoredItemAggregate> pair in section.Aggregates)
            {
                if (liveStoredItemRows.TryGetValue(
                        new StorageItemLiveKey(storage, pair.Key),
                        out StoredItemLiveView view))
                {
                    if (view?.Row != null && view.Row.activeInHierarchy)
                    {
                        UpdateStoredItemRowLive(view, pair.Value);
                    }
                }
            }
        }

        private static void UpdateStoredItemRowLive(
            StoredItemLiveView view,
            StoredItemAggregate aggregate)
        {
            if (view?.Mass == null)
            {
                return;
            }

            int massFingerprint = aggregate.Mass.GetHashCode();
            if (view.LastMassFingerprint != massFingerprint)
            {
                view.LastMassFingerprint = massFingerprint;
                SetTextIfChanged(view.Mass, GameUtil.GetFormattedMass(aggregate.Mass));
            }

            int temperatureFingerprint = aggregate.TryGetTemperature(out float temperature)
                ? temperature.GetHashCode()
                : int.MinValue + 1;
            if (view.LastTemperatureFingerprint != temperatureFingerprint)
            {
                view.LastTemperatureFingerprint = temperatureFingerprint;
                SetTextIfChanged(
                    view.Temperature,
                    temperatureFingerprint == int.MinValue + 1
                        ? string.Empty
                        : GameUtil.GetFormattedTemperature(
                            temperature,
                            GameUtil.TimeSlice.None,
                            GameUtil.TemperatureInterpretation.Absolute,
                            true,
                            false));
            }
        }

        private void RegisterPowerStorageLiveView(StorageNetworkPowerStorage storage, TextMeshProUGUI amount, TextMeshProUGUI details)
        {
            Storage owner = storage != null ? storage.GetComponent<Storage>() : null;
            if (owner != null)
            {
                livePowerStorageRows[owner] = new PowerStorageLiveView(storage, amount, details);
            }
        }

        private void RegisterPowerPortLiveView(Storage storage, TextMeshProUGUI amount, TextMeshProUGUI details)
        {
            if (storage != null)
            {
                livePowerPortRows[storage] = new PortStorageLiveView(amount, details);
            }
        }

        private void RegisterParticleLiveView(Storage storage, TextMeshProUGUI amount, TextMeshProUGUI details)
        {
            if (storage != null)
            {
                liveParticleRows[storage] = new PortStorageLiveView(amount, details);
            }
        }

        private void RegisterGeyserLiveView(Geyser geyser, TextMeshProUGUI amount, TextMeshProUGUI info)
        {
            if (geyser != null && amount != null)
            {
                liveGeyserRows[geyser] = new GeyserLiveView(amount, info);
            }
        }

        private void UpdateMainStorageRowsLive()
        {
            if (currentSnapshot?.Storages == null)
            {
                return;
            }

            foreach (KeyValuePair<Storage, StorageRowLiveView> pair in liveStorageRows)
            {
                if (pair.Key != null && liveStorageInfos.TryGetValue(pair.Key, out StorageInfo storageInfo))
                {
                    LiveStorageMetrics metrics = GetCachedLiveStorageMetrics(storageInfo);
                    int amountFingerprint = metrics.GetDisplayFingerprint(storageInfo);
                    if (pair.Value.LastAmountFingerprint != amountFingerprint)
                    {
                        pair.Value.LastAmountFingerprint = amountFingerprint;
                        SetTextIfChanged(
                            pair.Value.Amount,
                            BuildStorageAmountText(storageInfo, metrics));
                    }
                    bool offline = StorageNetworkStorageRules.IsOfflineNetworkServer(storageInfo);
                    int offlineFingerprint = offline ? 1 : 0;
                    if (pair.Value.Info != null &&
                        pair.Value.LastOfflineFingerprint != offlineFingerprint)
                    {
                        pair.Value.LastOfflineFingerprint = offlineFingerprint;
                        SetTextIfChanged(
                            pair.Value.Info,
                            offline
                                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SERVER_OFFLINE)
                                : pair.Value.OnlineInfo);
                        pair.Value.Info.color = offline
                            ? new Color(0.62f, 0.24f, 0.24f, 1f)
                            : new Color(0.34f, 0.36f, 0.34f, 1f);
                    }
                }
            }

            foreach (StorageTypeLiveView view in liveStorageTypeRows.Values)
            {
                UpdateStorageTypeLiveView(view);
            }

            UpdateStoredItemLiveViews();
            UpdateSpecialStorageLiveViews();
            UpdateGeyserLiveViews();
        }

        private void RefreshMainPanelLiveMetrics()
        {
            liveTotalStoredKg = 0f;
            liveTotalCapacityKg = 0f;
            liveStorageMetrics.Clear();
            if (currentSnapshot?.Storages == null)
            {
                return;
            }

            if (!ReferenceEquals(liveStorageInfoSnapshot, currentSnapshot))
            {
                liveStorageInfoSnapshot = currentSnapshot;
                liveStorageInfos.Clear();
                foreach (StorageInfo storageInfo in currentSnapshot.Storages)
                {
                    Storage storage = storageInfo?.Storage;
                    if (storage != null)
                    {
                        liveStorageInfos[storage] = storageInfo;
                    }
                }
            }

            if (TryGetMainIndexedMetrics(out liveTotalStoredKg, out liveTotalCapacityKg))
            {
                return;
            }

            foreach (StorageInfo storageInfo in currentSnapshot.Storages)
            {
                Storage storage = storageInfo?.Storage;
                if (storage != null &&
                    StorageNetworkStorageRules.CountsTowardNetworkCapacity(storage))
                {
                    GetLiveStorageMetrics(storageInfo, out float storedKg, out float capacityKg);
                    liveStorageMetrics[storage] = new LiveStorageMetrics(storedKg, capacityKg);
                    liveTotalStoredKg += storedKg;
                    liveTotalCapacityKg += capacityKg;
                }
            }
        }

        private bool TryGetMainIndexedMetrics(out float storedKg, out float capacityKg)
        {
            storedKg = 0f;
            capacityKg = 0f;
            if (currentSnapshot == null || !currentSnapshot.NetworkOnline)
            {
                return false;
            }

            int worldId = mainWorldFilterId;
            if (worldId == AllEnrollableWorldsFilterId ||
                worldId == UnsetEnrollableWorldFilterId)
            {
                // The all-world snapshot intentionally excludes undiscovered
                // worlds, while the runtime aggregate includes every online world.
                // Preserve the facade's existing semantics in that mode.
                return false;
            }

            StorageNetworkInventoryMetrics metrics =
                StorageNetworkInventoryIndexService.GetMetrics(
                    worldId,
                    includeRelatedWorlds: false,
                    allowStaleContent: true);
            if (!metrics.NetworkOnline)
            {
                return false;
            }

            storedKg = metrics.TotalStoredKg;
            capacityKg = metrics.TotalCapacityKg;
            return true;
        }

        private LiveStorageMetrics GetCachedLiveStorageMetrics(StorageInfo storageInfo)
        {
            Storage storage = storageInfo?.Storage;
            if (storage != null && liveStorageMetrics.TryGetValue(storage, out LiveStorageMetrics metrics))
            {
                return metrics;
            }

            GetLiveStorageMetrics(storageInfo, out float storedKg, out float capacityKg);
            metrics = new LiveStorageMetrics(storedKg, capacityKg);
            if (storage != null)
            {
                liveStorageMetrics[storage] = metrics;
            }

            return metrics;
        }

        private static void GetLiveStorageMetrics(
            StorageInfo storageInfo,
            out float storedKg,
            out float capacityKg)
        {
            storedKg = 0f;
            capacityKg = 0f;
            Storage owner = storageInfo?.Storage;
            if (owner == null || !StorageNetworkStorageRules.IsConnectedNetworkStorage(owner))
            {
                return;
            }

            bool indexedOwner = StorageNetworkContentIndexService.TryGetStorageMetrics(
                owner,
                allowStaleContent: true,
                out float ownerStoredKg,
                out capacityKg);
            IReadOnlyList<Storage> contentStorages = storageInfo.ContentStorages;
            if (contentStorages == null || contentStorages.Count == 0)
            {
                storedKg = indexedOwner ? ownerStoredKg : owner.MassStored();
                capacityKg = indexedOwner ? capacityKg : owner.Capacity();
                return;
            }

            foreach (Storage contentStorage in contentStorages)
            {
                if (contentStorage == null)
                {
                    continue;
                }

                storedKg += ReferenceEquals(contentStorage, owner) && indexedOwner
                    ? ownerStoredKg
                    : contentStorage.MassStored();
            }

            if (!indexedOwner)
            {
                capacityKg = owner.Capacity();
            }
        }

        private void UpdateStorageTypeLiveView(StorageTypeLiveView view)
        {
            if (view == null || view.Kind == StorageTypeDisplayKind.Geyser)
            {
                return;
            }

            float stored = 0f;
            float capacity = 0f;
            int offline = 0;
            Storage first = null;
            foreach (Storage storage in view.Storages)
            {
                if (storage == null || !liveStorageInfos.TryGetValue(storage, out StorageInfo storageInfo))
                {
                    continue;
                }

                first ??= storage;
                if (StorageNetworkStorageRules.IsOfflineNetworkServer(storageInfo))
                {
                    offline++;
                }

                switch (view.Kind)
                {
                    case StorageTypeDisplayKind.Power:
                        stored += GetDisplayedPowerStoredJoules(storage);
                        capacity += GetDisplayedPowerCapacityJoules(storage);
                        break;
                    case StorageTypeDisplayKind.ParticlePort:
                        if (first == storage)
                        {
                            stored = GetDisplayedParticleStored(storage);
                            capacity = GetDisplayedParticleCapacity(storage);
                        }
                        break;
                    case StorageTypeDisplayKind.ParticleServer:
                        stored += GetDisplayedParticleStored(storage);
                        capacity += GetDisplayedParticleCapacity(storage);
                        break;
                    default:
                        LiveStorageMetrics metrics = GetCachedLiveStorageMetrics(storageInfo);
                        stored += metrics.Stored;
                        capacity += metrics.Capacity;
                        break;
                }
            }

            float percent = capacity > 0f ? stored / capacity : 0f;
            int amountFingerprint = LiveStorageMetrics.CombineFingerprint(
                stored,
                capacity,
                (int)view.Kind);
            if (view.LastAmountFingerprint == amountFingerprint &&
                view.LastOfflineCount == offline)
            {
                return;
            }

            view.LastAmountFingerprint = amountFingerprint;
            view.LastOfflineCount = offline;
            string amount = view.Kind == StorageTypeDisplayKind.Power
                ? string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(capacity, "F1", GameUtil.TimeSlice.None),
                    Mathf.RoundToInt(percent * 100f))
                : view.Kind == StorageTypeDisplayKind.ParticlePort || view.Kind == StorageTypeDisplayKind.ParticleServer
                    ? string.Format("{0} / {1}  {2}%", FormatParticles(stored), FormatParticles(capacity), Mathf.RoundToInt(percent * 100f))
                    : string.Format("{0} / {1}  {2}%", GameUtil.GetFormattedMass(stored), GameUtil.GetFormattedMass(capacity), Mathf.RoundToInt(percent * 100f));
            SetTextIfChanged(view.Amount, amount);
            if (view.Info != null)
            {
                SetTextIfChanged(
                    view.Info,
                    offline > 0
                        ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SERVER_OFFLINE_COUNT), offline)
                        : string.Empty);
                view.Info.color = offline > 0
                    ? new Color(0.62f, 0.24f, 0.24f, 1f)
                    : new Color(0.34f, 0.36f, 0.34f, 1f);
            }
        }

        private void UpdateStoredItemLiveViews()
        {
            deadStoredItemLiveRows.Clear();
            deadStoredItemSections.Clear();
            foreach (StorageInfo storageInfo in currentSnapshot.Storages)
            {
                Storage storage = storageInfo?.Storage;
                if (storage == null ||
                    storageInfo.ContentStorages == null ||
                    !liveStoredItemSections.TryGetValue(storage, out StoredItemSectionLiveView section) ||
                    section?.Parent == null ||
                    !section.Parent.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (PopulateStoredItemSectionFromIndex(
                        section,
                        storageInfo.ContentStorages))
                {
                    continue;
                }

                foreach (Storage contentStorage in storageInfo.ContentStorages)
                {
                    if (contentStorage?.items == null)
                    {
                        continue;
                    }

                    foreach (GameObject item in contentStorage.items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        string itemKey = StorageItemUtility.GetStoredItemKey(item);
                        if (!section.Aggregates.TryGetValue(itemKey, out StoredItemAggregate aggregate))
                        {
                            section.ActiveKeys.Add(itemKey);
                        }

                        aggregate.Add(item);
                        section.Aggregates[itemKey] = aggregate;
                    }
                }
            }

            foreach (KeyValuePair<Storage, StoredItemSectionLiveView> pair in liveStoredItemSections)
            {
                StoredItemSectionLiveView section = pair.Value;
                if (pair.Key == null || section?.Parent == null)
                {
                    deadStoredItemSections.Add(pair.Key);
                    continue;
                }

                if (section.Parent.gameObject.activeInHierarchy)
                {
                    if (HasStoredItemStructureChanged(section))
                    {
                        ReconcileStoredItemSection(pair.Key, section);
                    }
                    else
                    {
                        UpdateStoredItemSectionLive(pair.Key, section);
                    }
                }
            }

            foreach (Storage storage in deadStoredItemSections)
            {
                liveStoredItemSections.Remove(storage);
            }

            foreach (KeyValuePair<StorageItemLiveKey, StoredItemLiveView> pair in liveStoredItemRows)
            {
                if (pair.Value?.Row == null || pair.Value.Mass == null)
                {
                    deadStoredItemLiveRows.Add(pair.Key);
                }
            }

            foreach (StorageItemLiveKey key in deadStoredItemLiveRows)
            {
                liveStoredItemRows.Remove(key);
            }

            deadStoredItemLiveRows.Clear();
            deadStoredItemSections.Clear();
        }

        private void UpdateSpecialStorageLiveViews()
        {
            foreach (PowerStorageLiveView view in livePowerStorageRows.Values)
            {
                if (view?.Storage == null)
                {
                    continue;
                }

                int fingerprint = LiveStorageMetrics.CombineFingerprint(
                    view.Storage.RawJoulesAvailable,
                    view.Storage.CapacityJoules,
                    view.Storage.JoulesLostPerCycle.GetHashCode());
                if (view.LastFingerprint == fingerprint)
                {
                    continue;
                }

                view.LastFingerprint = fingerprint;
                SetTextIfChanged(view.Amount, GameUtil.GetFormattedJoules(view.Storage.RawJoulesAvailable, "F2", GameUtil.TimeSlice.None));
                SetTextIfChanged(
                    view.Details,
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.VIRTUAL_POWER_ITEM_DETAILS),
                        GameUtil.GetFormattedJoules(view.Storage.CapacityJoules, "F1", GameUtil.TimeSlice.None),
                        string.Format(
                            Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TREND_PER_CYCLE),
                            string.Empty,
                            GameUtil.GetFormattedJoules(view.Storage.JoulesLostPerCycle, "F1", GameUtil.TimeSlice.None))));
            }

            foreach (KeyValuePair<Storage, PortStorageLiveView> pair in livePowerPortRows)
            {
                float stored = GetPowerPortStoredJoules(pair.Key);
                float capacity = GetPowerPortCapacityJoules(pair.Key);
                int fingerprint = LiveStorageMetrics.CombineFingerprint(stored, capacity, 1);
                if (pair.Value.LastFingerprint == fingerprint)
                {
                    continue;
                }

                pair.Value.LastFingerprint = fingerprint;
                SetTextIfChanged(pair.Value.Amount, GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None));
                SetTextIfChanged(pair.Value.Details, string.Format(
                    "{0} / {1}",
                    GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(capacity, "F1", GameUtil.TimeSlice.None)));
            }

            foreach (KeyValuePair<Storage, PortStorageLiveView> pair in liveParticleRows)
            {
                float stored = GetDisplayedParticleStored(pair.Key);
                float capacity = GetDisplayedParticleCapacity(pair.Key);
                int fingerprint = LiveStorageMetrics.CombineFingerprint(stored, capacity, 2);
                if (pair.Value.LastFingerprint == fingerprint)
                {
                    continue;
                }

                pair.Value.LastFingerprint = fingerprint;
                SetTextIfChanged(pair.Value.Amount, FormatParticles(stored));
                SetTextIfChanged(pair.Value.Details, string.Format("{0} / {1}", FormatParticles(stored), FormatParticles(capacity)));
            }
        }

        private void UpdateGeyserLiveViews()
        {
            foreach (KeyValuePair<Geyser, GeyserLiveView> pair in liveGeyserRows)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                SetTextIfChanged(pair.Value.Amount, StorageNetworkGeyserText.GetStorageListDetails(pair.Key));
                if (pair.Value.Info != null)
                {
                    bool erupting = IsGeyserErupting(pair.Key);
                    SetTextIfChanged(
                        pair.Value.Info,
                        erupting
                            ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_ERUPTING)
                            : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_NOT_ERUPTING));
                    pair.Value.Info.color = erupting
                        ? new Color(0.28f, 0.48f, 0.34f, 1f)
                        : new Color(0.62f, 0.24f, 0.24f, 1f);
                }
            }
        }

        private static string BuildStorageAmountText(
            StorageInfo storageInfo,
            LiveStorageMetrics metrics)
        {
            Storage storage = storageInfo?.Storage;
            if (storage == null)
            {
                return string.Empty;
            }

            if (StorageNetworkStorageRules.IsOfflineNetworkServer(storageInfo))
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SERVER_OFFLINE);
            }

            bool power = StorageNetworkStorageRules.IsPowerInputPort(storage) ||
                         StorageNetworkStorageRules.IsPowerOutputPort(storage) ||
                         StorageNetworkStorageRules.IsPowerStorageServer(storage);
            bool particle = StorageNetworkStorageRules.IsParticleInputPort(storage) ||
                            StorageNetworkStorageRules.IsParticleOutputPort(storage) ||
                            StorageNetworkStorageRules.IsParticleStorageServer(storage);
            float stored = power
                ? GetDisplayedPowerStoredJoules(storage)
                : particle
                    ? GetDisplayedParticleStored(storage)
                    : metrics.Stored;
            float capacity = power
                ? GetDisplayedPowerCapacityJoules(storage)
                : particle
                    ? GetDisplayedParticleCapacity(storage)
                    : metrics.Capacity;
            float percent = capacity > 0f ? stored / capacity : 0f;
            return power
                ? string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(capacity, "F1", GameUtil.TimeSlice.None),
                    Mathf.RoundToInt(percent * 100f))
                : particle
                    ? string.Format("{0} / {1}  {2}%", FormatParticles(stored), FormatParticles(capacity), Mathf.RoundToInt(percent * 100f))
                    : string.Format("{0} / {1}  {2}%", GameUtil.GetFormattedMass(stored), GameUtil.GetFormattedMass(capacity), Mathf.RoundToInt(percent * 100f));
        }

        private enum StorageTypeDisplayKind
        {
            Mass,
            Power,
            ParticlePort,
            ParticleServer,
            Geyser
        }

        private sealed class StorageRowLiveView
        {
            public StorageRowLiveView(TextMeshProUGUI amount, TextMeshProUGUI info, string onlineInfo)
            {
                Amount = amount;
                Info = info;
                OnlineInfo = onlineInfo ?? string.Empty;
            }

            public TextMeshProUGUI Amount { get; }
            public TextMeshProUGUI Info { get; }
            public string OnlineInfo { get; }
            public int LastAmountFingerprint { get; set; } = int.MinValue;
            public int LastOfflineFingerprint { get; set; } = int.MinValue;
        }

        private sealed class StorageTypeLiveView
        {
            public StorageTypeLiveView(Storage[] storages, TextMeshProUGUI amount, TextMeshProUGUI info, StorageTypeDisplayKind kind)
            {
                Storages = storages;
                Amount = amount;
                Info = info;
                Kind = kind;
            }

            public Storage[] Storages { get; }
            public TextMeshProUGUI Amount { get; }
            public TextMeshProUGUI Info { get; }
            public StorageTypeDisplayKind Kind { get; }
            public int LastAmountFingerprint { get; set; } = int.MinValue;
            public int LastOfflineCount { get; set; } = int.MinValue;
        }

        private sealed class StoredItemSectionLiveView
        {
            public const string EmptyRowKey = "\0storage-network-empty";

            public StoredItemSectionLiveView(Transform parent, bool showEmptyWhenNoItems)
            {
                Parent = parent;
                ShowEmptyWhenNoItems = showEmptyWhenNoItems;
                Rows = new StorageNetworkKeyedRowCache(parent, 32, 120);
            }

            public Transform Parent { get; }
            public bool ShowEmptyWhenNoItems { get; }
            public StorageNetworkKeyedRowCache Rows { get; }
            public Dictionary<string, StoredItemAggregate> Aggregates { get; } =
                new Dictionary<string, StoredItemAggregate>(StringComparer.Ordinal);
            public Dictionary<Tag, StorageNetworkIndexedItemTotal> IndexedTotals { get; } =
                new Dictionary<Tag, StorageNetworkIndexedItemTotal>();
            public List<string> ActiveKeys { get; } = new List<string>();
            public List<string> StructureKeys { get; } = new List<string>();

            public void ClearLiveValues()
            {
                Aggregates.Clear();
                IndexedTotals.Clear();
                ActiveKeys.Clear();
            }

            public static string GetRowKey(string itemKey)
            {
                return itemKey ?? string.Empty;
            }
        }

        private sealed class StoredItemLiveView
        {
            public StoredItemLiveView(GameObject row, TextMeshProUGUI mass, TextMeshProUGUI temperature)
            {
                Row = row;
                Mass = mass;
                Temperature = temperature;
            }

            public GameObject Row { get; }
            public TextMeshProUGUI Mass { get; }
            public TextMeshProUGUI Temperature { get; }
            public int LastMassFingerprint { get; set; } = int.MinValue;
            public int LastTemperatureFingerprint { get; set; } = int.MinValue;
        }

        private readonly struct LiveStorageMetrics
        {
            public LiveStorageMetrics(float stored, float capacity)
            {
                Stored = stored;
                Capacity = capacity;
            }

            public float Stored { get; }
            public float Capacity { get; }

            public int GetDisplayFingerprint(StorageInfo storageInfo)
            {
                Storage storage = storageInfo?.Storage;
                if (storage == null)
                {
                    return 0;
                }

                if (StorageNetworkStorageRules.IsPowerInputPort(storage) ||
                    StorageNetworkStorageRules.IsPowerOutputPort(storage) ||
                    StorageNetworkStorageRules.IsPowerStorageServer(storage))
                {
                    return CombineFingerprint(
                        GetDisplayedPowerStoredJoules(storage),
                        GetDisplayedPowerCapacityJoules(storage),
                        1);
                }

                if (StorageNetworkStorageRules.IsParticleInputPort(storage) ||
                    StorageNetworkStorageRules.IsParticleOutputPort(storage) ||
                    StorageNetworkStorageRules.IsParticleStorageServer(storage))
                {
                    return CombineFingerprint(
                        GetDisplayedParticleStored(storage),
                        GetDisplayedParticleCapacity(storage),
                        2);
                }

                return CombineFingerprint(Stored, Capacity, 0);
            }

            public static int CombineFingerprint(float stored, float capacity, int kind)
            {
                unchecked
                {
                    return ((stored.GetHashCode() * 397) ^ capacity.GetHashCode()) * 397 ^ kind;
                }
            }
        }

        private sealed class PowerStorageLiveView
        {
            public PowerStorageLiveView(StorageNetworkPowerStorage storage, TextMeshProUGUI amount, TextMeshProUGUI details)
            {
                Storage = storage;
                Amount = amount;
                Details = details;
            }

            public StorageNetworkPowerStorage Storage { get; }
            public TextMeshProUGUI Amount { get; }
            public TextMeshProUGUI Details { get; }
            public int LastFingerprint { get; set; } = int.MinValue;
        }

        private sealed class PortStorageLiveView
        {
            public PortStorageLiveView(TextMeshProUGUI amount, TextMeshProUGUI details)
            {
                Amount = amount;
                Details = details;
            }

            public TextMeshProUGUI Amount { get; }
            public TextMeshProUGUI Details { get; }
            public int LastFingerprint { get; set; } = int.MinValue;
        }

        private sealed class GeyserLiveView
        {
            public GeyserLiveView(TextMeshProUGUI amount, TextMeshProUGUI info)
            {
                Amount = amount;
                Info = info;
            }

            public TextMeshProUGUI Amount { get; }
            public TextMeshProUGUI Info { get; }
        }

        private readonly struct StorageItemLiveKey : IEquatable<StorageItemLiveKey>
        {
            public StorageItemLiveKey(Storage storage, string itemKey)
            {
                Storage = storage;
                ItemKey = itemKey ?? string.Empty;
            }

            public Storage Storage { get; }
            public string ItemKey { get; }

            public bool Equals(StorageItemLiveKey other)
            {
                return ReferenceEquals(Storage, other.Storage) &&
                       string.Equals(ItemKey, other.ItemKey, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is StorageItemLiveKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (RuntimeHelpers.GetHashCode(Storage) * 397) ^ StringComparer.Ordinal.GetHashCode(ItemKey);
                }
            }
        }

        private struct StoredItemAggregate
        {
            private float weightedTemperature;
            private float simpleTemperature;
            private float temperatureMass;
            private int temperatureCount;

            public float Mass { get; private set; }
            public GameObject Representative { get; private set; }

            public void Add(GameObject item)
            {
                if (Representative == null)
                {
                    Representative = item;
                }

                float mass = GetStoredItemMass(item);
                Mass += mass;
                PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                if (primaryElement == null)
                {
                    return;
                }

                float primaryMass = Mathf.Max(0f, primaryElement.Mass);
                if (primaryMass > 0f)
                {
                    weightedTemperature += primaryElement.Temperature * primaryMass;
                    temperatureMass += primaryMass;
                }

                simpleTemperature += primaryElement.Temperature;
                temperatureCount++;
            }

            public void Add(StorageNetworkIndexedItemTotal item)
            {
                if (Representative == null)
                {
                    Representative = item.Representative;
                }

                Mass += item.MassKg;
                if (!item.HasTemperature)
                {
                    return;
                }

                float mass = Mathf.Max(0f, item.MassKg);
                if (mass > 0f)
                {
                    weightedTemperature += item.AverageTemperature * mass;
                    temperatureMass += mass;
                }

                simpleTemperature += item.AverageTemperature;
                temperatureCount++;
            }

            public bool TryGetTemperature(out float temperature)
            {
                if (temperatureCount == 0)
                {
                    temperature = 0f;
                    return false;
                }

                temperature = temperatureMass > 0f
                    ? weightedTemperature / temperatureMass
                    : simpleTemperature / temperatureCount;
                return true;
            }
        }
    }
}
