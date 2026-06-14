using StorageNetwork.Components;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Core
{
    public static class StorageCategories
    {
        private const string VanillaStorageKey = "vanilla_storage";
        private const string RecipeBuildingKey = "recipe_building";
        private const string EnergyGeneratorKey = "energy_generator";
        public const string ModStorageKey = "mod_storage";
        public const string InputPortKey = "input_port";
        public const string OutputPortKey = "output_port";
        public const string MinionKey = "minion";
        public const string GeyserKey = "geyser";

        public static string GetKey(Storage storage)
        {
            if (storage == null)
            {
                return VanillaStorageKey;
            }

            if (HasCategoryModStorageTag(storage))
            {
                return ModStorageKey;
            }

            if (StorageNetworkStorageRules.HasInputPortTag(storage))
            {
                return InputPortKey;
            }

            if (StorageNetworkStorageRules.HasOutputPortTag(storage))
            {
                return OutputPortKey;
            }

            StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            if (enrollment == null || !enrollment.IncludedInSceneNetwork)
            {
                return VanillaStorageKey;
            }

            if (StorageNetworkStorageRules.HasInputPortTag(storage))
            {
                return InputPortKey;
            }

            if (StorageNetworkStorageRules.HasOutputPortTag(storage))
            {
                return OutputPortKey;
            }

            if (HasModStorageTag(storage))
            {
                return ModStorageKey;
            }

            if (enrollment.IsEnergyGeneratorBuilding())
            {
                return EnergyGeneratorKey;
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

            if (key == EnergyGeneratorKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_ENERGY_GENERATOR);
            }

            if (key == ModStorageKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_MOD_STORAGE);
            }

            if (key == InputPortKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_INPUT_PORT);
            }

            if (key == OutputPortKey)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.CATEGORY_OUTPUT_PORT);
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

            if (key == EnergyGeneratorKey)
            {
                return 4;
            }

            if (key == ModStorageKey)
            {
                return 2;
            }

            if (key == InputPortKey)
            {
                return 3;
            }

            if (key == OutputPortKey)
            {
                return 3;
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

        private static bool HasCategoryModStorageTag(Storage storage)
        {
            return HasTag(storage, StorageSceneTags.CategoryModStorage);
        }

        private static bool HasTag(Storage storage, Tag tag)
        {
            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            return prefabId != null && prefabId.HasTag(tag);
        }
    }
}
