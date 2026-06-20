using HarmonyLib;
using StorageNetwork.Core;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkInputPortFetchLoopPatch
    {
        [HarmonyPatch(typeof(FetchManager), nameof(FetchManager.IsFetchablePickup))]
        public static class FetchManagerIsFetchablePickupPatch
        {
            public static void Postfix(Pickupable pickup, FetchChore chore, Storage destination, ref bool __result)
            {
                if (!__result ||
                    pickup == null ||
                    chore == null ||
                    destination == null ||
                    chore.choreType != Db.Get().ChoreTypes.StorageFetch ||
                    !StorageNetworkStorageRules.IsSolidInputPort(destination))
                {
                    return;
                }

                Storage source = pickup.storage;
                if (source == null || source == destination)
                {
                    return;
                }

                if (StorageNetworkStorageRules.IsServerStorage(source) ||
                    StorageNetworkStorageRules.IsNetworkPortStorage(source))
                {
                    __result = false;
                }
            }
        }
    }
}
