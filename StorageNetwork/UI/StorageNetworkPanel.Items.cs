using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {


        private void DropSelectedItem(Storage storage, string itemKey, float requestedMass)
        {
            LogDebug(string.Format(
                "DropSelectedItem begin storage={0} storageGO={1} itemKey={2} requestedMass={3:0.###}",
                storage != null ? storage.GetInstanceID().ToString() : "null",
                storage != null ? storage.gameObject.GetProperName() : "null",
                itemKey,
                requestedMass));

            List<GameObject> items = FindStoredItems(storage, itemKey);
            LogDebug(string.Format("DropSelectedItem matched items={0}", items.Count));
            if (storage == null || items.Count == 0)
            {
                selectedItemStorage = null;
                selectedItemKey = null;
                LogDebug("DropSelectedItem abort: missing storage or items");
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                return;
            }

            Tag dropTag = GetStoredItemTag(items[0]);
            float mass = Mathf.Min(requestedMass, GetStoredItemsMass(items));
            LogDebug(string.Format(
                "DropSelectedItem resolved tag={0} tagValid={1} mass={2:0.###} firstItem={3}",
                dropTag,
                dropTag.IsValid,
                mass,
                items[0] != null ? items[0].GetProperName() : "null"));

            bool dropped = false;
            float remainingToDrop = mass;
            foreach (Storage sourceStorage in GetContentStorages(storage))
            {
                if (!dropTag.IsValid || remainingToDrop <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float sourceMass = GetStoredItemsMass(FindStoredItemsInStorage(sourceStorage, itemKey));
                float dropMass = Mathf.Min(remainingToDrop, sourceMass);
                if (dropMass <= 0f)
                {
                    continue;
                }

                bool sourceDropped = sourceStorage.DropSome(dropTag, dropMass, false, false, default(Vector3), true, true);
                dropped |= sourceDropped;
                if (sourceDropped)
                {
                    remainingToDrop -= dropMass;
                }
            }

            LogDebug(string.Format("DropSelectedItem DropSome result={0} remainingMass={1:0.###}", dropped, remainingToDrop));

            if (!dropped)
            {
                foreach (GameObject item in items.ToList())
                {
                    Storage sourceStorage = FindItemStorage(storage, item);
                    GameObject droppedItem = sourceStorage != null ? sourceStorage.Drop(item, true) : null;
                    LogDebug(string.Format(
                        "DropSelectedItem fallback Drop item={0} result={1}",
                        item != null ? item.GetProperName() : "null",
                        droppedItem != null ? droppedItem.GetProperName() : "null"));
                    if (droppedItem != null)
                    {
                        droppedItem.transform.SetPosition(storage.transform.GetPosition());
                    }
                }
            }

            selectedItemStorage = null;
            selectedItemKey = null;
            lastListSignature = null;
            RefreshStoragePanel(StoragePanelRefreshMode.Structure);
            LogDebug("DropSelectedItem end");
        }

        private void TransferSelectedItem(Storage source, string itemKey, Storage destination, float requestedMass)
        {
            List<GameObject> items = FindStoredItems(source, itemKey);
            if (source == null || destination == null || items.Count == 0)
            {
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                return;
            }

            Tag tag = GetStoredItemTag(items[0]);
            float maxTransfer = Mathf.Min(GetStoredItemsMass(items), Mathf.Max(0f, destination.RemainingCapacity()));
            float remaining = Mathf.Clamp(requestedMass, 0f, maxTransfer);
            float transferred = 0f;

            foreach (Storage sourceStorage in GetContentStorages(source))
            {
                while (tag.IsValid && remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    float sourceMass = GetStoredItemsMass(FindStoredItemsInStorage(sourceStorage, itemKey));
                    if (sourceMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    float moved = sourceStorage.Transfer(destination, tag, Mathf.Min(remaining, sourceMass), block_events: false, hide_popups: true);
                    if (moved <= 0f)
                    {
                        break;
                    }

                    transferred += moved;
                    remaining -= moved;
                }

                if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }
            }

            LogDebug(string.Format(
                "TransferSelectedItem source={0} destination={1} tag={2} requested={3:0.###} transferred={4:0.###}",
                source.gameObject.GetProperName(),
                destination.gameObject.GetProperName(),
                tag,
                requestedMass,
                transferred));

            selectedItemStorage = null;
            selectedItemKey = null;
            lastListSignature = null;
            RefreshStoragePanel(StoragePanelRefreshMode.Structure);
        }

        private static float GetStoredItemsMass(IEnumerable<GameObject> items)
        {
            return items?.Sum(GetStoredItemMass) ?? 0f;
        }

        private static float GetStoredItemMass(GameObject item)
        {
            if (item == null)
            {
                return 0f;
            }

            PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
            return primaryElement != null ? primaryElement.Mass : 0f;
        }

        private static List<GameObject> FindStoredItems(Storage storage, string itemKey)
        {
            return GetContentStorages(storage)
                .SelectMany(contentStorage => FindStoredItemsInStorage(contentStorage, itemKey))
                .ToList();
        }

        private static List<GameObject> FindStoredItemsInStorage(Storage storage, string itemKey)
        {
            return storage?.items
                .Where(item => item != null && GetStoredItemKey(item) == itemKey)
                .ToList() ?? new List<GameObject>();
        }

        private static IEnumerable<Storage> GetContentStorages(Storage storage)
        {
            HashSet<Storage> storages = new HashSet<Storage>();
            AddContentStorage(storages, storage);

            ComplexFabricator fabricator = storage != null ? storage.GetComponent<ComplexFabricator>() : null;
            if (fabricator != null)
            {
                AddContentStorage(storages, fabricator.inStorage);
                AddContentStorage(storages, fabricator.buildStorage);
                AddContentStorage(storages, fabricator.outStorage);
            }

            return storages;
        }

        private static void AddContentStorage(HashSet<Storage> storages, Storage storage)
        {
            if (storage != null)
            {
                storages.Add(storage);
            }
        }

        private static Storage FindItemStorage(Storage ownerStorage, GameObject item)
        {
            return GetContentStorages(ownerStorage)
                .FirstOrDefault(storage => storage.items.Contains(item));
        }

        private static Tag GetStoredItemTag(GameObject item)
        {
            if (item == null)
            {
                return Tag.Invalid;
            }

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                return prefabId.PrefabID();
            }

            PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
            return primaryElement != null ? primaryElement.ElementID.CreateTag() : Tag.Invalid;
        }

        private IEnumerable<StorageNetworkCategoryGroup> BuildCategoryGroups(IEnumerable<StorageInfo> storages)
        {
            Dictionary<string, StorageNetworkCategoryGroup> groups = new Dictionary<string, StorageNetworkCategoryGroup>();
            foreach (StorageInfo storageInfo in storages)
            {
                string key = GetStorageCategoryKey(storageInfo);
                if (!groups.TryGetValue(key, out StorageNetworkCategoryGroup group))
                {
                    group = new StorageNetworkCategoryGroup(key, GetStorageCategoryName(key));
                    groups.Add(key, group);
                }

                group.Storages.Add(storageInfo);
            }

            return groups.Values.OrderBy(group => GetStorageCategoryOrder(group.Key));
        }

        private void EnsureSelectedCategory(List<StorageNetworkCategoryGroup> groups)
        {
            if (groups.Count == 0)
            {
                selectedCategoryKey = null;
                return;
            }

            if (string.IsNullOrEmpty(selectedCategoryKey) || groups.All(group => group.Key != selectedCategoryKey))
            {
                selectedCategoryKey = groups[0].Key;
            }
        }

        private static string GetStorageCategoryKey(Storage storage)
        {
            return StorageCategories.GetKey(storage);
        }

        private static string GetStorageCategoryKey(StorageInfo storageInfo)
        {
            if (storageInfo != null && storageInfo.Minion != null)
            {
                return StorageCategories.MinionKey;
            }

            return storageInfo != null && storageInfo.Geyser != null
                ? StorageCategories.GeyserKey
                : GetStorageCategoryKey(storageInfo?.Storage);
        }

        private static string GetStorageCategoryName(string key)
        {
            return StorageCategories.GetName(key);
        }

        private static int GetStorageCategoryOrder(string key)
        {
            return StorageCategories.GetOrder(key);
        }

        private static string GetStoredItemKey(GameObject item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                return prefabId.PrefabID().ToString();
            }

            PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
            return primaryElement != null ? primaryElement.ElementID.ToString() : item.name;
        }

        private static string GetStorageTypeKey(StorageInfo storageInfo)
        {
            if (storageInfo?.Minion != null)
            {
                return StorageCategories.MinionKey;
            }

            if (storageInfo?.Geyser != null)
            {
                return GetObjectPrefabKey(storageInfo.GameObject, GetStorageTypeName(storageInfo));
            }

            return GetStoragePrefabKey(storageInfo?.Storage, GetStorageTypeName(storageInfo));
        }

        private static string GetStoragePrefabKey(Storage storage, string fallback = null)
        {
            KPrefabID prefabId = storage?.GetComponent<KPrefabID>();
            return prefabId != null ? prefabId.PrefabID().ToString() : (fallback ?? string.Empty);
        }

        private static string GetStorageTypeName(StorageInfo storageInfo)
        {
            if (storageInfo?.Minion != null)
            {
                return StorageCategories.GetName(StorageCategories.MinionKey);
            }

            GameObject gameObject = storageInfo?.GameObject;
            return gameObject != null ? gameObject.GetProperName() : storageInfo.Name;
        }

        private static string GetObjectPrefabKey(GameObject gameObject, string fallback = null)
        {
            KPrefabID prefabId = gameObject != null ? gameObject.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.PrefabID().ToString() : (fallback ?? string.Empty);
        }

        private static string GetStoredItemName(GameObject item)
        {
            return item != null ? item.GetProperName() : string.Empty;
        }

        private static void SetStoredItemIcon(Image icon, GameObject item)
        {
            if (icon == null || item == null)
            {
                return;
            }

            Sprite sprite = null;
            Color tint = Color.white;

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                var uiSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                sprite = uiSprite.first;
                tint = uiSprite.second;
            }

            if (sprite == null)
            {
                PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                if (primaryElement != null)
                {
                    var uiSprite = Def.GetUISprite(primaryElement.ElementID.CreateTag(), "ui", false);
                    sprite = uiSprite.first;
                    tint = uiSprite.second;
                }
            }

            icon.sprite = sprite;
            icon.color = sprite != null ? tint : Color.clear;
        }

        private static void FocusStorage(Storage storage)
        {
            FocusStorage(storage, 0f);
        }

        private static void FocusStorage(Storage storage, float screenOffsetRightPixels)
        {
            FocusObject(storage != null ? storage.gameObject : null, screenOffsetRightPixels);
        }

        private static void FocusObject(GameObject gameObject, float screenOffsetRightPixels)
        {
            if (gameObject == null || SelectTool.Instance == null)
            {
                return;
            }

            KSelectable selectable = gameObject.GetComponent<KSelectable>();
            if (selectable != null)
            {
                SelectTool.Instance.SelectAndFocus(GetOffsetFocusPosition(gameObject.transform.position, screenOffsetRightPixels), selectable, Vector3.zero);
            }
        }

        private static Vector3 GetOffsetFocusPosition(Vector3 targetPosition, float screenOffsetRightPixels)
        {
            if (screenOffsetRightPixels <= 0f || Camera.main == null || Screen.width <= 0)
            {
                return targetPosition;
            }

            float worldUnitsPerPixel = Camera.main.orthographic
                ? Camera.main.orthographicSize * 2f / Mathf.Max(1f, Screen.height)
                : 1f / Mathf.Max(1f, Screen.height);
            targetPosition.x -= screenOffsetRightPixels * worldUnitsPerPixel;
            return targetPosition;
        }

        private sealed class StorageNetworkCategoryGroup
        {
            public StorageNetworkCategoryGroup(string key, string name)
            {
                Key = key;
                Name = name;
            }

            public string Key { get; }

            public string Name { get; }

            public List<StorageInfo> Storages { get; } = new List<StorageInfo>();
        }
    }
}
