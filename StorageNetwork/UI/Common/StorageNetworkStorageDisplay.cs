using StorageNetwork.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkStorageDisplay
    {
        public static string GetCategoryKey(Storage storage)
        {
            return StorageCategories.GetKey(storage);
        }

        public static string GetCategoryKey(StorageInfo storageInfo)
        {
            if (storageInfo != null && storageInfo.Minion != null)
            {
                return StorageCategories.MinionKey;
            }

            return storageInfo != null && storageInfo.Geyser != null
                ? StorageCategories.GeyserKey
                : GetCategoryKey(storageInfo?.Storage);
        }

        public static string GetTypeKey(StorageInfo storageInfo)
        {
            if (storageInfo?.Minion != null)
            {
                return StorageCategories.MinionKey;
            }

            if (storageInfo?.Geyser != null)
            {
                return GetObjectPrefabKey(storageInfo.GameObject, GetTypeName(storageInfo));
            }

            return GetPrefabKey(storageInfo?.Storage, GetTypeName(storageInfo));
        }

        public static string GetPrefabKey(Storage storage, string fallback = null)
        {
            KPrefabID prefabId = storage?.GetComponent<KPrefabID>();
            return prefabId != null ? prefabId.PrefabID().ToString() : (fallback ?? string.Empty);
        }

        public static string GetTypeName(StorageInfo storageInfo)
        {
            if (storageInfo?.Minion != null)
            {
                return StorageCategories.GetName(StorageCategories.MinionKey);
            }

            GameObject gameObject = storageInfo?.GameObject;
            return gameObject != null ? gameObject.GetProperName() : storageInfo.Name;
        }

        public static Sprite GetTypeIcon(StorageInfo storageInfo, out Color tint)
        {
            tint = Color.white;
            GameObject gameObject = storageInfo?.GameObject;
            KPrefabID prefabId = gameObject != null ? gameObject.GetComponent<KPrefabID>() : null;
            if (prefabId != null)
            {
                var uiSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                tint = uiSprite.second;
                if (uiSprite.first != null)
                {
                    return uiSprite.first;
                }
            }

            return Assets.GetSprite("unknown");
        }

        public static string GetStoredItemName(GameObject item)
        {
            return item != null ? item.GetProperName() : string.Empty;
        }

        public static void SetStoredItemIcon(Image icon, GameObject item)
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

        private static string GetObjectPrefabKey(GameObject gameObject, string fallback = null)
        {
            KPrefabID prefabId = gameObject != null ? gameObject.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.PrefabID().ToString() : (fallback ?? string.Empty);
        }
    }
}
