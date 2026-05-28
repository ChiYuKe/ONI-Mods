using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StorageNetwork.UI;
using UnityEngine;

namespace StorageNetwork.Patches
{
    [HarmonyPatch]
    public static class SelectToolPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return AccessTools.GetDeclaredMethods(typeof(SelectTool))
                .Where(method => method.Name == "Select" || method.Name == "SelectAndFocus");
        }

        public static void Postfix()
        {
            if (!IsShiftLeftClick())
            {
                return;
            }

            StorageNetworkWorldTextPanel.HandleSelectionClick();
        }

        private static bool IsShiftLeftClick()
        {
            return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                   (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0));
        }
    }
}
