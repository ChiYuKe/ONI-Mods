using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.UI;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class SideScreenPatch
    {
        private const string SideScreenName = "StorageNetworkHubSideScreen";

        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public static class DetailsScreenOnPrefabInitPatch
        {
            public static void Postfix(DetailsScreen __instance)
            {
                EnsureRegistered(__instance);
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), nameof(DetailsScreen.Refresh))]
        public static class DetailsScreenRefreshPatch
        {
            public static void Prefix(DetailsScreen __instance)
            {
                EnsureRegistered(__instance);
            }
        }

        private static void EnsureRegistered(DetailsScreen detailsScreen)
        {
            if (detailsScreen == null)
            {
                return;
            }

            try
            {
                Register(detailsScreen);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[StorageNetwork] Failed to register side screen: " + exception);
            }
        }

        private static void Register(DetailsScreen detailsScreen)
        {
            Traverse traverse = Traverse.Create(detailsScreen);
            List<DetailsScreen.SideScreenRef> sideScreens =
                traverse.Field("sideScreens").GetValue<List<DetailsScreen.SideScreenRef>>();
            GameObject contentBody = traverse.Field("sideScreenContentBody").GetValue<GameObject>();

            if (sideScreens == null || contentBody == null || sideScreens.Exists(screen => screen.name == SideScreenName))
            {
                return;
            }

            GameObject screenObject = new GameObject(SideScreenName);
            screenObject.transform.SetParent(contentBody.transform, false);
            screenObject.SetActive(false);

            RectTransform rectTransform = screenObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            StorageNetworkHubSideScreen sideScreen = screenObject.AddComponent<StorageNetworkHubSideScreen>();
            sideScreen.name = SideScreenName;
            sideScreen.ContentContainer = screenObject;

            sideScreens.Add(new DetailsScreen.SideScreenRef
            {
                name = SideScreenName,
                offset = Vector2.zero,
                screenPrefab = sideScreen,
                tab = DetailsScreen.SidescreenTabTypes.Config
            });

            Debug.Log("[StorageNetwork] Registered hub side screen.");
        }
    }
}
