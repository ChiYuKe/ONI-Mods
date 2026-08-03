using HarmonyLib;
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
            // This prefix runs for every Storage in the colony. Capacity is the only
            // condition that can make the vanilla integer rounding overflow, so keep
            // the miss path to one null/capacity branch and never resolve network flags.
            return storage != null && storage.capacityKg >= UnsafeVanillaRoundedMassKg;
        }
    }
}
