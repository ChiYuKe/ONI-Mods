using HarmonyLib;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkWorldInventoryMirrorPatch
    {
        [HarmonyPatch(typeof(WorldInventory), nameof(WorldInventory.GetTotalAmount))]
        public static class WorldInventoryGetTotalAmountPatch
        {
            public static void Postfix(WorldInventory __instance, Tag tag, bool includeRelatedWorlds, ref float __result)
            {
                __result += StorageNetworkWorldInventoryMirrorService.GetMirroredAmount(__instance, tag, includeRelatedWorlds);
            }
        }

        [HarmonyPatch(typeof(WorldInventory), nameof(WorldInventory.GetAmountWithoutTag))]
        public static class WorldInventoryGetAmountWithoutTagPatch
        {
            public static void Postfix(WorldInventory __instance, Tag tag, bool includeRelatedWorlds, Tag[] forbiddenTags, ref float __result)
            {
                if (forbiddenTags == null)
                {
                    return;
                }

                __result += StorageNetworkWorldInventoryMirrorService.GetMirroredAmount(__instance, tag, includeRelatedWorlds, forbiddenTags);
            }
        }
    }
}
