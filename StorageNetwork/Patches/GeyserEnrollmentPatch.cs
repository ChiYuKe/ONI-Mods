using HarmonyLib;
using StorageNetwork.Components;
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
                AddGeyserEnrollment(__result);
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class GameOnSpawnPatch
        {
            public static void Postfix()
            {
                foreach (Geyser geyser in Object.FindObjectsByType<Geyser>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (geyser != null)
                    {
                        AddGeyserEnrollment(geyser.gameObject);
                    }
                }
            }
        }

        private static void AddGeyserEnrollment(GameObject go)
        {
            if (go == null || go.GetComponent<Geyser>() == null)
            {
                return;
            }

            go.AddOrGet<StorageNetworkEnrollment>();
            go.AddOrGet<StorageNetworkGeyserOutput>();
        }
    }
}
