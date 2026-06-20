using HarmonyLib;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class SolidOutputConstructionReservePatch
    {
        [HarmonyPatch(typeof(SolidConduitDispenser), "FindSuitableItem", new System.Type[0])]
        public static class SolidConduitDispenserFindSuitableItemPatch
        {
            public static void Postfix(SolidConduitDispenser __instance, ref Pickupable __result)
            {
                if (__result == null ||
                    __instance == null ||
                    __instance.storage == null ||
                    __instance.GetComponent<StorageNetwork.Components.StorageNetworkSolidOutputPortEgress>() == null)
                {
                    return;
                }

                if (!StorageNetworkConstructionSupplyService.IsConstructionReserved(__result.gameObject))
                {
                    return;
                }

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
                        !StorageNetworkConstructionSupplyService.IsConstructionReserved(item))
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
                if (__instance != null &&
                    go != null &&
                    __instance.GetComponent<StorageNetwork.Components.StorageNetworkSolidOutputPortEgress>() != null)
                {
                    StorageNetworkConstructionSupplyService.ClearConstructionReservation(go);
                    StorageNetworkConstructionSupplyService.ClearSolidOutputBufferMarker(go);
                }
            }
        }

        [HarmonyPatch(typeof(FetchChore), "Begin")]
        public static class FetchChoreBeginPatch
        {
            public static void Prefix(ref Chore.Precondition.Context context)
            {
                Pickupable pickupable = context.data as Pickupable;
                if (pickupable != null)
                {
                    StorageNetworkConstructionSupplyService.ClearSolidOutputBufferMarker(pickupable.gameObject);
                }
            }
        }
    }
}
