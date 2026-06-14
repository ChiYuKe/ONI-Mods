using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class TreeFilterableNetworkBypassPatch
    {
        [HarmonyPatch(typeof(TreeFilterable), nameof(TreeFilterable.UpdateFilters))]
        public static class TreeFilterableUpdateFiltersPatch
        {
            public static void Prefix(TreeFilterable __instance, ref System.Collections.Generic.HashSet<Tag> filters)
            {
                Storage storage = __instance != null ? __instance.GetFilterStorage() : null;
                StorageNetworkFilterBypass.Apply(storage);
                if (StorageNetworkFilterBypass.ShouldBypassUserFilter(storage))
                {
                    StorageNetworkFilterConfigurator.Configure(__instance);
                }

                if (StorageNetworkFilterSelectionNormalizer.TryNormalize(__instance, filters, out System.Collections.Generic.HashSet<Tag> normalized))
                {
                    filters = normalized;
                }
            }

            public static void Postfix(TreeFilterable __instance)
            {
                StorageNetworkFilterState.MarkUserConfigured(__instance);
                StorageNetworkFilterChangeTransferService.MoveRejectedItemsToNetwork(__instance);
            }
        }
    }
}
