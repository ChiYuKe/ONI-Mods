using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class SolidOutputConstructionReservePatch
    {
        [HarmonyPatch(typeof(StorageNetworkSolidOutputPortEgress), "OnSpawn")]
        public static class SolidOutputPortOnSpawnPatch
        {
            public static void Postfix(StorageNetworkSolidOutputPortEgress __instance)
            {
                StorageNetworkConstructionSupplyService.RegisterSolidOutputPort(__instance?.PortStorage);
            }
        }

        [HarmonyPatch(typeof(StorageNetworkSolidOutputPortEgress), "OnCleanUp")]
        public static class SolidOutputPortOnCleanUpPatch
        {
            public static void Prefix(StorageNetworkSolidOutputPortEgress __instance)
            {
                StorageNetworkConstructionSupplyService.UnregisterSolidOutputPort(__instance?.PortStorage);
            }
        }

        [HarmonyPatch(typeof(SolidConduitDispenser), "FindSuitableItem", new System.Type[0])]
        public static class SolidConduitDispenserFindSuitableItemPatch
        {
            public static void Postfix(SolidConduitDispenser __instance, ref Pickupable __result)
            {
                if (__instance == null ||
                    !StorageNetworkConstructionSupplyService.IsRegisteredSolidOutputPort(__instance.storage) ||
                    __result == null ||
                    !StorageNetworkConstructionSupplyService.IsConstructionReserved(__result))
                {
                    return;
                }

                StorageNetworkConstructionSupplyService.RecordLegacyReservedSelection();
                __result = FindUnreservedItem(__instance.storage);
            }

            private static Pickupable FindUnreservedItem(Storage storage)
            {
                if (storage?.items == null)
                {
                    return null;
                }

                foreach (UnityEngine.GameObject item in storage.items)
                {
                    Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
                    if (pickupable != null &&
                        !StorageNetworkConstructionSupplyService.IsConstructionReserved(pickupable))
                    {
                        return pickupable;
                    }
                }

                return null;
            }
        }

        [HarmonyPatch(typeof(Storage), "Transfer", new[] { typeof(UnityEngine.GameObject), typeof(Storage), typeof(bool), typeof(bool) })]
        public static class StorageTransferPatch
        {
            public static void Prefix(Storage __instance, UnityEngine.GameObject go)
            {
                if (go != null &&
                    StorageNetworkConstructionSupplyService.IsRegisteredSolidOutputPort(__instance))
                {
                    StorageNetworkConstructionSupplyService.ClearLegacyTagsForSolidOutputTransfer(go);
                }
            }
        }

        [HarmonyPatch(typeof(FetchChore), "Begin")]
        public static class FetchChoreBeginPatch
        {
            public static void Prefix(ref Chore.Precondition.Context context)
            {
                Pickupable pickupable = context.data as Pickupable;
                StorageNetworkConstructionSupplyService.ClearBufferMarkerForFetch(pickupable);
            }
        }
    }
}
