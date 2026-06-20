using HarmonyLib;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkLargeStorageMassPatch
    {
        private const float UnsafeVanillaRoundedMassKg = 2147483f;

        [HarmonyPatch(typeof(Storage), nameof(Storage.MassStored))]
        public static class StorageMassStoredPatch
        {
            public static bool Prefix(Storage __instance, ref float __result)
            {
                if (!ShouldUseSafeMassStored(__instance))
                {
                    return true;
                }

                __result = Mathf.Round(__instance.ExactMassStored() * 1000f) / 1000f;
                return false;
            }
        }

        private static bool ShouldUseSafeMassStored(Storage storage)
        {
            return storage != null &&
                   (StorageNetworkStorageRules.IsServerStorage(storage) ||
                    storage.capacityKg >= UnsafeVanillaRoundedMassKg);
        }
    }
}
