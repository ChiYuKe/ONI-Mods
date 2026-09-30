using StorageNetwork.Buildings;
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

            StorageNetwork.API.StorageNetworkDisplayInfo displayInfo =
                StorageNetworkInterfaceResolver.GetDisplayInfo(storageInfo?.Storage);
            if (displayInfo != null && !string.IsNullOrEmpty(displayInfo.TypeKey))
            {
                return displayInfo.TypeKey;
            }

            return GetPrefabKey(storageInfo?.Storage, GetTypeName(storageInfo));
        }

        public static string GetPrefabKey(Storage storage, string fallback = null)
        {
            if (storage != null)
            {
                KPrefabID prefabId = storage.GetComponent<KPrefabID>() ?? storage.GetComponentInParent<KPrefabID>();
                if (prefabId != null)
                {
                    return prefabId.PrefabID().ToString();
                }

                Building building = storage.GetComponent<Building>() ?? storage.GetComponentInParent<Building>();
                if (building != null && building.Def != null)
                {
                    return building.Def.PrefabID;
                }
            }

            return fallback ?? string.Empty;
        }

        public static string GetTypeName(StorageInfo storageInfo)
        {
            if (storageInfo?.Minion != null)
            {
                return StorageCategories.GetName(StorageCategories.MinionKey);
            }

            StorageNetwork.API.StorageNetworkDisplayInfo displayInfo =
                StorageNetworkInterfaceResolver.GetDisplayInfo(storageInfo?.Storage);
            if (displayInfo != null && !string.IsNullOrEmpty(displayInfo.TypeName))
            {
                return displayInfo.TypeName;
            }

            GameObject gameObject = storageInfo?.GameObject;
            if (gameObject != null)
            {
                Building building = gameObject.GetComponent<Building>() ?? gameObject.GetComponentInParent<Building>();
                if (building != null && building.Def != null && !string.IsNullOrEmpty(building.Def.Name))
                {
                    return StorageNetworkTextFormatting.StripKleiLinkFormatting(building.Def.Name);
                }

                KPrefabID prefabId = gameObject.GetComponent<KPrefabID>() ?? gameObject.GetComponentInParent<KPrefabID>();
                if (prefabId != null)
                {
                    BuildingDef buildingDef = Assets.GetBuildingDef(prefabId.PrefabID().Name);
                    if (buildingDef != null && !string.IsNullOrEmpty(buildingDef.Name))
                    {
                        return StorageNetworkTextFormatting.StripKleiLinkFormatting(buildingDef.Name);
                    }

                    GameObject prefab = Assets.GetPrefab(prefabId.PrefabID());
                    if (prefab != null)
                    {
                        string properName = prefab.GetProperName();
                        if (!string.IsNullOrEmpty(properName))
                        {
                            return StorageNetworkTextFormatting.StripKleiLinkFormatting(properName);
                        }
                    }
                }
            }

            return gameObject != null ? gameObject.GetProperName() : storageInfo?.Name ?? string.Empty;
        }

        public static string GetRowName(StorageInfo storageInfo)
        {
            StorageNetwork.API.StorageNetworkDisplayInfo displayInfo =
                StorageNetworkInterfaceResolver.GetDisplayInfo(storageInfo?.Storage);
            if (displayInfo != null && !string.IsNullOrEmpty(displayInfo.RowName))
            {
                return displayInfo.RowName;
            }

            return storageInfo?.Name ?? string.Empty;
        }

        public static Sprite GetTypeIcon(StorageInfo storageInfo, out Color tint)
        {
            tint = Color.white;
            StorageNetwork.API.StorageNetworkDisplayInfo displayInfo =
                StorageNetworkInterfaceResolver.GetDisplayInfo(storageInfo?.Storage);
            if (displayInfo != null && displayInfo.TypeIcon != null)
            {
                tint = displayInfo.TypeIconTint ?? Color.white;
                return displayInfo.TypeIcon;
            }

            GameObject gameObject = storageInfo?.GameObject;
            Building building = gameObject != null
                ? (gameObject.GetComponent<Building>() ?? gameObject.GetComponentInParent<Building>())
                : null;
            BuildingDef buildingDef = building?.Def;

            KPrefabID prefabId = gameObject != null
                ? (gameObject.GetComponent<KPrefabID>() ?? gameObject.GetComponentInParent<KPrefabID>())
                : null;

            if (buildingDef == null && prefabId != null)
            {
                buildingDef = Assets.GetBuildingDef(prefabId.PrefabID().Name);
            }

            if (buildingDef == null && storageInfo?.Storage != null)
            {
                string prefabKey = GetPrefabKey(storageInfo.Storage);
                if (!string.IsNullOrEmpty(prefabKey))
                {
                    buildingDef = Assets.GetBuildingDef(prefabKey);
                }
            }

            if (buildingDef != null)
            {
                Sprite buildingSprite = buildingDef.GetUISprite("ui", false);
                if (buildingSprite != null && buildingSprite != Assets.GetSprite("unknown"))
                {
                    tint = Color.white;
                    return buildingSprite;
                }
            }

            if (gameObject != null)
            {
                var goSprite = Def.GetUISprite(gameObject, "ui", false);
                if (goSprite?.first != null && goSprite.first != Assets.GetSprite("unknown"))
                {
                    tint = goSprite.second;
                    return goSprite.first;
                }
            }

            if (prefabId != null)
            {
                var tagSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                if (tagSprite?.first != null && tagSprite.first != Assets.GetSprite("unknown"))
                {
                    tint = tagSprite.second;
                    return tagSprite.first;
                }
            }

            if (storageInfo?.Geyser != null)
            {
                var geyserSprite = Def.GetUISprite(storageInfo.Geyser.gameObject, "ui", false);
                if (geyserSprite?.first != null && geyserSprite.first != Assets.GetSprite("unknown"))
                {
                    tint = geyserSprite.second;
                    return geyserSprite.first;
                }
            }

            if (storageInfo?.Storage != null)
            {
                if (StorageNetworkStorageRules.IsPowerStorageServer(storageInfo.Storage))
                {
                    string prefabKey = GetPrefabKey(storageInfo.Storage);
                    BuildingDef fallbackDef = (!string.IsNullOrEmpty(prefabKey) ? Assets.GetBuildingDef(prefabKey) : null) ??
                                              Assets.GetBuildingDef(LargeBatteryServerConfig.ID) ??
                                              Assets.GetBuildingDef(MediumBatteryServerConfig.ID) ??
                                              Assets.GetBuildingDef(SmallBatteryServerConfig.ID);
                    Sprite fallbackSprite = fallbackDef?.GetUISprite("ui", false);
                    if (fallbackSprite != null && fallbackSprite != Assets.GetSprite("unknown"))
                    {
                        tint = Color.white;
                        return fallbackSprite;
                    }
                }
                else if (StorageNetworkStorageRules.IsParticleStorageServer(storageInfo.Storage))
                {
                    string prefabKey = GetPrefabKey(storageInfo.Storage);
                    BuildingDef fallbackDef = (!string.IsNullOrEmpty(prefabKey) ? Assets.GetBuildingDef(prefabKey) : null) ??
                                              Assets.GetBuildingDef(LargeParticleServerConfig.ID) ??
                                              Assets.GetBuildingDef(MediumParticleServerConfig.ID) ??
                                              Assets.GetBuildingDef(SmallParticleServerConfig.ID);
                    Sprite fallbackSprite = fallbackDef?.GetUISprite("ui", false);
                    if (fallbackSprite != null && fallbackSprite != Assets.GetSprite("unknown"))
                    {
                        tint = Color.white;
                        return fallbackSprite;
                    }
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
