using HarmonyLib;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class ConstructableSupplyPatch
    {
        [HarmonyPatch(typeof(Constructable), "OnSpawn")]
        public static class ConstructableOnSpawnPatch
        {
            public static void Postfix(Constructable __instance)
            {
                StorageNetworkConstructionSupplyService.Register(__instance);
            }
        }

        [HarmonyPatch(typeof(Constructable), "OnCleanUp")]
        public static class ConstructableOnCleanUpPatch
        {
            public static void Prefix(Constructable __instance)
            {
                StorageNetworkConstructionSupplyService.Unregister(__instance);
            }
        }
    }
}
