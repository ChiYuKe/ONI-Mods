using HarmonyLib;
using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

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

                StorageNetworkSolidInputPortIngress ingress = destination.GetComponent<StorageNetworkSolidInputPortIngress>();
                if (ingress != null &&
                    ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
                {
                    Storage target = ingress.ResolveInputStorage();
                    GameObject item = pickup.gameObject;
                    HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(new[] { destination });
                    if (target == null ||
                        StorageTargetSelector.FindOutputTarget(
                            item,
                            StorageItemUtility.GetStorageMatchTags(item),
                            excluded,
                            target,
                            null,
                            StorageTargetSelector.GetObjectWorldId(destination.gameObject),
                            destination) == null)
                    {
                        __result = false;
                        return;
                    }
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
