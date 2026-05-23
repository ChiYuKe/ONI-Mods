using System;
using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.UI;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class OverlayOverviewPatch
    {
        [HarmonyPatch(typeof(OverlayScreen), "RegisterModes")]
        public static class OverlayScreenRegisterModesPatch
        {
            public static void Postfix(OverlayScreen __instance)
            {
                AccessTools.Method(typeof(OverlayScreen), "RegisterMode")
                    ?.Invoke(__instance, new object[] { new StorageNetworkOverviewMode() });
            }
        }

        [HarmonyPatch(typeof(OverlayMenu), "InitializeToggles")]
        public static class OverlayMenuInitializeTogglesPatch
        {
            public static void Postfix(OverlayMenu __instance)
            {
                try
                {
                    List<KIconToggleMenu.ToggleInfo> toggles = Traverse.Create(__instance)
                        .Field("overlayToggleInfos")
                        .GetValue<List<KIconToggleMenu.ToggleInfo>>();

                    if (toggles == null || toggles.Exists(toggle => toggle.text == STRINGS.UI.STORAGE_NETWORK.OVERVIEW_BUTTON))
                    {
                        return;
                    }

                    Type toggleType = AccessTools.Inner(typeof(OverlayMenu), "OverlayToggleInfo");
                    object toggle = Activator.CreateInstance(
                        toggleType,
                        STRINGS.UI.STORAGE_NETWORK.OVERVIEW_BUTTON.ToString(),
                        "OverviewUI_consumables_icon",
                        StorageNetworkOverviewMode.ID,
                        string.Empty,
                        Action.NumActions,
                        STRINGS.UI.STORAGE_NETWORK.OVERVIEW_TOOLTIP.ToString(),
                        STRINGS.UI.STORAGE_NETWORK.OVERVIEW_BUTTON.ToString());

                    toggles.Insert(0, (KIconToggleMenu.ToggleInfo)toggle);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to add overview overlay button: " + exception);
                }
            }
        }

        [HarmonyPatch(typeof(OverlayModes.Logic), nameof(OverlayModes.Logic.GetCustomLegendData))]
        public static class LogicOverlayLegendPatch
        {
            public static void Postfix(ref List<LegendEntry> __result)
            {
                if (__result == null)
                {
                    __result = new List<LegendEntry>();
                }

                __result.Add(new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_INPUT_PORT, STRINGS.UI.STORAGE_NETWORK.LEGEND_INPUT_PORT_TOOLTIP, Color.white, null, Assets.GetSprite("logicInput"), true));
                __result.Add(new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_OUTPUT_PORT, STRINGS.UI.STORAGE_NETWORK.LEGEND_OUTPUT_PORT_TOOLTIP, Color.white, null, Assets.GetSprite("logicOutput"), true));
                __result.Add(new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_INPUT, STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_INPUT_TOOLTIP, StorageNetworkPortVisualizer.ConnectedInputColor, null, null, true));
                __result.Add(new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_OUTPUT, STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_OUTPUT_TOOLTIP, StorageNetworkPortVisualizer.ConnectedOutputColor, null, null, true));
                __result.Add(new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_DISCONNECTED, STRINGS.UI.STORAGE_NETWORK.LEGEND_DISCONNECTED_TOOLTIP, StorageNetworkPortVisualizer.DisconnectedColor, null, null, true));
            }
        }
    }
}
