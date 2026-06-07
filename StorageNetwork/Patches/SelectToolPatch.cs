using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StorageNetwork.UI;

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
            StorageNetworkSelectionInputHandler.HandleSelectToolPostfix();
        }
    }
}
