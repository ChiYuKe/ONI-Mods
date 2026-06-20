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

        [HarmonyPatch(typeof(WorldInventory), nameof(WorldInventory.GetCountWithAdditionalTag))]
        public static class WorldInventoryGetCountWithAdditionalTagPatch
        {
            public static void Postfix(WorldInventory __instance, Tag tag, Tag additionalTag, bool includeRelatedWorlds, ref int __result)
            {
                __result += StorageNetworkWorldInventoryMirrorService.GetMirroredCountWithAdditionalTag(__instance, tag, additionalTag, includeRelatedWorlds);
            }
        }

        [HarmonyPatch(typeof(ReceptacleSideScreen), "GetAvailableAmount")]
        public static class ReceptacleSideScreenGetAvailableAmountPatch
        {
            public static void Postfix(ReceptacleSideScreen __instance, Tag tag, ref float __result)
            {
                SingleEntityReceptacle target = Traverse.Create(__instance).Field("targetReceptacle").GetValue<SingleEntityReceptacle>();
                WorldInventory inventory = target != null ? target.GetMyWorld()?.worldInventory : null;
                __result += StorageNetworkWorldInventoryMirrorService.GetMirroredUnitAmount(inventory, tag, true);
            }
        }
    }
}
