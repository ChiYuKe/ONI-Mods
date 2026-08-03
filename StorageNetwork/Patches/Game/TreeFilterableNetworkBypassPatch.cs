using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class TreeFilterableNetworkBypassPatch
    {
        [HarmonyPatch(typeof(TreeFilterable), nameof(TreeFilterable.UpdateFilters))]
        public static class TreeFilterableUpdateFiltersPatch
        {
            public static void Prefix(
                TreeFilterable __instance,
                ref System.Collections.Generic.HashSet<Tag> filters,
                out Storage __state)
            {
                if (!StorageNetworkRuntimeCatalog.TryGetStorage(__instance, out __state))
                {
                    return;
                }

                StorageNetworkFilterBypass.Apply(__state);
                if (StorageNetworkFilterBypass.ShouldBypassUserFilter(__state))
                {
                    StorageNetworkFilterConfigurator.Configure(__instance);
                }

                if (StorageNetworkFilterSelectionNormalizer.TryNormalize(__instance, filters, out System.Collections.Generic.HashSet<Tag> normalized))
                {
                    filters = normalized;
                }
            }

            public static void Postfix(TreeFilterable __instance, Storage __state)
            {
                if (__state == null)
                {
                    return;
                }

                StorageNetworkFilterState.MarkUserConfigured(__instance);
                StorageNetworkFilterChangeTransferService.MoveRejectedItemsToNetwork(__instance);
                StorageSceneRegistry.InvalidateCapabilities(__state);
            }
        }
    }
}
