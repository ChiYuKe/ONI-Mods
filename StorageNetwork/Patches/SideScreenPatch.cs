using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.UI.Installers;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class SideScreenPatch
    {
        [HarmonyPatch(typeof(ManagementMenu), "OnPrefabInit")]
        public static class ManagementMenuOnPrefabInitPatch
        {
            public static void Postfix(ManagementMenu __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                try
                {
                    StorageNetworkManagementMenuInstaller.Install(__instance);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to add management menu button: " + exception);
                }
            }
        }

        [HarmonyPatch(typeof(ClusterDestinationSideScreen), "IsValidForTarget")]
        public static class ClusterDestinationSideScreenIsValidForTargetPatch
        {
            public static void Postfix(GameObject target, ref bool __result)
            {
                if (__result || target == null)
                {
                    return;
                }

                StorageNetworkRelayModule relay = target.GetComponent<StorageNetworkRelayModule>();
                RocketModuleCluster module = target.GetComponent<RocketModuleCluster>();
                __result = relay != null &&
                           module != null &&
                           module.CraftInterface != null &&
                           module.CraftInterface.HasClusterDestinationSelector();
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public static class DetailsScreenOnPrefabInitPatch
        {
            public static void Postfix(DetailsScreen __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                try
                {
                    StorageNetworkCoreSideScreenInstaller.Install(__instance);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to add core side screen: " + exception);
                }
            }
        }
    }
}
