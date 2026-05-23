using HarmonyLib;
using StorageNetwork.Core;

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
                if (__result || storage == null || recipe == null || storage != __instance.inStorage)
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
                if (__instance?.inStorage == null || missingAmounts == null || missingAmounts.Count == 0)
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
                if (recipe == null || __instance.inStorage == null)
                {
                    return;
                }

                StorageNetworkTransferService.TryPullRecipeIngredients(__instance.inStorage, recipe);
            }
        }
    }
}
