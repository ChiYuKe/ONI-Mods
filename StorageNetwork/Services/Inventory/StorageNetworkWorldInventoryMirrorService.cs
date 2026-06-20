using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkWorldInventoryMirrorService
    {
        public static float GetMirroredAmount(WorldInventory inventory, Tag tag, bool includeRelatedWorlds, Tag[] forbiddenTags = null)
        {
            if (inventory == null || tag == Tag.Invalid || !TryGetWorldId(inventory, out int worldId))
            {
                return 0f;
            }

            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0f;
            }

            return StorageNetworkInventoryIndexService.GetAmount(worldId, includeRelatedWorlds, tag, forbiddenTags);
        }

        public static float GetMirroredEdibleCalories(WorldInventory inventory, bool includeRelatedWorlds, System.Collections.Generic.Dictionary<string, float> unitsById = null)
        {
            if (inventory == null || !TryGetWorldId(inventory, out int worldId))
            {
                return 0f;
            }

            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0f;
            }

            return StorageNetworkInventoryIndexService.GetEdibleCalories(worldId, includeRelatedWorlds, unitsById);
        }

        public static int GetMirroredCountWithAdditionalTag(WorldInventory inventory, Tag tag, Tag additionalTag, bool includeRelatedWorlds)
        {
            if (inventory == null || tag == Tag.Invalid || !TryGetWorldId(inventory, out int worldId))
            {
                return 0;
            }

            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0;
            }

            return StorageNetworkInventoryIndexService.GetCountWithAdditionalTag(worldId, includeRelatedWorlds, tag, additionalTag);
        }

        public static float GetMirroredUnitAmount(WorldInventory inventory, Tag tag, bool includeRelatedWorlds)
        {
            return Mathf.CeilToInt(GetMirroredAmount(inventory, tag, includeRelatedWorlds));
        }

        public static float GetMirroredEdibleCaloriesForId(WorldInventory inventory, bool includeRelatedWorlds, string foodId)
        {
            if (inventory == null || string.IsNullOrEmpty(foodId) || !TryGetWorldId(inventory, out int worldId))
            {
                return 0f;
            }

            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0f;
            }

            return StorageNetworkInventoryIndexService.GetEdibleCaloriesForId(worldId, includeRelatedWorlds, foodId);
        }

        private static bool TryGetWorldId(WorldInventory inventory, out int worldId)
        {
            WorldContainer world = inventory.GetComponent<WorldContainer>();
            if (world != null)
            {
                worldId = world.id;
                return true;
            }

            worldId = StorageTargetSelector.GetObjectWorldId(inventory.gameObject);
            return worldId >= 0;
        }
    }
}
