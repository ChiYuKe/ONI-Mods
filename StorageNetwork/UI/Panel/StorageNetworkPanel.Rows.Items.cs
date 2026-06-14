using System.Collections.Generic;
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
            StorageNetworkStorageDisplay.SetStoredItemIcon(icon, representative);

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

        private void CreateVirtualPowerItemRow(Transform parent, StorageNetworkPowerStorage powerStorage)
        {
            GameObject row = new GameObject("VirtualPowerItemRow");
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

            KImage background = row.AddComponent<KImage>();
            background.color = new Color(0.84f, 0.84f, 0.78f, 1f);

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>();
            iconObject.AddComponent<LayoutElement>().preferredWidth = 22f;

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = GetSpriteByName("oni_sprite_assets_5") ?? GetSpriteByName("status_item_electricity") ?? GetSpriteByName("icon_power") ?? GetSpriteByName("unknown");
            icon.color = icon.sprite != null ? Color.white : Color.clear;

            TextMeshProUGUI itemText = CreateText(
                "Text",
                row.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.VIRTUAL_POWER_ITEM_NAME),
                12,
                TextAlignmentOptions.MidlineLeft);
            itemText.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            itemText.textWrappingMode = TextWrappingModes.NoWrap;
            itemText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement itemTextLayout = itemText.gameObject.AddComponent<LayoutElement>();
            itemTextLayout.preferredWidth = 85f;
            itemTextLayout.flexibleWidth = 0f;

            TextMeshProUGUI massText = CreateText(
                "Joules",
                row.transform,
                GameUtil.GetFormattedJoules(powerStorage.RawJoulesAvailable, "F2", GameUtil.TimeSlice.None),
                12,
                TextAlignmentOptions.MidlineLeft);
            massText.color = new Color(0.30f, 0.31f, 0.31f, 1f);
            massText.textWrappingMode = TextWrappingModes.NoWrap;
            massText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

            TextMeshProUGUI detailText = CreateText(
                "Details",
                row.transform,
                string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.VIRTUAL_POWER_ITEM_DETAILS),
                    GameUtil.GetFormattedJoules(powerStorage.CapacityJoules, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(powerStorage.JoulesLostPerCycle, "F1", GameUtil.TimeSlice.None) + "/周期"),
                12,
                TextAlignmentOptions.MidlineLeft);
            detailText.color = new Color(0.36f, 0.38f, 0.38f, 1f);
            detailText.textWrappingMode = TextWrappingModes.NoWrap;
            detailText.overflowMode = TextOverflowModes.Ellipsis;
            detailText.gameObject.AddComponent<LayoutElement>().preferredWidth = 262f;

            ToolTip tooltip = row.AddComponent<ToolTip>();
            tooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.VIRTUAL_POWER_ITEM_TOOLTIP);
        }

        private void CreatePowerPortBatteryRow(Transform parent, Storage storage)
        {
            GameObject row = new GameObject("PowerPortBatteryRow");
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

            KImage background = row.AddComponent<KImage>();
            background.color = new Color(0.84f, 0.84f, 0.78f, 1f);

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>();
            iconObject.AddComponent<LayoutElement>().preferredWidth = 22f;

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = GetSpriteByName("oni_sprite_assets_5") ?? GetSpriteByName("status_item_electricity") ?? GetSpriteByName("icon_power") ?? GetSpriteByName("unknown");
            icon.color = icon.sprite != null ? Color.white : Color.clear;

            TextMeshProUGUI itemText = CreateText(
                "Text",
                row.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_BATTERY_NAME),
                12,
                TextAlignmentOptions.MidlineLeft);
            itemText.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            itemText.textWrappingMode = TextWrappingModes.NoWrap;
            itemText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement itemTextLayout = itemText.gameObject.AddComponent<LayoutElement>();
            itemTextLayout.preferredWidth = 85f;
            itemTextLayout.flexibleWidth = 0f;

            float stored = GetPowerPortStoredJoules(storage);
            float capacity = GetPowerPortCapacityJoules(storage);
            TextMeshProUGUI joulesText = CreateText(
                "Joules",
                row.transform,
                GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None),
                12,
                TextAlignmentOptions.MidlineLeft);
            joulesText.color = new Color(0.30f, 0.31f, 0.31f, 1f);
            joulesText.textWrappingMode = TextWrappingModes.NoWrap;
            joulesText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

            TextMeshProUGUI detailText = CreateText(
                "Details",
                row.transform,
                string.Format("{0} / {1}",
                    GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(capacity, "F1", GameUtil.TimeSlice.None)),
                12,
                TextAlignmentOptions.MidlineLeft);
            detailText.color = new Color(0.36f, 0.38f, 0.38f, 1f);
            detailText.textWrappingMode = TextWrappingModes.NoWrap;
            detailText.overflowMode = TextOverflowModes.Ellipsis;
            detailText.gameObject.AddComponent<LayoutElement>().preferredWidth = 262f;
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
    }
}
