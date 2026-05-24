using HarmonyLib;
using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class NetworkDeliveryPatch
    {
        [HarmonyPatch(typeof(ManualDeliveryKG), nameof(ManualDeliveryKG.RequestDelivery))]
        public static class ManualDeliveryRequestPatch
        {
            public static bool Prefix(ManualDeliveryKG __instance)
            {
                Storage destination = __instance.DebugStorage;
                Tag requestedTag = __instance.RequestedItemTag;
                if (destination == null || !requestedTag.IsValid)
                {
                    return true;
                }

                StorageNetworkFabricatorSettings settings = destination.GetComponent<StorageNetworkFabricatorSettings>();
                if (settings == null || !settings.RequestIngredientsFromNetwork)
                {
                    return true;
                }

                float missingAmount = __instance.Capacity - destination.GetAmountAvailable(requestedTag);
                if (missingAmount <= 0f)
                {
                    return false;
                }

                StorageNetworkTransferService.TryPull(destination, requestedTag, missingAmount, out _);
                return destination.GetAmountAvailable(requestedTag) < __instance.Capacity;
            }
        }

        [HarmonyPatch(typeof(ComplexFabricator), "HasIngredients")]
        public static class ComplexFabricatorHasIngredientsPatch
        {
            public static void Postfix(ComplexFabricator __instance, ComplexRecipe recipe, Storage storage, ref bool __result)
            {
                if (__result || storage == null || recipe == null || storage != __instance.inStorage || !ShouldRequestNetworkIngredients(__instance))
                {
                    return;
                }

                __result = StorageNetworkTransferService.HasRecipeIngredientsAvailable(storage, recipe);
            }
        }

        [HarmonyPatch(typeof(ComplexFabricator), "UpdateFetches")]
        public static class ComplexFabricatorUpdateFetchesPatch
        {
            public static void Prefix(ComplexFabricator __instance, DictionaryPool<Tag, float, ComplexFabricator>.PooledDictionary missingAmounts)
            {
                if (__instance?.inStorage == null ||
                    missingAmounts == null ||
                    missingAmounts.Count == 0 ||
                    !ShouldRequestNetworkIngredients(__instance))
                {
                    return;
                }

                StorageNetworkTransferService.TryPullMissingAmounts(__instance.inStorage, missingAmounts);
            }
        }

        [HarmonyPatch(typeof(ComplexFabricator), "TransferCurrentRecipeIngredientsForBuild")]
        public static class ComplexFabricatorIngredientTransferPatch
        {
            public static void Prefix(ComplexFabricator __instance)
            {
                ComplexRecipe recipe = __instance.CurrentWorkingOrder;
                if (recipe == null || __instance.inStorage == null || !ShouldRequestNetworkIngredients(__instance))
                {
                    return;
                }

                StorageNetworkTransferService.TryPullRecipeIngredients(__instance.inStorage, recipe);
            }
        }

        [HarmonyPatch(typeof(ComplexFabricator), "SpawnOrderProduct")]
        public static class ComplexFabricatorSpawnOrderProductPatch
        {
            public static void Postfix(ComplexFabricator __instance, ComplexRecipe recipe, List<GameObject> __result)
            {
                if (!ShouldStoreProductsToNetwork(__instance))
                {
                    return;
                }

                StorageNetworkTransferService.TryStoreProducedProducts(__instance, __result);
                StorageNetworkTransferService.TryStoreRecipeResults(__instance, recipe);
            }
        }

        private static bool ShouldRequestNetworkIngredients(ComplexFabricator fabricator)
        {
            StorageNetworkFabricatorSettings settings = fabricator != null
                ? fabricator.GetComponent<StorageNetworkFabricatorSettings>()
                : null;
            return settings != null && settings.RequestIngredientsFromNetwork;
        }

        private static bool ShouldStoreProductsToNetwork(ComplexFabricator fabricator)
        {
            StorageNetworkFabricatorSettings settings = fabricator != null
                ? fabricator.GetComponent<StorageNetworkFabricatorSettings>()
                : null;
            return settings != null && settings.StoreProductsToNetwork;
        }
    }
}
