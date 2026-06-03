using StorageNetwork.Components;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Core
{
    public static class StorageCategories
    {
        private const string VanillaStorageKey = "vanilla_storage";
        private const string RecipeBuildingKey = "recipe_building";
        public const string ModStorageKey = "mod_storage";
        public const string MinionKey = "minion";
        public const string GeyserKey = "geyser";

        public static string GetKey(Storage storage)
        {
            if (storage == null)
            {
                return VanillaStorageKey;
            }

            if (HasModStorageTag(storage))
            {
                return ModStorageKey;
            }

            StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            if (enrollment == null || !enrollment.IncludedInSceneNetwork)
            {
                return VanillaStorageKey;
            }

            return enrollment.IsComplexRecipeBuilding() ? RecipeBuildingKey : VanillaStorageKey;
        }

        public static string GetName(string key)
        {
            if (key == GeyserKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_GEYSER);
            }

            if (key == MinionKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_MINION);
            }

            if (key == RecipeBuildingKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_RECIPE_BUILDING);
            }

            if (key == ModStorageKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_MOD_STORAGE);
            }

            return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_VANILLA_STORAGE);
        }

        public static int GetOrder(string key)
        {
            if (key == GeyserKey)
            {
                return 5;
            }

            if (key == MinionKey)
            {
                return 4;
            }

            if (key == RecipeBuildingKey)
            {
                return 3;
            }

            if (key == ModStorageKey)
            {
                return 2;
            }

            return key == VanillaStorageKey ? 1 : 0;
        }

        public static bool HasModStorageTag(Storage storage)
        {
            return StorageNetworkStorageRules.HasModStorageTag(storage);
        }

        public static bool HasShowSettingsButtonTag(Storage storage)
        {
            return StorageNetworkStorageRules.HasSettingsButtonTag(storage);
        }
    }
}
