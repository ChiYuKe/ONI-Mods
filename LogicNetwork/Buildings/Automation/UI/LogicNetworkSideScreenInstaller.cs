using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LogicNetwork.UI;
using UnityEngine;

namespace LogicNetwork.UI.Installers
{
    internal static class LogicNetworkSideScreenInstaller
    {
        private static readonly FieldInfo SideScreensField = AccessTools.Field(typeof(DetailsScreen), "sideScreens");

        public static void Install(DetailsScreen detailsScreen)
        {
            if (detailsScreen == null || SideScreensField == null)
            {
                return;
            }

            List<DetailsScreen.SideScreenRef> sideScreens = SideScreensField.GetValue(detailsScreen) as List<DetailsScreen.SideScreenRef>;
            if (sideScreens == null || HasSideScreen<LogicNetworkSideScreen>(sideScreens))
            {
                return;
            }

            GameObject root = new GameObject("LogicNetworkSideScreen");
            root.SetActive(false);
            LogicNetworkSideScreen screen = root.AddComponent<LogicNetworkSideScreen>();
            sideScreens.Add(new DetailsScreen.SideScreenRef
            {
                name = "LogicNetworkSideScreen",
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
