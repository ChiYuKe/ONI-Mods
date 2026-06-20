using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Gameplay;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void AddInventoryCard(Storage storage, ComplexFabricator fabricator)
        {
            if (StorageNetworkStorageRules.IsPowerInputPort(storage) || StorageNetworkStorageRules.IsPowerOutputPort(storage))
            {
                AddPowerInventoryCard(storage);
                return;
            }

            if (StorageNetworkStorageRules.IsParticleInputPort(storage) || StorageNetworkStorageRules.IsParticleOutputPort(storage))
            {
                AddParticleInventoryCard(storage);
                return;
            }

            StorageNetworkPowerStorage powerStorage = storage != null ? storage.GetComponent<StorageNetworkPowerStorage>() : null;
            if (powerStorage != null)
            {
                AddPowerStorageInventoryCard(powerStorage);
                return;
            }

            List<GameObject> items = GetProductionStoredItems(storage, fabricator);
            List<IGrouping<string, GameObject>> groups = GetOrderedProductionItemGroups(items);
            float cardHeight = Mathf.Max(82f, 52f + Mathf.Max(1, groups.Count) * 26f);
            GameObject card = CreateProductionCard("InventoryCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_CONTENT_TITLE), 0f);
            MakeProductionCardAutoHeight(card, cardHeight);
            if (items.Count == 0)
            {
                CreateFinePrint(card.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT));
                productionInventoryView = null;
                return;
            }

            productionInventoryView = new ProductionInventoryCardView();
            foreach (IGrouping<string, GameObject> group in groups)
            {
                float mass = group.Sum(GetStoredItemMass);
                ProductionInventoryRowView row = CreateProductionSettingsItemRow(
                    card.transform,
                    StorageNetworkStorageDisplay.GetStoredItemName(group.FirstOrDefault()),
                    GameUtil.GetFormattedMass(mass),
                    group.FirstOrDefault());
                productionInventoryView.Rows[StorageItemUtility.GetStoredItemKey(group.FirstOrDefault())] = row;
            }
        }

        private void AddPowerInventoryCard(Storage storage)
        {
            float stored = GetPowerPortStoredJoules(storage);
            float capacity = GetPowerPortCapacityJoules(storage);
            GameObject card = CreateProductionCard("InventoryCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_BATTERY_TITLE), 0f);
            MakeProductionCardAutoHeight(card, 86f);
            ProductionInventoryRowView row = CreateProductionSettingsItemRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_PORT_BATTERY_NAME),
                string.Format(
                    "{0} / {1}",
                    GameUtil.GetFormattedJoules(stored, "F1", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(capacity, "F1", GameUtil.TimeSlice.None)),
                null);
            if (row.Icon != null)
            {
                row.Icon.sprite = GetSpriteByName("oni_sprite_assets_5") ?? GetSpriteByName("status_item_electricity") ?? GetSpriteByName("icon_power") ?? GetSpriteByName("unknown");
                row.Icon.color = row.Icon.sprite != null ? Color.white : Color.clear;
            }

            productionInventoryView = null;
        }

        private void AddParticleInventoryCard(Storage storage)
        {
            float stored = StorageNetworkParticleStorageService.GetAvailable(storage != null ? storage.gameObject : null);
            float capacity = StorageNetworkParticleStorageService.GetCapacity(storage != null ? storage.gameObject : null);
            GameObject card = CreateProductionCard("InventoryCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_STORAGE_TITLE), 0f);
            MakeProductionCardAutoHeight(card, 86f);
            ProductionInventoryRowView row = CreateProductionSettingsItemRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_ITEM_NAME),
                string.Format(
                    "{0} / {1}",
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_AMOUNT_VALUE), Mathf.FloorToInt(Mathf.Max(0f, stored))),
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_PORT_AMOUNT_VALUE), Mathf.FloorToInt(Mathf.Max(0f, capacity)))),
                null);
            if (row.Icon != null)
            {
                row.Icon.sprite = GetSpriteByName("status_item_high_energy_particle") ?? GetSpriteByName("ui_icon_radbolt") ?? GetSpriteByName("unknown");
                row.Icon.color = row.Icon.sprite != null ? Color.white : Color.clear;
            }

            productionInventoryView = null;
        }

        private void AddPowerStorageInventoryCard(StorageNetworkPowerStorage powerStorage)
        {
            GameObject card = CreateProductionCard("InventoryCard", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_CONTENT_TITLE), 0f);
            MakeProductionCardAutoHeight(card, 86f);
            ProductionInventoryRowView row = CreateProductionSettingsItemRow(
                card.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.VIRTUAL_POWER_ITEM_NAME),
                string.Format(
                    "{0} / {1}",
                    GameUtil.GetFormattedJoules(powerStorage.RawJoulesAvailable, "F2", GameUtil.TimeSlice.None),
                    GameUtil.GetFormattedJoules(powerStorage.CapacityJoules, "F1", GameUtil.TimeSlice.None)),
                null);
            if (row.Icon != null)
            {
                row.Icon.sprite = GetSpriteByName("oni_sprite_assets_5") ?? GetSpriteByName("status_item_electricity") ?? GetSpriteByName("icon_power") ?? GetSpriteByName("unknown");
                row.Icon.color = row.Icon.sprite != null ? Color.white : Color.clear;
            }

            productionInventoryView = null;
        }

        private void AddProductionSettingsItems(Storage storage, ComplexFabricator fabricator)
        {
            StorageNetworkPowerStorage powerStorage = storage != null ? storage.GetComponent<StorageNetworkPowerStorage>() : null;
            if (powerStorage != null)
            {
                CreateProductionSettingsItemRow(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.VIRTUAL_POWER_ITEM_NAME),
                    string.Format(
                        "{0} / {1}",
                        GameUtil.GetFormattedJoules(powerStorage.RawJoulesAvailable, "F2", GameUtil.TimeSlice.None),
                        GameUtil.GetFormattedJoules(powerStorage.CapacityJoules, "F1", GameUtil.TimeSlice.None)),
                    null);
                return;
            }

            List<GameObject> items = GetProductionStoredItems(storage, fabricator);
            if (items.Count == 0)
            {
                AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT), 12, FontStyles.Normal, 26f);
                return;
            }

            foreach (IGrouping<string, GameObject> group in GetOrderedProductionItemGroups(items))
            {
                float mass = group.Sum(GetStoredItemMass);
                CreateProductionSettingsItemRow(
                    StorageNetworkStorageDisplay.GetStoredItemName(group.FirstOrDefault()),
                    GameUtil.GetFormattedMass(mass),
                    group.FirstOrDefault());
            }
        }

        private ProductionInventoryRowView CreateProductionSettingsItemRow(string itemName, string formattedMass, GameObject representative)
        {
            return CreateProductionSettingsItemRow(productionSettingsContent, itemName, formattedMass, representative);
        }

        private ProductionInventoryRowView CreateProductionSettingsItemRow(Transform parent, string itemName, string formattedMass, GameObject representative)
        {
            GameObject row = CreatePlainImage("ProductionSettingsItemRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 24f;
            rowLayout.preferredHeight = 24f;
            rowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 1, 1);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>();
            iconObject.AddComponent<LayoutElement>().preferredWidth = 20f;
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            StorageNetworkStorageDisplay.SetStoredItemIcon(icon, representative);

            TextMeshProUGUI name = CreateText("Name", row.transform, itemName, 11, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI mass = CreateText("Mass", row.transform, formattedMass, 11, TextAlignmentOptions.MidlineRight);
            mass.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            mass.textWrappingMode = TextWrappingModes.NoWrap;
            mass.gameObject.AddComponent<LayoutElement>().preferredWidth = 92f;
            return new ProductionInventoryRowView
            {
                Name = name,
                Mass = mass,
                Icon = icon
            };
        }

        private void UpdateProductionInventoryCard(Storage storage, ComplexFabricator fabricator)
        {
            if (StorageNetworkStorageRules.IsPowerInputPort(storage) || StorageNetworkStorageRules.IsPowerOutputPort(storage))
            {
                return;
            }

            if (StorageNetworkStorageRules.IsParticleInputPort(storage) || StorageNetworkStorageRules.IsParticleOutputPort(storage))
            {
                return;
            }

            if (productionInventoryView == null)
            {
                return;
            }

            foreach (IGrouping<string, GameObject> group in GetProductionStoredItems(storage, fabricator).GroupBy(StorageItemUtility.GetStoredItemKey))
            {
                string key = group.Key;
                if (!productionInventoryView.Rows.TryGetValue(key, out ProductionInventoryRowView row))
                {
                    continue;
                }

                GameObject representative = group.FirstOrDefault();
                SetTextIfChanged(row.Name, StorageNetworkStorageDisplay.GetStoredItemName(representative));
                SetTextIfChanged(row.Mass, GameUtil.GetFormattedMass(group.Sum(GetStoredItemMass)));
                StorageNetworkStorageDisplay.SetStoredItemIcon(row.Icon, representative);
            }
        }

        private static List<GameObject> GetProductionStoredItems(Storage storage, ComplexFabricator fabricator)
        {
            return StorageNetworkProductionStorageCollector.GetProductionStorages(storage, fabricator)
                .SelectMany(itemStorage => itemStorage.items.Where(item => item != null))
                .ToList();
        }

        private static List<IGrouping<string, GameObject>> GetOrderedProductionItemGroups(IEnumerable<GameObject> items)
        {
            return items
                .GroupBy(StorageItemUtility.GetStoredItemKey)
                .OrderBy(group => StorageNetworkStorageDisplay.GetStoredItemName(group.FirstOrDefault()))
                .ToList();
        }
    }
}
