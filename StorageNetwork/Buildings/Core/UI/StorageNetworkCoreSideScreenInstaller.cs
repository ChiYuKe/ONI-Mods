using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.UI.Installers
{
    internal static class StorageNetworkCoreSideScreenInstaller
    {
        private static readonly FieldInfo SideScreensField = AccessTools.Field(typeof(DetailsScreen), "sideScreens");

        public static void Install(DetailsScreen detailsScreen)
        {
            if (detailsScreen == null || SideScreensField == null)
            {
                return;
            }

            List<DetailsScreen.SideScreenRef> sideScreens = SideScreensField.GetValue(detailsScreen) as List<DetailsScreen.SideScreenRef>;
            if (sideScreens == null || HasSideScreen<StorageNetworkCoreSideScreen>(sideScreens))
            {
                return;
            }

            GameObject root = new GameObject("StorageNetworkCoreSideScreen");
            root.SetActive(false);
            StorageNetworkCoreSideScreen screen = root.AddComponent<StorageNetworkCoreSideScreen>();
            sideScreens.Add(new DetailsScreen.SideScreenRef
            {
                name = "StorageNetworkCoreSideScreen",
                screenPrefab = screen,
                offset = Vector2.zero,
                tab = DetailsScreen.SidescreenTabTypes.Config
            });
        }

        private static bool HasSideScreen<T>(List<DetailsScreen.SideScreenRef> sideScreens) where T : SideScreenContent
        {
            foreach (DetailsScreen.SideScreenRef sideScreen in sideScreens)
            {
                if (sideScreen?.screenPrefab is T)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
