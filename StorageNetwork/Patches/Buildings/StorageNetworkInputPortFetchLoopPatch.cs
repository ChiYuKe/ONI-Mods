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
        [System.ThreadStatic]
        private static HashSet<Storage> excludedStorageWorkspace;

        [HarmonyPatch(typeof(FetchManager), nameof(FetchManager.IsFetchablePickup))]
        public static class FetchManagerIsFetchablePickupPatch
        {
            public static void Postfix(Pickupable pickup, FetchChore chore, Storage destination, ref bool __result)
            {
                if (!__result ||
                    pickup == null ||
                    chore == null ||
                    destination == null)
                {
                    return;
                }

                if (!StorageSceneRegistry.IsExplicitlyRegisteredStorage(destination))
                {
                    return;
                }

                StorageNetworkSolidInputPortIngress ingress = destination.GetComponent<StorageNetworkSolidInputPortIngress>();
                if (ingress == null &&
                    !StorageNetworkStorageRules.IsSolidInputPort(destination))
                {
                    return;
                }

                if (chore.choreType != Db.Get().ChoreTypes.StorageFetch)
                {
                    return;
                }

                if (ingress != null &&
                    ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
                {
                    Storage target = ingress.ResolveInputStorage();
                    GameObject item = pickup.gameObject;
                    HashSet<Storage> excluded = excludedStorageWorkspace ??
                        (excludedStorageWorkspace = new HashSet<Storage>());
                    excluded.Clear();
                    excluded.Add(destination);
                    try
                    {
                        if (target == null ||
                            StorageTargetSelector.FindOutputTarget(
                                item,
                                StorageItemUtility.GetStorageMatchTagsNonAlloc(item),
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
                    finally
                    {
                        excluded.Clear();
                    }
                }

                Storage source = pickup.storage;
                if (source == null || source == destination)
                {
                    return;
                }

                if (!StorageSceneRegistry.IsExplicitlyRegisteredStorage(source))
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
