using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkTags
    {
        public static readonly Tag Storage = TagManager.Create("StorageNetworkCategoryStorage");
        public static readonly Tag Liquid = TagManager.Create("StorageNetworkCategoryLiquid");
        public static readonly Tag Gas = TagManager.Create("StorageNetworkCategoryGas");
        public static readonly Tag Conveyor = TagManager.Create("StorageNetworkCategoryConveyor");
        public static readonly Tag Other = TagManager.Create("StorageNetworkCategoryOther");

        public static readonly Tag[] Categories =
        {
            Storage,
            Liquid,
            Gas,
            Conveyor,
            Other
        };

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
    }
}
