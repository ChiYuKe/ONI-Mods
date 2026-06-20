using HarmonyLib;
using StorageNetwork.Gameplay;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class GeyserEnrollmentPatch
    {
        [HarmonyPatch(
            typeof(GeyserGenericConfig),
            nameof(GeyserGenericConfig.CreateGeyser),
            new[]
            {
                typeof(string),
                typeof(string),
                typeof(int),
                typeof(int),
                typeof(string),
                typeof(string),
                typeof(HashedString),
                typeof(float),
                typeof(string[]),
                typeof(string[])
            })]
        public static class GeyserGenericConfigCreateGeyserPatch
        {
            public static void Postfix(GameObject __result)
            {
                StorageNetworkEnrollmentInstaller.AddGeyserEnrollment(__result);
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class GameOnSpawnPatch
        {
            public static void Postfix()
            {
                StorageNetworkEnrollmentInstaller.RefreshGeyserInstances();
            }
        }
    }
}
