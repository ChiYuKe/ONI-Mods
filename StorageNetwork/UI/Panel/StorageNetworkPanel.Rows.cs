using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
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
        // Deprecated: plain storage settings currently do not provide useful controls.
        // Keep this switch so the old entry point can be restored deliberately if needed.
        private const bool ShowDeprecatedStorageSettingsButton = false;


        private void CreateStorageTypeRow(List<StorageInfo> storages)
        {
            if (storages == null || storages.Count == 0)
            {
                return;
            }

            string typeKey = StorageNetworkStorageDisplay.GetTypeKey(storages[0]);
            string typeName = StorageNetworkStorageDisplay.GetTypeName(storages[0]);
            Sprite typeIcon = StorageNetworkStorageDisplay.GetTypeIcon(storages[0], out Color typeIconTint);
            bool expanded = expandedStorageTypes.TryGetValue(typeKey, out bool isExpanded) && isExpanded;
            bool isGeyserGroup = storages[0].Geyser != null;
            bool isPowerPortGroup = storages.All(storage => storage?.Storage != null &&
                (StorageNetworkStorageRules.IsPowerInputPort(storage.Storage) ||
                 StorageNetworkStorageRules.IsPowerOutputPort(storage.Storage)));
            bool isPowerStorageGroup = storages.All(storage => storage?.Storage != null &&
                StorageNetworkStorageRules.IsPowerStorageServer(storage.Storage));
            bool isParticlePortGroup = storages.All(storage => storage?.Storage != null &&
                (StorageNetworkStorageRules.IsParticleInputPort(storage.Storage) ||
                 StorageNetworkStorageRules.IsParticleOutputPort(storage.Storage)));
            bool isParticleServerGroup = storages.All(storage => storage?.Storage != null &&
                StorageNetworkStorageRules.IsParticleStorageServer(storage.Storage));
            int offlineServerCount = storages.Count(StorageNetworkStorageRules.IsOfflineNetworkServer);
            float storedKg = storages.Sum(storage => storage.StoredKg);
            float capacityKg = storages.Sum(storage => storage.CapacityKg);
            float powerStored = isPowerStorageGroup
                ? storages.Sum(info => GetDisplayedPowerStoredJoules(info.Storage))
                : isPowerPortGroup ? storages.Sum(info => GetDisplayedPowerStoredJoules(info.Storage)) : 0f;
            float powerCapacity = isPowerStorageGroup
                ? storages.Sum(info => GetDisplayedPowerCapacityJoules(info.Storage))
                : isPowerPortGroup ? storages.Sum(info => GetDisplayedPowerCapacityJoules(info.Storage)) : 0f;
            Storage particleSample = isParticlePortGroup
                ? storages.Select(info => info.Storage).FirstOrDefault(storage => storage != null)
                : null;
            float particleStored = isParticleServerGroup
                ? storages.Sum(info => GetDisplayedParticleStored(info.Storage))
                : isParticlePortGroup ? GetDisplayedParticleStored(particleSample) : 0f;
            float particleCapacity = isParticleServerGroup
                ? storages.Sum(info => GetDisplayedParticleCapacity(info.Storage))
                : isParticlePortGroup ? GetDisplayedParticleCapacity(particleSample) : 0f;
            float percent = isParticlePortGroup || isParticleServerGroup
                ? particleCapacity > 0f ? particleStored / particleCapacity : 0f
                : isPowerPortGroup || isPowerStorageGroup
                    ? powerCapacity > 0f ? powerStored / powerCapacity : 0f
                : capacityKg > 0f ? storedKg / capacityKg : 0f;
            string groupInfo = offlineServerCount > 0
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SERVER_OFFLINE_COUNT), offlineServerCount)
                : null;

            GameObject row = CreateBox("StorageTypeRow", listContent, new Color(0.86f, 0.85f, 0.80f, 1f));
            AddVerticalContainer(row, 0f, 0, 0, 0, 0);

            GameObject header = CreateFoldoutHeader(
                row.transform,
                expanded,
                typeName,
                isGeyserGroup
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_COUNT), storages.Count)
                    : isPowerPortGroup || isPowerStorageGroup
                    ? string.Format("{0} / {1}  {2}%",
                        GameUtil.GetFormattedJoules(powerStored, "F1", GameUtil.TimeSlice.None),
                        GameUtil.GetFormattedJoules(powerCapacity, "F1", GameUtil.TimeSlice.None),
                        Mathf.RoundToInt(percent * 100f))
                    : isParticlePortGroup || isParticleServerGroup
                    ? string.Format("{0} / {1}  {2}%",
                        FormatParticles(particleStored),
                        FormatParticles(particleCapacity),
                        Mathf.RoundToInt(percent * 100f))
                    : string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedMass(storedKg),
                    GameUtil.GetFormattedMass(capacityKg),
                    Mathf.RoundToInt(percent * 100f)),
                new Color(0.66f, 0.67f, 0.62f, 1f),
                14,
                300f,
                () =>
                {
                    expandedStorageTypes[typeKey] = !expanded;
                    RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                },
                groupInfo,
                null,
                null,
                offlineServerCount > 0 ? new Color(0.62f, 0.24f, 0.24f, 1f) : (Color?)null,
                typeIcon,
                typeIconTint);

            header.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 0, 0);

            if (!expanded)
            {
                row.AddComponent<LayoutElement>().preferredHeight = 34f;
                return;
            }

            GameObject storageList = CreateBox("Storages", row.transform, new Color(0.80f, 0.80f, 0.75f, 1f));
            AddVerticalContainer(storageList, 4f, 18, 0, 4, 4);
            storageList.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (StorageInfo storage in storages.OrderBy(storage => storage.Name))
            {
                CreateStorageRow(storage, storageList.transform);
            }

            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void CreateStorageRow(StorageInfo storageInfo, Transform parent)
        {
            if (storageInfo != null && storageInfo.Geyser != null)
            {
                CreateGeyserRow(storageInfo, parent);
                return;
            }

            Storage storage = storageInfo.Storage;
            if (storage == null)
            {
                return;
            }

            bool expanded = expandedStorages.TryGetValue(storage, out bool isExpanded) && isExpanded;
            bool selected = selectedItemStorage == storage && string.IsNullOrEmpty(selectedItemKey);
            float percent = storageInfo.CapacityKg > 0f ? storageInfo.StoredKg / storageInfo.CapacityKg : 0f;
            StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            bool powerPort = StorageNetworkStorageRules.IsPowerInputPort(storage) ||
                             StorageNetworkStorageRules.IsPowerOutputPort(storage);
            bool powerStorageServer = StorageNetworkStorageRules.IsPowerStorageServer(storage);
            bool particlePort = StorageNetworkStorageRules.IsParticleInputPort(storage) ||
                                StorageNetworkStorageRules.IsParticleOutputPort(storage);
            bool particleServer = StorageNetworkStorageRules.IsParticleStorageServer(storage);
            bool showSettingsButton = StorageNetworkStorageRules.IsProductionStorage(storage, enrollment) ||
                                      StorageNetworkStorageRules.IsConfigurablePort(storage) ||
                                      StorageNetworkStorageRules.HasSettingsButtonTag(storage) ||
                                      ShowDeprecatedStorageSettingsButton;
            bool showSourceModName = StorageNetworkStorageRules.HasModStorageTag(storage) &&
                !StorageNetworkStorageRules.IsServerStorage(storage) &&
                !StorageNetworkStorageRules.IsNetworkPortStorage(storage);
            string sourceModName = showSourceModName
                ? StorageNetworkModInfoResolver.GetSourceModName(storage)
                : null;
            bool serverOffline = StorageNetworkStorageRules.IsOfflineNetworkServer(storageInfo);
            float displayedStored = powerPort
                ? GetDisplayedPowerStoredJoules(storage)
                : powerStorageServer ? GetDisplayedPowerStoredJoules(storage)
                : particlePort || particleServer ? GetDisplayedParticleStored(storage) : storageInfo.StoredKg;
            float displayedCapacity = powerPort
                ? GetDisplayedPowerCapacityJoules(storage)
                : powerStorageServer ? GetDisplayedPowerCapacityJoules(storage)
                : particlePort || particleServer ? GetDisplayedParticleCapacity(storage) : storageInfo.CapacityKg;
            percent = displayedCapacity > 0f ? displayedStored / displayedCapacity : 0f;
            string amountText = serverOffline
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SERVER_OFFLINE)
                : powerPort || powerStorageServer
                ? string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedJoules(displayedStored, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(displayedCapacity, "F1", GameUtil.TimeSlice.None),
                    Mathf.RoundToInt(percent * 100f))
                : particlePort || particleServer
                ? string.Format("{0} / {1}  {2}%",
                    FormatParticles(displayedStored),
                    FormatParticles(displayedCapacity),
                    Mathf.RoundToInt(percent * 100f))
                : string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedMass(storageInfo.StoredKg),
                    GameUtil.GetFormattedMass(storageInfo.CapacityKg),
                    Mathf.RoundToInt(percent * 100f));

            GameObject row = CreateBox(
                "StorageRow",
                parent,
                selected ? new Color(0.72f, 0.77f, 0.80f, 1f) : new Color(0.88f, 0.87f, 0.82f, 1f));
            AddVerticalContainer(row, 0f, 0, 0, 0, 0);

            CreateFoldoutHeader(
                row.transform,
                expanded,
                storageInfo.Name,
                amountText,
                new Color(0.72f, 0.72f, 0.68f, 1f),
                13,
                210f,
                () =>
                {
                    expandedStorages[storage] = !expanded;
                    RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                },
                serverOffline
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SERVER_OFFLINE)
                    : string.IsNullOrEmpty(sourceModName) ? null : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SOURCE_MOD_NAME), sourceModName),
                showSettingsButton ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_SETTINGS) : null,
                showSettingsButton ? () => ShowStorageSettingsDialog(storage) : null,
                serverOffline ? new Color(0.62f, 0.24f, 0.24f, 1f) : (Color?)null);

            RegisterStorageDropTarget(row, storage);

            if (!expanded)
            {
                row.AddComponent<LayoutElement>().preferredHeight = 34f;
                return;
            }

            GameObject details = CreateBox("Details", row.transform, new Color(0.82f, 0.82f, 0.77f, 1f));
            VerticalLayoutGroup detailsLayout = details.AddComponent<VerticalLayoutGroup>();
            detailsLayout.padding = new RectOffset(12, 12, 8, 8);
            detailsLayout.spacing = 3f;
            detailsLayout.childControlHeight = true;
            detailsLayout.childControlWidth = true;
            detailsLayout.childForceExpandHeight = false;
            detailsLayout.childForceExpandWidth = true;

            StorageNetworkPowerStorage powerStorage = storage.GetComponent<StorageNetworkPowerStorage>();
            bool powerPortDetails = StorageNetworkStorageRules.IsPowerInputPort(storage) ||
                                    StorageNetworkStorageRules.IsPowerOutputPort(storage);
            bool particlePortDetails = StorageNetworkStorageRules.IsParticleInputPort(storage) ||
                                       StorageNetworkStorageRules.IsParticleOutputPort(storage);
            bool particleServerDetails = StorageNetworkStorageRules.IsParticleStorageServer(storage);
            List<GameObject> items = storageInfo.StoredItems.ToList();
            if (items.Count == 0 && powerStorage == null && !powerPortDetails && !particlePortDetails && !particleServerDetails)
            {
                TextMeshProUGUI empty = CreateText("Empty", details.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT), 12, TextAlignmentOptions.MidlineLeft);
                empty.color = new Color(0.34f, 0.35f, 0.35f, 1f);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            }
            else
            {
                if (powerPortDetails)
                {
                    CreatePowerPortBatteryRow(details.transform, storage);
                }

                if (particlePortDetails || particleServerDetails)
                {
                    CreateParticlePortStorageRow(details.transform, storage);
                }

                if (powerStorage != null)
                {
                    CreateVirtualPowerItemRow(details.transform, powerStorage);
                }

                foreach (IGrouping<string, GameObject> group in items.GroupBy(StorageItemUtility.GetStoredItemKey).OrderBy(group => StorageNetworkStorageDisplay.GetStoredItemName(group.FirstOrDefault())))
                {
                    float mass = group.Sum(GetStoredItemMass);
                    CreateStoredItemRow(
                        storage,
                        details.transform,
                        group.Key,
                        StorageNetworkStorageDisplay.GetStoredItemName(group.FirstOrDefault()),
                        GameUtil.GetFormattedMass(mass),
                        FormatStoredItemTemperature(group),
                        group.FirstOrDefault());
                }
            }

            ContentSizeFitter fitter = details.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void CreateInfoRow(string title, string details)
        {
            GameObject row = CreateBox("InfoRow", listContent, new Color(0.88f, 0.87f, 0.82f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 42f;
            TextMeshProUGUI text = CreateText("Text", row.transform, string.IsNullOrEmpty(details) ? title : title + "\n" + details, 13, TextAlignmentOptions.MidlineLeft);
            text.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            Stretch(text.rectTransform(), 12f, 6f);
            RebuildLayout();
        }

        private static float GetDisplayedParticleStored(Storage storage)
        {
            HighEnergyParticleStorage particleStorage = storage != null ? storage.GetComponent<HighEnergyParticleStorage>() : null;
            return particleStorage != null
                ? Mathf.Max(0f, particleStorage.Particles)
                : StorageNetworkParticleStorageService.GetAvailable(storage != null ? storage.gameObject : null);
        }

        private static float GetDisplayedParticleCapacity(Storage storage)
        {
            HighEnergyParticleStorage particleStorage = storage != null ? storage.GetComponent<HighEnergyParticleStorage>() : null;
            return particleStorage != null
                ? Mathf.Max(0f, particleStorage.Capacity())
                : StorageNetworkParticleStorageService.GetCapacity(storage != null ? storage.gameObject : null);
        }

        private static float GetDisplayedPowerStoredJoules(Storage storage)
        {
            StorageNetworkPowerStorage powerStorage = storage != null ? storage.GetComponent<StorageNetworkPowerStorage>() : null;
            return powerStorage != null ? powerStorage.RawJoulesAvailable : GetPowerPortStoredJoules(storage);
        }

        private static float GetDisplayedPowerCapacityJoules(Storage storage)
        {
            StorageNetworkPowerStorage powerStorage = storage != null ? storage.GetComponent<StorageNetworkPowerStorage>() : null;
            return powerStorage != null ? powerStorage.CapacityJoules : GetPowerPortCapacityJoules(storage);
        }

        private void CreateCategoryButton(StorageNetworkCategoryGroup group)
        {
            if (categoryContent == null || group == null)
            {
                return;
            }

            bool selected = group.Key == selectedCategoryKey;
            GameObject button = CreateStyledButton(
                "CategoryButton",
                categoryContent,
                string.Empty,
                () =>
                {
                    selectedCategoryKey = group.Key;
                    selectedItemStorage = null;
                    selectedItemKey = null;
                    RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                    UpdateCategorySummaryPanel();
                },
                selected ? KleiPinkStyle() : KleiBlueStyle());
            button.AddComponent<LayoutElement>().preferredHeight = 48f;
            RegisterCategoryDropTarget(button, group.Key);

            TextMeshProUGUI label = CreateText(
                "CategoryLabel",
                button.transform,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CATEGORY_COUNT), group.Name, group.Storages.Count),
                12,
                TextAlignmentOptions.Left);
            label.color = Color.white;
            label.richText = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.lineSpacing = -4f;
            Stretch(label.rectTransform(), 8f, 5f);
        }

    }
}

