using StorageNetwork.Components;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Core
{
    public static class StorageCategories
    {
        private const string SceneStorageKey = "scene_storage";
        private const string VanillaStorageKey = "vanilla_storage";
        private const string RecipeBuildingKey = "recipe_building";

        public static string GetKey(Storage storage)
        {
            if (storage == null)
            {
                return SceneStorageKey;
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
            if (key == RecipeBuildingKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_RECIPE_BUILDING);
            }

            return key == VanillaStorageKey
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_VANILLA_STORAGE)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_SCENE_STORAGE);
        }

        public static int GetOrder(string key)
        {
            if (key == RecipeBuildingKey)
            {
                return 2;
            }

            return key == VanillaStorageKey ? 1 : 0;
        }
    }
}
