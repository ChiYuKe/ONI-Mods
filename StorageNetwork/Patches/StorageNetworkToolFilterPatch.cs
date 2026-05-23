using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.UI;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkToolFilterPatch
    {
        [HarmonyPatch(typeof(FilteredDragTool), "GetDefaultFilters")]
        public static class FilteredDragToolGetDefaultFiltersPatch
        {
            public static void Postfix(Dictionary<string, ToolParameterMenu.ToggleState> filters)
            {
                if (!filters.ContainsKey(StorageNetworkToolFilters.StorageNetwork))
                {
                    filters.Add(StorageNetworkToolFilters.StorageNetwork, ToolParameterMenu.ToggleState.Off);
                }
            }
        }

        [HarmonyPatch(typeof(FilteredDragTool), nameof(FilteredDragTool.GetFilterLayerFromGameObject))]
        public static class FilteredDragToolGetFilterLayerFromGameObjectPatch
        {
            public static bool Prefix(GameObject input, ref string __result)
            {
                if (input != null && input.GetComponent<StorageNetworkCable>() != null)
                {
                    __result = StorageNetworkToolFilters.StorageNetwork;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(FilteredDragTool), "GetObjectLayerFromFilterLayer")]
        public static class FilteredDragToolGetObjectLayerFromFilterLayerPatch
        {
            public static bool Prefix(string filter_layer, ref ObjectLayer __result)
            {
                if (filter_layer == StorageNetworkToolFilters.StorageNetwork)
                {
                    __result = ObjectLayer.LogicWire;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ToolParameterMenu), nameof(ToolParameterMenu.PopulateMenu))]
        public static class ToolParameterMenuPopulateMenuPatch
        {
            public static void Prefix(Dictionary<string, ToolParameterMenu.ToggleState> parameters)
            {
                if (parameters == null || OverlayScreen.Instance == null || OverlayScreen.Instance.mode != StorageNetworkOverviewMode.ID)
                {
                    return;
                }

                foreach (string key in new List<string>(parameters.Keys))
                {
                    parameters[key] = key == StorageNetworkToolFilters.StorageNetwork
                        ? ToolParameterMenu.ToggleState.On
                        : ToolParameterMenu.ToggleState.Disabled;
                }

                if (!parameters.ContainsKey(StorageNetworkToolFilters.StorageNetwork))
                {
                    parameters.Add(StorageNetworkToolFilters.StorageNetwork, ToolParameterMenu.ToggleState.On);
                }
            }
        }
    }
}
