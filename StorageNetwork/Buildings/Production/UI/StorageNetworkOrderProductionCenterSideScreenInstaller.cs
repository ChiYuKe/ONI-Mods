using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.UI.Installers
{
    internal static class StorageNetworkOrderProductionCenterSideScreenInstaller
    {
        private static readonly FieldInfo SideScreensField = AccessTools.Field(typeof(DetailsScreen), "sideScreens");

        public static void Install(DetailsScreen detailsScreen)
        {
            if (detailsScreen == null || SideScreensField == null)
            {
                return;
            }

            List<DetailsScreen.SideScreenRef> sideScreens = SideScreensField.GetValue(detailsScreen) as List<DetailsScreen.SideScreenRef>;
            if (sideScreens == null)
            {
                return;
            }

            RemoveSideScreen<StorageNetworkOrderProductionCenterDiskSideScreen>(sideScreens);

            if (!HasSideScreen<StorageNetworkOrderProductionCenterSideScreen>(sideScreens))
            {
                GameObject root = new GameObject("StorageNetworkOrderProductionCenterSideScreen");
                root.SetActive(false);
                StorageNetworkOrderProductionCenterSideScreen screen = root.AddComponent<StorageNetworkOrderProductionCenterSideScreen>();
                sideScreens.Add(new DetailsScreen.SideScreenRef
                {
                    name = "StorageNetworkOrderProductionCenterSideScreen",
                    screenPrefab = screen,
                    offset = Vector2.zero,
                    tab = DetailsScreen.SidescreenTabTypes.Config
                });
            }

            if (!HasSideScreen<StorageNetworkEngravingDiskSideScreen>(sideScreens))
            {
                GameObject root = new GameObject("StorageNetworkEngravingDiskSideScreen");
                root.SetActive(false);
                StorageNetworkEngravingDiskSideScreen screen = root.AddComponent<StorageNetworkEngravingDiskSideScreen>();
                sideScreens.Add(new DetailsScreen.SideScreenRef
                {
                    name = "StorageNetworkEngravingDiskSideScreen",
                    screenPrefab = screen,
                    offset = Vector2.zero,
                    tab = DetailsScreen.SidescreenTabTypes.Config
                });
            }
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

        private static void RemoveSideScreen<T>(List<DetailsScreen.SideScreenRef> sideScreens) where T : SideScreenContent
        {
            for (int i = sideScreens.Count - 1; i >= 0; i--)
            {
                if (sideScreens[i]?.screenPrefab is T)
                {
                    sideScreens.RemoveAt(i);
                }
            }
        }
    }
}
