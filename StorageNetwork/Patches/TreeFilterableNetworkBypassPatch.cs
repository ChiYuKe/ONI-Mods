using HarmonyLib;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class TreeFilterableNetworkBypassPatch
    {
        [HarmonyPatch(typeof(TreeFilterable), nameof(TreeFilterable.UpdateFilters))]
        public static class TreeFilterableUpdateFiltersPatch
        {
            public static void Prefix(TreeFilterable __instance)
            {
                Storage storage = __instance != null ? __instance.GetFilterStorage() : null;
                StorageNetworkFilterBypass.Apply(storage);
            }
        }
    }
}
