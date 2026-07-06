using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.UI.Installers
{
    internal static class StorageNetworkLogicDiySideScreenInstaller
    {
        private static readonly FieldInfo SideScreensField = AccessTools.Field(typeof(DetailsScreen), "sideScreens");

        public static void Install(DetailsScreen detailsScreen)
        {
            if (detailsScreen == null || SideScreensField == null)
            {
                return;
            }

            List<DetailsScreen.SideScreenRef> sideScreens = SideScreensField.GetValue(detailsScreen) as List<DetailsScreen.SideScreenRef>;
            if (sideScreens == null || HasSideScreen<StorageNetworkLogicDiySideScreen>(sideScreens))
            {
                return;
            }

            GameObject root = new GameObject("StorageNetworkLogicDiySideScreen");
            root.SetActive(false);
            StorageNetworkLogicDiySideScreen screen = root.AddComponent<StorageNetworkLogicDiySideScreen>();
            sideScreens.Add(new DetailsScreen.SideScreenRef
            {
                name = "StorageNetworkLogicDiySideScreen",
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
