using StorageNetwork.Components;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Core
{
    public static class StorageCategories
    {
        private const string SceneStorageKey = "scene_storage";
        private const string VanillaStorageKey = "vanilla_storage";
        private const string RecipeBuildingKey = "recipe_building";
        private const string ModStorageKey = "mod_storage";
        public const string GeyserKey = "geyser";

        public static string GetKey(Storage storage)
        {
            if (storage == null)
            {
                return SceneStorageKey;
            }

            if (HasModStorageTag(storage))
            {
                return ModStorageKey;
            }

            StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            if (enrollment == null || !enrollment.IncludedInSceneNetwork)
            {
                return SceneStorageKey;
            }

            return enrollment.IsComplexRecipeBuilding() ? RecipeBuildingKey : VanillaStorageKey;
        }

        public static string GetName(string key)
        {
            if (key == GeyserKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_GEYSER);
            }

            if (key == RecipeBuildingKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_RECIPE_BUILDING);
            }

            if (key == ModStorageKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_MOD_STORAGE);
            }

            return key == VanillaStorageKey
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_VANILLA_STORAGE)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_SCENE_STORAGE);
        }

        public static int GetOrder(string key)
        {
            if (key == GeyserKey)
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
