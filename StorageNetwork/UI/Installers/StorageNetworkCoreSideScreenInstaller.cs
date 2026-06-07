using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.UI.Installers
{
    internal static class StorageNetworkCoreSideScreenInstaller
    {
        private const string ScreenName = "StorageNetworkCoreSideScreen";

        public static void Install(DetailsScreen detailsScreen)
        {
            if (detailsScreen == null)
            {
                return;
            }

            Traverse traverse = Traverse.Create(detailsScreen);
            List<DetailsScreen.SideScreenRef> sideScreens = traverse.Field("sideScreens").GetValue<List<DetailsScreen.SideScreenRef>>();
            GameObject contentBody = traverse.Field("sideScreenContentBody").GetValue<GameObject>();
            if (sideScreens == null || contentBody == null || sideScreens.Any(screen => screen.name == ScreenName))
            {
                return;
            }

            GameObject prefabObject = new GameObject(ScreenName);
            prefabObject.transform.SetParent(contentBody.transform, false);
            prefabObject.SetActive(false);

            StorageNetworkCoreSideScreen sideScreen = prefabObject.AddComponent<StorageNetworkCoreSideScreen>();
            sideScreens.Add(new DetailsScreen.SideScreenRef
            {
                name = ScreenName,
                offset = Vector2.zero,
                screenPrefab = sideScreen,
                tab = DetailsScreen.SidescreenTabTypes.Config
            });
        }
    }
}
