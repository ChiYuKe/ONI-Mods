using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkTags
    {
        private const string StorageKey = "storage";
        private const string GasProductionKey = "gasProduction";
        private const string PlantingKey = "planting";
        private const string LiquidKey = "liquid";
        private const string GasKey = "gas";
        private const string ConveyorKey = "conveyor";
        private const string IndustrialMachineryKey = "IndustrialMachinery";
        private const string OtherKey = "other";

        /// <summary>
        /// 标记储存网络连接组件，适用于任何可以连接到储存网络的建筑
        /// </summary>
        public static readonly Tag NetworkConnectable = TagManager.Create("StorageNetworkConnectable");


        /// <summary>
        /// 标记气体生产类别，适用于生产气体的建筑
        /// </summary>
        public static readonly Tag GasProduction = TagManager.Create("StorageNetworkCategoryGasProduction");

        public static readonly Tag Planting = TagManager.Create("StorageNetworkCategoryPlanting");

        /// <summary>
        /// 标记储存组件类别，适用于主要功能是储存固体物品的建筑，如储物柜
        /// </summary>
        public static readonly Tag Storage = TagManager.Create("StorageNetworkCategoryStorage");

        /// <summary>
        /// 标记液体储存类别，适用于储存液体的建筑
        /// </summary>
        public static readonly Tag Liquid = TagManager.Create("StorageNetworkCategoryLiquid");

        /// <summary>
        ///  标记气体储存类别，适用于储存气体的建筑
        /// </summary>
        public static readonly Tag Gas = TagManager.Create("StorageNetworkCategoryGas");

        /// <summary>
        /// 标记传送带储存类别，适用于传送带上的储存建筑
        /// </summary>
        public static readonly Tag Conveyor = TagManager.Create("StorageNetworkCategoryConveyor");

        /// <summary>
        /// 标记工业机械储存类别，适用于储存工业原料或产品的建筑，如精炼器
        /// </summary>
        public static readonly Tag IndustrialMachinery = TagManager.Create("IndustrialMachinery");

        /// <summary>
        /// 标记其他储存类别，适用于不符合上述任何类别的储存建筑
        /// </summary>
        public static readonly Tag Other = TagManager.Create("StorageNetworkCategoryOther");

        private static readonly CategoryDefinition[] CategoryDefinitions =
        {
            new CategoryDefinition(GasProduction, GasProductionKey, "气体生产", 0),
            new CategoryDefinition(Planting, PlantingKey, "种植", 1),
            new CategoryDefinition(Storage, StorageKey, "储存箱", 2),
            new CategoryDefinition(Liquid, LiquidKey, "液库", 3),
            new CategoryDefinition(Gas, GasKey, "气库", 4),
            new CategoryDefinition(Conveyor, ConveyorKey, "运输轨道", 5),
            new CategoryDefinition(IndustrialMachinery, IndustrialMachineryKey, "工业设备", 10),
            new CategoryDefinition(Other, OtherKey, "其他", 99)
        };

        public static readonly Tag[] Categories = CategoryDefinitions.Select(category => category.Tag).ToArray();

        public static void MarkStorageNetworkBuilding(GameObject go, Tag category, bool add_in_ns)
        {
            if (go == null)
            {
                return;
            }
            if (add_in_ns == true)
            {
                go.AddTag(NetworkConnectable);
            }
            
            MarkCategory(go, category);
        }

        public static void MarkStorageNetworkBuilding(KPrefabID prefabId, Tag category)
        {
            if (prefabId == null)
            {
                return;
            }

            prefabId.AddTag(NetworkConnectable, false);
            MarkCategory(prefabId, category);
        }

        public static bool CanConnectToNetwork(GameObject go)
        {
            KPrefabID prefabId = go != null ? go.GetComponent<KPrefabID>() : null;
            return prefabId != null && prefabId.HasTag(NetworkConnectable);
        }

        public static void EnsureStorageCategoryTag(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            KPrefabID prefabId = storage.GetComponent<KPrefabID>();
            if (prefabId == null || Categories.Any(prefabId.HasTag))
            {
                return;
            }

            prefabId.AddTag(InferStorageCategoryTag(storage), false);
        }

        public static Tag GetStorageCategoryTag(Storage storage)
        {
            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            if (prefabId == null)
            {
                return Other;
            }

            foreach (Tag category in Categories)
            {
                if (prefabId.HasTag(category))
                {
                    return category;
                }
            }

            return Other;
        }

        public static string GetStorageCategoryKey(Storage storage)
        {
            return GetCategoryDefinition(GetStorageCategoryTag(storage)).Key;
        }

        public static string GetStorageCategoryName(string key)
        {
            foreach (CategoryDefinition category in CategoryDefinitions)
            {
                if (category.Key == key)
                {
                    return category.Name;
                }
            }

            return "其他";
        }

        public static int GetStorageCategoryOrder(string key)
        {
            foreach (CategoryDefinition category in CategoryDefinitions)
            {
                if (category.Key == key)
                {
                    return category.Order;
                }
            }

            return 99;
        }

        private static void MarkCategory(GameObject go, Tag category)
        {
            KPrefabID prefabId = go != null ? go.GetComponent<KPrefabID>() : null;
            MarkCategory(prefabId, category);
        }

        private static void MarkCategory(KPrefabID prefabId, Tag category)
        {
            if (prefabId != null && category.IsValid && Categories.Contains(category))
            {
                prefabId.AddTag(category, false);
            }
        }

        private static CategoryDefinition GetCategoryDefinition(Tag tag)
        {
            CategoryDefinition category = CategoryDefinitions.FirstOrDefault(definition => definition.Tag == tag);
            return category.Tag.IsValid ? category : CategoryDefinitions.Last();
        }

        private static Tag InferStorageCategoryTag(Storage storage)
        {
            if (StorageHasTag(storage, GameTags.Liquid))
            {
                return Liquid;
            }

            if (StorageHasTag(storage, GameTags.Gas))
            {
                return Gas;
            }

            if (StorageHasTag(storage, "Conveyor".ToTag()))
            {
                return Conveyor;
            }

            if (StorageHasTag(storage, GameTags.StorageLocker) || StorageAcceptsSolidItems(storage))
            {
                return Storage;
            }

            return Other;
        }

        private static bool StorageHasTag(Storage storage, Tag tag)
        {
            if (storage == null || !tag.IsValid)
            {
                return false;
            }

            KPrefabID prefabId = storage.GetComponent<KPrefabID>();
            if (prefabId != null && prefabId.HasTag(tag))
            {
                return true;
            }

            return storage.storageFilters != null && storage.storageFilters.Contains(tag);
        }

        private static bool StorageAcceptsSolidItems(Storage storage)
        {
            if (storage == null || storage.storageFilters == null)
            {
                return false;
            }

            return storage.storageFilters.Any(tag =>
                tag == GameTags.Solid ||
                tag == GameTags.Pickupable ||
                tag == GameTags.Edible ||
                tag == GameTags.CookingIngredient ||
                tag == GameTags.ConsumableOre ||
                tag == GameTags.IndustrialIngredient ||
                tag == GameTags.IndustrialProduct ||
                tag == GameTags.Medicine ||
                tag == GameTags.Organics);
        }

        private readonly struct CategoryDefinition
        {
            public CategoryDefinition(Tag tag, string key, string name, int order)
            {
                Tag = tag;
                Key = key;
                Name = name;
                Order = order;
            }

            public Tag Tag { get; }

            public string Key { get; }

            public string Name { get; }

            public int Order { get; }
        }
    }
}
