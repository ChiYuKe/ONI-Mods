using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
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
        // Deprecated: plain storage settings currently do not provide useful controls.
        // Keep this switch so the old entry point can be restored deliberately if needed.
        private const bool ShowDeprecatedStorageSettingsButton = false;


        private void CreateStorageTypeRow(List<StorageInfo> storages)
        {
            if (storages == null || storages.Count == 0)
            {
                return;
            }

            string typeKey = GetStorageTypeKey(storages[0]);
            string typeName = GetStorageTypeName(storages[0]);
            bool expanded = expandedStorageTypes.TryGetValue(typeKey, out bool isExpanded) && isExpanded;
            bool isGeyserGroup = storages[0].Geyser != null;
            bool isMinionGroup = storages[0].Minion != null;
            float storedKg = storages.Sum(storage => storage.StoredKg);
            float capacityKg = storages.Sum(storage => storage.CapacityKg);
            float percent = capacityKg > 0f ? storedKg / capacityKg : 0f;

            GameObject row = CreateBox("StorageTypeRow", listContent, new Color(0.86f, 0.85f, 0.80f, 1f));
            AddVerticalContainer(row, 0f, 0, 0, 0, 0);

            GameObject header = CreateFoldoutHeader(
                row.transform,
                expanded,
                string.Format("{0}  x{1}", typeName, storages.Count),
                isGeyserGroup
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_COUNT), storages.Count)
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
                null,
                isMinionGroup ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_SETTINGS) : null,
                isMinionGroup ? () => ShowAllMinionSettingsDialog(storages) : null);

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
            bool isMinionStorage = storageInfo.Minion != null;
            bool showSettingsButton = StorageNetworkStorageRules.IsProductionStorage(storage, enrollment) ||
                                      StorageNetworkStorageRules.HasSettingsButtonTag(storage) ||
                                      isMinionStorage ||
                                      ShowDeprecatedStorageSettingsButton;
            string sourceModName = StorageNetworkStorageRules.HasModStorageTag(storage)
                ? StorageNetworkModInfoResolver.GetSourceModName(storage)
                : null;

            GameObject row = CreateBox(
                "StorageRow",
                parent,
                selected ? new Color(0.72f, 0.77f, 0.80f, 1f) : new Color(0.88f, 0.87f, 0.82f, 1f));
            AddVerticalContainer(row, 0f, 0, 0, 0, 0);

            CreateFoldoutHeader(
                row.transform,
                expanded,
                storageInfo.Name,
                string.Format("{0} / {1}  {2}%",
                    GameUtil.GetFormattedMass(storageInfo.StoredKg),
                    GameUtil.GetFormattedMass(storageInfo.CapacityKg),
                    Mathf.RoundToInt(percent * 100f)),
                new Color(0.72f, 0.72f, 0.68f, 1f),
                13,
                210f,
                () =>
                {
                    expandedStorages[storage] = !expanded;
                    RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                },
                string.IsNullOrEmpty(sourceModName) ? null : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SOURCE_MOD_NAME), sourceModName),
                showSettingsButton ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_SETTINGS) : null,
                showSettingsButton ? () =>
                {
                    if (isMinionStorage)
                    {
                        ShowMinionSettingsPanel(storageInfo.Minion, storage);
                    }
                    else
                    {
                        ShowStorageSettingsDialog(storage);
                    }
                } : null);

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

            List<GameObject> items = storageInfo.StoredItems.ToList();
            if (items.Count == 0)
            {
                TextMeshProUGUI empty = CreateText("Empty", details.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT), 12, TextAlignmentOptions.MidlineLeft);
                empty.color = new Color(0.34f, 0.35f, 0.35f, 1f);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            }
            else
            {
                foreach (IGrouping<string, GameObject> group in items.GroupBy(GetStoredItemKey).OrderBy(group => GetStoredItemName(group.FirstOrDefault())))
                {
                    float mass = group.Sum(GetStoredItemMass);
                    CreateStoredItemRow(
                        storage,
                        details.transform,
                        group.Key,
                        GetStoredItemName(group.FirstOrDefault()),
                        GameUtil.GetFormattedMass(mass),
                        FormatStoredItemTemperature(group),
                        group.FirstOrDefault());
                }
            }

            ContentSizeFitter fitter = details.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void CreateGeyserRow(StorageInfo storageInfo, Transform parent)
        {
            Geyser geyser = storageInfo.Geyser;
            if (geyser == null)
            {
                return;
            }

            bool expanded = expandedGeysers.TryGetValue(geyser, out bool isExpanded) && isExpanded;
            GameObject row = CreateBox("GeyserRow", parent, new Color(0.88f, 0.87f, 0.82f, 1f));
            AddVerticalContainer(row, 0f, 0, 0, 0, 0);

            string details = GetGeyserDetails(geyser);
            bool erupting = IsGeyserErupting(geyser);
            CreateFoldoutHeader(
                row.transform,
                expanded,
                storageInfo.Name,
                details,
                new Color(0.72f, 0.72f, 0.68f, 1f),
                13,
                320f,
                () =>
                {
                    expandedGeysers[geyser] = !expanded;
                    RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                },
                erupting
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_ERUPTING)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_NOT_ERUPTING),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_SETTINGS),
                () => ShowGeyserSettingsDialog(geyser),
                erupting ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            if (!expanded)
            {
                row.AddComponent<LayoutElement>().preferredHeight = 34f;
                return;
            }

            AddGeyserDescriptorDetails(row.transform, geyser);
            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static bool IsGeyserErupting(Geyser geyser)
        {
            ElementEmitter emitter = geyser != null ? geyser.GetComponent<ElementEmitter>() : null;
            return emitter != null && emitter.IsSimActive;
        }

        private void AddGeyserDescriptorDetails(Transform parent, Geyser geyser)
        {
            GameObject details = CreateBox("GeyserDetails", parent, new Color(0.82f, 0.82f, 0.77f, 1f));
            VerticalLayoutGroup layout = details.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 2f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            foreach (Descriptor descriptor in geyser.GetDescriptors(geyser.gameObject).Where(descriptor => descriptor.type == Descriptor.DescriptorType.Effect))
            {
                TextMeshProUGUI text = CreateText("GeyserDescriptor", details.transform, StripKleiLinkFormatting(descriptor.text), 11, TextAlignmentOptions.MidlineLeft);
                text.color = new Color(0.22f, 0.23f, 0.22f, 1f);
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Overflow;
                text.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            }

            details.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static string GetGeyserDetails(Geyser geyser)
        {
            if (geyser == null || geyser.configuration == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_ANALYZED);
            }

            Element element = ElementLoader.FindElementByHash(geyser.configuration.GetElement());
            string elementName = element != null ? element.name : geyser.configuration.GetElement().CreateTag().ProperName();
            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_OUTPUT),
                StripKleiLinkFormatting(elementName),
                GameUtil.GetFormattedMass(geyser.configuration.GetAverageEmission(), GameUtil.TimeSlice.PerSecond, GameUtil.MetricMassFormat.UseThreshold, true, "{0:0.#}"));
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

        private void CreateStoredItemRow(Storage storage, Transform parent, string itemKey, string itemName, string formattedMass, string formattedTemperature, GameObject representative)
        {
            GameObject row = new GameObject("ItemRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 24f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            bool selected = selectedItemStorage == storage && selectedItemKey == itemKey;
            KImage background = row.AddComponent<KImage>();
            background.color = selected ? new Color(0.62f, 0.67f, 0.70f, 1f) : Color.clear;
            background.colorStyleSetting = CreateColorStyle(
                background.color,
                new Color(0.68f, 0.72f, 0.74f, 1f),
                new Color(0.54f, 0.59f, 0.62f, 1f));

            KButton rowButton = row.AddComponent<KButton>();
            rowButton.bgImage = background;
            rowButton.additionalKImages = new KImage[0];
            rowButton.soundPlayer = new ButtonSoundPlayer();
            rowButton.onClick += () =>
            {
                selectedItemStorage = storage;
                selectedItemKey = itemKey;
                LogDebug(string.Format(
                    "Select item row storage={0} itemKey={1} itemName={2} mass={3}",
                    storage != null ? storage.gameObject.GetProperName() : "null",
                    itemKey,
                    itemName,
                    formattedMass));
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
            };

            RegisterItemDragSource(row, storage, itemKey, itemName, representative);

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>();
            iconObject.AddComponent<LayoutElement>().preferredWidth = 22f;

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            SetStoredItemIcon(icon, representative);

            TextMeshProUGUI itemText = CreateText(
                "Text",
                row.transform,
                itemName,
                12,
                TextAlignmentOptions.MidlineLeft);
            itemText.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            itemText.textWrappingMode = TextWrappingModes.NoWrap;
            itemText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement itemTextLayout = itemText.gameObject.AddComponent<LayoutElement>();
            itemTextLayout.preferredWidth = 85f;
            itemTextLayout.flexibleWidth = 0f;

            TextMeshProUGUI massText = CreateText("Mass", row.transform, formattedMass, 12, TextAlignmentOptions.MidlineLeft);
            massText.color = new Color(0.30f, 0.31f, 0.31f, 1f);
            massText.textWrappingMode = TextWrappingModes.NoWrap;
            massText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

            TextMeshProUGUI temperatureText = CreateText("Temperature", row.transform, formattedTemperature, 12, TextAlignmentOptions.MidlineLeft);
            temperatureText.color = new Color(0.36f, 0.38f, 0.38f, 1f);
            temperatureText.textWrappingMode = TextWrappingModes.NoWrap;
            temperatureText.gameObject.AddComponent<LayoutElement>().preferredWidth = 262f;

            if (selected)
            {
                GameObject transferButton = CreateGameButton("TransferButton", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER), () => ShowTransferDialog(storage, itemKey));
                LayoutElement transferLayout = transferButton.AddComponent<LayoutElement>();
                transferLayout.preferredWidth = 58f;
                transferLayout.preferredHeight = 20f;
                ToolTip transferTooltip = transferButton.AddComponent<ToolTip>();
                transferTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER_TOOLTIP);

                GameObject dropButton = CreateGameButton("DropButton", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.DROP), () => ShowDropDialog(storage, itemKey));
                LayoutElement dropLayout = dropButton.AddComponent<LayoutElement>();
                dropLayout.preferredWidth = 58f;
                dropLayout.preferredHeight = 20f;
                ToolTip tooltip = dropButton.AddComponent<ToolTip>();
                tooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.DROP_TOOLTIP);
            }
        }

        private static string FormatStoredItemTemperature(IEnumerable<GameObject> items)
        {
            float weightedTemperature = 0f;
            float totalMass = 0f;
            float simpleTemperature = 0f;
            int temperatureCount = 0;

            foreach (GameObject item in items)
            {
                PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                if (primaryElement == null)
                {
                    continue;
                }

                float mass = Mathf.Max(0f, primaryElement.Mass);
                if (mass > 0f)
                {
                    weightedTemperature += primaryElement.Temperature * mass;
                    totalMass += mass;
                }

                simpleTemperature += primaryElement.Temperature;
                temperatureCount++;
            }

            if (temperatureCount == 0)
            {
                return string.Empty;
            }

            float temperature = totalMass > 0f ? weightedTemperature / totalMass : simpleTemperature / temperatureCount;
            return GameUtil.GetFormattedTemperature(
                temperature,
                GameUtil.TimeSlice.None,
                GameUtil.TemperatureInterpretation.Absolute,
                true,
                false);
        }

        private void CreateTargetSelectionRow(Transform parent, Storage source, Storage target, bool selected, System.Action onClick)
        {
            GameObject row = CreateStyledButton("TargetButton", parent, string.Empty, onClick, selected ? KleiPinkStyle() : KleiBlueStyle());
            row.AddComponent<LayoutElement>().preferredHeight = 32f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI name = CreateText("Name", row.transform, target.GetProperName(), 12, TextAlignmentOptions.MidlineLeft);
            name.color = Color.white;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI remaining = CreateText(
                "Remaining",
                row.transform,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REMAINING_CAPACITY), GameUtil.GetFormattedMass(Mathf.Max(0f, target.RemainingCapacity()))),
                11,
                TextAlignmentOptions.MidlineRight);
            remaining.color = new Color(0.88f, 0.90f, 0.92f, 1f);
            remaining.textWrappingMode = TextWrappingModes.NoWrap;
            remaining.gameObject.AddComponent<LayoutElement>().preferredWidth = 115f;

            GameObject sourceLocateButton = CreateGameButton("LocateSourceButton", row.transform, string.Empty, () => FocusStorage(source));
            LayoutElement sourceLocateLayout = sourceLocateButton.AddComponent<LayoutElement>();
            sourceLocateLayout.preferredWidth = 28f;
            sourceLocateLayout.preferredHeight = 22f;
            AddButtonIcon(sourceLocateButton.transform, "action_follow_cam", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SOURCE_FALLBACK));
            ToolTip sourceTooltip = sourceLocateButton.AddComponent<ToolTip>();
            sourceTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.LOCATE_SOURCE_TOOLTIP);

            GameObject targetLocateButton = CreateGameButton("LocateTargetButton", row.transform, string.Empty, () => FocusStorage(target));
            LayoutElement targetLocateLayout = targetLocateButton.AddComponent<LayoutElement>();
            targetLocateLayout.preferredWidth = 28f;
            targetLocateLayout.preferredHeight = 22f;
            AddButtonIcon(targetLocateButton.transform, "action_follow_cam", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TARGET_FALLBACK));
            ToolTip targetTooltip = targetLocateButton.AddComponent<ToolTip>();
            targetTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.LOCATE_TARGET_TOOLTIP);

            TextMeshProUGUI capacity = CreateText(
                "Capacity",
                row.transform,
                string.Format("{0} / {1}", GameUtil.GetFormattedMass(target.MassStored()), GameUtil.GetFormattedMass(target.Capacity())),
                11,
                TextAlignmentOptions.MidlineRight);
            capacity.color = new Color(0.78f, 0.82f, 0.85f, 1f);
            capacity.textWrappingMode = TextWrappingModes.NoWrap;
            capacity.gameObject.AddComponent<LayoutElement>().preferredWidth = 125f;
        }

        private static void CreateFoldoutIcon(Transform parent, bool expanded)
        {
            GameObject iconObject = new GameObject("FoldoutIcon");
            iconObject.transform.SetParent(parent, false);
            iconObject.AddComponent<RectTransform>();
            LayoutElement layout = iconObject.AddComponent<LayoutElement>();
            layout.minWidth = 18f;
            layout.preferredWidth = 18f;
            layout.minHeight = 18f;
            layout.preferredHeight = 18f;

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            Sprite sprite = GetSpriteByName(expanded ? "iconDown" : "iconRight");
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.type = Image.Type.Simple;
                icon.color = new Color(0.28f, 0.30f, 0.30f, 0.72f);
                return;
            }

            UnityEngine.Object.DestroyImmediate(icon);
            TextMeshProUGUI arrow = CreateText("Arrow", iconObject.transform, expanded ? "▼" : "▶", 12, TextAlignmentOptions.Center);
            arrow.color = new Color(0.28f, 0.30f, 0.30f, 0.72f);
            Stretch(arrow.rectTransform(), 0f, 0f);
        }

        private static void AddButtonIcon(Transform parent, string spriteName, string fallbackText)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            Stretch(iconRect, 5f, 3f);

            Sprite sprite = GetSpriteByName(spriteName);
            if (sprite != null)
            {
                Image icon = iconObject.AddComponent<Image>();
                icon.sprite = sprite;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = new Color(0.92f, 0.94f, 0.96f, 1f);
                return;
            }

            TextMeshProUGUI text = CreateText("FallbackText", iconObject.transform, fallbackText, 10, TextAlignmentOptions.Center);
            text.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            Stretch(text.rectTransform(), 0f, 0f);
        }

        private static void AddButtonIconLabel(Transform parent, string spriteName, string fallbackText, string labelText)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(5f, 0f);
            iconRect.sizeDelta = new Vector2(16f, 16f);

            Sprite sprite = GetSpriteByName(spriteName);
            if (sprite != null)
            {
                Image icon = iconObject.AddComponent<Image>();
                icon.sprite = sprite;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            }
            else
            {
                TextMeshProUGUI fallback = CreateText("FallbackText", iconObject.transform, fallbackText, 10, TextAlignmentOptions.Center);
                fallback.color = new Color(0.92f, 0.94f, 0.96f, 1f);
                Stretch(fallback.rectTransform(), 0f, 0f);
            }

            TextMeshProUGUI label = CreateText("Label", parent, labelText, 11, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform labelRect = label.rectTransform();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 0f);
            labelRect.offsetMax = new Vector2(-5f, 0f);
        }
    }
}
