using HarmonyLib;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class NotificationScreenPatch
    {
        [HarmonyPatch(typeof(NotificationScreen), "OnPrefabInit")]
        public static class NotificationScreenOnPrefabInitPatch
        {
            public static void Postfix(NotificationScreen __instance)
            {
                try
                {
                    StorageNetworkNotifications.Register(__instance);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to register custom notification: " + exception);
                }
            }
        }
    }
}
